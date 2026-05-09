// 该文件由Cursor 自动生成

# ADR-028: TEngine 模块使用边界决策（Module Usage Policy）

## Status

Accepted — 2026-04-29

## Date

2026-04-29

## Last Verified

2026-04-29

## Decision Makers

Technical Director, Lead Programmer

## Summary

明确项目对 TEngine 6.0.0 框架 13 个核心子系统（`Runtime/Core` 7 项 + `Runtime/Module` 9 项 + `Runtime/Extension` 4 项）的使用边界。每个子系统按 **必用 / 可选 / 不用** 三档分类，并附触发条件和理由。同时，针对项目内 **3 个自建 FSM**（`SingleFingerFSM` / `SceneManager` / `InteractableObjectFsm`）正式确立"事件驱动 FSM 例外名单"，与 `GameApp.GameFlow`（走 `FsmModule`）形成清晰二分。

本 ADR 闭环 2026-04-29 治理 session 暴露的"模块使用规则未文档化"治理 gap：触发原因是 S2-08 实现完成后用户提问"为何未用 `TEngine.FsmModule`？是否其他模块也有类似重复造轮子？"。审计结果（`tengine-module-usage-audit-2026-04-29.md`）确认无重复造轮，但**前期取舍未 ADR 化**——本 ADR 即为该治理空白的正式补充。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Architecture / Framework Governance |
| **Knowledge Risk** | NONE — 基于已读源代码（`Assets/TEngine/Runtime/`）的事实陈述 |
| **References Consulted** | `docs/architecture/tengine-module-usage-audit-2026-04-29.md`（同源审计文档）；TEngine 6.0 模块源码 `Assets/TEngine/Runtime/Core/*` + `Assets/TEngine/Runtime/Module/*`；`Assets/GameScripts/HotFix/GameLogic/GameModule.cs` + `GameApp.cs`（项目使用样本）；**TEngine skill 体系**：`src/MyGame/ShadowGame/.claude/skills/tengine-dev/SKILL.md` + `references/{architecture,modules,conventions,event-system}.md`（2026-04-29 evening 交叉验证：§1 13 子系统决策表与 `references/architecture.md` §"核心模块列表"完全对齐；§3 自建 FSM 例外名单与 `references/conventions.md` §"模块设计规范"——"仅在需要帧驱动的模块才继承 Module"——隐含取向一致；发现 `references/modules.md` §FsmModule + §ObjectPool 自身 2 处 bug 已记入 tech-debt TD-7 / TD-8 并同步修复 skill 文档） |
| **Post-Cutoff APIs Used** | 无新 API；全部模块均在 TEngine 6.0 既定 API 范围 |
| **Verification Required** | (1) `rg "GameModule\.<Name>"` 实际调用应符合 §4 表格；(2) 任何新增的"自建 FSM"必须在 PR 描述中显式引用 §3 例外名单或本 ADR 修订流程 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-001（TEngine 框架选型）；ADR-005（YooAsset Lifecycle）；ADR-009（Scene Lifecycle）；ADR-013（Object Interaction）；ADR-027（GameEvent Interface Protocol） |
| **Supersedes** | 无（治理空白补充） |
| **Updates** | ADR-013 §"Alternative 2"（同步修订事实错误，2026-04-29 v3） |
| **Enables** | 后续 sprint 模块决策（ADR-017 Audio / 未来 i18n / ObjectPool / Debugger 接入）的"是否必须用 TEngine 模块"答案直接引用本 ADR §4 表格 |
| **Blocks** | 任何新增"自建 FSM"或"自建 X 模块"PR，必须先比对本 ADR §3/§4 决策；未列入例外名单的需要先发起本 ADR 修订 |

## Context

### Problem Statement

2026-04-29 用户在 S2-08 Object Interaction State Machine 实现完成后提出治理质疑：

> "为什么 生成的 fsm 脚本没有使用 TEngine 框架里的 `Fsm.cs` 基类？我需要确定在开发和设计之前，有没有正确理解和指导 TEngine 的能力和提供功能，严禁避免重复造轮子。需要确保在其他模块有没有类似的问题。"

复盘发现：

1. **`ADR-013` Alternative 2 含事实错误**：写"FsmModule 强依赖 GameObject"，但 `Fsm<T>.Create` 仅约束 `where T : class`，不强制 `MonoBehaviour`。该错误来自前期对源码的不充分阅读。
2. **项目的 TEngine 模块使用规则从未明示**：现状是"按需开通"（`GameModule.cs` facade 暴露了 9 项，实际 `Resource/Scene/Fsm/Timer` 4 项有用例，其余 0 用例），但**何时该用、何时不该用、为何不该用**没有 ADR 锚点。
3. **3 个自建 FSM 的合理性同样未 ADR 化**：`SingleFingerFSM` / `SceneManager` / `InteractableObjectFsm` 都有充分的"为什么不用 FsmModule"理由，但散落在各 ADR/story 的实施备注，缺少集中决策点。

### Goals (本 ADR 必须达成)

- **G1**：对 13 个子系统给出 **必用 / 可选 / 不用** 决策，可被新成员直接查表。
- **G2**：对自建 FSM 列出 **正式例外名单**，固化"事件驱动 reducer 风格 vs procedure-style"二分原则。
- **G3**：建立 **新增模块（或新增自建轮子）的修订流程**，避免治理空白复发。

### Non-Goals

- 不重新讨论 ADR-005/009/013 等已有 ADR 的具体决策（本 ADR 只在元层面给出"模块使用边界"）。
- 不要求重构 3 个自建 FSM 为 FsmModule（决策即"保持自建"）。
- 不为未来可能新增的 TEngine 模块（TEngine 后续版本升级）提前决策 —— 由对应 sprint ADR 评估。

## Decision

### §1. 模块决策表（13 项）

> 决策档位定义：
> - **必用 (M)**：项目核心运行依赖此模块，**禁止**自建替代品。
> - **可选 (O)**：按 sprint 需要启用，启用后通过 `GameModule.<Name>` facade；不启用时 `_<name>` 字段保持 null。
> - **不用 (X)**：明确不引入；如未来需要 → 修订本 ADR。

#### Runtime/Core（基础设施）

| 子系统 | 决策 | 理由 |
|---|---|---|
| `Module` / `ModuleSystem` | **M** | 模块注册中心；`RootModule.Update` 驱动；项目无替代品 |
| `MemoryPool` | **M** | `Fsm<T>` / `EventDispatcher` 等 `IMemory` 实现间接依赖；不可回避 |
| `GameTime` | **M** | `RootModule.Update → ModuleSystem.Update(GameTime.deltaTime, GameTime.unscaledDeltaTime)` 已硬编码 |
| `GameEvent` (`EventMgr` / `EventDispatcher` / `[EventInterface]` Source Generator) | **M** | `ADR-027` 项目通信骨干；`Get<T>().OnXxx()` + `AddEventListener<T>(I*Event_Event.OnXxx, ...)` 是唯一允许的跨模块通信通道 |
| `Log` | **M** | 替代 `Debug.Log`；项目代码 100% 已用 `Log.Info/Warning/Error/Assert` |
| `Utility` (Text/Json/Assembly/Marshal/Converter/Unity) | **M** | `Utility.Unity.AddDestroyListener`、`Utility.Json` 等已在用 |
| `Constant` / `DataStruct` (`EEventGroup` / `TypeNamePair` 等) | **M** | 框架内部依赖 |

#### Runtime/Module（功能模块）

| 模块 | 决策 | 启用条件 / 理由 |
|---|---|---|
| `ResourceModule` | **M** | `ADR-005` Accepted；`GameModule.Resource` 标准入口；UI / Config / Spike 已大量间接使用 |
| `SceneModule` | **M** | `ADR-009` Accepted；`SceneManager.cs` Story 002 接入 `LoadSceneAsync`；SP011 spike 已使用 |
| `FsmModule` | **O** | **Procedure-style 顶层流程首选**（`GameApp.GameFlow` 已用）；事件驱动 reducer 风格走 §3 例外名单 |
| `ProcedureModule` | **X** | `FsmModule + GameFlowState`（自建）已覆盖；启用条件：未来需要 `RestartProcedure` 等 procedure 特化能力时再修订本 ADR |
| `AudioModule` | **O** | 启用条件：`ADR-017 audio-mix` Accept 且 Sprint 3 Audio 接入。**真接入 API**: `GameModule.Audio.Initialize(AudioGroupConfig[], Transform, AudioMixer)` (verified `IAudioModule.cs:79` 2026-05-06 Sprint 5 S5-06 readiness check #2)；本 ADR §1 + ADR-017 §B 历史描述涉及 `Activate()` 是 **framework behavior assumption drift** (Type-2(c) per ADR-029 V2.0)，不是真 framework API — 待 S5-06 dev-story 完成后或 Sprint 5 retro 一并 history note 修订全文 |
| `TimerModule` | **O** | 已在 `UIModule.cs:412` 使用；新增使用场景按需自由选择，无需修订 ADR |
| `LocalizationModule` | **O** | 启用条件：i18n sprint（当前简体中文单语言，`design/CLAUDE.md`） |
| `ObjectPoolModule` | **O** | 启用条件：章节 8/9/10 大场景对象量超过 50 时再评估；当前章节 ≤10 puzzle objects 不需要 |
| `DebuggerModule` | **O**（建议）| 启用条件：Sprint 3 Polish 阶段为 perf-profile / soak-test 接入 in-game stats overlay |
| `DataSaveModule` (`PPData` / `DataBase`) | **O** | 启用条件：Settings sprint 用 `PPData` 存音量等小 KV；游戏存档继续走 `ADR-008` 自建（`PPData` 能力不足以支撑多 slot + JSON + 完整性校验） |
| `Settings` (`Settings.cs`) | **M** | `RootModule` 配置入口 |
| `UpdataDriver` (`UpdateDriver.cs`) | **M** | `RootModule` 即唯一驱动 |

#### Runtime/Extension（扩展层）

| 扩展 | 决策 | 理由 |
|---|---|---|
| `Json` (`Utility.Json`) | **M** | Save / Config 间接使用 |
| `Tween` | **X** | 项目统一 **DOTween**（独立第三方）；`ADR-013` 隐含；本 ADR 显式声明禁止用 `TEngine.Tween` 以避免双 tween 库混用 |
| `Material` | **X** | Unity 原生 + `ADR-002` 自研 URP shadow rendering 覆盖；本 ADR 显式排除 |
| `Unity` (`Utility.Unity`) | **M** | `GameApp` `Utility.Unity.AddDestroyListener(Release)` 已在用 |

> 🔧 **Amendment 2026-05-09 (post Sprint 5 S5-06 dev-story done 2026-05-08)**：本 §1 表格中 AudioModule 行 (line 103) 标注的 "**真接入 API**: `GameModule.Audio.Initialize(AudioGroupConfig[], Transform, AudioMixer)`" 已被 **drift-v2-(a) supersede**。
>
> **当前现行约束**（Sprint 5 S5-06 dev-story v3 / 2026-05-08 实证）：
> - `AudioModule.OnInit()` 框架内已自动 `Initialize(Settings.AudioSetting.audioGroupConfigs)`（`AudioModule.cs:322-326`）
> - 业务侧 `GameApp.Entrance` **禁止**手动调 `GameModule.Audio.Activate()` 或 `GameModule.Audio.Initialize(...)`
> - 业务侧仅调 `AudioManager.Instance.Initialize()`（项目层 facade）
>
> **演化链**（双重 supersede，保留作决策史）：
> 1. v1（ADR-028 §1 + ADR-017 §B 原文）：`Activate()` 假设
> 2. drift-v1（line 103 当前文字）：纠正为 `Initialize(AudioGroupConfig[], ...)` 真接入 API
> 3. **drift-v2-(a)（本 Amendment）**：进一步纠正为 framework 自动 OnInit-Initialize，业务侧禁止手动 Initialize
>
> **table line 103 末尾原本挂的"待修订全文"待办本次闭环**：表格原文不动（保留决策史），Amendment 为现行权威约束；ADR-017 §B 同步加 Amendment。
>
> **真相源**：
> - `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/modules.md` 「drift-v2-(a) ✅ 现行约定」
> - `src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/GameApp.cs:35-37`
> - `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/hotfix-development.md` 「热更入口 GameApp」节（已对齐 2026-05-09）
> - PlayMode 实证：`production/qa/playmode-audio-mix-architecture-2026-05-08.md`

### §2. `GameModule` Facade 强制约定

- **范围限定**：本约定**仅适用于 `Assets/GameScripts/HotFix/` 子树**（即热更程序集 `GameLogic` / `GameProto`）。主包程序集（`Assets/GameScripts/Main/` 的 `GameEntry.Awake` + `Procedure*` 启动流程）按 TEngine 框架原生模式合法直调 `ModuleSystem.GetModule<IXxxModule>()`（参见 `tengine-dev/references/architecture.md` §"启动流程"），**不**在本约定范围内。
- **统一入口**：HotFix 子树内所有 TEngine 模块的取用必须经 `Assets/GameScripts/HotFix/GameLogic/GameModule.cs` 暴露的属性（如 `GameModule.Resource`、`GameModule.Fsm`），**禁止**在 HotFix 中直接调用 `ModuleSystem.GetModule<I*Module>()`。
- **验证 grep**：CI 可加 `rg "ModuleSystem\.GetModule<I.*Module>" src/MyGame/ShadowGame/Assets/GameScripts/HotFix` —— 期望 0 命中（**注意**：路径仅限 HotFix；不要扫整个 `GameScripts/`）。当前快照（2026-04-29）：0 命中，符合。
- **新增 facade 字段**：当某 §1 表中决策为"O 启用条件成立"时，需要在 `GameModule.cs` 增加对应 `public static IXxxModule Xxx => _xxx ??= Get<IXxxModule>();` 字段 + `Shutdown()` 中清空 `_xxx = null;`。**已知 gap**：`GameModule.cs` 当前**未暴露 `ObjectPool` 字段**（已记入 `tech-debt-2026-04-29.md` TD-8），但 skill `modules.md` §ObjectPool sample 错误使用了 `GameModule.ObjectPool.Spawn(...)`——HotFix 代码当前若需要对象池，请按 `IObjectPoolModule` 真实 API 通过 `ModuleSystem.GetModule<IObjectPoolModule>()` 直调（属于本约定 §"启用条件未到"暂时例外），或先补 facade 字段。

### §3. 自建 FSM 例外名单（事件驱动 reducer 风格）

下列 FSM **正式列入例外名单**，允许保持自建（不用 `FsmModule`）。新增 FSM 时若申请进入本名单，需要在 PR 描述中引用本 ADR §3 + 给出"为什么不用 FsmModule"理由（参照下表的 Cons）。

| FSM 名 | 路径 | 风格 | 自建理由 | 与 FsmModule 的关键 trade-off |
|---|---|---|---|---|
| `SingleFingerFSM` | `HotFix/GameLogic/Input/SingleFingerFSM.cs` | High-frequency reducer / `in struct` ref / `ProfilerMarker` / zero-alloc | 输入管线 60 Hz × N 指；每帧 update 必须 zero-alloc；`ref TouchState` 传递避免 struct copy | FsmModule per-state class virtual call / `internal ChangeState` 强制状态类内切换，**与 zero-alloc + 外部 trigger reduce 模式严重冲突**；性能不可接受 |
| `SceneManager` | `HotFix/GameLogic/Scene/SceneManager.cs` | Event-driven trigger reducer (`OnRequestSceneChange` → 内部 `TransitionTo`) | 状态切换由 `ISceneEvent.OnRequestSceneChange` 单一外部 entry 驱动；需要 `IsTransitioning` / pending queue / `RecoverToIdle` 等业务属性；EditMode 测试已绿（S2-04/05/06 验证） | FsmModule 要求切换必须从 `FsmState.OnUpdate` 内部触发，外部 trigger 必须先写入 `owner` flag 再轮询；pending queue 在 `FsmState` 间传值需要 owner.SetData，绕路 |
| `InteractableObjectFsm` | `HotFix/GameLogic/ObjectInteraction/InteractableObjectFsm.cs` | Event-driven trigger reducer (6 trigger methods + C# `event StateChanged`) | 同 `SceneManager`；多对象场景每个对象一个 fsm，FsmModule 用 `(Type, name)` 区分需 `name = $"obj_{id}"` 字符串拼接；`event StateChanged` 提供 1:1 本地反馈（`ADR-011` 视觉反馈）比借全局事件更合适 | 见 `ADR-013 §Alternative 2` 修订版（2026-04-29 v3）的 6 项详细 Cons |

#### 二分原则（procedure-style vs trigger-reducer）

- **走 FsmModule（procedure-style，class-per-state）的特征**：
  - 顶层 / 长生命周期 / 单实例（如 GameFlow）
  - 状态切换主要发生在 `OnUpdate` 内部检查条件
  - 状态间允许丰富的 `OnEnter/OnLeave` 复杂初始化清理
  - **样板**：`GameApp.GameFlow`（`GameLoading → Lobby → LevelLoading → Gameplay → LevelEnd`）
- **走自建（trigger-reducer，enum + switch）的特征**：
  - 多实例 / 高频触发 / 短生命周期
  - 状态切换由外部事件 / 方法调用 trigger，**不在状态内部条件判断**
  - 需要 zero-alloc / `in struct` / `ProfilerMarker` / 1:1 本地 C# event 反馈
  - 状态数较少（≤6）且 trigger table 用 enum + switch 即可清晰表达
  - **样板**：`SingleFingerFSM` / `SceneManager` / `InteractableObjectFsm`

### §4. 治理流程（防止治理空白复发）

新增"自建轮子"或"启用新 TEngine 模块"时，必须遵循：

0. **Skill-first 原则（强制）**：在新建 ADR / 起草 Story / 实施任何 TEngine 相关代码**之前**，**必须先读** `Assets/Tests/../../.claude/skills/tengine-dev/SKILL.md` 入口 + 相关 reference（如设计 FSM 必读 `references/modules.md` §FsmModule + `references/architecture.md`）。本治理 session 复盘发现：`ADR-013 Alt 2 v2` 事实错误（"GameObject 依赖"）的根因之一就是**跳过了 skill 体系直接读源码**——虽然源码事实更权威，但 skill 提供"项目惯用模式"维度，两者交叉验证才能保证决策准确。**Skill 与源码冲突时**：skill 错（如 `modules.md` §FsmModule sample 引用 `internal ChangeState` 无法编译）→ 记入 tech-debt + ADR 中说明偏离；项目实施错（与 skill 冲突）→ 修代码或修 skill 二选一，由本 ADR 评审决定。
1. **新增自建 FSM**：PR 必须在描述中引用本 ADR §3 例外名单，并给出 trade-off 表格（参照 §3 模板）；如不符合 trigger-reducer 特征 → 必须用 FsmModule。
2. **启用 §1 表中"O 启用条件未到"的模块**：sprint 启动时由对应 ADR（如 `ADR-017` Audio 启用时）显式引用本 ADR，并增加 `GameModule` facade 字段；同时核对 `tengine-dev/references/modules.md` 对应章节是否需要更新（如本治理 §S1 / §S2 修复）。
3. **启用 §1 表中"X 不用"的模块**：必须先修订本 ADR，更新决策档位，并解释触发条件变化。
4. **任何 ADR 中讨论"是否用 TEngine X 模块"的 Alternative**：必须基于源码事实（`Assets/TEngine/Runtime/.../*.cs`）+ skill 文档 双重验证；两者冲突 → 先看源码，再修 skill。如有疑问先 grep + Read，再写 ADR。

### §5. 修订入口

本 ADR 是活文档，每次新增模块决策都应**追加 Revision History 行**而不是重写。`docs/architecture/architecture-traceability.md` 的索引同步更新。

## Alternatives Considered

### Alternative 1: 全部强制走 TEngine 模块（一律不允许自建）

- **Pros**: 极致一致；新成员只需学一套 API
- **Cons**: `SingleFingerFSM` 高频 + zero-alloc 路径无法用 FsmModule（性能不达标）；`SceneManager` / `InteractableObjectFsm` 重构成 FsmModule 代码量翻倍且语义更绕（详见 `ADR-013 Alt 2` 6 项 Cons）
- **Rejection Reason**: 性能 / 代码可维护性硬约束。"一致性"的收益不足以抵消重构 + 长期维护成本。

### Alternative 2: 不写 ADR，依赖 code review 自然约束

- **Pros**: 文档负担最小；灵活性最高
- **Cons**: 已被本次治理事件证伪：S2-08 实施时未充分阅读 `Fsm<T>.cs` 源码，导致 `ADR-013 Alt 2` 含事实错误；如无 ADR 锚点，未来同类决策仍会随性化
- **Rejection Reason**: 治理空白复发风险高；且 `ADR-013` 已经暴露事实错误需要修订，必须有元层 ADR 钉死决策

### Alternative 3: 按"全开 GameModule facade，谁先用谁负责"

- **Pros**: 技术栈最大化曝光
- **Cons**: 模块用时无 ADR 指导，极易"哪个看起来顺手就用哪个"，导致风格分裂；`AudioModule` 直接用 vs 走 `ADR-017` 抽象的取舍会反复争论
- **Rejection Reason**: §1 + §3 二分原则即解决该问题，无需"全开"。

## Consequences

### Positive

- **新成员快速查表**：碰到"该用 X 模块吗？"问题，本 ADR §1 直接给答案
- **PR 评审有 ADR 锚点**：自建 FSM PR 可被 reviewer 直接对照 §3 二分原则审查
- **`ADR-013` 事实错误闭环**：连同 §3 例外名单形成完整决策网
- **未来 sprint 模块启用决策路径明确**：`ADR-017` / i18n / `ObjectPool` 等启用工作只需更新 §1 表 + 增加 `GameModule` facade 字段，无需重新讨论原则

### Negative

- **本 ADR 是活文档**：每次启用新模块需追加 Revision History 行（治理负担）
- **§3 例外名单可能被滥用**：未来开发者可能套用"trigger-reducer 风格"作为"自建借口" —— 需要 reviewer 卡住"是否真满足 §3 二分原则的全部特征"

## Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| §3 例外名单被滥用 | MEDIUM | MEDIUM | PR 模板要求引用 §3 + 给出完整 trade-off 表格；reviewer 检查二分原则的"高频/外部 trigger/多实例/1:1 反馈"4 个特征 |
| §1 表过期（TEngine 升级新增模块） | LOW | LOW | sprint 启动时 architecture-review skill 重检索 `Assets/TEngine/Runtime/Module/` 目录，发现新模块时强制修订本 ADR |
| `MemoryPool` domain reload 残留状态污染测试 | LOW | LOW | EditMode 测试 `[SetUp]` 显式 `MemoryPool.ClearAll()`（如使用 FsmModule 路径） |

## Validation Criteria

- [x] 13 项 TEngine 子系统全部给出决策（§1 完整表格）
- [x] 3 个自建 FSM 列入例外名单 + trade-off 详述（§3）
- [x] 治理流程明确（§4 4 条）
- [x] 修订 `ADR-013` Alt 2 事实错误（同 session 完成，标 v3）
- [ ] `architecture-traceability.md` 索引添加本 ADR（**留作后续 entry**：与 `architecture.md` 同步更新）
- [ ] PR 模板增加"是否新增自建轮子？引用 ADR-028 §3"checkbox（**留作 devops sprint 任务**）

## GDD Requirements Addressed

无直接 GDD requirement —— 本 ADR 是元层架构治理决策。

## Decision Source

- 触发：用户在 S2-08 测试全绿后提出的治理质疑（2026-04-29）
- 方法论：先做 `tengine-module-usage-audit-2026-04-29.md` 全量审计，基于源码事实 + grep 数据再决策
- 决策依据：`Assets/TEngine/Runtime/` 源码（Read tool 已验证）+ `Assets/GameScripts/HotFix` 项目使用 grep 数据
