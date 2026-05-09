// 该文件由Cursor 自动生成

# 问题记录：ADR-009 listener-path driver 缺失（OnRequestSceneChange 没串到 BeginTransitionAsync）

> **日期**: 2026-05-09
> **发生工程**: MyGameStudio / src/MyGame/ShadowGame (Sprint 5 S5-1b dev-story)
> **发生次数**: 1 次（首次正面在 PlayMode 实测撞到 — Sprint 3 S3-03 期间是 spike 直调 `BeginTransitionAsync`，没走 listener 路径所以没暴露）
> **严重性**: 中（dev 期不阻塞 — 用 dev-only F4 driver 临时模拟 ✅ ；production 上线前必须补 driver，否则真实 boot pipeline 跑不动 ⛔）

---

## 问题现象

S5-1b 在 `GameApp.Entrance` 内完成 `_sceneManager` 接入后，按 R3 PlayMode probe 设计验 P1 first-boot：

1. `DevTestState.OnEnter` 派发 `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)` 触发 production 路径
2. spike 监听 production `_sceneManager` 的 8 lifecycle event，等 `OnSceneTransitionEnd(1)` 收到
3. **预期**：production 端有某个组件 listen `OnRequestSceneChange` 并调 `_sceneManager.BeginTransitionAsync(1)` → 11-step transition fire → spike 收到 8 event
4. **实测**：spike 等 30s 一个 event 都没收到，`_sceneManager.CurrentState` 一直 `TransitionOut`（仅状态切了，11-step 没启动）

**Grep 验证根因**（只是 production 端 listener handler 自己搞错，不是 spike bug）:

```
Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs:
  HandleRequestSceneChange(int targetChapterId):
    _currentChapterId = targetChapterId;
    _state = SceneTransitionState.TransitionOut;  // 仅此一行
    // ❌ NO BeginTransitionAsync(targetChapterId) call
```

production 端**没有任何代码**在 `OnRequestSceneChange` 收到后驱动 `BeginTransitionAsync` —— 11-step transition 只能由"业务代码主动调 `BeginTransitionAsync`"驱动，但 ADR-009 §11-step + §6-state machine 的 spec 文字暗示 listener-path 应当是闭环的。

## 根因

ADR-009 (Accepted, 2026-04-30 Sprint 3 S3-03 期间起草) §6-state machine 表述:

> `Idle → TransitionOut → Loading → Ready → TransitionIn → Idle`，由 `OnRequestSceneChange` 触发并驱动 11-step transition

但 `SceneManager.cs` 的实际实现里 **`OnRequestSceneChange` listener handler 只写了 state 切换 + `_currentChapterId` 设置**，**没**接续调 `BeginTransitionAsync`，导致 11-step transition 只能由"调用方主动调 `BeginTransitionAsync`"驱动 —— 这个 driver 入口现在**只在 dev spike** 里存在（`S301_AdditiveSceneLoading` / `S302_CleanupSequence` / `S303_SceneEventOrdering` 都是直调 `BeginTransitionAsync`），**production runtime 没有任何位置串这一步**。

S3-01/S3-02/S3-03 三个 spike CORE PASSED 没暴露这个问题是因为它们都是 spike 直调，**绕过了 listener 路径**。S5-1b 是首个走 production listener-path 的真实接入，所以才在这里炸出来。

## 临时方案 (S5-1b 已实施)

`DevTestState.cs`（**dev-only FSM state**）内加 fire-and-forget `UniTaskVoid` driver，等 800ms（保 spike listener 先订阅）后反射拿 `GameApp._sceneManager` 调 `BeginTransitionAsync(1)`：

```csharp
private static async UniTaskVoid DriveProductionSceneTransitionAsync(int targetChapterId, int delayMs)
{
    await UniTask.Delay(TimeSpan.FromMilliseconds(delayMs));
    var fi = typeof(GameApp).GetField("_sceneManager", BindingFlags.NonPublic | BindingFlags.Static);
    var prodScene = fi.GetValue(null) as SceneManager;
    if (prodScene == null) return;
    try
    {
        await prodScene.BeginTransitionAsync(targetChapterId);
    }
    catch (Exception e)
    {
        Log.Error($"[GameFlow] [S5-1b][F4] driver 异常：{e}");
    }
}
```

**严格 dev-only**：仅在 `DevTestState`（dev FSM state）调用，production runtime 路径不走。

## Production 修复路径（待 follow-up story）

候选方案（建议起 follow-up story 决策 + 落地）:

### 方案 A：SceneManager 内自闭环（最直接）

```csharp
// SceneManager.HandleRequestSceneChange
private async void HandleRequestSceneChange(int targetChapterId)
{
    _currentChapterId = targetChapterId;
    _state = SceneTransitionState.TransitionOut;
    try { await BeginTransitionAsync(targetChapterId); }
    catch (Exception e) { Log.Error(...); }
}
```

**优点**：listener-path 闭环；调用方调一次 `OnRequestSceneChange` 即可。
**缺点**：`async void` listener 不能 `await`，异常不易传递；handler 重入语义需想清楚（一帧内多次派发会怎样）。

### 方案 B：GameFlow 层加 SceneTransitionState (FSM)

`GameFlow` 模块新增 FSM state `SceneTransitionState`，监听 `OnRequestSceneChange` → `OnEnter` 内调 `_sceneManager.BeginTransitionAsync` → 等 `OnSceneTransitionEnd` → `ChangeState(NormalGameState)`。

**优点**：业务驱动逻辑集中在 GameFlow（与 ADR-013 §FSM 协议一致）；状态可审计。
**缺点**：多一层 indirection；FSM driver 与 SceneManager driver 的 timing 协调要想清楚。

### 方案 C：GameApp 静态 listener（轻量）

`GameApp` 注册一个静态 listener `HandleRequestSceneChange(int)` → 内部 fire-and-forget 调 `_sceneManager.BeginTransitionAsync`。

**优点**：实施成本最低（一个 listener handler + 一个 UniTaskVoid driver）。
**缺点**：与 GameFlow FSM 解耦得过远；fire-and-forget 异常路径只能走 logger。

## Agent 自检 checklist（仅本工程）

设计/实施"listener 触发 → 异步 driver"链路时:

- [ ] listener handler 内是否真的调了 driver？还是只 set 了 state 字段？grep `BeginTransitionAsync` / `LoadChapterSceneAsync` 等 driver method **在生产代码里** 至少一次 hit
- [ ] 如果 driver 是 `async`，listener handler 是 `void` 还是 `async void`？`async void` 异常逃逸到 Unity log 就没了，需明确是否能接受
- [ ] PlayMode probe 设计时**至少有一条 case 走 listener-path 完整闭环**（不能全部 spike 直调 driver method 绕开 listener）
- [ ] 每条 ADR 描述的 state machine 表（"X → Y by Z"），grep 验证 production code 内 Z 触发后有实际 transition driver 代码
- [ ] dev-only F4 / 反射 stub 写时必加 inline TODO marker + sprint-status / story closure note 显式 surface follow-up

## 与其他文档的关系

- 与 `ADR-009 (Accepted)` Scene Lifecycle: 本问题暴露了 ADR-009 spec 与实际 SceneManager.cs 实现的 gap；建议 follow-up story 同时修订 ADR-009 §6 state machine 表 + §11-step driver 入口表述清楚 driver 责任方
- 与 `~/.cursor/rules/string-literal-nested-quotes.mdc` 等跨工程 rule 不冲突（本问题是项目独有架构 gap，不沉淀跨工程）
- 与 `S5-1b evidence doc` (`production/qa/playmode-bootscene-load-2026-05-09.md` §10): 三处一致 surface

## 历史记录

- **2026-05-09 创建**（Session 25 #2）。触发场景：S5-1b dev-story `/dev-story` 实施期间 P1 PlayMode 实跑暴露 production listener-path driver 缺失；用户选 [A] (F4) 临时 dev-only driver in `DevTestState` + evidence doc + active.md + story closure note + 本 memo 四处 surface deficiency；按 `problem-to-rule-promotion.mdc` 协议判定为"项目独有 ADR-009 设计 gap"，仅写本工程 lessons memo，不沉淀跨工程 cursor rule。
- **预期 follow-up**：Sprint 5 retro 或 Sprint 6 起新 story（candidate name: "ADR-009 production listener-path driver 补完 + ADR 文字修订"）走完整 dev-story 流程；driver 落地后立即移除 `DevTestState.DriveProductionSceneTransitionAsync` 这个临时 stub。
