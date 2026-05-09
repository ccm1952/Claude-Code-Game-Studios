# TEngine 常见问题排障

## 目录

- [HybridCLR 问题](#hybridclr-问题)
- [YooAsset 资源问题](#yooasset-资源问题)
- [UI 模块问题](#ui-模块问题)
- [事件系统问题](#事件系统问题)
- [内存与性能问题](#内存与性能问题)
- [Luban 配置表问题](#luban-配置表问题)
- [UniTask 问题](#unitask-问题)

---

## HybridCLR 问题

### 问题：ExecutionEngineException: AOT code was not generated

**原因**：热更代码使用了主包没有 AOT 实例的泛型组合。

**排查步骤**：
1. 查看错误堆栈，找到具体的类型/方法名
2. 在 `Assets/HybridCLRData/AOTGenericReferences.cs` 中找是否缺少该泛型
3. 菜单：`HybridCLR → Generate → AOT Generic References` 重新生成
4. 若仍缺失，在文件中手动添加一个占位引用：
   ```csharp
   // 在 AOTGenericReferences.cs 中
   static void UseCustomGenericType()
   {
       _ = new List<MyCustomType>();  // 添加这行触发 AOT 生成
   }
   ```
5. 重新打包主包

---

### 问题：热更代码在 Editor 模式正常，真机报错

**原因**：Editor 模式下 Mono 编译，不走 HybridCLR；真机才会走 IL2CPP + HybridCLR 解释。

**排查步骤**：
1. 在 Android 上先测试（比 iOS 快）
2. 检查是否有 `dynamic`、`Emit` 等不支持的 C# 特性
3. 检查反射代码（`Type.GetMethod`、`Activator.CreateInstance` 等）在 AOT 环境是否受限
4. 开启 `Development Build` + `Script Debugging` 获取完整堆栈

---

### 问题：热更 DLL 加载失败 / 程序集找不到

**排查步骤**：
1. 检查 `HybridCLRSettings.asset` 中热更程序集列表是否包含所有 HotFix 程序集
2. 确认热更 DLL 已打进 AssetBundle（YooAsset 收集器配置中有 `HybridCLRData` 目录）
3. 检查 `ProcedureLoadAssembly.cs` 中加载的 DLL 文件名与实际生成的 DLL 名一致
4. 确认资源服务器上的 DLL 版本是最新的

---

### 问题：iOS 打包失败，提示找不到某些符号

**原因**：Burst / IL2CPP 链接器裁剪了部分代码。

**解决**：
```
在 Assets/link.xml 中保留需要的类型：
<linker>
  <assembly fullname="UnityEngine">
    <type fullname="UnityEngine.Rigidbody" preserve="all"/>
  </assembly>
</linker>
```

---

## YooAsset 资源问题

### 问题：资源加载失败，location 无效

**排查步骤**：
1. 检查 `AssetBundleCollector` 中是否有收集该路径的资产
2. 确认 location 写法（不含路径、不含扩展名，如 `HeroPrefab` 而非 `Actor/Hero/HeroPrefab.prefab`）
3. 调用 `GameModule.Resource.CheckLocationValid("location")` 返回 false 说明未收集
4. 确认已重新打包资源（Editor 模式使用模拟器时可能引用旧的清单）

---

### 问题：资源加载成功但内存一直增长

**排查步骤**：
1. 打开 TEngine 调试器（`~` 键或 DebuggerModule 按钮）查看资源引用计数
2. 找到引用计数不为 0 的资产，定位到加载位置
3. 常见原因：
   - 加载后忘记 `UnloadAsset`
   - 静态变量持有 Asset 引用
   - UI 关闭但 Widget 里的 Asset 未释放
4. 场景切换后调用：
   ```csharp
   GameModule.Resource.ForceUnloadUnusedAssets(gcCollection: true);
   ```

---

### 问题：热更后旧资源未更新（缓存问题）

**排查步骤**：
1. 检查 CDN 服务器上的资源版本文件是否更新
2. 确认 `RequestPackageVersionAsync` 获取到了新版本号
3. 检查 `UpdatePackageManifestAsync` 是否成功执行
4. 本地测试时清空 `Application.persistentDataPath` 下的缓存目录

---

## UI 模块问题

### 问题：UI 打开后界面空白 / 节点找不到

**排查步骤**：
1. 检查 `[Window]` 特性中 `location` 是否与 Prefab 文件名一致
2. 确认 Prefab 在 `AssetRaw/UI/Prefabs/` 下，且 YooAsset 已收集
3. 检查 `ScriptGenerator` 中的路径是否与 Prefab 节点层级一致
4. 在 `ScriptGenerator` 中加日志检查 `FindChild` 是否返回 null

---

### 问题：UIWidget 数量不对 / 复用有问题

**排查步骤**：
1. 检查 `AdjustIconNum` 调用时 `count` 参数是否正确
2. 确认 `OnRefresh` 在每次 Widget 显示时都被调用（框架保证）
3. 复用场景中检查 `SetData` 是否正确重置了所有状态（旧数据可能残留）

---

### 问题：UI 事件回调在对象销毁后仍触发

**原因**：在 `RegisterEvent()` 外使用了 `GameEvent.AddEventListener`，未自动清理。

**解决**：
```csharp
// ❌ 错误位置
protected override void OnCreate()
{
    GameEvent.AddEventListener<int>(EventId.OnHp, OnHp); // 不会自动清理
}

// ✅ 正确位置
protected override void RegisterEvent()
{
    AddUIEvent<int>(EventId.OnHp, OnHp); // 自动随窗口销毁清理
}
```

---

## 事件系统问题

### 问题：接口事件调用无响应

**排查步骤**：
1. 确认 `GameEventHelper.Init()` 在 `GameApp.Entrance` 中已调用
2. 确认接口上有 `[EventInterface]` 特性
3. 确认实现类已通过 `GameEventHelper.RegisterListener<IXxx>()` 注册
4. 检查接口事件的 Source Generator 是否已重新生成（修改接口后需要重新编译）

---

### 问题：int 事件发送后有些监听收不到

**原因**：参数类型不匹配（`Send<float>` vs `AddEventListener<int>`）。

**排查**：严格保证 `Send<T>` 和 `AddEventListener<T>` 的泛型类型完全一致。

---

## 内存与性能问题

### 问题：GC 频繁导致卡顿

**排查步骤**：
1. 在 Profiler 中查看 GC Alloc 热点
2. 常见原因：
   - 字符串拼接（用 `StringBuilder` 或插值，避免在 Update 中）
   - 闭包捕获（Lambda 中捕获变量会生成对象）
   - `Linq` 操作（每次都分配中间集合）
   - 频繁 `new` 数据对象（改用 `MemoryPool.Acquire<T>()`）

### 问题：启动加载时间过长

**优化策略**：
1. 减少 `PRELOAD` 标签的资源数量，只预加载核心资源
2. 使用进度回调展示加载进度，改善感知体验
3. 对于大型 Prefab 使用异步实例化（Unity 的 `InstantiateAsync`，或分帧创建）

### 问题：DrawCall 过高

**排查步骤**：
1. Frame Debugger 查看合批情况
2. UI：确保同 Atlas 的图片在同一 Canvas 下，使用 Sprite Atlas 自动合批
3. 3D：使用 GPU Instancing 或 Static Batching
4. 确认材质和 Shader 一致（不一致的材质无法合批）

---

## Luban 配置表问题

### 问题：生成脚本报错 / 表格式不对

**排查步骤**：
1. 确认 Excel 第2行类型拼写正确（`int`/`string`/`float` 等，区分大小写）
2. 数组类型在 Excel 单元格中用英文逗号分隔（`1,2,3`）
3. Bean 类型需要先在 `__beans__.xlsx` 中定义
4. 表注册的 `value_type` 必须与数据 Excel 的 Bean 类型名完全一致

---

### 问题：运行时找不到配置数据 / Tables 为 null

**排查步骤**：
1. 确认 `ConfigSystem.Instance.Load()` 已在 `ProcedurePreload` 后调用
2. 检查 `.bytes` 数据文件是否在 `AssetRaw/Configs/bytes/` 下
3. 确认数据文件已被 YooAsset AssetBundleCollector 收集（有 `PRELOAD` 标签）
4. 在 Editor 模式检查 `Resources.Load<TextAsset>("bytes/item")` 是否能加载（Debug 用）

---

## UniTask 问题

### 问题：UniTask 异常被吞掉，找不到错误

**原因**：`UniTaskVoid` 方法中的异常默认不会传播到调用方。

**解决**：
```csharp
// 方式1：在 UniTaskVoid 方法中 try-catch
public async UniTaskVoid DoSomethingAsync()
{
    try
    {
        await SomeOperation();
    }
    catch (Exception e)
    {
        Log.Error($"异步操作失败: {e}");
    }
}

// 方式2：设置全局未处理异常回调（在 GameApp 初始化时）
UniTaskScheduler.UnobservedExceptionHandler = e =>
{
    Log.Error($"未处理的 UniTask 异常: {e}");
};
```

### 问题：await 后 Unity 对象变成 null

**原因**：await 期间对象被销毁（场景切换、UI 关闭等）。

**解决**：
```csharp
var sprite = await GameModule.Resource.LoadAssetAsync<Sprite>("icon", _cts.Token);

// await 返回后检查对象是否还存在
if (this == null || _imgIcon == null) return; // Unity Object 的 null 检查

_imgIcon.sprite = sprite;
```

---

## 框架 v6.2.x 速查表（同步上游）

> 来自 TEngine 6.2.1 references "FAQ 章节 + quick reference 表" sync。集中处理 AI 编码时容易写错的高频签名。

### UIWindow 生命周期签名速查

```csharp
// ✅ 正确签名（UIBase 中定义）
protected virtual void OnCreate()      // 无参数
protected virtual void OnRefresh()     // 无参数
protected virtual void OnUpdate()      // 无参数
protected virtual void OnDestroy()     // 无参数（非 ProcedureBase.OnDestroy(ProcedureOwner)）

// ❌ 常见错误签名
protected override void OnCreate(object userData)   // 不存在
protected override void OnDestroy(bool isShutdown)  // 这是 ProcedureBase 的签名
```

UIWindow 数据通过 `UserData` 属性获取，不通过参数传入。

### SetSprite callback 类型速查

```csharp
// Image 扩展方法
void SetSprite(this Image image, string location,
    bool setNativeSize = false,
    Action<Image> callback = null,           // ← Action<Image>，不是 Action<Sprite>
    CancellationToken cancellationToken = default)

// SpriteRenderer 扩展方法
void SetSprite(this SpriteRenderer spriteRenderer, string location,
    Action<SpriteRenderer> callback = null,  // ← Action<SpriteRenderer>
    CancellationToken cancellationToken = default)

// SetSubSprite 没有 callback 重载（Image / SpriteRenderer 两版）
```

### GameEvent 清理方法速查

```csharp
// ✅ UI 内部事件自动清理
AddUIEvent(eventType, handler);  // UIWindow.OnDestroy 自动 RemoveAllUIEvent

// ✅ 单个监听移除
GameEvent.RemoveEventListener(eventType, handler);

// ✅ 局部批量清除（GameEventMgr）
_eventMgr.Clear();

// ✅ 全局清除（仅游戏退出时）
GameEvent.Shutdown();

// ❌ 不存在的方法（AI 常见幻觉）
GameEvent.UnRegisterAll();          // 编译错误
GameEvent.UnRegisterAll<T>();       // 编译错误
GameEvent.RegisterListener<T>();    // 编译错误（Source Generator 自动注册）
GameEvent.ClearAll();               // 编译错误
GameEvent.RemoveAll(eventId);       // 编译错误
```

---

## Sprint 5 spike-design parity check（lessons learned）

> 触发：S5-06 Audio Manager spike 在 PlayMode 验证发现"功能正确但 1 处与设计文档不一致"——`ISettingsEvent.OnSettingChanged` 真签名是 `(string, string)` 而非 design doc 写的 `(string, object)`。

### Self-check 流程

spike / story 实施前，对每个引用的 framework 接口做一次 grep verify：

```bash
# 验证 IEvent 接口真签名
rg "interface IXxxEvent" Assets/GameScripts/HotFix/GameLogic/IEvent/

# 验证 framework module API 真签名
rg "void Initialize" Assets/TEngine/Runtime/Module/AudioModule/
```

### Drift 处理协议

design vs framework 出现 drift 时：

1. **以 framework 为准**（design 不能改 framework 行为）
2. drift 写入当前 story 的 `## Implementation Notes` 段
3. retro 时 batch 修订涉及的 ADR / GDD（不在 sprint 内做）
4. 触发 ≥ 3 次 drift 即升级为正式协议（参 ADR-029 V2.0 governance）
