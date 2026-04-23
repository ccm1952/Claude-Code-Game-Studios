# 问题 / 决策：GameEvent 协议从 const int 切换到 `[EventInterface]`（ADR-006 → ADR-027）

**日期**：2026-04-23
**影响面**：所有模块间事件通信（Input / Rendering / Settings，后续覆盖 Puzzle / Scene / Narrative / UI / Audio 全域）
**严重等级**：Critical 架构冲突（C1）
**状态**：Resolved（原子切换已完成）

---

## 1. 问题概述

L4 代码审计发现架构冲突 C1：项目同时存在两套事件系统：

1. **TEngine 官方推荐**：`[EventInterface(EEventGroup.GroupXxx)]` 标记 C# 接口，Roslyn Source Generator `EventInterfaceGenerator` 编译期生成 `IXxx_Event` 枚举（事件 ID = `RuntimeId.ToRuntimeId("IXxx_Event.OnMethod")`）。已有 `ILoginUI` 在使用。
2. **ADR-006 方案**：`public static partial class EventId { public const int Evt_Xxx_Yyy = 1000; }`，按系统 100-ID 段手动分配。已实现 `EventId_Input.cs` / `EventId_Rendering.cs`。

两套方案并行会导致：
- 未来接口事件生成的哈希值有概率落入 `1000–2299` 手分配区，造成运行期 ID 碰撞。
- IDE 不能对 `Evt_Xxx` 做 find-usages（监听/派发分散 → 维护地狱）。
- Payload 类型约束弱（`GameEvent.Send(id, obj)` 的泛型类型安全会在编译期丢失）。
- 新团队成员不知道该学哪套，调试时文档路径断裂。

## 2. 用户决策（2026-04-23）

1. **切换到 TEngine `[EventInterface]`**（选项 A，"推翻 ADR-006 的 ID 分配和 Payload 条款"）。
2. 手势事件采用 **"一手势一方法"** 设计（`IGestureEvent.OnTap/OnDrag/OnRotate/OnPinch/OnLightDrag`），不再按 `GestureType` 枚举分派。
3. Puzzle Lock token 协议**完整继承 ADR-006 §4**（`LockToken.Puzzle` / `LockToken.Narrative` / `LockToken.Tutorial` + `HashSet<string>` 语义），后续以 `IPuzzleLockEvent` 接口落地。
4. 项目自定义**分组约定**：`GroupUI` = UI 通知（面板展开/关闭、弹窗队列）；`GroupLogic` = 业务事件（手势、关卡状态、渲染、设置变更）。不再按系统名分段。
5. **原子切换**：一次性完成代码 + 文档 + 测试迁移，不做过渡期兼容层。
6. **不跳过** `/architecture-decision` 正式流程。
7. **同步记录**到 `.claude/memory/`（本文件）。

## 3. ADR 编号决策

起初拟为 **ADR-019**，但 `architecture-traceability.md §4` / `architecture.md` / 多份 EPIC 文件已预留 ADR-019 ~ ADR-026 作为 P2 Presentation Layer placeholder（ADR-019 = Tutorial Step Engine）。为避免大面积改动，最终编号为 **ADR-027**（P2 区段之后的首个可用编号）。

## 4. 最终实施内容（原子提交）

### 架构文档
- 新建 `docs/architecture/adr-027-gameevent-interface-protocol.md`（Accepted）。
- `adr-006-gameevent-protocol.md` 状态 → `Superseded by ADR-027`，保留 §3/§4/§5/§6（生命周期 + token 协议 + ordering + doc 约定）。
- `architecture-traceability.md` 新增附录 A — EventId → Interface 全量映射表（覆盖 Input/Puzzle/Chapter/Scene/Narrative/Audio/Hint/Tutorial/Save/Settings/UI/Rendering 域，含已实现 + 待实现）。
- `control-manifest.md` §1.1 命名表、§1.4 全局规则、§2.2 事件协议章节、§10 接口目录 全面重写；新增 **"story 中遇到 `Evt_Xxx` 按附录 A 映射表替换"** 的通用规则。
- `.claude/docs/technical-preferences.md` §Signals/Events 更新为接口命名；ADR 列表追加 ADR-027，ADR-006 打 superseded 标记。
- `docs/engine-reference/unity/VERSION.md` API 模式一节改为接口事件范例。
- `docs/architecture/adr-010-input-abstraction.md` §GameEvent IDs → 接口命名（`IGestureEvent`）。
- `docs/architecture/adr-016-narrative-sequence-engine.md` Depends On / Ordering Note 加 ADR-027。
- `docs/registry/architecture.yaml`：`interfaces` 节新增 4 条（gesture_dispatch / settings_changed / shadow_rt_updated / puzzle_lock_token，带完整 signal_signature）；`forbidden_patterns` 节新增 3 条（const_int_event_id / raw_gameevent_send_with_hashed_int / asmdef_without_tengine_runtime_ref）。

### 代码
- **删除**：`Assets/GameScripts/HotFix/GameLogic/Input/EventId_Input.cs` + meta，`Assets/GameScripts/HotFix/GameLogic/Rendering/EventId_Rendering.cs` + meta。
- **新建**：
  - `Assets/GameScripts/HotFix/GameLogic/IEvent/IGestureEvent.cs`（`GroupLogic`，5 个方法）。
  - `Assets/GameScripts/HotFix/GameLogic/IEvent/ISettingsEvent.cs`（`GroupLogic`，`OnSettingChanged(string,string)` + `OnTouchSensitivityChanged(float)`）。
  - `Assets/GameScripts/HotFix/GameLogic/IEvent/IShadowRTEvent.cs`（`GroupLogic`，`OnShadowRTUpdated(ShadowRTData)`）。
- **重写**：
  - `GestureDispatcher.cs`：`switch (type) → GameEvent.Get<IGestureEvent>().OnXxx(data)`；删除 `GetEventId` 公开方法。
  - `ShadowRTReadback.cs` 内 `GameEvent.Send<ShadowRTData>(EventId.Evt_ShadowRT_Updated, payload)` → `GameEvent.Get<IShadowRTEvent>().OnShadowRTUpdated(payload)`。
  - `ShadowRTData.cs` / `InputConfigFromLuban.cs` 的 XML 文档注释指向新接口。

### 测试
- `Tests/EditMode/InputSystem/GestureDispatchTests.cs`：改为注册 `IGestureEvent_Event.OnXxx` 监听，验证派发路径 + 唯一性。
- `Tests/EditMode/Rendering/ShadowRTReadbackTests.cs`：改为验证 `IShadowRTEvent_Event.OnShadowRTUpdated` 与其他接口 ID 不冲突。
- `SP007_HybridCLRAsyncGPUTest.cs` 使用任意 ID（99901/99902），与迁移无关，保持不变。

### 初始化
- `GameApp.Entrance()` 首行已有 `GameEventHelper.Init()`（预审核确认无需改动）。

## 5. 后续义务（必须在合并前 / 合并后的首个 sprint 完成）

- **iOS AOT Generic References**：在下次 iOS 出包前执行 `HybridCLR → Generate → AOT Generic References`，确保 `GameEvent.Get<T>()` 对 `IGestureEvent / ISettingsEvent / IShadowRTEvent` 的泛型实例化不因 AOT 剪裁抛 `ExecutionEngineException`。
- **运行期烟测**：
  - 点屏 / 拖拽 / 双指旋转缩放 → 日志中 `OnTap/OnDrag/OnRotate/OnPinch` 按顺序输出。
  - ShadowRT readback 周期输出 → 匹配模块收到 `OnShadowRTUpdated` 回调。
  - 调设置中触摸灵敏度滑条 → `InputConfigFromLuban._sensitivityMultiplier` 更新（待 SettingsSystem 按 ADR-027 规范改造）。
- **分步落地剩余接口**（Object Interaction / Puzzle / Chapter / Scene / Narrative / Audio / Hint / Tutorial / Save / UI）按各自 sprint 计划在实现时创建，不纳入本次原子提交。

## 6. 教训 / 规则沉淀

- **当 ADR 与框架官方做法冲突时，优先对齐框架**。ADR-006 `const int` 本意是"集中管理 ID 避免冲突"，在 TEngine 提供 Source Generator 的情况下是重复造轮子，还制造了双轨制。
- **新引入的架构规范必须先查框架有无原生方案**，避免因审稿人"看起来合理"通过 ADR 后，Sprint 中才发现框架已提供等价能力。
- **asmdef 必须把 `TEngine.Runtime` 作为传递依赖**：任何引用 `GameLogic` 的 asmdef 若没引 `TEngine.Runtime`，Source Generator 注入的 `GameEventHelper.g.cs` 会编译失败（CS0246）。已在 `control-manifest.md` / `architecture.yaml` / `technical-preferences.md` / `ADR-027` 四处固化。
- **ADR 编号保留区要在 `/architecture-decision` 流程前就检查**，本次因已预留 P2 placeholder（019–026）导致需要跳号，下一个新 ADR 从 028 开始。

## 7. 相关文档 / PR

- ADR：`docs/architecture/adr-027-gameevent-interface-protocol.md`
- Superseded ADR：`docs/architecture/adr-006-gameevent-protocol.md`
- 映射表：`docs/architecture/architecture-traceability.md` Appendix A
- 注册表：`docs/registry/architecture.yaml`（interfaces + forbidden_patterns 节）
- 相关前置问题：`.claude/memory/problem_2026-04-22_asmdef-source-generator.md`、`.claude/memory/problem_2026-04-22_tengine-skill-violation.md`
