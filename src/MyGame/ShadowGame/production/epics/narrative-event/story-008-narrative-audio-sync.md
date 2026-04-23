// 该文件由Cursor 自动生成

# Story 008 — Narrative Audio Sync (Audio Ducking/Crossfade Synchronized with Narrative Effects)

> **Epic**: narrative-event
> **Type**: Visual / Feel
> **Status**: Ready
> **Priority**: Vertical Slice
> **Estimate**: 2d

---

## Context

| Field | Value |
|-------|-------|
| **GDD** | `design/gdd/narrative-event-system.md` — 原子效果: AudioDucking, AmbientAudioSwitch; 记忆重现演出典型流程 |
| **TR-IDs** | TR-narr-001 (AudioDucking effect type) |
| **ADR** | ADR-016 (AudioDucking + AmbientAudioSwitch atomic effects), ADR-017 (Audio Mix — ducking rules) |
| **Engine** | Unity 2022.3.62f2 LTS / TEngine AudioModule |
| **Assembly** | `GameLogic` |

### Control Manifest Rules

- **CM-4.3 (ADR-017)**: ducking 影响 Ambient + Music 层；SFX 层**不受 ducking 影响**
- **CM-4.3**: `finalVolume = clipBaseVolume × layerVolume × masterVolume × duckingMultiplier`
- **CM-4.3**: Music crossfade：双 AudioSource 策略，默认 3.0s 渐变
- **CM-4.3 (FORBIDDEN)**: 禁止使用 Unity Audio Mixer Groups
- **CM-4.3 (FORBIDDEN)**: 禁止直接创建 AudioSource，通过 TEngine AudioModule
- **CM-4.1**: 所有音频参数（duckRatio, fadeDuration, crossfadeDuration）来自 Luban `TbAudioEvent` / 序列配置

---

## Acceptance Criteria

1. **AC-001**: 记忆重现序列开始时（t=0），`AudioDuckingEffect` 在 0.5s 内将 Ambient + Music 压低到 duckRatio=0.3（30% 音量）
2. **AC-002**: 序列结束时（t=5.5s），`AudioDuckingEffect` 在 0.5s 内将 Ambient + Music 恢复到 duckRatio=1.0
3. **AC-003**: ducking 期间 SFX 层音量不变（演出 SFX 正常播放）
4. **AC-004**: `AmbientAudioSwitch` 效果：淡出当前环境音（fadeOutDuration），淡入新音轨（fadeInDuration），支持 holdDuration 后淡出
5. **AC-005**: Music crossfade：章节过渡时新音乐在 3.0s 内与旧音乐交叉淡化（无静音空白）
6. **AC-006**: ducking 开始 → 记忆音效（SFX）播放 → 纹理视频呈现：时间轴严格对齐（± 50ms 精度）
7. **AC-007**: 演出结束后 Ambient + Music 音量完全恢复（不因序列中断等情况留下半压状态）
8. **AC-008**: 所有 ducking 参数（duckRatio=0.3, fadeDuration=0.5s 等）来自 Luban 配置，不硬编码

---

## Implementation Notes

### AudioDucking 委托给 IAudioService

```csharp
// AudioDuckingEffect.Start()
GameEvent.Send(EventId.Evt_AudioDuckingRequest, new AudioDuckingPayload
{
    DuckRatio = _config.DuckRatio,      // 0.3 from Luban
    FadeDuration = _config.FadeDuration // 0.5s from Luban
});
// IsComplete = true（立即，实际渐变由 Audio System 执行）

// 序列结束时（duck restore）
GameEvent.Send(EventId.Evt_AudioDuckingRequest, new AudioDuckingPayload
{
    DuckRatio = 1.0f,
    FadeDuration = _config.FadeDuration
});
```

### AmbientAudioSwitch 实现

```csharp
// AmbientAudioSwitchEffect — 三阶段：fadeOut + hold + fadeIn(optional)
// 通过 GameEvent.Send(Evt_AmbientLayerChange, ...) 委托给 AudioManager
// IsComplete = true (fadeIn 开始后即可，hold 和 fadeOut 异步执行)
```

### 时间轴对齐验证

典型演出时间轴（来自 GDD）：
```
t=0.0s: AudioDucking(0.3, 0.5s)    ← 开始压低
t=0.0s: ObjectSnapToTarget          ← 吸附同步
t=0.5s: ColorTemperature(warm, 1s)  ← 色温变暖
t=0.8s: SFXOneShot(memory_sfx_01)   ← 记忆音效（压低后的音效，更清晰）
t=1.0s: TextureVideo(...)            ← 视频呈现
t=5.0s: ColorTemperature(neutral, 1s) ← 恢复
t=5.5s: AudioDucking(1.0, 0.5s)     ← 恢复音量
```

---

## Out of Scope

- IAudioService 的底层实现（属于 audio-system epic）
- 音频资产制作（SFX 内容）
- Timeline 内含音频（story-007 中说明 Timeline 音频自包含）

---

## QA Test Cases

### TC-001: Ducking 效果验证（Feel）

**Setup**: 在 PlayMode 中触发记忆重现序列，用 Unity Audio Mixer Inspector 观察（或程序读取音量值）  
**Verify**: t=0 时开始压低，t=0.5s Ambient 音量达到 duckRatio × originalVolume  
**Pass**: 压低曲线平滑，无突变；视觉上截图音量数值变化

### TC-002: SFX 不受 Ducking 影响

**Setup**: Ducking 激活期间，播放 SFX  
**Verify**: 比较 Ducking 前后 SFX 层音量值  
**Pass**: SFX 音量差值 < 2%（不受影响）

### TC-003: Ducking 恢复完整性

**Setup**: 序列正常完成  
**Verify**: t=5.5s + 0.5s（恢复完成）后读取 Ambient/Music 层音量  
**Pass**: 音量 = originalVolume（duckRatio=1.0，无残留压低）

### TC-004: 时间轴对齐（± 50ms）

**Setup**: 在序列 t=0.0s 和 t=0.8s 分别 Debug.Log 实际执行时间戳  
**Verify**: SFX 播放时间戳 vs 预期 0.8s  
**Pass**: 误差 ≤ 50ms（约 3 帧）

### TC-005: 序列中断后音量恢复（异常处理）

**Setup**: 在 ducking 激活期间（t=0.3s）强制取消序列（CancellationToken）  
**Verify**: 取消后等待 1s，检查音量  
**Pass**: Ambient/Music 音量完全恢复到正常值（try/finally 中执行 restore duck）

### TC-006: 章节 Music Crossfade（Feel）

**Setup**: 章节过渡序列开始，观察 Music 层音量变化  
**Verify**: 旧音乐 3s 内淡出，新音乐 3s 内淡入，两者有重叠期（无静音间隙）  
**Pass**: 录屏记录无静音；测试者评价"无缝过渡"

---

## Test Evidence

- **Evidence Doc**: `production/qa/evidence/story-008-narrative-audio-sync.md`
  - Ducking 音量曲线截图（Audio Mixer inspector 或代码读取值）
  - SFX 时间轴对齐日志（误差记录）
  - Music crossfade 录屏片段

---

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| story-001 (Sequence Engine) | Base | `AudioDuckingEffect` 是原子效果之一 |
| story-002 (Atomic Effects) | Base | `AudioDuckingEffect` 实现在此 story 验证 feel |
| audio-system epic | Integration | `IAudioService` ducking/crossfade API |
| ADR-017 (Audio Mix) | Architecture | ducking 规则（Ambient+Music 受影响，SFX 不受） |
| Luban TbAudioEvent | Config | duckRatio, fadeDuration 参数 |
