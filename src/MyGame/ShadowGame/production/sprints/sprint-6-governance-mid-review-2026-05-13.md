// 该文件由Cursor 自动生成

# Sprint 6 Governance Mid-Review — Tracks B+C 收官后阶段性治理沉淀

> **Sprint N**: 6 (in_progress — Track A/D/E 余下，本文档非 final retro)
> **日期**: 2026-05-13 evening (Session 30 continue 收尾后)
> **Scope**: dp8 + dp11 + dp12 三 candidate closure + CLAUDE.md ≤5 hr hard rule 2 次 [C] override 反思 + Sprint 6 final retro 待处理议题 carry-forward
> **Non-Scope**: 完整 Sprint 6 retro（留 Sprint 6 结束后 Track A playtest ×3 + Track D art asset + Track E gate-check 一并写 `sprint-6-retrospective.md`）
> **触发**: Sprint 6 Track C 100% 收官 (5/5 done: S6-05 + S6-06 + S6-07 + S6-08 + S6-04) + VS Chapter 1 epic 100% 完成 + 本 session ~7-8 hr governance debt 累积 + 3 candidate 同 session 三联触发
> **关联 retro**: [sprint-5-retrospective.md](./sprint-5-retrospective.md) (Sprint 5 final retro)
> **Governing ADR**: [ADR-029 governance maturity v3.0](../../../docs/architecture/adr-029-spec-quality-iteration-protocol.md) §V3-1.b 修复模式 + §V3-5 V4 trigger

---

## §0 Executive Summary

Sprint 6 Tracks B+C 100% 收官 (5 stories / 9 SP done out of Must Have 16 SP) — chapter 1 robustness 补全完成 (S6-07 main menu polish ✅ + S6-08 popup/inputblocker robust ✅ + S6-04 error/restart ✅) + ADR governance amend 完成 (S6-05 ✅ + S6-06 ✅)。**VS Chapter 1 epic 100% 完成** — ADR-030 §VS Build commit 第 1 项 chapter 1 robustness 补全 closure；≥3 playtest sessions prerequisite 100% ready。

**本中期治理沉淀 4 大议题**：

1. **dp8 candidate** (DevTestState `[main-menu]` mode 复用 阈值 4 触达) — **V3.1 trigger 候选**，决策 carry forward Sprint 6 final retro
2. **dp11 candidate** (sprint backlog placeholder wording drift) — Phase 1 story rewrite 修正 closure ✅
3. **dp12 candidate NEW** (isolated local SceneManager + global GameEvent collateral) — Phase 3 第 1 跑首次实战暴露 + Phase 3 spike amend collateral cleanup closure ✅ + lessons memo 沉淀；**ADR-009 V3.x watch list 候选**
4. **CLAUDE.md ≤5 hr hard rule 2 次连续 [C] override** — Sprint 6 末单次 session 总投入 ~7-8 hr，governance debt 累积 — 防御性策略待 Sprint 6 final retro 决策

**核心 governance pattern 实战收益**：

- **V3.0 governance maturity 持续运转** — Sprint 5 retro AI-1 promote V3.0 后，Sprint 6 Track C 实战中 dp11 即时 closure (Phase 1 story rewrite 阶段)、dp12 first-strike 即时 closure (Phase 3 spike amend 阶段)，2 个 dp 在同 sprint 内闭环未拖到 retro — **AI-8 cycle 改造 (dp ≥2 立即 V3 candidate) Sprint 6 首次实战见效** (相比 Sprint 5 Type-5 dp1→dp6 累计 11 day 才 promote)
- **R2 DEFICIENCY-FLAGGED PASS 模式三连实战** — S6-07 / S6-08 / S6-04 三 story 均走"R2 verify 找 deficiency → narrow scope [A] 决策 → Phase 1 readiness gate DEFICIENCY-FLAGGED PASS → Phase 2 production code 或 spike → Phase 3 R3 closure"路径，模式稳定可复用
- **同 session 多 candidate 三联触发首次实战** — S6-04 单 session 内同时触发 dp8 阈值 + dp11 closure + dp12 NEW first-strike，三 candidate 共存治理负担显著 → governance debt 累积压力实证

---

## §1 dp8 Candidate — DevTestState `[main-menu]` Mode 复用阈值 4 触达

### §1.1 问题描述

`Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs::OnEnter()` 内 `[main-menu]` 分支条件随每个新 spike 累加 `HasSpike("S?-??")`：

```csharp
if (DevTest.DevBootstrap.HasSpike("S5-02")
    || DevTest.DevBootstrap.HasSpike("S6-07")
    || DevTest.DevBootstrap.HasSpike("S6-08")
    || DevTest.DevBootstrap.HasSpike("S6-04"))  // ← 第 4 个 (S6-04 添加触发阈值)
{
    // pre-fire OnRequestSceneChange(1) OR 让 spike 自管 chapter 1 baseline
    ...
}
```

### §1.2 阈值触发历史

| Sprint | 添加项 | 累计 | 状态 |
|---|---|:---:|:---|
| Sprint 5 | S5-02 (chapter 1 happy path) | 1 | 初次引入 `[main-menu]` mode |
| Sprint 6 | S6-07 (main menu polish) | 2 | dp8 candidate 首次记录 (S6-07 Phase 2 中间 fix — DevTestState 仅识别 S5-02 → S6-07 落 [story-001c] 分支误判 fix) |
| Sprint 6 | S6-08 (popup/inputblocker robust) | 3 | dp8 candidate 距阈值 4 仅 1 |
| Sprint 6 | S6-04 (error/restart path) | **4** | **阈值触达 ✅** — Sprint 6 final retro 强制评估 promote V3.1 trigger |

### §1.3 V3.1 trigger 候选方案

**方案 A — `IDevSpike.IsMainMenuMode` flag**:

```csharp
public interface IDevSpike
{
    string Id { get; }
    bool IsMainMenuMode { get; }  // ← NEW per V3.1 candidate
    UniTask RunAsync();
}

// DevTestState.OnEnter():
if (DevTest.DevBootstrap.RegisteredSpikes.Any(s => s.IsMainMenuMode))
{
    ShowMainMenuPanelAsync().Forget();
    return;
}
```

**优点**: spike 自声明 mode 类别，DevTestState 不再 maintain `HasSpike` 列表；新增 spike 0 改动 DevTestState
**缺点**: 增加 `IDevSpike` 接口 surface (Sprint 5 + 6 现有 6 spikes 需追加属性)；不向后兼容

**方案 B — DevTestState central mode-dispatch refactor**:

```csharp
// DevBootstrap 内集中 mode 注册
public static void Register(IDevSpike spike, DevTestMode mode = DevTestMode.None)
{ ... }

// GameApp.RegisterDevSpikes():
DevBootstrap.Register(new S604Spike(), DevTestMode.MainMenu);

// DevTestState 仅 dispatch:
switch (DevBootstrap.GetActiveMode())
{
    case DevTestMode.MainMenu: ShowMainMenuPanelAsync().Forget(); break;
    case DevTestMode.Chapter1Loaded: PreLoadChapter1Async().Forget(); break;
    ...
}
```

**优点**: mode 与 spike 解耦；扩展 mode 类型 (chapter1-loaded / chapter2-loaded / popup-show) 自然
**缺点**: 改动面较大 (`DevBootstrap` + `DevTestState` + 6 现有 spike 注册点)；ROI 待评估

**方案 C — 保持现状显式 `HasSpike` 列表**:

**优点**: 0 改动 + 显式列表 reviewer 一眼可读
**缺点**: 每次新增 spike 1 处 amend；distinct spike 数量未来 ≥10 时维护成本上升

### §1.4 决策 (carry forward Sprint 6 final retro)

**本 mid-review 不做最终决策**。理由：
1. V3.1 trigger 候选属 ADR-029 V3 governance scope，需配合 V3.1 amend 整体节奏 (V3.0 promote 2026-05-12 仅 1 day → V3.1 sub-version overhead 节奏太密)
2. 当前距阈值 4 仅刚触达 1 sprint，需观察 Sprint 7+ 是否继续累加 (如 chapter 2 epic 引入 chapter-2-loaded mode → DevTestState 自然分化为多 mode → 方案 B ROI 提升)
3. 配 dp8 单独评估 cost > benefit；建议合并 Sprint 6 final retro 后 V3.x 一次性评估

**Sprint 6 final retro 准入议题**: dp8 candidate promote V3.1 trigger 方案 (A/B/C) 决策。

---

## §2 dp11 Candidate — Sprint Backlog Placeholder Wording Drift Closure ✅

### §2.1 问题描述

Sprint 5 [B] split_2 创建 S5-2b placeholder (chapter 1 error/restart path → Sprint 6) 时，placeholder description 内 2 处 wording drift from vendor `SceneManager.cs` 实际语义：

| Placeholder wording | Vendor 实际语义 | drift type |
|---|---|---|
| "mid-transition cancel" | `_pendingTargetChapterId = newest target` (`SceneManager.cs:350-352`) — **newest-wins pending overwrite**, 不是 cancel | wording drift |
| "repeated rapid debounce" | `OnRequestSceneChange` 多次连发时同 newest-wins pending overwrite — **不是 debounce 抑制** | wording drift |

### §2.2 触发场景

S6-04 Phase 1 story rewrite (`story-003-error-restart-path.md`) 期间，从 Sprint 0 placeholder 翻译到 V3.0.1 vendor reality compliant spec — R2 grep vendor `SceneManager.cs::OnRequestSceneChange` (line 312-352) + `DrainPending` (line 358-385) 实证语义后，识别 placeholder 2 处 wording 与 vendor 实际不符。

### §2.3 修复

S6-04 story-003 Phase 1 spec rewrite 直接 amend：

- AC-9 wording: "mid-transition cancel" → "newest-wins pending overwrite"
- R3 P2 case rename: NewestWinsPendingDuringTransition (替代 placeholder cancel wording)
- R3 P5 case rename: RapidNewestWinsOverwrite (替代 placeholder debounce wording)

evidence: `production/qa/playmode-error-restart-path-2026-05-13.md` §4 V3.0.1 Watch List Hooks Closure block。

### §2.4 governance value

**单次实战不足 promote V3.x candidate** — placeholder wording drift 在 Sprint backlog 阶段属于 expected (placeholder 本就是 "未来 implementor 重 verify" 标记)；S6-04 Phase 1 R2 grep vendor source 工序自然就 catch。

**真正 governance value**: V3.0 §V3-1.b 修复模式 enforcement 实战见效 — Phase 0 R2 vendor reality verify 工序 (ADR-029 V2.0 §V2-1.b R2 grep 完备性) 自动 catch placeholder wording drift。不需要单独 promote V3.x type。

### §2.5 决策

**状态: ✅ CLOSURE**。

**carry forward**: 0 — 本 candidate 已闭环。Sprint 6 final retro 无需再处理；仅作为 V3.0 §V3-1.b 修复模式 enforcement 实战 ROI 实例之一记录。

---

## §3 dp12 Candidate NEW — Isolated Local Instance + Global Event-Bus Collateral

### §3.1 问题描述

S6-04 Phase 3 R3 PlayMode **第 1 跑** P4/P5 precondition FAIL (期望 `state=Idle`, 实测 `state=Error, currentChapterId=1`)。

排查路径：

```
P4 报错 → 怀疑 P4 setup 问题 → 检查 P3 是否 cleanup 干净
  → P3 cleanup 只 local.Dispose() 一行 ✗
  → 检查 P3 fire 路径
  → GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99) 是 GLOBAL broadcast
  → production sm 也是 listener instance → 同步收到 broadcast
  → production sm TryResolveOrFail(99) 返 false → 进 Error
  → P4 precondition FAIL ✗
```

**核心**: `GameEvent.Get<IXxxEvent>().XxxMethod(...)` 不是 instance-bound 方法调用，而是 **global broadcast** — 任何订阅了 `IXxxEvent_Event.XxxMethod` 的 listener instance 都会被 invoke。

### §3.2 触发上下文

S6-04 spike 用 **M1 dual-layer 模式** (per S5-1b precedent `problem_2026-05-09_spike-vs-production-singleton-conflict.md`)：

- P1/P2/P4/P5 用 production `GameApp._sceneManager` (reflection 拿)
- **P3** 用 isolated local `new SceneManager()`

**dual-layer 模式 isolation 盲区**：

| isolation 维度 | M1 dual-layer 覆盖 | dp12 揭示 |
|---|:---:|---|
| 状态 isolation | ✅ | local 状态独立 |
| 资源持有 isolation | ✅ | local 持自己 fixture |
| Event listener fan-out isolation | ❌ | **GLOBAL broadcast 同时 invoke production + local 两个 listener** |

S5-1b 用 dual-layer 时 P4/P5 是 spike **最后 case**，跑完即结束 — collateral 未被后续 case observe。S6-04 P3 在**中间** + P4/P5 在后面 → **第一次暴露此模式**。

### §3.3 修复 (closure)

`RunP3Async(SceneManager productionSm)` 加 production sm collateral cleanup：

```csharp
private async UniTask RunP3Async(SceneManager productionSm)  // ← 新增参数
{
    var local = new SceneManager();
    // ... setup ...
    GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99);
    await UniTask.Delay(500);
    // ... local sm 验证 ...

    // Collateral cleanup (NEW per dp12 finding)
    try { local.Dispose(); } catch { /* ignore */ }
    if (productionSm.CurrentState == SceneManagerState.Error)
    {
        productionSm.RecoverToIdle();
    }

    // Cleanup verify assert (NEW per dp12 finding)
    _asserts["P3.cleanup_production_idle"] = productionSm.CurrentState == SceneManagerState.Idle
        ? "PASS" : $"FAIL: state={productionSm.CurrentState}";

    P3Passed = P3Passed && productionSm.CurrentState == SceneManagerState.Idle;
}
```

第 2 跑 5/5 PASS + 37/37 asserts + total 1867ms < 8s budget。

### §3.4 governance value

**dp12 是 dp11 不一样的等级** — dp11 是 placeholder wording 修复，governance cost 低；dp12 是 **dual-layer pattern 盲区暴露**，需要修订 dual-layer pattern 文档化。

**沉淀产出**:
- `.claude/memory/problem_2026-05-13_isolated-local-instance-via-global-event-bus.md` (lessons memo per problem-to-rule-promotion 元规则 [C] memo_only 决策；4225 行模板 + dual-layer pattern 补丁说明 + Agent self-check checklist)
- `production/qa/playmode-error-restart-path-2026-05-13.md` §4 dp12 NEW first-strike closure block
- `production/sprint-status.yaml` S6-04 entry dp12 details
- 本文档 §3 governance value 总结

### §3.5 ADR-009 V3.x watch list 候选评估

**问题本质**: GameEvent 设计为 global pub/sub bus — 如果 spike 需要 isolated local instance 行为，需 framework-level 支持 **scoped event-bus** 或 **instance-bound event API**。

**候选 V3.x 设计选项**:

**选项 1 — scoped event-bus**:
```csharp
public static class GameEvent
{
    public static IEventDispatcher GetScope(string scopeId) { ... }
}

// Spike:
var localScope = GameEvent.GetScope("spike-s604-p3");
localSm.RegisterScope(localScope);
localScope.Get<ISceneEvent>().OnRequestSceneChange(99);  // 仅 local sm listener invoke
```

**选项 2 — instance-bound event method**:
```csharp
public class SceneManager
{
    public void LocalFire<T>(Action<T> action) where T : IEvent { ... }
}

// Spike:
localSm.LocalFire<ISceneEvent>(e => e.OnRequestSceneChange(99));  // 仅 local sm 自身处理
```

**选项 3 — 不引入 framework 变更，仅文档化 cleanup pattern**: 同当前 S6-04 修复路径；每个用 isolated-local instance 的 spike 显式 cleanup production collateral。

### §3.6 决策

**状态: ✅ CLOSURE (修复 + 沉淀)**。

**carry forward**:
- **ADR-009 V3.x watch list**: dp12 候选加入 (spec amend candidate — Sprint 7+ 评估 scoped event-bus 或 instance-bound event API 引入)；本 mid-review 不写入 sprint-status.yaml (per [C] yaml_skip)，留 Sprint 6 final retro 一并写入
- **Sprint 6 final retro 准入议题**: dp12 governance value 是否升级为 ADR-029 V3.x candidate (Type-? "test isolation pattern: event-bus fan-out blind spot")，或留 ADR-009 单独 amend
- **Agent self-check** 已沉淀至 lessons memo §"Agent 自检 checklist"，下次写涉及 GameEvent 的 spike 时强制 check

---

## §4 CLAUDE.md ≤5 hr Hard Rule 2 次连续 [C] Override 反思

### §4.1 事实记录

Session 30 (2026-05-13 evening) 单 session 工作量：

| 阶段 | 时长 | 内容 |
|---|:---:|---|
| Phase 2~5 of S6-08 (popup/inputblocker robust) | ~3 hr | 实施 + R3 跑 2 round + evidence + closure |
| S6-04 Phase 0+1 (R2 + readiness gate) | ~1 hr | story rewrite + 7 R2 + 10 AC + readiness gate DEFICIENCY-FLAGGED PASS |
| **[C] override #1** | — | user 决策继续 S6-04 Phase 2~5 一气呵成 |
| S6-04 Phase 2 (spike code) | ~40 min | 620 行 spike + GameApp + DevTestState wire |
| S6-04 Phase 3 (R3 跑 + dp12 closure) | ~30 min | 第 1 跑 P4/P5 FAIL → dp12 暴露 → spike amend → 第 2 跑 5/5 PASS |
| S6-04 Phase 4 (evidence doc) | ~25 min | ~400 行 8 sections |
| S6-04 Phase 5 (closure) | ~15 min | story/EPIC/yaml/active.md amend |
| **[C] override #2** | — | user 决策继续 Sprint 6 retro 启动 (本 doc) |
| 本 mid-review 写作 | ~25 min (in_progress) | 本 doc + 决策 carry forward |

**总投入**: **~7-8 hr** (S6-08 Phase 2~5 ~3 hr + S6-04 Phase 0+1 ~1 hr + S6-04 Phase 2~5 ~2.25 hr + mid-review ~25 min) = **远超 CLAUDE.md ≤5 hr hard rule 60-80%**。

### §4.2 反思

#### §4.2.1 为什么 override 发生？

**触发因素**:
1. **任务自然边界压力**: S6-04 Phase 0+1 readiness gate ✅ 后，Phase 2~5 是连续工作 (spike → R3 → evidence → closure)，自然断点不明显
2. **single-sprint dp 闭环优先级**: dp12 在 Phase 3 第 1 跑 first-strike 暴露，立即修复 (Phase 3 amend) + 立即沉淀 (Phase 4 evidence + Phase 5 yaml + 本 mid-review lessons memo) 是 V3 governance maturity 模式 — 拖到下 session 会增加 context 重载成本
3. **Track C 一气呵成的吸引力**: S6-04 closure 同时是 Sprint 6 Track C 100% closure + VS Chapter 1 epic 100% closure 两个 milestone，单 session 完成成就感强 + 工程进度 reporting 清晰

#### §4.2.2 实际危害

- **质量风险**: Phase 3 第 1 跑 P4/P5 FAIL 时已是 session 第 5-6 hr，疲劳状态下排查 dp12 root cause；如果当时直接归因 P4/P5 写错 (而非 P3 collateral)，可能跑出 spurious "fix" 实际是 mask 真问题 — 实际本次没踩坑但是侥幸
- **commit 体积膨胀**: 单 commit 10 files / 1561 insertions — 如果需要 revert 影响面大 (好在 git history 可分段 cherry-pick)
- **认知负荷累积**: 本 mid-review 写作时已是 session ~7 hr，对 dp8/dp11/dp12 三 candidate 同时分析的认知 bandwidth 下降；本 doc 决策 carry forward 多数议题 (而非现场决) 部分是疲劳应对
- **governance debt 自身**: hard rule 2 连 override 形成 "override 习惯化" 风险 — 下个 session 如果再有同类压力，第 3 次 override 心理门槛降低

#### §4.2.3 hard rule 设计意图回顾

CLAUDE.md `session ≤5 hr hard rule` 设计意图 (per Sprint 4 retro Process Improvement #3 + Sprint 5 retro Process Improvement #3):
- 防止疲劳 → 质量下降
- 强制 session boundary → context 切换 + 复盘机会
- 控制单 commit 体积
- 避免单 contributor 单点 marathon → 项目可持续性

**本 session 实证**: 4 项设计意图本 session 都在某种程度上受影响 (前 3 项轻度，第 4 项不适用因 solo)。

### §4.3 防御性策略候选 (carry forward Sprint 6 final retro)

#### §4.3.1 选项 A — 强化 hard rule，加自动 enforcement

- 引入 session timer (TodoWrite 或类似工具显式记录 session 开始时间)
- ≥4 hr 时强制 trigger 一次 "是否结束 session" AskQuestion
- ≥5 hr 时**默认结束 session** (除非用户主动 [C] override 且记录 override 理由到 sprint-status.yaml governance debt 字段)
- override 限频: 每 sprint 最多 1 次 [C] override (Sprint 6 已用 2 次 = 透支)

**优点**: enforcement 力度大；session boundary 强制
**缺点**: AI 工具自主性受限；对单 session 自然边界长任务 (如 S6-04 Phase 2~5 4.25 hr 实际还是合理 unit) 不友好

#### §4.3.2 选项 B — 软化 hard rule 为 soft guideline，但加 governance debt 显性追踪

- hard rule → soft target (~5 hr 推荐，最多 ~7-8 hr 软上限)
- 每次超过 5 hr 自动追加 sprint-status.yaml `governance_debt` block 一行 (session id + 时长 + 理由)
- Sprint retro 时强制审视累积 debt — 如累计 ≥3 次 override 一个 sprint → 调整 sprint capacity plan (说明承诺过载)

**优点**: 务实承认 burst session 的真实价值；治理负担 visible
**缺点**: 容易 normalize override；hard rule 名字与实质不符

#### §4.3.3 选项 C — Phase-level checkpoint 替代 time-level

- 不按时长，按 "完成单元" (Phase 5 closure / commit) 设 hard checkpoint
- 每完成 1 个 Phase 5 closure → 强制 AskQuestion "是否继续 / 是否结束"
- ≤5 hr soft target 不再 binary，而是给 AskQuestion 时的 default 倾向 (≥5 hr 时默认建议结束，但用户决策权保留)

**优点**: 与工作的自然 unit 对齐；不破坏 burst session 的合理价值
**缺点**: Phase 5 closure 间隔不均 (S6-04 Phase 2~5 ~2.25 hr 一个 unit，但 Phase 0+1 + Phase 2~5 + mid-review 三个 unit 累计可超时)

#### §4.3.4 倾向 (carry forward Sprint 6 final retro 决策)

倾向 **选项 B + 选项 C 混合**: 软化为 soft guideline + governance debt 显性追踪 + Phase-level checkpoint AskQuestion。理由：
- 选项 A 过严，对 solo + burst-friendly 工作不友好
- 选项 B 单独太软，缺触发机制
- 选项 C 单独缺累积监控
- B + C 组合: AskQuestion checkpoint 触发 + governance debt 累积监控

**Sprint 6 final retro 准入议题**: hard rule 防御性策略最终决策 (A/B/C/B+C)；本 mid-review 不做最终决策。

### §4.4 立即可做的事 (do now, not carry forward)

1. **本 session 立即结束** — 不再启动 S6-01 playtest 1 或其他大动作 (尽管 active.md Next milestone 提及，但留下 session 起步)
2. **本 mid-review commit 后 session 强制结束** — 不再 expand scope
3. **不写 sprint-status.yaml governance_debt 字段** — per [C] yaml_skip 决策，留 Sprint 6 final retro 一并设计 schema 写入

---

## §5 进入 Sprint 6 Final Retro 待处理议题 (Carry-Forward Checklist)

Sprint 6 final retro (写于 Sprint 6 真正结束后，即 Track A playtest ×3 + Track D art + Track E gate-check 全 done 之后) 必须处理：

| # | 议题 | 来源 | 决策类型 | Priority |
|---|---|---|---|:---:|
| C-1 | **dp8 V3.1 trigger promote** — `IDevSpike.IsMainMenuMode` flag (方案 A) vs DevTestState central mode-dispatch refactor (方案 B) vs 保持现状 (方案 C) | §1.4 本 doc | ADR-029 V3.x 决策 | High |
| C-2 | **dp12 ADR-009 V3.x watch list 写入** — 写入 sprint-status.yaml `adr_009_v3_watch_list` (新建 block) + 评估是否升级 ADR-029 V3.x candidate Type-? "test isolation pattern: event-bus fan-out blind spot" | §3.6 本 doc | ADR-009 + ADR-029 V3.x | High |
| C-3 | **CLAUDE.md ≤5 hr hard rule 防御性策略** — 选项 A (强化) vs 选项 B (soft guideline + debt 追踪) vs 选项 C (Phase-level checkpoint) vs 组合 | §4.3 本 doc | CLAUDE.md amend + sprint-status.yaml schema 设计 | High |
| C-4 | **sprint-status.yaml `governance_debt` block schema 设计** — 字段：session_id / 实际时长 / hard rule 上限 / override 理由 / cumulative debt count；本 mid-review per [C] yaml_skip 未写入，待 schema 决策后一次性 retroactive 写入 (含本 session ~7-8 hr × 2 override) | C-3 衍生 | yaml schema design | High |
| C-5 | **Sprint 6 完整 retro** 写 `sprint-6-retrospective.md` (含 Tracks A/B/C/D/E 全 done 后总结 + 本 mid-review C-1~C-4 议题决策 + Track A playtest report meta-summary + V3 watch list Sprint 6 末评估) | Sprint 6 自然结束 | Retro 写作 | High |
| C-6 | **Sprint 5 retro AI-7/AI-8 cycle 改造实战 ROI 评估** — Sprint 6 是 AI-8 (dp ≥2 立即 V3 candidate) 首次实战 sprint，dp11 + dp12 同 sprint 闭环符合 AI-8 设计；Sprint 6 final retro 实证 AI-8 ROI 数据 | Sprint 5 retro AI-8 + 本 doc §0 | Action item ROI 评估 | Medium |
| C-7 | **dp11 不 promote V3.x 决策记录** — 单次实战 + V3.0 §V3-1.b enforcement 自然 catch 模式，无需单独 V3.x type；本 mid-review §2.5 已决策，仅作为 Sprint 6 final retro audit trail 引用 | §2.5 本 doc | 决策 record | Low |
| C-8 | **R2 DEFICIENCY-FLAGGED PASS 三连实战 (S6-07 + S6-08 + S6-04) ROI 评估** — Sprint 6 三 story 均走此路径，模式稳定 → 评估是否升级为 ADR-029 V3.x 标准 path (vs 现在 V2.0 §V2-1.b 可选 path) | §0 governance pattern 实战 | ADR-029 V3.x 决策 | Medium |

**carry-forward 总数: 8 项**，其中 5 项 High priority 必决。

---

## §6 References

- **本 mid-review 触发 commit**: `e033c28` "S6-04 Phase 2~5 ✅ DONE — chapter 1 error/restart path R3 PlayMode 5/5 PASS + V3.0.1 NEW dp12 candidate first-strike closure — Sprint 6 Track C 100% 收官 + VS Chapter 1 epic 100% 完成"
- **dp11 closure evidence**: `production/qa/playmode-error-restart-path-2026-05-13.md` §4 V3.0.1 Watch List Hooks Closure
- **dp12 first-strike evidence**: `production/qa/playmode-error-restart-path-2026-05-13.md` §4 dp12 NEW + `.claude/memory/problem_2026-05-13_isolated-local-instance-via-global-event-bus.md` (lessons memo)
- **dp8 实战记录**: `Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs` `[main-menu]` mode 第 4 个 HasSpike 触发位置 + S6-04 spike code `Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6-04_ErrorRestartPath.cs`
- **governance debt 累积上下文**: `production/session-state/active.md` "Earlier in Session 30 continue —" + sprint-status.yaml line 11 (updated 字段)
- **dual-layer pattern 原始**: `.claude/memory/problem_2026-05-09_spike-vs-production-singleton-conflict.md` (S5-1b 实战首发) → 本 doc §3 dp12 揭示 dual-layer 盲区，需 patch
- **R2 DEFICIENCY-FLAGGED PASS 三连**: S6-07/S6-08/S6-04 三 story Phase 1 readiness gate (各 story 文件 §History entry)
- **Governing ADR**: ADR-029 §V3-1.b 修复模式 + §V3-5 V4 trigger (`docs/architecture/adr-029-spec-quality-iteration-protocol.md`)
- **Sprint 5 retro 参考**: [sprint-5-retrospective.md](./sprint-5-retrospective.md) (Sprint 5 final retro 完整 schema 参考)
- **Sprint 6 plan**: [sprint-6.md](./sprint-6.md) (Sprint 6 commitment + Tracks + Action items 矩阵)

---

## §7 Verdict

**Sprint 6 governance mid-review 完成 ✅** — 3 candidate (dp8 阈值触达 / dp11 closure / dp12 first-strike closure) + 5 hr hard rule 2 次 override 反思全部沉淀。本 mid-review 严格遵守 [C] governance_only + [C] yaml_skip 决策 — 仅本文档 1 文件输出，不动 sprint-status.yaml，不写 sprint-6-retrospective.md (留 Sprint 6 真正结束后 final retro)。

**8 项 carry-forward 议题** 已列入 §5 checklist，等 Sprint 6 final retro 处理 (5 High priority 必决)。

**立即可做的事 (per §4.4)**: 本 session 立即结束。下 session 起步 S6-01 playtest 1 dev-story。

**本 doc 投入时长**: ~25-30 min (符合用户预算 ~30-45 min)。
