// 该文件由Cursor 自动生成

# Story 004: 质量档位系统（Quality Tier System）

> **Epic**: URP Shadow Rendering
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/urp-shadow-rendering.md`
**Requirements**: `TR-render-006`, `TR-render-003`, `TR-render-004`, `TR-render-013`, `TR-render-014`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

- TR-render-006: 3 quality tiers + 启动时自动检测档位
- TR-render-003: Shadow map 分辨率分级（512/1024/2048）
- TR-render-004: Shadow Cascades 分级（1/2/4 级）
- TR-render-013: 每帧最多 2 个额外光源投射阴影（Max 2 shadow-casting lights）
- TR-render-014: Shadow caster 优先级排序（玩家操作光源 > 上次操作 > 默认主光）

**ADR Governing Implementation**: ADR-002: URP Rendering Pipeline；SP-010 Findings
**ADR Decision Summary**: Shadow 质量档位由全局 `PerformanceMonitor` 拥有，Shadow Rendering 系统是 `Evt_QualityTierChanged` 事件的**被动监听者**，不自行管理帧时间统计。三档配置通过运行时修改 `UniversalRenderPipelineAsset` 属性切换。

**Engine**: Unity 2022.3.62f2 LTS — URP | **Risk**: MEDIUM
**Engine Notes**: URP 2022.3 支持运行时修改 `shadowResolution`、`shadowCascadeCount`、`softShadows`。运行时修改 `UniversalRenderPipelineAsset` 时需在帧边界应用，避免单帧 artifact。

**Control Manifest Rules (this layer)**:
- Required: Shadow quality tier 是 `Evt_QualityTierChanged` 的被动监听者，不自行管理帧时间（SP-010, ADR-018）
- Required: Settings UI 和自动降级共用同一质量状态机，`PerformanceMonitor` 是单一 owner（SP-010）
- Required: 所有质量档位参数（分辨率、cascade 数、软阴影 on/off）来自 Luban `TbRenderConfig`（ADR-007）
- Required: 质量档位 enum 为 `ShadowQualityTier { High, Medium, Low }`，对应 `IShadowRendering.SetQualityTier()`（ADR-002 API Surface）
- Required: 所有 event listener 在 `Init()` 注册，在 `Dispose()` 移除（ADR-006）
- Required: Shadow caster 优先级排序逻辑在 `ShadowCasterPriority` 组件上定义（GDD §8）
- Forbidden: Shadow Rendering 系统不得自行调用 `PerformanceMonitor.RequestDegradation()`（SP-010）
- Forbidden: 不硬编码 `2048`、`1024`、`512` 等分辨率数值（ADR-007）
- Guardrail: 质量档位切换响应时间 ≤ 500ms；Shadow pass 额外 draw calls < 20（ADR-002）

---

## Acceptance Criteria

*From GDD `design/gdd/urp-shadow-rendering.md`，scoped to this story:*

- [ ] **AC-1**: `IShadowRendering.SetQualityTier(ShadowQualityTier tier)` 接口运行时切换三个档位（High/Medium/Low），参数从 Luban `TbRenderConfig` 读取

| 参数 | High | Medium | Low |
|------|:----:|:------:|:---:|
| Main Shadow Map | 2048 | 1024 | 512 |
| Additional Light Shadow | 1024 | 512 | 256 |
| Shadow Atlas | 4096 | 2048 | 1024 |
| Shadow Cascade | 4 | 2 | 1 |
| Soft Shadows | On (PCF) | Off | Off |
| ShadowRT Update | 每帧 | 每帧 | 隔帧 |

- [ ] **AC-2**: Shadow Rendering 模块监听 `Evt_QualityTierChanged` 事件，收到后调用 `SetQualityTier()` 应用，不主动管理帧时间
- [ ] **AC-3**: 档位切换在帧边界（下一帧开始时）生效，不产生单帧渲染 artifact（无全黑帧或全白帧）
- [ ] **AC-4**: 每帧最多 2 个 Additional Light 投射实时阴影；第 3 个及以后光源自动切换为无阴影模式，仅提供照明
- [ ] **AC-5**: Shadow caster 优先级排序：玩家当前操作的光源 > 上次操作的光源 > 场景默认主光；优先级数值来自 Luban config
- [ ] **AC-6**: `IShadowRendering.GetCurrentQualityTier()` 返回当前档位，与 `PerformanceMonitor.CurrentLevel` 匹配
- [ ] **AC-7**: 启动时自动检测设备档位（GPU 型号 / benchmark 得分），选择对应默认档位；检测逻辑可通过 config 覆盖（手动指定）
- [ ] **AC-8**: 档位切换时 Shadow pass 额外 draw calls < 20（Frame Debugger 验证）

---

## Implementation Notes

*Derived from ADR-002, SP-010, control-manifest.md §2.7 & §5.1:*

**事件监听（模块初始化）：**
```csharp
public void OnModuleInit()
{
    GameEvent.AddEventListener<int>(EventId.Evt_QualityTierChanged, OnQualityTierChanged);
    // 启动时检测并应用默认档位
    var defaultTier = DetectDeviceQualityTier();
    ApplyQualityTier(defaultTier);
}

public void OnModuleDestroy()
{
    GameEvent.RemoveEventListener<int>(EventId.Evt_QualityTierChanged, OnQualityTierChanged);
}

private void OnQualityTierChanged(int tier)
{
    ApplyQualityTier((ShadowQualityTier)tier);
}
```

**质量档位应用（从 Luban 读参数）：**
```csharp
private void ApplyQualityTier(ShadowQualityTier tier)
{
    var config = Tables.Instance.TbRenderConfig.Get((int)tier);
    _urpAsset.shadowResolution = config.ShadowResolution;
    _urpAsset.shadowCascadeCount = config.ShadowCascadeCount;
    _urpAsset.softShadows = config.SoftShadowsEnabled;
    _shadowRT.Release();
    _shadowRT = new RenderTexture(config.ShadowRTResolution, config.ShadowRTResolution, 0, RenderTextureFormat.R8);
    _shadowRT.Create();
    _readbackEveryOtherFrame = (tier == ShadowQualityTier.Low);
    _currentTier = tier;
}
```

**Shadow Caster 优先级排序（GDD §8）：**
- `ShadowCasterPriority` 组件附加在光源上，枚举值：`Active`（玩家操作）/ `Recent`（上次操作）/ `Ambient`（装饰光）
- 每帧更新时，只保留优先级最高的 2 个光源的 Shadow enabled = true，其余置 false
- 优先级数值来自 Luban `TbLightConfig.ShadowPriority`

**设备档位自动检测（GDD §3 自动档位选择规则）：**
```csharp
private ShadowQualityTier DetectDeviceQualityTier()
{
    // 低端名单来自 Luban TbDeviceProfile
    if (IsLowEndGPU(SystemInfo.graphicsDeviceName)) return ShadowQualityTier.Low;
    if (SystemInfo.graphicsMemorySize >= 4096) return ShadowQualityTier.High;
    return ShadowQualityTier.Medium;
}
```

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 006**: 自动降级触发逻辑由 `PerformanceMonitor` 管理（本 story 只实现被动监听和应用）
- **Story 007**: Mobile 端额外的分辨率缩放和 LOD 配置（本 story 处理三档基础切换）
- **Story 003**: readback 频率切换（虽然 `_readbackEveryOtherFrame` 在此设置，但 readback 逻辑本身属于 Story 003）

---

## QA Test Cases

*Logic story — 自动化测试规格:*

- **AC-1**: 三档参数正确应用
  - Given: Luban `TbRenderConfig` 中已定义 High/Medium/Low 三行参数
  - When: 分别调用 `SetQualityTier(High)`、`SetQualityTier(Medium)`、`SetQualityTier(Low)`
  - Then: 每次调用后 `_urpAsset.shadowResolution` 分别为 2048/1024/512；`shadowCascadeCount` 为 4/2/1；`softShadows` 为 true/false/false
  - Edge cases: 同一档位重复调用时不重建 ShadowRT（幂等性）

- **AC-2**: 被动监听 Evt_QualityTierChanged
  - Given: `ShadowRenderingModule` 初始化完毕，已注册 listener
  - When: `GameEvent.Send(EventId.Evt_QualityTierChanged, (int)ShadowQualityTier.Low)`
  - Then: `OnQualityTierChanged` 被调用；`GetCurrentQualityTier()` 返回 Low
  - Edge cases: listener 在 `Dispose()` 后不再响应事件（无孤立 listener）

- **AC-4**: Shadow-casting light 上限
  - Given: 场景中有 3 个 Additional Light 均有 Shadow enabled = true
  - When: `ShadowCasterPrioritySystem.Update()` 执行
  - Then: 优先级最高的 2 个保持 Shadow enabled；第 3 个光源 `enableShadows = false`
  - Edge cases: 所有 3 个光源优先级相同时，取前 2 个（顺序稳定，不每帧变化）

- **AC-3**: 帧边界切换不产生 artifact
  - Given: 运行中的场景，切换从 High → Low
  - When: `SetQualityTier(Low)` 在帧中途调用
  - Then: 当前帧正常渲染完成，新参数在下一帧生效；无全黑帧（通过录屏逐帧分析）
  - Edge cases: 连续快速切换（High→Low→Medium 在 3 帧内）不崩溃

- **AC-7**: 设备自动检测
  - Given: Mock `SystemInfo.graphicsDeviceName` 为已知低端 GPU 型号（Mali-G52）
  - When: `DetectDeviceQualityTier()` 调用
  - Then: 返回 `ShadowQualityTier.Low`
  - Edge cases: 未知 GPU 型号时默认返回 Medium

---

## Test Evidence

**Story Type**: Logic
**Required evidence**:
- `tests/unit/urp-shadow-rendering/quality_tier_system_test.cs` — 必须存在且通过

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 001（ShadowRT 已创建，档位切换时需重建）
- Unlocks: Story 006（自动降级 Integration story 需要质量档位切换接口就绪），Story 007（Mobile 优化在质量档位框架上叠加）
