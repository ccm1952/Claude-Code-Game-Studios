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
| [`AGENTS.md`](AGENTS.md) | 跨工具短入口（Cursor / Claude Code / Codex 等）| **仅同步七条红线 + 关键指针**；协议叙述不重复抄录 |
| [`../../../AGENTS.md`](../../../AGENTS.md) | 工作区根入口 | **通常无需同步**；仅当工作区布局变化时更新 |

**单一真相源**：本文件（`src/MyGame/ShadowGame/CLAUDE.md`）。其他文件是派生镜像。

**升级工作流**：
1. 先改本文件（真相源）
2. 完整同步 `.cursor/rules/shadowgame-tengine.mdc`
3. 检查 `AGENTS.md` 的七条红线与本文件是否一致
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
| **L-2 深度 API**（L3 / L4 任务追加） | references 不足以覆盖某 API 细节时 | `SemanticSearch` / `Grep` / `Read` 查 `repowiki/zh/content/`，或 `Read` 真实源码 `Assets/TEngine/Runtime/` | 中（按需精确读章节） |

### 强制执行检查点

1. **进入 TEngine 任务第一步**：必须在 `TodoWrite` 的**第一项**添加 TEngine 前置阅读 todo（措辞可灵活，只要明确指向 SKILL.md 的阅读动作）。该项完成前，其他 todo 一律处于 `pending`。

2. **输出代码 / 方案前必须声明**：基于 SKILL.md 导航表判断是否需要 L-1 / L-2，并在回复中以类似格式声明：
   ```
   已读: SKILL.md + [references/xxx.md ...]
   未读: [说明为何跳过某些 reference]
   ```

3. **涉及 Editor 资产操作时**：禁止生成"让用户手动在 Unity Editor 里操作"的步骤，必须优先评估 unity-mcp 自动化可能性（读 `unity-mcp-guide.md`）。仅当 unity-mcp Bridge 不可用（MCP 状态错误或用户未开启 Bridge）时才降级为手动操作指南。

4. **MCP 绑定项目归属校验（涉及 Editor 写操作时 MUST）**：在调用**任何有副作用**的 unity-mcp 工具（`scene-create` / `scene-save` / `gameobject-create` / `gameobject-destroy` / `gameobject-component-add` / `assets-*`（除 `assets-find` / `assets-refresh`）/ `editor-application-set-state isPlaying=true` / `script-update-or-create` / `script-delete` 等）之前，**必须先校验 MCP 绑定的 Unity Editor 进程打开的项目是否为 ShadowGame**。

   **校验方法优先级**（越靠前越可信）：

   1. ⭐ **首选**：检查 `Packages/manifest.json` — `grep "com.aibridge.unity" src/MyGame/ShadowGame/Packages/manifest.json`，**只有装了 Bridge 的项目才运行 MCP Bridge**。多 Unity 实例并存时看哪个项目装了这个 Package
   2. ⭐ **次选**：`script-read` 读一个 ShadowGame 独有的文件（如 `Assets/GameScripts/HotFix/GameLogic/Input/InputBlocker.cs`），能读到且内容匹配 → 绑定正确
   3. 🚫 **不可信**：`~/Library/Logs/Unity/Editor.log` 的 `-projectpath` — macOS 上多个 Unity 进程共享这一个 log 文件，后启动的进程会截断前者的日志。该参数**只反映最新启动的进程**，**不反映** MCP 绑定的进程

   **匹配结论处理**：
   - ✅ 匹配 → 继续执行
   - ❌ 不匹配 → **立即停止所有写操作**，告知用户 "MCP 绑定的 Unity 项目不是 ShadowGame，请在 Unity Hub 中切换项目（或把 Bridge Package 装到 ShadowGame 项目）"，等用户切换后重新校验，禁止绕过

   > 警示：`project-0-MyGameStudio-unity-bridge` 这个 MCP server 命名只是 Cursor 层面的 project-scoped 标识符，**不保证**当前 Unity 进程匹配该项目。MCP Bridge 绑定的是**装了 `com.aibridge.unity` Package 并运行中的 Unity Editor 进程**。详见 `/.claude/memory/problem_2026-04-23_wrong-unity-project.md`。

5. **只读优先原则（涉及 MCP 时）**：MCP 操作永远**先用只读工具探测**（`gameobject-find` / `scene-list-opened` / `editor-application-get-state` / `console-get-logs` / `script-read` / `assets-find`），确认项目身份 + 场景状态正确后，再用写操作。

6. **MonoBehaviour 挂载前置自检（涉及 `gameobject-component-add` 时 MUST）**：调用 MCP 添加 MonoBehaviour 组件前，必须先用 `Glob **/<类名>.cs` 确认目标类有**同名 .cs 文件**（见核心原则编码红线第 6 条）。**看到 "The associated script can not be loaded" 警告时**，优先排查顺序：① `Glob` 文件名匹配 → ② `console-get-logs` 编译错误 → ③ 命名空间冲突 → ④ 才考虑 Assembly / HybridCLR 层面问题（禁止从 ④ 开始猜）。

### 违反记录（自检机制）

发现违反本协议（未读 SKILL.md 就写代码 / 未评估 unity-mcp 就给手动步骤 / L3+ 任务 references 不足却没补充 repowiki 查询）时：

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

| 等级 | 判断标准 | 阅读策略 | 声明步骤 |
|------|---------|-------------|---------|
| **L1 简单** | typo 修正、注释修改、日志输出、单行变量改名 | ❌ 跳过 | ❌ 跳过 |
| **L2 调用** | 调用已知 API、单一模块的局部修改 | ✅ 读 SKILL.md + 1 个 reference | 可选 |
| **L3 功能** | 新功能开发、跨文件修改、新增 UI/资源/事件逻辑 | ✅ 读 SKILL.md + 多个 references | ✅ 必须 |
| **L4 架构** | 模块设计、系统重构、多模块协作、架构决策 | ✅ 多 references + 必要时 SemanticSearch repowiki | ✅ 必须 |

> **判断原则**：宁可高估等级，不可低估——不确定时上调一级。

---

### 第一步：按等级阅读 tengine-dev skill（取代旧 wiki-query-agent 流程）

> **v6.2.x sync 后**：上游已用 `tengine-dev` skill 取代 `wiki-query-agent` subagent，本工程已删除 `.claude/agents/wiki-query-agent.md`。统一走 skill references。

**L1 任务直接跳到第三步。L2-L4 必须读 SKILL.md + 对应 references。**

**核心规则**：禁止把 `repowiki/` 原始文档大段复制到主上下文。SKILL.md 16 个 references 已经是结构化精华，按需读章节即可。

#### 会话内缓存（避免重复查询）

同一会话内同一 reference 已读过则直接复用，仅当任务涉及未覆盖的新主题时再追加阅读。

#### 触发时机

| 场景 | 必读 reference |
|------|------------|
| UI 开发 | `ui-development.md` + `ui-prefab-builder.md` |
| 资源加载 | `resource-management.md` |
| 热更代码 | `hotfix-development.md` |
| 热更包/Manifest | `hotpatch-management.md` |
| 事件系统 | `event-system.md` |
| 模块使用 | `modules.md` |
| Luban 配置 | `luban-config.md` |
| 代码规范 | `conventions.md` |
| 排错 | `troubleshooting.md` |

#### L-2 深度查询（仅当 references 不足以覆盖时）

按以下顺序：

1. `Read` 真实源码 `src/MyGame/ShadowGame/Assets/TEngine/Runtime/<相关模块>/`（ground truth）
2. `SemanticSearch` 查 `src/MyGame/ShadowGame/repowiki/zh/content/`（按主题精确读）
3. `Grep` 关键 API / 类名定位具体文件

**禁止**：把整份 `repowiki/zh/content/<file>.md` 全文 Read 进上下文。

#### 并行查询（L4 架构任务）

多主题用同一回合内多次 `SemanticSearch` 并行启动，汇总后再编码。

---

### 第二步：声明已读（L3/L4 必须，L2 可选，L1 跳过）

在输出代码 / 方案前，列出：
- 已读的 SKILL.md / references / repowiki 章节（含本会话复用的缓存）
- 关键规范摘要

格式示例：

```
已读: SKILL.md + references/ui-development.md + references/event-system.md
会话缓存: [resource-management.md — 上一回合已读，复用]
未读: [hotpatch-management.md — 本任务不涉及热更]
任务等级: L3
```

---

### 第三步：输出代码/方案

基于 references / 真实源码 编写实现。

**当 references / wiki 规范与代码实际 API 冲突时**：
1. 优先信任代码中的实际实现
2. 在输出中标注冲突点
3. 任务完成后**修订对应 reference**（直接 edit `.claude/skills/tengine-dev/references/<file>.md`），不再走旧的 `/wiki:sync` 命令

---

## 核心原则（编码红线）

1. **异步优先**：IO 操作用 `UniTask`，禁止同步加载/Coroutine
2. **模块访问**：通过 `GameModule.XXX` 访问，而非 `ModuleSystem.GetModule<T>()`
3. **资源必须释放**：`LoadAssetAsync` 对应 `UnloadAsset`，GameObject 用 `LoadGameObjectAsync`
4. **热更边界**：`GameScripts/Main` 不热更，`GameScripts/HotFix/` 全部热更
5. **事件解耦**：模块间用 `GameEvent`，UI 内部用 `AddUIEvent`
6. **MonoBehaviour / ScriptableObject 文件命名（MUST）**：继承自 `UnityEngine.MonoBehaviour` 或 `UnityEngine.ScriptableObject` 的 **public 类必须单独放在一个与类名完全同名的 `.cs` 文件中**（大小写敏感）。违反时 Unity 不会注册该类为可挂载组件，Inspector 搜不到，场景反序列化时会显示"The associated script can not be loaded"（编译不报错，极具欺骗性）。
   - ❌ 禁止：一个 `.cs` 文件定义多个 MonoBehaviour
   - ❌ 禁止：MonoBehaviour 与其他 public 类共存一个文件
   - ✅ 允许：同一文件内 MonoBehaviour + 同命名空间下的 `private` / `internal` 辅助类型
   - **自检**：创建/修改 MonoBehaviour 文件后，`Glob **/类名.cs` 确认文件名匹配；若调 `gameobject-component-add` 挂组件后 Inspector 显示"can not be loaded"警告，**第一排查项就是此规则**（详见 `/.claude/memory/problem_2026-04-23_monobehaviour-filename-mismatch.md`）

7. **测试 / Spike / 开发诊断代码挂载（MUST）**：
   - ❌ 禁止：把业务 / 测试 / Spike 的 MonoBehaviour 静态挂到 `Assets/Scenes/main.unity`（冷启动场景）。`main.unity` **只挂 `GameEntry`**。
   - ❌ 禁止：在 `ProcedureStartGame` 之前、`GameApp.Entrance` 之外直接访问 `GameModule.*`（会拿到 null，只能用轮询兜底，是反模式）。
   - ✅ 所有热更域测试 / Spike / 诊断代码，必须实现 `GameLogic.DevTest.IDevSpike`，在 `GameApp.Entrance` 里通过 `DevBootstrap.Register(new XxxSpike())` 注册，并由业务 FSM 的 `DevTestState` 动态挂载。
   - ✅ 注册代码、Spike 实现、`DevTestState` 整文件必须用 `#if UNITY_EDITOR || DEBUG` 包裹，Release 包零残留。
   - ✅ 动态挂载方式：`new GameObject("Xxx_Runtime").AddComponent<XxxRuntime>() + DontDestroyOnLoad`（纯代码，不依赖 Prefab）。Spike 生命周期由自己管，默认保留 OnGUI 直到手动停 PlayMode。
   - **自检**：涉及"测试/Spike 跑不起来"、"要挂 MonoBehaviour 到场景"、"修改 main.unity" 之前，必读本条。

8. **C# 字符串字面量内的引号嵌套（MUST）**：写 Assert message / `Log.*` / Exception message / 任何包含中文术语引用的 C# 字符串字面量时，**禁止**直接在双引号包围的字符串内再用 ASCII `"` 嵌套——这会被编译器解析为字符串提前闭合，触发 `CS1003: Syntax error, ',' expected` 等连锁报错。
   - ✅ **首选**：术语引用改用中文标点 `『…』` / `「…」` / `《…》`（不与 C# 语法冲突且可读性最好）。例：`"走『已在格点』路径"`。
   - ✅ **次选**：用 `\"` 转义，或改用逐字字符串 `@"...""...""..."`。
   - ❌ **禁止**：`"浮点小偏差 → 走"已在格点"路径"`（嵌套未转义 ASCII 双引号）。
   - **自检**：写完任何带 `"..."` 的 C# 字符串字面量后，**视觉扫一遍** ASCII `"` 应成对出现；如果违反，立即用 `『』` 替换术语两端的引号。
   - **CS1003 / CS1525 / CS1026 类 "Syntax error, X expected" 在中文代码文件里出现时，第一排查项就是本条**——`Grep` 该行 `"` 计数是否成对（详见 `/.claude/memory/problem_2026-04-29_csharp-string-literal-nested-quotes.md`）。

---

## 📚 Wiki 知识库

> **唯一权威来源：`repowiki/zh/content/`**

Wiki 目录索引：[repowiki/zh/content/index.md](repowiki/zh/content/index.md)

**主要模块覆盖**：核心架构 / 模块系统 / 资源管理 / 热更新 / 事件系统 / UI系统 / 音频 / 本地化 / 流程管理 / 配置系统 / 内存管理 / 性能优化 / API参考

---

## 历史 wiki-query-agent 子代理（已废弃）

> **2026-05-08 v6.2.1 sync 后已删除** `.claude/agents/wiki-query-agent.md`。
>
> 上游 TEngine 在 6.2.1 已经把 wiki-query-agent subagent 替换为更轻量的 `tengine-dev` skill。本工程同步删除子代理文件，理由：
> 1. `tengine-dev` skill 的 16 个 references 已 cover 95% framework 知识，无需独立 subagent
> 2. Cursor 工具不支持 Claude Code 的 subagent，原本就是降级模式
> 3. 简化心智模型：references 不够 → 直接 SemanticSearch / Read repowiki
>
> 上下文压力问题改由 SemanticSearch 解决（按主题精确读章节而非整份文档）。

---

## 补充文档参考（技能文档）

详细技能文档见 `.claude/skills/tengine-dev/references/`（主 Agent 直接 Read，按 SKILL.md 导航表选择）：

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

### 自动触发 references 同步的条件（主动检测，无需人工判断）

以下任一情况**应主动修订** `.claude/skills/tengine-dev/references/<file>.md`：

| 触发条件 | 说明 |
|---------|------|
| references 与代码实际 API 不符 | 以代码为准，edit 对应 reference 文件 |
| 代码中存在 references 未覆盖的新 API/模式 | 补充章节到合适的 reference |
| references 描述的类/方法在代码中已不存在 | 删除或修正条目 |
| 同一问题在 `.claude/memory/` 出现两次以上 | 沉淀为 references 章节 + 在 `.cursor/rules/shadowgame-tengine.mdc` 加硬规则 |

### 修订流程

直接 edit `.claude/skills/tengine-dev/references/<file>.md`，不再走 `/wiki:sync` 命令（旧 wiki-query-agent 时代的产物，已废弃）。重大 framework 变更同步追加到 `.cursor/rules/shadowgame-tengine.mdc` 的「框架 vendor patch 硬规则」节。
