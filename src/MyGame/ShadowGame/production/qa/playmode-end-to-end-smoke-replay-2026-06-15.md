# PlayMode R3 Evidence — S6-17 End-to-End Smoke Replay (Track F vs-chapter-1-008)

> **Story**: `production/epics/vs-chapter-1/story-008-end-to-end-smoke-replay.md`
> **Spike**: `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-17_EndToEndSmokeReplay.cs`
> **Run date**: 2026-06-15 12:28:59
> **JSON**: `~/Library/Application Support/DefaultCompany/Unity/S6-17_Result.json`

## Verdict

| Metric | Result |
|--------|--------|
| Overall | **All Passed** |
| Cases | **6/6** (P1–P5 + baseline tick suspend) |
| Asserts | **24/24 PASS** |
| `unexpectedErrorCount` | **0** |
| Total elapsed | **12,733 ms** |
| dp15 sniff | **PASS** — 0 mock `GameEvent.Get<I*Event>().*` fire |

## Case summary

| Case | Result | Notes |
|------|--------|-------|
| P1 InputSimulationMouseTapNewGame | PASS | `NewGameButton.onClick.Invoke()` → chapter 1 Idle |
| P2 InputSimulationObjectInteractionToShadowMatch | PASS | Tap+Drag via `TickForTest` → score history `[0.50, 0.75, 1.00]` |
| P3 PuzzleStateMachineToNarrative | PASS | `OnPerfectMatch` exactly 1; state `Complete` |
| P4 NarrativeSequenceWithAudioDuck | PASS | `MusicVolume≈0.300`; sequence + ducking fired |
| P5 dp15SniffSubClauseVerify | PASS | production input + shadow match wired; gesture path only |

## Production path verified (no mock fire)

```
NewGameButton.onClick
  → ISceneEvent.OnRequestSceneChange(1)
  → chapter 1 loaded

InputService.TickForTest (TouchState inject)
  → SingleFingerFSM → GestureDispatcher.Dispatch
  → IGestureEvent.OnTap / OnDrag
  → InteractionCoordinator raycast + InteractableObject drag/snap
  → IInteractionEvent.OnObjectTransformChanged
  → ShadowMatchCalculator → IShadowMatchEvent.OnMatchScoreUpdated
  → PuzzleStateMachine → OnPerfectMatch
  → NarrativeSequencePlayer → ducking + sequence
```

## Fixture alignment note (story-007 → story-008)

R3 实证：`gridSize=1` snap 落点为整数格 `(±1, 0)`，原 fixture `y=0.5` 不可达致 `max score≈0.75`。
`ShadowMatchCalculator` chapter 1 targets 已对齐为 `(-1,0,0)` / `(1,0,0)`（与 snap 网格一致）。

## AC-8 timing

Round-trip **12.7s**（含 narrative 2.7s wait）> story AC-8 草案 5s，但 < S5-02 历史 10s 上限；留 Sprint 6 retro 评估是否 amend AC-8 预算。
