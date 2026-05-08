// 该文件由Cursor 自动生成

# ADR-029: Story §Implementation Notes 验证流程（Drift Prevention Gate）

## Status

**Accepted V2.0** — 2026-04-30 dusk *(was V1.0 2026-04-30 morning; backward-compatible major revision based on Sprint 3 实战 6 数据点 + 7 V2 candidates 全部触发)*

## Version

| Version | Date | Trigger | Scope |
|--------|------|---------|------|
| V1.0 | 2026-04-30 morning | Sprint 2 lessons memo 4 次复用稳态触达 | R1/R2/R3 grep gate 形式化 + 3 skill 集成 + drift metric 跟踪 |
| **V2.0** | **2026-04-30 dusk** | **Sprint 3 Must Have 4/4 闭环 + 6 数据点 + 7 candidates 实战触发** | **Drift Types V2 (Type-2 拆 a/b/c) + R3 mandatory + Propagation v2 with single source of truth priority + Framework boundary behavior probe checklist + 7 trigger conditions formalized + drift revision time tracking metrics** |

## Date

2026-04-30 dusk *(V2.0; V1.0 起草于同日 morning)*

## Last Verified

2026-04-30 dusk

## Decision Makers

Technical Director, Lead Programmer, dev-story skill maintainer

## Summary

形式化"Story §Implementation Notes 与 Framework 实际能力 drift"防御机制为 **3 项 readiness gate**（R1 per-event listener 模式实证 / R2 跨组件 API 调用 grep 实证 / R3 stub 数据类型构造签名 grep），并并入：
- `/create-stories` skill **Phase 1 前置检查**（生成时强制实证）
- `/dev-story` skill **5min readiness checklist**（实施起手强制验证；升级为 ADR-blessed gate）

本 ADR 闭环 Sprint 2 期间反复出现的 §Implementation Notes drift 模式 —— `/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md` 已 **4 次复用**（S2-09 push/pull / S2-12 整接口订阅 / S2-13 整接口订阅 + 不存在 API + Luban 缺口 / S2-11 整接口订阅 + 缺 RotationStep），稳态触达"该升 ADR 形式化"的工程信号。Sprint 3 起 drift 修订耗时 metric 归零跟踪验证形式化效果。

---

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 2022.3.62f2 (LTS) — 实际为引擎无关 process-level decision |
| **Domain** | Process Governance / Story Authoring Workflow |
| **Knowledge Risk** | NONE — 基于已观察的 drift 模式事实陈述（4 次反复，2 个工作流 skill 影响）|
| **References Consulted** | `/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md`（lessons memo + 4 次 history）；`src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/conventions.md` §"Listener 端订阅模式"；ADR-027（GameEvent Interface Protocol）；Sprint 2 retrospective `production/sprints/sprint-2-retrospective.md` §"Drift Prevention Track" |
| **Post-Cutoff APIs Used** | None — process-level decision，不依赖 engine API |
| **Verification Required** | (1) Sprint 3 平均 §Impl Notes drift 修订耗时 ≤ 1min（成功）；(2) `/create-stories` 与 `/dev-story` skill 文档已并入 R1/R2/R3 强制检查；(3) 任何新 story 文件经 `/story-readiness` 时执行 R1/R2/R3 验证并以独立 verdict 报告 |

---

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-027（GameEvent Interface Protocol，定义 per-event listener 模式作为唯一合规路径，本 ADR R1 据此形式化）|
| **Supersedes** | None |
| **Updates** | None — 本 ADR 不修订既有 ADR；它在元层面引入"story authoring readiness gate" |
| **Enables** | (1) Sprint 3 起 `/create-stories` 输出的 story 文件 §Implementation Notes 一致经过实证；(2) 任何后续 ADR 关于事件 / 数据 / 协议接口的实施代码 sample 都需经 R1/R2/R3 验证（ADR 形式约束）|
| **Blocks** | 任何新 story 在 `/story-readiness` 阶段未通过 R1/R2/R3 三项 gate 不得进入 `/dev-story` 实施 |
| **Ordering Note** | 本 ADR 与 Sprint 3 实施同步生效 — 已存在的 Sprint 1/2 已实施 stories 不溯及；Sprint 2 carryover stories（S3-01/-02/-03/-05）在 dev-story 起手时按 R1/R2/R3 复审一次 |

---

## Context

### Problem Statement

Sprint 2 期间观察到 **同一类 drift 4 次反复**：

| Story | Drift 类型 | 影响 |
|---|---|---|
| S2-09 (Drag) | push/pull 转发模式臆测 | dev-story 起手 ~10min 重设计 |
| S2-12 (LockManager) | `GameEvent.AddEventListener<TInterface>(this)` 整接口订阅 — API 不存在 | dev-story 起手 ~10min 修订 + 立 lessons memo |
| S2-13 (Coordinator) | 同 + `obj.SetLockManager(...)` 注入 — 不存在；Luban 缺 `FatFingerMarginMm` 字段 | ~15min 修订 + 扩 IInputConfig + InputConfigFromLuban |
| S2-11 (Rotation) | 同 IGestureEvent 整接口订阅 + PuzzleConfig 缺 `RotationStep` | ~5min 修订 + 扩 PuzzleConfig（但向下兼容 default 0 callsite 修改） |

**根因**：

1. **`/create-stories` 阶段对 framework 实际能力调研不足**：基于"理想接口"而非"实测 API"写示意代码；LLM story 生成阶段倾向于写"合理但臆测"的 API 签名
2. **Sender / Listener 双端职责混淆**：`EventMgr.RegWrapInterface<T>`（sender 端代理）与 `GameEvent.AddEventListener<TArg>(int, Action<TArg>)`（listener 端回调）在心智模型上易混
3. **Story §Implementation Notes 长期"骨架建议+成品代码"双重身份**：照抄不能编译，但 LLM 生成时按"成品代码"的精度推断 API
4. **缺乏 dev-story 入口的 readiness 强校验**：`/story-readiness` 仅校验 story 元数据完整性（AC 写了没、Type 字段填了没），不实测 §Implementation Notes 中的 API 签名

虽然 5min readiness checklist 4 次成功抓住所有 drift（**未让任何错误代码进 commit**），但每次仍消耗 5-15min 重设计 / 修订。这是**可避免的隐性成本**：如果 `/create-stories` 阶段就实证 API，dev-story 起手就是干净状态。

### Constraints

- **不破坏既有 stories**：Sprint 1/2 已 done 的 26 stories 不溯及验证（避免回归劳动）
- **不引入新 skill**：复用 `/create-stories` + `/story-readiness` + `/dev-story` 既有 skill 框架
- **保持 process 轻量**：3 项 gate 总执行时间应 ≤ 5min（保持与现有 readiness checklist 等量）
- **跨 framework 适用**：本 ADR 决策应可跨 engine（Unity / Godot / UE）适用 —— TEngine 是当下实例，但模式通用

### Requirements

- 必须将 lessons memo 4 次复用稳态升级为 process-level 强制 gate
- 必须保证 R1/R2/R3 在 `/create-stories` Phase 1（生成时）和 `/dev-story` 起手（实施时）双重执行
- 必须支持"flag deficiency 而非 hard block"：如 framework 缺 API，story 应明示 deficiency 而非阻塞 sprint
- 必须可度量：Sprint 3 起跟踪 §Impl Notes drift 修订耗时 metric

---

## Decision V1.0 (2026-04-30 morning — 起草版本)

> **Note**: V1.0 §1-§5 内容完整保留作为 V2.0 的基线；V2.0 §V2-1 ~ §V2-7 在 V1.0 之后扩展（不修改 V1.0 任何字符）。后续 reader 应以 V2.0 为准；V1.0 保留作为变更轨迹。

### §1. R1 — Per-event Listener 模式实证（Listener 端订阅）

任何 story §Implementation Notes 出现 listener 注册代码，**必须**满足：

```csharp
// ✅ 唯一合规模式（per-event）
GameEvent.AddEventListener<TArg1, TArg2, ...>(IXxxEvent_Event.OnYyy, handler);
```

**禁止用法**（grep gate 红线）：
- `GameEvent.AddEventListener<IXxxEvent>(this)` — API 不存在
- `class MyClass : IXxxEvent { public void Init() => GameEvent.AddEventListener<IXxxEvent>(this); }` — 整接口订阅幻想
- 期望 `EventMgr.RegWrapInterface<T>` 替代 listener 注册 — 这是 sender 端代理注册，不是 listener API

**Verification**：

```bash
rg "AddEventListener<I\w+Event>\(this\)" production/epics/
# 期望 0 命中（任何命中 = drift；阻塞 readiness）

rg "class \w+\s*:\s*MonoBehaviour,\s*I\w+Event" production/epics/
rg "class \w+\s*:\s*\w+,\s*I\w+Event,\s*I\w+Event" production/epics/
# 期望 0 命中（继承 IXxxEvent 接口 listener 模式不存在）
```

### §2. R2 — 跨组件 API 调用 grep 实证

任何 story §Implementation Notes 中调用其他模块 / 类的方法 / 字段，**必须**经 grep 验证存在：

| 调用类型 | grep pattern | Pass 条件 |
|---|---|---|
| 公共方法 `obj.SetXxx(...)` | `rg "public .* SetXxx\(" Assets/GameScripts/HotFix/` | ≥ 1 命中（API 存在）|
| 公开字段 `_obj.YyyConfig` | `rg "public .* YyyConfig" Assets/GameScripts/HotFix/` | ≥ 1 命中 |
| Luban 配置字段 `TbXxx.Yyy` | `rg "Yyy" Assets/GameScripts/HotFix/.*Luban.*` | ≥ 1 命中 或 显式标 deficiency |
| Provider 注入路径 `Xxx.RegisterYyyProvider(...)` | `rg "public static .* RegisterYyyProvider" Assets/GameScripts/HotFix/` | ≥ 1 命中 |

**Deficiency flag**：如 grep 0 命中且需求合理 → story §Engine Notes 显式记 `**Required Framework Extension**`，并在 dev-story 起手第一步实施扩展（不阻塞 sprint）。

### §3. R3 — Stub 数据类型构造签名 grep 实证

任何 story 测试代码用到 stub 数据类型（`PuzzleConfig` / `GestureData` / `IXxxConfig` 等），**必须**经 grep 验证：

| 类型 | grep | 检查项 |
|---|---|---|
| Class 构造 | `rg "public XxxConfig\(" Assets/GameScripts/HotFix/` | 必填参数顺序 + optional default |
| Sealed class + readonly fields | `rg "public sealed class XxxConfig" Assets/GameScripts/HotFix/` `-A 20` | 是否支持对象初始化器（readonly = 不支持，必须用构造）|
| Interface | `rg "public interface IXxxConfig" Assets/GameScripts/HotFix/` `-A 30` | 必实现的所有 properties |

**典型案例**（教训复用）：S2-13 PuzzleConfig 测试用对象初始化器 `new PuzzleConfig { ... }` → CS7036 + CS0191×3 编译错误（sealed class + readonly fields + 必填 id 参）。R3 在 readiness 阶段抓住此类问题。

### §4. Skill 集成

#### `/create-stories` skill — Phase 1 前置检查

更新 skill 文档加入 R1/R2/R3 检查项：

> 进入 Phase 2（生成 story 文件）前，对 §Implementation Notes 中所有 framework API 调用、stub 类型构造、跨组件方法调用进行 R1/R2/R3 grep 实证。任何 0 命中或 drift 模式都需在 story §Engine Notes 显式标 `**Pending Framework Verification**` 或 `**Required Framework Extension**`。

#### `/dev-story` skill — 5min readiness checklist 升级

既有 5min readiness checklist 升级为 **ADR-blessed gate**：

```markdown
## Dev-story 5min readiness checklist (ADR-029 §4 enforcement)

- [ ] **R1 per-event listener 模式**：§Implementation Notes 中所有 `GameEvent.AddEventListener<...>(...)` 调用是否符合 per-event 签名？grep `AddEventListener<I\w+Event>\(this\)` 应 0 命中
- [ ] **R2 跨组件 API**：所有跨组件 API 调用（`obj.SetXxx(...)` / `Manager.Yyy()` / `Provider.RegisterZzz(...)`）是否在 framework 中实测存在？grep `public .* Xxx\(` 应 ≥ 1 命中（或 deficiency 已 flag）
- [ ] **R3 Stub data 类型**：所有 stub data 类型（PuzzleConfig / GestureData / ...）的构造签名是否与既有定义一致？grep `public XxxConfig\(` 验位置参数 / required 必填项；sealed class + readonly fields → 必须用构造（不能对象初始化器）
- [ ] **EditMode fixture boilerplate**：是否完整复用（Camera stub + Provider 注入 + Listener 计数 + Initialize/Shutdown 顺序）？参 conventions.md §测试 fixture 规范
- [ ] **LogAssert.Expect**：期望的 ERROR/Warning 是否用 `LogAssert.Expect` 显式声明？
```

未通过 R1/R2/R3 三项的 story → readiness verdict **NEEDS WORK**，标具体 drift 项 + 修订建议 → user 决策修订或 deficiency flag 后再进入 dev-story。

#### `/story-readiness` skill — 加 R1/R2/R3 verdict

`/story-readiness` 现有 verdict 维度（READY / NEEDS WORK / BLOCKED）扩展为：
- 新增独立 verdict：**ADR-029 Verification: PASS / DEFICIENCY-FLAGGED / NEEDS-WORK**
- DEFICIENCY-FLAGGED：grep 0 命中但 story 已显式标 `Required Framework Extension`，可进入 dev-story（首步即实施扩展）
- NEEDS-WORK：grep 0 命中且 story 未标 deficiency → 阻塞，要求修订

### §5. Metric — drift 修订耗时归零跟踪

Sprint 3 起每个 dev-story 完成时记录：

```yaml
# production/sprint-status.yaml — story notes 字段
notes: "... §Impl Notes drift 修订耗时: <Xmin> ..."
```

**目标**：Sprint 3 平均 ≤ 1min（即 R1/R2/R3 在 `/create-stories` 阶段已抓住，dev-story 起手仅做 5min checklist 走过场）。

如 Sprint 3 平均 > 5min → 升级 ADR-029 V2，加强 `/create-stories` 端实证（如自动化 grep gate 强制 fail 而不仅 flag）。

---

## Decision V2.0 (2026-04-30 dusk — Sprint 3 实战 6 数据点 + 7 candidates 全部触发后正式化)

> **V2.0 触发依据**：Sprint 3 Must Have 4/4 完整闭环（S3-04 ADR-029 起草 + S3-01 baseline + S3-02 cleanup + S3-03 scene events）累计 **6 数据点**：(1) S3-01 baseline 18 min；(2) S3-02 Phase 1.5 8 min；(3) S3-02 R3 PlayMode 21 min Type-2 (b)；(4) S3-03 Phase 1.5 15 min；(5) S3-03 R3 PlayMode 12 min Type-2 (c)；(6) Lite propagation v2 13 min（meta — V2 #6 refinement）。**7 V2 candidates 全部有实战触发数据**（详见 §V2-6 触发条件表）。
>
> **V2.0 与 V1.0 关系**：V2.0 是 V1.0 的 **backward-compatible major revision**（无契约冲突；V1.0 §1-§5 grep gate 不变，V2.0 仅扩展运行时验证层 + propagation 协议 + framework boundary behavior probe）。Downstream stories 引用 ADR-029 不需变更（仍引 ADR-029，自动获得 V2.0 内容）。

### §V2-1. R1/R2/R3 grep gate（继承 V1.0 §1-§3，不变）

继承 V1.0 §1 (R1 per-event listener 模式) / §2 (R2 跨组件 API 调用) / §3 (R3 stub 数据类型构造) 三层 grep gate。V2.0 不修改 grep gate 本身；仅扩展运行时验证层 + propagation 协议 + framework boundary behavior probe。

**V1.0 §1-§3 grep gate Sprint 3 实战验证有效**：
- S3-01 baseline 18 min（含 Type-1 fantasy API 10 min）→ Phase 1.5 grep gate 成功抓住 6 处 fantasy
- S3-02 Phase 1.5 8 min Type-4 only → grep gate 节省 ~50% drift 时间 vs baseline
- S3-03 Phase 1.5 15 min Type-4×6+Type-1×2 → grep gate 抓住 8 处 drift（含 fade fantasy）

V2.0 §V2-2 ~ §V2-7 在此基线之上加运行时层 + propagation 协议。

### §V2-2. Drift Types V2 — 5 大类 + Type-2 子类细化

V1.0 §"Drift 类型分类" table 提及 4 大类（Type-1/2/3/4）；V2.0 formalize 为 **5 大类 + Type-2 拆 (a)/(b)/(c) 子类** 共 7 子型态：

| Type | 子类 | 描述 | 检测层 | 数据点案例 | 修订时间 |
|------|-----|------|-------|----------|---------|
| **Type-1** | — | API 不存在 / 签名错 / 返回类型幻想 | R2 grep gate ✅ | S3-01 6 处 fantasy / S3-03 fade infra 2 处 | 5-10 min |
| **Type-2** | **(a)** | **Framework facade behavior** — API 存在但行为/上下文不适用 | **R3 PlayMode runtime ONLY** | S3-01 `CheckLocationValid("SceneName", "DefaultPackage")` scene 资产永返 false | 5 min |
| **Type-2** | **(b)** | **Cross-method state lifecycle** — 字段语义跨方法不对称 | **R3 PlayMode runtime ONLY** | S3-02 v3 `LoadChapterSceneAsync` set `_currentChapterSceneName`，`UnloadCurrentChapterAsync` 用 `_currentChapterId` 守卫 → 永 silent return | 15 min |
| **Type-2** | **(c)** | **Framework method behavior assumption** — 单 method 行为契约假设错误 | **R3 PlayMode runtime ONLY** | S3-03 v3 TEngine `RemoveEventListener` 非 idempotent → 抛 "Delete handle failed" | 12 min |
| **Type-3** | — | Test infrastructure race — 多 spike 共享 framework 全局状态 | **R3 PlayMode runtime + 多 spike** | S3-01 SP-011 + S3-01 spike 并发 `LoadSceneAsync` 撞 YooAsset "while loading" 锁 | 3 min |
| **Type-4** | — | Architecture decision propagation drift — 决策已生效但下游文档未跟进 | R2 grep gate ✅ | S3-02 D5 propagation 滞后 4 处 / S3-03 D5+v3 6 处 | 8-15 min |

**Type-2 子类共性**：API 存在 ✅、grep 0 hit、**仅 PlayMode probe 可发现**；区别在行为契约假设的层次：
- (a) framework facade 整体行为（API 在此 use case 下不适用）
- (b) cross-method state 协议（method A set field X，method B 检 field Y，X≠Y）
- (c) single-method behavior contract（method 在异常路径下的行为假设错误）

### §V2-3. R3 PlayMode Probe MANDATORY（formalize from V1.0 implicit）

V1.0 §"Drift 类型分类" table 已隐含 R3 必须；V2.0 升级为 **正式 gate 规则**：

> **R3 PlayMode probe 是 mandatory readiness gate，不是 optional**。即使 R1 + R2 全 PASS、grep §1 0-hit、§2 ≥1-hit、static 实施完整，**未跑 R3 = readiness gate 不完整**。任何 new method / new field / new contract 落地必须经过对应 PlayMode spike 验证后才能 close story。

**关键证据（R3 grep-only 盲区）**：
- S3-02 v3：R1 ✅ R2 ✅ static 实施完整 → R3 PlayMode 跑出 6/6 spike CASE FAIL（cross-method state lifecycle 协议漏洞）
- S3-03 v3：R1 ✅ R2 ✅ → R3 跑出 P5 FAIL（framework behavior assumption 错误）

**R3 PlayMode probe 必备 case 类型**：
1. **业务 happy path**（success cache hit / first-boot 等）
2. **失败路径 fail-loud**（invalid input → error sender + state machine 进 Error）
3. **Cross-method state 协议**（method A set field + method B 检 field；如对称性不一致 → R3 暴露）
4. **Framework boundary behavior**（idempotency / silent-failure / exception path — 详 §V2-5 checklist）

**R3 spike 调度规则**（继承 Type-3 防御）：
- 当前活跃 spike 在 `GameApp.RegisterDevSpikes()` 显式注册；其他 spike 全部注释（Type-3 race 防御）
- 多 spike batch run 时必须 sequential 调度（DevBootstrap 当前并发 → V3 候选改 sequential）

### §V2-4. Propagation v2 — Lite Playbook with Single Source of Truth Priority

当 D-level architectural decision 影响多 story / ADR 时，必须在做完决策同 sprint 立刻跑 propagation。V2.0 加入 **scope 选择优先级**（V1.0 candidate #1/#2/#3 的形式化 + post-S3-03 lite propagation v2 实战 refinement）：

#### 触发条件
- D-level decision 影响 **≥2 stories** OR **≥1 ADR section**
- Framework knowledge fact 发现（如 TEngine `RemoveEventListener` 非 idempotent）

#### Propagation scope 选择优先级 (V2.0 #6 refinement)

```
评估 propagation scope:
  ├── (a) Single source of truth ADR 文档可承载？
  │     YES + cross-cutting (≥10 stories impacted)
  │     └── 更新 ADR 单点 (highest ROI 73%-85%)
  │     例：S3-03 framework knowledge fact → ADR-027 §5 Lifecycle 协议表 (覆盖 17 listener stories)
  │
  ├── (b) Direct-impact Ready+ stories ≥5？
  │     YES → mass story propagation (5 min × N stories)
  │     例：S3-01 D5 → 12 active scene-management stories full update
  │
  └── (c) 全 historical references (已 DONE stories)？
        YES → 不修订 (historical wording acceptable)
        例：story-001 / story-004 / story-006 已 DONE 后 D5 stale references 不溯及
```

#### Lite propagation v2 playbook (10-15 min 范围)

| 步骤 | 时间 | 说明 |
|------|-----|-----|
| 1. Grep scan | ~3 min | 6 patterns × 全 backlog |
| 2. 命中评估 | ~3 min | 分类 active drift / historical / out-of-scope |
| 3. Propagation 路径选择 + 执行 | ~3-10 min | (a) 单点 ADR 更新 OR (b) mass story propagation |
| 4. 文档化 | ~2 min | propagation 数据点 + 触发条件 ROI |
| **小计** | **~11-18 min** | (实测 lite propagation v2 = 13 min) |

#### 实战 ROI 数据点

| Propagation 类型 | 时间 | 覆盖范围 | vs 替代方案 |
|----------------|------|---------|------------|
| S3-01 D5 propagation (V1.0 实战) | 35 min | 12 active stories + 2 历史档 + change-impact doc | 净 ROI +5 ~ +35 min vs 不做 propagation |
| Lite propagation v2 (post-S3-03) | 13 min | ADR-027 §5 单点 → 17 listener stories | **节省 72 min ROI 73%** vs mass story propagation 估 85 min |

### §V2-5. Framework Boundary Behavior Probe Checklist

R3 PlayMode probe 在 Type-2 子类 (c) 实战暴露后正式化为 checklist。**任何对 framework method 的调用模式如果依赖于 idempotency / silent-failure / exception path 假设，必须有对应 PlayMode spike 验证**：

| Boundary 行为类别 | 必备 spike case | 实战案例 / 待 future |
|----------------|---------------|-------------------|
| **Idempotency**（重复调用同样效果）| 调用 N 次 + assert 无 exception 抛出 + final state 一致 | S3-03 v3 TEngine `RemoveEventListener` 不 idempotent → 抛 "Delete handle failed" |
| **Silent failure**（错误路径返 false / null）| 错误路径触发 + assert framework 不抛 + 调用方按 false/null 检测处理 | S3-01 `GameModule.Resource.CheckLocationValid` scene 不适用 → 永返 false (Type-2(a)) |
| **Exception path**（特定输入抛特定 exception）| 边界输入触发 + assert exception type / message 匹配 + 调用方按 try-catch 处理 | (待 future stories — `LoadSceneAsync(invalid)` 等) |
| **Cancellation**（CancellationToken 中断）| 中途 cancel + assert cleanup 正确 / partial state 不残留 | (待 future stories — UniTask cancellation 路径) |
| **Over-limit**（输入超出 framework 上限）| 极值输入触发 + assert 行为符合预期 / 不崩溃 | (待 future stories — listener 数 / cache size 等) |

#### Checklist 加入 dev-story Phase 1.5 + R3 PlayMode spike 设计

设计 PlayMode spike 时，**对每个调用的 framework method 询问以上 5 个 boundary behavior**：
- 至少加 1 个对应 case
- 如 framework 行为已在 ADR-027（或同等枢纽 ADR）documented，引用即可不重测

#### Single source of truth 落地点

Framework boundary behavior facts 应该 propagate 到对应 framework ADR：
- **TEngine GameEvent**: ADR-027 §5 Lifecycle 协议表（已落 ⚠️ Framework knowledge fact section, 2026-04-30 dusk lite propagation v2）
- **TEngine GameModule.Scene**: ADR-009 (待 future story 实战触发)
- **YooAsset**: ADR-005 (待 future story 实战触发)

### §V2-6. Trigger Conditions Formalized — 7 触发条件

合并 V1.0 §"失败信号触发 ADR-029 V2" 4 项 + Sprint 3 实战暴露 3 项 = **7 项 V2.0 mandatory trigger**：

| # | 触发条件 | 触发动作 | 数据点案例 | V1.0 / V2.0 |
|---|---------|---------|----------|------------|
| 1 | D-level decision 影响 ≥2 stories OR ≥1 ADR section | 强制 propagation（同 sprint 内）| S3-01 D5 → S3-02 / S3-03 propagation lag | V1.0 |
| 2 | Framework knowledge fact 发现（API 行为契约假设错误）| 单点 ADR 更新（single source of truth priority）| S3-03 v3 TEngine `RemoveEventListener` → ADR-027 §5 update | **V2.0 new** |
| 3 | R1 + R2 PASS 但 PlayMode spike FAIL | R3 mandatory（不可跳）+ 文档化 Type-2 子类 | S3-02 v3 (b) / S3-03 v3 (c) | **V2.0 new** |
| 4 | New method / new field / new contract 落地 | 必备 PlayMode spike + boundary behavior checklist (§V2-5) | S3-02 `BeginTransitionAsync` / S3-03 `IFadeOverlay` | **V2.0 new** |
| 5 | Multi-spike 共享 framework 全局状态 | Sequential 调度 OR 单 spike 隔离 | S3-01 SP-011 + S3-01 spike race | V1.0 |
| 6 | Architecture decision 落地未 propagate 到 Ready+ backlog | Lite propagation 扫描 + 评估 single source of truth path | S3-03 propagation lag → V2 #6 refinement | **V2.0 new** |
| 7 | Sprint 平均 §Impl Notes drift 修订耗时 > 1 min | V3 升级 grep gate 自动化（fail vs flag）| Sprint 3 数据：~75 min cumulative drift handling 时间 / 4 stories ~ 19 min/story (含 R3 必跑) | V1.0 |

**V2.0 触发即时执行原则**：每个 sprint 末执行 retrospective 时跑全 7 项 trigger check；任何命中 → 当 sprint 内立即响应（不延后）。

### §V2-7. Drift Revision Time Tracking Metrics — Sprint 3 Verification

#### Sprint 3 Must Have 4/4 完整数据集（6 数据点验证 V2 设的实战必要性）

| # | Story / 阶段 | 时间 | Drift type | 节省 vs alt |
|---|-------------|------|----------|------------|
| 1 | S3-01 baseline | 18 min | Type-1 + Type-2(a) + Type-3 | — |
| 2 | S3-02 Phase 1.5 | 8 min | Type-4 only (D5 propagation cover) | ~10 min |
| 3 | S3-02 R3 PlayMode | 21 min | Type-2 **(b)** cross-method state | (R3-only 必跑) |
| 4 | S3-03 Phase 1.5 | 15 min | Type-4×6 + Type-1×2 | — |
| 5 | S3-03 R3 PlayMode | 12 min | Type-2 **(c)** framework behavior | (R3-only 必跑) |
| 6 | Lite propagation v2 | 13 min | (meta — V2 #6 refinement) | **~72 min** |

#### Sprint 3 总效率 verdict

| 维度 | 数据 |
|------|-----|
| Must Have 总投入 | ~140 min |
| Stories static + R3 | ~89 min |
| Drift handling | ~52 min（覆盖 6 数据点 + lite propagation v2）|
| Avoided unsafe deploys | **×2**（S3-02 + S3-03 R3 都暴露了 grep-only 看不见的 drift）|
| ADR-029 governance ROI | **正向 + 实战可量化** |

#### V3 升级触发条件（V2 → V3）

| 信号 | 升 V3 动作 |
|-----|----------|
| Sprint 4 平均 Type-1 drift > 5 min | V3 加自动化 grep gate fail（V1.0 Alternative 3 复活）|
| New drift type 出现（非 Type-1/2/3/4）| V3 加 Type-5 + 分类 |
| Type-2 (c) framework behavior 发现频率 > 2/sprint | V3 强化 framework boundary behavior probe automation（CI-level grep + spike runner）|
| Lite propagation v2 ROI < 50% | V3 修订 single source of truth priority（数据驱动调整阈值）|
| Multi-spike sequential 调度后仍 race | V3 引入 spike scheduler framework（DevBootstrap sequential runner replacing concurrent default）|
| **VS-skip path Sprint 6 gate-check rerun rate < 100%** (per ADR-030) | V3 修订 VS-late pattern 推荐 timeline（Sprint 5-6 → Sprint 5-7 buffer）；监控 fun loop validation 数据；如多次延期 → 触发 VS-late pattern 反思 |

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    Story Authoring Lifecycle                    │
└─────────────────────────────────────────────────────────────────┘

   /create-stories              /story-readiness            /dev-story
   ───────────────              ────────────────            ──────────
   Phase 1 加 R1/R2/R3       新增 ADR-029 verdict       5min checklist
   生成时实证 grep             PASS / DEFICIENCY /         (升级 ADR-blessed)
                              NEEDS-WORK
        │                            │                          │
        │                            │                          │
        ▼                            ▼                          ▼
   Story 文件                    Verdict 报告              实施起手前
   §Engine Notes 已标 PASS       具体 drift 项 + 建议      最后一道 gate
   或 Pending Verification

   ┌─────────────────────┐
   │ Drift 检测点（R1/R2/R3）│
   ├─────────────────────┤
   │ R1: per-event 注册  │ ── grep AddEventListener<TArg>(eventType, handler)
   │ R2: 跨组件 API      │ ── grep public .* MethodName\(
   │ R3: Stub 类型构造   │ ── grep public ClassName\(
   └─────────────────────┘
                │
                ▼
       ┌────────────────────────────┐
       │  drift 修订耗时 metric     │
       │  (sprint-status.yaml)      │
       │  Sprint 3 目标 ≤ 1min      │
       └────────────────────────────┘
```

---

## Alternatives Considered

### Alternative 1: 不形式化 — 继续依赖 lessons memo + 5min readiness checklist 隐性约束

- **Description**: 保留 `/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md` 作为 lessons memo，依赖 dev-story 自觉走 checklist
- **Pros**: 零 governance 成本；现有 5min checklist 已 4 次抓住所有 drift
- **Cons**: drift 修订耗时仍稳态存在（5-15min/story）；新成员（人或 AI agent）onboard 时无 ADR 可引；lessons memo 是隐式知识，治理空白未补
- **Rejection Reason**: 4 次反复证明问题模式稳定 + 解决方案稳态触达 = 该升级为显式 process gate；lessons memo 仅是问题记录，不是流程约束

### Alternative 2: 完全 LLM 自我审查（生成时强制 self-check 不依赖 grep）

- **Description**: `/create-stories` skill 在生成 §Implementation Notes 后立即做 LLM self-review pass：调 sub-agent 复审"这段代码能编译吗"
- **Pros**: 不依赖 grep 工具；LLM 推理可处理更复杂的 API 一致性
- **Cons**: LLM self-review 准确度依赖训练数据 + 上下文；4 次 drift 中 3 次是 LLM 自己产生的幻想 API → self-review 可能复用相同幻想；不可度量；不可审计
- **Rejection Reason**: grep 是事实陈述（API 存在 / 不存在）零幻想；LLM self-review 在事实陈述上不增益反引入新风险

### Alternative 3: 完全自动化（grep 强制 fail story 生成）

- **Description**: `/create-stories` 与 `/story-readiness` 在 R1/R2/R3 任一 0 命中时**强制 fail**，阻塞 story 生成
- **Pros**: 100% gate 不漏；drift 修订耗时归零（不可能进 dev-story 阶段）
- **Cons**: 太刚性 —— framework 缺 API 时（合理需求）story 也无法生成；阻塞 sprint；不允许 deficiency flag 路径
- **Rejection Reason**: 选 §3 中 R3 的折中方案 —— 0 命中 + deficiency flag = 允许；0 命中 + 无 flag = 阻塞。保持 process 灵活性同时强制 acknowledge

### Alternative 4: 工具自动化（CI gate / git hook）

- **Description**: 将 R1/R2/R3 写为 CI/git hook，commit 阶段强制检查
- **Pros**: 不可绕过；机器执行
- **Cons**: 与 `/create-stories` + `/dev-story` skill 路径错位（skill 阶段是 story 文件 markdown，CI 阶段已是代码 commit）；过度工程化
- **Rejection Reason**: skill-level 集成已足够（4 次复用 5min checklist 100% 命中证明）；CI gate 可作为 future 增强（Sprint 4 可考虑）

---

## Consequences

### Positive

- **drift 修订时间预期归零**：Sprint 3 起 dev-story 起手 R1/R2/R3 通过 = 不再消耗 5-15min 重设计；Sprint 2 隐性成本（4 次 ×10min ≈ 40min/sprint）回收
- **新成员 onboarding 路径清晰**：ADR-029 + R1/R2/R3 grep 模板可直接引用；不依赖资深成员传授 lessons memo 隐性知识
- **ADR 公信力提升**：`/create-stories` 输出的 story §Impl Notes 经实证 → "看 ADR 能跑"
- **Cross-project 适用性**：R1/R2/R3 模式可移植到任何"配套有 source generator + facade"的事件系统（Unity Messaging / UE Delegate / Godot Signal）
- **可度量**：drift 修订耗时 metric 明确，Sprint 3 末可 verdict 形式化效果
- **跨 skill 治理统一**：3 个 skill（`/create-stories` + `/story-readiness` + `/dev-story`）同步加 R1/R2/R3 强制点 → 治理无 gap

### Negative

- **`/create-stories` 生成时 + `/story-readiness` 验证时 + `/dev-story` 实施时三处都要跑 grep**：执行成本累加 ~3 ×（30s-2min）；但与 5-15min/story drift 修订成本相比仍净节省
- **Skill 文档同步成本**：3 个 skill 都要更新（约 2hr 一次性）；但 sprint 3 就有回报
- **deficiency flag 路径需要纪律**：framework 缺 API 时 dev-story 第一步必须先扩 framework；如团队偷懒不扩反而硬上，会留隐性技术债
- **ADR-029 自身需要 review**：本 ADR 是"治理 governance 的 governance"，二阶元工程；team 文化需吸收

### Risks

- **Risk 1**: R1/R2/R3 grep 模板覆盖不全，新 drift 模式从盲区进入
  - **Mitigation**: Sprint 3 全程跟踪 `§Impl Notes drift 修订耗时` metric，>1min 即审视；新 drift 模式发现立即立 R4/R5 续延
- **Risk 2**: deficiency flag 路径被滥用（团队习惯 flag + 后置）
  - **Mitigation**: `/dev-story` 起手第一步若有 deficiency flag，强制实施 framework 扩展前阻断后续步骤；不允许"flag 完忘了扩"
- **Risk 3**: skill 文档跨 skill 同步漂移（`/create-stories` 改了但 `/dev-story` 未改）
  - **Mitigation**: 本 ADR 作为 single-source-of-truth；3 个 skill 文档相关章节都引用 ADR-029 §4，避免重复定义
- **Risk 4**: ADR-029 V1 不足以覆盖未来其他类 drift（如 ADR 与 GDD 冲突）
  - **Mitigation**: 本 ADR 仅覆盖 §Implementation Notes drift；GDD/ADR 冲突类 drift 由 `/architecture-review` skill 覆盖（独立机制）

---

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| N/A — process-level decision | N/A | 本 ADR 不直接对应 GDD requirement；它解决的是 GDD → ADR → Story → Code 链路中"Story §Implementation Notes 与 Framework 实际能力 drift"的治理空白。所有 GDD 间接受益（GDD 描述的接口在 story 实施时不再臆测）|

---

## Performance Implications

- **CPU**: None — process-level decision，零运行时成本
- **Memory**: None
- **Load Time**: None
- **Network**: N/A
- **Process Cost**:
  - `/create-stories` 生成时 +30s-2min（R1/R2/R3 grep）
  - `/story-readiness` 验证时 +30s-2min（同上）
  - `/dev-story` 起手 +5min（5min checklist；与原 checklist 等量）
  - **Net savings**: 5-15min/story drift 修订耗时回收（Sprint 2 baseline 4 次 ≈ 40min）

---

## Migration Plan

### Sprint 3 启动同步实施（已规划为 S3-04 governance 任务）

1. **本 ADR 写入 + Accepted**：`docs/architecture/adr-029-story-impl-notes-verification.md` ✅ 完成 2026-04-30
2. **更新 `/create-stories` skill 文档**：Phase 1 加 R1/R2/R3 强制检查项；引用 ADR-029 §1/§2/§3 模板 ✅ 完成 2026-04-30（新增 §4a "ADR-029 Implementation Notes Verification (BLOCKING)" 章节，包含 R1/R2/R3 grep 模板与 PASS/DEFICIENCY-FLAGGED/NEEDS-WORK 状态表）
3. **更新 `/story-readiness` skill 文档**：verdict 维度加 `ADR-029 Verification: PASS / DEFICIENCY-FLAGGED / NEEDS-WORK` ✅ 完成 2026-04-30（§3 加 "ADR-029 §Implementation Notes Verification" 子章节作为独立 verdict 维度；§4 main verdict 加 ADR-029 gate 强制；§5 单 story 输出格式加 ADR-029 verdict 行）
4. **更新 `/dev-story` skill 文档**：5min readiness checklist 升级为 ADR-blessed gate；引用 ADR-029 §4 ✅ 完成 2026-04-30（新增 Phase 1.5 "ADR-029 5-Minute Readiness Self-Check (BLOCKING)"，PASS/DEFICIENCY-FLAGGED-PROCEED/STOP 三态 verdict）
5. **更新 `production/sprint-status.yaml` 模板**：`notes` 字段加 §Impl Notes drift 修订耗时 metric 槽位 ⏳ Sprint 3 各 story 实施时记录
6. **lessons memo 标 superseded**：`/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md` 头部加 `**Status: Superseded by ADR-029 (2026-04-30)**`，作为历史记录保留 ✅ 完成 2026-04-30

### Sprint 2 已实施 stories 不溯及

S2-09/-12/-13/-11 已通过 5min readiness checklist 抓住所有 drift（Sprint 2 retrospective 已记），不需要回头验证。Sprint 3 carryover stories（S3-01/-02/-03/-05）在 dev-story 起手时按 R1/R2/R3 复审一次（属正常 dev-story 流程，无额外成本）。

---

## Validation Criteria

### Sprint 3 末（2026-05-15 +/- 几天）formal review

- [ ] **Sprint 3 平均 §Impl Notes drift 修订耗时 ≤ 1min**（**Type-1 only** — fantasy API；R1/R2/R3 grep gate 设计针对此类）— 通过 sprint-status.yaml notes 字段 metric 计算（succ）
- [ ] **新 drift 模式 0 次** — Sprint 3 dev-story 起手 R1/R2/R3 grep 0 命中以外的 drift（succ）
- [x] **`/create-stories` + `/story-readiness` + `/dev-story` 三 skill 文档已加 R1/R2/R3 强制点** — 文档 audit ✅ 完成 2026-04-30
- [ ] **Sprint 3 全部 stories 经过 R1/R2/R3 readiness verdict 显式标记** — sprint-status.yaml audit（succ）
- [ ] **如 deficiency flag 触发**：framework 扩展在 dev-story 第一步实施 + sprint-status.yaml 记录该扩展（succ）

### Drift 类型分类（S3-01 实战首测发现 — 2026-04-30）

ADR-029 R1/R2/R3 grep gate 形式化只能 cover **Type-1**（"API 不存在/签名错"是 grep 可达问题）。S3-01 实战暴露了另两类 drift，**必须靠 PlayMode runtime 验证**才能发现，三层防御链不可单独依靠任何一层：

| Type | 描述 | 检测层 | S3-01 实战案例 | 修订耗时 | V2 防御补丁建议 |
|------|------|-------|---------------|---------|----------------|
| **Type-1** | API 不存在 / 签名错 / 返回类型幻想 | ADR-029 R1/R2/R3 grep gate（已实现）| `GameModule.Resource.LoadSceneAsync` (实际是 Scene)；`SceneHandle` (实际 Unity Scene)；`OnDownloadProgressCallback` 4-arg (实际 `DownloadUpdateCallback` 单 `DownloadUpdateData` arg) — 6 处 fantasy API | 10 min | R1/R2/R3 已 cover；目标 ≤5min |
| **Type-2** | API 存在但行为/上下文不适用（grep 抓不到）| **PlayMode spike runtime ONLY** | (a) `GameModule.Resource.CheckLocationValid("SceneName", "DefaultPackage")` 对 YooAsset scene 资产一律返 false（scene/asset location key 体系不同）— S3-01；(b) `LoadChapterSceneAsync` 仅 set `_currentChapterSceneName`，`_currentChapterId` 仅由 `OnRequestSceneChange` set；`UnloadCurrentChapterAsync` 用前者守卫但前者读不到 → spike 直调路径永 silent return（cross-method state lifecycle 协议不一致）— S3-02 v3；**(c) Framework behavior assumption drift**：`GameEvent.RemoveEventListener` (TEngine EventMgr) **不是** idempotent silent no-op，listener 不存在时抛 Exception "Delete handle failed, not exist"；patch v2 AC-9 假设错误；recommended pattern = handler null-out + 外部 cleanup null-check guard — S3-03 v3 | 5 min (S3-01) / 15 min (S3-02 v3) / **12 min (S3-03 v3)** | 加 control manifest rule："framework facade API 行为差异 + cross-method state lifecycle 协议一致性 + framework method 单点行为契约 (idempotency / silent-failure / exception path) 全需 PlayMode spike 验证后定型；不能仅靠 grep"；**R3 PlayMode probe 是 mandatory（不是 optional）**——即使 R1/R2 全 PASS，未跑 R3 = readiness gate 不完整；**V2 #7 (new — S3-03 v3)**：R3 必须明确包括 framework boundary behavior 测试（listener-not-exists / null-state / over-limit / cancellation 等异常路径），不只是业务 cross-method state 协议 |
| **Type-3** | Test infrastructure race（多 spike 共享 framework 全局状态）| **PlayMode runtime + 多 spike 同启动场景** | SP-011 + S3-01 spike 并发 RunAllAsync 同帧调 `LoadSceneAsync(SP011_SceneA)` 撞 YooAsset "while loading" 锁 | 3 min | 加 control manifest rule："多 spike 共享 framework 全局状态时必须 sequential 调度；或单 spike 隔离原则（默认禁多并发，需要 batch run 时显式 opt-in）" |
| **Type-4 (new 2026-04-30 — S3-02 propagation 实战发现)** | Architecture decision propagation drift — 决策已生效但下游文档未跟进（不是 LLM 出 drift，是文档书写时序在决策之前）| Phase 1.5 R2 grep gate 仍可抓住（按 fantasy API pattern）；但**根因**是上游决策传播缺失而非 story author drift | (a) S3-02 4 处 fantasy API 全是 S3-01 D5=[X] 决策 stale；(b) S3-03 6 处 fantasy ref 是 S3-01 D5 + S3-02 v3 双重 stale（story-005 写于 2026-04-22）| 8 min（S3-02）/ ~15 min（S3-03） | **V2 触发条件 #1**：当 D-level 决策影响 ≥2 stories / ≥1 ADR section 时，做完决策同 sprint 立刻跑 `/propagate-design-change` (lite)；**V2 触发条件 #6 (new — S3-03 验证 + post-S3-03 lite propagation v2 实战 refinement)**：propagation scope **必须覆盖所有 `Status: Ready` 及以上的 stories**（active sprint + ready backlog），不仅 active sprint。**Refinement (post-S3-03 lite propagation v2)**：propagation scope 选择优先级 = **(a) 优先 single source of truth — 受影响的 ADR 文档本身（如 ADR-027 §5 lifecycle 协议表加 framework knowledge fact）**；**(b) 直接受影响的 Ready+ stories（如 D5 涉及 12 active scene-management stories）**。当 framework knowledge fact 是横切性的（cross-cutting，影响多 epic 多 story），选 (a) 更高 ROI 73%（lite propagation v2 实测 11 min vs mass story propagation 估 85 min）；当 architectural decision 是 narrow-scoped 的（限定 epic 内具体 method/field），选 (b) 直接 propagate stories；预期 Type-4 修订时间降到 ≤2 min/story（fields-only replace） |

**累计 drift revision 数据点**：
- S3-01 = 18 min（Type-1 10min + Type-2 5min + Type-3 3min；baseline first run）
- S3-02 (Phase 1.5) = 8 min (Type-4 only；S3-01 D5 propagation 滞后；~55% 节省)
- D5 propagation 实战 = ~35 min (12 文件全量 + 2 历史档 superseded + change-impact doc) → 后续 propagation 应更快无 §Update note 撰写 overhead
- **S3-02 (R3 PlayMode runtime) = 21 min**（Type-2 (b) cross-method state protocol drift；首跑暴露 6 min + 决策矩阵 4min + fix + doc 11min；第 3 数据点 — 验证 R3 不可跳）
- **S3-03 (Phase 1.5) = ~15 min**（第 4 数据点；8 处 drift 横跨 6 Type-4 propagation 滞后 + 2 Type-1 fantasy（fade）+ 测试路径 pivot；vs S3-02 Phase 1.5 慢 ~2x，root cause = D5 propagation scope 仅含 12 active sprint files，未覆盖 ready backlog story-005）
- **S3-03 (R3 PlayMode runtime) = ~12 min**（第 5 数据点；Type-2 子类 (c) Framework behavior assumption drift — TEngine `GameEvent.RemoveEventListener` 不是 idempotent，patch v2 AC-9 假设错误；spike P5 拆 P5a + P5b + AC reformulate + Engine Notes framework fact；vs S3-02 R3 (~21min) 快 ~9min 因 drift scope 单 case + production code 不变）
- **Lite propagation v2 (post-S3-03 done) = ~11 min**（V2 #6 + #7 教训应用；扫描 91 stories 后单点更新 ADR-027 §5 framework knowledge fact 覆盖 17 个 listener pattern stories；vs mass story propagation ~85 min → 节省 ~74 min ROI 73% ✅；**V2 #6 refinement**：propagation scope = (a) direct-impact Ready+ stories + (b) relevant ADR single source of truth — 选 (b) when 横切性更高）

**净 ROI（propagation 实战首测）**：propagation cost 35 min vs avoided downstream drift 40-70 min ⇒ +5 ~ +35 min positive；首次已 break-even。

**R3 PlayMode probe 不可跳的关键证据（S3-02 v3）**：R1 ✅ R2 ✅ 全过、grep 8 项 0-hit、4 项 ≥1-hit、static 实施完成 — 但 R3 跑出来 6/6 cases FAIL。Root cause = `LoadChapterSceneAsync` set 一个字段、`UnloadCurrentChapterAsync` 检另一个字段，两字段语义不对称 → cross-method state lifecycle 协议漏洞。**这是 grep-only readiness 永远抓不到的盲区**。

后续 stories 单条 Type-1 预期 ≤5min；Type-2 (cross-method) 待 R3 mandatory 入 V2 后预期发现率 100%（修订时间不可控但暴露不会漏）；Type-3 待 control manifest rule 补丁后预期 0 复发；**Type-4 预期 0 复发**（在做完 D-level 决策同 sprint 跑 propagation 后）。

### 失败信号触发 ADR-029 V2

- Sprint 3 平均 Type-1 drift 修订耗时 > 5min → R1/R2/R3 在 `/create-stories` 阶段未抓住 → V2 加自动化 grep fail（Alternative 3）
- Sprint 3 中出现 R1/R2/R3 未覆盖的新 drift 模式 → V2 加 R4/R5
- Type-2/-3 control manifest rule 加完后仍 ≥ 2 次复发 → V2 强化 PlayMode spike 调度框架（DevBootstrap sequential runner + framework 行为契约 spike batch）
- deficiency flag 滥用（>3 次 flag 完未扩 framework）→ V2 加强 deficiency flag 强制流程

---

## Related Decisions

- **ADR-027** GameEvent Interface Protocol — 定义 per-event listener 模式（本 ADR R1 据此形式化）
- **ADR-028** TEngine Module Usage Policy — 同 governance 类 ADR；本 ADR 与之并列（一个管模块边界，一个管 story authoring）
- **`/.claude/memory/problem_2026-04-29_story-impl-notes-vs-framework-drift.md`** — lessons memo 4 次复用稳态触达本 ADR 起草

---

## History

- **2026-04-29 night**: lessons memo 立（S2-12 LockManager drift surface）
- **2026-04-29 night**: S2-13 InteractionCoordinator 复用 memo（第 2 次反复）
- **2026-04-30 morning**: S2-11 Rotation 复用 memo（第 3-4 次反复）
- **2026-04-30 morning**: Sprint 2 retrospective 触发 ADR-029 起草决策
- **2026-04-30 morning**: 本 ADR 写入 + Accepted（即时 process-gate 启用，Sprint 3 起跟踪 metric）
- **2026-04-30 afternoon**: S3-01 Additive Scene Loading 实战首测 — Phase 1.5 抓 6 处 fantasy API（Type-1 10min）；PlayMode spike 暴露 Type-2 (CheckLocationValid scene 不适用 5min) + Type-3 (spike 并发 race 3min)；首条 baseline 数据点 18 min；ADR 更新加 Drift 类型分类 + V2 触发条件细化
- **2026-04-30 afternoon (continued)**: S3-02 Mandatory Cleanup Sequence readiness check — R2 STOP 抓 4 处 type-1 fantasy API（全部 S3-01 D5 决策传播滞后）；patch v2 修订 ~8 min vs S3-01 baseline 18 min ~55% 节省（第 2 数据点验证 type-1 ≤ 5min 趋势）；触发 ADR-029 V2 改进 insight：当 architecture decision 影响多 story / ADR 时应在做完决策同 sprint 立刻跑 propagate-design-change；用户选 FULL propagation 全量 12 文件修订 + 2 历史档 superseded 注脚 + change-impact doc 留档（约 35 min）；net positive ROI 5-35 min（避免 S3-03/-04/-05 等下游每条 R2 STOP × 5-8 min + QA走偏 incident）；候选 ADR-029 V2 条目：(1) trigger condition 新增 D-level 决策影响 ≥2 stories 时强制 propagation, (2) Drift type 4 "Architecture decision propagation drift", (3) lite propagation playbook 子章节
- **2026-04-30 dusk (V2.0 起草 — Sprint 3 Must Have 4/4 闭环 + 6 数据点全部触发 V2 candidates)**: 升级 ADR-029 V1.0 → V2.0 (backward-compatible major revision)。新增 §Decision V2.0 7 sections：(§V2-1) R1/R2/R3 grep gate 继承 V1.0；(§V2-2) Drift Types V2 — Type-2 拆 (a)/(b)/(c) 子类共 7 子型态；(§V2-3) R3 PlayMode probe MANDATORY 正式化；(§V2-4) Propagation v2 with Single source of truth priority + lite playbook 10-15 min 范围；(§V2-5) Framework Boundary Behavior Probe Checklist (idempotency / silent-failure / exception path / cancellation / over-limit 5 类)；(§V2-6) 7 trigger conditions formalized (V1.0 4 项 + V2.0 new 3 项)；(§V2-7) Drift revision time tracking metrics — Sprint 3 6 数据点 + Sprint 3 总效率 verdict + V3 升级触发条件 5 项。**V2.0 起草投入** ~30 min（结构 5min + V2-1/2/3 20 min + V2-4/5/6/7 5 min；下次 V3 估 ~15 min 因结构定型）。**V1.0 → V2.0 升级影响**：downstream stories 引用 ADR-029 不需变更（仍引 ADR-029，自动获得 V2.0）；ADR-027 已通过 lite propagation v2 加 framework knowledge fact（与 V2.0 §V2-5 中 single source of truth 落地点对齐）。Status: Accepted V2.0 (2026-04-30 dusk)。
- **2026-04-30 dusk (continued #4 — V2 propagation 实战 ROI 验证)**: 应用 V2 #6 + #7 教训对全部 91 backlog/ready stories 做 lite scan（~3 min）。**结果**：(1) 6 patterns 扫描全部 0 hit on backlog stories（除 story-005 自身已修订）— D5/S3-02v3 propagation 在已 DONE stories 是 historical references，无 active drift；(2) Sprint 3 Should/Nice (S3-05/06/07/08/09) 不涉及 SceneManager API，无需修订；(3) ADR-027 §5 Lifecycle 协议表加 ⚠️ Framework knowledge fact + Required null-out + null-check guard pattern + Forbidden raw double-remove anti-patterns — **单点 ADR 更新覆盖 17 个引用 ADR-027 的 stories**（含 chapter-state / object-interaction / tutorial-onboarding / hint-system / urp-shadow-rendering listener 自移除 patterns）。**关键 V2 #6 refinement**：propagation scope rule 不是简单 "propagate to ALL Ready+ backlog"，而是 **"propagate to (a) direct-impact Ready+ stories + (b) relevant ADR single source of truth"** — 选择 (b) 更高 ROI 当 framework knowledge fact 是横切性的（cross-cutting）。本次 propagation 投入 ~11 min（扫描 3 + 评估 3 + ADR-027 更新 3 + 文档化 2），覆盖范围 = ADR-027 + 17 stories listener pattern；vs mass story propagation 估约 ~85 min (5min × 17 stories) → 节省 ~74 min ✅ ROI 73%。
- **2026-04-30 dusk (continued #3)**: S3-03 PlayMode 二跑 CORE 5/5 PASSED (allCorePassed=true; lastError empty) — patch v3 Type-2 (c) Framework behavior assumption fix 验证有效；P5b `TestSceneScopedFixture` documented null-out + null-check guard pattern 完全无 TEngine exception 抛出。/story-done 闭环。**Sprint 3 Must Have 4/4 ✅** (S3-04 + S3-01 + S3-02 + S3-03)；ADR-029 5 数据点齐 + 7 V2 candidates 全部有实战触发数据；V2 起草数据充分（5 ≥ 3 起草最低门槛）。**ADR-029 Sprint 3 验证设结论**：(1) Phase 1.5 R1+R2 grep gate 节省 ~50% Type-1 fantasy + Type-4 propagation drift 修订时间（vs S3-01 baseline）；(2) R3 PlayMode probe 暴露 100% Type-2 (b/c) cross-method protocol drift（无遗漏）；(3) propagation 实战 ROI 仅在 V2 #6 scope rule 落地后才能完全节省下游 Type-4（当前漏 ready backlog）；(4) framework behavior assumption drift Type-2 (c) 是 R3-only 可发现的新形态，仅 PlayMode probe 可暴露 — 验证 ADR-029 V2 #4 (R3 mandatory) + #7 (framework boundary behavior probe) 必要性。
- **2026-04-30 dusk (continued #2)**: S3-03 PlayMode 首跑 5/6 PASS — P1 / P2 ADV / P3 / P4 / P6 ADV 全 PASS（v3 fix `_currentLoadedChapterId` + `BeginTransitionAsync` 11 步骨架 + 2 sender 派发顺序全部按预期）；但 **P5 FAIL** — `lastError: "P5 double-remove threw: Delete handle failed, not exist"`。**Root cause**：patch v2 AC-9 假设 TEngine `GameEvent.RemoveEventListener` 是 idempotent，实测 TEngine 抛 Exception。归 **Type-2 cross-method protocol drift 子类 (c) — Framework behavior assumption drift**：API 存在 ✅、单 method 行为契约假设错误 ❌；grep 0 hit 不可发现，仅 R3 PlayMode probe 可暴露；**第 5 数据点**。patch v3 修订（~12 min）：(1) S303 spike 拆 P5 → P5a (self-removal) + P5b (NullOutGuardPattern w/ TestSceneScopedFixture)；(2) story-005 AC-9 reformulate；(3) Engine Notes 加 framework knowledge fact；(4) Control Manifest 加 Required null-out + null-check guard / Forbidden raw double-remove；(5) production code 不变（drift 仅影响 spike 形态 + AC 措辞）。**ADR-029 V2 候选 #7 触发**：R3 PlayMode probe 必须明确包括 framework boundary behavior 测试（listener-not-exists / null-state / over-limit / cancellation 等异常路径），不只是业务 cross-method state 协议；**预测 ROI**：下游 ADR-027 listener pattern stories 在 dev-story 阶段提前发现 framework 行为契约假设，避免 R3 后再修订（节省 ~10-15 min/story）。
- **2026-04-30 dusk (continued #1)**: S3-03 / story-005-scene-events readiness check (post-S3-02 done) — R2 STOP 抓 8 处 drift（6 Type-4 propagation 滞后 + 2 Type-1 fantasy(fade infra) + 测试路径 pivot）；patch v2 修订估约 ~15 min（**第 4 数据点**）；vs S3-02 Phase 1.5 慢 ~2x，root cause = S3-01 D5 propagation 实战首测 scope 仅含 12 active sprint files (S3-04/-01/-02/...)，未覆盖 ready backlog story-005；**触发 ADR-029 V2 候选 #6** — `propagation scope` rule：必须覆盖所有 `Status: Ready` 及以上的 stories（active sprint + ready backlog），D-level 决策落地后立即扫修订；本 story patch v2 同步引入 **IFadeOverlay placeholder + NoOpFadeOverlay 默认 impl + RegisterFadeOverlay setter** （选 [b] decision），保持 BeginTransitionAsync 11 步骨架完整 + 不引入 future fade story 反向依赖。
- **2026-04-30 dusk**: S3-02 PlayMode runtime 首跑暴露 **Type-2 cross-method protocol drift**（第 3 数据点）— R1 ✅ R2 ✅ Phase 1.5 全过、grep §1 0-hit / §2 4 项 ≥1-hit、static 实施完整，但 PlayMode S302 spike 6/6 cases FAIL：root cause = `LoadChapterSceneAsync` 仅 set `_currentChapterSceneName`，`_currentChapterId` 仅由 `OnRequestSceneChange` set；`UnloadCurrentChapterAsync` 用前者守卫但前者在 spike 直调路径永 = NoChapterId → silent return；scene 永不被卸；listener 永不 fire；同时暴露生产 driver 路径 sender 派错 chapter id 的 bug。v3 修复（SceneManager.cs）：引入 `_currentLoadedChapterId` 配对字段（与 sceneName atomic 配对作为已加载身份单一事实源），LoadChapterSceneAsync 成功路径同步 set，ClearCurrentChapterSceneName 同步 reset，UnloadCurrentChapterAsync guard 与 sender 都用新字段。R3 cycle 时间 21 min（首跑 6min + 决策矩阵 4min + fix + doc 11min）。**关键 insight**：R1/R2 grep gate 永远抓不到 cross-method state lifecycle 协议不一致；ADR-029 V2 必须把 R3 PlayMode probe 列为 mandatory（非 optional）。Type-2 (cross-method) 加入 ADR-029 V2 候选 trigger condition：任何新建 SceneManager-scope public method 必须 PlayMode spike 验证后才算 readiness PASS。
