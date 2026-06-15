// 该文件由Cursor 自动生成

# S6-15 R3 PlayMode Evidence — GameApp Provider Injection (RegisterPuzzleConfigProvider + RegisterInputConfigProvider) (2026-06-15)

> **Story**: S6-15 — GameApp Provider Injection (Track F vs-chapter-1-006 第 3 story emergent fix — S0-4 GameApp.Entrance 0 调 Register provider)
> **Sprint**: 6 (Track F emergent fix — VS Chapter 1 epic production wiring 5 处 gap 补全 第 3/5 story)
> **Epic**: vs-chapter-1
> **Type**: Logic (provider 静态注入 wiring + ~30 行 production code)
> **Engine**: Unity 2022.3.62f2 LTS + URP + HybridCLR + YooAsset 2.3.17 + UniTask + TEngine 6.2.1
> **Date**: 2026-06-15 (R3 PlayMode 实测；implementation Session 34；R3 first-run PASS)
> **Verdict**: ✅ **PASS** (6/6 R3 case first-run + 19/19 asserts + `all_passed=true` + `unexpected_error_count=0` + `fail_loud_provider_error_count=0` + `total_elapsed_ms=234` ≪ 5s budget)
> **Story file**: `production/epics/vs-chapter-1/story-006-gameapp-provider-injection.md`
> **Governing ADRs**: ADR-013 §Architecture (PuzzleConfigProvider + InputConfigProvider 静态注入约定) + Sprint 2 SP-013 + ADR-029 V3.0.1 (R2 DEFICIENCY-FLAGGED PASS — Luban TbPuzzle/TbInput 0-production → fixture) + ADR-030 §VS Build commit
> **Spike file**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-15_GameAppProviderInjection.cs` (~525 行 / 1 文件 + 3 内类 S615Spike : IDevSpike + S615Runtime + S615Tester — 沿用 S6-13/S6-14 precedent)
> **Production code**: `GameApp.cs` — 2× Register + 2× `BuildFixture*` helper (~30 行) + RegisterDevSpikes S614→S615 + `DevTestState.cs` HasSpike(S6-15) +1
> **JSON evidence**: `~/Library/Application Support/DefaultCompany/Unity/S6-15_Result.json` (timestamp: 2026-06-15 11:51:55 first-run PASS)

---

## §0 概要

S6-15 **GameApp provider injection (emergent fix Track F 第 3 story) 实施完成**。修复 S6-01 Phase 2.1 manual playtest 揭露的 **S0-4**：

- **S0-4**: `GameApp.Entrance()` 0 调 `InteractableObject.RegisterPuzzleConfigProvider` + `InteractionCoordinator.RegisterInputConfigProvider` — chapter 1 `InteractableObject.OnEnable` → `ResolvePuzzleConfig()` fail-loud `Log.Error` + drag 不可用；`InteractionCoordinator` fail-loud + InitWithDefaults fallback

**V3.0.1 dp15 第 3 个 production wiring 修复 case**：`rg 'RegisterPuzzleConfigProvider|RegisterInputConfigProvider' GameApp.cs` 从 0 hit → 各 ≥1 hit（与 story-004 GestureDispatcher production caller、story-005 scene MonoBehaviour 挂载互补）。

**ADR-029 R2 DEFICIENCY inline closed**：Luban `TbPuzzle` / `TbInputConfig` 0-production → `BuildFixturePuzzleConfigProvider` hardcoded + `InputConfigFromLuban.InitWithDefaults()`（S5-1b `BuildFixtureChapterDataProvider` 同模式；Sprint 7+ epic boundary 不变）。

R3 PlayMode **6/6 case first-run PASS**（baseline → P1 → P2 → P3 → P4 → P5 串行；chapter 1 baseline `OnRequestSceneChange(1)` + `WaitForIdleAsync` per S6-04/S6-14 precedent）：

| # | Case | 描述 | 状态 | asserts |
|---|------|------|------|---------|
| baseline | Chapter1Loaded | `OnRequestSceneChange(1)` → state=Idle, currentChapterId=1 | ✅ PASS (1/1) | 1 |
| P1 | StaticPuzzleConfigProviderRegistered | reflection `_puzzleConfigProvider` + invoke(1) 非 null | ✅ PASS (3/3) | 3 |
| P2 | StaticInputConfigProviderRegistered | reflection `_inputConfigProvider` + invoke() 非 null | ✅ PASS (3/3) | 3 |
| P3 | InteractableObjectPuzzleConfigResolved | 2× IO `_puzzleConfig` Id==1 bounds -10..10 | ✅ PASS (4/4) | 4 |
| P4 | CoordinatorInputConfigResolved | `IsLocked==false` + `_inputConfig` + FatFingerMarginMm==8 | ✅ PASS (4/4) | 4 |
| P5 | NoFailLoudProviderErrors | 0 provider 未注册 Log.Error + UnexpectedErrorCount==0 | ✅ PASS (4/4) | 4 |

**Total: 6/6 case PASS / 19/19 asserts / `all_passed=true` / `unexpected_error_count=0` / `fail_loud_provider_error_count=0` / `total_elapsed_ms=234` ≪ 5s budget**。

**Boot 路径**：`Assets/Scenes/main.unity` → GameApp.Entrance（provider 注册在 `InputService.Init()` 后、`StartGameLogic()` 前）→ RegisterDevSpikes(S615) → DevTestState `[main-menu]` mode → spike `RunAllAsync` 自驱 chapter 1 加载（**不需**手动点 NewGame）。

**与 S6-14 对比**：S6-14 allowlist 含 `PuzzleConfigProvider` 相关 fail-loud 字符串（provider 未注册时预期）；S6-15 **不** allowlist 这些错误 — P5 实证 0 条 provider 未注册 Log.Error。

---

## §1 R3 6 Case Detail

### §1.1 baseline — Chapter 1 加载

**Setup**: `main.unity` Play → DevTestState `[main-menu]` → S615Runtime.Start → `RunAllAsync`

**Action**:
1. reflection 拿 `GameApp._sceneManager`
2. `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)`
3. `WaitForIdleAsync(sm, timeoutSec: 15)` until `SceneManagerState.Idle`

**Result**: `state=Idle`, `currentChapterId=1` ✅

**Assert**: `baseline.chapter1_loaded` PASS

---

### §1.2 P1 StaticPuzzleConfigProviderRegistered

**Action**: reflection `InteractableObject._puzzleConfigProvider` static field + `provider(1)` invoke

**Result**:
- provider != null ✅
- invoke 返非 null `PuzzleConfig` ✅
- `Id == 1` ✅

**Asserts** (3/3 PASS): `P1.provider_non_null` / `P1.invoke_returns_non_null` / `P1.invoke_id`

---

### §1.3 P2 StaticInputConfigProviderRegistered

**Action**: reflection `InteractionCoordinator._inputConfigProvider` + `provider()` invoke

**Result**:
- provider != null ✅
- invoke 返非 null `IInputConfig` ✅
- 类型 `InputConfigFromLuban` ✅

**Asserts** (3/3 PASS): `P2.provider_non_null` / `P2.invoke_returns_non_null` / `P2.invoke_type`

---

### §1.4 P3 InteractableObjectPuzzleConfigResolved

**Action**: chapter 1 加载后 `FindObjectsOfType<InteractableObject>()` + reflection `_puzzleConfig`

**Result** (4/4 PASS):
- count == 2 ✅
- 每实例 `_puzzleConfig != null` ✅
- 每实例 `PuzzleConfig.Id == 1` ✅
- `InteractionBounds` MinX=-10, MaxX=10 (fixture) ✅

**P3 insight**: story-005 挂载 InteractableObject 后，本 story provider 注册使 `OnEnable` → `ResolvePuzzleConfig()` 成功 — S0-4 closure 实证。

---

### §1.5 P4 CoordinatorInputConfigResolved

**Action**: `FindObjectOfType<InteractionCoordinator>()` + reflection `_inputConfig` + `IsLocked` + `FatFingerMarginMm`

**Result** (4/4 PASS):
- coordinator 存在 ✅
- `IsLocked == false` ✅
- `_inputConfig != null` ✅
- `FatFingerMarginMm == 8` (InitWithDefaults 实证) ✅

---

### §1.6 P5 NoFailLoudProviderErrors

**Action**: `Application.logMessageReceived` spy 全程 boot → chapter 1 load

**Result** (4/4 PASS):
- 0 Log.Error 含 `PuzzleConfigProvider 未注册` ✅
- 0 Log.Error 含 `InputConfigProvider 未注册` ✅
- `UnexpectedErrorCount == 0` ✅
- `FailLoudProviderErrorCount == 0` ✅

---

## §2 R2 8/8 Closure 表

| # | Assumption | Phase 0 R2 | R3 PlayMode closure |
|---|------------|------------|---------------------|
| R2.1 | `RegisterPuzzleConfigProvider` API | ✅ `InteractableObject.cs:66` | ✅ P1 3 asserts |
| R2.2 | `RegisterInputConfigProvider` API | ✅ `InteractionCoordinator.cs:65` | ✅ P2 3 asserts |
| R2.3 | `PuzzleConfig` ctor（无 Default 静态字段） | ✅ DEFICIENCY amend | ✅ P1/P3 fixture resolved |
| R2.4 | `InputConfigFromLuban.InitWithDefaults()` | ✅ 无 DefaultInputConfig | ✅ P2/P4 |
| R2.5 | Luban TbPuzzle 0-production | ⚠️ DEFICIENCY → fixture | ✅ `BuildFixturePuzzleConfigProvider` production path |
| R2.6 | Luban TbInputConfig 0-production | ⚠️ DEFICIENCY → fixture | ✅ `BuildFixtureInputConfigProvider` production path |
| R2.7 | `GameApp.Entrance` boot 顺序 | ✅ provider 在 StartGameLogic 前 | ✅ Read `GameApp.cs:65-76` + P3/P4 chapter load 后 resolved |
| R2.8 | `Clear*ProviderForTest` helpers | ✅ EditMode 先例 | N/A (本 spike 验 production Register 路径) |

**R2 verdict**: ✅ **8/8 FULLY PASS** (R2.5/R2.6 DEFICIENCY inline closed via fixture — ADR-029 DEFICIENCY-FLAGGED PASS path closure)

---

## §3 AC 10/10 Verify

| AC | 描述 | Verify | Verdict |
|----|------|--------|---------|
| AC-1 | `RegisterPuzzleConfigProvider` 1 次 | rg `GameApp.cs` | ✅ PASS |
| AC-2 | `RegisterInputConfigProvider` 1 次 | rg `GameApp.cs` | ✅ PASS |
| AC-3 | 注入位置 InputService.Init 后、StartGameLogic 前 | Read `GameApp.cs:61-76` | ✅ PASS |
| AC-4 | `BuildFixturePuzzleConfigProvider` puzzleId=1 非 null | Read + P1/P3 | ✅ PASS |
| AC-5 | `BuildFixtureInputConfigProvider` InitWithDefaults | Read + P2/P4 | ✅ PASS |
| AC-6 | hardcoded fixture 非 PuzzleConfig.Default | R2.3 + Read | ✅ PASS |
| AC-7 | InputConfigFromLuban 非 DefaultInputConfig | R2.4 + Read | ✅ PASS |
| AC-8 | `_puzzleConfig != null` + 0 fail-loud puzzle provider | R3 P3 + P5 | ✅ PASS |
| AC-9 | `IsLocked==false` + InputConfig 注入 + 0 fail-loud input provider | R3 P4 + P5 | ✅ PASS |
| AC-10 | 0 unexpected console error | R3 P5 `unexpected_error_count=0` | ✅ PASS |

**AC verdict**: ✅ **10/10 PASS**

---

## §4 V3.0.1 Watch List Hooks

| dp | 本 story 角色 | closure 状态 |
|----|-------------|-------------|
| **dp15** EditMode green ≠ production wired sniff | 第 3 个 production wiring 修复 case (Register provider GameApp caller) | ⏳ 待 story-008 final pilot 终极 confirmation |
| **Type-2 (a)** Luban stub fallback drift | TbPuzzle/TbInput 0-production → fixture（Track F 累计 ≥3） | ⏳ Sprint 6 retro 评估 V3 trigger |
| **dp8** DevTestState [main-menu] mode 阈值 | HasSpike list 8 spike | ⏳ Sprint 6 retro 议题 1 V3.1 trigger 评估 |

---

## §5 Sprint 6 Track F Insight

- **Track F 进度**: 3/5 done (story-004 ✅ + story-005 ✅ + **story-006 ✅**) → epic 累计 **8/10 stories done**
- **NEXT**: story-007 ShadowMatch production wire (~2-3 hr scope 风险最高 — R2.1 verify 关键)
- **S0-4 closure**: InteractableObject drag 前置 `_puzzleConfig` resolved + Coordinator `_inputConfig` 注入成功 — manual playtest 物件交互 blocker 减 1
- **governance**: 估时 ~30 min 估中；R3 first-run PASS 无 SuspendTick 类 fix（与 S6-13 对比 — 本 spike 不注入 InputService Tick）

---

## §6 Files Changed

| # | 文件 | 变更 |
|---|------|------|
| 1 | `GameApp.cs` | 2× Register + 2× `BuildFixture*` helper + RegisterDevSpikes S614→S615 |
| 2 | `S6-15_GameAppProviderInjection.cs` | NEW ~525 行 spike |
| 3 | `DevTestState.cs` | HasSpike(S6-15) +1 (7→8) |
| 4 | `story-006-gameapp-provider-injection.md` | Status / History Phase 5 closure |
| 5 | `production/qa/playmode-gameapp-provider-injection-2026-06-12.md` | NEW 本 evidence doc |
| 6 | `EPIC.md` + `sprint-status.yaml` | Phase 5 closure sync |

**0 scene diff / 0 ProjectSettings diff**（per story spec）。

---

## §7 References

- `production/epics/vs-chapter-1/story-006-gameapp-provider-injection.md`
- `production/qa/playmode-chapter-1-scene-wiring-2026-05-15.md` (S6-14 evidence — P2 note story-006 provider deferred 现已 closure)
- `production/qa/playmode-input-pipeline-wiring-2026-05-14.md` (S6-13 evidence structure precedent)
- `Assets/Tests/EditMode/ObjectInteraction/InteractionCoordinatorTests.cs:58-70` (fixture 先例)
- `Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractableObject.cs:496-506` (fail-loud ResolvePuzzleConfig)

---

## §8 Verdict

✅ **PASS** — S6-15 / story-006 gameapp-provider-injection **DONE**

- R3: **6/6 case PASS / 19/19 asserts / first-run / 234ms / 0 unexpected error / 0 fail-loud provider error**
- AC: **10/10 PASS**
- R2: **8/8 PASS** (DEFICIENCY inline closed)
- Production logic: **~30 行** `GameApp.cs` provider wiring
- **Track F**: 3/5 stories done → **NEXT story-007**

> ⚠️ AI 生成，待人工审核
