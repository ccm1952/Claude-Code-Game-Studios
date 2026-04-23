// 该文件由Cursor 自动生成

# Story 002 — Atomic Effects (AudioDucking, ScreenFade, ObjectAnimate, ColorTemp)

> **Epic**: narrative-event
> **Type**: Logic
> **Status**: Ready
> **Priority**: Vertical Slice
> **Estimate**: 3d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/narrative-event-system.md` — 原子效果类型（Atomic Effect）|
| **TR-IDs** | TR-narr-001, TR-narr-002 |
| **ADR** | ADR-016 (10 atomic effect types, pluggable executors) |
| **Engine** | Unity 2022.3.62f2 LTS / DOTween / TEngine AudioModule / VideoPlayer |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-4.1**: 10 种原子效果作为可插拔 executor 实现（`IAtomicEffect` 接口）
- **CM-4.1**: 资源加载失败 → skip effect + `Log.Warning`（graceful degradation）
- **CM-4.3 (Audio)**: AudioDucking 通过 `IAudioService` 实现，不直接操作 AudioSource/Mixer
- **CM-4.3 (FORBIDDEN)**: 禁止使用 Unity Audio Mixer Groups
- **CM-1.4 (FORBIDDEN)**: 禁止使用 Coroutine；异步用 UniTask
- **CM-1.3**: 动画使用 DOTween

---

## Acceptance Criteria

1. **AC-001**: 实现以下 10 种 `IAtomicEffect`：
   - `AudioDuckingEffect`：通过 `IAudioService` 压低/恢复 ambient+music 层级
   - `ColorTemperatureEffect`：DOTween lerp 场景主光灯颜色（EaseInOutQuad）
   - `SFXOneShotEffect`：通过 `IAudioService` 播放单次音效
   - `CameraShakeEffect`：主摄像机轻微震动（intensity, duration, frequency）
   - `ScreenFadeEffect`：全屏 UI 遮罩淡入淡出（fadeColor, fadeIn, hold, fadeOut）
   - `TextureVideoEffect`：VideoPlayer 在指定 Renderer/UI 上播放（3阶段 alpha：淡入-保持-淡出）
   - `ObjectSnapEffect`：通过 `Evt_PuzzleSnapToTarget` 事件触发物件归位
   - `LightIntensityEffect`：DOTween lerp 指定灯光强度
   - `ShadowFadeEffect`：修改 WallReceiver shader 指定锚点影子 alpha
   - `ObjectFadeEffect`：DOTween lerp 指定物件 material alpha
2. **AC-002**: 每个 Effect 实现 `IAtomicEffect` 接口（`Start()`, `Update(float dt)`, `IsComplete`）
3. **AC-003**: `TextureVideoEffect` alpha 三阶段计算正确（phase1: fadeIn, phase2: hold, phase3: fadeOut）
4. **AC-004**: 资源加载失败（videoClipPath 不存在）→ `IsComplete=true`（立即结束），不阻塞序列
5. **AC-005**: `ColorTemperatureEffect` 使用 `EaseInOutQuad` 插值：`t < 0.5 ? 2t² : 1 - (-2t+2)²/2`
6. **AC-006**: `ObjectSnapEffect` 不直接控制物件，通过发送 `Evt_PuzzleSnapToTarget` 事件委托

---

## Implementation Notes

### IAtomicEffect 接口

```csharp
public interface IAtomicEffect
{
    bool IsComplete { get; }
    void Start();
    void Update(float deltaTime);
    void Dispose();  // 释放资源（如视频 handle）
}
```

### TextureVideoEffect alpha 公式

```csharp
float GetAlpha(float elapsed)
{
    if (elapsed < _fadeIn)
        return _targetAlpha * (elapsed / _fadeIn);
    if (elapsed < _fadeIn + _hold)
        return _targetAlpha;
    float fadeOutElapsed = elapsed - _fadeIn - _hold;
    return _targetAlpha * (1f - Mathf.Clamp01(fadeOutElapsed / _fadeOut));
}
```

### AudioDucking 通过 IAudioService

```csharp
// ADR-017: ducking 影响 Ambient + Music，不影响 SFX
GameEvent.Send(EventId.Evt_AudioDuckingRequest, new AudioDuckingPayload
{
    DuckRatio = _duckRatio,
    FadeDuration = _fadeDuration
});
// 恢复：DuckRatio = 1.0
```

### ObjectSnapEffect 委托

```csharp
// 不直接操作 Transform
GameEvent.Send(EventId.Evt_PuzzleSnapToTarget, new SnapPayload
{
    ObjectId = _objectId,
    TargetPosition = _targetPos,
    TargetRotation = _targetRot,
    Duration = _duration,
    Easing = _easing
});
// IsComplete = true（立即，实际动画由 Object Interaction 执行）
```

---

## Out of Scope

- VideoPlayer 资源加载（属于 YooAsset pipeline）
- TextureVideo 的 VideoPlayer 生命周期（在 story-001 序列引擎中管理 pre-warm/unload）
- 音频 ducking 的 IAudioService 实现（属于 audio-system epic）

---

## QA Test Cases

### TC-001: ColorTemperature 平滑插值

**Given**: originalColor=白色，targetColor=橘色，duration=1.0s  
**When**: 播放 1.0s  
**Then**: 颜色在 0.5s 时处于中间值（约 50% 混合），无突变；EaseInOutQuad 曲线验证

### TC-002: TextureVideo 三阶段 alpha

**Given**: fadeIn=0.5s, hold=3.0s, fadeOut=0.5s, targetAlpha=0.6  
**When**: 依次查询 elapsed=0.25s, 1.0s, 3.8s  
**Then**: alpha ≈ 0.3, 0.6, 0.3（各阶段正确）

### TC-003: AudioDucking 不影响 SFX

**Given**: `AudioDuckingEffect.Start()` 执行  
**When**: 在 Audio System 验证各层音量  
**Then**: Ambient 和 Music 层音量降低，SFX 层音量不变

### TC-004: 资源加载失败立即完成

**Given**: `TextureVideoEffect` 配置了不存在的 videoClipPath  
**When**: `Start()` 调用（资源加载失败）  
**Then**: `IsComplete = true`，`Log.Warning` 被记录，序列继续执行下一效果

### TC-005: ObjectSnapEffect 委托事件

**Given**: `ObjectSnapEffect.Start()` 调用  
**When**: 检查事件总线  
**Then**: `Evt_PuzzleSnapToTarget` 被发送，携带正确的 objectId 和 targetTransform；`IsComplete = true`

---

## Test Evidence

- **Unit Test**: `tests/unit/NarrativeEngine_AtomicEffects_Test.cs`（每种效果独立测试）

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-001 (Sequence Engine) | Base | `IAtomicEffect` 被引擎调用 |
| IAudioService (audio-system) | Integration | AudioDucking, SFXOneShot |
| DOTween | Library | ColorTemperature, LightIntensity, ObjectFade, ShadowFade |
| VideoPlayer | Engine API | TextureVideoEffect |
| WallReceiver Shader | Asset | ShadowFadeEffect shader param |
| EventId.cs (1500-1599) | Code | Evt_AudioDuckingRequest, Evt_PuzzleSnapToTarget |
