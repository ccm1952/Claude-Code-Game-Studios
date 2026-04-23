# CLAUDE.md

请使用中文写提案和回答
这个文件为 Claude Code (claude.ai/code) 提供指导，用于处理此代码库中的代码。

TEngine 基于 HybridCLR + YooAsset + UniTask + Luban 构建。

---

## ⚠️ 跨工具镜像提醒（升级本文件时必读）

本协议（尤其是「🎯 TEngine 任务前置协议」与「⚡ 强制工作流」）同时被以下文件镜像，修改本文件时**必须同步更新**，否则会发生工具间行为漂移：

| 镜像文件 | 角色 | 同步策略 |
|---------|------|---------|
| [`.cursor/rules/shadowgame-tengine.mdc`](../../../.cursor/rules/shadowgame-tengine.mdc) | Cursor 硬强制规则（按 glob 自动挂载）| **完整镜像** — 协议叙述、红线、违反流程都需跟随本文件更新 |
| [`AGENTS.md`](AGENTS.md) | 跨工具短入口（Cursor / Claude Code / Codex 等）| **仅同步五条红线 + 关键指针**；协议叙述不重复抄录 |
| [`../../../AGENTS.md`](../../../AGENTS.md) | 工作区根入口 | **通常无需同步**；仅当工作区布局变化时更新 |

**单一真相源**：本文件（`src/MyGame/ShadowGame/CLAUDE.md`）。其他文件是派生镜像。

**升级工作流**：
1. 先改本文件（真相源）
2. 完整同步 `.cursor/rules/shadowgame-tengine.mdc`
3. 检查 `AGENTS.md` 的五条红线与本文件是否一致
4. 在 commit message 里标注"同步镜像：yes/no"

---

## 🎯 TEngine 任务前置协议（MUST）

> **强制度**：MUST — 触发条件下必读，未完成前置阅读不得写代码 / 改代码 / 操作资源。
> **本协议是「⚡ 强制工作流」的前置步骤**，完成后方可继续现有工作流（L1-L4 分级）。

### 触发条件

任务涉及以下任一项即触发（无论等级）：

- 使用 / 修改 TEngine 模块（`GameModule.*` / `ModuleSystem` / `UIWindow` / `UIWidget` / `ProcedureBase` / `GameEvent` 等）
- 读写 HotFix 程序集（`Assets/GameScripts/HotFix/**`）
- 操作 Unity Editor 资产（场景 `.unity` / Prefab / 材质 / Shader / 动画 / ScriptableObject）
- 需要通过 unity-mcp 自动化 Editor 操作
- 涉及 YooAsset 资源加载 / 热更 / 包策略
- 涉及 HybridCLR 程序集边界或 AOT 限制
- 涉及 Luban 配置表生成或访问
- 涉及 UniTask 异步规范

### 三级阅读策略

| 层级 | 何时读 | 阅读对象 | 成本 |
|------|-------|---------|------|
| **L-0 导航**（所有 TEngine 任务必读） | 任务开始前 | [`.claude/skills/tengine-dev/SKILL.md`](.claude/skills/tengine-dev/SKILL.md) | 极低（~50 行） |
| **L-1 Editor 操作**（涉及场景 / 资产 / MCP 时追加） | 需要操作 Unity Editor 时 | `.claude/skills/tengine-dev/references/` 下对应文件：`scene-gameobject.md` / `script-asset-workflow.md` / `unity-mcp-guide.md` / `ui-prefab-builder.md` / `material-shader-vfx.md` / `editor-automation.md` | 低（100-300 行） |
| **L-2 深度 API**（L3 / L4 任务追加） | 需要深度 API 规范时 | 调用 `wiki-query-agent` 查询 `repowiki/zh/content/` | 中（subagent 独立上下文） |

### 强制执行检查点

1. **进入 TEngine 任务第一步**：必须在 `TodoWrite` 的**第一项**添加 TEngine 前置阅读 todo（措辞可灵活，只要明确指向 SKILL.md 的阅读动作）。该项完成前，其他 todo 一律处于 `pending`。

2. **输出代码 / 方案前必须声明**：基于 SKILL.md 导航表判断是否需要 L-1 / L-2，并在回复中以类似格式声明：
   ```
   已读: SKILL.md + [references/xxx.md ...]
   未读: [说明为何跳过某些 reference]
   ```

3. **涉及 Editor 资产操作时**：禁止生成"让用户手动在 Unity Editor 里操作"的步骤，必须优先评估 unity-mcp 自动化可能性（读 `unity-mcp-guide.md`）。仅当 unity-mcp Bridge 不可用（MCP 状态错误或用户未开启 Bridge）时才降级为手动操作指南。

### 违反记录（自检机制）

发现违反本协议（未读 SKILL.md 就写代码 / 未评估 unity-mcp 就给手动步骤 / L3+ 任务未调 wiki-query-agent）时：

1. **立即写入** 工程根 `/.claude/memory/problem_YYYY-MM-DD_tengine-skill-violation.md`，格式参考 `/.claude/memory/problem_2026-04-22_asmdef-source-generator.md`。
2. **当前任务重启**：回滚或暂停当前代码产出，先补齐阅读，再重新产出。
3. **同会话内二次违反**：无条件中止任务，主动向用户报告。

### 边界说明（豁免情况）

以下情况**不触发**本协议，仍走现有 L1-L4 流程：

- **L1 任务**：typo 修正 / 单行注释 / 日志字符串修改
- **纯 docs / ADR / production 文档编写任务**（不涉及代码或资产）
- **非 TEngine 的 .NET 标准库代码**：纯算法、CRC32、JSON 序列化、数据结构等
- **纯 EditMode 单元测试且被测代码无 TEngine 依赖**

---

## ⚡ 强制工作流（所有任务必须遵守）

> **禁止跳过** — 无论任务大小，必须按此顺序执行：

### 第零步：判断任务等级（新增）

在执行任何操作前，先判断任务等级：

| 等级 | 判断标准 | wiki 查询策略 | 声明步骤 |
|------|---------|-------------|---------|
| **L1 简单** | typo 修正、注释修改、日志输出、单行变量改名 | ❌ 跳过查询 | ❌ 跳过 |
| **L2 调用** | 调用已知 API、单一模块的局部修改 | ✅ 轻量查询（只查该 API） | 可选 |
| **L3 功能** | 新功能开发、跨文件修改、新增 UI/资源/事件逻辑 | ✅ 全量查询 | ✅ 必须 |
| **L4 架构** | 模块设计、系统重构、多模块协作、架构决策 | ✅ 并行多主题查询 | ✅ 必须 |

> **判断原则**：宁可高估等级，不可低估——不确定时上调一级。

---

### 第一步：按等级查询 Wiki（使用 wiki-query-agent）

**L1 任务直接跳到第三步。L2-L4 必须先调用 `wiki-query-agent`。**

**核心规则**：wiki 文档内容**必须经由 wiki-query-agent 处理后引用**，不得将原始文档大段复制到主 Agent 上下文中（目的：保持主 Agent 上下文干净，专注代码生成）。

#### 会话内缓存（避免重复查询）

同一会话中已查询过的主题无需重复查询：
- 直接引用本次会话已获取的规范摘要
- 仅当任务涉及**本次会话未覆盖的新主题**时才启动新查询

```
示例：
会话内已查询 UIWindow 规范
→ 后续 UIWindow 相关任务：直接引用已有摘要，不重新查询
→ 后续涉及 GameEvent 的任务：UIWindow 摘要复用，仅补充查询 GameEvent
```

#### 触发时机

| 场景 | 必须查询主题 |
|------|------------|
| UI 开发 | UIWindow 生命周期、UIWidget 规范、资源加载释放 |
| 资源加载 | LoadAssetAsync API、释放时机、YooAsset 规范 |
| 热更代码 | HybridCLR 程序集划分、GameApp 入口、热更边界 |
| 事件系统 | GameEvent 用法、AddUIEvent 规范、事件解耦模式 |
| 模块使用 | GameModule.XXX API、模块生命周期 |
| Luban 配置 | 配置表生成流程、访问方式 |
| 代码规范 | 命名约定、设计模式、架构约束 |

#### 调用方式

```
使用 Agent 工具，subagent_type = "wiki-query-agent"
在 prompt 中描述需要查询的技术问题或功能点
```

#### 并行查询（L4 架构任务）

多主题时并行启动多个 wiki-query-agent，汇总后再编码：
```
同时查询 UI规范 + 资源管理规范 → 汇总后再编码
```

---

### 第二步：声明已查询文档（L3/L4 必须，L2 可选，L1 跳过）

在输出代码/方案前，列出：
- 已通过 wiki-query-agent 查询的主题（含本次会话复用的缓存主题）
- 关键规范摘要（来自 subagent 返回结果）

---

### 第三步：输出代码/方案

基于 wiki-query-agent 返回的规范编写实现。

**当 wiki 规范与代码实际 API 冲突时**：
1. 优先信任代码中的实际实现
2. 在输出中标注冲突点
3. 任务完成后触发 `/wiki:sync` 同步文档

---

## 核心原则（编码红线）

1. **异步优先**：IO 操作用 `UniTask`，禁止同步加载/Coroutine
2. **模块访问**：通过 `GameModule.XXX` 访问，而非 `ModuleSystem.GetModule<T>()`
3. **资源必须释放**：`LoadAssetAsync` 对应 `UnloadAsset`，GameObject 用 `LoadGameObjectAsync`
4. **热更边界**：`GameScripts/Main` 不热更，`GameScripts/HotFix/` 全部热更
5. **事件解耦**：模块间用 `GameEvent`，UI 内部用 `AddUIEvent`

---

## 📚 Wiki 知识库

> **唯一权威来源：`repowiki/zh/content/`**

Wiki 目录索引：[repowiki/zh/content/index.md](repowiki/zh/content/index.md)

**主要模块覆盖**：核心架构 / 模块系统 / 资源管理 / 热更新 / 事件系统 / UI系统 / 音频 / 本地化 / 流程管理 / 配置系统 / 内存管理 / 性能优化 / API参考

---

## wiki-query-agent 使用规范

### 为什么必须用 subagent
- wiki-query-agent 在独立上下文中运行，**不占用主 Agent 上下文窗口**
- 大量文档内容由 subagent 处理后，只返回精华摘要给主 Agent
- 保持主 Agent 上下文干净，专注于代码生成

---

## 补充文档参考（技能文档）

详细技能文档见 `.claude/skills/tengine-dev/references/`（仅供 wiki-query-agent 内部查阅）：

| 文档 | 内容 |
|-----|------|
| architecture.md | 项目结构/启动流程 |
| modules.md | 模块 API（Timer/Scene/Audio/Fsm）|
| ui-development.md | UI 开发 |
| event-system.md | 事件系统 |
| resource-management.md | 资源加载 |
| hotfix-development.md | 热更代码 |
| luban-config.md | 配置表 |
| conventions.md | 代码规范 |
| troubleshooting.md | 问题排查 |
| unity-mcp-guide.md | MCP 工具索引 |
| ui-prefab-builder.md | UI Prefab 拼接 |
| scene-gameobject.md | 场景/GameObject 操作 |
| script-asset-workflow.md | 脚本/资源管理 |

---

## 🔧 自我优化机制

### 问题记录
发现问题时记录到 `.claude/memory/`：
- 文件名：`problem_YYYY-MM-DD.md`
- 内容：问题现象、原因分析、解决方案

### 自动触发文档同步的条件（主动检测，无需人工判断）

以下任一情况**自动触发** `/wiki:sync`，无需等待用户指令：

| 触发条件 | 说明 |
|---------|------|
| wiki 规范与代码实际 API 不符 | 以代码为准，更新 wiki |
| 代码中存在 wiki 未覆盖的新 API/模式 | 补充 wiki 文档 |
| wiki 描述的类/方法在代码中已不存在 | 删除或修正 wiki 条目 |
| 同一问题在 `.claude/memory/` 出现两次以上 | 说明根因是 wiki 缺失，补充文档 |

### 手动触发文档同步
```bash
/wiki:sync
```
