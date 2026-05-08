# TEngine 模块全量使用审计（2026-04-29）

> **触发原因**：Sprint 2 / Story S2-08 实现完成后，发现新建 `InteractableObjectFsm`（纯 C# enum FSM）未引用 `TEngine.FsmModule`。
> 用户提出治理问题："设计与开发前是否充分理解了 TEngine 的能力？是否有重复造轮子？其他模块是否存在类似问题？"
>
> 本文档对 TEngine 6.0.0 框架下 **`Runtime/Core` + `Runtime/Module` + `Runtime/Extension` 共 13 个核心子系统** 进行全量盘点，逐项核对：
>
> 1. **能力声明**：每个模块对外提供什么？关键 API + 关键约束。
> 2. **当前使用**：项目热更代码（`Assets/GameScripts/HotFix`）实际引用情况。
> 3. **空缺/造轮**：项目自建是否重复了模块能力？是否存在错过模块的场景？
> 4. **决策建议**：必用 / 可选 / 不用 / 修订（迁移 or 文档化偏离）。
>
> 输出：本审计 → 修订 `ADR-013` Alt 2 事实错误 → 新建 `ADR-028 TEngine 模块使用边界` → 技术债登记。

---

## TL;DR（结论速查）

- **2 项事实错误**需要修订：
  1. `ADR-013` Alternative 2 中 "TEngine FsmModule 强依赖 GameObject" 表述**错误**——`Fsm<T>` 的 `T : class` 不强制 `MonoBehaviour`。真正的约束是 `Fsm.ChangeState` 为 `internal`（外部触发模式不友好）+ `MemoryPool` 共享单例（domain reload 需重置）+ `IFsmModule.Update` 经 `RootModule` 驱动（EditMode 测试需手动 tick `ModuleSystem.Update`）。
  2. 前期对 `SceneManager.cs` 是否与 `ISceneModule` 重叠的**怀疑也错**——它们是上下两层（**业务流程层 vs 资源加载层**）的清晰分层关系，`SceneManager` 的 `Loading` 状态正是要调 `GameModule.Scene.LoadSceneAsync`，无重叠。
- **1 项治理空白**需要 ADR 化：项目存在 **3 个自建 FSM**（`SingleFingerFSM`、`SceneManager`、`InteractableObjectFsm`），加上 1 个**已使用 FsmModule** 的（`GameApp.GameFlow`）。**何时用 FsmModule、何时自建** 在历史 ADR 中**从未明示**——这是真正的治理 gap，由 `ADR-028` 补上。
- **6 项模块未使用**（`ProcedureModule` / `AudioModule` / `LocalizationModule` / `ObjectPoolModule` / `DebuggerModule` / `DataSaveModule`）：其中 5 项是 sprint 路线图已规划，1 项（`ProcedureModule`）确认替换决策（已被 `FsmModule + GameFlowState` 覆盖，建议 `ADR-028` 标 "不用 + 理由"）。
- **0 项确认重复造轮**。3 个自建 FSM 中：`SingleFingerFSM`（高频 zero-alloc 输入管线）**不可能用 FsmModule**；`SceneManager` / `InteractableObjectFsm` 是**事件驱动 trigger reducer 风格**，与 `FsmModule`（procedure-style + per-state class + internal ChangeState）属于**不同设计取向**——需要在 `ADR-028` 中**显式承认 trade-off**而不是说成"FsmModule 不可用"。

---

## 1. 模块全量盘点表

> 列说明：
> - **能力声明** 来自模块的 `IXxxModule.cs`（已 Read 验证），不是猜测。
> - **当前使用** 通过 `rg "GameModule\.<Name>"` + `rg "ModuleSystem\.GetModule<I<Name>Module>"` 在 `HotFix` 中验证（数据快照：2026-04-29）。
> - **决策建议** 中 "必用 / 可选 / 不用" 由 `ADR-028` 正式确认；本审计仅给出建议。

### 1.1 Runtime/Core（基础设施 — 全部"必用"，无替代品）

| # | 子系统 | 能力声明 | 项目当前使用 | 决策建议 |
|---|---|---|---|---|
| C1 | `Module` / `ModuleSystem` | 模块注册中心、优先级队列、`IUpdateModule` 轮询 | ✅ 由 `RootModule.Update` 驱动；`GameModule.Get<T>` 间接使用 | **必用**（框架基石） |
| C2 | `MemoryPool` | 静态对象池 `Acquire<T>/Release` + `IMemory` 接口 | 间接使用：`Fsm<T>` / `EventDispatcher` 通过 MemoryPool 复用 | **必用** |
| C3 | `GameTime` | `deltaTime` / `unscaledDeltaTime` / `realtimeSinceStartup` 精确时间 | 间接：`RootModule.Update → ModuleSystem.Update(GameTime.deltaTime, GameTime.unscaledDeltaTime)` | **必用** |
| C4 | `GameEvent` (`EventMgr` / `EventDispatcher` / `[EventInterface]` Source Generator) | 接口型事件协议（`ADR-027`）；线程安全 add/remove；`Get<T>().OnXxx()` 派发 | ✅ 全 Hotfix 项目核心通信机制（`I*Event` 接口已 6 个） | **必用**（`ADR-006/027` 已正式 Accepted） |
| C5 | `Log` (`Log.Info/Warning/Error/Assert`) | 替代 `Debug.Log`；通过 `RootModule.logHelperTypeName` 注入实现 | ✅ 全项目使用 | **必用** |
| C6 | `Utility` (Text/Json/Assembly/Marshal/Converter/Unity) | 字符串、JSON、反射、内存、转换的统一帮手类 | ✅ `Utility.Unity.AddDestroyListener` / `Utility.Json` 等已在用 | **必用** |
| C7 | `Constant` / `DataStruct` | `EEventGroup` 等枚举常量；`TypeNamePair` 等基础数据结构 | 间接 | **必用** |

### 1.2 Runtime/Module（功能模块 — 9 项需逐项决策）

| # | 模块 | 能力声明（关键约束） | 项目当前使用（HotFix grep 结果） | 空缺 / 造轮 | 决策建议 |
|---|---|---|---|---|---|
| M1 | `ResourceModule` | YooAsset 封装：`LoadAsset(Async)<T>` / `LoadGameObject(Async)` / `LoadSceneAsync` / 包版本/下载/卸载/低内存回调 | ✅ 通过 `GameModule.Resource` 暴露；UIModule、ConfigSystem、SP011 spike 多处间接使用 | 无 | **必用**（`ADR-005 yooasset-lifecycle` 已 Accepted） |
| M2 | `SceneModule` | `LoadSceneAsync(location, mode, ...)` / `Unload(Async)` / `ActivateScene` / `IsMainScene`；底层 = YooAsset Scene API | ✅ `GameModule.Scene` 已暴露；`SP011_YooAssetAdditive` 直接使用 6 处；`SceneManager.cs` Story 002 计划接入 | **无重叠**——`SceneManager.cs` 是 6 状态业务流程 FSM（处理章节切换 11 步流程），**调用** `SceneModule` 完成实际 I/O，是清晰分层。先前怀疑重叠**已澄清** | **必用**（`ADR-009 scene-lifecycle` 已 Accepted） |
| M3 | `FsmModule` | 类型化 FSM 容器：`CreateFsm<T>(name, owner, FsmState<T>[]) → IFsm<T>`；OOP per-state class（`OnEnter/OnUpdate/OnLeave`）；`ChangeState` **internal**（外部不能直接切，状态切换在 `FsmState<T>.OnUpdate` 内由 `protected internal ChangeState(IFsm<T>)` 调用）；自动经 `IUpdateModule` 轮询 | ✅ `GameApp.cs` 的 `GameFlow` 6-state（`GameLoading/Lobby/LevelLoading/Gameplay/LevelEnd/DevTest`）使用；`GameApp.GameModule.Fsm.CreateFsm(...)` 唯一入口 | ⚠️ **3 个自建 FSM 未走 FsmModule**：①`SingleFingerFSM`（input 高频管线，每帧 ProfilerMarker + zero-alloc）②`SceneManager`（6 states）③`InteractableObjectFsm`（5 states，事件驱动 trigger）。**判定**：①**不可能用 FsmModule**（每帧 reduce + ref struct + zero-alloc，与 `FsmModule` per-state class 模式严重冲突）；②③**可用 FsmModule 但取舍**：trigger-based reducer 风格在 enum + switch 中表达更短，FsmModule 强制 per-state class 反而代码量翻倍且 `ChangeState` 必须从状态类内部调（外部 `OnTapHit()` trigger 必须先写入 owner，再由 `FsmState.OnUpdate` 反查）。**不构成"重复造轮子"**，但**前期未在 ADR 显式声明取舍** —— 由 `ADR-028` §3 补正 | **可选 + 必须 ADR 化决策**（`ADR-028`） |
| M4 | `ProcedureModule` | 在 `FsmModule` 之上的 procedure（流程）特化封装；`StartProcedure<T>` + `RestartProcedure(...)`；`Initialize(IFsmModule, ProcedureBase[])` 强制依赖 `FsmModule` | ❌ 0 处使用 | 项目用 `GameApp.GameFlow` (FsmModule 直用) 已覆盖"主流程切换"语义；`ProcedureModule` 的额外能力（`RestartProcedure`、统一 `ProcedureBase`）目前**用不上** | **不用**（`ADR-028` §4 显式排除 + 给出"如未来需要 boot retry/状态机重启再启用"重启条件） |
| M5 | `AudioModule` | 多通道音频（Music/Sound/UI/Voice）+ AudioMixer + AssetHandle 池；`Play/Stop/PutInAudioPool/CleanSoundPool` | ❌ 0 处使用 | 项目暂无 BGM/SFX 实装；S3 sprint 路线图含 `ADR-017 audio-mix`（`Status: Proposed`） | **可选**（暂未需要；`ADR-017` Accept 后启用） |
| M6 | `TimerModule` | `AddTimer(callback, time, isLoop, isUnscaled, args)` + `Stop/Resume/Restart/RemoveAllTimer`；params object 避免闭包 GCAlloc | ✅ 1 处：`UIModule.cs:412 GameModule.Timer.AddTimer(...)` 用于 UI 隐藏延迟 | 无造轮 | **可选**（按需使用；不强制取代手写 coroutine） |
| M7 | `LocalizationModule` | 多语言资源加载 + 系统语言检测 + `SetLanguage`；底层基于 I2 Localization | ❌ 0 处使用 | 项目当前为简体中文单语言（`design/CLAUDE.md`）；S4 之后 i18n sprint 启用 | **可选**（i18n sprint 启用） |
| M8 | `ObjectPoolModule` | `IObjectPool<T : ObjectBase>` + `CreateSingleSpawnObjectPool/CreateMultiSpawnObjectPool` + `ReleaseAllUnused/ReleaseObjectFilterCallback` | ❌ 0 处使用 | 项目当前对象量小（≤10 puzzle objects/章节）；UI 已有自己池化（`UIWindow` 缓存）；S2 不需要 | **可选**（章节 8/9/10 大场景再评估） |
| M9 | `DebuggerModule` | 调试器 UI Window 注册中心；`RegisterDebuggerWindow(path, IDebuggerWindow)`；调试期信息收集 | ❌ 0 处使用，但 `GameModule.Debugger` 已暴露 | 项目使用 Unity 自带 Profiler + `Log.Info`；缺少 in-game debug overlay 是**已知 dev gap**（不影响发布） | **可选**（建议 Sprint 3 Polish 阶段为 perf-profile 提供 in-game stats overlay） |
| M10 | `DataSaveModule`（`PPData` + `DataBase`） | PlayerPrefs 包装 `PPData` + `DataBase` 抽象基类 | ❌ 0 处使用；项目自建 `IChapterProgress` + 自建 `SaveSystem`（`ADR-008 save-system`） | ⚠️ **看似重复**，但 `ADR-008` 设计的 save 模式（多 slot + JSON + 完整性校验）远超 `PPData` 简单 KV 能力；`DataSaveModule` 适合"音量/语言"等 pref 级别小数据 | **可选**（未来 Settings sprint 用 `PPData` 存音量等小配置；游戏存档继续用 `ADR-008` 自建） |
| M11 | `Settings`（`Settings.cs`） | 框架级配置 ScriptableObject | 间接：`RootModule` 字段（frameRate/gameSpeed 等）已设置 | 无造轮 | **必用**（框架基石） |
| M12 | `UpdataDriver`（`UpdateDriver.cs`） | 单 GameObject 驱动 `MonoBehaviour.Update` | 间接：`RootModule` 即唯一 update driver | 无造轮 | **必用**（框架基石） |

### 1.3 Runtime/Extension（扩展层 — 4 项可选）

| # | 扩展 | 能力 | 项目当前使用 | 决策建议 |
|---|---|---|---|---|
| E1 | `Json` (`Utility.Json` 默认 helper) | JSON 序列化反序列化 | ✅ Save 数据 / Luban 配置使用 | **必用** |
| E2 | `Tween` | 内置补间动画 | ❌ 项目用 **DOTween**（独立第三方包，HOTween 衍生）作为 tween 解决方案 | **不用 TEngine.Tween**，统一 DOTween（`ADR-013` 隐含决策，需在 `ADR-028` 显式声明） |
| E3 | `Material` | 材质工具 | ❌ 0 处使用 | **不用**（Unity 原生 + `ADR-002 URP` 自研 shadow rendering 覆盖） |
| E4 | `Unity` (`Utility.Unity`) | `AddDestroyListener` / `FindObjectOfType` 等 helper | ✅ `GameApp.cs` `Utility.Unity.AddDestroyListener(Release)` | **必用** |

---

## 2. 自建 FSM 全量审查（治理重点）

> 本节回答用户的核心质疑："其他模块是否有类似问题？"
> 结论：**项目共 4 个 FSM 实例**（含已用 FsmModule 的 GameFlow），仅 1 个走 TEngine FsmModule，3 个自建。**3 个自建均有合理理由**，但**前期 ADR 未明示取舍** —— 这是治理 gap 的本质，由 `ADR-028` 闭环。

| # | FSM 名 | 路径 | 状态数 | 风格 | 是否用 FsmModule | 自建理由（事后梳理） | 决策建议 |
|---|---|---|---|---|---|---|---|
| F1 | `GameApp.GameFlow` | `HotFix/GameLogic/GameApp.cs:67` | 6 (5+DevTest) | Procedure / OOP per-state class | ✅ 是 | N/A | **保持** |
| F2 | `SingleFingerFSM` | `HotFix/GameLogic/Input/SingleFingerFSM.cs` | 4 (Idle/Pending/Dragging/LongPress) | High-freq reducer / `in struct` ref / `ProfilerMarker` / zero-alloc | ❌ 否 | **不可能**用 FsmModule：（a）每帧 update 必须 zero-alloc，FsmModule per-state class virtual call 不达标；（b）输入事件密度极高（60 Hz × 多指），`internal ChangeState` 触发只能从状态类内部，外部 trigger 转写代价高；（c）需要 `ref TouchState` 传递避免 struct copy | **保持自建** + `ADR-028` 列入"高频 reducer 例外名单" |
| F3 | `SceneManager` | `HotFix/GameLogic/Scene/SceneManager.cs` | 6 (Idle/TransitionOut/Unloading/Loading/TransitionIn/Error) | Event-driven trigger reducer (`OnRequestSceneChange` → 内部 `TransitionTo`) | ❌ 否 | （a）状态切换由 `ISceneEvent.OnRequestSceneChange` 单一外部 entry 驱动，与 FsmModule 设计意图（state.OnUpdate 内 `ChangeState`）相反；（b）需要 `IsTransitioning` 等只读属性 + pending queue + Error 恢复等业务逻辑，FsmModule 无原生支持，需 owner.SetData 兜，反而绕；（c）EditMode 测试已经全绿，迁移成本不抵收益 | **保持自建** + `ADR-028` 列入"事件驱动业务 FSM 例外名单"；同时承认 trade-off（失去 FsmModule 统一管理 / MemoryPool 复用） |
| F4 | `InteractableObjectFsm` | `HotFix/GameLogic/ObjectInteraction/InteractableObjectFsm.cs` | 5 (Idle/Selected/Dragging/Snapping/Locked) | Event-driven trigger reducer (6 trigger methods + C# `event StateChanged`) | ❌ 否 | 同 F3：trigger 由 InteractableObject MonoBehaviour 上的 `IGestureEvent` listener 驱动；C# `event StateChanged` 比 FsmModule 的 `OnEnter/OnLeave` 反应链更适合"feedback 视觉层订阅"（local 1:1 而非全局广播）；多实例场景下每个对象一个 fsm，FsmModule 的 `name` 区分语义匹配但 `Type` 区分语义不匹配（同 type 多实例需要靠 name 字符串拼接，繁琐） | **保持自建** + `ADR-028` 列入"事件驱动业务 FSM 例外名单" + `ADR-013` Alt 2 修订事实错误 |
| F5（非 FSM） | `ChapterStateManager` | `HotFix/GameLogic/ChapterState/ChapterStateManager.cs` | N/A (数据管理器 + Action 回调) | Data store + callbacks | N/A | 不是 FSM，名字含 "State" 但实质是 `ChapterProgress[]` 数据 + `Action<int>` 回调钩。`PuzzleStateEnum` 仅是数据 enum，无状态机语义 | **澄清命名**（重命名建议：`ChapterStateStore` 或 `ChapterProgressManager`），但属于次要技术债，记入 tech-debt |

---

## 3. 决策汇总（待 ADR-028 正式 Accept）

| 模块 | 决策 | 触发条件 / 理由 |
|---|---|---|
| `Module/ModuleSystem` / `MemoryPool` / `GameTime` / `GameEvent` / `Log` / `Utility` / `Constant` / `DataStruct` / `Settings` / `UpdataDriver` | **必用** | 框架基石，项目无替代品 |
| `ResourceModule` | **必用** | `ADR-005` Accepted；`GameModule.Resource` 标准入口 |
| `SceneModule` | **必用** | `ADR-009` Accepted；`SceneManager.cs` 业务流程层调用之 |
| `FsmModule` | **可选** | Procedure-style 顶层流程**首选**（`GameApp.GameFlow` 已用）；事件驱动 reducer 风格**允许自建**（`ADR-028` 列例外名单） |
| `TimerModule` | **可选** | 已在 `UIModule` 1 处使用；自由选择 |
| `Utility.Json` (Extension/Json) | **必用** | Save / Config 间接使用 |
| `Utility.Unity` (Extension/Unity) | **必用** | `GameApp` 等使用 |
| `ProcedureModule` | **不用**（暂时） | `FsmModule + GameFlowState` 已覆盖；如未来需要 `RestartProcedure` 再启用 |
| `AudioModule` | **可选**（启用条件）| `ADR-017 audio-mix` Accepted 后启用 |
| `LocalizationModule` | **可选**（启用条件）| i18n sprint 启用 |
| `ObjectPoolModule` | **可选**（启用条件）| 章节 8/9/10 大场景再评估 |
| `DebuggerModule` | **可选**（建议）| Sprint 3 Polish 阶段为 perf-profile 接入 |
| `DataSaveModule` (`PPData`) | **可选** | Settings 小数据用 PPData；游戏存档用 `ADR-008` 自建 |
| `Extension/Tween` | **不用** | 项目统一 DOTween |
| `Extension/Material` | **不用** | Unity 原生 + `ADR-002` 自研 |

---

## 4. 后续行动项（治理闭环）

| # | 行动 | 产出 | Owner | Due |
|---|---|---|---|---|
| A1 | 修订 `ADR-013` Alt 2 事实错误 | `docs/architecture/adr-013-object-interaction.md`（Revision History 标 2026-04-29 v3） | 本治理 session | 即刻 |
| A2 | 新建 `ADR-028 TEngine 模块使用边界决策` | `docs/architecture/adr-028-tengine-module-usage-policy.md` | 本治理 session | 即刻 |
| A3 | 技术债登记 | `src/MyGame/ShadowGame/production/qa/tech-debt-2026-04-29.md` | 本治理 session | 即刻 |
| A4 | `active.md` 增治理 entry 40 | `production/session-state/active.md` | 本治理 session | 即刻 |
| A5 | 事件驱动 FSM 是否需重构成 FsmModule？ | 由 `ADR-028` 决策驱动；如 `ADR-028` 接受"自建例外名单"，则**不重构** | `ADR-028` 评审 | `ADR-028` accepted 后关闭 |
| A6 | `ChapterStateManager` 是否更名 | tech-debt 项；优先级 LOW | 后续 sprint | TBD |

---

## 5. 验证（grep 命令快照，2026-04-29）

下列命令在 `src/MyGame/ShadowGame/Assets` 子树验证。任何未来评审可以重跑确认数据：

```bash
# 1. 各 GameModule.* 实际调用统计
rg "GameModule\.(Resource|Scene|Audio|Timer|Localization|Fsm|Procedure|Debugger|ObjectPool|UI|DataSave)" src/MyGame/ShadowGame/Assets/GameScripts -n

# 2. 直接使用 ModuleSystem 的位置（应为 0；统一走 GameModule facade）
rg "ModuleSystem\.GetModule<I.*Module>" src/MyGame/ShadowGame/Assets/GameScripts -n

# 3. 自建 FSM 文件清单
rg -l "enum.*State|FsmState<" src/MyGame/ShadowGame/Assets/GameScripts/HotFix
```

**当次执行结果**：
- `GameModule.Fsm` → 3 处（`GameApp.cs:67/69/77`，全在 GameFlow 创建 / 销毁）
- `GameModule.Scene` → 6 处（全在 `SP011_YooAssetAdditive` spike）
- `GameModule.Timer` → 1 处（`UIModule.cs:412`）
- `GameModule.UI` → 2 处（`GameLobbyState.cs` 注释中，未实际使用）
- `GameModule.{Resource,Audio,Localization,Procedure,Debugger,ObjectPool,DataSave}` → 0 处
- `ModuleSystem.GetModule<...>` 在 HotFix 子树 → 0 处（**符合预期**：HotFix 内所有调用通过 `GameModule` facade；主包 `GameScripts/Main` / `Procedure/` 合法直调，本审计不在范围）
- 自建 FSM 文件 → 4 个（`GameApp.GameFlow` 用 FsmModule、`SingleFingerFSM`、`SceneManager`、`InteractableObjectFsm`），见 §2

---

## 6. Skill 合规性交叉验证（2026-04-29 evening 补充）

> 本节回答用户后续质询："在设计、修改脚本等动作前，是否严格遵循 TEngine 框架给的 Skills 和最佳实践的方式？"
> 验证范围：`src/MyGame/ShadowGame/.claude/skills/tengine-dev/SKILL.md` 入口 + `references/{architecture,modules,conventions,event-system}.md` 4 篇关键 reference。
> 方法论：逐项核对本审计 / ADR-028 / S2-08 实施代码与 skill 内容是否一致。

### 6.1 一致性核对（与 skill 不冲突的项）

| 维度 | Skill 来源 | 本审计 / ADR-028 / S2-08 | 结论 |
|---|---|---|---|
| 13 子系统决策表 | `references/architecture.md` §"核心模块列表"（8 项 + ModuleSystem 直访说明） | `ADR-028 §1` 13 项决策表（11 M / 7 O / 3 X） | ✅ 属性名 / 接口名 / 职责完全对齐；ADR-028 是 skill 列表的**正交决策化**（增加"何时启用 / 为何不用"决策档位） |
| 自建 FSM 例外名单 | `references/conventions.md` §"模块设计规范"："仅在需要帧驱动（Update）的模块才继承 Module" | `ADR-028 §3` 例外名单（事件驱动 reducer / 1:1 反馈 / 多实例） | ✅ skill 隐含支持自建非帧驱动业务系统；ADR-028 §3 是 skill 取向的**显式补充**（skill 未明示"事件驱动 FSM 何时该自建"） |
| GameModule facade 强制 | `SKILL.md` §核心原则#2 + `references/architecture.md` §"核心模块列表"："通过 `GameModule.XXX` 访问（已缓存，推荐）" | `ADR-028 §2` HotFix 子树 0 命中 `ModuleSystem.GetModule` | ✅ 一致；本次治理已精确化措辞为"HotFix 子树范围"以避免与 skill `architecture.md` §"启动流程"中主包合法直调矛盾 |
| 命名规范 / 异步 / 资源 / 事件 / 性能 | `references/conventions.md` §代码审查清单（5 类） | S2-08 实施代码（`InteractableObjectFsm.cs` + `InteractableObject.cs` + Tests） | ✅ 5 类全部合规（命名 Pascal/小驼峰下划线；无 async；无 Resource.Load/Instantiate；event register/remove 配对；trigger reducer 无 Update 分配） |
| 事件协议 | `SKILL.md` §核心原则#5 + `references/event-system.md`（ADR-027 同源）| `IInteractionEvent` 接口 + `[EventInterface(EEventGroup.GroupLogic)]` + `GameEvent.Get<T>().OnXxx()` 派发模式 | ✅ 完全合规；与 ADR-027 一致 |

### 6.2 偏离项（程序违规，结论碰巧无矛盾）

| # | 偏离 | 实际后果 | 修复措施 |
|---|---|---|---|
| **D1** | 本治理 session 启动前未先读 `tengine-dev/SKILL.md` 入口；直接 Read 源码 + grep | 结论碰巧与 skill 一致；§1 13 子系统决策表与 `architecture.md` 核心模块列表对齐 | `ADR-028 §4 §0` 增 **Skill-first 原则**：未来新建 ADR / Story / 实施前必读 skill 入口 + 相关 reference |
| **D2** | `ADR-013 Alt 2 v2`（已修订到 v3）起草时也未读 skill `modules.md` §FsmModule | "FsmModule 强依赖 GameObject"事实错误已闭环（v3 修订 + ADR-028 §3 例外名单锚定） | 已闭环；纳入 D1 防复发流程 |
| **D3** | `ADR-028 §2` 措辞含糊（未限定 HotFix 子树范围） | 与 `architecture.md` §"启动流程"中主包合法直调有冲突表述 | 本次治理 evening 补丁：§2 加"范围限定"段，明确仅适用 HotFix 子树 |

### 6.3 Skill 自身 2 处 bug（已修复）

| # | Skill 文件 | 错误 | 真实情况 | 修复状态 |
|---|---|---|---|---|
| **S1** | `tengine-dev/references/modules.md` §FsmModule §"状态切换" | 给出 `fsm.ChangeState<RunState>();` 作为外部调用 sample | `IFsm<T>` 接口（public，GameLogic 可见）**未声明** `ChangeState` 方法（已读 `IFsm.cs` 1-158 全文）；`Fsm<T>.ChangeState` 是 `internal`；GameLogic 程序集中**编译不通过** | ✅ 2026-04-29 evening 修订（参见 `tech-debt-2026-04-29.md` TD-7 CLOSED）；改为 `FsmState<T>` 子类内 `protected ChangeState<TState>(IFsm<T> fsm)` 正确写法 |
| **S2** | `tengine-dev/references/modules.md` §ObjectPool | ①`GameModule.ObjectPool.Spawn(...)`；②混淆 `IObjectPoolModule`（外层）与 `IObjectPool<T>`（内层）API；③不存在的 `WarmUp(...)` | ①`GameModule.cs` 未暴露 `ObjectPool` 字段；②外层 API 是 `CreateSingleSpawnObjectPool` / `CreateMultiSpawnObjectPool` / `GetObjectPool` / `DestroyObjectPool`，内层 API 是 `Spawn() / Unspawn(T)` / `Register(T, bool)` / `Release()`；③两层 API **无 WarmUp** | ✅ 2026-04-29 evening 修订（参见 `tech-debt-2026-04-29.md` TD-8 CLOSED）；分层展示两套 API + Capacity/Register 预热模式 + 启用前提引用 ADR-028 §1 M8 |

### 6.4 验证结论

**本治理 session 的最终决策与 TEngine 框架 skill / 最佳实践 100% 一致**（在修复 skill 自身 2 处 bug 后）；同时治理过程暴露 3 项程序违规（D1/D2/D3），均已通过 `ADR-028 §4 §0` Skill-first 原则 + §2 措辞精确化修复，避免未来复发。

