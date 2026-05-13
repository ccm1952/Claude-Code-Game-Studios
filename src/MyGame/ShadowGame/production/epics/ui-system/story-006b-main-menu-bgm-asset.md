// 该文件由Cursor 自动生成

# Story: MainMenu BGM Asset + Luban Entry — Sprint 7+ Production Polish Phase Backlog

> **Epic**: ui-system (Audio epic 边界 — 添加 AudioConfig entry 涉及 Audio epic 修改；本 story 是 ui-system epic 衍生的 follow-on 但实施时主要触 AudioConfigFromLuban / Luban TbAudio 资源)
> **Story ID**: ui-system-006b (Sprint 7+ follow-on placeholder 衍生自 S6-07 Phase 2.0 R2.8 [D] mixed strategy closure 2026-05-14 morning)
> **Sprint**: TBD (Sprint 7+ Production stage polish phase 起步时实施)
> **Story Type**: Audio Asset / Integration
> **Complexity Points**: 1
> **GDD Requirement**: TR-ui-005 (9 UIWindows) — main menu polish 完整 audio 体验补齐
> **ADR References**: ADR-017 (Audio Mix) + ADR-028 (TEngine Module Usage Policy) + ADR-027 §4 (GameEvent Interface Protocol — IAudioEvent.OnPlayMusic/OnStopMusic) + ADR-029 V3.0.1 §V2-1 R2 DEFICIENCY-FLAGGED PASS path closure governance precedent
> **Status**: Backlog (Sprint 7+ Production polish phase)
> **Created**: 2026-05-14 morning (Session 30 — Phase 2.0 R2.8 [D] mixed strategy closure 衍生)
> **Depends on**: S6-07 ✅ ui-system-006 main menu UIWindow polish (production code OnRefresh BGM hook 已 wire `AudioManager.Instance.PlayMusic("main_menu_bgm", 1.0f)`，仅需 asset + config entry 添加 BGM 即响)；Sprint 7+ Production stage advance (production/stage.txt: Pre-Production → Production)

---

## Context

S6-07 ui-system-006 main menu UIWindow polish 实施时 (Sprint 6 Session 30 Phase 2.0 R2.8 verify) 发现：

- `main_menu_bgm` clipId grep 0-hit in production code
- `AudioConfigFromLuban.InitWithDefaults()` (line 154-158) 仅 1 Music entry `chapter1_ambient` (id=100)；`InitFromLuban` 是 stub TODO[S5-XX+] fallback 走 InitWithDefaults
- `AudioManager.PlayMusic(string clipId, ...)` 内置 fail-safe (clipId 不存在 → Log.Warning + return 不抛 exception)

**[D] mixed strategy 决策 (Session 30 Phase 2.0 user)**: 遵 epic 边界 — Phase 2 production code 不改 AudioConfigFromLuban (跨 Audio epic 边界 ui-system epic 不 polluted)；BGM hook 保留 production code OnRefresh `AudioManager.Instance.PlayMusic("main_menu_bgm", 1.0f)` 调用 (走 PlayMusic fail-safe Log.Warning+no-op)；本 follow-on backlog story 处理真 BGM asset + AudioConfig entry add (Sprint 7+ Production polish phase 起步时实施)。

**governance impact**: epic 边界 cleanup ✅；UI epic 仅触 Audio facade `AudioManager.Instance.PlayMusic` API 不触 Audio config table；spec 不退 (BGM hook 完整保留 in MainMenuPanel.cs OnRefresh)；Sprint 7+ 真 asset 添加后 BGM 自动响 0 code change in MainMenuPanel.cs。

---

## Acceptance Criteria

- [ ] **AC-1** `main_menu_bgm` AudioClip asset 准入 — 资源放 `Assets/Resources/Audio/Music/main_menu_bgm.{wav|ogg|mp3}` (与 AudioConfigFromLuban clipPath 路径一致 — 沿 `chapter1_ambient` precedent line 157 `clipPath: "Audio/Music/chapter1_ambient"`)；BGM 风格符合 art-bible.md / sound-design 规范 (悬念 / 主题 / 适合 main menu 心理学)
- [ ] **AC-2** AudioConfig entry add — 二选一路径：(a) 短期 stub path：`AudioConfigFromLuban.InitWithDefaults()` line 158 后追加 `RegisterMusic(new MusicConfig(id: 200, name: "main_menu_bgm", clipPath: "Audio/Music/main_menu_bgm", volume: 0.6f));` (与 chapter1_ambient 同等 baseline volume) — Sprint 7+ Luban TbAudio 真表 schema 未 ready 时走；(b) 长期 production path：Luban TbAudio.Music 真表 entry add (id, name=main_menu_bgm, clipPath, volume) + `AudioConfigFromLuban.InitFromLuban()` activation gate 切换 — Sprint 7+ Luban TbAudio schema generated 后走
- [ ] **AC-3** S6-07 R3 P3 spike 重跑验证 — fade-in + BGM hook mock spy assert 升级为 actual playback verify (mock spy invocation count assert 仍 PASS + 新增 GameModule.Audio.MusicVolume > 0 reflection capture or AudioManager 内部 _music layer state assert) — 5 asserts → 7 asserts；JSON evidence dump 重生成
- [ ] **AC-4** 0 unexpected console error/warning (Log.Warning `Music 'main_menu_bgm' not found in config` 不再 emit — PlayMusic 走正常 PlayMusicInternal path)
- [ ] **AC-5** main menu polish 完整 audio 体验 manual verify — chapter 1 launch flow E2E：main menu show 时 BGM 渐入 → user click NewGame → ISceneEvent.OnRequestSceneChange(1) → chapter 1 transition 期间 BGM 走 SceneManager / ChapterStateManager BGM 切换 hook (具体 hook 实现待 SceneManager epic Sprint 7+ chapter BGM transition story add — 本 story 不实施 BGM 切换，仅实施 main menu 启动 BGM)

---

## Implementation Notes

### File Targets

1. **Add asset** `Assets/Resources/Audio/Music/main_menu_bgm.{wav|ogg|mp3}` — sound design 阶段产出 + Unity Editor import settings 配置 (Compression / Load Type / Force To Mono 等 per ADR-017 §X audio asset import standards — 待 audio sound design epic Sprint 7+ 起步时统一)
2. **Update** `Assets/GameScripts/HotFix/GameLogic/Audio/AudioConfigFromLuban.cs` (line 158 后追加 1 RegisterMusic call) **OR** Luban TbAudio.Music 真表 entry add (具体路径 Sprint 7+ Audio epic Sprint 7+ Luban schema state 决定)

### Dev Notes

- 本 story 是 epic 边界 cleanup 模式实战 — 跨 epic boundary deficiency 留 follow-on placeholder 而非污染当前 sprint scope (per S6-07 Phase 2.0 R2.8 [D] mixed strategy precedent)
- 真 BGM asset 资源 (sound design + 美工配合) 是 sound design epic Sprint 7+ 工作；本 story 仅承担 asset add + config entry add 工作 (~1 hr)
- Sprint 7+ Production stage advance 后期 evaluate 是否 batch 与其他 chapter / scene BGM asset (chapter2_bgm / chapter3_bgm 等) 合并到一个 audio asset polish story
- 真 BGM 添加后，S6-07 R3 P3 case 自动升级 actual playback verify (从 mock spy 验证升级为真 playback verify)

---

## Dependencies

- S6-07 ✅ ui-system-006 main menu UIWindow polish DONE (production code BGM hook 已 wired — 本 story 仅添 asset + config entry)
- Sprint 7+ Production stage advance (production/stage.txt: Pre-Production → Production；S6-10 stage advance done 后)
- Sound design epic Sprint 7+ (BGM asset 制作 — 主题旋律 / mix / mastering)

---

## Out of Scope

- BGM 切换 (main menu → chapter 1 → chapter X transition 期间 BGM crossfade) — 留 SceneManager epic Sprint 7+ chapter BGM transition story
- chapter1_bgm + chapter2_bgm + ... 其他 chapter BGM asset (本 story scope 仅 main_menu_bgm 1 个)
- BGM volume / EQ / mix automation polish (留 audio polish epic Sprint 7+)

---

## History

- **2026-05-14 morning (Sprint 6 Session 30 Phase 2.0 R2.8 [D] mixed strategy closure 衍生)**: backlog placeholder 创建 — 衍生自 S6-07 ui-system-006 Phase 2.0 R2.8 deficiency flag closure 决策 [D] (epic 边界遵守 — UI epic 不 polluted Audio config table；BGM hook 完整保留 production code OnRefresh；真 asset + config entry 留 Sprint 7+ Production polish phase 起步时实施)。1 SP estimate (asset add + config entry add + S6-07 R3 P3 case 重跑验证)。governance precedent: 跨 epic boundary deficiency 留 follow-on placeholder 模式实战 (per ADR-029 V2.0 §V2-1 R2 DEFICIENCY-FLAGGED PASS path → CLOSURE 升级路径)。
