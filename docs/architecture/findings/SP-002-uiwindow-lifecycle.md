// 该文件由Cursor 自动生成

# SP-002 Findings: UIWindow 生命周期调用时序

> **Status**: ✅ 已验证（源码审查）
> **Date**: 2026-04-22
> **Source**: `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs`, `UIWindow.cs`

## 重要发现

**UIModule 不在 TEngine Runtime 中，而是在 GameLogic 热更程序集中实现。** 路径为 `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`。

## 生命周期时序

### 首次打开 (ShowWindow)

```
LoadAsync → Handle_Completed [资源加载完]
  → IsPrepare = true → callback
  → InternalCreate() [OnCreate]      ← 仅首次，_isCreate == false 时
  → InternalRefresh() [OnRefresh]    ← 每次打开
  → Show
```

### 重新打开 (窗口实例已存在)

```
TryInvoke → InternalRefresh() [OnRefresh]
```

`OnCreate` **不再调用**。

### OnUpdate 触发条件

```csharp
if (!IsPrepare || !Visible)
    return false; // OnUpdate 不执行
```

仅 `IsPrepare == true && Visible == true` 时每帧调用。

### HideTimeToClose 行为

| 值 | 行为 |
|----|------|
| ≤ 0 | 立即 `CloseUI` |
| > 0 | `Visible = false`, `IsHide = true` → Timer 到期后 `CloseUI` |

- `CancelHideToCloseTimer` 可取消定时关闭（清除 IsHide + RemoveTimer）
- Hide 期间 `Visible = false`，因此 **OnUpdate 不执行**

## 编码规范（基于发现）

| 回调 | 触发时机 | 推荐用途 |
|------|---------|---------|
| `OnCreate` | 首次加载后，调用一次 | 组件引用获取、GameEvent 注册、静态初始化 |
| `OnRefresh` | 每次打开（含首次）| 数据绑定、UI 内容更新（使用 userDatas 参数）|
| `OnUpdate` | 每帧，仅 Visible == true | 实时数据轮询（如 HintButton opacity）|
| `OnDestroy` | 真正销毁时 | GameEvent 注销、资源释放 |

## 行动项

- [x] 确认 OnCreate → OnRefresh 同帧顺序 — 已确认
- [x] 确认 Hide 期间 OnUpdate 不执行 — 已确认
- [x] UIModule 在 GameLogic 热更程序集 — 已确认（注意 ADR-011 需标注）
