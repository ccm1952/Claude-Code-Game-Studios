// 该文件由Cursor 自动生成

# Story 007 — Match Feedback (NearMatch & PerfectMatch Visual/Audio)

> **Epic**: shadow-puzzle
> **Type**: Visual / Feel
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 2d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/shadow-puzzle-system.md` — Visual/Audio Requirements & Game Feel |
| **TR-IDs** | TR-puzzle-003, TR-puzzle-009 |
| **ADR** | ADR-014 (NearMatch glow effect, PerfectMatch snap animation), ADR-017 (Audio feedback) |
| **Engine** | Unity 2022.3.62f2 LTS / DOTween / TEngine AudioModule |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-4.1**: 所有 SFX 事件定义来自 Luban `TbAudioEvent`
- **CM-4.1**: 所有音频播放通过 `IAudioService`（TEngine AudioModule），不直接操作 AudioSource
- **CM-4.1 (FORBIDDEN)**: 禁止使用 Unity Audio Mixer Groups
- **CM-1.3**: 动画使用 DOTween（已列为 approved library）
- **CM-1.4 (FORBIDDEN)**: 禁止 `GameObject.Find` / `FindObjectOfType`，使用注入引用
- **CM-2.2**: NearMatch 发光监听 `Evt_NearMatchEnter`(1201) / `Evt_NearMatchExit`(1202)

---

## Acceptance Criteria

1. **AC-001**: NearMatch 进入时影子轮廓边缘出现柔和暖色光晕（LinearGradient，6帧渐入），退出时 12 帧渐隐
2. **AC-002**: NearMatch 光晕使用 material property（`_GlowIntensity`），不产生额外 draw call（TR-hint-011 对齐）
3. **AC-003**: PerfectMatch 进入时物件自动吸附到精确位置（EaseOutBack 曲线，18-30帧），有轻微回弹感
4. **AC-004**: PerfectMatch 吸附动画完成后调用 `IPuzzleStateMachine.OnSnapAnimationComplete()`（状态机推进到 Complete）
5. **AC-005**: NearMatch 进入时触发 50ms 轻微震动（仅移动端，受 `Settings.haptic_enabled` 控制）
6. **AC-006**: PerfectMatch 触发时播放满足感共鸣音 + 轻微风铃音效（SFX ID 来自 `TbAudioEvent`）
7. **AC-007**: NearMatch 进入时播放轻微音调升起效果（通过 `IAudioService` 触发）
8. **AC-008**: 所有动画参数（snapDuration, glowFadeIn/Out 帧数）可通过 Luban 配置表调节，无硬编码

---

## Implementation Notes

### NearMatch 发光效果

```csharp
// 响应 Evt_NearMatchEnter：
// 对谜题中所有 puzzle 物件的 shadow shader material 设置 glow 属性
DOTween.To(
    () => _wallReceiverMaterial.GetFloat("_GlowIntensity"),
    v => _wallReceiverMaterial.SetFloat("_GlowIntensity", v),
    targetGlow, glowFadeInDuration
).SetEase(Ease.Linear);

// 响应 Evt_NearMatchExit：
// 反向 tween 至 0
```

- 使用 material property 驱动（与 WallReceiver shader 的 `_GlowIntensity` 属性配合），不新建粒子系统

### PerfectMatch 吸附

```csharp
// 响应 Evt_PerfectMatch（携带 targetTransform 数据）：
DOTween.Sequence()
    .Append(transform.DOMove(targetPos, snapDuration).SetEase(Ease.OutBack))
    .Append(transform.DORotate(targetRot, snapDuration * 0.8f).SetEase(Ease.OutBack))
    .OnComplete(() => _stateMachine.OnSnapAnimationComplete());
```

- `snapDuration` 来自 Luban `TbPuzzle.snapDuration`（默认 0.5s，范围 0.3-0.8s）
- `Ease.OutBack` 提供轻微回弹感（GDD 要求）

### 触感震动

```csharp
// NearMatch 进入
if (Settings.hapticEnabled && Application.isMobilePlatform)
    Handheld.Vibrate(); // 或 TEngine 封装的 HapticFeedback.Light()

// PerfectMatch
if (Settings.hapticEnabled && Application.isMobilePlatform)
    HapticFeedback.Medium(); // 150ms 中等震动 + 渐弱
```

---

## Out of Scope

- Hint System 的 Layer 1 光晕（属于 hint-system epic）
- 记忆重现演出的色温变化（属于 narrative-event epic）
- Shadow RT 实时更新（属于 foundation layer）

---

## QA Test Cases

### TC-001: NearMatch 光晕渲染（Visual）

**Setup**: 在 Play Mode 中，将谜题 matchScore 设置到 0.42（触发 NearMatch）  
**Verify**: 影子轮廓边缘出现暖色光晕，alpha 从 0 渐变到目标值，过渡时间约 6帧（0.1s）；截图记录  
**Pass**: 光晕颜色为暖色（非白色/冷色），无闪烁，无额外 draw call 增加

### TC-002: NearMatch 退出光晕消退（Visual）

**Setup**: 谜题在 NearMatch 状态（光晕显示）  
**Verify**: 将 matchScore 降到 0.33（低于退出阈值），光晕在约 12帧（0.2s）内渐隐至 0  
**Pass**: 无突变，渐隐平滑；截图记录退出过程

### TC-003: PerfectMatch 吸附感（Feel）

**Setup**: 在 Play Mode 中触发 PerfectMatch  
**Verify**: 物件平滑滑入目标位置，有轻微回弹（EaseOutBack），不超出目标位置太多；使用录屏记录  
**Pass**: 吸附过程 ≥ 18帧（0.3s），回弹量视觉上"温柔"（GDD 要求无硬切）；3 名测试者评价"满足"或"自然"

### TC-004: OnSnapAnimationComplete 回调触发

**Setup**: 在 EditMode 测试中，模拟 PerfectMatch 触发  
**Verify**: DOTween 动画完成后，`IPuzzleStateMachine.OnSnapAnimationComplete()` 被调用  
**Pass**: 状态机推进到 Complete 状态，`Evt_PuzzleComplete` 广播

### TC-005: 无额外 Draw Call（Visual 性能）

**Setup**: Unity Frame Debugger，谜题在 NoMatch 状态记录 draw call 数量  
**Verify**: 切换到 NearMatch 状态，再次记录 draw call  
**Pass**: Draw call 数量无增加（material property 驱动，不新建对象）

---

## Test Evidence

- **Evidence Doc**: `production/qa/evidence/story-007-match-feedback.md`
  - NearMatch 光晕截图（进入 + 退出）
  - PerfectMatch 吸附录屏（≥ 3 帧关键帧截图）
  - Frame Debugger draw call 对比截图

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-001 (StateMachine) | Blocking | 监听状态变更事件 |
| story-005 (PerfectMatch) | Blocking | `Evt_PerfectMatch` 携带 targetTransform |
| ADR-017 (Audio) | Integration | SFX 播放路径 |
| WallReceiver Shader | Asset | 需要 `_GlowIntensity` shader property |
| TbPuzzle (snapDuration) | Config | 吸附动画时长 |
| TbAudioEvent | Config | NearMatch / PerfectMatch SFX ID |
