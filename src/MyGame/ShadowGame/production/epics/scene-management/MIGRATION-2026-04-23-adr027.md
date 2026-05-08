// 该文件由Cursor 自动生成

# Scene Management Epic — ADR-006 → ADR-027 迁移报告

**迁移日期**: 2026-04-23
**触发原因**: S2-05 `/story-readiness` 发现 `story-001-scene-state-machine.md` 仍引用 ADR-006（已 Superseded），与 Chapter State epic 已完成的 ADR-027 迁移不一致
**用户决策**: D1-B（一次性迁移整个 Scene epic）+ D2-B（`ISceneEvent` 2 方法实装 + 7 保留）+ E（直接重写 story 文件）

---

## 一、统一契约：`ISceneEvent` 9 方法

替代 ADR-006 的 `Evt_Scene*` int 常量（1400–1407）+ 9 个 `XxxPayload` struct 方案。方法参数即 payload，无 struct 传输。

| 方法签名 | 实装状态 | Sender | 主要 Listener |
|---------|---------|--------|--------------|
| `OnRequestSceneChange(int targetChapterId)` | **S2-05 实装** | Chapter State / Narrative / Pause Menu | Scene Manager（唯一）|
| `OnSceneReady(int chapterId)` | **S2-05 实装** | Scene Manager | UI / Input / Gameplay 全体 |
| `OnSceneTransitionBegin(int fromChapterId, int toChapterId)` | 签名冻结，S2-17 sender | Scene Manager (Step 3) | UI / Audio |
| `OnSceneUnloadBegin(int chapterId)` | 签名冻结，S2-17（对应旧 Story 003 早期落地）sender | Scene Manager (Step 5) | 所有 scene-scoped 系统 |
| `OnSceneDownloadProgress(float progress, long downloadedBytes, long totalBytes)` | 签名冻结，S2-?（对应旧 Story 002）sender | Scene Manager (Step 8) | UI |
| `OnSceneLoadProgress(string sceneName, float progress)` | 签名冻结，Story 002 sender | Scene Manager (Step 9) | UI |
| `OnSceneLoadComplete(int chapterId, string bgmAsset)` | 签名冻结，Story 002 sender | Scene Manager (Step 10) | Audio |
| `OnSceneTransitionEnd(int chapterId)` | 签名冻结，Story 005 sender | Scene Manager (Step 11 after fade-in) | 所有 |
| `OnSceneLoadFailed(int chapterId, string error)` | 签名冻结，Story 002 sender | Scene Manager (Error path) | UI（错误对话框）|

**决策要点（D2-B）**：9 个方法一次性在 S2-05 冻结签名，确保 Source Generator 一次生成 `ISceneEvent_Gen` + `ISceneEvent_Event`，后续 story 仅做 sender 实装 + listener 订阅，不再改契约本身。

## 二、改动文件清单

### 2.1 文档（production/epics/scene-management/）

| 文件 | 变更类型 | 说明 |
|------|---------|------|
| `EPIC.md` | **targeted patch** | Governing ADRs 列表 + Revision note + TR-scene-014 ADR-027 + Stories 表 ADR 列 |
| `story-001-scene-state-machine.md` | **完整重写** | ADR-006 → ADR-027；AC 扩展为 15 条（A: `ISceneEvent` 契约 / B: 状态机骨架 / C: `OnRequestSceneChange` 处理 / D: 排队与恢复 / E: 生命周期）；实现注附完整 `ISceneEvent.cs` + `SceneManager.cs` 源码；测试路径更正为 `Assets/Tests/EditMode/SceneManagement/SceneStateMachineTests.cs` |
| `story-002-additive-scene-loading.md` | targeted patch | 4 处 `Evt_*` → `ISceneEvent.On*`；实现注 `GameEvent.Send` → `GameEvent.Get<ISceneEvent>().On*`；测试路径更正 |
| `story-003-cleanup-sequence.md` | targeted patch | 4 处 `Evt_SceneUnloadBegin` → `ISceneEvent.OnSceneUnloadBegin`；Control Manifest Rules 更新；测试路径更正 |
| `story-004-transition-mutex.md` | targeted patch | 5 处 `Evt_RequestSceneChange` / `Evt_SceneReady` → `ISceneEvent.On*`；**story 范围收窄**（Story 001 已覆盖基础 max-1 队列，本 story 仅 `_inflightChapterId` 去重 + 边界测试）；测试路径更正 |
| `story-005-scene-events.md` | **完整重写** | 从"定义 8 个 Evt 常量 + 8 payload struct + 顺序"重写为"3 个 sender 补齐（TransitionBegin / TransitionEnd）+ 端到端顺序集成测试 + scene-scoped self-removal 文档模式"；**story 范围大幅收窄**；测试路径更正 |
| `story-006-luban-scene-mapping.md` | targeted patch | 2 处 `Evt_*` → `ISceneEvent.On*`；实现注代码块更新 |
| `MIGRATION-2026-04-23-adr027.md` | **新建** | 本报告 |

### 2.2 未改动但受影响的文件

| 文件 | 影响 | 处理 |
|------|------|------|
| `docs/architecture/adr-027-gameevent-interface-protocol.md` Line 429 附录 | 原文 `Evt_Scene{...} ×8` → `ISceneEvent.On{...} ×8` —— **漏算 `Evt_RequestSceneChange`**（前缀不是 `Evt_Scene`，属于第 9 个 Scene 域事件）| 待在下次 ADR propagate 或独立 PR 中补为 `Evt_RequestSceneChange + Evt_Scene{...} ×8 = 9 方法` |
| `production/sprint-status.yaml` | S2-05 entry 在本迁移完成后方可 `ready-for-dev` → `/dev-story`；Scene epic 中其他 story（S2-06 Story 004、S2-07 Story 006）后续 `/story-readiness` 会直接通过 | 由 `/dev-story` 完成时更新 |
| `production/epics/chapter-state/` 全部 story | 无影响（Chapter State epic 已完成 ADR-027 迁移，Track A 闭环 4/4 完成）| 无 |

### 2.3 尚未落地的源码改动（由后续 dev-story 创建）

- **新建** `Assets/GameScripts/HotFix/GameLogic/IEvent/ISceneEvent.cs`（S2-05 dev-story 产物；9 方法 + `[EventInterface(EEventGroup.GroupLogic)]`）
- **新建** `Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs`（S2-05 dev-story；骨架 + `OnRequestSceneChange` handler + `_pendingTargetChapterId` 队列）
- **新建** `Assets/Tests/EditMode/SceneManagement/SceneStateMachineTests.cs`（S2-05 dev-story；12–14 NUnit 覆盖 AC-1..AC-15；采用 TENGINE-SG-002 fixture 模式规避 `GameEventHelper.Init()` 问题）

---

## 三、Control Manifest Rules 增删

### 已移除（ADR-006 源）

- `All event IDs defined as public const int in centralized EventId static class`
- `Event ID allocation: Scene 1400-1499`
- `Every EventId constant must have XML doc comment with Sender(s), Listener(s), Payload type, and Cascade info`
- `Cross-event cascade depth must not exceed 3`（语义保留，改归 ADR-027 §2）
- `Use struct for frequent event payloads to avoid GC allocation`
- `Never define event IDs outside EventId.cs`
- `Never use anonymous object parameters or raw int encoding for payloads`（语义保留，改归 ADR-027）
- `Event dispatch per Send ≤ 0.05ms`（guardrail 保留，改归 ADR-027 §2 per-method ≤ 0.01ms）

### 新增（ADR-027 源）

- `All scene-domain cross-module events via ISceneEvent interface methods — 单一契约` (ADR-027 §1)
- `ISceneEvent 置于 GameLogic/IEvent/ 模块；[EventInterface(EEventGroup.GroupLogic)]` (ADR-027 §1)
- `ISceneEvent 接口方法参数即 payload —— 禁止 XxxPayload struct` (ADR-027 §1)
- `Scene transitions triggered exclusively via ISceneEvent.OnRequestSceneChange(int) interface method` — 外部系统不得直接调 Scene Manager 方法 (ADR-027 §1)
- `Scene-scoped listeners must self-remove inside ISceneEvent.OnSceneUnloadBegin handler via GameEvent.RemoveEventListener` (ADR-027 §3)
- `Never define Evt_Scene* int constants or SceneXxxPayload structs — ADR-006 协议在本 epic 整体作废` (ADR-027 §1)
- `Never call GameEvent.Get<ISceneEvent>().OnXxx(...) inside the same ISceneEvent method's own listener` — 防再入 (ADR-027 §2)
- `ISceneEvent per-method dispatch ≤ 0.01ms (p99)` (ADR-027 §2)

---

## 四、语义精确保留（non-breaking）

迁移只改"派发机制的代码形态"，以下行为契约**字节级等价**：

| 语义 | 旧形态 | 新形态 |
|------|--------|--------|
| 事件派发同步性 | `GameEvent.Send(int, struct)` 同步 | `GameEvent.Get<ISceneEvent>().OnX(...)` 同步 |
| 多 listener 广播 | AddListener + dispatcher 遍历 | SG 生成的 `Action<T1,T2,...>` delegate 链式调用 |
| Listener 生命周期 | `Add/RemoveListener` 配对 | `AddEventListener<T1,...>/RemoveEventListener<T1,...>` 配对，严格同型 |
| 再入保护 | handler 内不得 Send 同 EventId | handler 内不得 `Get<ISceneEvent>().Ox` 同方法 |
| Scene-scoped self-removal | 旧 `RemoveListener(EventId.Evt_SceneUnloadBegin, h)` | 新 `RemoveEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, h)` |
| 同章请求语义 | `Evt_RequestSceneChange(same chapter)` → `Evt_SceneReady` | `OnRequestSceneChange(same)` → `OnSceneReady` |
| max-1 队列 | `_pendingRequest: RequestSceneChangePayload?` | `_pendingTargetChapterId: int?` |
| 事件顺序（8 lifecycle） | 1400→1401→[1403]→1402→1404→1405→1406；失败 →1407 替换尾 | `Begin→UnloadBegin→[DownloadProgress*]→LoadProgress*→LoadComplete→Ready→TransitionEnd`；失败 →`LoadFailed` 替换尾 |

**下游系统影响**：任何未来订阅 Scene 事件的系统（Audio、UI、ChapterState、Gameplay）只能使用 `ISceneEvent_Event.OnX` 静态 ID 字段 + `GameEvent.AddEventListener<T1,...>` 泛型 API；**不再**存在 `EventId.Evt_Scene*` 常量或 `XxxPayload` 类型可引用。Chapter State epic 的实装中也未声明 `EventId.Evt_Scene*`，故无反向兼容负担。

---

## 五、遗留 / 待办

1. **ADR-027 附录修订**：Line 429 附录行当前只记 `Evt_Scene{...} ×8 → ISceneEvent.On{...} ×8`；应改为 `Evt_RequestSceneChange + Evt_Scene{...} ×8 → ISceneEvent.On{...} ×9`。建议路径：(a) 走一次 `/propagate-design-change` 流程触发 ADR 小修订 PR，或 (b) 在下一个 Scene story 接触 ADR-027 时顺手补。不影响本迁移完成度。

2. **`architecture-traceability.md` 附录同步**：同理，Scene epic 章节需记录 9 个 Scene 域事件的 TR-scene-014 映射（原记 8 个）。

3. **源码实装**（由 S2-05 `/dev-story` 负责）：
   - `ISceneEvent.cs`（GameLogic/IEvent/）—— 9 方法接口 + `[EventInterface]`
   - `SceneManager.cs`（GameLogic/Scene/）—— 骨架
   - `SceneStateMachineTests.cs`（Assets/Tests/EditMode/SceneManagement/）—— AC-1..AC-15

---

## 六、迁移质量自检

- [x] 所有 `Evt_Scene*` / `Evt_Request*` / `*Payload` 文本引用已转为明确的"历史参照"（Revision note / Forbidden 列表 / 代码注释），无"应改未改"残留 → 用 grep 验证通过
- [x] 所有 `ADR-006` 引用均标注 Superseded 或"由 ADR-027 取代"语境，无无上下文孤立引用 → grep 验证通过
- [x] 测试文件路径全部从旧式 `tests/integration/scene-management/*.cs` / `tests/unit/scene-management/*.cs` 更正为项目标准 `Assets/Tests/EditMode/SceneManagement/*.cs` → 6 个 story 文件内全部已更
- [x] 所有 story 的 `Performance` 声明存在（或明确标记 N/A） → Story 001 已补；其他 story 原文已有段落（由 Story 005 的 guardrail ≤ 0.01ms 兜底）
- [x] 9 方法契约的 AC-1 枚举与 `ISceneEvent.cs` 源码声明的方法列表字节级对应 → Story 001 内部自检
