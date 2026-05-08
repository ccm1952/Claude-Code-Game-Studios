// 该文件由Cursor 自动生成

# Change Impact Report: S3-01 D5 — SceneManager Scene Identity Caching Strategy

> **Date**: 2026-04-30 afternoon
> **Decision source**: `production/epics/scene-management/story-002-additive-scene-loading.md` §D5 (S3-01 patch v2 + commit + PlayMode CORE PASSED)
> **Decision summary**: SceneManager **不缓存** YooAsset `SceneHandle` / Unity `Scene` 引用；改用 `_currentChapterSceneName: string` (YooAsset location)。Scene 操作走 `GameModule.Scene.LoadSceneAsync(name, mode, callback) → UniTask<Scene>` + `GameModule.Scene.UnloadAsync(name) → UniTask<bool>` + `GameModule.Scene.ActivateScene(name)` framework facade（**不**是 `GameModule.Resource.LoadSceneAsync` — 该 API 不存在）。
> **Author**: ADR-029 first practical propagation run
> **Triggered by**: ADR-029 V2 触发条件改进 — 当 architecture decision 影响多 story 时，应在做完决策的同 sprint 立刻跑 propagate-design-change 扫描所有 downstream files

---

## 1. Change Summary

S3-01 patch v2（2026-04-30）+ S3-01 P0 修订（2026-04-30）+ S3-01 PlayMode CORE PASSED（2026-04-30）共同确立了 **D5=[X]**：

```
之前 (2026-04-22 ADR-009 第一稿假设):
  SceneManager._currentSceneHandle: SceneHandle (YooAsset)
  Load:   _currentSceneHandle = await GameModule.Resource.LoadSceneAsync(name, mode)
  Unload: await GameModule.Resource.UnloadSceneAsync(_currentSceneHandle); _currentSceneHandle = null

现在 (2026-04-30 D5=[X] + S3-01 PlayMode 实测):
  SceneManager._currentChapterSceneName: string (YooAsset location)
  Load:   Scene scene = await GameModule.Scene.LoadSceneAsync(name, mode, progress);
          if (scene.IsValid() && scene.isLoaded) {
              GameModule.Scene.ActivateScene(name);
              _currentChapterSceneName = name;
          }
  Unload: await GameModule.Scene.UnloadAsync(_currentChapterSceneName);
          ClearCurrentChapterSceneName();  // 内部 setter, S3-02 cleanup 调用
```

**修订原因**：
1. **Fantasy API 1**：`GameModule.Resource.LoadSceneAsync` / `UnloadSceneAsync(SceneHandle)` 不存在（被 ADR-029 R2 grep gate 抓住）
2. **Fantasy API 2**：YooAsset 2.3.17 + TEngine 6.0 wrapper 返 Unity 原生 `Scene` struct，不是 YooAsset `SceneHandle`
3. **Cache strategy refinement**：
   - `Scene` 是 value-type；async invalidation 风险
   - YooAsset 内部按 location 索引；framework wrapper `UnloadAsync(name)` 等价
   - 字符串 cache 简单稳定
4. **`CheckLocationValid` 不可前置**：对 scene 资产一律返 false（S3-01 P0 修订移除）

---

## 2. Affected Files Inventory

### ✅ DONE (post-propagation, this run)

| # | File | Type | Patch |
|---|------|------|-------|
| 1 | `production/epics/scene-management/story-002-additive-scene-loading.md` | Story | S3-01 patch v2 + P0 修订 + DevBootstrap fix（pre-propagation; PlayMode CORE PASSED）|
| 2 | `production/epics/scene-management/story-003-cleanup-sequence.md` | Story | patch v2（4 处 fantasy API + Test Evidence pivot to PlayMode spike + ClearCurrentChapterSceneName setter + NoChapterId 哨兵；估 8 min vs S3-01 baseline 18 min ~55% 节省）|
| 3 | `docs/architecture/control-manifest.md` | Active rules | §2.4 cleanup 序列重写 + §2.5 加 4 条 Required + §2.5 加 2 条 Forbidden（active dev rules 直接生效）|
| 4 | `production/qa/qa-plan-sprint-3-2026-04-30.md` | Active sprint QA | S3-01 + S3-02 全 EditMode → PlayMode spike pivot + `_currentSceneHandle` 5 处替换 + first-boot guard NoChapterId |
| 5 | `design/gdd/scene-management.md` | GDD source | §Core Rules 加载 + 卸载规则 重写 + §TEngine Integration code example 重写 |
| 6 | `docs/architecture/adr-005-yooasset-lifecycle.md` | ADR | Status 加 D5 update note + Engine Compat Post-Cutoff APIs 修订 + §Common Pitfalls code 重写 + §Validation Criteria scene line superseded + §GDD Requirements row 修订 + 末尾加 §Scene Loading Update 完整 note |
| 7 | `docs/architecture/adr-009-scene-lifecycle.md` | ADR | Status 加 D5 update note + Engine Compat Post-Cutoff APIs + Knowledge Risk LOW + §Decoupling diagram + Step 6 in 11-step flow + §SceneHandle Ownership → §Scene Identity Caching 重写 + §Forbidden Patterns 加 fantasy + cache field forbidden + §Risks 修订 + §Migration Plan Step 1 marked DONE + §Validation Criteria SceneHandle line superseded + 2 GDD Requirements rows 修订 + 末尾加 §Scene Handle Update 完整 note |
| 8 | `docs/architecture/architecture.md` | Master arch | P4 资源闭环描述 + Engine Capability Map Scene 行 + Scene Management Owns/Engine APIs 表 + ProcedureMain ASCII + Forbidden Patterns row（5 处概览刷）|
| 9 | `production/epics/scene-management/EPIC.md` | Epic | Overview tech 描述 + Governing ADRs §ADR-009 行 + TR-scene-017 描述（3 处）|
| 10 | `docs/architecture/architecture-traceability.md` | Trace index | TR-scene-017 行修订 |
| 11 | `production/sprints/sprint-3.md` | Sprint plan | S3-01 行加 ✅ + 描述刷 |
| 12 | `production/epics/scene-management/story-002-additive-scene-loading.md` (minor) | Story §Dependencies | "Story 003 uses `_currentSceneHandle`" → "uses `_currentChapterSceneName` + `ClearCurrentChapterSceneName()` setter exposed" |
| 13 | `docs/architecture/sprint0-spike-plan.md` | 历史档 | 顶部加 ⚠️ 历史档 superseded 注脚 + authoritative API 指引 |
| 14 | `production/qa/qa-plan-sprint-2-2026-04-22.md` | 历史档 | 顶部加 ⚠️ 历史档 retrospective superseded 注脚 |

### ✅ Already clean (no patch needed)

- `story-001-scene-state-machine.md`（DONE，无 D5 drift）
- `story-004-transition-mutex.md`（无 D5 drift）
- `story-005-scene-events.md` (S3-03)（**关键**：clean — S3-03 readiness check 应直接 PASS 验证 V2 触发条件改进）
- `story-006-luban-scene-mapping.md`（无 D5 drift）

---

## 3. Drift Revision Time Data Points (ADR-029 metric)

| Story / Doc | Drift type breakdown | Time | Notes |
|-------------|---------------------|------|-------|
| **S3-01 (baseline)** | Type-1 fantasy API 6处 / Type-2 CheckLocationValid / Type-3 spike race | **18 min** | First baseline; 含 patch v2 全文 + 2 轮 PlayMode + D1-D6 + 3 修复 option |
| **S3-02** | Type-1 only 4处 (S3-01 D5 propagate 滞后) | **~8 min** | ~55% 节省 vs baseline |
| **Propagation run (this)** | 12 文件全量修订 | **~35 min** | First propagation experiment |
| **节省预估** | 避免 S3-03/-04/-05 等下游每条 R2 STOP × 5 stories × 5-8 min = **25-40 min**；avoid QA走偏 incident × 1-2 = **15-30 min** | **avoidance ≈ 40-70 min** | Net ROI: 5-35 min positive |

**Net insight**：propagate-design-change 对 architecture-level decision 是 **net positive ROI**（即使首次 35 min 也 break-even）。后续应作为 ADR-029 V2 的强制环节插入：当 design decision 影响多 story 时，做完决策同 sprint 立刻跑全量 propagation。

---

## 4. ADR-029 V2 Touch-up Recommendations

基于本次实战，建议 ADR-029 V2 加以下条目：

1. **Trigger condition**: "When a design decision (D-level) in any story or ADR materially affects ≥2 downstream stories or ≥1 ADR section, run `/propagate-design-change` (lite version) within 1 working day of decision finalization。"
2. **Lite propagation playbook**: 不需要走 GDD revision 全 8-step skill；只需：(a) grep 全量 fantasy API patterns，(b) impact matrix 分类（DONE / Likely Superseded / Needs Review / 历史档），(c) per-priority 修订（P0 active rules → P1 ADR/GDD → P2 概览 → P3 历史档），(d) 写 change-impact doc 留档。
3. **Drift type 4 候选**: "Architecture decision propagation drift" — design decision 已生效但下游文档未跟进。本次 S3-02 的 R2 STOP 全部属于此类，是 stale doc reference 而不是新决策drift。Type-4 修订时间应可降到 ≤2 min/story（仅替换字段名/wrapper namespace）。

---

## 5. Resolution Decisions

所有 14 个文件用户选 **[FULL]** 全量修订（vs P0+P1 / P0 / MIN）。无文件标 Superseded（ADR-005/009 用 in-place update + dedicated update note section 模式，保留原文 trail 不删）。

---

## 6. Follow-Up Actions

- [x] 全 14 文件修订完成
- [ ] 写本 change-impact doc（本文件）
- [ ] 更新 `production/session-state/active.md` Session 21 (continued #3) 加 propagation 实战 ROI 数据点 + ADR-029 V2 改进建议
- [ ] 后续 `/dev-story` S3-02 验证 control manifest 修订生效（dev 跑 R-checks 时引用刷新后的 cleanup 序列）
- [ ] 后续 `/dev-story` S3-03 验证 V2 触发条件改进有效性（story-005 should PASS readiness check directly with 0 R2 drift）
- [ ] Sprint 3 末 retrospective：评估 propagation ROI 实际数据 vs 估计；如确实 net positive，正式 propose ADR-029 V2 加 trigger condition 条款

---

## 7. Verification

post-propagation grep 应满足：
- ✅ Active rule docs (`control-manifest.md`, `qa-plan-sprint-3-2026-04-30.md`) 0 hit `_currentSceneHandle`
- ✅ Active stories (S3-02 / 002 patch v2) 0 hit `GameModule.Resource.UnloadSceneAsync`
- ⚠️ ADR-005/009 仍有 SceneHandle 字面量（在 §Original notes / §Update notes 中作为历史 trail 保留，**不删除**）
- ⚠️ 历史档 (sprint0-spike-plan.md, qa-plan-sprint-2) 仍有 SceneHandle，但顶部 superseded 注脚已加

**最终 verdict**: ✅ **COMPLETE** — 12 个文件全量修订 + 2 个历史档 superseded 注脚 + 1 个 change-impact doc 留档。Sprint 3 后续 stories 预期 0 复发 D5 drift；S3-02 / S3-03 / S3-05 dev-story Phase 1.5 R2 应直接 PASS。
