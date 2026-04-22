# CLAUDE.md

请使用中文写提案和回答
这个文件为 Claude Code (claude.ai/code) 提供指导，用于处理此代码库中的代码。

TEngine 基于 HybridCLR + YooAsset + UniTask + Luban 构建。

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
