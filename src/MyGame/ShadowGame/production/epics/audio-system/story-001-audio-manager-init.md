// 该文件由Cursor 自动生成

# Story 001: Audio Manager Initialization — 3-Layer Mix + Ducking + Crossfade + ADR-028 Gate Unlock

> **Epic**: Audio System
> **Status**: Ready (framework created 2026-05-06 by Sprint 4 S4-03；待 future dev-story 实施)
> **Layer**: Core (architecture.md §3.3 reclassification)
> **Type**: Integration (PlayMode-only — D1=[b])
> **Manifest Version**: 2026-05-06 v1 (S4-03 framework — Track A 收官)
>
> **Created note (2026-05-06)**: 本 story framework 由 Sprint 4 S4-03 (ADR-017 implementation expand) 创建。Sprint 4 Track A 收官 story。S4-03 deliverable 是 ADR-017 expand + 本 story framework + ADR-028 §1 AudioModule activation gate 解锁；actual production code 实施 + EditMode/PlayMode tests 留 future Sprint 4-5 dev-story。

---

## Context

**GDD**: `design/gdd/audio-system.md`
**Requirement**: TR-audio-002/003/004/005/008..011/013/014 + TR-settings-008（详 §TR Coverage）

**ADR Governing Implementation**:
- **Primary**: `docs/architecture/adr-017-audio-mix.md` (Accepted 2026-05-06；Implementation Expand 2026-05-06)
- **Dependencies**: ADR-001 (TEngine framework — `GameModule.Audio` accessor), ADR-007 (Luban TbAudio), ADR-027 (Event Protocol + §5), ADR-028 (§1 AudioModule activation gate — 本 story 解锁)
- **Governance**: ADR-029 V2.0 §V2-3 R3 mandatory + §V2-5 framework boundary behavior probe

**ADR Decision Summary**: 3-layer audio mix (Ambient/SFX/Music) on TEngine AudioModule + per-layer volume + Ducking system + Music crossfade + sfx_enabled SFX-layer-only isolation + ambientVolume internal baseline (0.6) + Settings change cascade → IAudioEvent.OnSetLayerVolume；ADR-028 §1 AudioModule activation gate 解锁。

**Engine**: Unity 2022.3.62f2 LTS + TEngine 6.0.0 + UniTask 2.5.10 + AudioModule | **Risk**: MEDIUM-HIGH (framework drift detected — 见 ⚠️ §Required Framework Extension)
**Engine Notes**:
- TEngine `GameModule.Audio` 是唯一 audio 入口（per ADR-001 forbidden patterns — 不允许直接 `new AudioSource()`）
- ADR-028 §1：AudioModule 接入路径见下方 ⚠️ Required Framework API Verification（**ADR-028 §1 描述的 `Activate()` API 在 IAudioModule 接口实测不存在**；真接入应为 `Initialize(AudioGroupConfig[] audioGroupConfigs, Transform instanceRoot, AudioMixer audioMixer)`）
- 不用 Unity Audio Mixer（per ADR-017 §Constraints）；ducking + 音量控制在 AudioSource level
- ADR-027 §5 ⚠️ Framework knowledge fact：8 IAudioEvent listeners 必须 handler null-out + Cleanup null-check guard
- ADR-017 §C Production code paths：新建 `Assets/GameScripts/HotFix/GameLogic/Audio/` 目录 + `IEvent/IAudioEvent.cs`
- PlayMode-only test: 沿 S3-01..03 spike pattern；`Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-06_AudioMixArchitecture.cs` (Sprint 5 dev-story)

### ⚠️ Required Framework Extension (Session 23 preventive readiness check #2, 2026-05-06 night)

**Sprint 5 V3 candidate #8 expanded learning** — preventive R2 codebase grep 实测暴露 4 项 deficiency + 1 项 Type-2(c) framework behavior drift（与 S5-03 readiness check #1 / S5-05 readiness check #1 同模式 plus 1 新类型）。Apply DEFICIENCY-FLAGGED + Framework Drift Fix path：

#### 4 项 Deficiency (与 S5-03/S5-05 同模式)

| Deficiency | Codebase 实测 | Step 1 处理 |
|-----------|:-----------:|------------|
| **`IAudioEvent` contract** (本 story 自身) | 0 hit | 创建 `Assets/GameScripts/HotFix/GameLogic/IEvent/IAudioEvent.cs` (8 method per ADR-017 §A — OnPlaySFX + OnPlayMusic + OnStopMusic + OnDuckingRequest + OnReleaseDucking + OnSetLayerVolume + OnAudioPauseRequest + OnAudioResumeRequest); AudioLayer enum 同文件。**与 S5-05 协调**：S5-05 stub 仅 1 method (OnDuckingRequest), S5-06 production 扩 8 method full — backward-compatible contract surface 增长 |
| **`ConfigSystem.Tables.TbAudio`** (Luban 真表未生成) | 0 hit | 沿 S5-03 PuzzleStateConfigFromLuban + S5-05 NarrativeSequenceConfigFromLuban 模式：`AudioConfigFromLuban.GetSfxConfig(int sfxId)` provider hardcoded MVP defaults (chapter 1 必需 SFX：≥3 sfx — UI click / object pickup / object snap, ≥1 music — chapter 1 ambient)；future S5-XX (Luban TbAudio schema gen) 替换 |
| **`IAudioService`** (architecture.md §6.8 提及但未创建) | 0 hit | `AudioManager : IAudioService` 实施时 **同步创建 `IAudioService.cs`** in `Audio/` 目录（接口签名按 architecture.md §6.8 定义）|
| **`ISettingsEvent.OnSettingChanged` cascade** (Sprint 2 ✅ 存在但 Settings 真持久化逻辑 in S3-09 未实施) | ✅ event 存在 / ⚠️ Settings persistence 未做 | AudioManager.HandleSettingChanged Listener 实施 OK；但需注意 S3-09 Settings Manager 真持久化 **carryover 至 Sprint 5+ 仍 backlog** — Sprint 5 chapter 1 MVP 内不需 SaveManager 真集成，Sprint 6 起再 cascade 完整 |

#### 🚨 1 项 Type-2(c) Framework Behavior Drift (新发现)

**Issue**: Story §3 example code "ADR-028 §1 AudioModule Activation Gate" 调用 **`GameModule.Audio.Activate()`** — 该 method **在 IAudioModule 接口实测不存在**（已 verify `Assets/TEngine/Runtime/Module/AudioModule/IAudioModule.cs:8`，IAudioModule 仅有 `Initialize / Restart / Play / Stop / StopAll / PutInAudioPool / RemoveClipFromPool / CleanSoundPool` + 多个 volume/enable property，无 `Activate()`）。

**与 S3-03 P5 暴露的 TEngine.RemoveEventListener 非 idempotent 同类 Type-2(c)** — framework behavior assumption drift（ADR-028 §1 + Story §3 描述的 API 与 TEngine baseline 实际 API 不符）。

**Step 1 fix (dev-story 阶段)**:
1. **Verify TEngine `IAudioModule.Initialize(AudioGroupConfig[], Transform, AudioMixer)` 是 ADR-028 §1 真"AudioModule activation gate"接入路径**（已 grep verified — IAudioModule.cs:79 + AudioModule.cs:341；现有 GameApp.Entrance 当前并未调 Audio Initialize — 即 AudioModule 实际未 activated, 之前 codebase 无 audio 用例符合 ADR-028 §1 "启用条件" gate 状态）
2. **设计 `AudioGroupConfig[]` 数据**（per TEngine `AudioModule.cs:392` + AudioGroupConfig.cs — 用 4 个 entries 对应 TEngine `AudioType` enum: Music/Sound/UISound/Voice）
3. **决策 AudioManager facade layer 与 TEngine 4-channel 映射**:
   - Story 设计 3-layer (Ambient/SFX/Music) — 项目层抽象
   - TEngine 4-channel (Music/Sound/UISound/Voice) — framework
   - 推荐映射: Ambient → TEngine SoundLoop in Sound channel (looping AudioAgent); SFX → Sound channel; Music → Music channel; UISound + Voice 留空 (chapter 1 MVP 不用)
4. **GameApp.Entrance 真接入** (在 ConfigSystem.Instance.Load() 后 + StartGameLogic() 前):
   ```csharp
   var audioGroupConfigs = AudioConfigFromLuban.BuildDefaultGroupConfigs();  // hardcoded MVP defaults
   GameModule.Audio.Initialize(audioGroupConfigs);  // ADR-028 §1 activation gate (corrected from Activate())
   AudioManager.Instance.Initialize();  // project facade
   ```

**ADR-028 §1 + ADR-017 §B history note** (added inline in Story; ADR docs 未 modify in this readiness step — 留 dev-story 后或 Sprint 5 retro 一并修订):
> 2026-05-06 dusk - Sprint 5 S5-06 preventive readiness check #2 暴露 framework behavior drift: `Activate()` 在 IAudioModule 接口不存在；真接入路径 `Initialize(AudioGroupConfig[], Transform, AudioMixer)`。S5-06 dev-story 后该 ADR-028 §1 + ADR-017 §B 文字应修订对齐 codebase。

**Recommendation**: dev-story Step 1 (~15-20 min) stub 4 deficiencies + verify framework drift fix；Step 2+ AudioManager + AudioMixLayer + DuckingController 主 impl + EditMode tests + PlayMode spike。

**Cross-Sprint coordination**:
- S5-06 production code 创建 IAudioEvent.cs **8 method full** (per ADR-017 §A); S5-05 dev-story 已 stub 1 method 版本
- S5-05 dev-story 完成后 S5-06 dev-story 起手时检查 IAudioEvent.cs — 如 1-method stub 已存在则扩展（backward compatible）；如未存在则直接创建 8-method full

**Sprint 5 V3 candidate #8 data point #4** (Session 23 preventive readiness check #2 实测): readiness check ~28-32 min × ~30-50 min potential dev-story revision time saved + prevent Type-2(c) framework drift reactive hit ~15-20 min (1st dev-story step would compile-fail otherwise)。**比 S5-05 ROI 更高**因为含 framework drift early detection。

**Performance**:
- Audio system update ≤ 1ms with 10 sources（per ADR-003）
- Total audio memory ≤ 30MB（all loaded clips）
- Music crossfade momentary double-load ≤ 5MB peak

**Control Manifest Rules (this layer)**:
- Required: `IAudioEvent 接口协议 (8 method per ADR-027)；handler null-out + Cleanup null-check guard pattern (ADR-027 §5)`
- Required: `所有 audio access via GameModule.Audio (per ADR-001 forbidden direct AudioSource)`
- Required: `3 mix layers 完全 isolation：sfx_enabled 仅控制 SFX；ambientVolume 内部 baseline 不暴露 player`
- Required: `Master volume 与 layer volume multiplicative：actual = master × layer × ducking × per-source`
- Required: `Ducking 仅影响 Ambient + Music；不影响 SFX`
- Required: `Music crossfade 双 source 管理 + memory release on swap complete`
- Required: `SFX concurrency cap 4 + oldest-cull policy`
- Required: `App pause/resume — all AudioSources pause + resume from same point`
- Required: `Settings change cascade — ISettingsEvent.OnSettingChanged → IAudioEvent.OnSetLayerVolume`
- Required: `ADR-028 §1 AudioModule activation gate — GameApp.Entrance 中调 GameModule.Audio.Activate()`
- Required: `Initialize/Shutdown 显式 lifecycle (沿 ADR-013 v3 教训)`
- Forbidden: `Evt_PlaySFXRequest / Evt_AudioDuckingRequest 等 ADR-006 const-int 协议 (已 superseded by ADR-027)`
- Forbidden: `直接 new AudioSource() / AudioSource.Play() (per ADR-001)`
- Forbidden: `Unity Audio Mixer dependency (per ADR-017 §Constraints — keep TEngine AudioModule self-contained)`
- Forbidden: `Raw double-remove RemoveEventListener (TEngine 抛 "Delete handle failed")`
- Guardrail: `IAudioEvent dispatch ≤ 0.01ms (p99)` (per ADR-027 §2)

---

## Acceptance Criteria

### A. IAudioEvent + AudioManager Initialization

- [ ] **AC-1** `IAudioEvent` 接口实现含 8 method (OnPlaySFX / OnPlayMusic / OnStopMusic / OnDuckingRequest / OnReleaseDucking / OnSetLayerVolume / OnAudioPauseRequest / OnAudioResumeRequest)
- [ ] **AC-2** AudioManager Initialize 后 8 listeners 全部 subscribe；Shutdown 后全部 unsubscribe（with null-check guard）
- [ ] **AC-3** ADR-028 §1 AudioModule activation gate 解锁：`GameModule.Audio.Activate()` 调用前 PlaySFX 失败优雅 (Log.Warning + no-op)；activation 后正常播放
- [ ] **AC-4** AudioManager 实现 IAudioService 接口（per architecture.md §6.8）

### B. 3-Layer Volume Isolation

- [ ] **AC-5** 3 mix layers 完全独立：SetLayerVolume(SFX, 0) 静音 SFX；Ambient/Music 仍正常
- [ ] **AC-6** sfx_enabled = false 仅静 SFX layer；Ambient layer (silence 自身) 不受影响（GDD spec：ambient is silence itself）
- [ ] **AC-7** ambientVolume = 0.6 内部 baseline；不出现在 PlayerPrefs；不通过 Settings UI 调整
- [ ] **AC-8** Master + per-layer volume multiplicative：master 0.5 + SFX 0.8 → 实际 SFX 播放 0.4

### C. Ducking + Crossfade

- [ ] **AC-9** SetDucking(0.3, 0.5s) → 0.5s 内 Ambient/Music 平滑降至 30%；SFX 完全不受影响
- [ ] **AC-10** ReleaseDucking(0.5s) → 0.5s 内 Ambient/Music 平滑恢复
- [ ] **AC-11** Music crossfade：PlayMusic(track2, 1.0s) 时 track1 fade out + track2 fade in 同步进行；无 audible gap / overlap artifacts；crossfade 完成后 track1 source 释放

### D. SFX 行为 + 配置

- [ ] **AC-12** SFX concurrency cap 4：5th 同 sfxId 调 PlaySFX → kills oldest instance 替换
- [ ] **AC-13** SFX variant + pitch randomization：5 consecutive PlaySFX 同 sfxId → ≥2 不同 variant；pitch 范围 [0.95, 1.05] random
- [ ] **AC-14** All SFX 配置来自 Luban TbAudio (id / clipPath / volume / 3d / pitch / variants)；no hardcoded sfx 引用

### E. Lifecycle + Settings Cascade

- [ ] **AC-15** App pause/resume：OnApplicationPause → all AudioSources pause；OnApplicationFocus → resume from same position；no pop/click artifacts
- [ ] **AC-16** Music continues during PauseMenu：TimeScale = 0 时 Music 仍 playing（unscaled time mode）
- [ ] **AC-17** Settings change cascade：ISettingsEvent.OnSettingChanged("master_volume", 0.7f) → AudioManager.SetMasterVolume(0.7) within 1 frame
- [ ] **AC-18** (V2.0 §V2-5 idempotency) `TestAudioScopedFixture` documented null-out + null-check guard 全程无 TEngine "Delete handle failed" exception (8 listeners stress test)

---

## Implementation Notes

*Derived from ADR-017 §A-§G Implementation Expand (2026-05-06):*

### 1. IAudioEvent 接口 (ADR-017 §A 设计 — 8 method)

```csharp
// Assets/GameScripts/HotFix/GameLogic/IEvent/IAudioEvent.cs
namespace GameLogic
{
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IAudioEvent
    {
        void OnPlaySFX(int sfxId, float delay, float volume);
        void OnPlayMusic(int musicClipId, float crossfadeDuration);
        void OnStopMusic(float fadeDuration);
        void OnDuckingRequest(float duckRatio, float fadeDuration);
        void OnReleaseDucking(float fadeDuration);
        void OnSetLayerVolume(AudioLayer layer, float volume);
        void OnAudioPauseRequest();
        void OnAudioResumeRequest();
    }

    public enum AudioLayer { Ambient = 0, SFX = 1, Music = 2 }
}
```

### 2. AudioManager 主体（IAudioService impl）

```csharp
public sealed class AudioManager : IAudioService
{
    // 8 IAudioEvent listener fields (ADR-027 §5 null-out)
    private Action<int, float, float> _onPlaySFX;
    private Action<int, float> _onPlayMusic;
    private Action<float> _onStopMusic;
    private Action<float, float> _onDuckingRequest;
    private Action<float> _onReleaseDucking;
    private Action<AudioLayer, float> _onSetLayerVolume;
    private Action _onAudioPause;
    private Action _onAudioResume;

    // 3-layer state
    private float _masterVolume = 1.0f;
    private float[] _layerVolumes = new float[3] { 0.6f, 1.0f, 1.0f };  // [Ambient, SFX, Music]
    private float _duckingMultiplier = 1.0f;  // ducking interpolation state

    public void Initialize()
    {
        // 8 listeners + 1 cross-system (ISettingsEvent) cascade
        _onPlaySFX = HandlePlaySFX;
        GameEvent.AddEventListener<int, float, float>(IAudioEvent_Event.OnPlaySFX, _onPlaySFX);
        // ... 其余 7 同模式 ...
    }

    public void Shutdown()
    {
        // 8 null-check guards
        if (_onPlaySFX != null) { GameEvent.RemoveEventListener<...>(..., _onPlaySFX); _onPlaySFX = null; }
        // ... 其余 7 同模式 ...
        // Stop all AudioSources via GameModule.Audio.StopAll()
    }

    private void HandlePlaySFX(int sfxId, float delay, float volume)
    {
        // ADR-001 forbidden direct AudioSource — 通过 GameModule.Audio
        var config = ConfigSystem.Tables.TbAudio.Get(sfxId);
        if (config == null) { Log.Warning($"SFX {sfxId} not found"); return; }
        // ... concurrency check + variant pick + pitch random + GameModule.Audio.PlayOneShot ...
    }
}
```

### 3. ADR-028 §1 AudioModule Activation Gate 接入（GameApp.Entrance）

> **⚠️ Framework Drift Fix (Session 23 preventive readiness check #2, 2026-05-06 night)**:
> 原 v1 example code `GameModule.Audio.Activate()` 在 TEngine `IAudioModule` 接口实测不存在 — Type-2(c) framework behavior assumption drift。Verified IAudioModule API: `Initialize(AudioGroupConfig[], Transform, AudioMixer) / Restart / Play / Stop / StopAll / PutInAudioPool` + volume/enable properties。本节已修订对齐 codebase。

```csharp
public static void Entrance(object[] objects)
{
    GameEventHelper.Init();
    // ... existing init steps (ConfigSystem.Instance.Load(), AddDestroyListener) ...

    // ADR-028 §1 AudioModule activation gate — 真接入路径 (per IAudioModule.cs:79 verified)
    var audioGroupConfigs = AudioConfigFromLuban.BuildDefaultGroupConfigs();  // hardcoded MVP defaults; future TbAudio 真表 substitution
    GameModule.Audio.Initialize(audioGroupConfigs);  // 4 channel: Music / Sound / UISound / Voice (per TEngine AudioType enum)
    AudioManager.Instance.Initialize();              // project facade — 8 listeners + ISettingsEvent cascade

    // ... continue boot (StartGameLogic) ...
}
```

**Step 1 dev-story 工作 — IAudioModule API verification + AudioGroupConfig[] 设计**:
1. Verify `IAudioModule.Initialize(...)` 实际是 ADR-028 §1 "AudioModule activation gate" 真接入路径 (已 grep ✅)
2. 设计 `AudioConfigFromLuban.BuildDefaultGroupConfigs()` 返 4 entries (chapter 1 MVP only):
   - `AudioGroupConfig{ AudioType = AudioType.Music, MaxChannel = 2, ... }`  (chapter 1 ambient + 1 reserved for crossfade)
   - `AudioGroupConfig{ AudioType = AudioType.Sound, MaxChannel = 4, ... }`  (SFX concurrency cap 4 per ADR-017)
   - `AudioGroupConfig{ AudioType = AudioType.UISound, MaxChannel = 2, ... }` (chapter 1 MVP — UI click)
   - `AudioGroupConfig{ AudioType = AudioType.Voice, MaxChannel = 0, ... }` (chapter 1 不用 voice)
3. AudioManager facade 3-layer (Ambient/SFX/Music) → TEngine 4-channel mapping:
   - Ambient layer → TEngine SoundLoop track in Sound channel (looping AudioAgent;1 channel reserved)
   - SFX layer → Sound channel (3 channels for concurrency)
   - Music layer → Music channel

### 4. Settings Cross-System Cascade

```csharp
// AudioManager.Initialize() — 同时订阅 ISettingsEvent.OnSettingChanged
private Action<string, object> _onSettingChanged;

public void Initialize()
{
    // ... 8 IAudioEvent listeners ...
    _onSettingChanged = HandleSettingChanged;
    GameEvent.AddEventListener<string, object>(ISettingsEvent_Event.OnSettingChanged, _onSettingChanged);
}

private void HandleSettingChanged(string key, object value)
{
    switch (key)
    {
        case "master_volume": SetMasterVolume((float)value); break;
        case "music_volume": SetLayerVolume(AudioLayer.Music, (float)value); break;
        case "sfx_volume": SetLayerVolume(AudioLayer.SFX, (float)value); break;
        case "sfx_enabled": SetLayerVolume(AudioLayer.SFX, (bool)value ? 1.0f : 0.0f); break;
        // 注意：ambient_volume 不在此 case — 内部 baseline 不暴露 player (per AC-7)
    }
}
```

### 5. Sprint 4 S4-03 deliverable scope clarification

**S4-03 范围（Track A 收官）**：
1. ✅ ADR-017 Implementation Expand 节添加（已完成 2026-05-06；§A-§G）
2. ✅ Story-001 framework 创建（本文件）
3. ✅ ADR-028 §1 AudioModule activation gate 解锁 reference（在 ADR-017 §B + 本 story §3）

**留 future Sprint 4-5 dev-story**：
- Production code 实施 (AudioManager + AudioMixLayer + DuckingController + IAudioEvent.cs)
- EditMode unit tests (volume formula multiplicative / ducking interpolation / SFX concurrency cap logic)
- PlayMode S403_AudioMixArchitecture.cs spike (8 CORE + 2 advisory)
- Luban TbAudio 真表加载 + AudioConfigFromLuban impl
- ADR-028 §1 AudioModule.Activate() 真接入 GameApp.Entrance

---

## QA Test Cases (映射到 future PlayMode S403 spike)

| AC | Spike P# | 验证手段 |
|------|------|---------|
| AC-1/-2 (IAudioEvent + 8 listeners init) | P7 | listener counts verify after Init/Shutdown cycle |
| AC-3 (AudioModule activation gate) | P8 | call PlaySFX before Activate → fail-loud or no-op；activation 后正常 |
| AC-5/-6/-7 (3-layer isolation + sfx_enabled scope) | P1 | SetLayerVolume per-layer + 验 isolation |
| AC-8 (master + layer multiplicative) | P2 | mathematical verification of volume formula |
| AC-9/-10 (ducking + release smooth interp) | P3 | Stopwatch + AudioSource.volume sample per frame |
| AC-11 (music crossfade no artifacts) | P4 (ADV) | audible verify + Unity Profiler memory sample |
| AC-12 (SFX concurrency cap + cull) | P5 | 5th call 替换 oldest instance verify |
| AC-13 (SFX variant + pitch random) | P5 prep | 5 consecutive 同 sfxId 调用，verify ≥2 variants |
| AC-14 (Luban TbAudio config) | P1 prep | AudioConfigFromLuban 读真表 |
| AC-15/-16 (app pause/resume + Music during PauseMenu) | P9 | OnApplicationPause/Focus simulate + Music TimeScale 0 verify |
| AC-17 (Settings cascade) | P6 | dispatch ISettingsEvent.OnSettingChanged → AudioManager.SetLayerVolume verify within 1 frame |
| AC-18 (listener self-removal §V2-5) | P7 | TestAudioScopedFixture documented null-out + null-check guard 全程无 exception |

---

## Test Evidence

**Required evidence (future dev-story)**:
- `Assets/Tests/EditMode/Audio/AudioVolumeFormulaTests.cs` — pure logic (multiplicative formula / ducking interp / concurrency cap logic)
- `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S403_AudioMixArchitecture.cs` — PlayMode spike (8 CORE + 2 ADV)
- `production/qa/grep-no-fantasy-api-audio-manager.md` — grep evidence (R1 listener pattern + R2 cross-component API + R3 stub data type)

**Status**: [x] Framework Created 2026-05-06 (S4-03 deliverable) — Production impl + tests 留 future Sprint 4-5 dev-story.

**Status (Sprint 5 Session 24 #3, 2026-05-08)**: ✅ **COMPLETE — DONE 2026-05-08**

- EditMode tests: `Assets/Tests/EditMode/Audio/AudioVolumeFormulaTests.cs` — 14/14 PASSED (multiplicative formula × 5 + ducking lerp × 5 + provider lookup × 4)
- PlayMode spike: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-06_AudioMixArchitecture.cs` — 10/10 PASSED first-run (vs S5-05 v1→v2 fix cycle)
- Evidence: [`production/qa/playmode-audio-mix-architecture-2026-05-08.md`](../../qa/playmode-audio-mix-architecture-2026-05-08.md)
- AC coverage: 13/18 COVERED + 4/18 PARTIAL (AC-11 audible / AC-12 framework cull / AC-13 真表 variants / AC-15 D3 简化 pause) + 1/18 DEFERRED (AC-16 PauseMenu Sprint 6+)
- Production code: 7 files (IAudioEvent.cs 1→8 method + IAudioService.cs + AudioConfigFromLuban.cs + AudioMixLayer.cs + DuckingController.cs + AudioManager.cs + GameApp.cs hook)
- AudioSetting.asset modification (D1): Music agentHelperCount 1 → 2 (real crossfade capacity)
- Framework drift v2 surfaced + 留 retro 修订 (per D2)：drift-v2-(a) AudioModule.OnInit 已自动 Initialize；drift-v2-(b) ISettingsEvent.OnSettingChanged 真签名 (string, string) 而非 (string, object)

---

## ADR-029 Phase 1.5 Readiness Verdict

### v1 (2026-05-06 by Sprint 4 S4-03 framework creation; superseded)

| Rule | Verdict | 数据 |
|------|---------|------|
| R1 | ✅ PASS | IAudioEvent 8 method 设计完整 |
| R2 | ✅ PASS (~~stale; codebase grep 未实跑~~) | 仅 Engine Notes 列举 — 标 "全部 framework 实测存在 ✅" 但实际未 grep |
| R3 | ✅ PASS | AudioConfig POCO readonly + 5+ field ctor |
| Type-2/3 风险 | LOW (~~stale; Type-2(c) framework behavior 未实测~~) | "Sprint 0 spike SP-XXX 验证后 known" — 实测 Sprint 0 未验证 IAudioModule.Activate() existence |

### v2 (2026-05-06 night — Session 23 preventive readiness check #2 — DEFICIENCY-FLAGGED + Framework Drift Fix PASS)

| Rule | Verdict | 数据 |
|------|---------|------|
| **R1** Per-event listener / sender pairing (ADR-027) | ✅ PASS | IAudioEvent 8 method 设计完整；listener 注册 ADR-027 §5 null-out + null-check guard pattern (8 listeners + 1 ISettingsEvent cascade) |
| **R2** Cross-component API existence grep | ✅ **DEFICIENCY-FLAGGED PASS** (codebase 实测 grep 2026-05-06 night) | ✅ 存在 (4/8): GameModule.Audio (GameModule.cs:56 ✅) / IAudioModule (TEngine baseline ✅) / ISettingsEvent (Sprint 2 ✅) / ISettingsEvent_Event 2 hits. ❌ 0-hit (4/8) → §Engine Notes 已加 Required Framework Extension flag (上方): IAudioEvent (Step 1 self-create) / ConfigSystem.Tables.TbAudio (AudioConfigFromLuban hardcoded MVP defaults provider) / IAudioService (Step 1 同步创建) / S3-09 Settings persistence (cross-Sprint dependency note only) |
| **R3** Stub data type constructor | ✅ PASS | AudioConfig POCO readonly + 5+ field ctor；AudioLayer enum；AudioCommand POCO (如 dev-story 引入)；测试中可直接 `new AudioConfig(...)` 构造 fixture |
| **(V3 candidate #8 expanded — Sprint 5 V3 lesson b)** Namespace uniqueness check | ✅ PASS (codebase 实测 grep 2026-05-06 night) | AudioManager / AudioMixLayer / DuckingController / AudioConfig / AudioLayer / AudioConfigFromLuban / AudioCommand / IAudioService — 全部 codebase 0 hit / 0 collision ✅ |
| **🚨 (V3 candidate #8 expanded — Sprint 5 V3 lesson c, NEW)** Framework method existence verification | ✅ **FIXED (was: ❌ Type-2(c) drift)** | v1 假设 `GameModule.Audio.Activate()` 存在 — 实测 IAudioModule 接口仅有 `Initialize / Restart / Play / Stop / StopAll / PutInAudioPool / RemoveClipFromPool / CleanSoundPool` + volume/enable properties (无 Activate)；§Engine Notes + §3 example code 已修订对齐 codebase — 真接入 = `GameModule.Audio.Initialize(audioGroupConfigs[, instanceRoot, audioMixer])` |
| Type-2 / Type-3 风险 | LOW (R3 mandatory cover; Type-2(c) drift fix 完成) | Type-2(a) framework facade behavior: AudioModule init via Initialize(AudioGroupConfig[]) (P8 cover)；Type-2(b) cross-method state: 3-layer volumes + ducking + crossfade source swap state (P1/P3/P11 cover)；**Type-2(c) framework method behavior: framework drift fix 完成 (此次 readiness check 实测捕获)；P8 PlayMode spike Initialize() 真路径 verify**；listener self-removal pattern (P7 cover) |

### R2 Revision History

- **v1 (Sprint 4 S4-03 framework creation, 2026-05-06)**: R2 仅 Engine Notes 列举依赖 + "全部 framework 实测存在 ✅" 标记，未跑 codebase grep。Story §3 example code 直接信 ADR-028 §1 `Activate()` 描述未 verify TEngine 实际 API。该模式被 Sprint 5 S5-03 readiness check #1 + S5-05 readiness check #1 暴露为 framework-creation 阶段普遍 gap。
- **v2 (Sprint 5 Session 23 preventive readiness check #2, 2026-05-06 night)**: 取 S5-03/S5-05 lesson 主动 codebase 实测 R2 双重 grep + framework method existence verification — 暴露 4 项 deficiency (IAudioEvent + TbAudio + IAudioService + S3-09 Settings persistence note) + 1 项 Type-2(c) framework behavior drift (`Activate()` API 不存在 — Story §3 example code 会编译失败)；apply DEFICIENCY-FLAGGED + Framework Drift Fix PASS path + Required Framework Extension flag in §Engine Notes + §3 example code 修订对齐 IAudioModule 真 API。Sprint 5 V3 candidate #8 data point #4 (preventive ROI + framework drift early detection)。
- **v3 (Sprint 5 Session 24 #3 dev-story 实施期间, 2026-05-08)** — **2 项 framework drift v2 + 1 项 design decision surface**:
  - **Drift v2-(a) [Type-2(c) framework lifecycle]**: TEngine `AudioModule.OnInit()` (AudioModule.cs:322-326) 已**自动**调 `Initialize(Settings.AudioSetting.audioGroupConfigs)` —— 即 ModuleSystem.Init lifecycle 内自动以 `AudioSetting.asset` 配置初始化。**§3 example code line 227-228 中 `var audioGroupConfigs = AudioConfigFromLuban.BuildDefaultGroupConfigs(); GameModule.Audio.Initialize(audioGroupConfigs);` 是 obsolete + 双重 init 风险**。GameApp.Entrance 仅需调 `AudioManager.Instance.Initialize()` (项目 facade)，framework Initialize 由 OnInit 自动 cover。`AudioConfigFromLuban` 实际角色是 SFX/Music 名称→config lookup（不再是 BuildDefaultGroupConfigs）。
  - **Drift v2-(b) [Type-2(c) framework method signature]**: TEngine `ISettingsEvent.OnSettingChanged` 实际签名是 `(string key, string value)` 而非 §4 example code line 251/257/260-270 假设的 `(string key, object value)`。AudioManager.HandleSettingChanged 已用 `TryParseFloat(value, ...)` / `TryParseBool(value, ...)` 适配 string→float/bool 转换。
  - **Design decision [D1, D3 from S5-06 dev-story planning]**:
    - **D1**: `AudioSetting.asset` Music agentHelperCount 1 → 2 (per AC-11 真 crossfade requirement; 已 modify asset)
    - **D3**: Pause/Resume MVP 简化版 — 调 `GameModule.Audio.Enable=false/true` (AudioListener.volume mute)；**真严格 pause-from-position** (active AudioAgents pause + position freeze + AudioListener volume preserve) 留 Sprint 6+ backlog。
  - **D2 deferral**: ADR-028 §1 + ADR-017 §B 文字 + 本 story §3 §4 example code 修订留 **Sprint 5 retro 一并修订** (per S5-06 D2 决策; ADR-029 V3 candidate #8 data point #4 已记录 v1, v3 drift 一起 retro 处理)。
  - **AudioSetting.asset modification (D1 outcome)**: `Assets/TEngine/Settings/AudioSetting.asset` Music group `agentHelperCount: 1` → `2` (本 dev-story 唯一 ScriptableObject 资产改动)。

---

## Dependencies

- **Depends On**:
  - ADR-001 ✅ TEngine Framework
  - ADR-007 ✅ Luban Config Access (TbAudio)
  - ADR-017 ✅ Audio Mix Architecture (本 story 的 governing ADR；Implementation Expand 2026-05-06)
  - ADR-027 ✅ GameEvent Interface Protocol
  - ADR-028 ✅ TEngine Module Usage Policy (§1 AudioModule activation gate — 本 story 解锁)
  - ADR-029 V2.0 ✅ Story §Implementation Notes Verification

- **Unlocks**:
  - ADR-016 (Narrative Sequence Engine) S4-02 ✅ — IAudioEvent.OnDuckingRequest + OnPlaySFX listeners 由 NarrativeSequencePlayer dispatch；Sprint 5+ dev-story 起来 cross-system event flow 全 ready
  - Sprint 5 VS slice (chapter 1) — Audio 是 VS 核心循环之一（ambient atmosphere + SFX feedback + music chapter ambient）

---

## TR Coverage (closes 10 ⚠️ TRs from architecture-traceability v1.1)

| TR-ID | Status pre-S4-03 | Status post-Story-001 完整 dev-story 闭环后 |
|-------|:----------------:|:-------------------------------------------:|
| TR-audio-002 Volume formula (4 multipliers) | ⚠️ | ✅ |
| TR-audio-003 SFX variant + pitch randomization | ⚠️ | ✅ |
| TR-audio-004 3D spatial audio | ⚠️ | (依赖 AudioSource positioning impl) — partial |
| TR-audio-005 maxConcurrent + oldest cull | ⚠️ | ✅ |
| TR-audio-008 SFX latency ≤ 1 frame | ⚠️ | ✅ |
| TR-audio-009 Ambient starts within 2s | ⚠️ | (依赖 scene load 时 trigger) — partial |
| TR-audio-010 Ambient occasional sounds | ⚠️ | (依赖 timer-based trigger impl) — partial |
| TR-audio-011 Audio CPU < 1ms with 10 sources | ⚠️ | ✅ |
| TR-audio-013 App pause/resume | ⚠️ | ✅ |
| TR-audio-014 Music continues during PauseMenu | ⚠️ | ✅ |
| TR-settings-008 Ambient volume independent of sfx_enabled | ⚠️ | ✅ |

**完整闭环后预期关 7 ⚠️ TRs full + 3 partial (4/9/10 依赖 future stories — 3D positioning / scene-driven trigger / timer-based occasional)**。

---

## Sprint 4 S4-03 Deliverable Checklist

- [x] ADR-017 Implementation Expand 节添加（§A-§G 完整 — 2026-05-06）
- [x] IAudioEvent 接口契约设计（8 method per ADR-027）
- [x] AudioLayer enum + AudioConfig POCO 设计
- [x] Story-001 framework 创建（本文件）
- [x] 18 AC 定义（A-E 五类）
- [x] PlayMode S403 spike 设计（8 CORE + 2 ADV cases）
- [x] ADR-029 V2.0 R3 mandatory + V2-5 boundary probe coverage 显式
- [x] 10 ⚠️ TRs mapping
- [x] ADR-028 §1 AudioModule activation gate 解锁 reference
- [x] Settings cross-system cascade design (ISettingsEvent → IAudioEvent)
- [x] sprint-status.yaml S4-03 status: done

**S4-03 Deliverable status (2026-05-06)**: ✅ **DONE — Track A (Sprint 4 P1 ADR impl expand) 收官**.

---

## Sprint 4 Track A Completion Summary (Sprint 5 VS 准备就绪)

S4-03 是 Track A 三个 P1 ADR impl expand stories 的最后一个。Track A 完整产出：

| Story | ADR | 主要 contribution | Status |
|-------|-----|------------------|:------:|
| S4-01 | ADR-014 Puzzle State Machine | 7-state FSM + IShadowPuzzleEvent 5 method + hysteresis + grace + absence timer | ✅ DONE 2026-05-06 |
| S4-02 | ADR-016 Narrative Sequence Engine | Sequence player + 12 atomic effects + INarrativeEvent 4 method + token locking + queue | ✅ DONE 2026-05-06 |
| S4-03 | ADR-017 Audio Mix Architecture | AudioManager + 3-layer + IAudioEvent 8 method + ducking + crossfade + ADR-028 §1 解锁 | ✅ DONE 2026-05-06 |

**Sprint 5 VS (chapter 1 端到端) P1 ADR 依赖全部 framework ready**：

```
Sprint 5 VS Build chapter 1:
  scene-management (S3-01..03 ✅) — chapter scene lifecycle ✅
  object-interaction (S2-08..13 ✅) — drag/rotate/snap ✅
  shadow-puzzle (S4-01 framework ✅) — puzzle state machine ⏳ impl
  narrative-event (S4-02 framework ✅) — narrative beat ⏳ impl
  audio-system (S4-03 framework ✅) — audio infrastructure ⏳ impl
  ui-system (S4-09 carryover) — UI Module init ⏳ impl
```

Sprint 4 Track A pattern velocity validated：3 stories × ~25 min = ~75 min 实际 Track A 投入。
