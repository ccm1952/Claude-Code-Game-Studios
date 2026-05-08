// 该文件由Cursor 自动生成

# Problem Memo — Story §Implementation Notes 与 Framework 实际能力 drift 的反复模式

> **Status: Superseded by [ADR-029 Story §Implementation Notes 验证流程](../../docs/architecture/adr-029-story-impl-notes-verification.md)** — 2026-04-30
>
> 本 memo 作为 ADR-029 的 lessons / history 记录保留；R1/R2/R3 解决方案已升级为 process-level ADR-blessed gate（详 ADR-029 §1/§2/§3）。后续 drift 防御走 ADR-029 的 `/create-stories` + `/story-readiness` + `/dev-story` 三 skill 集成路径，不再追加本 memo。

**Date**: 2026-04-29
**Severity**: HIGH（架构治理 / 工作流问题）
**Recurrence**: 已 4 次反复（S2-09 push/pull 转发分歧 + S2-12 整接口订阅 + S2-13 整接口订阅+不存在 API+Luban 缺口 + S2-11 整接口订阅+缺 RotationStep）→ 触发 ADR-029 起草

---

## 现象

Story 文件的 `## Implementation Notes` 章节给出了一段"参考实施代码"，但**实际照抄不能编译 / 不能运行**，因 framework 不支持其使用的 API 模式。已观察到的具体 drift：

### S2-12 InteractionLockManager（本次发现）

Story-006 §Implementation Notes 写：
```csharp
public sealed class InteractionLockManager : IInteractionEvent, ISceneEvent
{
    public void Init() {
        GameEvent.AddEventListener<IInteractionEvent>(this);   // ← 不存在的签名
        GameEvent.AddEventListener<ISceneEvent>(this);
    }
}
```

实际 TEngine `GameEvent.cs` 仅暴露：
```csharp
public static bool AddEventListener<TArg1>(int eventType, Action<TArg1> handler) { ... }
public static bool AddEventListener<TArg1, TArg2>(int eventType, Action<TArg1, TArg2> handler) { ... }
// ...
```

且 `EventMgr.RegWrapInterface<T>(T callerWrap)` 是 **sender 端**代理注册（让 source generator 生成的 `_Gen` 类拿到 dispatcher），**不**是 listener 端 API。

### S2-09（已发生）

Story-002 与 Story-007 对 `OnDrag` 事件的 push / pull 责任分配有矛盾，dev-story 阶段才发现需要协商（详见 S2-09 完成笔记）。

---

## 根因

1. **ADR / Story 设计阶段对 framework 实际能力调研不足** —— 基于"理想接口"而非"实测 API"写示意代码
2. **Sender / Listener 双端职责混淆** —— `EventMgr.RegWrapInterface<T>` 与 `GameEvent.AddEventListener<TArg>(int, Action<TArg>)` 在心智模型上易混（前者注册"接口代理"做派发；后者注册"具体回调"做接收）
3. **Story 模板长期不维护** —— `Implementation Notes` 应当是"骨架建议 + 强约束"而非"可直接照抄的成品代码"，否则与 framework 演进脱节
4. **缺乏 dev-story 入口的 readiness 强校验** —— `/story-readiness` 仅校验 story 元数据完整性，未实测 §Implementation Notes 中的 API 签名是否存在

---

## 影响

- 实施时被迫 surface drift，打断节奏（S2-09 中等损失，S2-12 中等损失）
- 反复回到"修改 story Implementation Notes 同步刷新"的 patch 循环（X1 patch v2 模式）
- 若团队成员盲目照抄 §Implementation Notes 而不验证，将引入编译错误 → 浪费 build-fix-rebuild 时间
- ADR 内容公信力下降（"看 ADR 不一定能跑"）

---

## 解决方案

### R1：Dev-story 实施前必须验证 §Implementation Notes API 签名

进入实装前，对 §Implementation Notes 中的所有 framework API 调用做一次 **5 分钟 grep 验证**：
1. 所有 `GameEvent.Xxx<T>(...)` 调用 → grep `public static .* Xxx<` 在 TEngine 实际源码中
2. 所有 `_Gen` / `_Event` 引用 → 验证 source generator 已生成相应 partial class
3. 所有继承 / 接口实施 → 验证签名匹配

如发现 drift → 立即 surface 给 user，提供 3 个选项：
- A. 按 framework 实际能力重写 §Implementation Notes（推荐）
- B. 立 ADR / finding 反推 framework 增强
- C. 在 framework 层加 API 适配层

### R2：Story 模板加 §Engine Notes 的"API verifies-as" 字段

在 story 模板新增可选 callout：
```markdown
**API Verified Against**: TEngine git@<commit-sha> at <date>（YES / NO — pending dev-story 验证）
```

填 NO 的 story 在 `/story-readiness` 阶段应被 flag 为 NEEDS WORK。

### R3：ADR-027 §"Listener 端订阅模式" 章节补充

明确：**Listener 端唯一支持模式**是 per-event 注册：
```csharp
GameEvent.AddEventListener<TArg1, TArg2, ...>(IXxxEvent_Event.OnYyy, handler);
```

**禁止用法**：
- `GameEvent.AddEventListener<IXxxEvent>(this)` —— 不存在
- `class MyClass : IXxxEvent { public void Init() => GameEvent.AddEventListener<IXxxEvent>(this); }` —— 不存在
- 期望 `EventMgr.RegWrapInterface<T>` 替代 listener 注册 —— 这是 sender 端代理注册，不是 listener API

---

## Cross-project 适用性

本 drift 不仅限于 TEngine —— 任何"配套有 source generator + facade"的事件系统（Unity Messaging / UE Delegate / Godot Signal）都存在类似风险：
- Source generator 生成 sender 端代理可能让人误以为 listener 端也有对应 helper
- "整接口订阅"是常见的设计偏好但很多 framework 不支持

**Cross-project rule**：在做 framework 选型 / ADR 起草时，必须**测一遍**关键事件流的 listener 注册路径（new + 5 行测试代码即可）。

---

## 沉淀产物

- 本 memo（沉淀完成）
- `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/conventions.md` — 加 "## Listener 端订阅模式" 章节
- `production/epics/object-interaction/story-006-interaction-lock-manager.md` X1 patch v2 — §Engine Notes + §Implementation Notes 同步刷新
- 后续考虑：起 ADR-029 "Story §Implementation Notes 验证流程" 形式化 R1 + R2

---

## Agent Self-Checklist

每次 `/dev-story` 起手必检：
- [ ] §Implementation Notes 的 framework API 是否在 framework 源码中实测存在？
- [ ] Listener 端注册是否走 per-event 模式（`int eventType, Action<TArg> handler`）？
- [ ] 是否避免了 `class : IXxxEvent + AddEventListener<TInterface>(this)` 整接口订阅幻想？
- [ ] 如发现 drift → surface 而非 silent 偏离？

---

## History

- 2026-04-29 night：S2-12 InteractionLockManager 发现 §Implementation Notes 用了 `GameEvent.AddEventListener<TInterface>(this)` 不存在 API → surface drift → 选 [A] 修订 + [P1] 立即沉淀 → 本 memo + conventions.md 章节 + story X1 patch v2
- 2026-04-29 day：S2-09 InteractableObject Drag 实施时发现 push/pull 转发不一致问题 → 已在 active.md X3 patch 中记录（未升级到 cross-project rule）
