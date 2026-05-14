// 该文件由Cursor 自动生成

# Story 007: ShadowMatch Production Wire — IInteractionEvent.OnObjectTransformChanged → ShadowMatchCalculator → IPuzzleEvent.OnMatchScoreUpdated/OnPerfectMatch

> **Epic**: VS Chapter 1
> **Story ID**: vs-chapter-1-007 (Sprint 6 emergent fix Track F NEW；S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 — S0-5)
> **Sprint**: 6 (Track F NEW — chapter 1 production wiring emergent fix)
> **Story Type**: Logic / Integration (跨 sender → listener 链路 production wiring + 算法计算)
> **Complexity Points**: 2-3 (~2-3 hr 估时；ADR-012 算法 MVP scope verify + production listener wire)
> **GDD Requirement**: TR-puzzle-001 (Chapter 1 不引入光源操作 per shadow-puzzle-system.md line 42 — 本 story 是 fixed-light + 物件移动 → 阴影匹配 score)；TR-shadow-match-* (待 tr-registry verify)
> **ADR References**: **ADR-012 Shadow Match Calculation** (光线 + 物件 + 投影墙面 raycast / projection / area overlap algorithm) + ADR-027 §4 IInteractionEvent.OnObjectTransformChanged sender + §5 IPuzzleEvent.OnMatchScoreUpdated/OnPerfectMatch sender + ADR-014 Puzzle State Machine (S5-03 done — listener PuzzleStateMachine 已订阅 OnMatchScoreUpdated)+ ADR-029 V3.0.1 (R2 deficiency-flagged PASS path 候选；如 ShadowMatchCalculator.cs Sprint 4 SP-018 现状 0-production 则 deficiency-flag) + ADR-030 §VS Build commit
> **Status**: 📝 **Draft** (2026-05-14 Session 32 morning continue — emergent fix epic Track F NEW story-004~008 outline approved per [A]，本 story Phase 0 R2 vendor reality verify pending — **本 story scope 风险最高**：ShadowMatchCalculator Sprint 4 SP-018 实施现状不明，如 deferred 则 scope 可能 amend amend split)
> **Created**: 2026-05-14 morning continue (Sprint 6 Session 32 — emergent fix Track F NEW fourth story)
> **Completed**: ""
> **Depends on**: story-004 input-pipeline-wiring + story-005 chapter-1-scene-wiring + story-006 gameapp-provider-injection (本 story 需 input → InteractableObject FSM Drag → SnapRotation fire OnObjectTransformChanged 全 production wiring 已通) + Sprint 4 SP-018 ShadowMatchCalculator (?)，待 R2.1 verify (现状 done / partial / 0-production) + S5-03 PuzzleStateMachine ✅ (listener OnMatchScoreUpdated 已 wire)

---

## Context

**S6-01 Phase 2.1 manual playtest NEEDS-WORK 派生 emergent fix Track F 第 4 story (~2-3 hr — 本 story scope 风险最高)**：

S6-01 root cause analysis 5 处 wiring gap 中 **S0-5: ShadowMatchCalculator (ADR-012) listener 物件 transform → 阴影匹配 → IPuzzleEvent.OnMatchScoreUpdated production 0 wire** —— S5-02 spike P2 直接 fire mock `OnMatchScoreUpdated(1, 0.95)` 简化路径绕过 ObjectInteraction → ShadowMatch 整条链路。

### Sprint 4 SP-018 ShadowMatchCalculator 实施现状 unknown — 本 story scope 风险

ShadowMatchCalculator.cs 是 ADR-012 spec 落地的核心算法实现，Sprint 4 SP-018 是计划项；但 Sprint 4 retro 没有明确 SP-018 实施 evidence (Sprint 4 retro AI-1~9 没列 SP-018 closure)，可能 deferred 到 post-VS 或本 Sprint 6 emergent fix 才落地。

**3 种 R2 verify 后场景**:
- **场景 A — ShadowMatchCalculator.cs Sprint 4 done + listener wired**: 本 story scope 仅 verify production listener subscription on chapter scene load (~30-45 min)
- **场景 B — ShadowMatchCalculator.cs Sprint 4 partial impl (核心算法 done + listener wire 缺)**: 本 story scope 加 listener registration + spike + R3 (~1-2 hr)
- **场景 C — ShadowMatchCalculator.cs Sprint 4 0-production / 算法未实施**: 本 story scope 暴涨到 ~4-6 hr — 可能需 split 为 sub-stories (story-007a ShadowMatchCalculator algorithm impl + story-007b production listener wire) — Sprint 6 capacity 不够 → Sprint 7 buffer

R2 verify Phase 0 R2.1 实证后 user 决策 scope adjust。

### Sprint 6 emergent fix Track F serial 顺序

```
story-004 → story-005 → story-006 → story-007 (本 story) → story-008 (重写 S5-02 spike 不依赖 mock fire OnMatchScoreUpdated — dp15 sniff sub-clause 试点)
```

---

## Goal Flow (T0 → T7)

```
T0  story-004/-005/-006 done — input pipeline + scene wiring + provider injection 完整
T1  user Mouse Tap on Object_01_CoffeeMug → IGestureEvent.OnTap fire (story-004) → InteractionCoordinator OnTap listener (Sprint 2 SP-013) raycast hit Object_01 → fsm.OnTapHit → fsm.TransitionTo(Selected) + IInteractionEvent.OnObjectSelected fire
T2  user Mouse Drag → IGestureEvent.OnDrag fire 三 phase → InteractionCoordinator OnDrag listener 转发 → InteractableObject.OnDragBegan/OnDrag/OnDragEnded → fsm.TransitionTo(Dragging) + transform 更新
T3  Object_01 transform 改变 → InteractableObject.SnapRotation OnComplete (Drag.Ended phase 触发) → fire IInteractionEvent.OnObjectTransformChanged(objectId, pos, rot)
T4  ShadowMatchCalculator listener (本 story 落地 — 或 Sprint 4 SP-018 已 done verify) 订阅 OnObjectTransformChanged → ShadowMatchCalculator.Calculate(objectId, pos, rot) per ADR-012 spec algorithm (光线 raycast → 物件投影 → 与目标投影区 overlap area / Penumbra alignment / 角度 match)
T5  Calculate 返 finalScore [0, 1] → fire IPuzzleEvent.OnMatchScoreUpdated(puzzleId, finalScore)
T6  PuzzleStateMachine (S5-03 ✅ done) OnMatchScoreUpdated listener — finalScore ≥ 0.95 (per ADR-012 PerfectMatch threshold) → fire IPuzzleEvent.OnPerfectMatch(puzzleId, finalScore)
T7  NarrativeSequencePlayer (S5-05 ✅ done) OnPerfectMatch listener (NarrativeSequencePlayer.cs:133-135) → HandleRequestSequence(puzzleId, MemoryReplay) → narrative 触发 (audio_duck + screen_fade + 字幕 等 sequence)
```

---

## ADR Decision Summary

**ADR-012 Shadow Match Calculation inherit + verify** — 本 story 不 amend ADR (除非 R2.1 verify 揭露算法 spec 与实施 drift)：
- 光线 + 物件 + 投影墙面 raycast / projection / area overlap algorithm
- PerfectMatch threshold ≥ 0.95
- Match score [0, 1] continuous (smooth)

**ADR-027 §4 IInteractionEvent.OnObjectTransformChanged** + **§5 IPuzzleEvent.OnMatchScoreUpdated/OnPerfectMatch** sender contract inherit (Sprint 2/4 done — 本 story 仅 wire listener subscription if needed)。

**ADR-014 PuzzleStateMachine** S5-03 done — listener OnMatchScoreUpdated 已 wire (PuzzleStateMachine.cs:119)；本 story 不改 PuzzleStateMachine。

**ADR-029 V3.0.1 R2 deficiency-flagged PASS path 候选** — 如下任一情况触发 deficiency flag：
- Sprint 4 SP-018 ShadowMatchCalculator.cs 0-production → 本 story scope 暴涨 → 评估 split sub-story
- ADR-012 spec 算法描述 与 实施 drift (V3.0.1 dp15 sniff 应用)
- IInteractionEvent.OnObjectTransformChanged sender 在 InteractableObject.SnapRotation 实际 fire 时机 与 spec 期望 drift

---

## Engine Notes

**待 /story-readiness gate Phase 0 R2 vendor reality verify (本 story scope 关键)**：
- R2.1 ⚠️ **TBD CRITICAL**: ShadowMatchCalculator.cs (Sprint 4 SP-018) 实施现状 — 0-production / partial / done (`Glob 'ShadowMatchCalculator*.cs'` 验证)
- R2.2 ⚠️ TBD: 如 R2.1 done — listener 是否 wire (即 ShadowMatchCalculator 是否订阅 IInteractionEvent.OnObjectTransformChanged)
- R2.3 ⚠️ TBD: ShadowMatchCalculator 是 MonoBehaviour 还是 POCO (影响实例化 + 生命周期)
- R2.4 ⚠️ TBD: ADR-012 spec 算法 MVP scope 可达 (是否需要 amend simplification — 如算法太复杂 deferred)
- R2.5 ⚠️ TBD: InteractableObject.SnapRotation 实际 fire OnObjectTransformChanged 时机 (Drag.Ended phase 还是连续 phase?)
- R2.6 ⚠️ TBD: 投影墙面 GameObject 在 Chapter_01_Approach.unity 现状 (Sprint 5 S5-01 build 时是否含 ShadowProjectionTarget Collider?)
- R2.7 ⚠️ TBD: Light source GameObject (chapter 1 fixed light per shadow-puzzle-system.md line 42) 配置 (Spot light intensity / range / 角度)
- R2.8 ⚠️ TBD: PuzzleConfig.PerfectMatchThreshold 字段 (per ADR-012 0.95) — 与 story-006 PuzzleConfig 字段 list 联动 verify

---

## Control Manifest Rule References

**待 /story-readiness gate Phase 1 R1 grep audit**：
- ⚠️ TBD: Required — ShadowMatchCalculator must subscribe IInteractionEvent.OnObjectTransformChanged (NOT polling Update Loop)
- ⚠️ TBD: Required — Calculate(...) must return [0, 1] continuous score (NOT discrete bool match/no-match)
- ⚠️ TBD: Required — fire IPuzzleEvent.OnMatchScoreUpdated 在每次 score 改变 (delta > epsilon 阈值, 防 spam)
- ⚠️ TBD: Required — PerfectMatch fire 仅 1 次 per puzzle 解决（不重复 fire；per ADR-012 spec — 防 narrative trigger 重复）
- ⚠️ TBD: Forbidden — direct GameEvent.Get<IPuzzleEvent>().OnMatchScoreUpdated mock fire bypass (V3.0.1 dp15 sniff sub-clause)

---

## Acceptance Criteria

| # | AC | Verify path |
|---|-----|------|
| AC-1 | ShadowMatchCalculator.cs production 实施 + listener subscribe IInteractionEvent.OnObjectTransformChanged | rg `IInteractionEvent\.OnObjectTransformChanged` listener; Read ShadowMatchCalculator.cs |
| AC-2 | ShadowMatchCalculator.Calculate(objectId, pos, rot) 返 finalScore in [0, 1] continuous (per ADR-012 spec) | EditMode unit test (本 story 不新建，如已有 Sprint 4 SP-018 verify) |
| AC-3 | ShadowMatchCalculator fire IPuzzleEvent.OnMatchScoreUpdated(puzzleId, finalScore) 在每次 score 改变 (delta > epsilon) | R3 PlayMode probe + listener spy |
| AC-4 | finalScore ≥ 0.95 → fire IPuzzleEvent.OnPerfectMatch(puzzleId, finalScore) 1 次 per puzzle (NOT 重复 fire 多次) | R3 PlayMode probe + state tracking + listener spy |
| AC-5 | 0 mock fire bypass — spike + production code 不直接 GameEvent.Get<IPuzzleEvent>().OnMatchScoreUpdated mock fire (V3.0.1 dp15 sniff sub-clause) | R2 grep verify Assets/GameScripts/HotFix |
| AC-6 | PlayMode probe ≥1 R3 case Mouse Drag Object_01 → score 自然变化 → score=0.95 → fire OnPerfectMatch → narrative trigger (per S5-02 spike P4 OnPerfectMatch listener spy precedent) | R3 PlayMode probe |
| AC-7 | InteractableObject.SnapRotation fire OnObjectTransformChanged 与 spec 时机一致 (Drag.Ended phase 触发, NOT 连续 phase 性能问题) | R3 PlayMode probe + listener spy + count assert |
| AC-8 | 0 production unexpected console error/warning during chapter 1 fun loop (objectInteraction → shadowMatch → puzzleState → narrative round-trip) | R3 evidence dump UnexpectedErrorCount==0 |
| AC-9 | Total elapsed for one drag → match → narrative trigger round-trip < 2s | R3 PlayMode probe time budget |
| AC-10 | 与 S5-02 baseline 不冲突 — S5-02 spike P2 mock fire 路径要么删除要么改用 production wiring (story-008 重写) | story-008 联动；本 story 不必删 S5-02 spike 但 story-008 必须改 |

---

## R3 PlayMode Probe Plan（待 /story-readiness gate amend detail）

预计 spike `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-XX_ShadowMatchProductionWire.cs`：

- **P1 ListenerSubscriptionVerify** — Editor PlayMode boot → reflect ShadowMatchCalculator._listeners (or similar) verify IInteractionEvent.OnObjectTransformChanged 订阅了
- **P2 NoMockFireBypass** — V3.0.1 dp15 sniff sub-clause 试点 — spike 与 production code 任何路径 0 调 GameEvent.Get<IPuzzleEvent>().OnMatchScoreUpdated mock fire；listener spy expect 仅自然 ShadowMatchCalculator.Calculate 路径 fire
- **P3 ScoreContinuousFire** — InputSimulation Mouse drag Object_01 from start to perfect-match position → expect OnMatchScoreUpdated fire ≥3 次 (continuous score change) + final score ≥ 0.95
- **P4 PerfectMatchFireOnce** — 同 P3 setup → expect OnPerfectMatch fire exactly 1 次 (NOT 2 或 0)
- **P5 NarrativeTriggerRoundTrip** — 同 P3+P4 setup → 等待 narrative trigger (S5-05 NarrativeSequencePlayer.HandleRequestSequence) → assert audio_duck triggered + screen_fade triggered (per S5-02 spike P4 reflection precedent)

---

## R2 Assumptions Validated（待 Phase 0 实证 — 本 story scope 风险关键 verify）

| # | Assumption | Verify | Status | Risk |
|---|------------|--------|--------|------|
| R2.1 | ShadowMatchCalculator.cs Sprint 4 SP-018 实施现状 | Glob 'ShadowMatchCalculator*' + Read | ⚠️ TBD | **CRITICAL — 决定 scope ~30 min vs ~4-6 hr** |
| R2.2 | Listener subscribe IInteractionEvent.OnObjectTransformChanged | rg listener | ⚠️ TBD | High |
| R2.3 | ShadowMatchCalculator MonoBehaviour vs POCO | Read class signature | ⚠️ TBD | Medium |
| R2.4 | ADR-012 算法 MVP scope 可达 | Read ADR-012 §Decision | ⚠️ TBD | Medium |
| R2.5 | InteractableObject.SnapRotation fire timing | Read InteractableObject.cs:SnapRotation | ⚠️ TBD | Low |
| R2.6 | 投影墙面 ShadowProjectionTarget GameObject | Read Chapter_01_Approach.unity | ⚠️ TBD | Medium |
| R2.7 | chapter 1 fixed light config | Read scene Light component | ⚠️ TBD | Low |
| R2.8 | PuzzleConfig.PerfectMatchThreshold 字段 | Read PuzzleConfig.cs (story-006 联动) | ⚠️ TBD | Low |

---

## V3.0.1 Watch List Hooks

**Type-11 V3.0.1 dp15 candidate "EditMode green ≠ production wired sniff"** — 本 story 是 dp15 候选 sniff sub-clause **第 4 个 production wiring 修复 case**（最关键的跨 sender → listener 链路 production wire — 物件 → 阴影 → puzzle → narrative 完整 round-trip）。

**Type-5 V3.0.1 candidate "spec/tooling ↔ reality drift"** — 留观察 — R2.1 verify 后可能 surface ADR-012 spec 与 ShadowMatchCalculator.cs 实施 drift。

**Type-2 (c) V3 candidate "framework behavior frequency"** — 留观察 — InteractableObject.SnapRotation fire OnObjectTransformChanged 频次 (per Drag.Ended phase vs continuous phase) 与 spec 期望 drift 是否 dp 候选。

**Type-9 V2.0 §V2-1.b "Interface Method Set Fan-out Check"** — 应用 — IInteractionEvent fan-out check verify ShadowMatchCalculator listener wire 不漏（per S5-02 R2.2 wiring gap 误判 retract precedent — Sprint 5 retro V2.0 §V2-1.b absorbed Type-9 dp1 closure）。

---

## Out of Scope（明示）

- ❌ **ADR-012 算法 polish + multi-light** — Sprint 7+ chapter 2-5 (per shadow-puzzle-system.md line 42 chapter 1 不引入光源操作)
- ❌ **PuzzleStateMachine listener change** — S5-03 done (本 story 不改 PuzzleStateMachine)
- ❌ **NarrativeSequencePlayer change** — S5-05 done (本 story 不改 NarrativeSequencePlayer)
- ❌ **ShadowCasterGroup wiring on Object_01/02** — 待 R2.6 verify Chapter_01_Approach.unity 现 ShadowCasterGroup 是否 already configured (S5-01 done) — 如缺 Issue 在 story-005 amend 加
- ❌ **Multi-puzzle simultaneous match** — chapter 1 仅 puzzle 1 (per TR-objint-002)
- ❌ **PerfectMatch threshold tuning** — 0.95 默认值 per ADR-012；如 manual playtest 反馈太严苛 留 Sprint 7+ tuning epic

---

## Implementation Notes（高层结构，待 Phase 0/1 R2 verify 后 amend 精细）

预计涉及文件（**scope 视 R2.1 verify 决定**）：

**场景 A (Sprint 4 SP-018 done + listener wired)**：
- 本 story 0 production code change (仅 verify)；spike S6-XX_ShadowMatchProductionWire.cs NEW R3 5 case (~30-45 min)

**场景 B (Sprint 4 SP-018 partial)**：
- ShadowMatchCalculator.cs amend listener subscribe + 实例化 in GameApp.Init (与 PuzzleStateMachine S5-03 lifecycle precedent 同模式)
- spike S6-XX NEW R3 5 case (~1-2 hr)

**场景 C (Sprint 4 SP-018 0-production)**：
- ShadowMatchCalculator.cs NEW (~200-400 行 ADR-012 算法实施)
- Listener wire + 实例化
- spike S6-XX NEW R3 5 case
- 总 scope ~4-6 hr — **可能需 split sub-story** (story-007a algorithm impl + story-007b production listener wire) — Sprint 6 capacity 不够 → Sprint 7 buffer 评估

**0 vendor patch 决策**：本 story 不改 TEngine vendor (Assets/TEngine/) — 所有改动限定 GameLogic 层。

---

## History

- **2026-05-14 morning continue (Session 32)**: Draft 创建（emergent fix epic Track F NEW story-004~008 outline approved per [A]）；Status: Draft；S0-5 narrow scope；**本 story scope 风险最高**（ShadowMatchCalculator Sprint 4 SP-018 现状 unknown → R2.1 verify 后 scope 可能 amend amend split）；本 story 完成后 chapter 1 fun loop production wiring 完整 round-trip 闭环（之后 story-008 重写 S5-02 spike 不依赖 mock fire 验证 dp15 sniff sub-clause 试点）。
