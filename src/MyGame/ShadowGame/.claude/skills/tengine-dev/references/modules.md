# TEngine 模块 API 速查

## 目录

- [GameModule 统一访问入口](#gamemodule-统一访问入口)
- [TimerModule 计时器](#timermodule-计时器)
- [SceneModule 场景管理](#scenemodule-场景管理)
- [AudioModule 音频](#audiomodule-音频)
- [FsmModule 有限状态机](#fsmmodule-有限状态机)
- [MemoryPool 内存池](#memorypool-内存池)
- [ObjectPool 对象池](#objectpool-对象池)
- [Log 日志系统](#log-日志系统)
- [LocalizationModule 本地化](#localizationmodule-本地化)

---

## GameModule 统一访问入口

所有模块通过 `GameModule` 静态类访问，内部已缓存，禁止重复 `ModuleSystem.GetModule<T>()`：

```csharp
GameModule.Resource      // IResourceModule  — 资源加载
GameModule.UI            // UIModule         — UI 管理
GameModule.Audio         // IAudioModule     — 音频
GameModule.Timer         // ITimerModule     — 计时器
GameModule.Scene         // ISceneModule     — 场景
GameModule.Fsm           // IFsmModule       — 有限状态机
GameModule.Procedure     // IProcedureModule — 流程
GameModule.Localization  // ILocalizationModule — 本地化
```

---

## TimerModule 计时器

### 添加计时器

```csharp
// 单次计时器（3秒后触发一次）
int tid = GameModule.Timer.AddTimer(OnTimerTick, time: 3f);

// 循环计时器（每1秒触发）
int tid = GameModule.Timer.AddTimer(OnTimerTick, time: 1f, isLoop: true);

// 非缩放时间（不受 Time.timeScale 影响）
int tid = GameModule.Timer.AddTimer(OnTimerTick, time: 5f, isLoop: false, isUnscaled: true);
```

### 控制计时器

```csharp
GameModule.Timer.Stop(timerId);         // 暂停
GameModule.Timer.Resume(timerId);       // 恢复
GameModule.Timer.Restart(timerId);      // 重置并重新开始
GameModule.Timer.RemoveTimer(timerId);  // 移除（销毁对象时必须调用）
GameModule.Timer.RemoveAllTimer();      // 清除所有计时器
```

### 查询

```csharp
float left   = GameModule.Timer.GetLeftTime(timerId);   // 剩余时间
bool running = GameModule.Timer.IsRunning(timerId);     // 是否运行中
```

**最佳实践**：在对象销毁（`OnDestroy` 或 `UIWindow.OnDestroy`）时调用 `RemoveTimer(tid)`，防止空引用。

---

## SceneModule 场景管理

### 加载场景

```csharp
// 单场景加载（替换当前场景）
Scene scene = await GameModule.Scene.LoadSceneAsync("SceneName");

// 叠加加载
Scene scene = await GameModule.Scene.LoadSceneAsync("SceneName", LoadSceneMode.Additive);

// 带进度回调
Scene scene = await GameModule.Scene.LoadSceneAsync(
    "SceneName",
    LoadSceneMode.Single,
    progressCallBack: p => { /* p: 0~1 */ }
);
```

### 卸载 / 激活

```csharp
bool ok = await GameModule.Scene.UnloadAsync("SceneName");
GameModule.Scene.ActivateScene("SceneName");  // 设为活动场景
bool has = GameModule.Scene.IsContainScene("SceneName");
```

**注意**：场景资产必须放在 `AssetRaw/Scenes/` 并由 YooAsset 管理，通过 location 字符串寻址，禁止 `SceneManager.LoadScene()`。

---

## AudioModule 音频

### 播放

```csharp
// 背景音乐（循环）
AudioAgent agent = GameModule.Audio.Play(AudioType.Music, "path/to/bgm", bLoop: true);

// 音效（单次）
AudioAgent agent = GameModule.Audio.Play(AudioType.Sound, "path/to/sfx");

// UI 音效（异步加载，fire and forget）
AudioAgent agent = GameModule.Audio.Play(AudioType.UISound, "ui_click", bAsync: true);
```

### 停止

```csharp
GameModule.Audio.Stop(AudioType.Music, fadeout: true);  // 渐出停止
GameModule.Audio.Stop(AudioType.Sound);
GameModule.Audio.StopAll(fadeout: false);
```

### 音量控制

```csharp
GameModule.Audio.Volume      = 1.0f;   // 全局音量 (0~1)
GameModule.Audio.MusicVolume = 0.8f;   // 音乐音量
GameModule.Audio.SoundVolume = 1.0f;   // 音效音量
GameModule.Audio.MusicEnable = true;   // 音乐开关
GameModule.Audio.SoundEnable = true;   // 音效开关
```

---

## FsmModule 有限状态机

> **设计取向**：TEngine FsmModule 是 **procedure-style** FSM —— OOP per-state class、状态切换在 `OnUpdate` 内由状态自身触发。最适合**顶层 / 单实例 / 长生命周期**的流程（如 GameFlow / NPC AI）。**事件驱动 trigger reducer 风格的 FSM**（外部事件即时切换、多对象实例、高频低延迟、需要 1:1 本地反馈 C# event）请参考项目 `ADR-028 §3` 自建例外名单的二分原则。

### 创建并启动状态机

```csharp
// 定义状态（每个状态一个 class，继承 FsmState<T>）
public class IdleState : FsmState<MyOwner>
{
    protected override void OnEnter(IFsm<MyOwner> fsm)  { }

    protected override void OnUpdate(IFsm<MyOwner> fsm, float elapseSeconds, float realElapseSeconds)
    {
        // 状态切换必须在状态内部触发（见下方 §状态切换）
        if (someCondition)
        {
            ChangeState<RunState>(fsm); // ✅ 调基类 protected 包装方法
        }
    }

    protected override void OnLeave(IFsm<MyOwner> fsm, bool isShutdown) { }
}

// 创建状态机
IFsm<MyOwner> fsm = GameModule.Fsm.CreateFsm<MyOwner>(
    "FsmName",
    owner,
    new IdleState(),
    new RunState(),
    new AttackState()
);
fsm.Start<IdleState>();
```

### 状态切换

**关键约束**：`Fsm<T>.ChangeState` 在 TEngine 6.0 源码中是 `internal`（仅 `TEngine.Runtime` 程序集内可见），`IFsm<T>` 接口（public）**未声明** `ChangeState` 方法。这意味着：

```csharp
// ❌ 错误（GameLogic 程序集中编译不通过）
fsm.ChangeState<RunState>();

// ✅ 正确：从 FsmState<T> 子类内部调用 protected 包装方法
public class IdleState : FsmState<MyOwner>
{
    protected override void OnUpdate(IFsm<MyOwner> fsm, float elapseSeconds, float realElapseSeconds)
    {
        if (CheckRunCondition(fsm.Owner))
        {
            ChangeState<RunState>(fsm);            // 泛型版
            // 或：ChangeState(fsm, typeof(RunState));  // Type 版
        }
    }
}
```

**外部事件触发切换**：FsmModule 不支持外部代码直接 `ChangeState` 当前状态。如外部需要触发切换，可借 `fsm.SetData(...)` 写入 trigger flag，由当前状态在 `OnUpdate` 内检查 + 切换：

```csharp
// 外部代码
fsm.SetData("RequestedTransition", typeof(AttackState));

// 状态内 OnUpdate
protected override void OnUpdate(IFsm<MyOwner> fsm, float elapseSeconds, float realElapseSeconds)
{
    if (fsm.HasData("RequestedTransition"))
    {
        var target = fsm.GetData<Type>("RequestedTransition");
        fsm.RemoveData("RequestedTransition");
        ChangeState(fsm, target);
    }
}
```

> **如果**外部即时切换是核心需求（如手势 / 输入 / 业务事件驱动的多对象 FSM），FsmModule 的"绕路"模式可能与设计意图严重不符，此时请参考 `ADR-028 §3` 评估是否走"自建 trigger reducer FSM"路径（项目内样板：`SingleFingerFSM` / `SceneManager` / `InteractableObjectFsm`）。

### 数据传递

```csharp
// 状态间共享数据
fsm.SetData<int>("Key", value);
int val = fsm.GetData<int>("Key");
bool exists = fsm.HasData("Key");
fsm.RemoveData("Key");
```

### 销毁

```csharp
GameModule.Fsm.DestroyFsm<MyOwner>("FsmName");
```

---

## MemoryPool 内存池

用于频繁创建/销毁的纯 C# 对象，避免 GC 分配：

```csharp
// 对象需实现 IMemory 接口
public class DamageInfo : IMemory
{
    public int Damage;
    public int TargetId;
    
    public void Clear()  // 归还池时重置数据
    {
        Damage = 0;
        TargetId = 0;
    }
}

// 获取
var info = MemoryPool.Acquire<DamageInfo>();
info.Damage = 100;

// 归还（不要再使用此引用）
MemoryPool.Release(info);
```

**规则**：`Release` 后禁止再访问该对象；不能 `Release` 同一对象两次。

---

## ObjectPool 对象池

> **启用前提**（项目当前为"未启用"状态，参见 `ADR-028 §1` M8 决策）：
> - `Assets/GameScripts/HotFix/GameLogic/GameModule.cs` 当前**未暴露** `ObjectPool` facade 字段；启用前需要按 `ADR-028 §2` 流程添加：
>   ```csharp
>   public static IObjectPoolModule ObjectPool => _objectPool ??= Get<IObjectPoolModule>();
>   private static IObjectPoolModule _objectPool;
>   // Shutdown() 中清空 _objectPool = null;
>   ```
> - 启用时机：场景对象量超过 ~50 时再评估（章节 8/9/10 大场景）；项目当前 ≤ 10 puzzle objects/章节，UI 已自管池化（UIWindow 缓存），不需要 ObjectPoolModule。

TEngine 对象池为**两层 API**结构：

- **`IObjectPoolModule`**（外层管理器，由 `GameModule.ObjectPool` 暴露后取用）：负责创建 / 获取 / 销毁不同类型的对象池
- **`IObjectPool<T>`（`T : ObjectBase`）**（单个池子）：负责该类型对象的实际 Spawn / Unspawn

### 第一步：定义 ObjectBase 子类

```csharp
// 你的池化对象必须继承 ObjectBase
public class BulletObject : ObjectBase
{
    public GameObject Prefab; // 业务字段

    // 框架要求的清理钩子
    protected override void Release(bool isShutdown)
    {
        // 真实销毁逻辑（如池容量超出时）
    }
}
```

### 第二步：通过外层管理器创建池

```csharp
// 待 GameModule.ObjectPool facade 字段添加后：
IObjectPool<BulletObject> pool = GameModule.ObjectPool.CreateMultiSpawnObjectPool<BulletObject>(
    name:     "BulletPool",
    capacity: 20,         // 池容量
    expireTime: 30f,      // 对象过期秒数
    priority: 0
);

// 或单次获取池：
IObjectPool<BulletObject> singlePool =
    GameModule.ObjectPool.CreateSingleSpawnObjectPool<BulletObject>("UniqueBoss");
```

> 当前未启用阶段（GameModule.ObjectPool 字段未加），可临时通过 `ModuleSystem.GetModule<IObjectPoolModule>()` 直访 —— 但属于 `ADR-028 §"启用条件未到"暂时例外`，不推荐扩散到生产代码。

### 第三步：从池中 Spawn / 归还 Unspawn

```csharp
// 获取对象（池空时由 Register 注入的实例补充；用完必须 Unspawn 归还）
BulletObject bullet = pool.Spawn();

// 归还（不要 Destroy；不要 Spawn 同一对象后再 Spawn 同一对象不归还）
pool.Unspawn(bullet);
```

### 预热（不存在 WarmUp 方法，用 Capacity + Register 模式）

```csharp
// TEngine 的 IObjectPool<T> 接口中没有 WarmUp 方法。预热模式：
pool.Capacity = 10;
for (int i = 0; i < 10; i++)
{
    var obj = new BulletObject { Prefab = ... };
    pool.Register(obj, spawned: false); // 先注册到池中（未占用状态）
}
```

### GameObject Prefab 池化的常用替代方案

如目标只是"GameObject Instantiate 复用"而**不需要复杂的池子管理（容量 / 优先级 / 过期）**，更轻量的做法是直接用 `IResourceModule.LoadGameObject` + 自管引用计数（参见 `references/resource-management.md`），由 YooAsset 的 AssetReference 管理即可。完整 `ObjectPoolModule` 适合需要主动控制 Spawn 时机 + Capacity 上限 + 自动过期回收的高频场景。

---

## Log 日志系统

```csharp
Log.Debug("调试信息，仅 Development Build 输出");
Log.Info("普通信息");
Log.Warning("警告信息");
Log.Error("错误信息");
Log.Fatal("严重错误，通常需要停止游戏");

// 条件断言
Log.Assert(condition, "条件不满足时的提示");

// 格式化
Log.Info($"玩家 {playerId} 死亡，位置 {pos}");
```

**发布规则**：`Log.Debug` 在非 Development 包中会被自动剥离；`Log.Error` 和 `Log.Fatal` 始终保留。

---

## LocalizationModule 本地化

```csharp
// 获取本地化字符串
string text = GameModule.Localization.GetString("key");

// 带参数
string text = GameModule.Localization.GetString("key", arg1, arg2);

// 切换语言
GameModule.Localization.Language = Language.ChineseSimplified;

// 查询当前语言
Language lang = GameModule.Localization.Language;
```

**集成说明**：TEngine 集成 I2Localization 编辑器工具（`Assets/Editor/I2Localization/`），本地化数据需在 I2Localization 面板中配置，并正确标记资源供 YooAsset 管理。

---

## 框架 v6.2.x 修订亮点（同步上游）

> 来自 TEngine 6.2.0 / 6.2.1 release notes，仅补充已有功能描述空白点，不引入功能新增。

### GameModule 完整入口（之前 references 未列）

```csharp
GameModule.Base       // RootModule         — 根模块（框架初始化入口）
GameModule.Debugger   // IDebuggerModule    — 调试器（运行时按 ~ 键呼出）
GameModule.Procedure  // IProcedureModule   — 流程
```

`Base` 通过 `FindObjectOfType<RootModule>()` 获取（非 `ModuleSystem.GetModule` 路径），其余通过 `ModuleSystem.GetModule<T>()` 获取并缓存。`UI` 属性类型是 `UIModule`（实现单例，不是 `IUIModule` 接口）。

### SceneModule 进度回调真实参数名

```csharp
Scene scene = await GameModule.Scene.LoadSceneAsync(
    "SceneName",
    LoadSceneMode.Single,
    progressCallBack: p => Log.Info($"加载进度: {p:P}")  // 0~1
);
```

> 真实参数名 `progressCallBack`（驼峰大小写不严格，但确为框架实际签名）。

### Log 系统完整 API

```csharp
Log.Debug(string);    // 仅 Development Build 输出，发布包条件剥离
Log.Info(string);     // 普通信息
Log.Warning(string);  // 警告
Log.Error(string);    // 错误，始终保留（NUnit 中触发隐式 fail）
Log.Fatal(string);    // 严重错误
Log.Assert(bool, string);  // 断言失败时输出
```

发布规则：`Log.Debug` 在非 Development 包中由编译器条件剥离；`Log.Error` / `Log.Fatal` 始终保留。

---

## Sprint 5 累计 framework drift batch

> Sprint 5 实施过程发现的 ADR-028 vs 实际 framework API 偏差，待 Sprint 5 retro 时 batch 入 ADR；先在此 reference 标注真实 API。

### drift-v1: AudioModule 初始化签名

ADR-028 §1 原描述用了 `Activate()` 空参签名，实测真实 API：

```csharp
GameModule.Audio.Initialize(Settings.AudioSetting.audioGroupConfigs);
```

入参是 `AudioGroupConfig[]`（音频组配置数组）。框架**没有**空参 `Activate()` 重载。

### drift-v2-(a): AudioModule 自动 Initialize 已生效

`AudioModule.OnInit()` 框架内部已自动调用 `Initialize(Settings.AudioSetting.audioGroupConfigs)`，业务侧**不需要**再次手动调用：

```csharp
// AudioModule.OnInit 框架内部：
public override void OnInit()
{
    Initialize(Settings.AudioSetting.audioGroupConfigs);
    // ...
}
```

业务侧直接 `GameModule.Audio.Play(...)` 即可。重复 `Initialize` 是无害的（idempotent）但徒增噪音。
