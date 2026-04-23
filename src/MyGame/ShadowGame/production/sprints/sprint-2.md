// 该文件由Cursor 自动生成

# Sprint 2 — Core Layer (Chapter × Scene × Object Interaction)

> **Sprint N**: 2
> **Phase**: Pre-Production
> **Start**: 2026-04-23
> **End (expected)**: TBD（AI 协作模式下以 story 完成为准）
> **Review Mode**: solo
> **Previous Sprint**: [sprint-1.md](./sprint-1.md)（13/13 ✅ / 122 tests / +18% velocity）
> **Retrospective**: [sprint-1-retrospective.md](./sprint-1-retrospective.md)

---

## Sprint Goal

**打通"启动 → 场景切换 → 章节进度 → 谜题解锁 → 单物件交互"的纵向切片**，构建 Core 层核心骨架。完成后，游戏将具备：从存档读取进度、加载对应章节场景、在场景内拖拽/吸附物件并触发谜题状态更新的完整链路。

---

## Capacity & Estimation Model

> **⚠️ 估算单位变更（Sprint 1 Retro Action #2）**：从"人日"改为 **story 复杂度点数**。
>
> - **1 点**：纯 Logic，3–5 个测试，零外部依赖
> - **2 点**：多模块集成 / 含 GameEvent / Luban 读取
> - **3 点**：Visual / Shader / PlayMode / 真机依赖
>
> **Sprint 1 baseline** = 25 点 / 13 stories（单 session）

| 指标 | 数值 |
|------|------|
| Sprint 承诺（Must + Should）| **14 stories / 25 点** |
| Nice to Have 延伸 | 3 stories / 7 点 |
| 总候选 | 17 stories / 32 点 |
| Buffer（即 Nice 部分）| ~28% 延伸空间 |

---

## Sprint 0 Add-on: SP-011 (Pre-work)

> ⚠️ Scene Management Story 002 Engine Risk = MEDIUM（YooAsset Additive Scene 在热更环境的互操作性）。按 Sprint 1 Retro 的谨慎原则，先做 spike 再进 story。

| ID | Spike | 目标 | 产出 | 估算 |
|----|-------|------|------|:----:|
| SP-011 | YooAsset Additive Scene Compatibility | 验证 YooAsset 2.3.17 `LoadSceneAsync(Additive)` 在 HybridCLR 热更程序集下正常运作；SceneHandle 能正确被 Unload 释放；多场景并存内存回收有效 | `docs/spikes/SP-011-yooasset-additive-scene.md`（3 项 PASS/FAIL）+ 参考代码片段 | **0.5 点** |

> 若 SP-011 发现阻塞 → Scene Story 002/003 延后到 Sprint 3，该 sprint 以其他 stories 补位。

---

## Tasks

### Must Have (Critical Path) — 10 stories / 19 点

**Track A — Chapter State 闭环（先做，解锁其他系统的数据源）**

| ID | Story | Type | Complexity | Depends on | AC 要点 |
|----|-------|:----:|:----------:|------------|---------|
| S2-01 | `chapter-state/story-002-puzzle-ordering` | Logic | **1 点** | S1-11 ✅ | `TbPuzzle.PuzzleOrder` 驱动；Complete 不可逆；`GetActivePuzzle` 返回首个 Idle |
| S2-02 | `chapter-state/story-003-chapter-progression` | Logic | **2 点** | S2-01 | 章节完成 → 下一章 `Locked → Idle`；immutable completion；最后章节特殊处理 |
| S2-03 | `chapter-state/story-004-state-events` | Integration | **1 点** | S2-02 | `Evt_PuzzleStateChanged` / `Evt_ChapterComplete` / `Evt_RequestSceneChange` 广播 |
| S2-04 | `chapter-state/story-005-save-integration` | Integration | **2 点** | S2-02 + S1-10 ✅ | `IChapterProgress` 双向打通；运行时更新 → SaveManager 落盘；启动时从存档还原 |

**Track B — Scene Management 骨架**

| ID | Story | Type | Complexity | Depends on | AC 要点 |
|----|-------|:----:|:----------:|------------|---------|
| SP-011 | **YooAsset Additive Spike**（前置）| Spike | **0.5 点** | — | 真机 3 项验证 |
| S2-05 | `scene-management/story-001-scene-state-machine` | Logic | **2 点** | SP-011 | 6 状态 FSM；`Idle → TransitionOut → Unloading → Loading → TransitionIn → Idle (+Error)`；互斥；队列 max=1 |
| S2-06 | `scene-management/story-004-transition-mutex` | Logic | **1 点** | S2-05 | 过渡期间新请求入队；重复切同章节 no-op；队列新请求覆盖旧请求 |
| S2-07 | `scene-management/story-006-luban-scene-mapping` | Config/Data | **1 点** | S2-05 | `TbChapter.SceneName` 映射；未知场景抛 error；降级到 MainMenu |

**Track C — Object Interaction 最小可交互**

| ID | Story | Type | Complexity | Depends on | AC 要点 |
|----|-------|:----:|:----------:|------------|---------|
| S2-08 | `object-interaction/story-001-interaction-state-machine` | Logic | **2 点** | S1-03 ✅（Gesture）| 6 状态 FSM；`Idle → Selected → Dragging → Snapping → Locked`；`InteractionCoordinator` 保证单选 |
| S2-09 | `object-interaction/story-002-drag-mechanics` | Logic | **2 点** | S2-08 | 1:1 追踪；InteractionBounds 回弹；response ≤ 16ms；fat-finger 补偿 |
| S2-10 | `object-interaction/story-004-grid-snap` | Logic | **2 点** | S2-09 | Grid formula；EaseOutQuad 插值；snap 结束发 `ObjectTransformChanged` |

### Should Have — 4 stories / 9 点

| ID | Story | Type | Complexity | Depends on | 说明 |
|----|-------|:----:|:----------:|------------|------|
| S2-11 | `object-interaction/story-003-rotation-mechanics` | Logic | **2 点** | S2-08 | 单指旋转 15° snap；方向键支持 |
| S2-12 | `object-interaction/story-006-interaction-lock-manager` | Logic | **2 点** | S2-08 | HashSet token 防重；SP-006 决策实施 |
| S2-13 | `object-interaction/story-007-multi-object-scene` | Integration | **2 点** | S2-08 + S2-12 | 多物件单选；InteractionCoordinator 单例 |
| S2-14 | `scene-management/story-002-additive-scene-loading` | Integration | **3 点** | S2-05 + SP-011 PASS | 真·YooAsset Additive 加载；3 场景内存上限 |

### Nice to Have — 3 stories / 7 点

| ID | Story | Type | Complexity | Depends on | 说明 |
|----|-------|:----:|:----------:|------------|------|
| S2-15 | `object-interaction/story-005-selection-feedback` | Visual/Feel | **3 点** | S2-08 | Outline shader + Scale bounce；依赖手动 Editor 验证 |
| S2-16 | `scene-management/story-003-cleanup-sequence` | Integration | **2 点** | S2-14 | UnloadUnusedAssets + GC.Collect 11 步流程 |
| S2-17 | `scene-management/story-005-scene-events` | Integration | **2 点** | S2-14 | 8 个 lifecycle event 按确定顺序广播 |

---

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|:------------:|
| （无）| Sprint 1 13/13 全部完成 | — |

---

## Critical Path

```
SP-011 Spike (0.5 点)
        ↓
Track A（Chapter 闭环）   Track B（Scene 骨架）   Track C（Object Int 最小）
S2-01 → S2-02 → S2-03    S2-05 → S2-06, S2-07    S2-08 → S2-09 → S2-10
         ↓
       S2-04 (Save ↔ Chapter)
```

**最长依赖链**: S2-01 → S2-02 → S2-04 = 5 点（Track A）
**三轨并行**: Track A 6点 ‖ Track B（含 Spike）4.5点 ‖ Track C 6点

---

## Dependencies on External Factors

- **Luban 配置表**：需要 `TbChapter` / `TbPuzzle` 至少有 1 个测试数据（S2-01/S2-04/S2-07 需要）。若 Luban 表未填，用 ScriptableObject 或 in-memory fixture 兜底（记在 Story Implementation Notes 里）。
- **YooAsset 构建管线**：SP-011 需能在 Editor Simulate Mode 运行；真机构建留到 Sprint 3 polish 阶段。

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|:----------:|:------:|------------|
| SP-011 发现 YooAsset + HybridCLR 热更不兼容 | LOW | HIGH | Story 002/003/014 延后到 Sprint 3；Must Have 范围收缩到 10 - 2 = 8 stories |
| Luban 表未准备好 | MEDIUM | MEDIUM | 先用 mock 数据实现 + 加 TODO；真表接入延后到 Sprint 3 |
| Visual/Feel story (S2-15) 自动化测试覆盖有限 | HIGH | LOW | Sprint 1 Retro Action #4：抽取参数层做单测，shader 本身手动 Editor 验证 |
| Object Int FSM 与 Input 的事件时序冲突 | LOW | MEDIUM | S1-03 GestureDispatcher 已定稿；AC 明确 Tap/Drag/Rotate 事件消费点 |

---

## Definition of Done for this Sprint

- [ ] SP-011 Spike 产出 PASS/FAIL 报告
- [ ] All Must Have stories（10 个）Status = Complete（通过 `/story-done`）
- [ ] 所有 Logic / Integration story 有对应 EditMode 测试，全绿
- [ ] `production/qa/qa-plan-sprint-2.md` 存在（Phase 5 要求）
- [ ] Code review 通过（零 ADR 违规）
- [ ] Unity Editor 编译零错误
- [ ] 枚举/常量引用在 Story 实现前 grep 校验（Retro Action #1）
- [ ] active.md 更新反映 Sprint 2 完成状态
- [ ] Smoke check 通过（若 Sprint 2 结束前 Test Runner 绿 & 可冷启动进入 MainMenu）

---

## Recommended Execution Order

**Phase 1（开场）— SP-011 Spike**
> 先摸清 YooAsset Additive 在 HybridCLR 环境下的可行性。半天内完成。

**Phase 2（Track A 主干）— 纵向切片 Save ↔ Chapter**
1. S2-01 Puzzle Ordering
2. S2-02 Chapter Progression
3. S2-04 Save Integration（与 S2-03 并行准备）
4. S2-03 State Events

**Phase 3（Track B + Track C 并行）**
- B：S2-05 → S2-06 → S2-07
- C：S2-08 → S2-09 → S2-10

**Phase 4（Should Have 延伸）**
- S2-11 → S2-12 → S2-13（Object Int 全家桶）
- S2-14 Additive Scene Loading（若 SP-011 PASS）

**Phase 5（Nice to Have，时间允许才做）**
- S2-15 Selection Feedback（Visual）
- S2-16 Cleanup Sequence
- S2-17 Scene Events

---

## Next Steps

1. **[完成]** ~~`/qa-plan sprint`~~ → `production/qa/qa-plan-sprint-2-2026-04-22.md` 已出（DRAFT）
2. **立即**：完成 SP-011 YooAsset Additive Spike
3. **Phase 2 开始**：`/story-readiness chapter-state/story-002-puzzle-ordering` → `/dev-story`
4. **Sprint 中**：`/sprint-status` 查进度
5. **Sprint 末**：`/retrospective sprint-2`

---

## QA Plan

**Path**: `production/qa/qa-plan-sprint-2-2026-04-22.md`

**覆盖**: 17 stories + SP-011 spike，分 Logic/Integration/Visual-Feel/Config 4 类
- 15 EditMode / 3 PlayMode 自动化测试
- 4 份 manual evidence（S2-04/07/13/14/15/16 中的 Visual/真机验证）
- 3 个 playtest 要求（S2-09/11/15）
- 1 个 grep gate（S2-07 场景名硬编码扫描）
