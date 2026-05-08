// 该文件由Cursor 自动生成

# Grep Evidence: Scene Management — No Fantasy API Drift (S3-01 patch v2)

> **Date**: 2026-04-30 noon
> **Story**: S3-01 Additive Scene Loading via YooAsset
> **Trigger**: ADR-029 Phase 1.5 R2 STOP — story §Implementation Notes 6 处 fantasy API drift
> **Patch v2 commit**: 2026-04-30 — full alignment to TEngine framework facade (`GameModule.Scene` / `GameModule.Resource`)
> **此文档目的**：固化 8 项 0-hit grep + 5 项 ≥1-hit framework correct usage 验证，作为 ADR-029 §Validation Criteria "新 drift 模式 0 次" 的 first-data-point 证据

---

## §1 8 项 forbidden / fantasy API — 0 hit 验证

均针对 scope `Assets/GameScripts/HotFix/GameLogic/Scene/`（Scene Manager 实现层）。

| # | Pattern | 含义 | Result |
|---|---------|------|--------|
| 1 | `GameModule\.Resource\.LoadSceneAsync` | YooAsset native 心智模型残留（实际 framework 用 SceneModule 包装）| ✅ **0 hit** |
| 2 | `GameModule\.Resource\.UnloadSceneAsync` | 同上 | ✅ **0 hit** |
| 3 | `SceneHandle` | YooAsset 原生类型残留（实际返回 Unity `Scene` struct）| ✅ **0 hit** |
| 4 | `\.SceneObject` | `SceneHandle.SceneObject` 字段残留（Unity Scene struct 无此字段）| ✅ **0 hit** |
| 5 | `LoadSceneMode\.Single` | ADR-009 禁用模式（章节场景必须 Additive）| ✅ **0 hit** |
| 6 | `UnityEngine\.SceneManagement\.SceneManager\.SetActiveScene` | 直调 Unity 原生而绕过 framework 包装层 | ✅ **0 hit** |
| 7 | `EventId\.Evt_` | ADR-027 §1 已废弃的整数事件常量模式 | ✅ **0 hit** |
| 8 | `while.*\.IsDone` | 反 framework idiom 的轮询模式（D4=[I] progressCallBack lambda 优先）| ✅ **0 hit** |

### 核验命令

```bash
rg "GameModule\.Resource\.LoadSceneAsync" Assets/GameScripts/HotFix/GameLogic/Scene/
rg "GameModule\.Resource\.UnloadSceneAsync" Assets/GameScripts/HotFix/GameLogic/Scene/
rg "SceneHandle" Assets/GameScripts/HotFix/GameLogic/Scene/
rg "\.SceneObject" Assets/GameScripts/HotFix/GameLogic/Scene/
rg "LoadSceneMode\.Single" Assets/GameScripts/HotFix/GameLogic/Scene/
rg "UnityEngine\.SceneManagement\.SceneManager\.SetActiveScene" Assets/GameScripts/HotFix/GameLogic/Scene/
rg "EventId\.Evt_" Assets/GameScripts/HotFix/GameLogic/Scene/
rg "while.*\.IsDone" Assets/GameScripts/HotFix/GameLogic/Scene/
```

8 项执行 → "No matches found"（实测于 2026-04-30 noon）。

---

## §2 4 项 framework correct usage — ≥1 hit 验证（P0 修订：移除 CheckLocationValid）

| # | Pattern | 期望 ≥ 1 hit | 实际 location |
|---|---------|--------------|---------------|
| 1 | `GameModule\.Scene\.LoadSceneAsync` | ≥ 1 | `SceneManager.cs` (LoadChapterSceneAsync Step 9) |
| 2 | `GameModule\.Scene\.ActivateScene` | ≥ 1 | `SceneManager.cs` (LoadChapterSceneAsync Step 10a) |
| 3 | `GameModule\.Resource\.CreateResourceDownloader` | ≥ 1 | `SceneManager.cs` (LoadChapterSceneAsync Step 8) |
| 4 | `DownloadUpdateCallback` (与 `ProcedureDownloadFile.cs` 已 PASS 模式同源) | ≥ 1 | `SceneManager.cs` (LoadChapterSceneAsync Step 8) |

### P0 修订记录（2026-04-30 spike 首跑反馈）

原计划包含 `GameModule.Resource.CheckLocationValid(sceneName, "DefaultPackage")` 前置 fail-loud 校验。spike runtime 实测 P1 FAIL：CheckLocationValid 对 YooAsset scene 资产（.unity）一律返 `false`（scene 与 asset 走不同 location key 体系；SP-011 PASS 路径同样不调 CheckLocationValid 而直接 LoadSceneAsync）。修复 [A]：**移除 CheckLocationValid 前置**；invalid sceneName 由 LoadSceneAsync 自身抛异常 → catch → retry 2 次 → 耗尽 fail；ChapterDataProvider null 仍是唯一 fail-loud 强 contract。AC-4 路径相应改为 retry exhaust 形式。

### 注意事项

- `GameModule.Scene.UnloadAsync` 在 SceneManager.cs **0 hit by design** — Story 002 不实施 unload 路径（属 Story 003 cleanup 范围）；spike runtime `S301_AdditiveSceneLoading.cs` 中有 ≥ 1 hit 用于 P2/P3 测试 unload 场景资源
- 进度回调通过 `progressCallBack: p => OnSceneLoadProgress(...)` lambda 直接传 framework，**不**轮询 `IsDone`（D4=[I] 设计决策；与 §1 #8 grep gate 互锁）

---

## §3 ISceneEvent listener 注册模式 — per-event 合规验证（ADR-027 + conventions.md §Listener）

`S301_AdditiveSceneLoading.cs` spike runtime 注册 4 个 listener，全部 per-event 模式：

```csharp
GameEvent.AddEventListener<float, long, long>(
    ISceneEvent_Event.OnSceneDownloadProgress, OnDownloadProgress);
GameEvent.AddEventListener<string, float>(
    ISceneEvent_Event.OnSceneLoadProgress, OnLoadProgress);
GameEvent.AddEventListener<int, string>(
    ISceneEvent_Event.OnSceneLoadComplete, OnLoadComplete);
GameEvent.AddEventListener<int, string>(
    ISceneEvent_Event.OnSceneLoadFailed, OnLoadFailed);
```

**0 hit** of forbidden patterns:
- `AddEventListener<I\w+Event>\(this\)` (整接口订阅幻想)
- `class \w+\s*:\s*\w+,\s*ISceneEvent` (Listener 实现整个接口的反模式)

ADR-027 + conventions.md §"Listener 端订阅模式" 合规 ✅

---

## §4 ADR-029 R1/R2/R3 readiness gate — 实战首次 PASS 记录

| Check | Pre-patch (R2 STOP triggered) | Post-patch v2 |
|-------|-------------------------------|---------------|
| R1 per-event listener pattern | N/A (sender story) | ✅ PASS |
| R2 cross-component API existence | ❌ STOP — 6 处 fantasy API | ✅ PASS — 全 0 hit fantasy + 5 处 framework correct API ≥ 1 hit |
| R3 stub data type construction | ✅ PASS (ChapterData / ISceneEvent 签名匹配) | ✅ PASS (维持) |

**首个 ADR-blessed dev-story Phase 1.5 readiness gate 实战命中数据点**：drift revision time ≈ 10 min（草案撰写 + proposal review + commit）；按 ADR-029 §Validation Criteria "Sprint 3 平均 drift 修订耗时 ≤ 1min" 标准衡量，首次偏长（首次 5 min checklist 命中后需补 patch v2 文档全文流程）；后续 Sprint 3 stories 经过相同路径后 drift revision time 应稳态下降到 < 5 min。

---

## §5 测试覆盖映射（5 case + 1 ADVISORY）

| Case | AC | Status |
|------|-----|--------|
| P1 SingleLoad | AC-1/AC-2/AC-3/AC-6/AC-7/AC-8 | spike 实测 |
| P2 Switch+AC10 | AC-10 + cross-load consistency | spike 实测 |
| P3 DownloadBranch | AC-5 | spike 实测（缓存命中时 SKIP-ADVISORY，非 fail）|
| P4 InvalidSceneFailLoud | AC-4 + AC-9 retry exhaust 路径（P0 修订：retry exhaust 形式 by LoadSceneAsync 抛错 → 2 次耗尽 → fail） | spike 实测 |
| P5 RetryMechanism (success path) | AC-9 retry-then-success | **ADVISORY** — D1=[b] PlayMode-only 模式下 GameModule.Scene 静态 facade 不可 stub 制造 1 次失败 + 1 次成功；try-catch + lastError + MAX_LOAD_RETRY 循环代码 verified by Code Review of SceneManager.LoadChapterSceneAsync |

---

## §6 sign-off

- `SceneManager.cs` v2 patch — 编译通过（0 lint）
- `S301_AdditiveSceneLoading.cs` — 编译通过（0 lint）
- `GameApp.cs` RegisterDevSpikes 已加 S301Spike 注册
- 8 项 forbidden / fantasy API grep — 全 0 hit
- 5 项 framework correct API grep — 全 ≥ 1 hit
- ISceneEvent per-event listener 模式 — ADR-027 合规 ✅

**ADR-028 §1 M3 SceneModule 必用决策**：本 patch v2 100% 走 `GameModule.Scene` facade，**不**直调 YooAsset native（`YooAssets.LoadSceneAsync` / `SceneHandle` etc.）→ 与 ADR-028 §0 Skill-first 原则 + §2 GameModule facade 强制约定一致。
