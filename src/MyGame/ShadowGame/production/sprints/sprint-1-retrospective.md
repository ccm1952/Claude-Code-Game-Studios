// 该文件由Cursor 自动生成

# Retrospective: Sprint 1 — Foundation Layer

> **Period**: 2026-04-22 (single-session sprint)
> **Phase**: Pre-Production
> **Generated**: 2026-04-22
> **Verdict**: ✅ **PASS**（Must Have 8/8 + Should Have 3/3 + Nice to Have 2/2 = 13/13）

---

## Metrics

| Metric | Planned | Actual | Delta |
|--------|:-------:|:------:|:-----:|
| Stories committed (Must + Should) | 11 | 11 ✅ | 0 |
| Stories completed (incl. Nice to Have) | 11 | **13** | +2 |
| Must Have completion rate | 100% | **100%** | — |
| Sprint total (all priorities) | 13 | 13 | **100%** |
| EditMode 测试数 | — | **122** | — |
| 测试通过率 | 100% | **100%** | — |
| 估时 (人日) | 9.0d（含 buffer 2d） | ~1d（AI 协作） | -8d |
| Code review ADR 违规 | 0 | 0 | 0 |

**总结**: 承诺 100% 达成，超额交付两个 Nice to Have stories。

---

## Velocity Trend

| Sprint | Planned | Completed | Rate | Notes |
|--------|:-------:|:---------:|:----:|-------|
| Sprint 0 (Spike) | 10 findings | 10 | 100% | SP-001~010 技术验证 |
| Sprint 1 (current) | 11 | 13 | **118%** | Foundation 全部完成 |

**趋势**：**Increasing**。第一个实施性 sprint 超额达成，建立了可信的 velocity baseline。

> ⚠️ AI 协作模式下的 velocity 数据不能直接套用到"人 × 天"单位——真实消耗是 chat 轮次 + 人工验证时间，应跟踪"session 数 + 测试数"而非"工期"。

---

## What Went Well

- **Story 模板完整性驱动自动化交付**：每个 story 都嵌入了 AC / Implementation Notes / ADR References / Control Manifest Rules，AI 实现阶段几乎零歧义，一次性通过率极高（Story 004/005 一轮通过，其他 stories 最多 1 轮微调）。
- **分层测试策略生效**：Logic stories 全部覆盖 EditMode 单元测试；Visual/Integration stories 抽取可测逻辑（如 `ShadowRTConfig`、`ShadowRenderingModule` 参数层）做 mock 测试，总 coverage 达 122 tests。
- **并行三轨执行**：Input / URP Shadow / Save+Chapter 三条并行轨道显著缩短临界路径，最长依赖链仅 2.0d。
- **SP-007 真机验证提前做**：避免了 S1-12 ShadowRT ReadBack 在实现阶段才发现 HybridCLR AOT 阻塞。
- **asmdef 问题主动归档到技能**：首次遇到 `TEngine.Runtime` Source Generator 引用问题后立即记录到 `.claude/docs/technical-preferences.md` + `.claude/skills/dev-story/SKILL.md` + `control-manifest.md` + `.claude/memory/`，后续 stories 零次复发。
- **IEEE CRC32 测试向量**：直接用 `123456789 → CBF43926` 作为 golden 测试用例，避免"自证"式的无效测试。

---

## What Went Poorly

- **字段/枚举命名不一致导致返工**：
  - `GestureData.RotationDelta/PinchScale` vs ADR-010 的 `AngleDelta/ScaleDelta` — Story 001 code review 时发现并修复。
  - `GestureType.LongPress` 在 Story 005 测试中被使用，但枚举实际是 `LightDrag` — 编译错误阻塞一次测试运行。
  - **根因**：Story 文件中 QA Test Cases 写的示例枚举值未与 GDD/ADR 交叉校验。

- **Unity API 自动更新弹窗中断测试**：
  - 首次创建 `ShadowRTReadback.cs` 引用了旧版 AsyncGPUReadback 回调签名，Unity 触发 Script Updating Consent 弹窗，中断 Test Runner 刷新导致误报"少 9 个测试"。
  - **根因**：Unity 2022.3.62f2 比 LLM 训练截止时间晚，某些 API 签名已变更，需依赖 Unity 自动升级。

- **HybridCLR 本地数据缺失**：
  - `Reimport All` 触发了完整 IL2CPP 编译路径，但 HybridCLR 本地目录未初始化 → `DirectoryNotFoundException`。
  - **根因**：`Reimport All` 是误操作（只需增量编译），但项目首次遇到时没人知道要先运行 `HybridCLR → Installer`。

- **Visual/Feel stories 难以自动化**：
  - S1-08 WallReceiver Shader 的 8 个 AC 中只有 2 个能通过 C# 测试覆盖（AC-5 Glow API、AC-7 无高光）。AC-1/3/8（明暗比、GPU 时间、Frame Debugger）必须手动 Unity Editor 验证。
  - **根因**：Visual 测试天然依赖 PlayMode 或真机 profiler，EditMode 无法覆盖。

---

## Blockers Encountered

| Blocker | Duration | Resolution | Prevention |
|---------|:--------:|------------|------------|
| asmdef 未引用 TEngine.Runtime（Source Generator 错误） | 1 轮 | 补充 asmdef 引用 + 归档到 skill/rules | ✅ 已写入 `control-manifest.md` Foundation §2.1 和 `dev-story` SKILL |
| `GestureType.LongPress` 不存在 | 1 轮 | 改为 `LightDrag` | Story 模板应包含"枚举值校验"步骤 |
| Unity API 自动更新弹窗 | 1 次 | 选 "Yes, for these and other files" | 文档化在 Unity 相关工作流中 |
| HybridCLR IL2CPP 目录缺失 | 1 次 | 提示用户运行 HybridCLR Installer | 新成员 onboarding 文档补充"不要用 Reimport All"告警 |

---

## Estimation Accuracy

| Story | Estimated (Plan) | Actual | Variance | Likely Cause |
|-------|:-------:|:------:|:--------:|--------------|
| S1-04/05 (Blocker + Filter) | 0.5d × 2 = 1.0d | < 1 session | 超预估准确 | Story 模板已完备，零歧义 |
| S1-08 (WallReceiver Shader) | 1.5d | 1 session（含手动场景搭建） | 准确 | SP-005 已提供 shader 骨架 |
| S1-10 (Atomic Write + CRC32) | 1.0d | < 1 session | 超预估 | ISaveMigration 接口在 S1-09 已打桩 |
| S1-12 (ShadowRT ReadBack) | 1.0d | < 1 session | 超预估 | SP-007 真机验证已做 |

**整体估时准确度**：**N/A**（AI 协作模式使传统人日估算失效）。建议 Sprint 2 起改用 "story 复杂度点数"：
- **1 点**：纯 Logic + 3-5 测试（S1-01/04/05/09）
- **2 点**：集成事件 + Luban/外部依赖（S1-03/06/10）
- **3 点**：Visual/Shader/PlayMode 依赖（S1-08/12）
- **Sprint 1 实际消耗**：约 25 点 / 13 stories

---

## Carryover Analysis

| Task | Status |
|------|:------:|
| (无 carryover) | — 全部在一个 session 内交付 |

---

## Technical Debt Status

| 指标 | 当前 | Sprint 0 (baseline) | 趋势 |
|------|:----:|:-------------------:|:----:|
| TODO 标记 | 16 (遗留) | 16 | **Stable** |
| FIXME 标记 | 0 | 0 | Stable |
| HACK 标记 | 0 | 0 | Stable |
| 代码文件 | 23 新增 | — | — |
| 测试文件 | 13 | — | — |
| 测试数 | 122 | 12 (SP-007 + ShadowPuzzle 遗留) | **+110** |

**分析**：Sprint 1 新增代码 **零技术债务**。全部 16 处 TODO 都在 GameFlow 各 State（Sprint 0 之前遗留，属于 TEngine 模板代码，未来由 Sprint 2 的 Scene Management epic 处理）。

---

## Previous Action Items Follow-Up

| Action（来自 Sprint 0 / Gate Check）| Status | Notes |
|-------------------------------|:------:|-------|
| Test framework 初始化 + EditMode 测试 | ✅ Done | 122 tests |
| Architecture Traceability Index | ✅ Done | `docs/architecture/architecture-traceability.md` |
| Event ID 冲突修复（ADR-006 vs 010/011）| ✅ Done | 3 个冲突修复 |
| UX interaction-patterns + core screens | ✅ Done | `design/ux/` |
| Accessibility requirements | ✅ Done | `design/accessibility-requirements.md` |
| SP-007 真机验证（HybridCLR + AsyncGPU）| ✅ Done | 3 项 PASS |
| Control Manifest | ✅ Done | 576 行 |
| All 91 Stories created | ✅ Done | Foundation 39 + Feature 52 |

**100% 历史 action items 已关闭，无遗留 process smell。**

---

## Action Items for Sprint 2

| # | Action | Owner | Priority | Deadline |
|---|--------|-------|:--------:|----------|
| 1 | Story 模板增加"枚举值校验"步骤（生成前 grep 核实所引用枚举） | dev-story skill | High | Sprint 2 开始前 |
| 2 | 引入 story 复杂度点数（1/2/3 点），替代人日估算 | producer | High | Sprint 2 Planning |
| 3 | 新建 `docs/onboarding/unity-workflow.md` 记录 HybridCLR Installer + API 自动更新等陷阱 | — | Medium | Sprint 2 内 |
| 4 | Visual/Feel stories 增加"可测逻辑抽取"约定（将参数/生命周期从 shader 分离到 C#）| tech-lead | Medium | Sprint 2 起 |
| 5 | Sprint 2 推进 Core 层：Object Interaction（7 stories）+ Scene Management（6 stories）+ ChapterState 剩余 4 stories | producer | High | Sprint 2 |

---

## Process Improvements

- **Story 模板自检清单**：在 `/create-stories` 结束前加一步"引用枚举/常量/字段名是否在产品代码中存在"的 grep 校验，预防 `LongPress` 类错误。
- **Velocity 单位切换**：从"人日"改为"story 复杂度点数"。Sprint 1 baseline = 25 点 / 13 stories，Sprint 2 按此规划。
- **AI API 知识盲区处理**：对 Unity 2022.3 + HybridCLR + URP 的最新 API 变更，优先引用官方 Upgrade Guide 或让 Unity 自动更新，避免 LLM 凭记忆写旧签名。

---

## Summary

Sprint 1 是项目的第一个真正意义上的实施 sprint，**13/13 stories 全部交付**，122 EditMode 测试全绿，零 ADR 违规，零遗留技术债务。验证了"GDD → ADR → Control Manifest → Story → 实现"的工作流在 AI 协作模式下完全畅通。最重要的改进方向：把 estimation 单位从人日切换到 story 复杂度点数，并在 Story 模板生成阶段加入枚举/字段名校验，消除小概率但高代价的返工。

---

**下一步**: `/sprint-plan sprint-2` — 规划 Core 层 stories（Object Interaction + Scene Management + ChapterState 剩余）。
