// 该文件由Cursor 自动生成

# Sprint 3 — Multi-Scene Integration × ADR-029 治理形式化 × Visual Polish 起步

> **Sprint N**: 3
> **Phase**: Pre-Production（Core 层最后一公里 + Polish 起步）
> **Start**: 2026-04-30
> **End (expected)**: 2026-05-15（15 自然日 / 11 工作日；按 Sprint 2 实际 8 天闭环 22 SP 估算，Sprint 3 ~14-18 SP 应有足够 buffer）
> **Review Mode**: solo
> **Previous Sprint**: [sprint-2.md](./sprint-2.md)（13/14 commitments ✅ / 93% / +198 tests）
> **Retrospective**: [sprint-2-retrospective.md](./sprint-2-retrospective.md)

---

## Sprint Goal

**完成 Multi-Scene Integration 链路**（YooAsset Additive 真·加载 + Cleanup Sequence + Scene Events）、**形式化 Story §Implementation Notes 验证流程为 ADR-029**（drift 第 4 次反复触发条件成熟）、**起步 Visual Polish**（Object 选中反馈）。结束本 sprint 后游戏将具备：完整的多场景动态加载/卸载/事件链路 + Object 交互的视觉反馈 + 系统化的 story drift 预防 gate。

---

## Capacity & Estimation Model

> 沿用 Sprint 1/2 复杂度点数模型（1/2/3 点）。Sprint 2 baseline = 22 点 / 13 stories（93% commitment 达成 + 提前 ~10 天闭环）。

| 指标 | 数值 |
|------|------|
| Sprint 承诺（Must + Should）| **7 stories / 14 点** |
| Nice to Have 延伸 | 2 stories / 3 点 |
| 总候选 | 9 items / 17 点 |
| Buffer | ~22% 延伸空间（含 PlayMode 测试 batch 不确定时长）|

**Velocity 参考**：Sprint 1 = 25 SP / 13 stories；Sprint 2 = 22 SP / 13 stories；Sprint 3 计划 14 SP / 7 stories（保守，因含首次真·YooAsset Additive 多场景集成测试 + governance 任务）。

---

## Tasks

### Must Have (Critical Path) — 4 items / 8 点

**Track A — Multi-Scene Integration（Carryover from Sprint 2；解锁 Sprint 4 多章节切换）**

| ID | Story | Type | Complexity | Depends on | AC 要点 |
|----|-------|:----:|:----------:|------------|---------|
| S3-01 ✅ | `scene-management/story-002-additive-scene-loading` | Integration (PlayMode-only) | **3 点** | S2-05 ✅ + SP-011 PASS ✅ | 真·YooAsset Additive 加载；3 场景内存上限；Loading 状态机推进；SceneManager 持 `_currentChapterSceneName: string` (S3-01 D5；不缓存 SceneHandle) — DONE 2026-04-30 PlayMode CORE PASSED |
| S3-02 ✅ | `scene-management/story-003-cleanup-sequence` | Integration (PlayMode-only) | **2 点** | S3-01 ✅ | UnloadUnusedAssets + GC.Collect 4 步 cleanup + try-finally；OnSceneUnloadBegin 派发；5-cycle 内存回收 — DONE 2026-04-30 dusk PlayMode CORE PASSED 6/6（v3 Type-2 cross-method protocol fix _currentLoadedChapterId 配对字段）|
| S3-03 ✅ | `scene-management/story-005-scene-events` | Integration (PlayMode-only) | **2 点** | S3-01 ✅ + S3-02 ✅ | 2 个 sender 端实装 (OnSceneTransitionBegin/End) + BeginTransitionAsync 11 步骨架 + IFadeOverlay placeholder + NoOpFadeOverlay default + RegisterFadeOverlay setter + null-out + null-check guard self-removal pattern — DONE 2026-04-30 dusk PlayMode CORE 5/5 PASSED（patch v2 Type-4×6+Type-1×2 + patch v3 Type-2(c) Framework behavior assumption fix；ADR-029 累计 5 数据点；V2 候选 #7 触发）|

**Track B — Governance（Sprint 2 retro action #1）**

| ID | Item | Type | Complexity | Depends on | AC 要点 |
|----|------|:----:|:----------:|------------|---------|
| S3-04 | **ADR-029 起草："Story §Implementation Notes 验证流程"** | Governance / ADR | **1 点** | drift memo 4 次复用 ✅ | 形式化 R1 (per-event listener 模式) / R2 (跨组件 API grep 实证) / R3 (stub 类型构造签名 grep)；并入 `/create-stories` Phase 1 前置检查；Sprint 3 起每个 story dev-story 完成时记录"§Impl Notes drift 修订耗时" |

### Should Have — 3 items / 6 点

| ID | Story | Type | Complexity | Depends on | 说明 |
|----|-------|:----:|:----------:|------------|------|
| S3-05 | `object-interaction/story-005-selection-feedback` | Visual/Feel | **3 点** | S2-08 ✅ | Outline shader + Scale bounce；Visual Polish 起步；Sprint 1 Visual story 教训：抽参数层做 EditMode 单测，shader 本身手动 Editor 验证 |
| S3-06 | **PlayMode 测试 batch**（Sprint 2 推延项汇总）| QA / PlayMode | **2 点** | S2-09 / S2-10 / S2-11 / S2-13 ✅ | DOTween 精确 duration（EaseOutBack / EaseOutQuad）+ Raycast 物理 + fat-finger 数学 + 10 obj 性能；同一 PlayMode session 集中验 |
| S3-07 | **Onboarding doc** `docs/onboarding/unity-workflow.md`（Sprint 1 retro action #3 carryover）| Docs | **1 点** | — | HybridCLR Installer + Unity API 自动更新 + DOTween EditMode + Visual EditMode 受限 + 测试 fixture boilerplate 一处合一 |

### Nice to Have — 2 items / 3 点

| ID | Story | Type | Complexity | Depends on | 说明 |
|----|-------|:----:|:----------:|------------|------|
| S3-08 | `ui-system/story-001-uimodule-setup` | Integration | **2 点** | — | UI 系统初始化；Sprint 4 UI/UX 启动前的探路 story；如 Sprint 3 余力允许做 |
| S3-09 | `settings-accessibility/story-001-settings-manager` | Logic | **1 点** | — | Settings 单例 + 持久化；与 SaveManager 已有集成路径 |

---

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|:------------:|
| S2-14 → **S3-01** Additive Scene Loading | Sprint 2 自然停顿点（should-have 3/3 + 累计 13 stories）；与 Sprint 3 多场景 batch 做更合理 | 3 点（不变）|
| S2-16 → **S3-02** Cleanup Sequence | nice-to-have；依赖 S2-14 | 2 点（不变）|
| S2-17 → **S3-03** Scene Events | nice-to-have；依赖 S2-14 | 2 点（不变）|
| S2-15 → **S3-05** Selection Feedback | nice-to-have；Visual/Feel；推到 Polish 起步 | 3 点（不变）|

---

## Critical Path

```
S3-04 ADR-029（独立，1 点）— 可与 Track A 并行

Track A（Multi-Scene 链路）:
S3-01 Additive Scene Loading (3 点)
        ↓
S3-02 Cleanup Sequence (2 点)  +  S3-03 Scene Events (2 点)  ← 可并行
        ↓
（Track A 闭环 = 7 点）
```

**最长依赖链**: S3-01 → (S3-02 ‖ S3-03) = 5 点
**两轨并行**: Track A 7 点 ‖ Track B (S3-04 + Should Have) 7 点

---

## Dependencies on External Factors

- **YooAsset 真·Additive Mode**：S3-01 需要 YooAsset 真·Additive 模式（不再走 Editor Simulate）；SP-011 已 PASS 证明可行性，本 sprint 进入实装。
- **Luban TbScene 配置**：S3-01/-02/-03 需要至少 2 个测试章节场景配置（用现有 mock 或 in-memory fixture 兜底）。
- **DOTween + Outline Shader**：S3-05 需要 Outline shader（Sprint 0 SP-005 已有骨架）；URP 渲染管线兼容。

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|:----------:|:------:|------------|
| YooAsset Additive 真·加载在 HybridCLR 热更环境下出现 SP-011 未覆盖的边缘 case | LOW | HIGH | SP-011 PASS 但 PlayMode 实际测试可能露新问题；S3-01 起手做 5min smoke test 再深入实施 |
| ADR-029 形式化未能消除 drift 修订时间 | MEDIUM | LOW | Sprint 3 全程跟踪 "§Impl Notes drift 修订耗时"；目标 ≤ 1min；如未降低则 Sprint 4 起评估 ADR-029 V2 |
| Visual story (S3-05) 自动化测试覆盖有限 | HIGH | LOW | Sprint 1 Visual 教训复用：抽参数层做 EditMode 单测，shader 本身手动 Editor 验证；明确 PlayMode/真机推延项 |
| PlayMode 测试 batch 一次性发现多个 Sprint 2 推延项 bug | MEDIUM | MEDIUM | S3-06 是收 Sprint 2 推延项，发现 bug 走 hotfix workflow（不开新 story；记 bug 报告 + 修 + 写 PlayMode test） |
| Sprint 3 跨"自然停顿点"叠加 Polish 与 Production 类工作可能割裂 | LOW | LOW | sprint goal 明确 3 主题（Multi-Scene + ADR-029 + Polish 起步），不同 track 并行清晰 |

---

## Definition of Done for this Sprint

- [ ] All Must Have items（4 个）Status = Complete（通过 `/story-done` 或 ADR Accepted）
- [ ] 所有 Logic / Integration story 有对应 EditMode 测试，全绿
- [ ] PlayMode 测试 batch (S3-06) 全绿（含 Sprint 2 推延项验证）
- [ ] `production/qa/qa-plan-sprint-3.md` 存在
- [ ] Code review 通过（零 ADR 违规）
- [ ] Unity Editor 编译零错误
- [ ] ADR-029 已 Accepted 并并入 `/create-stories` skill 的 Phase 1 前置检查
- [ ] active.md 更新反映 Sprint 3 完成状态
- [ ] Smoke check 通过
- [ ] **drift 修订耗时统计**：Sprint 3 平均 §Impl Notes drift 修订耗时 ≤ 1min（验 ADR-029 形式化效果）

---

## Recommended Execution Order

**Phase 1（开场，独立可并行）— ADR-029 起草**
> S3-04 是 governance work，与 Track A 完全解耦；优先做完，让后续 stories 走新 readiness 流程。半 session 完成。

**Phase 2（Track A 主干）— Multi-Scene 集成**
1. S3-01 Additive Scene Loading（关键起点；先 5min smoke test 验 YooAsset 真·Additive 在 HybridCLR 下的实际行为）
2. S3-02 Cleanup Sequence + S3-03 Scene Events（并行；都依赖 S3-01）

**Phase 3（Should Have 延伸）**
- S3-05 Selection Feedback（Visual Polish 起步；Track A 闭环后做）
- S3-06 PlayMode 测试 batch（汇总 Sprint 2 推延项；与 Track A 完成同步收尾）
- S3-07 Onboarding doc（任何时候可做；独立 docs 任务）

**Phase 4（Nice to Have，时间允许才做）**
- S3-08 UI Module Setup（Sprint 4 UI/UX 启动探路）
- S3-09 Settings Manager

---

## Next Steps

1. **`/qa-plan sprint`** → `production/qa/qa-plan-sprint-3.md`（实施前必跑）
2. **`/architecture-decision ADR-029`** → S3-04 起草并 Accepted
3. **`/story-readiness scene-management/story-002-additive-scene-loading`** → 5min smoke test
4. **`/dev-story scene-management/story-002-additive-scene-loading`** → S3-01 实施
5. **Sprint 中**：`/sprint-status` 查进度
6. **Sprint 末**：`/retrospective sprint-3`

---

## QA Plan

**Path**: `production/qa/qa-plan-sprint-3-2026-04-30.md` ✅ 已生成（2026-04-30）

**预期覆盖**: 9 items（含 governance + docs）
- 6 EditMode + 1 PlayMode batch + 1 Visual manual + 1 ADR review + 1 Docs review
- 2 份 manual evidence（S3-01 真机 / S3-05 Editor）
- 1 个 metrics 报告（drift 修订耗时统计）

> ⚠️ **注**：S3-04 ADR-029 不走标准 dev-story 路径，走 `/architecture-decision`。S3-07 Onboarding doc 走 `/onboard` skill。
