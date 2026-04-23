// 该文件由Cursor 自动生成

# Sprint 0 Spike Plan — 影子回忆 (Shadow Memory)

> **版本**: 1.0.0
> **日期**: 2026-04-22
> **基于**: `docs/architecture/architecture.md` Section 9 — 10 个 Open Questions
> **引擎**: Unity 2022.3.62f2 LTS
> **框架**: TEngine 6.0.0 + HybridCLR + YooAsset 2.3.17 + UniTask 2.5.10
> **Sprint 0 目标**: 消除所有阻塞 Sprint 1 编码启动的技术未知项

---

## 📋 目录

1. [Spike 列表总览](#1-spike-列表总览)
2. [SP-001 — GameEvent Payload 类型支持](#sp-001--gameevent-payload-类型支持)
3. [SP-002 — UIWindow 生命周期调用时序](#sp-002--uiwindow-生命周期调用时序)
4. [SP-003 — YooAsset ResourcePackage 多包策略](#sp-003--yooasset-resourcepackage-多包策略)
5. [SP-004 — Luban Tables 异步线程安全性](#sp-004--luban-tables-异步线程安全性)
6. [SP-005 — WallReceiver Shader URP 兼容性](#sp-005--wallreceiver-shader-urp-兼容性)
7. [SP-006 — PuzzleLockAll 双发送者 Token 防护](#sp-006--puzzlelockall-双发送者-token-防护)
8. [SP-007 — HybridCLR + AsyncGPUReadback AOT 兼容性](#sp-007--hybridclr--asyncgpureadback-aot-兼容性)
9. [SP-008 — 末章合并序列 Luban 配置表达方案](#sp-008--末章合并序列-luban-配置表达方案)
10. [SP-009 — I2 Localization 与 TEngine 集成模式](#sp-009--i2-localization-与-tengine-集成模式)
11. [SP-010 — 性能自动降级架构归属](#sp-010--性能自动降级架构归属)
12. [Sprint 0 排期](#12-sprint-0-排期)
13. [风险评估与回退策略](#13-风险评估与回退策略)

---

## 1. Spike 列表总览

| Spike | 问题摘要 | 优先级 | 预估 (h) | 执行者 | 依赖 | 状态 |
|-------|---------|:------:|:--------:|--------|------|------|
| SP-001 | GameEvent payload struct/class 支持 | **P0** | 2h | Lead Programmer | — | ✅ 源码确认（AOT 泛型待真机验证） |
| SP-002 | UIWindow OnCreate/OnRefresh 调用时序 | **P0** | 2h | Lead Programmer | — | ✅ 源码确认 |
| SP-003 | YooAsset 单包 vs 多包初始化策略 | **P0** | 3h | TD | — | ✅ 决策：单包 |
| SP-004 | Luban Tables 多 UniTask 并发访问安全 | **P0** | 2h | Lead Programmer | — | ✅ 确认：主线程只读安全 |
| SP-005 | WallReceiver shader URP ShaderGraph vs HLSL | **P0** | 4h | Technical Artist | — | ✅ 决策：纯 HLSL（待 Editor 验证 GPU 时间）|
| SP-006 | PuzzleLockAll 双发送者 unlock 安全设计 | **P1** | 2h | TD | SP-001 | ✅ 决策：HashSet token 防护 |
| SP-007 | HybridCLR + AsyncGPUReadback GPU 回调兼容 | **P0** | 4h | Engine Programmer | — | ✅ 真机验证通过 |
| SP-008 | 末章合并序列 Luban 配置表达 | **P1** | 3h | Game Designer + TD | SP-004 | ✅ 决策：Sequence Chain |
| SP-009 | I2 Localization 与 TEngine 集成方式 | **P1** | 3h | Lead Programmer | SP-002 | ✅ 确认：TEngine 内嵌封装 |
| SP-010 | 性能自动降级 — 全局模块 vs 渲染自管理 | **P1** | 3h | TD | SP-007 | ✅ 决策：全局 PerformanceMonitor |
| | **合计** | | **28h** | | | |

> **P0** = 阻塞 Sprint 1 启动 / **P1** = 阻塞对应系统的 Sprint

### 重要提前发现（源码预审结论）

在撰写本文档过程中，对项目内 TEngine 源码的预审已回答 SP-001 和 SP-002 的**主要部分**：

- **SP-001**: `GameEvent.Send<TArg1>(int, TArg1)` 至 `Send<TArg1..TArg6>(int, ...)` 已在源码中确认存在，支持泛型参数传递任意 struct/class。架构文档 Section 5.3 中所有 payload 设计**均有效**，无需切换为 static event bus。剩余验证点：HybridCLR AOT 环境下泛型 boxing 行为。
- **SP-002**: `UIModule.cs:477-478` 确认首次打开调用顺序为 `InternalCreate() → InternalRefresh()` 同帧顺序调用；重新打开（窗口未销毁）仅调用 `InternalRefresh()`。`OnUpdate()` 仅在 `IsPrepare && Visible` 时触发。

---

## SP-001 — GameEvent Payload 类型支持

### 问题（原文）

TEngine `GameEvent.Send` 的 payload 支持什么类型？是否支持 struct/class payload，还是只支持 int 参数？需要验证源码。如果不支持复杂 payload，事件通信方案需要调整为 static event bus 或额外的 payload registry。

### Spike ID
`SP-001`

### 受影响 ADR
- ADR-006: GameEvent Communication Protocol (payload 约定的核心依据)
- ADR-001: TEngine 6.0 Framework Adoption

### 受影响系统
**全部跨模块通信**（所有 GameEvent 发送方和接收方）。这是最高优先级问题，错误假设将导致 Section 5.3 整个事件表重新设计。

### 调查方法

**Step 1 — 源码确认（已完成）**

阅读 `Assets/TEngine/Runtime/Core/GameEvent/GameEvent.cs`，确认：

```csharp
// 已确认存在：支持 0-6 个泛型类型参数
public static void Send<TArg1>(int eventType, TArg1 arg1)
public static void Send<TArg1, TArg2>(int eventType, TArg1 arg1, TArg2 arg2)
// ... 直至 TArg6
```

支持的 payload 类型：任意泛型类型（struct、class、基础类型均可）。

**Step 2 — HybridCLR AOT 泛型实例化验证**

在 HybridCLR 热更程序集（`GameLogic.dll`）中写一个小型验证脚本：

```csharp
// [验证目标] 确认 AOT 环境中 GameEvent.Send<struct> 不会 MissingMethodException
public struct TestPayload { public int puzzleId; public float score; }

// 测试发送
GameEvent.Send<TestPayload>(99999, new TestPayload { puzzleId = 1, score = 0.85f });

// 测试接收
GameEvent.AddEventListener<TestPayload>(99999, (p) => {
    Debug.Log($"Received: {p.puzzleId}, {p.score}");
});
```

在 Editor Play Mode（AOT 模拟）和真机构建（完整 HybridCLR）两种环境下各运行一次。

**Step 3 — 多参数 struct 测试**

验证架构文档中最复杂的 payload 场景：

```csharp
// Evt_ObjectTransformChanged: { int objectId, Vector3 pos, Quaternion rot }
// 需要验证 3 参数发送
GameEvent.Send<int, Vector3, Quaternion>(Evt_ObjectTransformChanged, 1, pos, rot);
GameEvent.AddEventListener<int, Vector3, Quaternion>(Evt_ObjectTransformChanged, OnTransformChanged);
```

### 验收标准
1. ✅ 确认 `GameEvent.Send<T>(int, T)` 在 Editor 和真机下均无异常
2. ✅ 确认 struct payload 无意外 GC 分配（Profiler 验证）
3. ✅ 确认最多 3-4 参数同时传递可正常工作
4. ✅ 产出：`docs/architecture/findings/SP-001-gameevent-payload.md`，说明允许的 payload 模式和任何限制

### 预估时间
**2h**（1h 源码阅读已完成；1h 真机验证）

### 优先级
**P0** — 阻塞 ADR-006 撰写和所有系统的事件设计定稿

### 依赖
无

### 输出产物
- `docs/architecture/findings/SP-001-gameevent-payload.md` — payload 类型约定备忘录
- 更新 ADR-006 草稿（payload 约定小节）

---

## SP-002 — UIWindow 生命周期调用时序

### 问题（原文）

TEngine `UIWindow` 的生命周期回调（`OnCreate`/`OnRefresh`/`OnUpdate`/`OnClose`）的确切调用时序需要从源码验证。特别是 `OnRefresh` vs `OnCreate` 在首次打开时的行为。

### Spike ID
`SP-002`

### 受影响 ADR
- ADR-011: UIWindow Management & Layer Strategy

### 受影响系统
**UI System**（全部 9 个 UIWindow 的初始化逻辑、数据绑定位置选择）

### 调查方法

**Step 1 — 源码确认（已完成）**

阅读 `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs:477-478` 和 `UIWindow.cs`，确认：

```
首次打开：
  LoadAsync → Handle_Completed [资源加载完] → IsPrepare=true → callback → 
  InternalCreate() [OnCreate] → InternalRefresh() [OnRefresh] → Show

重新打开（窗口实例已存在，未被 Destroy）：
  TryInvoke → InternalRefresh() [OnRefresh]  ← OnCreate 不再调用

Update 触发条件：
  InternalUpdate() 仅在 IsPrepare == true && Visible == true 时调用

Destroy 时：
  InternalDestroy() → RemoveAllUIEvent → OnDestroy → Object.Destroy(panel)
```

**Step 2 — 验证 HideTimeToClose 行为**

UIWindow 有 `HideTimeToClose` 属性。当窗口被 Hide（而非 Close）后，经过 timer 才真正销毁。需要确认：Hide 状态下是否仍执行 `OnUpdate`？

```csharp
// 检查 InternalUpdate 内的 Visible 判断
if (!IsPrepare || !Visible)
    return false; // ← Visible=false 时 OnUpdate 不执行（已从源码确认）
```

**Step 3 — 产出生命周期图**

绘制完整状态机图，明确各阶段该放什么逻辑：

| 回调 | 触发时机 | 推荐用途 |
|------|---------|---------|
| `OnCreate` | 首次加载后，调用一次 | 组件引用获取、事件注册、静态初始化 |
| `OnRefresh` | 每次打开（含首次）| 数据刷新、UI 内容更新（用 userDatas） |
| `OnUpdate` | 每帧，仅 Visible==true | 实时数据轮询（谨慎使用） |
| `OnDestroy` | 真正销毁时 | 资源释放、事件注销 |
| `OnSetVisible` | 每次 Show/Hide | 过渡动画触发 |

### 验收标准
1. ✅ 产出"UIWindow 生命周期备忘录"，明确每个回调的触发条件
2. ✅ 确认 `OnCreate` 和 `OnRefresh` 在首次打开时的顺序（同帧，Create 在前）
3. ✅ 确认 `HideTimeToClose` 期间 `OnUpdate` 不执行
4. ✅ 所有 9 个 UIWindow 的初始化位置已根据结论定稿

### 预估时间
**2h**（源码已读；1h 写 HUD/MainMenu 两个窗口的验证 Prototype；1h 产出文档）

### 优先级
**P0** — UIWindow 用法影响所有 UI 实现。OnCreate vs OnRefresh 放错数据初始化会导致重复加载或数据不一致 bug。

### 依赖
无

### 输出产物
- `docs/architecture/findings/SP-002-uiwindow-lifecycle.md` — 生命周期图 + 编码规范
- 更新 ADR-011 草稿

---

## SP-003 — YooAsset ResourcePackage 多包策略

### 问题（原文）

YooAsset `ResourcePackage` 的多包初始化策略：项目是否需要多个 ResourcePackage（一个给场景，一个给共享资源），还是单包策略足够？影响 ADR-005 的决策。

### Spike ID
`SP-003`

### 受影响 ADR
- ADR-005: YooAsset Resource Loading & Lifecycle Pattern
- ADR-009: Scene Lifecycle & Additive Scene Strategy

### 受影响系统
**Scene Management**，**所有资源加载路径**

### 调查方法

**Step 1 — 查看现有项目配置**

```
检查路径：
  Assets/GameScripts/Procedure/ProcedureInitPackage.cs  ← 包初始化流程
  Assets/GameScripts/Procedure/ProcedureInitResources.cs
  Assets/GameScripts/Procedure/ProcedurePreload.cs
```

确认当前 `DefaultPackageName` 和是否已有多包配置。

**Step 2 — 评估多包 vs 单包的项目需求**

| 维度 | 单包策略 | 多包策略 |
|------|---------|---------|
| 初始化复杂度 | 低 | 高（每包独立 InitializeAsync） |
| 适合场景 | < 2GB 内容，无按需 DLC | 大型内容分批下载，或多语言包 |
| 下载控制 | 全量或全部 | 可按包单独更新 |
| 项目匹配 | 5 章 + 共享资源 | — |

**Step 3 — 分析 5 章内容体量**

根据 GDD 估算：
- 每章场景 = 1 个 Additive Scene
- 共享资源（UI prefabs, SFX, Music）= 常驻内存
- 总体量估计（需与美术确认）

**Step 4 — 验证单包策略中场景卸载的资源释放**

```csharp
// 验证单包策略下 SceneHandle 卸载后共享资源不被误卸载
var handle = await GameModule.Resource.LoadSceneAsync("Chapter1", LoadSceneMode.Additive);
// 卸载场景后：
await GameModule.Resource.UnloadSceneAsync(handle);
// 确认共享的 SFX、UI prefab 资源句柄仍然有效
```

### 验收标准
1. ✅ 确定项目使用单包还是双包策略，并记录决策理由
2. ✅ 确认现有 `ProcedureInitPackage.cs` 的包配置与决策一致
3. ✅ 确认场景卸载不影响跨场景共享资源的存活
4. ✅ 产出 ADR-005 的"YooAsset 包策略"小节草稿

### 预估时间
**3h**（1h 源码审查；1h 体量分析；1h 产出文档）

### 优先级
**P0** — 影响整个资源加载架构和场景切换流程，需在 Scene Management 系统编码前确定。

### 依赖
无

### 输出产物
- `docs/architecture/findings/SP-003-yooasset-package-strategy.md`
- 更新 ADR-005 草稿

---

## SP-004 — Luban Tables 异步线程安全性

### 问题（原文）

Luban 生成代码中 `Tables` 单例的线程安全性——如果在 async 上下文中从多个 UniTask 并发访问，是否需要额外同步？

### Spike ID
`SP-004`

### 受影响 ADR
- ADR-007: Luban Config Table Access Pattern

### 受影响系统
**所有配置驱动系统**（Chapter State、Shadow Puzzle、Hint、Narrative、Audio、Tutorial）

### 调查方法

**Step 1 — 检查 Luban 生成代码结构**

```
检查路径：
  Assets/GameScripts/HotFix/GameProto/  ← Luban 生成代码位置
  确认 Tables 类的静态单例初始化方式
  确认 TbChapter / TbPuzzle 等表的数据容器类型（IReadOnlyDictionary？List？）
```

**Step 2 — UniTask 并发场景分析**

UniTask 在 Unity 主线程上下文（`PlayerLoop`）运行时，`async/await` 的续接默认回到主线程。确认：

```csharp
// UniTask 默认行为：所有 await 续接在主线程
await UniTask.SwitchToMainThread(); // 不需要显式切换

// 问题场景：是否存在 UniTask.Run（线程池）中访问 Tables？
await UniTask.Run(() => {
    var row = Tables.TbPuzzle.Get(id); // ← 若此处是线程池，需要验证
});
```

**Step 3 — 确认访问模式**

本项目中 Luban 表访问的正确模式应为：
- `Tables.Init()` 在主线程完成（Step 7 of 初始化顺序）
- 之后所有 `Tables.TbX.Get(id)` 均在主线程调用（无跨线程访问）
- 所有 UniTask async 方法均在 PlayerLoop context（主线程）续接

**Step 4 — 压力验证**

```csharp
// 在同一帧内同时调用多个 async 方法，均读 Tables
var t1 = ReadChapterDataAsync(1);
var t2 = ReadPuzzleDataAsync(1);
var t3 = ReadHintConfigAsync(1);
await UniTask.WhenAll(t1, t2, t3);
// 监控：无 NullRef、无 InvalidOperation
```

### 验收标准
1. ✅ 确认 Luban Tables 生成的数据容器为只读（`IReadOnlyDictionary` 或等效）
2. ✅ 确认项目所有 UniTask async 路径均在主线程续接（无 `UniTask.Run` 跨线程读 Tables）
3. ✅ 产出："Tables 访问不需要额外同步锁，但必须避免 `UniTask.Run` 中读表"的结论文档

### 预估时间
**2h**

### 优先级
**P0** — 所有 async 初始化路径（Step 7-14）均涉及 Tables 读取。

### 依赖
无

### 输出产物
- `docs/architecture/findings/SP-004-luban-thread-safety.md`
- 更新 ADR-007 草稿中"并发访问"小节

---

## SP-005 — WallReceiver Shader URP 兼容性

### 问题（原文）

WallReceiver 自定义 shader 的 URP 兼容性——需要确认 URP 2022.3 的 ShaderGraph 是否支持自定义 shadow sampling pass，还是必须写纯 HLSL。

### Spike ID
`SP-005`

### 受影响 ADR
- ADR-002: URP Rendering Pipeline for Shadow Projection

### 受影响系统
**URP Shadow Rendering**（WallReceiver shader、ShadowRT 采样）

### 调查方法

**Step 1 — 评估 ShaderGraph Shadow Sample 能力**

在 Unity 2022.3 URP ShaderGraph 中，确认：
1. `Custom Function Node` 能否调用 `SAMPLE_TEXTURE2D_SHADOW` / `TransformWorldToShadowCoord`？
2. URP Shadow Caster Pass 是否可通过 ShaderGraph 的 "Shadow Caster" 目标复写？
3. R8 灰度格式 `ShadowRT` 能否通过 ShaderGraph Sampler2D 节点采样？

**Step 2 — 原型实现**

创建一个最小化 WallReceiver shader 原型：

```hlsl
// 方案 A: 纯 HLSL (Custom Unlit Shader)
Shader "ShadowGame/WallReceiver"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            TEXTURE2D(_ShadowRT);
            SAMPLER(sampler_ShadowRT);
            
            // 对 ShadowRT 采样，输出灰度值
            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half shadow = SAMPLE_TEXTURE2D(_ShadowRT, sampler_ShadowRT, uv).r;
                return half4(shadow, shadow, shadow, 1);
            }
            ENDHLSL
        }
    }
}
```

```
方案 B: ShaderGraph
  - 新建 Shader Graph (URP Lit target)
  - 添加 Texture2D property: _ShadowRT
  - Sample Texture 2D node → 接 r 通道 → 连接 Base Color
  - 验证能否正常采样 R8 格式 RenderTexture
```

**Step 3 — 性能对比**

- 两方案均在测试场景中挂载到 Wall quad（1080p）
- Profiler GPU 帧时间：目标 ≤ 0.5ms

**Step 4 — 章节风格过渡验证**

确认 `TransitionShadowStyle(from, to, duration)` 所需的 shader 参数插值可通过 Material.Lerp 或 DOTween 实现。

### 验收标准
1. ✅ 确定 WallReceiver shader 使用 ShaderGraph 还是纯 HLSL（明确推荐方案）
2. ✅ 原型在 Unity 2022.3 URP 下正确采样 RenderTexture（R8 或 RGBA32）
3. ✅ GPU 时间 ≤ 0.5ms（Galaxy A 系设备等价评估）
4. ✅ 产出原型 shader 文件和 ADR-002 shadow rendering 小节草稿

### 预估时间
**4h**（最长的技术原型类 spike）

### 优先级
**P0** — Shadow Puzzle System 的核心视觉效果依赖此 shader。若 ShaderGraph 不支持，需提前确定 HLSL 路线。

### 依赖
无

### 输出产物
- `Assets/GameScripts/HotFix/GameLogic/Rendering/WallReceiver.shader` — 原型文件
- `docs/architecture/findings/SP-005-wallreceiver-shader.md`
- 更新 ADR-002 草稿

---

## SP-006 — PuzzleLockAll 双发送者 Token 防护

### 问题（原文）

`PuzzleLockAllEvent` 有两个合法发送者（Shadow Puzzle 和 Narrative），Object Interaction 不关心发送者——但是否需要一个 sequence number 或 token 来防止错误的 unlock（如 Narrative pop 了 Puzzle 的 lock）？

### Spike ID
`SP-006`

### 受影响 ADR
- ADR-006: GameEvent Communication Protocol
- ADR-013: Object Interaction State Machine

### 受影响系统
**Object Interaction**（被锁定方），**Narrative Event System**（发送 PuzzleLockAll + PuzzleUnlock），**Shadow Puzzle System**（发送 PuzzleLockAll）

### 调查方法

**Step 1 — 分析实际并发场景**

梳理所有可能导致 lock/unlock 错误配对的时序：

```
场景 A（正常）:
  Shadow Puzzle → PuzzleLockAll          Object Interaction: locked
  Narrative → PuzzleUnlock               Object Interaction: unlocked ✅

场景 B（问题）:
  Narrative → PuzzleLockAll              Object Interaction: locked (counter=1)
  Shadow Puzzle → PuzzleLockAll          Object Interaction: locked (counter=2)
  Narrative → PuzzleUnlock               Object Interaction: unlocked (counter=1)? 还是直接解锁?
```

**Step 2 — 评估三种方案**

| 方案 | 实现 | 安全性 | 复杂度 |
|------|------|-------|--------|
| A: 引用计数 Lock Stack | `PushLock(token)` / `PopLock(token)` | 高 | 中 |
| B: 单一发送者规则 | 只允许 Narrative 发送 Unlock | 中 | 低 |
| C: 事件 payload 带 token | `PuzzleLockAll { string lockerId }` / `PuzzleUnlock { string lockerId }` | 高 | 中 |

**Step 3 — 结合架构约束得出结论**

检查：
- Shadow Puzzle 发送 `PuzzleLockAll` 是否只在 `PerfectMatch` 状态（此时 Narrative 也会发送）？
- 如果两者顺序已被架构保证（Narrative 始终最后 Unlock），方案 B 足够
- 若无法保证顺序，选方案 A（引用计数）

**Step 4 — 设计决策文档**

输出 ADR-006 的"PuzzleLockAll 防护机制"小节：

```csharp
// 推荐方案 A: 如果需要 token 防护
public static void PushLock(string token) { _lockStack.Push(token); }
public static void PopLock(string token) {
    if (_lockStack.TryPeek(out var top) && top == token)
        _lockStack.Pop();
    // else: 日志警告，不崩溃
}
```

### 验收标准
1. ✅ 明确 `PuzzleLockAll` 在架构中是否需要 token 防护（有/无，说明理由）
2. ✅ 若需要：定义 Lock/Unlock 的 payload 扩展和 Object Interaction 的计数逻辑
3. ✅ 更新 Section 5.3 事件表中 `Evt_PuzzleLockAll` 和 `Evt_PuzzleUnlock` 的 payload 定义

### 预估时间
**2h**（设计讨论类 spike，无需写代码）

### 优先级
**P1** — 影响 Object Interaction 和 Narrative 系统的接口，但不阻塞 Sprint 1（Foundation Layer 不涉及）。

### 依赖
SP-001（确认 GameEvent payload 支持 string token）

### 输出产物
- `docs/architecture/findings/SP-006-puzzlelock-token.md`
- 更新 ADR-006 的 payload 约定小节

---

## SP-007 — HybridCLR + AsyncGPUReadback AOT 兼容性

### 问题（原文）

HybridCLR 对 `AsyncGPUReadback` 回调中 hot-fix 代码的支持是否存在已知限制？ShadowRT 读回在 GPU callback 中执行，需确认 AOT 兼容性。

### Spike ID
`SP-007`

### 受影响 ADR
- ADR-002: URP Rendering Pipeline
- ADR-004: HybridCLR Assembly Boundary Rules

### 受影响系统
**URP Shadow Rendering**（ShadowRT CPU 读回）、**Shadow Puzzle System**（依赖读回数据计算 match score）

### 调查方法

**Step 1 — 研究 AsyncGPUReadback 的 callback 线程模型**

`AsyncGPUReadback.Request` 的回调在 Unity 主线程的 `AsyncGPUReadback.Update()` 中触发（非 GPU worker 线程），但触发时机是 `EndOfFrame` 阶段。

确认：HybridCLR 热更代码能否在 `EndOfFrame` callback 中执行？

**Step 2 — 最小化真机验证原型**

```csharp
// 在 GameLogic.dll（热更程序集）中编写验证代码
public class AsyncGPUReadbackTest : MonoBehaviour
{
    private RenderTexture _rt;
    
    void Start()
    {
        _rt = new RenderTexture(256, 256, 0, RenderTextureFormat.R8);
        // 触发一次 AsyncGPUReadback
        AsyncGPUReadback.Request(_rt, 0, TextureFormat.R8, OnReadbackComplete);
    }
    
    // 此回调在 GameLogic.dll（热更）中定义
    private void OnReadbackComplete(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.LogError("[SP-007] AsyncGPUReadback failed in HybridCLR context");
            return;
        }
        var data = request.GetData<byte>();
        Debug.Log($"[SP-007] SUCCESS: Got {data.Length} bytes, first={data[0]}");
    }
}
```

**Step 3 — 验证矩阵**

| 环境 | 预期 | 实际 |
|------|------|------|
| Editor Play Mode (AOT) | ✅ 正常 | ? |
| iOS Build + HybridCLR | ✅ 正常 | ? |
| Android Build + HybridCLR | ✅ 正常 | ? |
| iOS Build + HybridCLR (IL2CPP AOT 泛型) | ⚠️ 需关注 | ? |

**Step 4 — 若发现兼容性问题，验证回退方案**

```csharp
// 回退方案：将 AsyncGPUReadback 封装移到 Default 程序集（非热更）
// GameLogic 通过接口调用
public interface IShadowRTReader
{
    void RequestReadback(RenderTexture rt, Action<NativeArray<byte>> onComplete);
}
// Default 程序集实现，GameLogic 热更代码通过 DI 获取
```

**Step 5 — AOT 泛型元数据注册**

检查 `Assets/GameScripts/HotFix/` 中是否有 AOT 泛型元数据注册代码（`RuntimeApi.RegisterAOTGenericInstances`），确认 `AsyncGPUReadbackRequest.GetData<byte>()` 是否已注册。

### 验收标准
1. ✅ 在目标平台（iOS/Android）真机上验证 `AsyncGPUReadback` 回调在 GameLogic 热更代码中正常执行
2. ✅ 若有问题：提供具体报错信息和回退方案实施计划
3. ✅ 产出兼容性报告，明确是否需要将 ShadowRT reader 移至 Default 程序集

### 预估时间
**4h**（2h 原型编写；2h 真机打包验证）

### 优先级
**P0** — ShadowRT 读回是 Shadow Puzzle 核心计分机制的数据源。若 HybridCLR 不兼容，整个 shadow matching 的架构需要调整。**最高技术风险项。**

### 依赖
无（可并行进行）

### 输出产物
- `Assets/GameScripts/HotFix/GameLogic/Test/AsyncGPUReadbackTest.cs` — 验证脚本
- `docs/architecture/findings/SP-007-hybridclr-asyncgpu.md` — 兼容性报告
- 若失败：更新 ADR-002 和 ADR-004 的"assembly boundary"决策

---

## SP-008 — 末章合并序列 Luban 配置表达方案

### 问题（原文）

章末最后谜题的"合并序列"（记忆重现 + 章节过渡无缝衔接）在 Luban 配置表中如何表达？是两个 sequence 的 chain，还是一个合并的长 sequence？

### Spike ID
`SP-008`

### 受影响 ADR
- ADR-016: Narrative Sequence Engine

### 受影响系统
**Narrative Event System**，**Chapter State System**（触发时机），**Audio System**（章节过渡 BGM crossfade）

### 调查方法

**Step 1 — 分析末章合并序列的设计需求**

根据架构文档 Section 5.2，普通谜题完成流程：
```
PerfectMatchEvent → Narrative 查 NarrativeSequenceId → 播放 → SequenceComplete 
→ PuzzleUnlock → ChapterState.OnPuzzleComplete
```

末章最后谜题特殊：完成后需要无缝接章节过渡序列，不能有明显间断。

**Step 2 — 评估两种 Luban 配置方案**

**方案 A: Sequence Chain（两个独立 sequence，带 nextSequenceId 链接）**

```lua
-- TbNarrativeSequence
{
  id = "ch5_puzzle5_complete",
  effects = [...],        -- 记忆重现效果
  nextSequenceId = "ch5_chapter_transition",  -- 自动衔接
  nextSequenceDelay = 0.0
}
{
  id = "ch5_chapter_transition",
  effects = [...],        -- 章节过渡效果
  nextSequenceId = ""
}
```

**方案 B: 合并长 Sequence（单一 sequence，包含全部 effects）**

```lua
{
  id = "ch5_puzzle5_complete_and_transition",
  effects = [
    { t=0.0, type="AudioDucking", ... },
    -- 记忆重现内容
    { t=5.0, type="ScreenFade", ... },
    -- 章节过渡内容
    { t=8.0, type="LoadNextChapter", targetChapterId=0 }  -- 游戏结束
  ]
}
```

| 维度 | 方案 A | 方案 B |
|------|--------|--------|
| 配置复用性 | 高（Chapter Transition 可复用） | 低 |
| 无缝过渡保障 | 中（依赖 delay=0 实现无缝）| 高（单序列无间断） |
| 策划可维护性 | 高 | 中 |
| 实现复杂度 | 中（需 chain 逻辑）| 低 |

**Step 3 — 确认 Narrative Engine 需支持的能力**

确定选择后，更新 `INarrativeEvent` 接口是否需要：
- 方案 A: `sequenceQueue.Enqueue(nextId)` 在 SequenceComplete 时自动触发
- 方案 B: 无需额外机制，单序列完整运行

**Step 4 — 与策划对齐**

明确：末章结束是否还需要其他特殊处理（片尾字幕？回到主菜单？），将需求纳入配置方案。

### 验收标准
1. ✅ 确定采用方案 A 或方案 B，并说明理由
2. ✅ 产出 `TbNarrativeSequence` 的 Luban Schema 草稿（包含 chain 字段或不包含）
3. ✅ 更新 ADR-016 的"sequence 配置格式"小节

### 预估时间
**3h**（1h 方案分析；1h 策划对齐讨论；1h 产出 Luban schema 草稿）

### 优先级
**P1** — 影响 Narrative System 的数据格式，需在该系统 Sprint 前确定。不阻塞 Foundation Layer。

### 依赖
SP-004（确认 Luban Tables 访问无线程问题后）

### 输出产物
- `docs/architecture/findings/SP-008-narrative-sequence-schema.md`
- `TbNarrativeSequence.yaml` schema 草稿（Luban 配置表结构定义）
- 更新 ADR-016 草稿

---

## SP-009 — I2 Localization 与 TEngine 集成模式

### 问题（原文）

I2 Localization 与 TEngine 的集成方式——TEngine 是否自带 localization 包装，还是 I2 独立于 TEngine 运行？影响 ADR-022。

### Spike ID
`SP-009`

### 受影响 ADR
- ADR-022: I2 Localization Integration & String Management

### 受影响系统
**Settings & Accessibility**（语言切换），**UI System**（所有文本显示）

### 调查方法

**Step 1 — 检查项目内 TEngine LocalizationModule**

项目中存在 `Assets/TEngine/Runtime/Module/LocalizationModule/` 目录：

```
检查：
  LocalizationModule/Core/LocalizeDropdown.cs  ← 已发现
  
  问题：TEngine LocalizationModule 是对 I2 的包装，还是独立实现？
  检查是否 using I2.Loc 命名空间
```

**Step 2 — 检查 I2 Localization 的独立配置**

```
检查路径：
  Assets/I2/Localization/  ← I2 标准安装目录
  Assets/Resources/I2Languages/  ← I2 语言资源文件
```

确认：
1. I2 LanguageSource 资产是否已配置
2. 语言数据存储在 StreamingAssets 还是 YooAsset bundle
3. Runtime 切换语言 API: `LocalizationManager.CurrentLanguage = "Chinese (Simplified)"`

**Step 3 — 评估集成策略**

```csharp
// 方案 A: TEngine LocalizationModule 包装 I2（若已实现）
GameModule.Localization.SetLanguage("zh-CN");

// 方案 B: I2 独立运行，Settings System 直接调用 I2 API
LocalizationManager.CurrentLanguage = "Chinese (Simplified)";
```

**Step 4 — 验证 YooAsset 下的 I2 语言资源加载**

I2 默认从 Resources 加载，需确认是否已适配 YooAsset 热更包：

```csharp
// 检查 I2 的 ResourceManager 设置
// 若语言资源在 YooAsset bundle 中，需要配置自定义 IResourceExtractor
```

**Step 5 — 运行时语言切换验证**

```csharp
// 测试：运行时切换中英文不需要重启场景
LocalizationManager.CurrentLanguage = "English";
LocalizationManager.CurrentLanguage = "Chinese (Simplified)";
// 验证：UI 文本是否立即更新
```

### 验收标准
1. ✅ 确认 TEngine LocalizationModule 是 I2 包装还是独立实现
2. ✅ 确认 Settings System 调用哪个 API 进行语言切换
3. ✅ 验证语言资源在 YooAsset 环境下正确加载
4. ✅ 产出 ADR-022 草稿和语言切换调用示例

### 预估时间
**3h**

### 优先级
**P1** — 不阻塞 Sprint 1（Foundation Layer），但需在 Settings + UI Sprint 前确定。

### 依赖
SP-002（确认 UIWindow 如何响应语言切换事件）

### 输出产物
- `docs/architecture/findings/SP-009-i2-localization.md`
- 更新 ADR-022 草稿

---

## SP-010 — 性能自动降级架构归属

### 问题（原文）

性能自动降级（TR-render-016: 连续 5 帧 > 20ms 降一档）是由 URP Shadow Rendering 自行管理，还是由一个全局 Performance Monitor 模块统一管理所有系统的降级？

### Spike ID
`SP-010`

### 受影响 ADR
- ADR-018: Performance Monitoring & Auto-Degradation Strategy
- ADR-002: URP Rendering Pipeline
- ADR-021: Quality Tier Auto-Detection & Dynamic Switching

### 受影响系统
**URP Shadow Rendering**（阴影质量分级），**跨系统**（潜在的全局 Performance Monitor 模块）

### 调查方法

**Step 1 — 明确降级触发源**

TR-render-016 规定：连续 5 帧帧时间 > 20ms 时触发降级。但降级只影响 Shadow Rendering 的质量，还是其他系统（Narrative 动画速度、UI 复杂度等）也需要响应？

检查：其他 GDD 中是否有类似的性能响应需求？

**Step 2 — 两方案分析**

**方案 A: 渲染自管理（Shadow Rendering 模块内部）**

```csharp
// URP Shadow Rendering 内部维护帧时间统计
public class ShadowRenderingSystem
{
    private float[] _frameTimes = new float[5];
    private int _qualityTier = 2; // 0=Low, 1=Mid, 2=High
    
    void Update()
    {
        RecordFrameTime(Time.deltaTime * 1000f);
        if (AllFramesAbove20ms())
            DecreaseQualityTier();
    }
}
```

优点：简单，职责内聚
缺点：若其他系统也需要响应性能，各自重复实现

**方案 B: 全局 Performance Monitor 模块**

```csharp
// 独立的 PerformanceMonitor 模块（Foundation Layer）
// 通过 GameEvent 广播降级信号
public class PerformanceMonitor
{
    void Update()
    {
        if (ShouldDegrade())
            GameEvent.Send(Evt_QualityTierChanged, currentTier - 1);
    }
}
// Shadow Rendering 监听事件并调整质量
// 未来其他系统也可监听
```

优点：集中管理，可扩展
缺点：过度设计（若只有 Shadow Rendering 需要响应）

**Step 3 — 决策依据**

| 条件 | 推荐方案 |
|------|---------|
| 只有 Shadow Rendering 需要降级 | 方案 A |
| 多个系统（2+）需要响应性能降级 | 方案 B |
| 需要在设置界面手动切换质量档 | 方案 B（Settings 集中控制） |

**Step 4 — 与 Settings 系统的联动**

TR-render-016 的自动降级 vs 玩家手动在 Settings 中选择质量档——两者是否共用同一套质量状态机？若是，则 Performance Monitor 是更好的选择（Settings 也写入同一状态）。

### 验收标准
1. ✅ 确定采用方案 A 或方案 B，说明理由
2. ✅ 若方案 B：明确 Performance Monitor 的层级归属（Foundation 还是 Platform 服务）
3. ✅ 明确质量档状态机的所有者（谁读写，谁监听）
4. ✅ 产出 ADR-018 和 ADR-021 草稿

### 预估时间
**3h**（架构设计讨论类 spike）

### 优先级
**P1** — 不阻塞 Sprint 1（Foundation Layer），但 URP Shadow Rendering 系统实现前需确定。

### 依赖
SP-007（确认 AsyncGPUReadback 兼容性后，了解 ShadowRT 读回的实际性能开销）

### 输出产物
- `docs/architecture/findings/SP-010-performance-monitor.md`
- 更新 ADR-018 和 ADR-021 草稿

---

## 12. Sprint 0 排期

### 总体参数

| 项目 | 数值 |
|------|------|
| Sprint 0 工期 | 4 天 |
| 总估算工时 | 28 人·时 |
| 执行人员 | 4 人（TD、Lead Programmer、Engine Programmer、Technical Artist）|
| 目标 | Sprint 1 启动时，所有 P0 Open Questions 已关闭 |

### 每日排期

```
Day 1（周一）— P0 优先，最高风险项同步启动
───────────────────────────────────────────────
  Lead Programmer:  SP-001 GameEvent payload 验证       [2h]
                    SP-002 UIWindow 生命周期产出文档      [2h]
  Engine Programmer: SP-007 HybridCLR+AsyncGPUReadback  [4h] ← 最高风险，尽早启动
  Technical Artist: SP-005 WallReceiver shader 开始原型  [4h] ← 最长 spike

  Day 1 产出:
    - SP-001 结论文档（GameEvent payload 已确认支持 struct）
    - SP-002 UIWindow 生命周期图
    - SP-007 Editor/真机初步结果
    - SP-005 Shader 原型初稿

Day 2（周二）— P0 完成，P0 补位
───────────────────────────────────────────────
  TD:               SP-003 YooAsset 多包策略决策         [3h]
  Lead Programmer:  SP-004 Luban Tables 线程安全         [2h]
                    SP-009 I2 Localization 集成模式       [3h]
  Engine Programmer: SP-007 真机验证（iOS/Android 构建）  [4h 续]
  Technical Artist: SP-005 Shader 原型完成 + 文档         [4h 续]

  Day 2 产出:
    - SP-003 YooAsset 包策略决定
    - SP-004 Luban 线程安全结论
    - SP-007 真机验证结果
    - SP-005 WallReceiver shader 原型完成

Day 3（周三）— P1 推进，ADR 草稿撰写
───────────────────────────────────────────────
  TD + Lead:        SP-006 PuzzleLockAll token 设计讨论  [2h]
  TD:               SP-010 性能降级架构归属决策           [3h]
  Game Designer+TD: SP-008 Narrative Sequence Luban 方案 [3h]
  Lead Programmer:  ADR-001, ADR-006 草稿（基于 SP-001）  [3h]

  Day 3 产出:
    - SP-006 lock token 设计决策
    - SP-008 Luban sequence schema 草稿
    - SP-010 Performance Monitor 架构决策
    - ADR-001 + ADR-006 草稿

Day 4（周四）— 汇总收口，ADR 集中撰写，Sprint 1 准备
───────────────────────────────────────────────
  All:              所有 Spike 结论文档归档              [2h]
  TD + Lead:        ADR-002, ADR-004, ADR-005 草稿        [4h]
  Lead Programmer:  ADR-007, ADR-008 草稿               [3h]
  TD:               Sprint 1 故事筛选 + 排期评审          [2h]

  Day 4 产出:
    - 全部 10 个 findings 文档归档
    - ADR-001 ~ ADR-011（P0 必备）草稿完成
    - Sprint 1 Sprint Planning Ready
```

### 并行执行图

```
        Lead Programmer      Engine Programmer    Technical Artist      TD
Day 1   SP-001 (2h)          SP-007 part1 (4h)   SP-005 part1 (4h)    —
        SP-002 (2h)
        
Day 2   SP-004 (2h)          SP-007 part2 (4h)   SP-005 part2 (4h)    SP-003 (3h)
        SP-009 (3h)          [真机验证]
        
Day 3   ADR-001,006 (3h)     —                   —                     SP-010 (3h)
        SP-006 with TD (2h)                                            SP-006 with LP (2h)
                                                                        SP-008 with GD (3h)
                                                                        
Day 4   ADR-007,008 (3h)     —                   —                     ADR-002,004,005 (4h)
        归档 (2h)                                                       Sprint 1 排期 (2h)
```

### 关键路径

`SP-007 (Day1-2)` → `SP-010 (Day3)` → ADR-018 草稿

`SP-001 (Day1)` → `SP-006 (Day3)` → ADR-006 定稿

---

## 13. 风险评估与回退策略

### 高风险 Spike

#### SP-007 — HybridCLR + AsyncGPUReadback（最高风险）

| | 内容 |
|-|------|
| **假设** | `AsyncGPUReadback` 的 `Action<AsyncGPUReadbackRequest>` 回调可在 HybridCLR 热更程序集（GameLogic.dll）中实现 |
| **若假设错误** | 真机上回调中的热更代码抛出 `MissingMethodException` 或 `ExecutionEngineException` |
| **回退策略** | **立即执行方案**：将 `AsyncGPUReadback.Request()` 的调用和回调委托封装在 `Default` 程序集（非热更）中，`GameLogic` 热更代码通过接口 `IShadowRTReader` 注入并消费数据。额外成本：~4h 重构 + 接口定义 |
| **对架构影响** | ADR-004 需新增规则：GPU 回调委托必须在 Default 程序集注册 |
| **早期预警** | Day 1 Editor 模式验证通过但 Day 2 真机失败 → 立即启动回退方案，不等 Day 4 |

#### SP-005 — WallReceiver Shader URP 兼容性（中高风险）

| | 内容 |
|-|------|
| **假设** | URP 2022.3 ShaderGraph 的 Custom Function Node 可访问 `_ShadowRT` RenderTexture 并输出正确灰度值 |
| **若假设错误** | ShaderGraph 无法满足自定义 shadow sampling 需求，必须写纯 HLSL |
| **回退策略** | 切换到纯 HLSL Unlit Shader（方案已在 Step 2 备好代码框架），不影响 GPU 预算，追加约 1h 适配时间 |
| **对架构影响** | 无（纯实现细节，不影响 API 边界）|

#### SP-001 — GameEvent AOT 泛型实例化（低风险，但影响全局）

| | 内容 |
|-|------|
| **假设** | `GameEvent.Send<GestureData>(int, GestureData)` 在 HybridCLR AOT 环境下正常工作（无 MissingMethod） |
| **若假设错误** | 所有 struct payload 的泛型事件需要改为 class，或需要手动注册 AOT 泛型元数据 |
| **回退策略 A** | 将所有 payload struct 改为 class（GC 压力增加，可接受）|
| **回退策略 B** | 在 AOT 泛型元数据注册文件中添加所有 payload 类型的实例化声明（零运行时开销，需维护注册列表）|
| **早期预警** | SP-007 真机验证同步测试 SP-001 的泛型调用 |

### 中风险 Spike

| Spike | 若假设错误的影响 | 额外时间 |
|-------|---------------|---------|
| SP-003 | 需要双包策略，ProcedureInitPackage 需重写 | +4h |
| SP-004 | 需要在所有 async 路径加读写锁 | +3h |
| SP-008 | 方案 A/B 选错，Narrative Engine 需返工配置格式 | +4h |

### 低风险 Spike

| Spike | 风险说明 |
|-------|---------|
| SP-002 | UIWindow 源码已读，结论已基本明确，风险极低 |
| SP-006 | 纯设计讨论，无技术风险，最差结果是决策延迟 |
| SP-009 | I2 是成熟商业插件，集成方式有官方文档支撑 |
| SP-010 | 架构归属讨论，无技术风险 |

### Sprint 0 失败条件

若以下任一条件成立，Sprint 0 结束时仍**不具备 Sprint 1 启动条件**：

1. SP-007 真机验证失败且回退方案未实施（Shadow Puzzle 无数据来源）
2. SP-005 shader 无法在 URP 中实现 shadow sampling（核心视觉缺失）
3. SP-001 AOT 泛型问题未解决（所有事件通信方案不确定）

---

*Sprint 0 Spike Plan — 影子回忆 (Shadow Memory)*
*生成日期: 2026-04-22*
*下一步: 执行完成后，将各 findings 文档归档至 `docs/architecture/findings/`，并更新 Open Questions 表格中各项状态*
