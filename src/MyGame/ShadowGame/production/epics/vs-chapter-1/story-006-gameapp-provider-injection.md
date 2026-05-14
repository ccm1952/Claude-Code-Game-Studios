// 该文件由Cursor 自动生成

# Story 006: GameApp Provider Injection — InteractableObject.RegisterPuzzleConfigProvider + InteractionCoordinator.RegisterInputConfigProvider

> **Epic**: VS Chapter 1
> **Story ID**: vs-chapter-1-006 (Sprint 6 emergent fix Track F NEW；S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 — S0-4)
> **Sprint**: 6 (Track F NEW — chapter 1 production wiring emergent fix)
> **Story Type**: Logic (provider 静态注入 wiring)
> **Complexity Points**: 1 (~30 min 估时)
> **GDD Requirement**: 无 GDD TR-ID 直接锚点 — 本 story 是 Sprint 2 SP-013 设计的静态注入约定 production wiring 落地 (非 GDD requirement)
> **ADR References**: **ADR-013 §Architecture (PuzzleConfigProvider + InputConfigProvider 静态注入约定)** + Sprint 2 SP-013 (RegisterPuzzleConfigProvider 与 RegisterChapterDataProvider 同模式 precedent) + ADR-027 §3 IGestureEvent + §4 IInteractionEvent + ADR-029 V3.0.1 (R2 deficiency-flagged PASS path 候选 — Luban TbPuzzle / TbInput 可能 0-production stub) + ADR-030 §VS Build commit
> **Status**: 📝 **Draft** (2026-05-14 Session 32 morning continue — emergent fix epic Track F NEW story-004~008 outline approved per [A]，本 story Phase 0 R2 vendor reality verify pending)
> **Created**: 2026-05-14 morning continue (Sprint 6 Session 32 — emergent fix Track F NEW third story)
> **Completed**: ""
> **Depends on**: story-004 input-pipeline-wiring (本 story InputConfigProvider 注入需 GestureRecognizer 已能 fire IGestureEvent 才生效) + story-005 chapter-1-scene-wiring (本 story PuzzleConfigProvider 注入需 InteractableObject MonoBehaviour 已挂 才能 OnEnable 时调 RegisterPuzzleConfigProvider) + Sprint 2 SP-013 ✅ + S5-1b ✅ (BuildFixtureChapterDataProvider precedent — 同模式 stub fallback)

---

## Context

**S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 emergent fix Track F 第 3 story (~30 min provider 静态注入 wiring)**：

S6-01 root cause analysis 5 处 wiring gap 中 **S0-4: GameApp.Entrance 0 调 InteractableObject.RegisterPuzzleConfigProvider + InteractionCoordinator.RegisterInputConfigProvider** —— `rg --type cs GameApp.cs 'RegisterPuzzleConfigProvider\|RegisterInputConfigProvider'` 0 hit。

Sprint 2 SP-013 设计的静态注入约定（与 RegisterChapterDataProvider 同模式）：
- `InteractableObject.RegisterPuzzleConfigProvider(Func<int, PuzzleConfig> provider)` — boot pipeline 在 InteractableObject OnEnable 之前调一次；fail-loud：未注册或 provider 返 null → `Log.Error` + 退化 (drag 不可用)
- `InteractionCoordinator.RegisterInputConfigProvider(Func<IInputConfig> provider)` — boot pipeline 在 Coordinator OnEnable 之前调一次；fail-loud：未注册 → `Log.Error` + 走 InputConfigFromLuban.InitWithDefaults() fallback

本 story 在 GameApp.Entrance Initialize() 内调 2 个 Register provider，sit between StartGameLogic() 之前（per S5-1b RegisterChapterDataProvider 注入位置 precedent）。

---

## Goal Flow (T0 → T5)

```
T0  GameApp.Entrance(object[]) 被 TEngine boot pipeline 调用 → Init() 进入
T1  Init() 内既有 SceneManager Init + RegisterChapterDataProvider + RegisterFadeOverlay 序列 (S5-1b done)
T2  在 SceneManager Init 之后、StartGameLogic() 之前，加 2 个 Register provider:
    InteractableObject.RegisterPuzzleConfigProvider(BuildFixturePuzzleConfigProvider());
    InteractionCoordinator.RegisterInputConfigProvider(BuildFixtureInputConfigProvider());
T3  BuildFixturePuzzleConfigProvider() return Func<int, PuzzleConfig>:
    puzzleId => TbPuzzle.Get(puzzleId) ?? PuzzleConfig.Default
    (Luban TbPuzzle 表生成现状 R2 verify 后选择 — 如 stub 则 fallback PuzzleConfig.Default)
T4  BuildFixtureInputConfigProvider() return Func<IInputConfig>:
    () => InputConfigFromLuban.Default ?? new DefaultInputConfig()
    (与 BuildFixtureChapterDataProvider hardcoded 同模式 — 如 Luban TbInput 0-production 走 InputConfigFromLuban.InitWithDefaults() S6-07 R2.6 提及的 stub fallback)
T5  R3 PlayMode probe verify Editor PlayMode start 后:
    InteractableObject._puzzleConfig != null (provider 返非 null) + 0 fail-loud Log.Error;
    InteractionCoordinator IsLocked == false + InputConfig 注入成功
```

---

## ADR Decision Summary

**ADR-013 §Architecture inherit** — 本 story 不 amend：
- PuzzleConfigProvider + InputConfigProvider 静态注入约定 (Sprint 2 SP-013 定义)
- 与 ADR-009 RegisterChapterDataProvider 同模式 (S5-1b done — BuildFixtureChapterDataProvider precedent)

**ADR-029 V3.0.1 R2 deficiency-flagged PASS path 候选** — 如下任一情况触发 deficiency flag:
- Luban TbPuzzle 表生成 0-production → fallback 到 PuzzleConfig.Default (S5-1b BuildFixtureChapterDataProvider 同 stub fallback precedent)
- Luban TbInput 表生成 0-production → fallback 到 InputConfigFromLuban.InitWithDefaults() S6-07 R2.6 已 surfaced stub fallback

**Sprint 7+ ADR-XX (TbPuzzle / TbInput 真接入) epic boundary** — 本 story 仅 stub fallback；真 Luban 表生成 + 真 InputConfig wiring 留 Sprint 7+ epic（同 BuildFixtureChapterDataProvider 留 post-VS）。

---

## Engine Notes

**待 /story-readiness gate Phase 0 R2 vendor reality verify**：
- R2.1 ⚠️ TBD: InteractableObject.RegisterPuzzleConfigProvider 签名 verify (Func<int, PuzzleConfig> 还是其他 signature)
- R2.2 ⚠️ TBD: InteractionCoordinator.RegisterInputConfigProvider 签名 verify (Func<IInputConfig> 还是其他)
- R2.3 ⚠️ TBD: PuzzleConfig POCO 结构 + Default value (是否 readonly static field)
- R2.4 ⚠️ TBD: IInputConfig interface + DefaultInputConfig / InputConfigFromLuban 现状
- R2.5 ⚠️ TBD: Luban TbPuzzle 表生成 production 现状 (0-production / partial / done) — 影响 fallback strategy
- R2.6 ⚠️ TBD: Luban TbInput 表生成 production 现状 — 影响 fallback strategy
- R2.7 ⚠️ TBD: GameApp.Init() 调用顺序 — Register provider 必须在 Initialize() 之前 + main menu show 之前
- R2.8 ⚠️ TBD: ClearPuzzleConfigProviderForTest / ClearInputConfigProviderForTest 测试 helper (per Sprint 2 SP-013 EditMode test fixture precedent — 不影响 production 但 R2 完整性 verify)

---

## Control Manifest Rule References

**待 /story-readiness gate Phase 1 R1 grep audit**：
- ⚠️ TBD: Required — Provider 必须在 Coordinator/InteractableObject OnEnable 之前注册 (per Sprint 2 SP-013 注入约定)
- ⚠️ TBD: Required — Fallback stub (per S5-1b BuildFixtureChapterDataProvider precedent — Luban 表 stub fallback)
- ⚠️ TBD: Forbidden — Provider 注入后 mid-runtime 改 (注入是 boot phase one-time，不应运行时 swap)

---

## Acceptance Criteria

| # | AC | Verify path |
|---|-----|------|
| AC-1 | GameApp.cs Init() 内调 `InteractableObject.RegisterPuzzleConfigProvider(...)` 1 次 | rg `InteractableObject\.RegisterPuzzleConfigProvider` Assets/GameScripts/HotFix/GameLogic/GameApp.cs |
| AC-2 | GameApp.cs Init() 内调 `InteractionCoordinator.RegisterInputConfigProvider(...)` 1 次 | rg `InteractionCoordinator\.RegisterInputConfigProvider` Assets/GameScripts/HotFix/GameLogic/GameApp.cs |
| AC-3 | Provider 注入位置在 SceneManager Init 之后、StartGameLogic() 之前 (与 S5-1b precedent 同模式) | Read GameApp.cs Init() method ordering |
| AC-4 | BuildFixturePuzzleConfigProvider() inline helper or 独立 method — Func<int, PuzzleConfig> return type | Read GameApp.cs |
| AC-5 | BuildFixtureInputConfigProvider() inline helper or 独立 method — Func<IInputConfig> return type | Read GameApp.cs |
| AC-6 | Luban TbPuzzle 0-production 时 fallback 到 PuzzleConfig.Default 或 hardcoded fixture (per S5-1b BuildFixtureChapterDataProvider precedent) | R2 verify + Read |
| AC-7 | Luban TbInput 0-production 时 fallback 到 InputConfigFromLuban.InitWithDefaults() 或 DefaultInputConfig (per S6-07 R2.6 提及 stub fallback) | R2 verify + Read |
| AC-8 | PlayMode probe Editor start 后 InteractableObject._puzzleConfig != null + 0 fail-loud Log.Error('PuzzleConfigProvider 未注册...') | R3 PlayMode probe + console listener |
| AC-9 | PlayMode probe Editor start 后 InteractionCoordinator IsLocked == false + InputConfig 注入成功 + 0 fail-loud Log.Error | R3 PlayMode probe + console listener |
| AC-10 | 0 unexpected console error/warning during boot (provider 注入 fail-safe, 不抛异常 + warning 仅 fallback 路径 expected) | R3 evidence dump UnexpectedErrorCount==0 |

---

## R3 PlayMode Probe Plan（待 /story-readiness gate amend detail）

预计 spike `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-XX_GameAppProviderInjection.cs`：

- **P1 PuzzleConfigProviderRegistered** — Editor PlayMode boot → reflect InteractableObject._inputConfigProvider 字段 / 调 InteractableObject.RegisterPuzzleConfigProvider sniff invocation count > 0
- **P2 InputConfigProviderRegistered** — InteractionCoordinator._inputConfigProvider != null after boot
- **P3 InteractableObjectPuzzleConfigResolved** — chapter 1 scene loaded 后 FindObjectOfType<InteractableObject>()._puzzleConfig != null + value matches PuzzleConfig.Default 或 TbPuzzle.Get(1)
- **P4 NoFailLoudErrorOnBoot** — Application.logMessageReceived listener spy expect 0 Log.Error containing 'PuzzleConfigProvider 未注册' or 'InputConfigProvider 未注册'
- **P5 FallbackStubResilience** — 测 Luban TbPuzzle 不存在时 provider lambda return PuzzleConfig.Default + fallback 路径 walk through

---

## R2 Assumptions Validated（待 Phase 0 实证）

| # | Assumption | Verify | Status |
|---|------------|--------|--------|
| R2.1 | InteractableObject.RegisterPuzzleConfigProvider 签名 | Read InteractableObject.cs static method | ⚠️ TBD |
| R2.2 | InteractionCoordinator.RegisterInputConfigProvider 签名 | Read InteractionCoordinator.cs | ⚠️ TBD |
| R2.3 | PuzzleConfig POCO 结构 + Default | Read PuzzleConfig.cs | ⚠️ TBD |
| R2.4 | IInputConfig interface + DefaultInputConfig | Read IInputConfig.cs + DefaultInputConfig.cs | ⚠️ TBD |
| R2.5 | Luban TbPuzzle 现状 | Read Luban gen + grep TbPuzzle.cs | ⚠️ TBD |
| R2.6 | Luban TbInput 现状 | Read Luban gen + grep TbInput.cs | ⚠️ TBD |
| R2.7 | GameApp.Init() 顺序 | Read GameApp.cs Init() | ⚠️ TBD |
| R2.8 | ClearProviderForTest helper | Read InteractableObject.cs / InteractionCoordinator.cs | ⚠️ TBD |

---

## V3.0.1 Watch List Hooks

**Type-11 V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff"** — 本 story 是 dp15 候选 sniff sub-clause **第 3 个 production wiring 修复 case**（GameApp.Init Register provider 0-production caller → fix 后 2 处调用）。

**Type-2 (a) V3 candidate "Luban stub fallback drift"** — 留观察 — 如 Luban TbPuzzle / TbInput 0-production 时 fallback 走 PuzzleConfig.Default + DefaultInputConfig (与 S5-1b BuildFixtureChapterDataProvider hardcoded fixture 同模式)，是否多 sprint 累计 fallback 路径 stub fallback ≥3 个 → V3 trigger evaluate。

**Type-5 V3.0.1 candidate "spec/tooling ↔ reality drift"** — 留观察 — Sprint 2 SP-013 PuzzleConfig 字段 list 与本 story Goal Flow T3 期望是否 drift；R2.3 verify。

---

## Out of Scope（明示）

- ❌ **Luban TbPuzzle 真接入** — Sprint 7+ post-VS Luban 真表生成 epic
- ❌ **Luban TbInput 真接入** — Sprint 7+ post-VS Luban 真表生成 epic
- ❌ **PuzzleConfig 多 puzzle support** — chapter 1 仅 puzzle 1 (per TR-objint-002)；多 puzzle 留 chapter 2-5 epic
- ❌ **InputConfig hot reload** — Settings menu input remap (S5-09 backlog)
- ❌ **Provider mid-runtime swap** — Forbidden per Sprint 2 SP-013 spec (Provider 是 boot phase one-time)

---

## Implementation Notes（高层结构，待 Phase 0/1 R2 verify 后 amend 精细）

预计涉及文件：
1. **GameApp.cs** Init() — 加 2 处 Register call + 2 个 BuildFixtureXxxProvider helper method (与 BuildFixtureChapterDataProvider precedent inline 同模式)
2. **(可能 Stub class) DefaultPuzzleConfig.cs / DefaultInputConfig.cs** — 如 R2.3/R2.4 verify 缺 Default/Stub class 则 NEW (~30-50 行 each)
3. **Spike S6-XX_GameAppProviderInjection.cs** NEW — R3 PlayMode probe 5 case 验 provider 注入 + fail-loud no-trigger + fallback resilience

**~5-15 行 production code change estimate** (GameApp.cs Init 加 2 Register + 2 BuildFixture helper inline；如缺 Default class 另加 ~30-50 行)。

---

## History

- **2026-05-14 morning continue (Session 32)**: Draft 创建（emergent fix epic Track F NEW story-004~008 outline approved per [A]）；Status: Draft；S0-4 narrow scope；本 story 是 5 处 wiring gap 中最少 production code change 的 (~30 min 估时)；与 story-005 联动 (story-005 InteractableObject MonoBehaviour 挂载后 OnEnable 才会 trigger PuzzleConfigProvider invocation)。
