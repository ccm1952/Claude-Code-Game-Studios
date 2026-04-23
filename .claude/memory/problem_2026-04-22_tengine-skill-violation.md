// 该文件由Cursor 自动生成

# 问题记录：SP-011 Spike 未遵守 TEngine 前置协议

> **日期**: 2026-04-22
> **发生次数**: 1 次（SP-011 YooAsset Additive Spike 实现）
> **严重性**: 中等（产出可用但偏离最佳实践；给用户带来不必要的手工操作负担）

## 问题现象

执行 Sprint 2 前置 Spike SP-011（YooAsset Additive Scene Compatibility）时，Agent 直接创建了 `SP011_YooAssetAdditiveTest.cs` + `SP011_YooAssetAdditiveLauncher`（MonoBehaviour）+ Spike 报告，**全程未读** `src/MyGame/ShadowGame/.claude/skills/tengine-dev/`。

### 具体偏离

| 偏离 | 实际做法 | 应有做法 |
|------|---------|---------|
| 未读 skill SKILL.md | 直接模仿 SP-007 模式写代码 | 先读 `.claude/skills/tengine-dev/SKILL.md` 导航 |
| 未评估 unity-mcp | 给用户生成 10 行手动操作步骤（Editor 中建场景、挂 GameObject、Add Component） | 读 `unity-mcp-guide.md` 发现 `manage_scene` / `manage_gameobject` / `manage_components` 可自动化完成 |
| 未读 hotfix-development.md | Launcher 直接挂 MonoBehaviour，对齐 SP-007 野路子 | skill 提示 ProcedureBase 是 TEngine-native 方式，可供权衡选择 |

## 原因分析

### 根因 1：skill 触发条件未在 CLAUDE.md 中明确强制

`tengine-dev/SKILL.md` 的 frontmatter 写明"在 TEngine 框架项目中编写或修改任何代码时触发"，但这是 skill 自身声明，`CLAUDE.md` 并未在工程规则层面硬性要求 Agent 必读。

Agent 仅遵守了 `CLAUDE.md` 的 wiki-query-agent 规则体系（查 `repowiki/zh/content/`），忽略了更直接的 `.claude/skills/tengine-dev/references/`（尤其是 `unity-mcp-guide.md` 这块 wiki 未覆盖的内容）。

### 根因 2：skill 与 wiki 的边界在工程规则中未定义

`.claude/skills/tengine-dev/references/` 与 `repowiki/zh/content/` 内容有重叠也有差异（MCP 自动化只在 skill 有，深度 API 规范在 wiki 更全）。Agent 无法判断先读哪个、是否都读。

### 根因 3：沿用 SP-007 模式形成路径依赖

SP-007 当时选了 MonoBehaviour 挂场景的野路子（一次性 Spike 工具合理选择），但 SP-011 直接模仿而非重新评估，导致"一次决策传染多次任务"。

## 解决方案

### 工程规则层面（已落地）

在 `src/MyGame/ShadowGame/CLAUDE.md` 新增「🎯 TEngine 任务前置协议（MUST）」一节，作为「⚡ 强制工作流」的前置步骤：

- 触发条件列举 8 项（TEngine 模块 / HotFix / Editor 资产 / unity-mcp / YooAsset / HybridCLR / Luban / UniTask）
- 三级阅读策略：L-0 SKILL.md 导航 / L-1 对应 reference / L-2 调 wiki-query-agent
- 强制检查点：TodoWrite 第一项必为 TEngine 前置阅读；涉及 Editor 操作必须先评估 unity-mcp
- 违反自检：写入 `/.claude/memory/problem_YYYY-MM-DD_tengine-skill-violation.md`；同会话二次违反中止任务

### SP-011 具体修正（待决策）

当前产出（`SP011_YooAssetAdditiveTest.cs` + Launcher + 报告骨架）可**保留作为一次性 Spike 工具**，或按 TEngine 规范重构：

- 方案 A：保留现状，用 unity-mcp 自动完成场景创建 + GameObject 挂载（解决手工操作负担，不改代码结构）
- 方案 B：重构为 `ProcedureSP011Test : ProcedureBase` 走 TEngine-native 流程（对齐框架最佳实践，但对 Spike 属于过度设计）

## 受影响 / 已更新的文档

- [x] `src/MyGame/ShadowGame/CLAUDE.md` — 新增「🎯 TEngine 任务前置协议（MUST）」
- [x] `/.claude/memory/problem_2026-04-22_tengine-skill-violation.md` — 本文件
- [ ] Sprint 1 Retrospective Action Items — 待补一条「TEngine 任务前置协议」Action
- [ ] `.claude/docs/technical-preferences.md` — 可考虑补充 skill 优先级声明（可选）

## 预防复发的机制

1. **TodoWrite 硬约束**：任何涉及 TEngine / HotFix / Unity Editor 操作的任务，第一项 todo 必须为 SKILL.md 阅读。
2. **Editor 操作强制 MCP 评估**：禁止直接生成"请在 Unity Editor 中..."指南，除非已确认 unity-mcp Bridge 不可用。
3. **违反自动触发补救**：发现违反立即写 memory + 回滚 + 重启任务。
