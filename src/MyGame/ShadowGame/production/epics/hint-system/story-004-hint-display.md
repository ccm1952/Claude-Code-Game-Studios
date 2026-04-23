// 该文件由Cursor 自动生成

# Story 004 — Hint Display (HintButton UI + Hint Overlay Presentation)

> **Epic**: hint-system
> **Type**: UI
> **Status**: Ready
> **Priority**: MVP
> **Estimate**: 2d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/hint-system.md` — Visual/Audio Requirements; UI Requirements; Game Feel |
| **TR-IDs** | TR-hint-001, TR-hint-011, TR-hint-012 |
| **ADR** | ADR-015 (L1 zero draw calls, L2 < 0.5ms), ADR-011 (UIWindow management) |
| **Engine** | Unity 2022.3.62f2 LTS / TEngine UIModule / UGUI / DOTween |
| **Assembly** | `GameLogic` — UIModule |

### Control Manifest Rules

- **CM-5.2**: 所有 UI 面板通过 `GameModule.UI.ShowWindow<T>()` 管理
- **CM-5.2**: HUD 层（layer=100）面板预加载，不频繁 Show/Hide（而是改 visibility）
- **CM-5.2**: UI 触摸目标 ≥ 44×44dp（Apple HIG）
- **CM-5.2**: `SetUISafeFitHelper` 在根 Canvas 上设置
- **CM-4.2**: L1 光晕不增加额外 draw call（material property 驱动，TR-hint-011）
- **CM-4.2**: L2 虚影渲染开销 < 0.5ms（TR-hint-012）
- **CM-5.2 (FORBIDDEN)**: 禁止使用 `Resources.Load` 加载 UI prefab；使用 `GameModule.Resource.LoadAssetAsync`
- **CM-1.4 (FORBIDDEN)**: 禁止使用 UI Toolkit

---

## Acceptance Criteria

1. **AC-001**: 提示按钮（HintButton）在谜题 Active 状态后 30s 开始从 alpha=0.3 渐变到 alpha=1.0（rampDuration=10s）
2. **AC-002**: L3 剩余次数显示在按钮角标（小圆点 + 数字），次数为 0 时按钮变灰并显示 tooltip "已无更多提示"
3. **AC-003**: 点击 HintButton 触发 `Evt_RequestExplicitHint`，响应延迟 ≤ 100ms
4. **AC-004**: L1 光晕：目标物件边缘 `_GlowIntensity` property 渐变（alpha 0→0.15→0，周期 3s），不新增 draw call
5. **AC-005**: L2 虚影：投影接收面（墙面）上显示 alpha=0.08-0.12 的局部影子轮廓，持续 2s 后 1s 内消散，渲染耗时 ≤ 0.5ms
6. **AC-006**: L3 提示：屏幕下方显示文字提示（内心独白风格），目标物件上方显示方向箭头；持续 5s 后 1s 渐隐
7. **AC-007**: 按钮在 iPhone 13 Mini（375pt 宽）上可触达，不遮挡主要操作区域（右下角，与谜题交互区域无重叠）
8. **AC-008**: Layer 1 在设置中关闭后（`Settings.hints_passive_enabled=false`）不播放光晕；Layer 3 按钮始终可用

---

## Implementation Notes

### HintButton 渐显逻辑

```csharp
// HUDPanel.HintButton — Active 后 30s 触发渐变
// 监听 Evt_PuzzleStateChanged(Active) 后启动计时
private void StartHintButtonRamp()
{
    // rampStart=30s, rampDuration=10s — 来自 Luban TbHintConfig
    GameModule.Timer.AddTimer(
        _config.HintButtonRampStart,
        () => _hintButton.DOFade(1.0f, _config.HintButtonRampDuration)
    );
}
```

### L1 光晕（无额外 DrawCall）

```csharp
// 响应 Evt_HintAvailable{layer=1, targetObjectId}
// 找到目标物件的 Renderer，设置 material property
_targetRenderer.SetPropertyBlock(/* _GlowIntensity: 0 → 0.15 → 0 */);
// 使用 DOTween + MaterialPropertyBlock 驱动，不修改 Material 本身
```

### L2 虚影（投影接收面）

```csharp
// 在 WallReceiver 的 shader 中启用 ghost outline pass
// targetAlpha 0.08-0.12, 持续 2s，边缘模糊
// 渲染预算：仅影响 WallReceiver 材质，< 0.5ms
```

### L3 文字 + 箭头

```csharp
// 文字：HintTextPanel（HUD Layer），TextMeshPro，内心独白语气
// 箭头：在目标物件 World Space 上方的 RectTransform，带呼吸缩放动画（DOTween）
// 持续 5s → DOFade(0, 1s) 渐隐
```

---

## Out of Scope

- HintSystem 触发逻辑（story-001, 002, 003）
- L3 文字内容（来自 Luban TbHintConfig）
- 缺席谜题特殊文案（story-006）

---

## QA Test Cases

### TC-001: 提示按钮渐显（UI）

**Setup**: 谜题进入 Active 状态，观察 HintButton 初始 alpha=0.3  
**Verify**: 30 秒后按钮开始 10s 渐变至 alpha=1.0；截图记录关键帧  
**Pass**: 渐变平滑（SmoothStep），无跳变；最终 alpha ≥ 0.95

### TC-002: L3 次数角标

**Setup**: l3RemainingCount=3，触发 2 次 L3  
**Verify**: 角标数字从 3 → 2 → 1；最后一次触发后按钮变灰（alpha 降低），tooltip 显示  
**Pass**: 角标数字正确更新；按钮灰化后点击无响应（不发送 Evt_RequestExplicitHint）

### TC-003: L1 无额外 DrawCall（Visual 性能）

**Setup**: Frame Debugger，记录 NearMatch 之前的 draw call 数量  
**Verify**: Hint L1 触发（光晕激活），再次统计 draw call  
**Pass**: Draw call 数量无增加（0 额外 draw call）

### TC-004: L2 虚影渲染预算

**Setup**: Unity Profiler，PlayMode，谜题进入 L2Active 状态  
**Verify**: GPU timeline 中 WallReceiver 相关渲染时间  
**Pass**: 增量渲染时间 ≤ 0.5ms（基准对比）

### TC-005: L3 文字响应延迟

**Setup**: 测量从按钮点击到文字开始显示的延迟  
**Verify**: 点击时间戳 → L3 文字 alpha 开始增加的时间戳  
**Pass**: 延迟 ≤ 100ms（6 帧，60fps）

---

## Test Evidence

- **Evidence Doc**: `production/qa/evidence/story-004-hint-display.md`
  - L1 光晕截图（DrawCall 对比）
  - L2 虚影截图（渲染预算 Profiler）
  - L3 文字 + 箭头截图
  - HintButton 渐显截图（30s 前、渐变中、渐变后）

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-002 (Three-Tier) | Blocking | `Evt_HintAvailable{layer, targetObjectId}` 触发展示 |
| WallReceiver Shader | Asset | L2 虚影 pass |
| Object Renderer (material property block) | Asset | L1 光晕 |
| Luban TbHintConfig | Config | rampStart, rampDuration, L3 文字内容 |
| ADR-011 (UIWindow) | Architecture | HUD Panel 生命周期 |
