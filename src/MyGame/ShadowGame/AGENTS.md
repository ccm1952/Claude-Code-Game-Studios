<!-- 该文件由 Cursor 自动生成 -->

# AGENTS.md — ShadowGame (Unity 2022.3 + TEngine) 子工程

> 跨工具（Cursor / Claude Code / Codex 等）AI 代理进入本目录的**短入口**。
> 协议原文请以 [`CLAUDE.md`](CLAUDE.md) 与 [`.claude/skills/tengine-dev/SKILL.md`](.claude/skills/tengine-dev/SKILL.md) 为准；本文件仅做导航 + 硬红线兜底。

---

## 技术栈

Unity 2022.3.62f2 LTS · TEngine · HybridCLR（代码热更）· YooAsset 2.3.17（资源热更）· UniTask（异步）· Luban（配置表）

---

## 进入任务前的第一动作（MUST）

**任何涉及本目录下 `.cs` / `.unity` / `.prefab` / `.shader` / `.mat` / `.asset` / `.anim` / `.controller` / `.asmdef` 的设计、读、改、生成操作，必须先完成：**

1. 判定任务等级 **L1 / L2 / L3 / L4**（判断标准见 [`CLAUDE.md` 强制工作流第零步](CLAUDE.md)）
2. L2+ 必读 [`.claude/skills/tengine-dev/SKILL.md`](.claude/skills/tengine-dev/SKILL.md) 导航表
3. 按导航表追加阅读 `.claude/skills/tengine-dev/references/` 下的对应 reference
4. L3/L4 调用 wiki 查询：
   - **Claude Code** 用 `wiki-query-agent` subagent
   - **Cursor** 降级为 `SemanticSearch` + `Read` 查 `repowiki/zh/content/`（并在已读声明中注明降级）
5. 在产出代码 / 方案前输出「已读声明」块（格式见下）

**Cursor 用户补充**：`.cursor/rules/shadowgame-tengine.mdc` 已按 glob 自动挂载，包含强制 TodoWrite 前置项、违反处理流程等完整约束，请严格遵守。

---

## 已读声明格式（L2+ 必须）

```
任务等级: L2 / L3 / L4
已读: SKILL.md + [references/xxx.md, references/yyy.md]
会话缓存: [本会话已查过的主题]
未读: [references/zzz.md — 跳过理由]
wiki 查询: [主题 A, 主题 B] （Cursor 模式：降级为 SemanticSearch）
```

---

## 五条编码红线（兜底，禁止越线）

1. **异步优先**：IO 操作用 `UniTask`，**禁止**同步加载 / `Coroutine`
2. **模块访问**：统一走 `GameModule.XXX`，**禁止** `ModuleSystem.GetModule<T>()`
3. **资源必须释放**：`LoadAssetAsync` 对应 `UnloadAsset`；GameObject 用 `LoadGameObjectAsync`
4. **热更边界**：`GameScripts/Main/` 不热更，`GameScripts/HotFix/` 全部热更；依赖方向 `GameLogic → TEngine.Runtime` 单向
5. **事件解耦**：模块间用 `GameEvent`，UI 内部用 `AddUIEvent`

> 额外 asmdef 约束：所有引用 `GameLogic` 的 `.asmdef` 必须**同时**引用 `TEngine.Runtime`，否则 Source Generator 会编译失败（根因见 `/.claude/memory/problem_2026-04-22_asmdef-source-generator.md`）。

---

## Editor 操作优先级

涉及场景 / Prefab / 材质 / Shader / 动画 / ScriptableObject 时：

- **禁止**输出"请在 Unity Editor 里手动执行 A/B/C"这类让用户手动操作的步骤
- **必须**先读 [`.claude/skills/tengine-dev/references/unity-mcp-guide.md`](.claude/skills/tengine-dev/references/unity-mcp-guide.md) 评估 `manage_scene` / `manage_gameobject` / `manage_prefabs` / `manage_components` / `manage_script` / `batch_execute` 的自动化可能性
- 仅当 unity-mcp Bridge 不可用（MCP 状态错误 / 用户未开启 Bridge）时，才降级为**明确标注降级原因**的手动操作指南

---

## 豁免场景

以下情况**不触发**本协议，但仍需在回复首段声明豁免理由：

- L1 任务（typo / 单行注释 / 日志字符串 / 变量改名）
- 纯 docs / ADR / production 文档编写（不涉及代码或资产）
- 非 TEngine 的 .NET 标准库代码（纯算法、CRC32、JSON 序列化等，无 TEngine 依赖）
- 纯 EditMode 单元测试且被测代码无 TEngine 依赖

---

## 违反自检

发现任何违反（未读 SKILL.md 就写代码 / 未评估 unity-mcp 就给手动步骤 / L3+ 未查 wiki / 未出已读声明）时：

1. 立即写入工程根 `/.claude/memory/problem_YYYY-MM-DD_tengine-skill-violation.md`（格式参考现有同名文件）
2. 回滚或暂停已产出的代码 / 资产改动
3. 补齐阅读后重新产出
4. **同一会话内二次违反**：无条件中止任务，向用户明确报告

---

## 真相源索引

| 用途 | 路径 |
|------|------|
| 本工程 AI 协议原文（完整版）| [`CLAUDE.md`](CLAUDE.md) |
| TEngine 技能导航 + references | [`.claude/skills/tengine-dev/SKILL.md`](.claude/skills/tengine-dev/SKILL.md) |
| 深度 API / 架构知识库 | [`repowiki/zh/content/`](repowiki/zh/content/) |
| Cursor 硬规则镜像 | [`../../../.cursor/rules/shadowgame-tengine.mdc`](../../../.cursor/rules/shadowgame-tengine.mdc) |
| 已发现的 TEngine 陷阱 | [`/.claude/memory/`](../../../.claude/memory/) |
