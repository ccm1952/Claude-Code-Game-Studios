// 该文件由Cursor 自动生成

# 问题记录：Isolated Local Instance 经 Global Event Bus 触发 → Production Singleton Collateral Pollution（dp12 NEW）

> **日期**: 2026-05-13
> **发生工程**: MyGameStudio / src/MyGame/ShadowGame (Sprint 6 S6-04 dev-story Phase 3 R3 PlayMode 第 1 跑)
> **发生次数**: 第 1 次（S5-1b dual-layer 模式延续中首次踩此分支）
> **严重性**: 中-高（test isolation 假象 — 写 spike 时以为 `new SceneManager()` 就够 isolated 了，实际 collateral 在 R3 PlayMode 第 1 跑才暴露；错误信号 P4/P5 FAIL 完全指向"P4/P5 写错了"而非 P3，排查路径不显然）

---

## 问题现象

S6-04 spike (`S6-04_ErrorRestartPath.cs`) 用 **M1 dual-layer 模式** (per `problem_2026-05-09_spike-vs-production-singleton-conflict.md`)：

- P1/P2 用 production `GameApp._sceneManager`（reflection 拿）— 测 AC-8/-9/-10 production 路径
- **P3** 用 isolated local `new SceneManager()` — 测 chapter 99 bad sceneId retry exhaust 路径
- P4/P5 用 production sm — 测 AC-10 silent drop + RecoverToIdle + rapid newest-wins

**Phase 3 R3 PlayMode 第 1 跑**：

| Case | 期望 | 实测 | 状态 |
|------|------|------|------|
| P1 UnknownChapterTryResolveOrFail | production sm → Error + currentChapterId=1 | ✅ 符合 | PASS |
| P2 NewestWinsPendingDuringTransition | production sm chapter 2→1 final=1 | ✅ 符合 | PASS |
| **P3 AssetLoadFailRetryExhaust** | local sm → Error；production sm 不动 | local sm → Error ✅；**production sm 也进 Error ❌** | **PASS（自身）但污染** |
| **P4 RestartFromErrorRecovery** | precondition `state=Idle, currentChapterId=1` | **precondition `state=Error, currentChapterId=1` 直接 FAIL** | **FAIL** |
| **P5 RapidNewestWinsOverwrite** | precondition `state=Idle` | 同上 | **FAIL** |

**错误信号弱**：P4/P5 报 "precondition fail" 指向 P4/P5 自己（state=Error 不对），完全没指向 P3。第一反应会去查 P4/P5 的 setup / production sm wiring 是否对 — 而真正的肇事者是 P3 case 末尾留下的 collateral state。

---

## 根因

**核心**：`GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99)` **不是 instance-bound 方法调用**，而是**全局 broadcast**。任何订阅了 `ISceneEvent_Event.OnRequestSceneChange` 的 listener 都会被 invoke。

P3 setup 链路：

```csharp
// P3 RunP3Async
var local = new SceneManager();
local.Init();
local.RegisterChapterDataProvider(BuildLocalFixtureWithChapter99());
local.RegisterFadeOverlay(new NoOpFadeOverlay());
// local sm 内部 ctor: GameEvent.AddEventListener<int>(ISceneEvent_Event.OnRequestSceneChange, OnRequest);
//   ↑ 它订阅了 GLOBAL EVENT BUS

// Production sm 也早已订阅了 OnRequestSceneChange（GameApp 启动时 wire 的）

// 现在 P3 fire:
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99);
// ↑ Broadcast:
//   1. local sm.OnRequest(99) → BeginTransitionAsync(99) → 用 LocalFixture 的 chapter 99 bad sceneId → retry × 2 → Error ✅ (P3 期望)
//   2. PRODUCTION sm.OnRequest(99) → TryResolveOrFail(99) → ProductionProvider(99) == null → Error ❌ (collateral)
```

两个 sm **同时**响应同一个 broadcast，local 走 retry exhaust，production 走 unknown chapter — **两条独立的 Error path**。但 P3 末尾只 cleanup local：

```csharp
finally { local.Dispose(); local = null; }
// production sm 还在 Error 状态！P4/P5 precondition FAIL
```

**S5-1b dual-layer 为什么没踩到**：S5-1b P4/P5 是 fail-loud `chapter 99 unknown` —— **local sm 注册了 ProviderReturningNull / Skip register** + production sm 注册的 production fixture 也认得 chapter 99（当时无 chapter 99 fixture）→ production sm 同样进 Error。但 S5-1b P4/P5 是 dual-layer 的最后 case，**跑完就 spike 结束**，没有后续 case 读 production state，所以 collateral 没暴露。S6-04 P3 在中间 + P4/P5 在后面 → 第一次暴露此模式。

**关键认知翻新**：

| 假设（错） | 现实 |
|-----------|------|
| `new SceneManager()` 是隔离的，fire event 只影响这个实例 | event 是 global broadcast，所有 listener instance 都收 |
| dual-layer 的 isolation 是"实例 isolation" | dual-layer 实际只 isolate 了**状态 + 资源持有**，没 isolate **event listener fan-out** |
| Cleanup `local.Dispose()` 就够了 | Cleanup 必须考虑**所有可能收到 broadcast 的 instance**，含 production |

---

## 修复（Phase 3 spike amend，第 2 跑 5/5 PASS）

`RunP3Async(SceneManager productionSm)` 加 production sm collateral cleanup：

```csharp
private async UniTask RunP3Async(SceneManager productionSm)  // ← 新增参数
{
    var local = new SceneManager();
    local.Init();
    local.RegisterChapterDataProvider(BuildLocalFixtureWithChapter99());
    local.RegisterFadeOverlay(new NoOpFadeOverlay());

    GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99);
    await UniTask.Delay(TimeSpan.FromMilliseconds(500));

    // local sm 验证（原有）
    P3Passed = local.CurrentState == SceneManagerState.Error
            && deltaLF >= 1
            && lastLF.chapterId == 99
            && !string.IsNullOrEmpty(lastLF.error);

    // ← Collateral cleanup（NEW per dp12 finding）
    try { local.Dispose(); } catch { /* ignore */ }
    if (productionSm != null && productionSm.CurrentState == SceneManagerState.Error)
    {
        productionSm.RecoverToIdle();
        _p3Events.Add($"Cleanup: production sm RecoverToIdle() → state={productionSm.CurrentState}");
    }

    // ← Reassert production 状态干净（NEW per dp12 finding）
    _asserts["P3.cleanup_production_idle"] = productionSm.CurrentState == SceneManagerState.Idle
        ? "PASS: production sm RecoverToIdle 后 state=Idle"
        : $"FAIL: productionSm.CurrentState={productionSm.CurrentState}";

    P3Passed = P3Passed && productionSm.CurrentState == SceneManagerState.Idle;  // ← 综合判定
}
```

**关键约束**：

1. **每个 isolated-local case 必须接收 productionSm 引用** — 不能仅持 local 引用就 cleanup
2. **Broadcast event 后 cleanup 必须 check `productionSm.CurrentState`** — 如果 production 也被牵连进 Error，必须 `RecoverToIdle()` 复位
3. **Cleanup 自己也要 assert** — P3 加 `P3.cleanup_production_idle` 显式声明 cleanup 验证（让未来回归测试能 catch 同样的 collateral 回潮）
4. **P3Passed 综合判定** — 不是仅"local 进 Error 就 PASS"，而是"local 进 Error + production 不被污染（或污染后已成功 RecoverToIdle）"

---

## Agent 自检 checklist（仅本工程，写 spike 时）

- [ ] 本 spike 是否用 **isolated local instance**（`new SceneManager()` / `new XxxManager()`）？
- [ ] 该 instance 的 ctor 是否 **订阅了 GameEvent 全局事件**？grep `GameEvent.AddEventListener` in instance ctor / Init / Awake
- [ ] 该 instance 通过 `GameEvent.Get<IXxxEvent>().XxxMethod(...)` 触发的 action，是否会被 **production 端的同类 instance 同步收到**？
- [ ] 如是，case cleanup 是否同时复位 production instance？（grep production singleton accessor + check `state == Error/Active/...` + 复位 method 如 `RecoverToIdle()` / `Reset()` / `Stop()` / `Clear()`）
- [ ] Cleanup 后是否 **显式 assert production instance 干净**（如 `production_idle: state==Idle`）？— audit trail，未来回归测试能 catch 同样的 collateral 回潮
- [ ] case 综合判定（如 `P3Passed`）是否包含 production cleanup verify？— 不能仅看 local 自身行为
- [ ] case 跑完后 spike 后续 case 是否假设 `production.state == Idle`？如假设了，**必前置 cleanup verify**

---

## 替代方案：什么时候 isolated-local broadcast 不可避免

| 场景 | 替代方案 |
|------|----------|
| spike 必须用 `GameEvent.Get<IXxxEvent>().XxxMethod(...)` 触发（API 设计就是 broadcast） | dual-layer + cleanup verify（本 memo 模式） |
| event 不是 broadcast，而是 method call on instance（`local.Method(...)`） | 直接 method call，避免走 global event bus —— **优选** |
| local instance 可临时 unsubscribe production listener | `production.UnsubscribeAll()` → spike test → `production.ReSubscribe()` — 风险高，pre-existing state 难还原 |
| Sprint 7+ 可能引入：**ISceneEvent.Get<ISceneEvent>(SceneManager target)** scoped event-bus | ADR-009 V3.x 评估时考虑 scoped event 设计（dp12 promote 候选） |

---

## 与其他文档的关系

- **延伸自** `problem_2026-05-09_spike-vs-production-singleton-conflict.md` (M1 dual-layer pattern)：dual-layer 解决了**状态 + 资源 isolation**，但 dp12 揭示**event fan-out isolation** 是它没覆盖的另一维度，本 memo 是 dual-layer pattern 的补丁说明
- 与 `ADR-009 V3.x watch list` 候选：S6-04 evidence doc + sprint-status.yaml dp12 已记录；ADR-009 spec 未来评估是否引入 **scoped event-bus** 或 **instance-bound event API**（如 `sceneManager.LocalFire(IXxxEvent)` 仅自身 listener 响应）
- 与 `S6-04 evidence doc` (`production/qa/playmode-error-restart-path-2026-05-13.md` §4 NEW dp12 candidate first-strike closure)：两处一致 surface
- **不沉淀跨工程 cursor rule**（per user 决策 [C] memo_only）：理由 — Unity GameEvent / TEngine 是本工程特有的 event-bus 实现；跨语言 / 跨框架的 pub/sub 模式虽通用但每个框架的 isolation 语义差异大，强行抽象成跨工程 rule ROI 低；本工程内 memo + Sprint 6 retro 讨论 + ADR-009 V3.x watch list 三层覆盖已足

---

## 历史记录

- **2026-05-13 创建**（Session 30 continue Phase 3 第 1 跑）。触发场景：S6-04 R3 PlayMode 第 1 跑 P4/P5 precondition FAIL → 排查发现 P3 isolated local sm fire `OnRequestSceneChange(99)` 是 global broadcast → production sm 收到 → 走 TryResolveOrFail FAIL → 进 Error → 污染 P4/P5 precondition；spike amend `RunP3Async(productionSm)` 加 collateral RecoverToIdle cleanup → 第 2 跑 5/5 PASS。按 `problem-to-rule-promotion.mdc` 协议判定为"易踩坑 + 错误信号弱（P4/P5 报错指向 P4/P5 自己而非 P3）+ 修复路径不显然 + Unity GameEvent / TEngine 本工程特有 event-bus"，用户选 [C] 仅写本工程 lessons memo，不沉淀跨工程 cursor rule（与 problem_2026-05-09_spike-vs-production-singleton-conflict.md 相同决策路径）。
- **预期 follow-up**：
  - Sprint 6 retro 议题 — 评估 ADR-029 V3.x 是否加 "Type-? event-bus fan-out isolation check" R2 grep 子条款（写 spike 前对 instance ctor 内 `GameEvent.AddEventListener` 列表 + 对应 production singleton 是否同样订阅做 fan-out 检查）
  - ADR-009 V3.x watch list 评估 scoped event-bus / instance-bound event API 引入（与 dp12 candidate 联动）
  - S6-04 spike 文件 (`Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-04_ErrorRestartPath.cs`) 作为 **dual-layer + event fan-out cleanup** 实施模板，未来涉及 GameEvent broadcast 的 spike 引用
