// 该文件由Cursor 自动生成

# Story 006: 性能自动降级集成（Performance Auto-Degradation Integration）

> **Epic**: URP Shadow Rendering
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Integration
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/urp-shadow-rendering.md`
**Requirements**: `TR-render-016`, `TR-render-017`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

- TR-render-016: 自动质量降级——连续 5 帧 > 20ms 触发降一档
- TR-render-017: Draw call 预算 ≤ 150（移动端）/ 额外 Shadow pass draw calls < 20

**ADR Governing Implementation**: ADR-018: Performance Monitoring；ADR-002: URP Rendering Pipeline；SP-010 Findings
**ADR Decision Summary**: 全局 `PerformanceMonitor`（Core Layer）拥有质量状态机，5 帧 > 20ms 触发 Level 2 降级（drop shadow quality tier）；Shadow Rendering 模块是 `Evt_QualityTierChanged` 的被动监听者，响应降级指令。两系统通过 GameEvent 解耦，Shadow Rendering 不直接访问 PerformanceMonitor。

**Engine**: Unity 2022.3.62f2 LTS — URP | **Risk**: MEDIUM
**Engine Notes**: `FrameTimingManager` 在部分低端 Android 设备上可能不可用，需 fallback 到 `Time.unscaledDeltaTime`。URP 质量档位运行时切换需在帧边界应用，避免渲染 artifact。

**Control Manifest Rules (this layer)**:
- Required: 全局 PerformanceMonitor 是质量状态机的单一 owner（ADR-018, SP-010）
- Required: 降级触发：5 consecutive frames > 20ms → drop 一档；恢复：60 consecutive frames < 12ms → 升一档（ADR-018）
- Required: 恢复有 30 帧验证窗口（14ms 阈值），验证失败则回退并翻倍等待（ADR-018）
- Required: Shadow Rendering 通过 `Evt_QualityTierChanged` 接收降级通知，不自行管理帧时间（SP-010）
- Required: Level 4（Critical）降级时通过 UI System 显示 toast（ADR-018）
- Required: 手动质量选择（Settings UI 调用 `PerformanceMonitor.SetUserPreference()`）时关闭自动降级（SP-010）
- Forbidden: Shadow Rendering 模块不调用 `PerformanceMonitor.RequestDegradation()`（SP-010）
- Forbidden: 不在 Shadow Rendering 内维护独立帧时间统计（SP-010）
- Guardrail: 降级响应时间 ≤ 5 帧（~83ms）；PerformanceMonitor 自身 CPU 开销 < 0.1ms/frame（ADR-018）

---

## Acceptance Criteria

*From GDD `design/gdd/urp-shadow-rendering.md` + ADR-018，scoped to this story:*

- [ ] **AC-1**: `PerformanceMonitor` 检测到连续 5 帧 > 20ms 时，通过 `Evt_QualityTierChanged` 广播降级指令；`ShadowRenderingModule` 在同帧或下一帧内完成质量档位切换
- [ ] **AC-2**: 降级 5 级联：Level 1（Mild）→ 禁用 `ShadowCasterPriority.Ambient` 光源的阴影；Level 2（Moderate）→ drop shadow quality tier；Level 3（Severe）→ ShadowRT readback 降为隔帧；Level 4（Critical）→ 强制 Low tier + 显示 toast
- [ ] **AC-3**: 连续 60 帧 < 12ms 触发恢复尝试，30 帧验证窗口（14ms 阈值）；验证失败则回退并将 recoveryFrameCount 翻倍（120 帧）防止振荡
- [ ] **AC-4**: 手动质量选择（Settings 调用 `SetUserPreference(tier)`）时，`_autoDegrade = false`，`PerformanceMonitor` 停止帧时间统计并不自动变更档位
- [ ] **AC-5**: 降级切换无可见跳变——玩家看不到质量突变（"玩家不可感知的品质降低"）；Level 1-3 切换无任何 UI 提示
- [ ] **AC-6**: Level 4（Critical）触发时，通过 `GameEvent.Send(EventId.Evt_ShowToast, "已优化画质以保证流畅")` 显示 3 秒 toast
- [ ] **AC-7**: 10 分钟持续负载压力测试中，降级档位稳定（不在同一档位之间振荡超过 3 次/分钟）
- [ ] **AC-8**: `PerformanceMonitor` 调用自身 CPU 消耗 < 0.1ms/frame（Profiler marker 验证）

---

## Implementation Notes

*Derived from ADR-018, SP-010, control-manifest.md §2.7:*

**系统边界说明（Integration story 的核心）：**

```
PerformanceMonitor (Foundation Layer — ADR-018)
  └─ 帧时间检测（LateUpdate）
  └─ 触发降级 → ApplyTier() → GameEvent.Send(Evt_QualityTierChanged, newTier)

ShadowRenderingModule (Foundation Layer — ADR-002)
  └─ GameEvent.AddEventListener(Evt_QualityTierChanged, OnQualityTierChanged)
  └─ OnQualityTierChanged() → ApplyQualityTier()
```

这两个模块之间**只通过 GameEvent 通信**，不持有互相的引用。

**PerformanceMonitor 帧时间检测（来自 SP-010 实现设计）：**
```csharp
// LateUpdate 中
_frameTimes[_frameIndex % 5] = Time.unscaledDeltaTime * 1000f;
_frameIndex++;

if (_frameIndex >= 5 && AllFramesAbove(20f) && _currentTier > ShadowQualityTier.Low)
{
    _currentTier = (ShadowQualityTier)((int)_currentTier - 1);
    GameEvent.Send<int>(EventId.Evt_QualityTierChanged, (int)_currentTier);
}
```

**降级 4 级 Shadow 专属处理（来自 ADR-018 Degradation Cascade）：**

| Level | Shadow 处理 | 实现位置 |
|-------|------------|---------|
| Level 1 (Mild) | 禁用 `ShadowCasterPriority.Ambient` 光源 | `ShadowRenderingModule.DisableAmbientShadows()` |
| Level 2 (Moderate) | Drop shadow quality tier | 监听 `Evt_QualityTierChanged`，Story 004 的 `ApplyQualityTier()` |
| Level 3 (Severe) | ShadowRT readback 隔帧 | Story 003 的 `_readbackEveryOtherFrame = true` |
| Level 4 (Critical) | 强制 Low + 禁用额外光阴影 | `Evt_QualityTierChanged(Low)` + `Evt_ShowToast` |

**恢复振荡防止（来自 ADR-018）：**
```csharp
if (verificationFailed)
{
    currentLevel = previousLevel; // 回退
    recoveryFrameCount *= 2;      // 下次需要更长时间
    consecutiveUnderTarget = 0;
}
```

**EventId 分配**（需在 `EventId.cs` 中注册）：
- `Evt_QualityTierChanged`：归属 Foundation Layer 区间（需 ADR-006 分配一个 100-ID 范围外的值，或在现有范围内找一个合适位置）

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 004**: `SetQualityTier()` 和 `ApplyQualityTier()` 的实现（本 story 验证两系统的集成行为）
- **Story 003**: readback 隔帧逻辑（本 story 触发 Level 3 降级，隔帧切换由 Story 003 负责）
- **PerformanceMonitor epic**: PerformanceMonitor 的完整实现属于独立模块（本 story 假设 PerformanceMonitor 已实现并发送 `Evt_QualityTierChanged`）

---

## QA Test Cases

*Integration story — 自动化 + 压力测试规格:*

- **AC-1**: 5 帧超限触发降级
  - Given: `PerformanceMonitor` 已初始化，当前档位为 Medium，`_autoDegrade = true`
  - When: Mock `Time.unscaledDeltaTime` 连续 5 帧返回 `21ms`（> 20ms）
  - Then: `Evt_QualityTierChanged` 被发送一次，参数为 `(int)ShadowQualityTier.Low`；`ShadowRenderingModule._currentTier == Low`
  - Edge cases: 第 4 帧 Mock 为 `18ms`，计数重置，下一次需要再连续 5 帧才触发

- **AC-2**: 降级 4 级联顺序
  - Given: 从 Normal 级别开始，`_autoDegrade = true`
  - When: 连续触发 4 次 5 帧超限降级
  - Then: 降级顺序为 Normal→Level1→Level2→Level3→Level4；每次降级只降一档
  - Edge cases: 已在 Level4 时继续触发，不再降级，不发送事件

- **AC-3**: 恢复验证窗口
  - Given: 当前档位为 Low（被降级）
  - When: Mock 连续 60 帧 `deltaTime < 12ms`，触发恢复；恢复期间第 15 帧 Mock 返回 `15ms`（> 14ms 阈值）
  - Then: 30 帧验证失败，档位回退到 Low；`recoveryFrameCount` 变为 120
  - Edge cases: 30 帧全部 < 14ms 时恢复成功，档位升到 Medium，`recoveryFrameCount` 不变

- **AC-4**: 手动选择禁用自动降级
  - Given: `PerformanceMonitor` 处于 Normal 状态
  - When: `SetUserPreference(ShadowQualityTier.High)` 被调用，之后 Mock 5 帧 > 20ms
  - Then: 不触发 `Evt_QualityTierChanged`；`_autoDegrade == false`；档位保持 High
  - Edge cases: 调用 `EnableAutoDegrade()` 后，5 帧 > 20ms 重新触发降级

- **AC-7**: 10 分钟稳定性
  - Given: 压力测试场景（大量 shadow caster，帧时间约 25ms）
  - When: 运行 10 分钟
  - Then: 降级档位收敛到 Low/Medium，稳定期（后 5 分钟）内档位变化 ≤ 3 次；无内存泄漏
  - Edge cases: 热切换场景后（卸载 + 加载）性能监控状态正确重置

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `tests/integration/urp-shadow-rendering/performance_auto_degrade_test.cs` — 包含 AC-1 到 AC-4 的自动化测试
- 压力测试报告（手动）：`production/qa/evidence/perf-auto-degrade-stress-test.md`（10 分钟测试结果，AC-7）

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 004（质量档位切换接口就绪），Story 003（readback 隔帧接口就绪），`PerformanceMonitor` 模块完成实现（独立 epic）
- Unlocks: 整个 URP Shadow Rendering epic 的 Definition of Done 完成条件——本 story 是最后一个 Integration 验证项
