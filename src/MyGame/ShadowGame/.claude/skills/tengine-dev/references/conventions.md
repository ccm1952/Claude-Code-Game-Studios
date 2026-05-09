# TEngine 代码规范与设计模式

## 目录

- [命名规范](#命名规范)
- [UI 节点命名规范](#ui-节点命名规范)
- [异步编程规范](#异步编程规范)
- [禁止的代码模式](#禁止的代码模式)
- [推荐的代码模式](#推荐的代码模式)
- [模块设计规范](#模块设计规范)
- [代码审查清单](#代码审查清单)
- [Git 工作流](#git-工作流)

---

## 命名规范

### C# 类型命名

| 类型 | 规范 | 示例 |
|------|------|------|
| 模块接口 | `IXxxModule` | `IResourceModule` |
| 模块实现 | `XxxModule` | `ResourceModule` |
| 事件接口 | `IXxxEvent` / `IXxxUI` + `[EventInterface]` | `ILoginUI` |
| UIWindow 子类 | `XxxUI` / `XxxWindow` | `BattleMainUI` |
| UIWidget 子类 | `XxxWidget` / `XxxItem` | `SkillSlotWidget` |
| 流程状态 | `ProcedureXxx` | `ProcedureLogin` |
| 状态机状态 | `XxxState` | `IdleState` |
| 系统类 | `XxxSystem` | `LoginSystem` |
| 配置类（Luban 生成） | `TbXxx` / `Xxx`（行数据类） | `TbItem` / `Item` |
| 内存池对象 | 实现 `IMemory` | `DamageInfo : IMemory` |

### 字段命名

```csharp
public class BattleMainUI : UIWindow
{
    // 私有字段：小驼峰，下划线前缀（组件引用加类型前缀见下方UI规范）
    private int _currentHp;
    private Button _btnAttack;
    private Text _txtHp;
    
    // 私有静态：下划线前缀
    private static int _instanceCount;
    
    // 常量：全大写下划线
    private const int MAX_LEVEL = 100;
    
    // 公开属性：大驼峰
    public int CurrentHp => _currentHp;
    
    // 事件/委托字段：大驼峰
    public event Action OnDead;
}
```

### 方法命名

```csharp
// 异步方法：Async 后缀（返回 UniTask）
public async UniTask LoadDataAsync() { }

// 事件回调：On 前缀 + 事件名
private void OnHpChanged(int hp) { }
private void OnBtnAttackClicked() { }

// 初始化类：Init / Initialize / Setup
private void InitUI() { }

// 工厂/创建类：Create
private ItemWidget CreateItemWidget(ItemConfig cfg) { }
```

---

## UI 节点命名规范

Prefab 节点命名前缀决定 `UIScriptGenerator` 自动生成的绑定类型（来自 `ScriptGeneratorSetting.asset`）：

| 前缀 | 生成类型 | 示例节点名 |
|------|---------|----------|
| `m_go_` | `GameObject` | `m_go_Effect` |
| `m_item_` | `UIWidget`（子类）| `m_item_Slot` |
| `m_tf_` | `Transform` | `m_tf_Container` |
| `m_rect_` | `RectTransform` | `m_rect_Panel` |
| `m_text_` | `Text` | `m_text_Title` |
| `m_tmp_` | `TextMeshProUGUI` | `m_tmp_Name` |
| `m_richText_` | `RichTextItem` | `m_richText_Desc` |
| `m_btn_` | `Button` | `m_btn_Start` |
| `m_img_` | `Image` | `m_img_Icon` |
| `m_rimg_` | `RawImage` | `m_rimg_Avatar` |
| `m_toggle_` | `Toggle` | `m_toggle_Sound` |
| `m_group_` | `ToggleGroup` | `m_group_Tab` |
| `m_slider_` | `Slider` | `m_slider_Volume` |
| `m_input_` | `InputField` | `m_input_Name` |
| `m_tInput_` | `TMP_InputField` | `m_tInput_Search` |
| `m_scroll_` | `ScrollRect` | `m_scroll_List` |
| `m_scrollBar_` | `Scrollbar` | `m_scrollBar_Vert` |
| `m_grid_` | `GridLayoutGroup` | `m_grid_Items` |
| `m_hlay_` | `HorizontalLayoutGroup` | `m_hlay_Tabs` |
| `m_vlay_` | `VerticalLayoutGroup` | `m_vlay_List` |
| `m_canvasGroup_` | `CanvasGroup` | `m_canvasGroup_Fade` |
| `m_curve_` | `AnimationCurve` | `m_curve_Anim` |
| `m_canvas_` | `Canvas` | `m_canvas_Overlay` |
| `m_dropdown_` | `Dropdown` | `m_dropdown_Select` |
| `m_tmpInput_` | `TMP_InputField` | `m_tmpInput_Search` |
| `m_tmpDropdown_` | `TMP_Dropdown` | `m_tmpDropdown_Lang` |
| `m_richText_` | `RichTextItem` | `m_richText_Desc` |

不需要绑定的节点无需加前缀，UIScriptGenerator 会忽略。

### 前缀匹配顺序警示（同步上游 v6.2.x）

`ScriptGeneratorSetting.asset` 中 regex 按序匹配，长前缀必须先于短前缀声明：

| 必须先匹配 | 才能匹配 | 否则会被误识别为 |
|-----------|---------|--------------|
| `m_scrollBar` | `m_scroll` | `m_scrollBar_X` 会被 `m_scroll` 先匹配 → 生成 `ScrollRect` |
| `m_tmpInput` | `m_tmp` | `m_tmpInput_X` 会被 `m_tmp` 先匹配 → 生成 `TextMeshProUGUI` |
| `m_tmpDropdown` | `m_tmp` | 同上 |
| `m_richText` | `m_text` | `m_richText_X` 会被 `m_text` 先匹配 → 生成 `Text` |

新增前缀时检查 `Assets/TEngine/Settings/Resources/ScriptGeneratorSetting.asset` 的 `uiElementRegex` 字段顺序。

### 旧 m_tInput_ 已废弃（v6.2.0 修订）

```
❌ m_tInput_Search     // 旧前缀，已被废弃
✅ m_tmpInput_Search   // 新前缀，统一 TMP 命名
```

旧节点重命名为 `m_tmpInput_` 后重新生成绑定。

---

## 异步编程规范

### 基本规则

```csharp
// ✅ 异步方法返回 UniTask（有返回值）
public async UniTask<int> GetPlayerLevelAsync()
{
    var data = await LoadPlayerDataAsync();
    return data.Level;
}

// ✅ 无返回值异步方法（fire and forget）：返回 UniTaskVoid
public async UniTaskVoid StartBattleAsync()
{
    await LoadBattleResourcesAsync();
    EnterBattle();
}

// ✅ 调用 UniTaskVoid 方法不需要 await，加 .Forget() 忽略警告
StartBattleAsync().Forget();

// ✅ 也可以直接调用有返回值的 UniTask（不关心结果时）
GameModule.UI.ShowUIAsync<LoginUI>(); // 不 await 也不会有警告（框架内部处理）
```

### CancellationToken 使用

```csharp
// ✅ 异步加载时传递 CancellationToken，防止对象销毁后回调执行
private CancellationTokenSource _cts = new CancellationTokenSource();

private async UniTaskVoid LoadAsync()
{
    try
    {
        var asset = await GameModule.Resource.LoadAssetAsync<Sprite>("icon", _cts.Token);
        _img.sprite = asset;  // 取消后这里不会执行
    }
    catch (OperationCanceledException)
    {
        // 正常取消，不报错
    }
}

protected override void OnDestroy()
{
    _cts.Cancel();
    _cts.Dispose();
}
```

### 并发加载

```csharp
// ✅ 多个资源并发加载（提速）
var (sprite, prefab, config) = await UniTask.WhenAll(
    GameModule.Resource.LoadAssetAsync<Sprite>("hero_icon"),
    GameModule.Resource.LoadGameObjectAsync("HeroModel"),
    GameModule.Resource.LoadAssetAsync<TextAsset>("hero_config")
);

// ✅ 批量加载
var tasks = locations.Select(loc => GameModule.Resource.LoadAssetAsync<Sprite>(loc));
var sprites = await UniTask.WhenAll(tasks);
```

### 禁止的异步模式

```csharp
// ❌ 禁止：Task（应使用 UniTask）
public async Task LoadAsync() { }

// ❌ 禁止：Coroutine（应使用 async/await）
IEnumerator LoadCoroutine() { yield return null; }

// ❌ 禁止：在 Update 中 await（错误用法）
void Update()
{
    await SomeAsync(); // 编译错误，但警惕逻辑错误
}

// ❌ 禁止：忽略 UniTask 警告（应加 .Forget() 或 await）
SomeUniTaskVoidMethod(); // 警告
```

---

## 禁止的代码模式

```csharp
// ❌ 禁止 Resources.Load
var sprite = Resources.Load<Sprite>("icon"); // 使用 GameModule.Resource

// ❌ 禁止直接 Instantiate Prefab
var go = Instantiate(heroPrefab); // 使用 GameModule.Resource.LoadGameObjectAsync

// ❌ 禁止 Object.FindObjectOfType（性能差）
var player = FindObjectOfType<Player>();

// ❌ 禁止在 Update 中频繁创建对象
void Update()
{
    var data = new DamageData(); // GC 压力，使用 MemoryPool
}

// ❌ 禁止直接持有跨模块的强引用（用事件替代）
public BattleSystem battleSystem; // 用 GameEvent.Get<IBattleEvent>()

// ❌ 禁止在 UI 外部直接访问 UI 组件
var ui = FindObjectOfType<BattleMainUI>(); // 用 GameModule.UI.GetUIAsync<T>
ui._btnAttack.onClick.AddListener(...);   // 禁止外部访问私有组件

// ❌ 禁止静态持有 Asset 引用
public static Sprite HeroIcon; // 导致无法卸载，内存泄漏

// ❌ 禁止忽略 async 返回值（会吞异常）
GameModule.Resource.LoadAssetAsync<Sprite>("icon"); // 要么 await，要么 .Forget() + 错误处理
```

---

## 推荐的代码模式

```csharp
// ✅ 对象池复用频繁创建的对象
var dmg = MemoryPool.Acquire<DamageInfo>();
dmg.Damage = 100;
ProcessDamage(dmg);
MemoryPool.Release(dmg);

// ✅ 事件驱动的模块间通信
GameEvent.Get<IBattleEvent>().OnPlayerDead(playerId);

// ✅ 通过 GameModule 访问模块（不直接持有引用）
GameModule.Audio.Play(AudioType.Sound, "attack_sfx");

// ✅ 读取配置表（单次获取，不重复 Get）
var cfg = ConfigSystem.Instance.Tables.TbSkill.Get(skillId);
if (cfg == null) { Log.Error($"找不到技能配置: {skillId}"); return; }

// ✅ 场景间数据传递通过数据管理类，不放全局变量
GameDataMgr.Instance.PlayerLevel = 10;
```

---

## 模块设计规范

### 新增业务模块（System）

```csharp
// ✅ 单例系统模板
public class LoginSystem
{
    private static LoginSystem _instance;
    public static LoginSystem Instance => _instance ??= new LoginSystem();

    private readonly GameEventMgr _eventMgr = new GameEventMgr();

    public void Initialize()
    {
        _eventMgr.AddEvent<string>(GameEventDef.OnAccountLogin, OnAccountLogin);
    }

    public void Dispose()
    {
        _eventMgr.Clear();
        _instance = null;
    }

    private void OnAccountLogin(string account) { /* 处理登录 */ }
}
```

### 新增热更模块（继承 TEngine 模块）

```csharp
// 仅在需要帧驱动（Update）的模块才继承 Module
public interface IMyCustomModule : IModule
{
    void DoSomething();
}

public class MyCustomModule : Module, IMyCustomModule
{
    public override void Update(float elapseSeconds, float realElapseSeconds)
    {
        // 每帧逻辑
    }

    public void DoSomething() { }
}

// 注册（在 GameApp 初始化时）
ModuleSystem.GetModule<IMyCustomModule>();
```

---

## 代码审查清单

提交代码前自查：

**资源管理**
- [ ] 每个 `LoadAssetAsync` 都有对应 `UnloadAsset`（或使用 `LoadGameObjectAsync`）
- [ ] 没有使用 `Resources.Load()` 或 `Instantiate()`
- [ ] 没有静态变量持有 Unity Asset 引用

**异步编程**
- [ ] 没有使用 `Task`（应使用 `UniTask`）
- [ ] 没有使用 `Coroutine`（应使用 `async/await`）
- [ ] 有异步操作的类都在 `OnDestroy` 中取消 `CancellationToken`

**事件系统**
- [ ] 非 UIWindow 类中注册的事件都有对应的 `RemoveEventListener` 或使用 `GameEventMgr`
- [ ] UI 内部事件都在 `RegisterEvent()` 中用 `AddUIEvent` 注册

**热更代码**
- [ ] 没有反向引用主包 internal 类型
- [ ] 新的泛型组合已添加到 AOT 引用（如有 HybridCLR AOT 错误）

**性能**
- [ ] 没有在 `Update` 中频繁创建对象（改用对象池）
- [ ] 没有在 `Update` 中做字符串拼接（改用 Timer + 缓存）
- [ ] 列表/滚动视图使用了 Widget 复用（`AdjustIconNum`）

---

## 测试 fixture 规范（EditMode / NUnit）

> 触发背景：S2-10 GridSnapTests 漏复用 stub Camera 导致 SetUp 阶段触发 fail-loud `Log.Error`，被 Unity Test Runner 视为 11/11 全失败。详见 `/.claude/memory/problem_2026-04-29_test-fixture-fail-loud-boilerplate-skip.md`。

### 关键事实（Unity NUnit 隐式行为）

- **`[SetUp]` / `[Test]` 阶段任何未 expect 的 `LogType.Error` 都会让该测试自动失败**——即使代码没抛异常、没 `Assert.Fail`。
- `Debug.LogError` / `TEngine.Log.Error` / `Console.LogError` 都属于 `LogType.Error`。
- 容忍单个预期 ERROR 的唯一合规方式：`LogAssert.Expect(LogType.Error, regex)`（在该 `Log.Error` 调用**之前**注册）。
- **禁止**用 `LogAssert.ignoreFailingMessages = true` 全局放行——会掩盖真正的 fixture bug。

### R1 — 同 epic 新 fixture 必须**整段**复用既有 fixture 的 boilerplate

新建测试 fixture（同一 epic / system）时，第一动作是读同 epic 内已有 fixture 的 `SetUp` / `TearDown` / `MakeXxx` 工厂方法**整段**复用，而非选择性挑取。

| 操作 | 是否允许 |
|---|---|
| 增加新 stub（本 system 多出来的依赖） | ✅ 允许 |
| 减少 dispatcher 桩（如本 fixture 不依赖 `IGestureEvent`，可不实例化 `IGestureEvent_Gen`，但**注释**说明"不依赖 X"） | ✅ 允许，必须注释 |
| 减少 fail-loud 校验涉及的 stub（camera / config provider / 其他 `Initialize` 校验依赖） | ❌ 禁止 |
| 减少 `_ = new IXxxEvent_Gen(dispatcher)` 注册（多个 fixture 行为一致时） | ⚠️ 需评估，缺失会导致事件发不出去而 silent fail |

**判断哪些 stub 必备的方法**：

```bash
# Grep 被测类的 Log.Error，每条触发条件对应一个必备 stub
rg "Log\.Error" Assets/GameScripts/HotFix/GameLogic/<system>/<class>.cs
```

每一条 `Log.Error` 都对应 SetUp 中的 stub 注入（如 `_gameplayCamera`、`PuzzleConfigProvider`、`_objectId`、`_puzzleId` 等）。

### R2 — fail-loud 改造与测试 fixture 同步审查

被测组件的某条路径从 silent return / `enabled = false` 改成 `Log.Error`（fail-loud）时，**同一次提交**必须：

1. `rg "AddComponent<<class>>" Assets/Tests/` 列出所有引用此组件的测试 fixture
2. 每个 fixture 的 SetUp 是否已注入新 fail-loud 校验对应的 stub？没有 → 同提交补齐
3. 在 ADR / story `Implementation Notes` 段标注："fail-loud 校验 X 已审查 N 个 fixture，全部已注入 stub"

跳过此审查 = 等下次跑测试时 boom 全红。

### R3 — 预期 ERROR 用 `LogAssert.Expect` 显式声明

测试**期望**触发 ERROR log 的场景（如"未注册 provider 应日志 Error"），必须用：

```csharp
[Test]
public void AC5_OnEnable_LogsError_WhenProviderNotRegistered()
{
    LogAssert.Expect(LogType.Error,
        new System.Text.RegularExpressions.Regex(@"PuzzleConfigProvider 未注册"));
    InteractableObject.ClearPuzzleConfigProviderForTest();
    var go = MakeInteractableObject(out var io, objectId: 7, puzzleId: 0);
    // … 略
}
```

参考实现：`DragMechanicsTests.cs` AC5_* 测试（行 266 / 283）。

### 新 fixture 创建 checklist

写每个新 `[TestFixture]` 前 **逐条勾选**：

- [ ] 已读同 epic 已有 fixture 的 SetUp / TearDown / Make 工厂方法**整段**
- [ ] 已 `rg "Log\.Error"` 被测类，每条 ERROR 触发条件对应：① 一个 SetUp 注入 stub  或 ② 一个 `LogAssert.Expect`（针对该测试）
- [ ] 测试 asmdef 引用 `GameLogic` + `TEngine.Runtime`（参 `/.claude/memory/problem_2026-04-22_asmdef-source-generator.md`）
- [ ] `MakeXxx` 工厂方法显式调 `Initialize()`（EditMode 中 PlayerLoop 不驱动 `OnEnable`，参 `/.claude/memory/problem_2026-04-29_*editmode-lifecycle*.md` 路线图，如尚未独立成 memo 见 `story-002-drag-mechanics.md` X1 patch v3）
- [ ] `TearDown` 中销毁所有 SetUp 创建的 GameObject + 调用 `Shutdown()` + 清 reflection-注入的全局状态（如 `ClearPuzzleConfigProviderForTest`）

### 抽 fixture base class 的触发阈值（P2）

当某 epic 出现 ≥ 3 个测试 fixture 复用同样 boilerplate 时，触发"抽 abstract base class"动作（如 `InteractableObjectFixtureBase`），子类只 override 必要差异。低于 3 个 fixture 时**不**预先抽，避免抽象失误。

---

## Listener 端订阅模式（GameEvent / EventMgr）

> **背景**：S2-12 InteractionLockManager 实施时发现 story-006 §Implementation Notes 用了"整接口订阅"风格 `GameEvent.AddEventListener<IInteractionEvent>(this)` —— 与 TEngine 实际 API 不符。详见 `/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md`。

### 唯一支持的 listener 注册模式（per-event）

```csharp
// ✅ 正确：per-event 签名
GameEvent.AddEventListener<TArg1>(int eventType, Action<TArg1> handler);
GameEvent.AddEventListener<TArg1, TArg2>(int eventType, Action<TArg1, TArg2> handler);
// ... 最多到 6 args
GameEvent.AddEventListener(int eventType, Action handler);   // 无参版本

// 实际示例（InteractionLockManager.Init）
GameEvent.AddEventListener<string>(
    IInteractionEvent_Event.OnRequestPuzzleLockAll, OnRequestPuzzleLockAll);
GameEvent.AddEventListener<int>(
    ISceneEvent_Event.OnSceneUnloadBegin, OnSceneUnloadBegin);
```

EventId 常量来自 `IXxxEvent_Event` 静态类（由 GameEventSourceGenerator 生成；与接口方法一一对应）。

### 禁止的 listener 注册模式（不存在的 API）

```csharp
// ❌ 错误：整接口订阅 — 没有这种签名
GameEvent.AddEventListener<IInteractionEvent>(this);
GameEvent.AddEventListener<ISceneEvent>(this);

// ❌ 错误：误用 sender 端代理注册做 listener
EventMgr.RegWrapInterface<IInteractionEvent>(this);   // 这是 sender 端
```

`EventMgr.RegWrapInterface<T>(T)` 是 **sender 端**注册 —— 让 source generator 生成的 `IXxxEvent_Gen` 类拿到 dispatcher 用于 `GameEvent.Get<TInterface>().OnYyy(...)` 派发；**不**是 listener 端 API。

### 配套 sender 端

```csharp
// ✅ 正确：通过 facade 派发（ADR-027 §3 推荐）
GameEvent.Get<IInteractionEvent>().OnInteractionLockChanged(true);
GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(chapterId);
```

### POCO 监听器实施模板（推荐）

POCO（不继承 MonoBehaviour）的监听器**不**实施 `IXxxEvent` 接口本身 —— 仅按需 per-event 注册关心的事件：

```csharp
public sealed class MyManager
{
    private bool _listenersRegistered;

    public void Init()
    {
        if (_listenersRegistered) return;
        GameEvent.AddEventListener<string>(IFooEvent_Event.OnBar, OnBar);
        _listenersRegistered = true;
    }

    public void Dispose()
    {
        if (_listenersRegistered)
        {
            GameEvent.RemoveEventListener<string>(IFooEvent_Event.OnBar, OnBar);
            _listenersRegistered = false;
        }
    }

    private void OnBar(string arg) { /* ... */ }
}
```

### 反例（之前 story 模板的错误写法）

```csharp
// ❌ 不要这样写 — 与 framework 不符（TEngine 不支持整接口订阅）
public sealed class MyManager : IFooEvent, IBarEvent
{
    public void Init()
    {
        GameEvent.AddEventListener<IFooEvent>(this);    // ← 编译报错或 silently 不工作
        GameEvent.AddEventListener<IBarEvent>(this);    // ← 同上
    }

    void IFooEvent.OnBar(string arg) { /* listener */ }
    // 还要实施所有其他接口方法（噪音 + 误导）
}
```

### Self-checklist（dev-story 起手）

- [ ] §Implementation Notes 中的 `GameEvent.AddEventListener<...>(...)` 调用是否符合 per-event 签名？
- [ ] 是否避免了 `class : IXxxEvent + AddEventListener<TInterface>(this)` 反模式？
- [ ] 反复发现的 drift 是否触发 Problem→Rule promotion 协议？

---

## Git 工作流

```
main / master       ← 稳定版本，禁止直接提交
feature/xxx         ← 功能开发分支
fix/xxx             ← Bug 修复分支
hotfix/xxx          ← 线上紧急修复
```

**提交信息格式**：

```
feat: 添加登录界面 UI
fix: 修复资源加载后引用计数未减一的问题
refactor: 重构战斗系统事件通信方式
perf: 优化技能列表 Widget 复用逻辑
docs: 更新热更开发规范文档
```

**分支合并前检查**：
1. Unity 编译无错误无警告
2. 真机（Android/iOS）运行正常
3. 内存占用无明显增长（调试器模块监控）
4. 已自查代码审查清单
