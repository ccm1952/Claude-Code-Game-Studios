// 该文件由Cursor 自动生成

# SP-010 Findings: 性能自动降级架构归属

> **Status**: ✅ 设计决策完成
> **Date**: 2026-04-22

## 结论

**采用方案 B: 全局 Performance Monitor 模块（Foundation Layer）。** 通过 GameEvent 广播质量档变更，各系统独立响应。

## 决策依据

| 条件 | 评估 |
|------|------|
| 只有 Shadow Rendering 需要降级？ | **否** — Settings 系统也需要读写质量档 |
| 需要在设置界面手动切换？ | **是** — TR-settings-001 要求手动质量选择 |
| 多系统响应？ | **是** — Shadow Rendering + Audio (音频质量) + 未来可能的粒子效果 |

### 方案 B 优于方案 A 的核心理由

1. **Settings 集成**: 手动质量切换和自动降级共用同一质量状态机，避免状态冲突
2. **单一数据源**: `PerformanceMonitor` 持有 `currentQualityTier`，Settings UI 和自动降级逻辑均读写此状态
3. **可扩展性**: 新系统只需监听 `Evt_QualityTierChanged` 即可响应

## 架构设计

### Quality Tier 定义

```csharp
public enum QualityTier
{
    Low    = 0,  // 影子分辨率 256, 无软边缘
    Medium = 1,  // 影子分辨率 512, 软边缘 1x
    High   = 2   // 影子分辨率 1024, 软边缘 2x
}
```

### PerformanceMonitor 模块（Foundation Layer）

```csharp
public class PerformanceMonitor : IModule
{
    private QualityTier _currentTier;
    private QualityTier _userPreference;    // Settings 界面设置的值
    private bool _autoDegrade = true;       // 是否启用自动降级
    private float[] _frameTimes = new float[5];
    private int _frameIndex;

    public QualityTier CurrentTier => _currentTier;

    public void SetUserPreference(QualityTier tier)
    {
        _userPreference = tier;
        _autoDegrade = false;               // 手动设置时关闭自动降级
        ApplyTier(tier);
    }

    public void EnableAutoDegrade()
    {
        _autoDegrade = true;
    }

    void OnUpdate()
    {
        if (!_autoDegrade) return;

        _frameTimes[_frameIndex++ % 5] = Time.unscaledDeltaTime * 1000f;

        if (_frameIndex >= 5 && AllFramesAbove(20f) && _currentTier > QualityTier.Low)
            ApplyTier(_currentTier - 1);
    }

    private void ApplyTier(QualityTier newTier)
    {
        if (newTier == _currentTier) return;
        _currentTier = newTier;
        GameEvent.Send<int>(EventId.Evt_QualityTierChanged, (int)newTier);
    }
}
```

### 监听方（示例 — Shadow Rendering）

```csharp
GameEvent.AddEventListener<int>(EventId.Evt_QualityTierChanged, OnQualityChanged);

private void OnQualityChanged(int tier)
{
    switch ((QualityTier)tier)
    {
        case QualityTier.Low:    _shadowRT.Release(); _shadowRT = new RT(256, 256, ...); break;
        case QualityTier.Medium: _shadowRT.Release(); _shadowRT = new RT(512, 512, ...); break;
        case QualityTier.High:   _shadowRT.Release(); _shadowRT = new RT(1024, 1024, ...); break;
    }
}
```

## 质量状态机所有者

| 角色 | 读 | 写 |
|------|:--:|:--:|
| PerformanceMonitor | ✅ | ✅ (自动降级) |
| Settings UI | ✅ | ✅ (通过 SetUserPreference) |
| Shadow Rendering | ✅ (监听) | ❌ |
| Audio System | ✅ (监听) | ❌ |

## ADR-018 / ADR-021 影响

- PerformanceMonitor 归属 **Foundation Layer**
- 新增 `Evt_QualityTierChanged` 事件到 ADR-006 EventId（在 Foundation Layer 区间分配）
- ADR-002 中 Shadow Rendering 改为 **被动监听** 质量变更，不自行管理帧时间统计
