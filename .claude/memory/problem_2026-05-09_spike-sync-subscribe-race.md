// 该文件由Cursor 自动生成

# 问题记录：Spike listener subscribe 时机 vs sync-fired event race（同步事件首次派发被漏接）

> **日期**: 2026-05-09
> **发生工程**: MyGameStudio / src/MyGame/ShadowGame (Sprint 5 S5-1c dev-story)
> **发生次数**: 第 1 次正面撞（Sprint 3 S3-01..03 / Sprint 5 S5-1b 三组 R3 spike 都没暴露 — 因为它们都是 spike **直调** `BeginTransitionAsync` 异步驱动 + 等若干帧后 listener subscribe 才触 event；S5-1c 是首个走 production listener-path 自驱 + handler 内同步 fire `OnSceneTransitionBegin` 的 R3 spike）
> **严重性**: 中（spike 设计层面 — 错了会导致 P1 漏 capture 第 1 个事件 → 整 case FAIL → 误以为是 production code bug；正面解了之后 R3 设计可以更大胆复用 listener-path）

---

## 问题现象

S5-1c story 把 ADR-009 production listener-path driver 接入 `SceneManager.OnRequestSceneChange` handler 内，handler 改后路径如下：

```
DevTestState.OnEnter
  ↓ ISceneEvent.OnRequestSceneChange(1) 派发                  ← T0
  ↓
SceneManager.OnRequestSceneChange(1)（listener handler）
  ↓ TransitionTo(SceneManagerState.TransitionOut)              ← T1（同步）
  ↓ DriveTransitionAsync(1).Forget();                          ← T2（fire-and-forget）
  ↓
DriveTransitionAsync.async UniTaskVoid
  ↓ await BeginTransitionAsync(1)
  ↓
BeginTransitionAsync.line 657 (TransitionTo OUT 已发) → line 659 ISceneEvent.OnSceneTransitionBegin(-1, 1) 派发 ← T3（仍同帧，仍同步）
```

**关键**：T3 处 `OnSceneTransitionBegin` 是**同步**派发的（spike 直调 `OnRequestSceneChange(1)` 后到 `OnSceneTransitionBegin` 之间没有 `await Yield` / `await Delay` / 跨帧 boundary），而 spike 端的 listener subscribe 时机如果晚于 T3，就会**漏接** `OnSceneTransitionBegin` 这个第 1 个生命周期事件。

S5-1c spike 第 1 版按 S5-1b 模板写：

```csharp
// S5-1b 时这样写没问题（因为 S5-1b 是 spike 直调 BeginTransitionAsync 异步路径）
public class S51cRuntime : MonoBehaviour
{
    void Start()  // ← 错位置：Start 在 AddComponent 同帧 end-of-frame 才跑
    {
        GameEvent.AddEventListener<int, int>(SceneEvent.OnSceneTransitionBegin, OnTransitionBegin);
        // ... subscribe rest
    }
}

// DevTestState.OnEnter
DevBootstrap.RunRequested();          // 触发 spike：AddComponent<S51cRuntime>() — Awake() 同步执行；Start() 推迟到 end-of-frame
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);  // 同步 fire OnSceneTransitionBegin（在 spike Start() 跑之前）
// ↑ spike 漏接 OnSceneTransitionBegin → P1 events.Count < 6 → asserts FAIL
```

**实测**：P1 case 跑出来 events 数组里**没有** `OnSceneTransitionBegin(-1, 1)`，第一项是 `OnSceneLoadProgress(...)`（已经在 BeginTransitionAsync 中段了）。误以为 production code 漏 send `OnSceneTransitionBegin` event，实际是 spike 漏 subscribe。

---

## 根因

Unity MonoBehaviour 执行顺序：

| 时机 | 触发条件 |
|------|----------|
| `Awake()` | `AddComponent<T>()` 调用**同步**返回前执行（同帧、同栈） |
| `OnEnable()` | `Awake()` 之后立即同步执行（如 `enabled=true`） |
| `Start()` | 该 MonoBehaviour 第 1 次 `Update` 之前的 frame boundary（**至少推迟到 end-of-frame**） |

S5-1b 时 spike subscribe 写在 `Start()` 没问题 —— 因为 S5-1b 是 spike 直调 `await BeginTransitionAsync(1)` (异步路径，spike 内部已先 `await UniTask.NextFrame()` 让 spike Start 跑完才驱动)，第 1 个事件 `OnSceneTransitionBegin` 至少跨了 1 帧。

但 S5-1c 是真实 listener-path：

```
DevTestState.OnEnter
  ↓ DevBootstrap.RunRequested()         → AddComponent<S51cRuntime>() → Awake() 同步、Start() 推迟
  ↓ OnRequestSceneChange(1)             → SceneManager.OnRequestSceneChange handler 同步
                                        → DriveTransitionAsync(1).Forget()  
                                        → await BeginTransitionAsync(1)（仍同帧 第一次 await 之前）
                                        → ISceneEvent.OnSceneTransitionBegin(-1, 1) 同步 fire
  ↓                                     ← 此时 spike Start() 还没跑！
  // spike 漏接 OnSceneTransitionBegin
end-of-frame
  ↓ S51cRuntime.Start() 跑              ← 太晚了，第 1 个事件已经过了
```

S3-01..03 / S5-1b 的 R3 spike 没有撞这个 race 是因为它们的 R3 case **第 1 个事件不是同步 fire**：

| Spike | 第 1 事件路径 | 同步 vs 异步 |
|-------|--------------|------------|
| S301_AdditiveSceneLoading | spike 直调 `await GameModule.Scene.LoadAsync(...)` → 内部跨多帧 yield → spike Start 早跑完 | 异步 ✅ |
| S302_CleanupSequence | 同上 | 异步 ✅ |
| S303_SceneEventOrdering | 同上 + spike 内部 `await UniTask.Yield()` 抢占 | 异步 ✅ |
| S5-1b | spike P1 用 reflection 拿 production `_sceneManager` + `await BeginTransitionAsync(1)` 直调 → 同上 | 异步 ✅ |
| **S5-1c** | DevTestState.OnEnter `OnRequestSceneChange(1)` 派发 → handler 同步 → `OnSceneTransitionBegin` 同步 fire | **同步 ❌** |

S5-1c 是首个 R3 spike 走"listener handler 同步链路"（`async UniTaskVoid` driver 内首个 `await` 之前都是同帧同步）的；之前所有 spike 都是"先 yield 再 drive"。

## 规则（解法 — 按推荐度从高到低）

### 解法 1（推荐）：spike subscribe 提前到 `Awake()`

`AddComponent<T>()` 调用是同步的，`Awake()` 在 `AddComponent` 返回前就已经跑完。把 listener subscribe 写在 `Awake()` 而非 `Start()`，可以**保证 subscribe 早于任何后续派发**：

```csharp
public class S51cRuntime : MonoBehaviour
{
    void Awake()  // ← 正确位置 — AddComponent 同步返回前已 subscribe
    {
        GameEvent.AddEventListener<int, int>(SceneEvent.OnSceneTransitionBegin, OnTransitionBegin);
        GameEvent.AddEventListener<string, float>(SceneEvent.OnSceneLoadProgress, OnLoadProgress);
        // ... subscribe rest
    }

    void OnDestroy()  // ← 配对清空
    {
        GameEvent.RemoveEventListener<int, int>(SceneEvent.OnSceneTransitionBegin, OnTransitionBegin);
        // ...
    }
}
```

**调用 timeline 验证**：

```
DevTestState.OnEnter
  ↓ DevBootstrap.RunRequested()
    ↓ AddComponent<S51cRuntime>()
       ↓ S51cRuntime.Awake() ← subscribe 完成 ✅
       ↓ AddComponent 返回
    ↓ RunRequested() 返回
  ↓ OnRequestSceneChange(1) → SceneManager handler → DriveTransitionAsync → BeginTransitionAsync → OnSceneTransitionBegin fire ← spike 接到 ✅
```

### 解法 2（辅助）：DevTestState 调用顺序保证 spike 先 ready

跟解法 1 配套 — 即便 spike 用 `Awake()`，driver 端调用顺序也要先 spawn spike 再触 trigger。错例：

```csharp
// ❌ 反例 — driver 先派发，spawn spike 后才有机会 subscribe
public override void OnEnter(IFsm<...> fsm)
{
    GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
    DevBootstrap.RunRequested();  // ← 太晚
}
```

```csharp
// ✅ 正例 — spike 先 spawn (Awake() subscribe)，再触 trigger
public override void OnEnter(IFsm<...> fsm)
{
    DevBootstrap.RunRequested();  // ← 先：spawn S51cRuntime → Awake() subscribe
    GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);  // ← 再：派发 trigger
}
```

### 解法 3（差解 — 不推荐）：在派发 trigger 前 `await UniTask.NextFrame()`

```csharp
// ❌ 不推荐 — 把 race 隐藏起来，没解决根因
await UniTask.NextFrame();  // 跨一帧让 spike Start() 跑
GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
```

**为什么不推荐**：
1. 把"spike Start 时机"这个 invariant 从代码里抹掉，改了 spike 之后没 grep 痕迹
2. 跨帧 + `await` 会改变 PlayMode 实测的 timing fingerprint（如 `OnSceneTransitionBegin` 跨多少帧）
3. 后续 R3 case 复用同一 listener handler 时还是会撞同样问题

### 解法 4（适用面更窄）：spike 内 first-event polling fallback

如果 listener-path 第 1 个事件实在没法保证 spike 已 subscribe（比如 `Awake()` 内 subscribe 也有特殊跨域 boundary），spike 可加一个 polling fallback：

```csharp
// spike Tester 内 — 如果第一个 event 漏接，从 SceneManager 状态字段反推
if (!receivedTransitionBegin && _sceneManager.CurrentState != Idle)
{
    // 推断已经过了 OnSceneTransitionBegin → polling 后续 events 而非依赖第一个
    Debug.LogWarning("OnSceneTransitionBegin 漏 capture，从 state field 推断 transition 已启动");
    receivedTransitionBegin = true;  // 继续测后续 invariant
}
```

仅作 last resort — 多数情况解法 1+2 已足够。

## Agent 自检 checklist（仅本工程，但可推广到任何 Unity + 自定义 event 项目）

设计 R3 PlayMode spike 时（"AddComponent<T>() spawn spike + 调 trigger 派发 event + spike 监听"模式）：

- [ ] **spike 监听是否写在 `Awake()` 而非 `Start()`**？
  - `Awake()` 在 `AddComponent` 同步返回前执行；`Start()` 至少推迟到 end-of-frame。
  - 如果第 1 个事件可能在同帧同步 fire（listener handler 内同步派发的），必须用 `Awake()`
- [ ] **driver 调用顺序是否先 spawn spike，再派发 trigger**？
  - `DevBootstrap.RunRequested()` / `AddComponent<T>()` 在前，`OnRequestXxx(...)` 派发在后
  - 如果两个调用之间没 await，trigger 派发的 handler 内任何 sync-fire 事件都依赖 spike subscribe 已完成
- [ ] **listener handler 是否同帧同步 fire 多个事件**？
  - grep production handler 是否走 `async void` / `Forget()` 但首次 `await` 之前已派发 ≥1 个事件
  - 如果是，spike 端必须按解法 1+2 配置；不能用 `Start()`
- [ ] **R3 复用 S5-1b/S3-xx spike 模板时，第 1 个事件路径是否一致**？
  - S5-1b/S3-xx 第 1 事件来自 spike 直调 `await driver()`（异步路径）
  - S5-1c 第 1 事件来自 listener handler 同步 fire（同步路径）
  - **两类不能复用同样的 spike subscribe 时机** — sync-fire 路径必须 Awake() subscribe
- [ ] **PlayMode 跑出来"第 1 事件漏 capture"时，先排查 spike subscribe 时机而非 production code**
  - 如果 events 数组缺少**第 1 个**生命周期事件、其他事件正常 → 99% 是 spike subscribe 太晚
  - 如果 events 数组**全部**生命周期事件都缺失 → 才是 production code 漏派发或 listener-path 没串通

## 与其他文档 / 协议的关系

- **与 `problem_2026-05-09_adr009-listener-path-driver-missing.md`**：本 race 是该 problem 修复后（S5-1c）首次撞到的二阶问题；前者是 production code 协议层 gap，本文是 spike test 协议层 gap，二者互补
- **与 `problem_2026-05-09_spike-vs-production-singleton-conflict.md` (M1 dual-layer)**：M1 dual-layer 解决"singleton state vs isolation state"；本文解决"sync-fire event vs subscribe timing"；两条 spike 设计准则建议合并到 future"R3 spike 设计 playbook"统一文档
- **与 `ADR-029 V2.0 §V2-7 V3 watch list`**：本 race 已在 sprint-status.yaml `sprint_5_adr_029_v3_watch_list` 标记 V3 candidate Type-2(c) "spike subscribe race"；Sprint 5 retro 时评估是否 promote 为正式 V3 candidate Type-6
- **与 `~/.cursor/rules/problem-to-rule-promotion.mdc`**：本问题满足"修复路径不显然 + 错误信号弱（events 缺第 1 个）+ 框架级隐式规则（MonoBehaviour Awake/Start timing）"3 项触发条件；按协议沉淀本工程 lessons memo（不沉淀跨工程 rule — 因为这是 Unity + 自定义 event system 才会撞，不属"全语言通用"）
- **与 `S5-1c evidence doc §6 Watch List Hooks`**：本 memo 是该 §6 的 expanded technical post-mortem

## 历史记录

- **2026-05-09 创建**（Session 25 #3）。触发场景：S5-1c `/dev-story` 实施期间 P1 first-boot R3 case 第 1 次跑漏 capture `OnSceneTransitionBegin`，调试 ~30 min 排除 production code，最后定位到 spike `Start()` subscribe 太晚 + DevTestState OnEnter 调用顺序不对；同步修复 (a) `S51cRuntime.Awake()` subscribe + (b) `DevTestState.OnEnter` 顺序调整 RunRequested → OnRequestSceneChange，第 2 跑 5/5 PASS。按 `problem-to-rule-promotion.mdc` 协议判定为"Unity + custom event system 项目独有的 sync-fire event race"，沉淀本工程 lessons memo + sprint-status.yaml `sprint_5_adr_029_v3_watch_list` Type-6 candidate 入档；不沉淀跨工程 cursor rule（因为只在 Unity + 自定义 event system 触发）。
- **预期效果**：Sprint 5+ 后续 R3 spike 设计时（特别是 listener-path / sync-fire event 路径），按 Agent 自检 checklist 1-3 即可避免重蹈；S5-1c 实测调试成本 ~30 min（race 定位 ~20 min + 修复 ~10 min），预期 future R3 spike 撞同 race 时 < 5 min 即可定位（按 checklist 第 5 条直接排查 spike subscribe 时机）。
- **预期 follow-up**：Sprint 5 retro 评估是否 promote 为 ADR-029 V3 candidate Type-6 "spike subscribe race"（如再发生 ≥1 次同类 race → promote；如本次后即使复用 listener-path 模板都无 race → 保持 problem memo + checklist 即可）；与 `problem_2026-05-09_spike-vs-production-singleton-conflict.md` (M1 dual-layer) 合并为"R3 spike 设计 playbook" candidate 文档（如 R3 spike 模板复用率达 ≥6 次实战触发标准化文档需求）。
