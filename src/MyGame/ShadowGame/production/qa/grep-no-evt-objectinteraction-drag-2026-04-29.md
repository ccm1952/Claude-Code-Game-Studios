// 该文件由Cursor 自动生成

# Grep Evidence: S2-09 Drag with Boundary Clamping (ADR-006 / Camera.main 合规检测)

> **Story**: S2-09 — Touch-to-World Drag with Boundary Clamping
> **Date**: 2026-04-29 evening
> **Manifest**: story-002-drag-mechanics.md AC-6 (协议合规 grep)

---

## 检测项 1：ADR-006 Evt_DragGesture / Evt_TapGesture 残留

**Command**:
```
rg "EventId\.Evt_(DragGesture|TapGesture)" Assets/GameScripts/HotFix/
```

**Result**: **0 命中**

**结论**：✅ 全 HotFix 代码无 ADR-006 Drag/Tap gesture 常量引用。Drag 数据流改为 `IGestureEvent.OnDrag(GestureData)` per-method listener（push 模式），完全走 ADR-027 接口事件协议。

---

## 检测项 2：ObjectInteraction 子目录 Camera.main 调用

**Command**:
```
rg "Camera\.main" Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/
```

**Result**: 2 命中（皆**反向警示文案**，非实际调用）

| 行号 | 文件 | 命中文本 | 性质 |
|---|---|---|---|
| 49 | `InteractableObject.cs` | `[Tooltip("...禁止 Camera.main fallback —— ADR-012）。")]` | Inspector Tooltip 字符串 |
| 118 | `InteractableObject.cs` | `Log.Error($"...（ADR-012 禁止 Camera.main fallback）");` | Log 消息文本 |

**结论**：✅ 0 处实际调用 `Camera.main` 属性。`_gameplayCamera` 通过 `[SerializeField]` Inspector 注入；OnEnable 时若 null 直接 fail-loud 禁用组件（不 fallback）。符合 ADR-012 + story-002 AC-6。

---

## 检测项 3：ObjectInteraction 子目录 ADR-006 Evt_* 全量残留

**Command**:
```
rg "EventId\.Evt_" Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/
```

**Result**: 1 命中（**反向警示文案**，非实际调用）

| 行号 | 文件 | 命中文本 | 性质 |
|---|---|---|---|
| 44 | `InteractableObjectFsm.cs` | `/// 禁止使用 <c>EventId.Evt_*</c> 常量（ADR-006 全废）。` | XML 文档注释 |

**结论**：✅ 0 处实际调用 ADR-006 `EventId.Evt_*` 常量。Object Interaction 子系统全量切到 ADR-027 接口事件协议。

---

## Sender / Listener 协议路径合规

| 路径 | 实施方式 | 合规性 |
|---|---|---|
| InteractableObject 接收 IGestureEvent.OnDrag | `GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnDrag, OnDrag)` per-method 写法（与 IInteractionEvent.OnInteractionLockChanged listener 同模式） | ✅ ADR-027 + skill `event-system.md` §"int 事件" 模式 |
| InteractableObject 接收 IInteractionEvent.OnInteractionLockChanged | `GameEvent.AddEventListener<bool>(IInteractionEvent_Event.OnInteractionLockChanged, OnInteractionLockChanged)` | ✅ S2-08 已建立 |
| OnDisable 配对 RemoveEventListener | 两条 listener 各自配对 Remove | ✅ skill §"GameEventMgr" 替代方案：单 component 多 listener 手动 Add/Remove 合规 |

---

## 整体验证

- ✅ ADR-006 Evt_DragGesture / Evt_TapGesture 残留 = 0
- ✅ ObjectInteraction 子目录 Camera.main 实际调用 = 0
- ✅ ObjectInteraction 子目录 ADR-006 Evt_* 实际调用 = 0
- ✅ 协议路径全 ADR-027 接口事件 + per-method listener 模式

S2-09 X1 patch 后，Object Interaction 子系统协议合规度保持 **100%**。

---

## 关联文档

- [story-002-drag-mechanics.md](../epics/object-interaction/story-002-drag-mechanics.md) AC-6
- [grep-no-evt-objectinteraction-2026-04-29.md](./grep-no-evt-objectinteraction-2026-04-29.md) — S2-08 母 evidence（IInteractionEvent 全量迁移）
- [ADR-027](../../../../../docs/architecture/adr-027-game-event-interface-protocol.md) — GameEvent Interface Protocol
- [ADR-013](../../../../../docs/architecture/adr-013-object-interaction.md) — Object Interaction State Machine
