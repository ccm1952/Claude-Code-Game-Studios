# 问题记录：ADR-029 V2.0 R2 grep 完备性 — interface 内 method 需实 grep 不 trust story wording (Type-9 meta-drift)

> **日期**: 2026-05-12
> **发生工程**: MyGameStudio / src/MyGame/ShadowGame (Sprint 5 S5-02 dev-story Phase 1.5 self-check)
> **发生次数**: 第 1 次（Session 27 #2 dev-story Phase 1.5 自检暴露 Session 27 #1 readiness gate R2.2 误判）
> **严重性**: 中（readiness gate R2 grep 不完备会误判 wiring gap → 实施环节多写无用 bridge → 实跑 R3 PlayMode 双重 trigger fail 才被发现）

---

## 问题现象

Sprint 5 S5-02 story-readiness gate Session 27 #1 R2.2 grep:

```bash
rg "AddEventListener.*OnPuzzleComplete" Assets/GameScripts/HotFix/
```

得 **0 hit**，因此判定 "PuzzleStateMachine.cs:284 派发 `OnPuzzleComplete` 后无 listener → chain 断裂 → 需新建 `PuzzleCompleteToNarrativeBridge.cs` 作 Required Framework Extension"。

实际 dev-story Phase 1.5 self-check 重查发现 `NarrativeSequencePlayer.cs:127-135` 实际订阅 **4 个 event**：

```csharp
GameEvent.AddEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnRequestSequence, _onRequestSequence);
GameEvent.AddEventListener<int, float>(IShadowPuzzleEvent_Event.OnPerfectMatch, _onPerfectMatch);   // ⭐
GameEvent.AddEventListener<int, float>(IShadowPuzzleEvent_Event.OnAbsenceAccepted, _onAbsenceAccepted);
GameEvent.AddEventListener<int>(IChapterStateEvent_Event.OnChapterComplete, _onChapterComplete);
```

`OnPerfectMatchHandler`（line 239-242）→ `HandleRequestSequence(puzzleId, MemoryReplay)` → **puzzle→narrative chain 直 wire**。

即 readiness gate 当时仅 grep `OnPuzzleComplete` listener，未 grep 同接口 (`IShadowPuzzleEvent`) 其他 method (`OnPerfectMatch` / `OnAbsenceAccepted`) listener，造成误判 wiring gap。

## 根因

ADR-029 V2.0 §V2-2 R2 grep 协议表述："对 story `## Implementation Notes` 中每个 cross-component API call 实 grep" — 隐含 trust story 原文 wording。

但 **wiring gap 的存在与否取决于"接口整体 method 集是否被监听"，而非"story 提到的某一具体 method 是否被监听"**。当 story 假设错位（e.g. story 假设 `OnPuzzleComplete` 是 narrative trigger 主路径，实际 vendor 用 `OnPerfectMatch`），单 method grep 会误判。

正确判定逻辑：

```
对每个怀疑 wiring gap 的 chain 链接点：
  1. read 接口文件 (e.g. IShadowPuzzleEvent.cs) → list 所有 method
  2. grep 每个 method 的 listener (rg "AddEventListener.*OnXxxMethod")
  3. 如有 ≥1 method 被监听 → 链路存在 (可能走不同 method)
  4. 全部 method 0-hit → 真 wiring gap
```

## 规则

### R2 grep 增强协议 (R2.0 协议补丁，提议沉淀 ADR-029 V2.0 §V2-2 子条款)

对怀疑 wiring gap 的每个 cross-component event chain：

1. **先 read 接口文件** (`Assets/GameScripts/HotFix/.../IXxxEvent.cs`)，list 所有 method (`OnA`, `OnB`, `OnC`, ...)
2. **逐 method grep listener**: `rg "AddEventListener.*OnA" Assets/GameScripts/HotFix/`, `rg "AddEventListener.*OnB" ...`, etc.
3. **如有任一 method ≥1 hit listener** → wiring chain 存在（可能走不同 method 而非 story 假设的 method） → 改 story wording 对齐实际路径
4. **全部 method 0-hit** → 真 wiring gap，按 ADR-029 V2.0 §V2-2 deficiency-flagged 协议处理

### 适用场景

- ADR contract method-level wiring (`IXxxEvent → IYyyEvent` chain 类)
- vendor framework lifecycle hook listener （vendor 改 method name 同时 spec 未同步时）
- cross-system listener / sender 角色错位（如 sender vs listener 混淆）

## Agent 自检 checklist

R2 readiness gate 时：

- [ ] 对每个 cross-component event chain，先 read 接口文件而非 trust story wording
- [ ] grep listener 时遍历该接口全部 method（不止 story 提到的那 1 个）
- [ ] 如同接口其他 method 有 listener，重新评估 chain wiring，可能 story 假设错位
- [ ] readiness gate verdict 中"wiring gap"判定明示其 grep 覆盖范围（"grep `Onmethod1` 0-hit `OnMethod2` 0-hit ... 全 0-hit" 才可判 wiring gap）

dev-story Phase 1.5 时：

- [ ] 重新 grep R2 readiness gate 提出的 wiring gap，验证是否仍然 0-hit 全部 method
- [ ] 如发现 wiring gap 误判 → AskQuestion 用户撤回 + amend story
- [ ] sprint-status.yaml watch_list 添加 Type-9 'R2 grep 完备性' meta-drift dp

## 与 ADR-029 V2.0 §V2-2 协议关系

ADR-029 V2.0 §V2-2 R2 当前文本（如适用 sprint-5）：

> "对 story `## Implementation Notes` 中每个 cross-component API call，rg 验证该 API 在 production code 中存在。≥1 hit per called API 为通过。"

提议增补子条款：

> "对怀疑 wiring gap 的 chain 链接点（即 cross-system event listener 路径），先 read 接口文件 list 所有 method，逐 method grep listener；任一 method ≥1 hit → wiring 存在，story wording 需对齐实际路径；全部 method 0-hit → 真 wiring gap，按 deficiency-flagged 协议处理。"

Sprint 5 retro 评估 promote 为 ADR-029 V2.1 正式条款。

## 历史记录

- 2026-05-12 创建。触发场景：MyGameStudio Sprint 5 S5-02 dev-story Phase 1.5 self-check 重查 Session 27 #1 readiness gate R2.2 wiring gap 判定。
  - Session 27 #1 R2.2 grep 仅 `rg "AddEventListener.*OnPuzzleComplete"` → 0 hit → 判定 wiring gap → story-002 amend `**Required Framework Extension**` flag (建 `PuzzleCompleteToNarrativeBridge.cs`)
  - Session 27 #2 dev-story Phase 1.5 重 read `NarrativeSequencePlayer.cs:127-135` 发现 `OnPerfectMatch` listener 已 wire puzzle→narrative chain
  - 撤回 wiring gap 误判 + amend story-002 R2.2 → ✅ PASS + R2 综合 verdict DEFICIENCY-FLAGGED PASS → ✅ PASS
  - 撤回 V3 Type-2(c) dp1
  - 新沉淀 V3 Type-9 dp1 'R2 grep 完备性' meta-drift（第 1 次见，留观察 1-2 story 后 promote）
  - 节省 Phase 2 实施 ~30 min（bridge）+ Phase 3 P4 实跑双重 trigger fail 排查 ~30 min ≈ 60 min total

## 关联文档

- [`docs/architecture/adr-029-story-impl-notes-verification.md`](../../docs/architecture/adr-029-story-impl-notes-verification.md) §V2-2 R2 协议
- [`src/MyGame/ShadowGame/production/epics/vs-chapter-1/story-002-end-to-end-flow.md`](../../src/MyGame/ShadowGame/production/epics/vs-chapter-1/story-002-end-to-end-flow.md) History Session 27 #2 entry
- 历史 R2 readiness gate 关联 lessons memo: [`problem_2026-04-29_story-impl-notes-vs-framework-drift.md`](problem_2026-04-29_story-impl-notes-vs-framework-drift.md)
