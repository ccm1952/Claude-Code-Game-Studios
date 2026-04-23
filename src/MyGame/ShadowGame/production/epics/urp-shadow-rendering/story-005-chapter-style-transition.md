// 该文件由Cursor 自动生成

# Story 005: 章节影子风格过渡（Chapter Shadow Style Transition）

> **Epic**: URP Shadow Rendering
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Visual/Feel
> **Manifest Version**: 2026-04-22

## Context

**GDD**: `design/gdd/urp-shadow-rendering.md`
**Requirements**: `TR-render-012`, `TR-render-011`, `TR-render-005`
*(Requirement text lives in `docs/architecture/tr-registry.yaml` — read fresh at review time)*

- TR-render-012: 5 个章节各自的影子风格预设（Penumbra、Shadow Color、Contrast、Edge Sharpness）
- TR-render-011: 影子特效接口（`SetShadowStyle`、`TransitionShadowStyle`、`SetShadowGlow`、`FreezeShadow`）
- TR-render-005: Shadow distance 8m 可配置（章节间可能调整）

**ADR Governing Implementation**: ADR-002: URP Rendering Pipeline
**ADR Decision Summary**: 章节风格参数由 Luban 配置表定义，运行时通过 DOTween 在 1.2s 内 lerp 所有参数（色温、阴影色、发光强度、边缘柔化），避免视觉跳变。使用 `Material Property Block` 或 `shader global` 切换，兼容 SRP Batcher。

**Engine**: Unity 2022.3.62f2 LTS — URP | **Risk**: LOW
**Engine Notes**: DOTween 在 Unity 2022.3 LTS 稳定支持。`MaterialPropertyBlock` 与 SRP Batcher 的兼容性需确认（URP 2022.3 中 MPB 可能破坏 SRP Batch 合并——如有影响，改用 `Shader.SetGlobalFloat` 全局参数方案）。

**Control Manifest Rules (this layer)**:
- Required: 5 章节风格预设由 Luban `TbChapterShadowStyle` 配置表定义，不硬编码（ADR-007）
- Required: 章节风格过渡通过 DOTween lerp 1.2s 实现，不允许瞬时跳变（ADR-002）
- Required: `TransitionShadowStyle` 返回 `UniTask` 以支持等待完成（tech-prefs UniTask 优先）
- Required: 所有 DOTween tween 在场景卸载（`Evt_SceneUnloadBegin`）时 Kill（ADR-009 场景生命周期）
- Required: 模块访问通过 `GameModule.XXX` 静态访问器（ADR-001）
- Forbidden: 不硬编码章节风格数值（颜色值、Penumbra 范围、Contrast ratio 等）（ADR-007）
- Forbidden: 不使用 Coroutine 实现过渡动画——使用 DOTween + UniTask（tech-prefs）
- Guardrail: 风格过渡时无单帧突变（GDD 验收标准）；过渡时间 0.5-1.2s（ADR-002）

---

## Acceptance Criteria

*From GDD `design/gdd/urp-shadow-rendering.md`，scoped to this story:*

- [ ] **AC-1**: `IShadowRendering.SetShadowStyle(ShadowStylePreset preset)` 接口立即（0 动画）切换到目标风格预设（用于初始化）
- [ ] **AC-2**: `IShadowRendering.TransitionShadowStyle(from, to, duration)` 在 `duration` 秒内线性 lerp 以下参数，无跳变：
  - `ColorTemperature`（-1 to +1）
  - `ShadowColor`（Color）
  - `GlowIntensity`（0 to 1）
  - `EdgeSoftness`（0 to 1）
- [ ] **AC-3**: 5 章节预设参数与 GDD §7 完全一致（从 Luban 配置读取，通过参数值对比验证）：

| 章节 | ColorTemp | ShadowColor | GlowIntensity | EdgeSoftness | ContrastRatio |
|:----:|:---------:|:-----------:|:-------------:|:------------:|:-------------:|
| Ch.1 | +0.3 | `#2C2C2E` | 0.4 | 1.0（最锐利） | 5:1 |
| Ch.2 | 0.0 | `#322C28` | 0.3 | 0.95 | 5:1 |
| Ch.3 | -0.2→实际值 | `#352A22` | 0.2 | 0.85 | 4:1 |
| Ch.4 | -0.1→实际值 | `#2C2E33` | 0.1 | 0.7 | 4:1 |
| Ch.5 | +0.1 | `#2E3035` | 0.5 | 0.5→0.8 | 3:1→4:1 |

- [ ] **AC-4**: 章节切换过渡期间若同时触发 PerfectMatch 定格（`FreezeShadow()`），以插值中间值定格，不重置到 from 或 to 端点
- [ ] **AC-5**: 过渡进行中若再次调用 `TransitionShadowStyle`（章节快速连跳），正确中断当前过渡从当前参数值开始新过渡
- [ ] **AC-6**: 场景卸载时（`Evt_SceneUnloadBegin`）所有进行中的 DOTween 动画被 Kill，不留后台 tween
- [ ] **AC-7**: 过渡完成后调用 `IShadowRendering.GetCurrentQualityTier()` 等接口不受风格过渡影响（风格和档位是独立状态）

---

## Implementation Notes

*Derived from ADR-002, control-manifest.md §5.1:*

**数据结构（对应 ADR-002 API Surface）：**
```csharp
public struct ShadowStylePreset
{
    public float ColorTemperature;   // -1 to +1
    public Color ShadowColor;
    public float GlowIntensity;     // 0 to 1
    public float EdgeSoftness;      // 0 to 1
    public float ContrastMultiplier; // 1.0-3.0
    public float PenumbraWidth;     // px
}
```

**风格加载（Luban）：**
```csharp
public ShadowStylePreset LoadPreset(int chapterId)
{
    var row = Tables.Instance.TbChapterShadowStyle.Get(chapterId);
    return new ShadowStylePreset
    {
        ColorTemperature = row.ColorTemperature,
        ShadowColor = row.ShadowColor,
        GlowIntensity = row.GlowIntensity,
        EdgeSoftness = row.EdgeSoftness,
        ContrastMultiplier = row.ContrastMultiplier,
        PenumbraWidth = row.PenumbraWidth
    };
}
```

**过渡实现（DOTween + UniTask）：**
```csharp
public async UniTask TransitionShadowStyle(ShadowStylePreset from, ShadowStylePreset to, float duration)
{
    _activeTween?.Kill();
    float progress = 0f;
    _activeTween = DOTween.To(
        () => progress,
        t => {
            progress = t;
            var current = LerpPreset(from, to, t);
            ApplyPresetImmediate(current);
        },
        1f, duration
    ).SetEase(Ease.Linear);
    await _activeTween.AsyncWaitForCompletion();
}
```

**场景卸载清理：**
```csharp
// 在 Init() 中注册
GameEvent.AddEventListener(EventId.Evt_SceneUnloadBegin, OnSceneUnloadBegin);

private void OnSceneUnloadBegin()
{
    _activeTween?.Kill();
    _activeTween = null;
}
```

**Ch.5 特殊处理（双段风格）：**
- Ch.5 的 EdgeSoftness 从 0.5 过渡到 0.8（AbsenceAccepted 演出触发二次过渡）
- 第一段（进入章节）：EdgeSoftness = 0.5，ContrastRatio = 3:1
- 第二段（AbsenceAccepted 后）：EdgeSoftness = 0.8，ContrastRatio = 4:1
- 两段均由 Narrative Event System 触发 `TransitionShadowStyle`，本系统不感知章节逻辑

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- **Story 002**: WallReceiver Shader 属性接口实现（本 story 调用已有接口，不修改 Shader）
- **Shadow Puzzle System**: NearMatch 发光触发（本 story 只实现 `SetShadowGlow` 接口，调用由 Puzzle System 决定）
- **Narrative Event System**: 触发章节切换的时机（本 story 只响应传入的 preset 参数）

---

## QA Test Cases

*Visual/Feel story — 人工验证步骤:*

- **AC-2**: 过渡无跳变
  - Setup: 在测试场景中调用 `TransitionShadowStyle(Ch1Preset, Ch2Preset, 1.2f)`，使用录屏软件以 60fps 录制墙面变化
  - Verify: 逐帧查看录屏，确认 `ShadowColor`、`EdgeSoftness`、`GlowIntensity` 在 1.2s 内平滑变化；无任何帧出现端点值跳变
  - Pass condition: 无可见跳变帧（主观评测 + 逐帧对比）；过渡时间误差 ≤ 100ms

- **AC-3**: 5 章节预设与 GDD 参数一致
  - Setup: 分别调用 `SetShadowStyle(ChN_Preset)` (N=1..5)，截取材质参数快照
  - Verify: 对比 Luban `TbChapterShadowStyle` 中各行的参数值与截图中 Material Inspector 显示的参数值
  - Pass condition: 所有章节的 ColorTemperature、ShadowColor RGB、EdgeSoftness 与 GDD §7 表格误差 ≤ 1/255（颜色）/ 0.01（float）

- **AC-4**: PerfectMatch 期间定格中间值
  - Setup: 触发 `TransitionShadowStyle(Ch1, Ch2, 5.0f)`（故意放慢），在 t=2.5s 时调用 `FreezeShadow(duration=10f)`
  - Verify: 墙面影子在 t=2.5s 的参数中间值上定格；即使过渡 DOTween 未完成，ShadowRT 不再更新
  - Pass condition: 定格后材质参数保持在 t=2.5s 时的插值状态；截图比较确认

- **AC-5**: 中断过渡并从中间值继续
  - Setup: 调用 `TransitionShadowStyle(Ch1, Ch3, 2.0f)`，在 t=1.0s 时再次调用 `TransitionShadowStyle(currentState, Ch2, 1.0f)`
  - Verify: 第二次过渡从 t=1.0s 时的中间参数值开始，不回到 Ch1 的起点
  - Pass condition: 无视觉"回弹"（参数不回退到 Ch1 的值）；过渡平滑

- **AC-6**: 场景卸载 Kill DOTween
  - Setup: 触发长时间过渡（30s），在过渡中途发送 `Evt_SceneUnloadBegin`
  - Verify: 过渡 DOTween 停止；下一帧材质参数不再变化；无 DOTween "completed on destroyed object" 警告
  - Pass condition: Unity Console 无 DOTween 或 NullReferenceException 警告

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- `production/qa/evidence/chapter-style-transition-evidence.md` — 包含以下内容：
  - 5 张截图（各章节风格在墙面上的效果）
  - 1 段过渡录屏分析（Ch1→Ch2，逐帧确认无跳变）
  - Luban 参数与 GDD §7 对照表
  - TA/美术 签字确认各章节视觉氛围符合 GDD §7 描述（暖色童年/自然校园/冷调城市/压抑沉寂/超现实空灵）

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 002（WallReceiver Shader 属性接口已实现，DOTween 才能控制参数）
- Unlocks: 无（本 story 为独立的 Visual/Feel story）；但 Narrative Event System 需要本 story 完成才能触发章节切换演出
