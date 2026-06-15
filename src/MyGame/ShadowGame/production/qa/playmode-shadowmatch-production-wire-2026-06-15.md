// 该文件由Cursor 自动生成

# S6-16 R3 PlayMode Evidence — ShadowMatch Production Wire (2026-06-15)

> **Story**: S6-16 — ShadowMatch Production Wire (Track F vs-chapter-1-007 第 4 story — S0-5 ShadowMatchCalculator 0-production)
> **Verdict**: ✅ **PASS** (6/6 R3 case + 15/15 asserts + `all_passed=true` + `unexpected_error_count=0` + `total_elapsed_ms=2970` ≪ 9s budget)
> **JSON**: `~/Library/Application Support/DefaultCompany/Unity/S6-16_Result.json` (2026-06-15 11:59:55)

## §0 概要

**R2.1 实证：场景 C** — `ShadowMatchCalculator.cs` 0-production → 本 story inline MVP impl（fixture pose-distance scoring；ADR-012 完整 AsyncGPUReadback deferred shadow-puzzle epic）。

**S0-5 closure**：`GameApp.Entrance` 注册 `ShadowMatchCalculator` → 订阅 `IInteractionEvent.OnObjectTransformChanged` → epsilon-gated `IShadowMatchEvent.OnMatchScoreUpdated` → `PuzzleStateMachine` → `OnPerfectMatch` → `NarrativeSequencePlayer`。

| Case | 结果 | 要点 |
|------|------|------|
| baseline | ✅ | chapter 1 loaded |
| P1 | ✅ | calculator Init + listener subscribed |
| P2 | ✅ | 无 mock OnMatchScoreUpdated bypass |
| P3 | ✅ | 5 次 score update；final score 1.000 |
| P4 | ✅ | OnPerfectMatch exactly 1；state Complete（PerfectMatch 后合法 cascade） |
| P5 | ✅ | sequence start + ducking；MusicVolume 0.300 |

## §6 Files Changed

| 文件 | 变更 |
|------|------|
| `ShadowPuzzle/ShadowMatchCalculator.cs` | NEW MVP calculator + listener |
| `GameApp.cs` | Init/Dispose + S616 spike 切换 |
| `S6-16_ShadowMatchProductionWire.cs` | NEW R3 spike |
| `DevTestState.cs` | HasSpike(S6-16) |

> ⚠️ AI 生成，待人工审核
