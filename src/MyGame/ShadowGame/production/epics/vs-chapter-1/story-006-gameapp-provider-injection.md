// 该文件由Cursor 自动生成

# Story 006: GameApp Provider Injection — InteractableObject.RegisterPuzzleConfigProvider + InteractionCoordinator.RegisterInputConfigProvider

> **Epic**: VS Chapter 1
> **Story ID**: vs-chapter-1-006 (Sprint 6 emergent fix Track F NEW；S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 — S0-4)
> **Sprint**: 6 (Track F NEW — chapter 1 production wiring emergent fix)
> **Story Type**: Logic (provider 静态注入 wiring)
> **Complexity Points**: 1 (~30 min 估时)
> **GDD Requirement**: 无 GDD TR-ID 直接锚点 — 本 story 是 Sprint 2 SP-013 设计的静态注入约定 production wiring 落地 (非 GDD requirement)
> **ADR References**: **ADR-013 §Architecture (PuzzleConfigProvider + InputConfigProvider 静态注入约定)** + Sprint 2 SP-013 (RegisterPuzzleConfigProvider 与 RegisterChapterDataProvider 同模式 precedent) + ADR-027 §3 IGestureEvent + §4 IInteractionEvent + ADR-029 V3.0.1 (R2 deficiency-flagged PASS path 候选 — Luban TbPuzzle / TbInput 可能 0-production stub) + ADR-030 §VS Build commit
> **Status**: ✅ **Done** (2026-06-15 — Phase 2 `/dev-story` implementation ✅ + R3 PlayMode 6/6 PASS + 19/19 asserts + evidence doc closure)
> **Created**: 2026-05-14 morning continue (Sprint 6 Session 32 — emergent fix Track F NEW third story)
> **Completed**: 2026-06-15
> **Depends on**: story-004 ✅ Done (input-pipeline-wiring) + story-005 ✅ Done (chapter-1-scene-wiring) + Sprint 2 SP-013 ✅ + S5-1b ✅ (BuildFixtureChapterDataProvider precedent)

---

## Context

**S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 emergent fix Track F 第 3 story (~30 min provider 静态注入 wiring)**：

S6-01 root cause analysis 5 处 wiring gap 中 **S0-4: GameApp.Entrance 0 调 InteractableObject.RegisterPuzzleConfigProvider + InteractionCoordinator.RegisterInputConfigProvider** —— `rg --type cs GameApp.cs 'RegisterPuzzleConfigProvider\|RegisterInputConfigProvider'` 0 hit。

Sprint 2 SP-013 设计的静态注入约定（与 RegisterChapterDataProvider 同模式）：
- `InteractableObject.RegisterPuzzleConfigProvider(Func<int, PuzzleConfig> provider)` — boot pipeline 在 InteractableObject OnEnable 之前调一次；fail-loud：未注册或 provider 返 null → `Log.Error` + 退化 (drag 不可用)
- `InteractionCoordinator.RegisterInputConfigProvider(Func<IInputConfig> provider)` — boot pipeline 在 Coordinator OnEnable 之前调一次；fail-loud：未注册 → `Log.Error` + 走 InputConfigFromLuban.InitWithDefaults() fallback

本 story 在 `GameApp.Entrance()` 内、`InputService.Init()` 之后、`StartGameLogic()` 之前调 2 个 Register provider（per S5-1b RegisterChapterDataProvider 注入位置 precedent + R2.7 实证 boot 顺序）。

---

## Goal Flow (T0 → T6)

```
T0  GameApp.Entrance(object[]) 被 TEngine boot pipeline 调用
T1  SceneManager Init + RegisterChapterDataProvider + RegisterFadeOverlay (S5-1b done)
T2  InputService Init (S6-13 story-004 done — Driver 层 Mouse→FSM→Dispatch)
T3  在 InputService.Init() 之后、RegisterDevSpikes()/StartGameLogic() 之前，加 2 个 Register provider（须在 chapter scene 加载 / InteractableObject.OnEnable 之前完成）:
    InteractableObject.RegisterPuzzleConfigProvider(BuildFixturePuzzleConfigProvider());
    InteractionCoordinator.RegisterInputConfigProvider(BuildFixtureInputConfigProvider());
T4  BuildFixturePuzzleConfigProvider() → Func<int, PuzzleConfig> hardcoded fixture（R2.5 实证 Luban TbPuzzle 0-production；**无** PuzzleConfig.Default 静态字段）:
    id => id switch {
        1 => new PuzzleConfig(id: 1,
            interactionBounds: new InteractionBounds(-10f, 10f, -10f, 10f),
            gridSize: 1f, snapSpeed: 0.2f, rotationStep: 15f),
        _ => null   // 未知 id fail-loud，与未来 ConfigSystem.Tables.TbPuzzle.Get 行为一致
    }
    (沿 InteractionCoordinatorTests.cs:58-62 EditMode fixture 先例 + S5-1b BuildFixtureChapterDataProvider 同模式)
T5  BuildFixtureInputConfigProvider() → Func<IInputConfig>（R2.6 实证 Luban TbInputConfig 0-production；**无** DefaultInputConfig 类）:
    () => { var cfg = new InputConfigFromLuban(); cfg.InitWithDefaults(); return cfg; }
    (沿 InteractionCoordinatorTests.cs:65-70 EditMode fixture 先例；Sprint 7+ migration: InitFromLuban row swap)
T6  R3 PlayMode probe（S6-15 spike）— `main.unity` boot → chapter 1 baseline 加载后:
    static provider 已注册 + InteractableObject._puzzleConfig != null + InteractionCoordinator IsLocked==false
    + 0 Log.Error('PuzzleConfigProvider 未注册' / 'InputConfigProvider 未注册')
```

---

## ADR Decision Summary

**ADR-013 §Architecture inherit** — 本 story 不 amend：
- PuzzleConfigProvider + InputConfigProvider 静态注入约定 (Sprint 2 SP-013 定义)
- 与 ADR-009 RegisterChapterDataProvider 同模式 (S5-1b done — BuildFixtureChapterDataProvider precedent)

**ADR-029 V3.0.1 R2 deficiency-flagged PASS path 候选** — Phase 0 R2 实证触发（2026-06-12）:
- Luban **TbPuzzle 0-production** → hardcoded `BuildFixturePuzzleConfigProvider` fixture（**非** `PuzzleConfig.Default` — R2.3 实证无此静态字段）
- Luban **TbInputConfig 0-production** → `InputConfigFromLuban.InitWithDefaults()` fixture（**非** `DefaultInputConfig` 类 — R2.4 实证不存在）

**Sprint 7+ ADR-XX (TbPuzzle / TbInput 真接入) epic boundary** — 本 story 仅 stub fallback；真 Luban 表生成 + 真 InputConfig wiring 留 Sprint 7+ epic（同 BuildFixtureChapterDataProvider 留 post-VS）。

---

## Engine Notes

**Phase 0+1 R2 vendor reality verify ✅ DONE** (2026-06-12 Session 34) — 详 **§R2 Assumptions Validated** 表。

**ADR-029 verdict**: **✅ DEFICIENCY-FLAGGED PASS** — R1 ✅ + R2 ⚠️ DEFICIENCY-FLAGGED PASS (Luban TbPuzzle/TbInputConfig 0-production inline closed via fixture) + R3 ✅ PASS (§R3 PlayMode Probe Plan stub 构造签名与 `PuzzleConfig.cs:41` / `InputConfigFromLuban.InitWithDefaults()` 一致)。详 **§ADR-029 Verification**。

**perf budget**: boot 仅 2 次 static Register + 2 lambda；无 per-frame alloc；R3 spike 目标 **< 5s** total elapsed（沿 S6-13/S6-14 precedent；chapter 1 baseline 加载占主要时间）。

---

## Control Manifest Rule References

**Phase 1 R1 grep audit ✅ DONE** (2026-06-12 Session 34)：

- ✅ **Required** — `RegisterPuzzleConfigProvider` / `RegisterInputConfigProvider` 必须在任意 `InteractableObject` / `InteractionCoordinator` `OnEnable`→`Initialize()` 之前完成（`GameApp.Entrance` 内 `StartGameLogic()` 之前；per ADR-013 §Architecture + `InteractableObject.cs:64` / `InteractionCoordinator.cs:63` 文档）
- ✅ **Required** — Luban TbPuzzle / TbInputConfig 0-production 时走 hardcoded fixture fallback（per S5-1b `BuildFixtureChapterDataProvider` precedent；R2.5/R2.6 DEFICIENCY inline closed）
- ✅ **Forbidden** — boot phase 之后 mid-runtime swap provider（Sprint 2 SP-013 注入约定 one-time static field；无 `Unregister` API）

---

## Acceptance Criteria

| # | AC | Verify path |
|---|-----|------|
| AC-1 | GameApp.cs Init() 内调 `InteractableObject.RegisterPuzzleConfigProvider(...)` 1 次 | rg `InteractableObject\.RegisterPuzzleConfigProvider` Assets/GameScripts/HotFix/GameLogic/GameApp.cs |
| AC-2 | GameApp.cs Init() 内调 `InteractionCoordinator.RegisterInputConfigProvider(...)` 1 次 | rg `InteractionCoordinator\.RegisterInputConfigProvider` Assets/GameScripts/HotFix/GameLogic/GameApp.cs |
| AC-3 | Provider 注入位置在 SceneManager Init + InputService.Init 之后、`RegisterDevSpikes()`/`StartGameLogic()` 之前 | Read `GameApp.cs` `Entrance()` ordering (`:50-69` baseline + NEW 2 Register 插入 `:62-63` 后) |
| AC-4 | `BuildFixturePuzzleConfigProvider()` — `Func<int, PuzzleConfig>` return；puzzleId=1 返非 null hardcoded fixture | Read `GameApp.cs` |
| AC-5 | `BuildFixtureInputConfigProvider()` — `Func<IInputConfig>` return；返 `InputConfigFromLuban` + `InitWithDefaults()` | Read `GameApp.cs` |
| AC-6 | Luban TbPuzzle 0-production 时 `BuildFixturePuzzleConfigProvider` hardcoded fixture（`new PuzzleConfig(id:1, InteractionBounds(-10..10), ...)`）；**非** `PuzzleConfig.Default` | R2.5 ✅ + Read `GameApp.cs` |
| AC-7 | Luban TbInputConfig 0-production 时 `BuildFixtureInputConfigProvider` 返 `InputConfigFromLuban.InitWithDefaults()`；**非** `DefaultInputConfig` 类 | R2.6 ✅ + Read `GameApp.cs` |
| AC-8 | PlayMode probe Editor start 后 InteractableObject._puzzleConfig != null + 0 fail-loud Log.Error('PuzzleConfigProvider 未注册...') | R3 PlayMode probe + console listener |
| AC-9 | PlayMode probe Editor start 后 InteractionCoordinator IsLocked == false + InputConfig 注入成功 + 0 fail-loud Log.Error | R3 PlayMode probe + console listener |
| AC-10 | 0 unexpected console error/warning during boot (provider 注入 fail-safe, 不抛异常 + warning 仅 fallback 路径 expected) | R3 evidence dump UnexpectedErrorCount==0 |

---

## R3 PlayMode Probe Plan（Phase 1 readiness gate amend ✅ 2026-06-12）

Spike `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-15_GameAppProviderInjection.cs` (~350-450 行；1 file + 3 inner class `S615Spike` : `IDevSpike` + `S615Runtime` + `S615Tester` per S6-13/S6-14 precedent)。

**Boot 路径**: `Assets/Scenes/main.unity` Play → `DevTestState` `[main-menu]` → spike 自驱（**不需**手动点 NewGame）。

**Run order**: baseline → P1 → P2 → P3 → P4 → P5（chapter 1 baseline = `OnRequestSceneChange(1)` + `WaitForIdleAsync` per S6-04/S6-14）。

| Case | Setup | Action | Assert |
|------|-------|--------|--------|
| **baseline** | `main.unity` boot 完成 | `OnRequestSceneChange(1)` + wait Idle | `state=Idle`, `currentChapterId=1` |
| **P1 StaticPuzzleConfigProviderRegistered** | boot 后（chapter 1 加载前即可） | reflection 读 `InteractableObject` static `_puzzleConfigProvider` field | `!= null`；invoke `provider(1)` 返非 null `PuzzleConfig` |
| **P2 StaticInputConfigProviderRegistered** | 同 P1 | reflection 读 `InteractionCoordinator` static `_inputConfigProvider` | `!= null`；invoke `provider()` 返非 null `IInputConfig` |
| **P3 InteractableObjectPuzzleConfigResolved** | chapter 1 baseline 加载后 | `FindObjectsOfType<InteractableObject>()` | 每实例 reflection `_puzzleConfig != null`；`Id==1`；`InteractionBounds` MinX=-10 MaxX=10（fixture 值） |
| **P4 CoordinatorInputConfigResolved** | chapter 1 baseline 后 | `FindObjectOfType<InteractionCoordinator>()` | `IsLocked==false`；reflection `_inputConfig != null`；`FatFingerMarginMm > 0`（`InitWithDefaults` 实证 8mm） |
| **P5 NoFailLoudProviderErrors** | `Application.logMessageReceived` spy 全程 | boot → chapter 1 load 全流程 | 0 `Log.Error` 含 `PuzzleConfigProvider 未注册`；0 含 `InputConfigProvider 未注册`；`UnexpectedErrorCount==0` |

**JSON evidence**: `~/Library/Application Support/DefaultCompany/Unity/S6-15_Result.json`（`WriteResultJson()` per S6-13 precedent）。

**dp15 sniff sub-clause（implementation 后 grep verify）**: `rg 'RegisterPuzzleConfigProvider' GameApp.cs` production caller **≥1**；`rg 'RegisterInputConfigProvider' GameApp.cs` **≥1**（本 story = dp15 第 3 个 production wiring 修复 case）。

**R3 stub 构造签名（ADR-029 R3 verify）**:
- `PuzzleConfig(int id, InteractionBounds interactionBounds, float gridSize=1f, float snapSpeed=0.2f, float rotationStep=15f)` — `PuzzleConfig.cs:41`
- `new InteractionBounds(-10f, 10f, -10f, 10f)` — `InteractionBounds` struct ctor 4 float
- `new InputConfigFromLuban()` + `InitWithDefaults()` — `InputConfigFromLuban.cs:54`（无参实例方法）

**GameApp/DevTestState 切换**（Phase 2 implementation）: `RegisterDevSpikes` S614→S615；`DevTestState` `[main-menu]` HasSpike list +1 `S6-15`（dp8 阈值 7→8）。

---

## R2 Assumptions Validated（Phase 0 ✅ DONE 2026-06-12 Session 34）

| # | Assumption | Verify | Status |
|---|------------|--------|--------|
| R2.1 | `InteractableObject.RegisterPuzzleConfigProvider(Func<int, PuzzleConfig>)` | `InteractableObject.cs:66` | ✅ FULLY MATCH |
| R2.2 | `InteractionCoordinator.RegisterInputConfigProvider(Func<IInputConfig>)` | `InteractionCoordinator.cs:65` | ✅ FULLY MATCH |
| R2.3 | `PuzzleConfig` POCO — sealed class + readonly 字段 + ctor | `ObjectInteraction/PuzzleConfig.cs:41` `PuzzleConfig(int id, InteractionBounds, float gridSize=1f, float snapSpeed=0.2f, float rotationStep=15f)` | ✅ **无 `PuzzleConfig.Default`** — 原 Goal Flow T3 幻觉已 amend 为 hardcoded fixture |
| R2.4 | `IInputConfig` + 实现类 | `IInputConfig.cs` + `InputConfigFromLuban.InitWithDefaults()` (`InputConfigFromLuban.cs:54`) | ✅ **无 `DefaultInputConfig` 类** — 原 Goal Flow T4 幻觉已 amend |
| R2.5 | Luban TbPuzzle production 现状 | `glob **/TbPuzzle*.cs` → **0 file**；`PuzzleStateConfigFromLuban` 是 ShadowPuzzle 另一 POCO，非 ObjectInteraction `PuzzleConfig` | ⚠️ **0-production DEFICIENCY** → `BuildFixturePuzzleConfigProvider` hardcoded（S5-1b precedent） |
| R2.6 | Luban TbInputConfig production 现状 | `ConfigSystem.Tables.TbInput*` 未接入；`InputService.Init()` 已自用 `InitWithDefaults()` | ⚠️ **0-production DEFICIENCY** → `BuildFixtureInputConfigProvider` 返 `InputConfigFromLuban` + `InitWithDefaults()` |
| R2.7 | `GameApp.Entrance()` boot 顺序 | `GameApp.cs:50-69` SceneManager → InputService.Init → (NEW providers here) → RegisterDevSpikes → StartGameLogic | ✅ provider 须在 `StartGameLogic()` 前（chapter scene / OnEnable 前） |
| R2.8 | `ClearPuzzleConfigProviderForTest` / `ClearInputConfigProviderForTest` | `InteractableObject.cs:75` / `InteractionCoordinator.cs:71` | ✅ EditMode test fixture 先例完整 |

**R2 fail-loud 行为实证**（implementation 须知）:
- `InteractableObject.ResolvePuzzleConfig()` (`:496-506`) — provider 未注册或返 null → `Log.Error` + `_puzzleConfig` 仍 null → drag 不工作（不调 `enabled=false`）
- `InteractionCoordinator.ResolveInputConfig()` (`:328-345`) — provider 未注册 → `Log.Error` + **自动 fallback** `InputConfigFromLuban.InitWithDefaults()`（AC-9 仍要求注册以避免 fail-loud Log.Error）

**EditMode fixture 先例**（Goal Flow T4/T5 来源）: `Assets/Tests/EditMode/ObjectInteraction/InteractionCoordinatorTests.cs:58-70`

---

## V3.0.1 Watch List Hooks

**Type-11 V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff"** — 本 story 是 dp15 候选 sniff sub-clause **第 3 个 production wiring 修复 case**（GameApp.Init Register provider 0-production caller → fix 后 2 处调用）。

**Type-2 (a) V3 candidate "Luban stub fallback drift"** — 留观察 — TbPuzzle/TbInputConfig 0-production 时本 story 走 hardcoded fixture + `InitWithDefaults()`（与 S5-1b `BuildFixtureChapterDataProvider` 同模式）；Sprint 6 Track F 累计 stub fallback 路径 ≥3 → V3 trigger evaluate。

**Type-5 V3.0.1 candidate "spec/tooling ↔ reality drift"** — R2.3 closure ✅（原 Goal Flow `PuzzleConfig.Default` 幻觉已 amend）；无新增 drift。

---

## Test Evidence

**Story Type**: Logic → Evidence doc + R3 JSON（per S6-13 / S6-14 precedent）：

- **Evidence doc**: `production/qa/playmode-gameapp-provider-injection-2026-06-12.md` ~350-450 行 8 sections (§0 概要 + §1 R3 5 case detail + §2 R2 8/8 closure 表 + §3 AC 10/10 verify + §4 V3.0.1 Watch List Hooks dp15 第 3 case + §5 Sprint 6 Track F insight + §6 Files changed + §7 References + §8 Verdict)
- **R3 spike JSON dump**: `~/Library/Application Support/DefaultCompany/Unity/S6-15_Result.json`
- **Production grep verify**: `rg 'RegisterPuzzleConfigProvider|RegisterInputConfigProvider' Assets/GameScripts/HotFix/GameLogic/GameApp.cs` → 各 ≥1 hit（AC-1/AC-2 + dp15 sniff）
- **0 scene diff**: 本 story 不改 `Chapter_01_Approach.unity`
- **0 ProjectSettings diff**: 无 Layer/Tag 变更

---

## ADR-029 Verification

**Phase 1 R1+R2+R3 readiness gate verdict (Session 34 2026-06-12)**：

- **R1 ✅ PASS** — per-event listener mode forbidden pattern grep audit:
  - `rg "AddEventListener<I\w+Event>\(this\)" story-006-gameapp-provider-injection.md` → 0 hits ✅
  - `rg "class \w+\s*:\s*\w+,\s*I\w+Event" story-006-gameapp-provider-injection.md` → 0 hits ✅
  - 本 story 不含 listener 实现代码；production listener 已在 Sprint 2 SP-013 以 per-event 模式存在
- **R2 ⚠️ DEFICIENCY-FLAGGED PASS** — cross-component API existence（Phase 0 实证 2026-06-12）:
  - `InteractableObject.RegisterPuzzleConfigProvider` `InteractableObject.cs:66` ✅
  - `InteractionCoordinator.RegisterInputConfigProvider` `InteractionCoordinator.cs:65` ✅
  - `PuzzleConfig` ctor `ObjectInteraction/PuzzleConfig.cs:41` ✅（**无 Default 静态字段**）
  - `InputConfigFromLuban.InitWithDefaults()` `InputConfigFromLuban.cs:54` ✅（**无 DefaultInputConfig 类**）
  - Luban `TbPuzzle` / `TbInputConfig` **0-production** ⚠️ DEFICIENCY → inline closed via `BuildFixture*` hardcoded fixture（S5-1b precedent；Sprint 7+ epic boundary）
  - `GameApp.Entrance` boot 顺序 `GameApp.cs:50-69` ✅
  - `ClearPuzzleConfigProviderForTest` / `ClearInputConfigProviderForTest` ✅
- **R3 ✅ PASS** — stub data type construction signature verify:
  - `PuzzleConfig(int, InteractionBounds, float gridSize, float snapSpeed, float rotationStep)` 与 §R3 PlayMode Probe Plan P1/P3 fixture 一致
  - `InputConfigFromLuban` 无参构造 + `InitWithDefaults()` 与 P2/P4 fixture 一致
  - spike 不直接 `new PuzzleConfig` 绕过 provider — 验 production `GameApp` Register 路径 + chapter 1 加载后 `_puzzleConfig` resolved

**ADR-029 verdict**: **✅ DEFICIENCY-FLAGGED PASS** (R1 ✅ + R2 ⚠️ DEFICIENCY-FLAGGED PASS + R3 ✅ PASS) → **READY for `/dev-story`**

---

## Out of Scope（明示）

- ❌ **Luban TbPuzzle 真接入** — Sprint 7+ post-VS Luban 真表生成 epic
- ❌ **Luban TbInput 真接入** — Sprint 7+ post-VS Luban 真表生成 epic
- ❌ **PuzzleConfig 多 puzzle support** — chapter 1 仅 puzzle 1 (per TR-objint-002)；多 puzzle 留 chapter 2-5 epic
- ❌ **InputConfig hot reload** — Settings menu input remap (S5-09 backlog)
- ❌ **Provider mid-runtime swap** — Forbidden per Sprint 2 SP-013 spec (Provider 是 boot phase one-time)

---

## Implementation Notes（Phase 1 readiness gate amend ✅ 2026-06-12）

预计涉及文件（**~5-15 行 production logic** + spike NEW）：

1. **`GameApp.cs`** `Entrance()` — `InputService.Init()` 之后插入 2 行 Register + 2 个 `BuildFixture*` private static helper（与 `BuildFixtureChapterDataProvider` 同模式，`:80-99` 旁）
2. **`S6-15_GameAppProviderInjection.cs`** NEW ~350-450 行 — 5 R3 case + JSON dump（§R3 PlayMode Probe Plan）
3. **`GameApp.cs`** `RegisterDevSpikes` — S614Spike → **S615Spike** 切换（注释保留 S614 复跑入口）
4. **`DevTestState.cs`** `[main-menu]` mode HasSpike list +1 `S6-15`（7→8 spike；dp8 candidate）

**不需 NEW 文件**: ~~`DefaultPuzzleConfig.cs` / `DefaultInputConfig.cs`~~（R2.3/R2.4 实证已有 `PuzzleConfig` ctor + `InputConfigFromLuban`）。

**0 scene / 0 ProjectSettings change**。

---

## History

- **2026-05-14 morning continue (Session 32)**: Draft 创建（emergent fix epic Track F NEW story-004~008 outline approved per [A]）；Status: Draft；S0-4 narrow scope；本 story 是 5 处 wiring gap 中最少 production code change 的 (~30 min 估时)；与 story-005 联动 (story-005 InteractableObject MonoBehaviour 挂载后 OnEnable 才会 trigger PuzzleConfigProvider invocation)。
- **2026-06-12 (Session 34)**: `/story-readiness` gate user **[B] 决策** — 仅 amend Goal Flow + §R2 表（Phase 0 partial）。
- **2026-06-12 (Session 34 continue)**: **Phase 1 readiness gate gap closure amend ✅ DONE** — AC-3/6/7 修订 + §Control Manifest 3 条落档 + §R3 PlayMode Probe Plan 5 case 精化 (`S6-15_GameAppProviderInjection.cs`) + §Test Evidence NEW + §ADR-029 Verification NEW + §Implementation Notes 精化（剔除 Default 类 NEW）+ §Engine Notes full verdict；Status: Draft → **✅ READY**；next `/dev-story` implementation (~30 min)。
- **2026-06-15**: **Phase 2~5 `/dev-story` + R3 closure ✅ DONE** — `GameApp.cs` 2× Register + 2× `BuildFixture*` helper (~30 行) + spike `S6-15_GameAppProviderInjection.cs` NEW (~525 行) + GameApp S614→S615 + DevTestState HasSpike(S6-15)；R3 PlayMode **6/6 case PASS + 19/19 asserts + `all_passed=true` + `unexpected_error_count=0` + `fail_loud_provider_error_count=0` + `total_elapsed_ms=234`** first-run；evidence `production/qa/playmode-gameapp-provider-injection-2026-06-12.md`；Status: READY → **✅ Done**；Track F 3/5 done → NEXT story-007 shadowmatch production wire。
