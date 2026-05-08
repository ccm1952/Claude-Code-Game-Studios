// 该文件由Cursor 自动生成

# Grep Evidence: Cleanup Sequence — No Fantasy API Drift (S3-02 patch v2)

> **Date**: 2026-04-30 afternoon
> **Story**: S3-02 Mandatory Cleanup Sequence
> **Trigger**: ADR-029 Phase 1.5 R2 STOP — 4 处 type-1 fantasy API drift（全部 S3-01 D5 决策传播滞后；属 ADR-029 V2 候选 Drift Type-4 "Architecture decision propagation drift"）
> **Patch v2 commit**: 2026-04-30 — full alignment to S3-01 D5 decision (sceneName string + GameModule.Scene wrapper) + Test Evidence pivot to PlayMode spike (D1=[b] inherited)
> **此文档目的**：作为 ADR-029 第 2 数据点（vs S3-01 baseline 18 min）的 grep evidence；同时验证 S3-01 D5 全量 propagation 实战首测有效性（control-manifest + 2 ADR + GDD 修订后 framework correct usage pattern 是否在 active 代码层落地）

---

## §1 8 项 forbidden / fantasy API — 0 hit 验证

均针对 scope `Assets/GameScripts/HotFix/GameLogic/`（HotFix 业务代码层；TEngine Runtime / SP-011 历史档不算）。

| # | Pattern | 含义 | Scope | Result |
|---|---------|------|-------|--------|
| 1 | `_currentSceneHandle` | S3-01 D5=[X] 决定不缓存 SceneHandle；该字段已替换为 `_currentChapterSceneName: string` | `Scene/` | ✅ **0 hit** |
| 2 | `GameModule\.Resource\.UnloadSceneAsync` | Fantasy API（实际在 `GameModule.Scene.UnloadAsync(string)`）| `GameLogic/` | ✅ **0 hit** |
| 3 | `GameModule\.Resource\.LoadSceneAsync` | Fantasy API（实际在 `GameModule.Scene.LoadSceneAsync`）| `GameLogic/` | ✅ **0 hit** |
| 4 | `SceneHandle` | YooAsset 原生类型残留（framework wrapper 不暴露给业务层）| `GameLogic/` | ✅ **0 hit** |
| 5 | `LoadSceneMode\.Single` | ADR-009 禁用模式（章节场景必须 Additive）| `GameLogic/` | ✅ **0 hit** |
| 6 | `private\s+Scene\s+_` | 缓存 Unity Scene struct 字段（D5=[X] 禁，aliasing 风险）| `Scene/` | ✅ **0 hit** |
| 7 | `EventId\.Evt_` | ADR-027 §1 已废弃的整数事件常量模式 | `Scene/` | ✅ **0 hit** |
| 8 | `Resources\.UnloadAsset\(` | Asset 释放走 `AssetHandle.Release()`（ADR-005），不直调 Unity 原生 | `GameLogic/` | ✅ **0 hit** |

### 核验命令

```bash
rg "_currentSceneHandle" Assets/GameScripts/HotFix/GameLogic/Scene/
rg "GameModule\.Resource\.UnloadSceneAsync" Assets/GameScripts/HotFix/GameLogic/
rg "GameModule\.Resource\.LoadSceneAsync" Assets/GameScripts/HotFix/GameLogic/
rg "SceneHandle" Assets/GameScripts/HotFix/GameLogic/
rg "LoadSceneMode\.Single" Assets/GameScripts/HotFix/GameLogic/
rg "private\s+Scene\s+_" Assets/GameScripts/HotFix/GameLogic/Scene/
rg "EventId\.Evt_" Assets/GameScripts/HotFix/GameLogic/Scene/
rg "Resources\.UnloadAsset\(" Assets/GameScripts/HotFix/GameLogic/
```

8 项执行 → "No matches found"（实测于 2026-04-30 afternoon — pre-/dev-story 阶段已 0；S3-02 实施只新增 framework correct usage，不应引入 fantasy patterns）。

---

## §2 4 项 framework correct usage — ≥1 hit 期望（pre-/dev-story baseline + post-impl 校验）

| # | Pattern | Scope | Pre-/dev-story baseline | Post-impl 期望 |
|---|---------|-------|------------------------|---------------|
| 1 | `GameModule\.Scene\.UnloadAsync` | `GameLogic/` | 6 hits（S301 spike:4 + SP011 spike:2）| ≥ 7 hits（+ S302_CleanupSequence.cs spike + SceneManager.UnloadCurrentChapterAsync()）|
| 2 | `_currentChapterSceneName \|\| ClearCurrentChapterSceneName` | `Scene/SceneManager.cs` | 5 hits（已含 S3-01 字段 + setter + testhook）| ≥ 6 hits（+ UnloadCurrentChapterAsync() 内部读 + setter call）|
| 3 | `Resources\.UnloadUnusedAssets\(\)` | `GameLogic/` | 2 hits（SP-011 P3 + SingletonSystem 历史）| ≥ 3 hits（+ SceneManager.UnloadCurrentChapterAsync()）|
| 4 | `ISceneEvent_Event\.OnSceneUnloadBegin` | `GameLogic/Scene/` | 0 hit（未实装）| ≥ 1 hit（SceneManager.UnloadCurrentChapterAsync() Step 5 派发；spike listener 可选）|

### 核验命令

```bash
rg "GameModule\.Scene\.UnloadAsync" Assets/GameScripts/HotFix/GameLogic/
rg "_currentChapterSceneName|ClearCurrentChapterSceneName" Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs
rg "Resources\.UnloadUnusedAssets\(\)" Assets/GameScripts/HotFix/GameLogic/
rg "ISceneEvent_Event\.OnSceneUnloadBegin" Assets/GameScripts/HotFix/GameLogic/Scene/
```

实测结果（pre-/dev-story 阶段 — 2026-04-30 afternoon）：
1. `GameModule.Scene.UnloadAsync` = 6 hit（S301 spike 4 + SP-011 spike 2；SceneManager.cs L424 在 §1 P2 切换路径用过但被 grep 工具按 file 计数命中合并到 6）
2. `_currentChapterSceneName | ClearCurrentChapterSceneName` = 5 hit（SceneManager.cs L97/100/437/463 + 1 处 testhook）
3. `Resources.UnloadUnusedAssets()` = 2 hit
4. `ISceneEvent_Event.OnSceneUnloadBegin` = 0 hit ⚠️ **预期** — S3-02 实施后应 ≥ 1（cleanup sender 调用点）

post-impl 实测（dev-story 完成 — 2026-04-30 late afternoon）：
1. `GameModule.Scene.UnloadAsync` = **9 hit** ✅（S302 spike:1 + SceneManager:2 + S301 spike:4 + SP-011 spike:2）
2. `_currentChapterSceneName | ClearCurrentChapterSceneName | SetCurrentChapterSceneNameForTest` = **14 hit** ✅（SceneManager.cs 内部读写 + testhook setter + UnloadCurrentChapterAsync()）
3. `Resources.UnloadUnusedAssets()` = **7 hit** ✅（S302 spike:2 + SceneManager:3 + SingletonSystem 历史:1 + SP-011 spike:1）
4. `ISceneEvent_Event.OnSceneUnloadBegin` = **8 hit** ✅（SceneManager.UnloadCurrentChapterAsync() Step 5 sender + S302 spike:6 listener wireup）

**§2 4 项均 ≥ 1 hit ✅**；framework correct usage pattern 已在 active 代码层完整落地（S3-01 D5 全量 propagation 实战首测 PASS）。

post-impl §1 复查：8 项 forbidden / fantasy API 仍全部 **0 hit** ✅（dev-story 实施未引入任何 fantasy pattern）。

---

### §2.5 PlayMode runtime probe 暴露 Type-2 drift（v3 修复 — 2026-04-30 dusk）

**R3 PlayMode 实跑结果**：6/6 cases FAIL（`OnSceneUnloadBegin listener count = 0`，`sceneCountNow=2 vs baseline=1`，scene 永不被卸载，sceneName 永不被清，全部 case 级联失败）。

**Root cause（Type-2 cross-method protocol drift）**：
- `LoadChapterSceneAsync` 仅 set `_currentChapterSceneName`（S3-01 D5 cache），**不** set `_currentChapterId`
- `_currentChapterId` 仅由 `OnRequestSceneChange` 状态机入口 set
- spike 直调 `LoadChapterSceneAsync` 路径下 `_currentChapterId` 永留 NoChapterId
- `UnloadCurrentChapterAsync` 守卫 `if (_currentChapterId == NoChapterId || ...)` 永真 → silent return
- 同时暴露生产 driver 路径 bug：driver 通过 `OnRequestSceneChange` 进入时 `_currentChapterId` 已是 NEW target，sender `OnSceneUnloadBegin(_currentChapterId)` 派错 chapter id（应派 OLD）

**v3 修复（SceneManager.cs 2026-04-30 dusk）**：
1. 新增 `private int _currentLoadedChapterId = NoChapterId` 字段，与 `_currentChapterSceneName` 配对作为"已加载身份"单一事实源
2. `LoadChapterSceneAsync` 成功路径同步 set 两字段
3. `ClearCurrentChapterSceneName()` 同步 reset 两字段（原子配对）
4. `UnloadCurrentChapterAsync` guard 检 `_currentLoadedChapterId`，sender 派 `_currentLoadedChapterId`
5. 新增 `public int CurrentLoadedChapterIdForTest` 测试 getter

**post-v3 grep 增量**（新 pattern）：

| Pattern | Pre-v3 | Post-v3 | Verdict |
|---------|--------|---------|---------|
| `_currentLoadedChapterId` | 0 | **6 hit** | ✅ 新字段 declare + Load set + Clear reset + Unload guard + Unload sender + getter |
| `CurrentLoadedChapterIdForTest` | 0 | **1 hit** | ✅ 测试桩 getter |

**post-v3 §2 影响（无回归）**：
- `_currentChapterSceneName | Clear* | SetForTest` = 14 hit 不变（旧字段读写未删，新字段在 Clear/Load/Unload 同步 mutate）
- `OnSceneUnloadBegin` = 8 hit 不变（sender 调用点未增减，仅参数源变更）
- §1 8 项 forbidden API 仍 0 hit ✅

---

## §3 ADR-029 R1/R2/R3 verdict

| Rule | 名称 | Verdict (post-patch v2) | 数据 |
|------|------|------------------------|------|
| **R1** | Per-event listener / sender pairing (ADR-027) | ✅ PASS | story 走 sender 模式 `GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(_currentChapterId)`；listener-side self-removal 责任落在订阅系统（非本 story 域） |
| **R2** | Cross-component API existence grep | ❌ STOP → ✅ PASS post-patch v2 | 4 处 type-1（type-4 propagation drift）：(1) `_currentSceneHandle` → `_currentChapterSceneName`；(2) `GameModule.Resource.UnloadSceneAsync(handle)` → `GameModule.Scene.UnloadAsync(string)`；(3) `null` 直写 → `ClearCurrentChapterSceneName()` setter；(4) `Tests/EditMode/CleanupSequenceTests.cs` → `S302_CleanupSequence.cs` PlayMode spike |
| **R3** | Stub data type constructor | ✅ PASS | 无 stub data type 引用 |
| Type-2 / Type-3 风险 | — | LOW | `GameModule.Scene.UnloadAsync` S3-01 P2 实证 PASS；spike sequential 调度（SP-011 已注销；S302 单 spike 注册）|

---

## §4 ADR-029 第 2 数据点 + 决策传播 ROI 验证

### Drift revision time 数据点（vs S3-01 baseline）

| Story | Total | Type-1 | Type-2 | Type-3 | Type-4 (new) | Notes |
|-------|-------|--------|--------|--------|--------------|-------|
| **S3-01 (baseline)** | **18 min** | 10 min | 5 min | 3 min | — | First baseline；含 patch v2 全文 + 2 轮 PlayMode + D1-D6 + 3 修复 option |
| **S3-02** | **~8 min** | — | — | — | 8 min | ~55% 节省 vs baseline；全部属 type-4 propagation drift（S3-01 D5 决策已生效 2026-04-30 commit，但 story-003 写于 2026-04-22 未跟进）|
| Cumulative trend | — | ≤5 min target | TBD post-rule | TBD post-rule | ≤2 min target post-V2 trigger | post-propagation 实战 |

### Propagation 实战首测 ROI（同日 2026-04-30 afternoon）

- **Cost**: ~35 min（grep matrix + 12 文件全量修订 + 2 历史档 superseded + change-impact doc）
- **Avoided**: 25-40 min（S3-03/-04/-05 等下游 R2 STOP × 5 stories × 5-8 min）+ 15-30 min（QA 走偏 incident）= 40-70 min
- **Net positive ROI**: +5 ~ +35 min（首次破均衡）

**重要 insight**：`/propagate-design-change` lite version 对 architecture decision (D-level) 是 net positive ROI；ADR-029 V2 候选 §1 trigger condition 应加入。

---

## §5 ADR-028 §1 M3 SceneModule 必用决策合规

本 patch v2 100% 走 `GameModule.Scene` facade，**不**直调 YooAsset native（`YooAssets.LoadSceneAsync` / `SceneHandle` etc.）；**不**直调 Unity 原生 `UnityEngine.SceneManagement.SceneManager.LoadSceneAsync` → 与 ADR-028 §0 Skill-first 原则 + §2 GameModule facade 强制约定一致。

cleanup 序列对 framework facade 调用清单：
1. `GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(...)`（ADR-027 sender 模式）
2. `await UniTask.Yield()`（UniTask 标准）
3. `await GameModule.Scene.UnloadAsync(string)`（SceneModule wrapper）
4. `ClearCurrentChapterSceneName()`（SceneManager 内部 setter）
5. `await Resources.UnloadUnusedAssets().ToUniTask()`（Unity native + UniTask extension）
6. `GC.Collect()`（.NET 原生，同步）

---

## §6 Test coverage mapping

S3-02 patch v2 §QA Test Cases 6 cases 映射到 PlayMode spike `S302_CleanupSequence.cs`：

| AC ID | spike case | 验证 grep evidence 项 |
|-------|-----------|---------------------|
| AC-1 (Order) | P1 | §2-1 `GameModule.Scene.UnloadAsync` 必用；§2-3 `Resources.UnloadUnusedAssets()` 必用 |
| AC-2 (SceneNameNull) | P2 | §1-1 `_currentSceneHandle` 0 hit + §2-2 `ClearCurrentChapterSceneName` 必调 |
| AC-3 (FirstBoot) | P3 | NoChapterId 哨兵守卫；不命中 §2-4 sender |
| AC-4 (CleanupOnError) | P4 | try-finally 断言 §2-1 即使 throw 也跑 §2-3 |
| AC-5 (SharedAsset) advisory | P5 | `LoadAssetAsync` ≥1 hit + `UnloadUnusedAssets` 不释放（refcount > 0）|
| AC-6 (5-cycle MemoryLeak) | P6 | 5 cycle baseline ±5%；与 SP-011 P3 同模式 |

---

## §7 Verdict

✅ **ADR-029 Phase 1.5 readiness PASS**（post-patch v2 + grep evidence）— S3-02 可进入 `/dev-story` 实施阶段。

后续 dev-story 实施完成后重跑 §2 4 项 grep，确认全部 ≥1 hit；§1 8 项保持 0 hit；最终 spike PlayMode CORE PASSED → `/story-done`。
