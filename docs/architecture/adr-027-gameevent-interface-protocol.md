// 该文件由Cursor 自动生成

# ADR-027: GameEvent Interface Protocol（接口事件协议）

## Status

Accepted — 2026-04-23

## Date

2026-04-23

## Last Verified

2026-04-23

## Decision Makers

Technical Director, Lead Programmer

## Summary

项目 event 系统从 ADR-006 的「手写 `public const int` + 集中式 `EventId.cs` + 100-ID-per-system 分配」方案，切换到 TEngine 6.0 官方推荐的「`[EventInterface]` 接口 + Roslyn Source Generator 自动生成 ID」方案。仅保留 TEngine 内置的两种分组（`GroupUI` / `GroupLogic`），不再按"系统"分段。完整继承 ADR-006 §3（生命周期）/§4（Token 锁协议）/§5（顺序保证）/§6（文档约定）。Q4 一次性切换，Q6 同步记录 memory。

本 ADR 取代 ADR-006 的 §1（ID 分配）与 §2（Payload 写法），保留其余协议。ADR-006 迁入 `Superseded by ADR-027` 状态。

> **编号说明**：本 ADR 原计划占用 ADR-019，但 `architecture.md` / `architecture-traceability.md §4` / 多个 EPIC 文档已为 ADR-019 预留 P2 Tutorial Step Engine 名额。为避免编号冲突与下游连带改动，本 ADR 重编号为 ADR-027（P2 placeholder 区段 019–026 之后的下一个自由编号），语义不变。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) |
| **Domain** | Core / Event Communication |
| **Knowledge Risk** | LOW — TEngine 6.0 `EventInterfaceGenerator` 为既定实现，已在 `GameLogic/IEvent/ILoginUI.cs` 样例编译通过；`tengine-dev/references/event-system.md` 对 API 约定有完整文档 |
| **References Consulted** | ADR-001, ADR-004, ADR-006 (superseded), `docs/engine-reference/unity/VERSION.md`, `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/event-system.md`, project source (`Assets/TEngine/Runtime/Core/GameEvent/EventInterfaceAttribute.cs`) |
| **Post-Cutoff APIs Used** | `GameEvent.Get<T>()`, `GameEventHelper.Init()`, `[EventInterface(EEventGroup)]` — 都是 TEngine 6.0 既定 API，由 Roslyn Source Generator 静态生成 ID / 代理 / 注册（`_Event.g.cs`、`_Gen.g.cs`、`GameEventHelper.g.cs`） |
| **Verification Required** | (1) 编译时 Source Generator 必须产出 `GameEventHelper.g.cs`；(2) 所有引用 `GameLogic` 的 asmdef 必须同时引用 `TEngine.Runtime`（见 Decision §8）；(3) `GameApp.Entrance` 第一行必须调用 `GameEventHelper.Init()` |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-001 (TEngine 6.0 Framework)；ADR-004 (HybridCLR Assembly Boundary — Source Generator 生成物随 HotFix 程序集热更) |
| **Supersedes** | ADR-006 §1（ID 分配）与 §2（Payload 写法）。ADR-006 §3/§4/§5/§6 仍然有效，以本 ADR 的接口形式呈现 |
| **Enables** | ADR-016 (Narrative Sequence Engine) — 所有叙事触发/分派改走接口；ADR-024 (Analytics Telemetry, future) — 订阅接口事件采集遥测 |
| **Blocks** | 所有跨模块事件新增工作必须等本 ADR Accepted 后才允许落地 |
| **Ordering Note** | 本 ADR 接受后需同步更新 ADR-006（标记 Superseded）、ADR-010、ADR-016、control-manifest、architecture-traceability、technical-preferences、engine-reference。一次性原子切换（Q4） |

## Context

### Problem Statement

ADR-006 落地了一套「`public const int Evt_Xxx` + 集中式 `EventId` 静态类 + 每系统 100 ID 范围」的事件 ID 分配方案。然而 TEngine 6.0 框架自带 Roslyn Source Generator（`EventInterfaceGenerator`），会根据 `[EventInterface]` 标记的接口自动生成：

- `IXxx_Event.g.cs` — 每个方法对应一个 `static readonly int` 事件 ID（通过 `RuntimeId.ToRuntimeId(...)` 的编译期哈希）
- `IXxx_Gen.g.cs` — 实现接口并在方法体内调用 `dispatcher.Send(事件ID)`
- `GameEventHelper.g.cs` — 一次性实例化所有 `_Gen` 代理并注册

ShadowGame 项目目前**同时存在两套实现**：

| 位置 | 风格 | 来源协议 |
|------|------|---------|
| `GameLogic/IEvent/ILoginUI.cs` | `[EventInterface(EEventGroup.GroupUI)]` 接口事件 | TEngine 官方推荐；`src/MyGame/ShadowGame/CLAUDE.md` 首推；`tengine-dev/references/event-system.md` 规定 |
| `GameLogic/Input/EventId_Input.cs` | `public const int Evt_Gesture_Tap = 1000` | ADR-006 强制要求 |
| `GameLogic/Rendering/EventId_Rendering.cs` | `public const int Evt_ShadowRT_Updated = 1100` | ADR-006 强制要求 |

并存导致：

1. **协议间矛盾** — `src/MyGame/ShadowGame/CLAUDE.md` 首推接口事件，`tengine-dev/references/event-system.md:225-230` 明确写："❌ 不要创建 GameEventDef.cs 手动维护 const int 常量 / ✅ 只需定义 `[EventInterface]` 接口"。ADR-006 与该规则硬冲突。
2. **开发者困惑** — 新增事件时不知走哪条路；已经出现 `EventId_Input.cs`（const int）与 `IEvent/ILoginUI.cs`（接口）两种混用格局。
3. **ID 命名空间重叠** — 手写 `public const int = 1000`（小整数）与 Source Generator 的 `RuntimeId.ToRuntimeId("xxx")`（编译期哈希成的 int，通常 > 10^6）虽然不会实际碰撞，但**同一个事件可能被两种方式分配两个不同 ID**（若开发者误将同一语义事件同时定义在 EventId.cs 和 IXxx 接口），产生"幽灵监听"——发送方和订阅方 ID 对不上。
4. **类型安全缺失** — ADR-006 §2 的 payload 必须通过 `{Name}Payload` struct + 文档约定维持，发送方/接收方类型约定靠 code review。接口事件则在编译期强制方法签名匹配。
5. **asmdef 约束也应进协议** — `problem_2026-04-22_asmdef-source-generator.md` 记录了 Source Generator 在子 asmdef（如 `EditModeTests`）内失败的事故；`technical-preferences.md` 有规定但未进 ADR 正文。

### Current State

- `ADR-006` 状态：Proposed（从未被正式 Accepted，只在 `architecture-review-2026-04-22.md` 中引用）
- `GameLogic/IEvent/ILoginUI.cs`：已存在，接口事件风格
- `GameLogic/Input/EventId_Input.cs`：`Evt_Gesture_Tap=1000 .. Evt_Gesture_LightDrag=1004`，`Evt_Settings_TouchSensitivityChanged=2050`
- `GameLogic/Rendering/EventId_Rendering.cs`：`Evt_ShadowRT_Updated=1100`
- `GestureDispatcher.cs`：用 `GameEvent.Send(int eventId, GestureData data)` 分派
- `GameApp.Entrance`：已调用 `GameEventHelper.Init()` ✅（保留验证）

### Constraints

- TEngine 6.0 `GameEvent` 仍是唯一事件机制（ADR-001 继承）
- 禁止 C# events / delegates / ScriptableObject channels 做跨模块通信（ADR-001 继承）
- `[EventInterface]` 标记的接口及其 `_Gen.g.cs` 生成物必须在 `GameLogic` 热更程序集（ADR-004 继承）
- 任何引用 `GameLogic` 的 asmdef（含测试）必须同时引用 `TEngine.Runtime`，否则 `GameEventHelper.g.cs` 编译失败（见 `problem_2026-04-22_asmdef-source-generator.md`）
- 移动端分发延迟目标 < 0.05ms / Send，无 GC 分配（ADR-006 继承）
- `GameEvent.Send<T1,...,T6>` 泛型重载上限 6 参数（TEngine 6.0 既定）

### Requirements

- 类型安全：发送方与接收方参数类型在编译期一致
- 新增事件零摩擦：只需在 `IEvent/` 下加方法；不需要改 ID 表、不需要开会调 range
- 完整保留 ADR-006 §3（生命周期）§4（Token 锁）§5（顺序保证）§6（文档约定）
- HybridCLR 热更兼容：新接口可通过 `.dll.bytes` 热更新（`GameEventHelper.Init()` 在 Entrance 首行重置所有代理）
- 与项目既有 `ILoginUI.cs` 样例完全一致
- `EventId_Input.cs` / `EventId_Rendering.cs` 等遗留 `const int` 文件彻底删除（Q4 一次性切换）

## Decision

### 1. 接口定义协议（取代 ADR-006 §1）

所有跨模块事件**必须**通过 `[EventInterface]` 接口定义。**禁止**在 GameLogic 中手写 `public const int Evt_Xxx`（`GameEvent` 内部实现除外）。

```csharp
// File: Assets/GameScripts/HotFix/GameLogic/IEvent/IGestureEvent.cs
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 手势事件（Input System → ObjectInteraction / Tutorial / LightSource）。
    /// Sender: InputService.GestureDispatcher.
    /// Listener: ObjectInteractionSystem, TutorialSystem, LightSourceController.
    /// Cascade: 1（监听者可触发 Evt_ObjectSelected 等后续事件）。
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IGestureEvent
    {
        void OnTap(GestureData data);
        void OnDrag(GestureData data);
        void OnRotate(GestureData data);
        void OnPinch(GestureData data);
        void OnLightDrag(GestureData data);
    }
}
```

使用范式：

```csharp
// 发送（强类型，无需查 ID 表）
GameEvent.Get<IGestureEvent>().OnTap(gestureData);

// 订阅（UIWindow / UIWidget 自动清理）
protected override void RegisterEvent()
{
    AddUIEvent<GestureData>(IGestureEvent_Event.OnTap, OnTapReceived);
}

// 订阅（普通 C# 类，配合 GameEventMgr 批量清理）
private readonly GameEventMgr _eventMgr = new();
public void Init()    => _eventMgr.AddEvent<GestureData>(IGestureEvent_Event.OnTap, OnTapReceived);
public void Dispose() => _eventMgr.Clear();
```

### 2. 分组约定（Q3 决策）

仅使用 TEngine 框架内置的两种分组（`Assets/TEngine/Runtime/Core/GameEvent/EventInterfaceAttribute.cs`）：

| 分组 | 用途 | 接口命名 | 典型目标层 |
|------|------|---------|---------|
| `EEventGroup.GroupUI` | UI 通知（UIWindow 显示/关闭、UI 状态更新、弹窗队列） | `I{UIName}` | UI Layer |
| `EEventGroup.GroupLogic` | 业务事件（手势、输入、渲染、场景、叙事、Settings、存档） | `I{Domain}Event` | Logic / Feature Layer |

**废弃 ADR-006 §1 的 "100-ID-per-system" 分段方案**。ID 由 Source Generator 按 `"{InterfaceName}_Event.{MethodName}"` 格式调用 `RuntimeId.ToRuntimeId(...)` 编译期哈希生成（例：`ILoginUI_Event.ShowLoginUI`），不再手动分配。开发者不需要手写该字符串，直接使用生成的 `IXxx_Event.Method` 符号即可。

**接口粒度原则**（补充 TEngine 约定）：

- 同一功能域（gesture / shadow RT / settings / scene / save / ...）聚合到一个接口
- 一个接口可跨系统（例：`ISettingsEvent` 由 Settings 发送，Input/Audio/UI 都订阅）
- 接口方法数目软上限 15（超过拆分）

### 3. Payload 协议（精简 ADR-006 §2）

Payload 直接用**接口方法参数**表达（TEngine 最多 6 参数）：

- 原子事件：基元或单 struct（`void OnTouchSensitivityChanged(float multiplier)`、`void OnTap(GestureData data)`）
- 多参事件：按需展开（`void OnSceneLoadProgress(string sceneName, float progress)`）
- 参数 > 6：定义 struct payload（保留 ADR-006 的 `{Name}Payload` 命名）
- 无参事件：方法无参数

**ADR-006 §2 "Fallback Pattern"（静态 `EventPayloadRegistry`）废弃**：`GameEvent.Send<T1,...,T6>` 泛型重载已验证支持（`tengine-dev/references/event-system.md` L65-78），无需静态 registry fallback。

Payload 数据类型约束（继承 ADR-006）：

- 频繁事件（per-frame / per-gesture）：`struct`，避免 GC
- 含引用类型集合 / 大数据：`class`
- 严禁 `object` 盒装传递

### 4. 多 Sender Token 锁协议（Q2 决策：完整继承 ADR-006 §4）

`Evt_PuzzleLockAll` / `Evt_PuzzleUnlock` 仍然有 Shadow Puzzle 与 Narrative 两个合法发送方。迁移到接口事件后：

```csharp
// File: Assets/GameScripts/HotFix/GameLogic/IEvent/IPuzzleLockEvent.cs
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 全局 Puzzle 锁协议。多 Sender Token Stack 模型。
    /// Senders: ShadowPuzzleSystem (token="puzzle"), NarrativeSystem (token="narrative").
    /// Listener: ObjectInteractionLockManager.
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IPuzzleLockEvent
    {
        void OnPuzzleLockAll(PuzzleLockPayload payload);
        void OnPuzzleUnlock(PuzzleLockPayload payload);
    }
}
```

**完整保留** ADR-006 §4 所有条款：

- `LockToken.Puzzle = "puzzle"` / `LockToken.Narrative = "narrative"` 常量类（唯一合法 token 源）
- `HashSet<string>` 活动锁集合；非空 → 锁定
- 重复锁 / 未知 token unlock → no-op + `Log.Warning`
- `ISceneEvent.OnSceneUnloadBegin` 中 `_activeLockTokens.Clear()`（场景切换强制重置）

（细节实现见 ADR-006 §4 第 395-430 行代码块，迁移时仅接口调用方式变更，逻辑不变。）

### 5. 生命周期协议（继承 ADR-006 §3）

| 上下文 | 注册方式 | 清理方式 |
|-------|---------|---------|
| `UIWindow` / `UIWidget` | `RegisterEvent()` 中 `AddUIEvent(...)` | 框架 `InternalDestroy` 自动 |
| 普通 C# 类（System / Manager） | `Init()` 中 `_eventMgr.AddEvent(...)` | `Dispose()` 中 `_eventMgr.Clear()` |
| 场景级监听（非全局） | 监听 `ISceneEvent.OnSceneUnloadBegin` 显式 `RemoveEventListener` | 同左，必须严格配对 |
| 类整体实现接口作 Listener（整套订阅） | `GameEventHelper.RegisterListener<IXxx>(instance)` | `GameEventHelper.UnregisterListener<IXxx>(instance)` 对称调用 |

`AddUIEvent` 泛型参数写法示例：

```csharp
// 无参
AddUIEvent(IGestureEvent_Event.OnPuzzleComplete, OnPuzzleComplete);

// 1 个参数（struct payload）
AddUIEvent<GestureData>(IGestureEvent_Event.OnTap, OnTapReceived);

// 2 个参数
AddUIEvent<int, string>(IXxxEvent_Event.OnXxx, OnXxxReceived);
```

**选用原则**：

- `AddUIEvent` — 仅在 `UIWindow` / `UIWidget` 的 `RegisterEvent()` 中使用（自动清理）
- `GameEventMgr` — 普通 C# 类中订阅单个方法；适合模块内部少量订阅
- `GameEventHelper.RegisterListener<T>(instance)` — 类整体实现一个 `[EventInterface]` 接口、希望所有方法作为一组订阅时使用；**必须**对称调用 `UnregisterListener`

`GameApp.Entrance` **首行**必须：

```csharp
public static void Entrance(object[] objects)
{
    GameEventHelper.Init();      // 必须第一行，否则 GameEvent.Get<T>() 全部无响应
    // ... 其他初始化
}
```

### 6. 顺序与交付保证（完全继承 ADR-006 §5）

不做任何改动：

- 同步 dispatch、主线程、无 re-entrancy（禁止监听器内 `Send` 相同事件 ID）
- Cascade depth ≤ 3，超过需 1 帧延迟（`GameModule.Timer`）
- 监听者执行顺序无保证（不得假设）

### 7. 文档约定（继承 ADR-006 §6，目标改为接口）

- **接口级** XML doc：Sender(s)、Listener(s)、Cascade
- **方法级** XML doc：每个参数语义（尤其是 struct 字段含义）
- 废弃事件：接口方法加 `[Obsolete("superseded by IYyy.Method, removed 2026-05-01")]`，不删直到下个 major

### 8. asmdef 约束（正式进协议）

**硬性规则**：任何 `.asmdef` 若 `references` 包含 `GameLogic`，则**必须**同时包含 `TEngine.Runtime`。

原因：`EventInterfaceGenerator` 会在所有引用 `GameLogic` 的程序集中生成 `GameEventHelper.g.cs`，该生成物 `using TEngine;`；若目标 asmdef 未引用 `TEngine.Runtime`，会以 `CS0246: type or namespace 'TEngine' could not be found` 编译失败。

适用范围：

- 游戏主 asmdef（`GameLogic.asmdef` 本身：引用传递）
- 测试 asmdef：`EditModeTests.asmdef` / `PlayModeTests.asmdef`
- 工具 asmdef、编辑器扩展 asmdef
- 任何自定义模块 asmdef 只要引用 `GameLogic`

（详见 `src/MyGame/ShadowGame/.claude/memory/problem_2026-04-22_asmdef-source-generator.md`，该记录的根因分析被本条正式纳入协议。）

## Alternatives Considered

### Alternative 1: 维持 ADR-006 `public const int` + 禁用 `[EventInterface]`（Rejected）

- **Description**：删除 `ILoginUI.cs`、回到纯 `EventId.cs`，全部 const int
- **Pros**：改动少；不需要学 Source Generator
- **Cons**：违反 TEngine 官方推荐；违反 `src/MyGame/ShadowGame/CLAUDE.md` 首推方针；违反 `tengine-dev/references/event-system.md` 硬性规则；失去类型安全；`EventId.cs` 单文件 100+ 常量长期膨胀；与官方生态（UIWindow.AddUIEvent 样例全是接口）脱节
- **Rejection**：长期技术债；与项目多份已签署文档自相矛盾

### Alternative 2: 两套并存（Rejected）

- **Description**：老事件用 const int，新事件用 `[EventInterface]`
- **Pros**：不需要一次性迁移
- **Cons**：开发者困惑；同一语义事件被误定义两次的风险；`control-manifest` 规则无法统一表述；L4 审查难以自动化
- **Rejection**：增加协议复杂度，无净收益

### Alternative 3: 全部切到 `[EventInterface]`（Chosen — 本 ADR）

- **Description**：本 ADR 的方案
- **Pros**：TEngine 官方推荐；类型安全；ID 由 Source Generator 管理；自动注册生命周期；开发者心智模型单一
- **Cons**：一次性迁移约 7 个 code 文件 + 11 个 docs 文件；未实施 story 文件的文本仍引用老 `Evt_Xxx` 命名（处理策略见 Migration §B）
- **Why Chosen**：符合官方生态、减少长期维护负担、与现有 `ILoginUI.cs` 样例一致、修正协议自相矛盾

### Alternative 4: 第三方消息库（MessagePipe / UniRx / R3）（Rejected）

与 ADR-006 Alt 4 完全相同的理由：外部依赖 + HybridCLR 兼容性待验证 + 过度工程。

## Consequences

### Positive

- **类型安全**：发送方法签名与订阅方法签名编译期一致；payload mismatch 从 `InvalidCastException` 降级为编译错误
- **零 ID 维护**：不再维护 `EventId.cs`；Source Generator 负责 ID 分配与唯一性
- **官方推荐**：与 TEngine 社区最佳实践、项目内 `CLAUDE.md` / `tengine-dev` skill 一致
- **热更兼容**：接口与 `_Gen.g.cs` 同处 `GameLogic.dll.bytes`，热更覆盖即生效
- **自动发现**：Source Generator 自动枚举所有 `[EventInterface]` 接口并注册；漏注册在编译期就暴露
- **L4 审查简化**：`grep "public const int Evt_"` 应为空；自动化 CI 检查易写

### Negative

- **一次性迁移成本**：7 个 .cs 文件 + 11 个 docs 文件需一次性切换（详见 Migration §A）
- **未实施 story 文件留有老名**：~50 个 story 文件在文本层引用 `Evt_Gesture_Tap` 等老命名（详见 Migration §B 策略）
- **调试时 ID 不直观**：`GameEvent.Send(123456789)`（hash 值）不如 `GameEvent.Send(1000)` 直白；但 `GameEvent.Get<IGestureEvent>().OnTap()` 可读性反向强很多，Mitigation 由调用语法解决

### Risks

| 风险 | 概率 | 影响 | 缓解 |
|------|------|------|------|
| 新 asmdef 漏引用 `TEngine.Runtime` 导致 Source Generator 失败 | MEDIUM | BUILD BLOCK | Decision §8 进协议；control-manifest 强制规则；`problem_2026-04-22` 记录已有前车之鉴 |
| 一次性切换期半迁移态 | LOW (Q4 atomic) | HIGH | 单 PR 内完成所有代码 + 核心文档；CI 检查 `grep "public const int Evt_"` 返 0 |
| `GameEventHelper.Init()` 被遗漏 | LOW（已验证当前 GameApp.Entrance 已调用）| 全站 `GameEvent.Get<T>()` 无响应 | Migration §A 最后一步显式验证；加 CI 静态 assertion |
| 遗漏订阅者 | LOW | MEDIUM | 删除 `EventId_Input.cs` 后编译错误会定位所有旧订阅点 |
| HybridCLR 热更时 Source Generator 产物 stale | LOW | HIGH | 热更包重新编译时 Source Generator 自动重跑；`GameEventHelper.Init()` 在 HotFix Entrance 首行重注册，覆盖热更前的代理实例 |
| Story 文本未同步引起开发者按老名实现 | MEDIUM | LOW | Migration §B：`control-manifest` 加一条总览规则（"Story 若引用 `Evt_Xxx`，实现时按 ADR-027 映射表替换"）+ `architecture-traceability` 附录加 EventId→Interface 映射表 |
| **iOS AOT Generic References 失效**：每次热更新增 `[EventInterface]` 接口，`GameEvent.Get<IXxx>()` 是泛型方法，iOS IL2CPP + HybridCLR 必须在主包 AOT 中有对应泛型实例，否则抛 `ExecutionEngineException` | MEDIUM | HIGH (iOS only) | Migration §A 步骤 11.5 明文要求：新接口落地后执行 **HybridCLR → Generate → AOT Generic References → 重新打主包**；CI 中为 iOS 生产构建添加 AOT References 一致性 lint |
| `GameEvent.Get<T>()` 热更后返回 stale 代理实例 | LOW | HIGH | 依赖 TEngine 6.0 `Init()` 覆盖写入语义（需 Sprint 验证）；热更 Entrance 首行调用 `GameEventHelper.Init()` 保证代理刷新 |
| Unity Roslyn Source Generator 增量缓存：接口文件移动未修改不重跑 | LOW | MEDIUM | Sprint 验证：移动接口文件后确认 `GameEventHelper.g.cs` 正确更新；必要时 `Library/ScriptAssemblies` 强制清理 |

## Performance Implications

| 指标 | 预期 | 预算 | 备注 |
|------|------|------|------|
| Dispatch 延迟 | < 0.05ms / Send | 16.67ms / frame | 同 ADR-006；Source Generator 代理是一次性查表 |
| Runtime Memory（接口代理） | ~0.5 KB | 1500 MB mobile | `GameEventHelper.Init()` 一次性实例化所有 `_Gen` 代理，每个 ~30B |
| Runtime Memory（事件 ID） | 0（static readonly int 编译为内联） | 同上 | |
| GC（struct payload） | 0 / dispatch | 0 target hot path | 同 ADR-006 |
| Source Generator 编译时开销 | < 1s 首次，< 200ms 增量 | Unity 编译预算 | ShadowGame 现有 2 个接口，预计增长到 ~20 个接口，总量可控 |

## Migration Plan — 一次性切换（Q4）

### §A Code & Core Docs（必做，本 ADR Accepted 后立即执行）

1. **新建** `GameLogic/IEvent/IGestureEvent.cs`（GroupLogic，5 方法）
2. **新建** `GameLogic/IEvent/ISettingsEvent.cs`（GroupLogic，`OnTouchSensitivityChanged` + 预留 `OnVolumeChanged` 等，按后续 Settings story 补）
3. **新建** `GameLogic/IEvent/IShadowRTEvent.cs`（GroupLogic，`OnShadowRTUpdated(ShadowRTData data)`）
4. **删除** `GameLogic/Input/EventId_Input.cs` 及 `.cs.meta`
5. **删除** `GameLogic/Rendering/EventId_Rendering.cs` 及 `.cs.meta`
6. **重写** `GameLogic/Input/GestureDispatcher.cs`：`switch (type)` → 按 `GestureType` 选择 `GameEvent.Get<IGestureEvent>().OnXxx(data)` 调用
7. **重写** `GameLogic/Rendering/ShadowRTReadback.cs`：`GameEvent.Send(EventId.Evt_ShadowRT_Updated, data)` → `GameEvent.Get<IShadowRTEvent>().OnShadowRTUpdated(data)`
8. **重写** `GameLogic/Input/InputConfigFromLuban.cs`：触摸灵敏度相关调用改接口
9. **重写** `GameLogic/Test/SP007_HybridCLRAsyncGPUTest.cs`：订阅 `IShadowRTEvent_Event.OnShadowRTUpdated`
10. **重写** 测试：`Tests/EditMode/InputSystem/GestureDispatchTests.cs`、`Tests/EditMode/Rendering/ShadowRTReadbackTests.cs`
11. **验证** `GameApp.Entrance` 首行 `GameEventHelper.Init()`（已存在，只确认）
11.5. **（iOS 关键）重新生成 AOT Generic References**：Unity 菜单 HybridCLR → Generate → AOT Generic References，让主包获取 `GameEvent.Get<IGestureEvent>/<IShadowRTEvent>/<ISettingsEvent>` 的 AOT 泛型实例；**重新打主包**并覆盖 iOS 测试真机。若省略此步，iOS 运行时触发任一新接口调用会抛 `ExecutionEngineException`
12. **更新** `adr-006-gameevent-protocol.md`：`Status: Superseded by ADR-027`（加头注解释哪些条款被保留）
13. **更新** `adr-010-input-abstraction.md` §"GameEvent IDs"：改引接口命名
14. **更新** `adr-016-narrative-sequence-engine.md` Depends On：加 ADR-027
15. **更新** `control-manifest.md` §2.2：ADR-006 引用 → ADR-027 + 规则替换（const int → interface）
16. **更新** `architecture-traceability.md`：表格中 ADR-006 单元格调整为 "ADR-027 (supersedes ADR-006)"；末尾附加 EventId→Interface 映射表
17. **更新** `.claude/docs/technical-preferences.md` L30（Signals/Events）、L93（ADR-006 条目加 superseded 标记）、ADR 列表追加 ADR-027
18. **更新** `docs/engine-reference/unity/VERSION.md` L43（Events API pattern）
19. **新增** `.claude/memory/problem_2026-04-23_event-protocol-pivot.md`（Q6 同步记录）
20. **更新** `docs/registry/architecture.yaml`：`interfaces` 节追加 3 条（IGestureEvent / ISettingsEvent / IShadowRTEvent，每条带 signal_signature）

### §B Story 文本处理策略（deferred）

~50 个 story 文件引用 `Evt_Xxx` 命名。策略：

- **不**在一次性切换中批量改 story 文件（scope creep 风险）
- 在 `control-manifest.md` §2.2 加一条总览规则：**"Story 规范中若引用 `Evt_Xxx_Yyy` 命名，实施时按 ADR-027 附录的 EventId → Interface 映射表替换。映射表之外的名称需联系 Architecture Owner 补充。"**
- 在 `architecture-traceability.md` 新建附录 **"Appendix: ADR-006 EventId → ADR-027 Interface Mapping"**，列出 ADR-006 的全部 `Evt_Xxx` 到新接口方法的一对一映射
- 每个 story 在 `/dev-story` 启动时由 dev 根据映射表填入新命名；**禁止**提交含 `Evt_Xxx` 的新代码

### Rollback 计划

若切换后 HybridCLR 热更出现 Source Generator 产物不一致：回滚单 PR → 恢复 `EventId_Input.cs` / `EventId_Rendering.cs` → 标记 ADR-027 为 `Rejected`，回到 ADR-006 并开新 Spike 调查热更冲突根因。

## Validation Criteria

### 静态 / 编译期

- [ ] `grep -r "public const int Evt_" src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic` → 0 条
- [ ] Unity 编译通过，`GameEventHelper.g.cs`（生成物）包含 `IGestureEvent`、`IShadowRTEvent`、`ISettingsEvent`、`ILoginUI`
- [ ] `GestureDispatchTests.cs` / `ShadowRTReadbackTests.cs` 单测通过，覆盖 "`GameEvent.Get<IGestureEvent>().OnTap(data)` 触发 handler 一次且仅一次、`GestureData` struct 字段完整传递" 断言
- [ ] `GameApp.Entrance` 首行为 `GameEventHelper.Init()`（静态扫描）
- [ ] `adr-006-gameevent-protocol.md` 顶部 Status 为 `Superseded by ADR-027`
- [ ] `control-manifest.md` §2.2 无 `public const int` 规则
- [ ] 新接口 XML doc 完整（Sender / Listener / Cascade）
- [ ] `architecture-traceability.md` 附录包含完整 EventId → Interface 映射
- [ ] `.claude/memory/problem_2026-04-23_event-protocol-pivot.md` 已写入
- [ ] 所有 HotFix asmdef（及测试 asmdef）同时引用 `GameLogic` + `TEngine.Runtime`

### Sprint 验证项（需真机 / Profiler 确认，进 Sprint 0 Spike backlog）

- [ ] **UV-01** 热更链路：热更后执行 `GameEventHelper.Init()` → 断言 `GameEvent.Get<IGestureEvent>()` 返回的代理实例地址与热更前不同（验证 Init 是"覆盖写入"而非"追加"）
- [ ] **UV-02** 性能基准：中端 Android 真机 Unity Profiler 采样 `GameEvent.Get<IGestureEvent>().OnTap(data)` 的 `Send` 延迟 < 0.05ms、GC Alloc = 0
- [ ] **UV-03** 增量编译健壮性：将一个 `[EventInterface]` 接口文件从一个子目录移动到另一个子目录（不修改内容），确认 `GameEventHelper.g.cs` 正确更新（若未更新，需在 Migration 规范中追加 "移动接口文件后强制清 `Library/ScriptAssemblies`"）
- [ ] **UV-04** iOS AOT：新接口首次热更后在 iOS 真机执行 `GameEvent.Get<INewInterface>().Method(...)`，断言不抛 `ExecutionEngineException`；若抛，验证 Migration 步骤 11.5（AOT Generic References 重生成）是否正确执行

## GDD Requirements Addressed

继承 ADR-006 GDD 覆盖表。本次切换**不改变**任何 GDD 技术需求的实现覆盖度，仅变更实现命名。具体映射见 Migration §B 提及的 `architecture-traceability.md` 附录。

典型 TR 映射示例：

| GDD TR | 原 ADR-006 EventId | ADR-027 接口方法 |
|--------|-------------------|-----------------|
| TR-concept-013 (Event-driven comm) | 整个 `EventId` 静态类 | `[EventInterface]` 生态 |
| TR-input-gesture-dispatch | `Evt_Gesture_{Tap,Drag,Rotate,Pinch,LightDrag}` | `IGestureEvent.{OnTap,OnDrag,OnRotate,OnPinch,OnLightDrag}` |
| TR-settings-003 (real-time apply) | `Evt_SettingChanged` (2000), `Evt_Settings_TouchSensitivityChanged` (2050) | `ISettingsEvent.OnSettingChanged(...)` / `OnTouchSensitivityChanged(float)` |
| TR-shadowrt-readback | `Evt_ShadowRT_Updated` (1100) | `IShadowRTEvent.OnShadowRTUpdated(ShadowRTData)` |
| TR-objint-016 (lock/unlock) | `Evt_PuzzleLockAll/Unlock` | `IPuzzleLockEvent.OnPuzzleLockAll/OnPuzzleUnlock` |
| TR-narr-012 (sequence complete) | `Evt_SequenceComplete` | `INarrativeEvent.OnSequenceComplete` (后续 Narrative sprint) |
| TR-scene-014 (transition events) | `Evt_Scene{...}` ×8 | `ISceneEvent.On{...}` ×8 (后续 Scene sprint) |

（完整映射表在 `architecture-traceability.md` 附录，Accepted 后同步添加。）

## Related Decisions

- **Supersedes**: ADR-006 §1, §2（`docs/architecture/adr-006-gameevent-protocol.md`）
- **Inherits**: ADR-006 §3, §4, §5, §6（生命周期、Token、顺序、文档约定 — 不变）
- **Depends On**: ADR-001 (TEngine Framework)、ADR-004 (HybridCLR Assembly Boundary)
- **Updates**: ADR-010 §GameEvent IDs；ADR-016 §Depends On
- **Referenced by (future)**: ADR-024 (Analytics Telemetry)
- **Memory Record**: `src/MyGame/ShadowGame/.claude/memory/problem_2026-04-23_event-protocol-pivot.md`
- **Implementation Exemplar**: `GameLogic/IEvent/ILoginUI.cs`（已存在的参考实现）
- **Skill Alignment**: `.claude/skills/tengine-dev/references/event-system.md`（本 ADR 与该文档完全一致）
