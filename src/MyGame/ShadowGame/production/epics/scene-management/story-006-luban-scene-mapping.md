// 该文件由Cursor 自动生成

# Story 006: Luban Scene Name ↔ Chapter ID Mapping

> **Epic**: Scene Management
> **Status**: Complete
> **Completed**: 2026-04-29
> **Layer**: Core
> **Type**: Config/Data
> **Manifest Version**: 2026-04-22
>
> **Revision note (2026-04-23)**: `Evt_SceneLoadFailed` / `Evt_SceneLoadComplete` + payload struct 引用已迁移到 ADR-027 接口方法 `ISceneEvent.OnSceneLoadFailed(chapterId, error)` / `OnSceneLoadComplete(chapterId, bgmAsset)`（签名由 Story 001 冻结）。

## Context

**GDD**: `design/gdd/scene-management.md`
**Requirement**: `TR-scene-015` (Luban-driven scene registry; no hardcoded scene names)

**ADR Governing Implementation**: ADR-009: Scene Lifecycle + ADR-007: Luban Config Access + ADR-027: GameEvent Interface Protocol
**ADR Decision Summary**: Scene name ↔ chapter ID mapping is read from Luban `TbChapter.sceneId`. No hardcoded scene names exist anywhere in code. This allows hot-update of scene mappings post-launch. Scene Manager reads the mapping before every scene load and resolves the asset path from the config.

**Engine**: Unity 2022.3.62f2 LTS + Luban (latest) | **Risk**: LOW
**Engine Notes**: `TbChapter` is a Luban-generated table in the `GameProto` assembly. Access via `ConfigSystem.Tables.TbChapter.Get(chapterId)`. Returns a `ChapterData` object with at minimum: `int Id`, `string SceneId`, `string BgmAsset`, `float EmotionalWeight`. SP-004 confirmed: all reads on main thread are safe; never read inside `UniTask.Run()`.
**Performance**: N/A — Config/Data story；`TbChapter.Get(id)` 在章节切换冷路径调用（~5 次/游戏），查表为 O(1) 哈希访问，无 runtime 数据生成，无帧级开销。

**Control Manifest Rules (this layer)**:
- Required: `All config reads via Tables.Instance.TbXXX.Get(id)` (ADR-007)
- Required: `Tables.Init() executes at boot Step 7 (ProcedureMain) — MUST complete before ANY system reads config` (ADR-007)
- Required: `Scene name ↔ chapter ID mapping from Luban TbChapter.sceneId — no hardcoded scene names` (ADR-009)
- Required: `Config data objects are read-only after Init() — never modify a field on a Luban-generated data object at runtime` (ADR-007)
- Forbidden: `Never hardcode gameplay values` (ADR-007)
- Forbidden: `Never access Luban Tables from UniTask.Run() (thread pool)` (SP-004)
- Forbidden: `Never cache config table references in persistent fields` (ADR-007)
- Forbidden: `Never hand-edit any file in GameProto` (ADR-007)

---

## Acceptance Criteria

*From GDD `design/gdd/scene-management.md`, scoped to this story:*

- [ ] **Deferred to Luban TbChapter 落地** —— `TbChapter` table contains at minimum these fields per chapter row: `Id (int)`, `SceneId (string)`, `BgmAsset (string)`, `EmotionalWeight (float)`, `OverlayColor (Color/string)`
- [x] `SceneManager` resolves `chapter` data by calling `_chapterDataProvider(chapterId)` before every transition — no hardcoded scene names in any C# production file（grep evidence ✅；Luban 真接入待 TbChapter 落地）
- [x] If provider returns null (unknown chapter ID), Scene Manager transitions to `Error` state and dispatches `ISceneEvent.OnSceneLoadFailed(chapterId, error)` via `GameEvent.Get<ISceneEvent>()` with descriptive error message
- [ ] **Deferred to S2-14**（BeginTransition 11 步）—— `EmotionalWeight` field from `TbChapter` is used to scale fade durations: `fadeOutDuration = FADE_BASE_OUT * emotionalWeight` and `fadeInDuration = FADE_BASE_IN * emotionalWeight`
- [ ] **Deferred to S2-17**（lifecycle senders）—— `BgmAsset` field is forwarded as the `bgmAsset` parameter of `ISceneEvent.OnSceneLoadComplete(chapterId, bgmAsset)` for the Audio system to switch music
- [ ] **Deferred to Transition Overlay UI epic（不在 Sprint 2 范围）**—— `OverlayColor` field is used by the Transition Overlay Canvas for chapter-specific fade color (resolves from config, not hardcoded)
- [x] A grep/static analysis scan of the entire codebase finds zero hardcoded scene name strings (e.g., `"Chapter_01_Approach"`, `"Chapter_02_SharedSpace"`) —— evidence: `production/qa/grep-no-hardcoded-scene-names-2026-04-29.md`

---

## Implementation Notes

*Derived from ADR-007 + ADR-009 Implementation Guidelines:*

Scene name resolution is a few lines at the start of `BeginTransitionAsync(int targetChapterId)`:
```csharp
private async UniTask BeginTransitionAsync(int targetChapterId)
{
    var chapterData = ConfigSystem.Tables.TbChapter.Get(targetChapterId);
    if (chapterData == null) {
        GameEvent.Get<ISceneEvent>().OnSceneLoadFailed(
            targetChapterId,
            $"Chapter ID {targetChapterId} not found in TbChapter");
        TransitionTo(SceneManagerState.Error);
        return;
    }
    string sceneName = chapterData.SceneId;
    float fadeOut = FADE_BASE_OUT * chapterData.EmotionalWeight;
    float fadeIn  = FADE_BASE_IN  * chapterData.EmotionalWeight;
    // ... continue with 11-step flow from story 001/002/003/005
}
```

**Luban table definition** (`TbChapter.xlsx` or equivalent — do NOT hand-edit the generated `TbChapter.cs`):
| Field | Type | Notes |
|-------|------|-------|
| Id | int | Chapter number (1–5) |
| SceneId | string | Unity scene name (e.g., `"Chapter_01_Approach"`) |
| BgmAsset | string | YooAsset address for music clip |
| EmotionalWeight | float | 0.8–1.5 — multiplier for fade duration |
| OverlayColor | string | Hex color string for transition overlay |

**No caching of table references**: read via `ConfigSystem.Tables.TbChapter.Get(id)` each time (exception: within a single method call for performance-hot paths — but BeginTransition is not per-frame).

**SP-004 note**: `BeginTransition` is called from the main thread (GameEvent handler). The `Get()` call is safe. Never wrap it in `UniTask.Run()`.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: The actual `LoadSceneAsync` call using the resolved `sceneName`
- Story 005: Events that carry BgmAsset in their payloads (already implemented there)
- Art Bible: defining what the chapter overlay colors should be — the config value is provided by Design

---

## QA Test Cases

*Config/Data type — smoke check against loaded data:*

- **AC-1**: TbChapter rows exist and are valid
  - Setup: Game starts; `Tables.Init()` completes
  - Verify: `ConfigSystem.Tables.TbChapter.Get(1)` returns non-null; `SceneId` is non-empty string; `EmotionalWeight` is in range [0.5, 2.0]
  - Pass condition: All 5 chapter rows return valid `SceneId` values; no nulls or empty strings

- **AC-2**: No hardcoded scene names in codebase
  - Setup: Run grep on all `.cs` files in `Assets/GameScripts/`
  - Verify: No file contains string literals matching pattern `"Chapter_0[1-5]_"` or known scene names
  - Pass condition: Zero matches

- **AC-3**: Unknown chapter ID → Error state + `OnSceneLoadFailed`
  - Setup: Dispatch `GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99)` (chapter 99 不在 TbChapter)
  - Verify: Scene Manager enters Error state; listener 收到 `OnSceneLoadFailed(99, error)`，error 非空
  - Pass condition: No NullReferenceException; graceful error message; UI recoverable

- **AC-4**: EmotionalWeight scales fade duration
  - Setup: Chapter 1 has `EmotionalWeight=0.8`; Chapter 5 has `EmotionalWeight=1.5`
  - Verify: Fade-out duration for Chapter 1 = `0.8 × 0.8s = 0.64s`; for Chapter 5 = `1.5 × 0.8s = 1.2s`
  - Pass condition: Actual tween duration matches formula within ±0.05s tolerance

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**:
- `Assets/Tests/EditMode/SceneManagement/LubanSceneMappingTests.cs` — 8 tests covering ChapterData POCO + 柔和 fail-loud + DrainPending 校验路径
- `production/qa/grep-no-hardcoded-scene-names-2026-04-29.md` — AC-2/AC-7 静态扫描 evidence

**Status**: [x] **Created and passing** — EditMode Run All **242/242 全绿**（baseline 234 + S2-07 8，零回归）；HotFix production code grep `Chapter_0[1-5]_` 与 `"Chapter_*"` 均 0 命中

---

## Implementation Log (2026-04-29)

### 范围收窄说明

发现两处 story-readiness（2026-04-23）未捕获的硬约束：

1. **Luban `TbChapter` 表完全不存在** —— `Tables.cs` 仅有 demo `TbItem`；`ConfigSystem.Tables.TbChapter.Get(id)` 直接编译错误
2. **3/6 AC 依赖 Story 002（S2-14 Should Have，未落地）** —— AC-4 (`EmotionalWeight × fade`) / AC-5 (`BgmAsset` payload) / AC-6 (`OverlayColor` overlay) 均需要 `BeginTransition` 11 步流程或 lifecycle sender 才能真正实施

按 sprint-2.md L127 早就预案的"in-memory fixture + provider 抽象"路径（与 S2-01..S2-04 Stub 模式一致）执行，本 story 实际交付 **3/6 AC**（AC-2 grep + AC-3 fail-loud + AC-7 grep 同义）；其余 3/6 AC 显式标 **Deferred to S2-14 / S2-17 / Transition Overlay UI**。

### 5 个设计决策（讨论后拍板）

| ID | 选项 | 决策 | 理由 |
|---|---|---|---|
| 大方向 | P1 vs P2 (真 Luban 接入) vs P3 (推迟) | **P1** | 与 sprint-2.md 兜底一致，与 S2-01..S2-04 风格一致；Provider 抽象未来 Luban 接入只改 1 行 |
| M1 | Provider 形态 | **α** `Func<int, ChapterData>`（与 `ChapterStateManager._unlockConditionProvider` 同模式） | 一致最高 |
| M2 | Provider 未知章节返回 | **α** 返回 `null` | 与 Luban `TbChapter.Get(id)` 真实行为一致，不抛 |
| M3 | fixture 数据放置 | **γ** production 0 fixture；测试内手工构造 ChapterData | 真正满足 AC-2 "production 零硬编码 scene name" |
| M4-bis | Default 行为 | **柔和 fail-loud**：provider 未注册 → 跳过校验（兼容 baseline）；provider 注册了但返 null → fail-loud（AC-3 真正保护场景） | 不破坏 S2-05/S2-06 现有 41 个测试；boot pipeline 未注册 provider 视为部署 bug 而非运行时 bug |
| M4 | 校验插入点 | **α** `OnRequestSceneChange` 不同章分支前 + `DrainPending` 启新过渡前各一次 | 早失败 |

### 实施（2 新 1 改 1 evidence）

**新建：**
- `Assets/GameScripts/HotFix/GameLogic/Scene/ChapterData.cs` — POCO 5 readonly 字段（`Id` / `SceneId` / `BgmAsset` / `EmotionalWeight default=1.0` / `OverlayColor default="#FFFFFF"`）；XML 标注每字段未来 Luban `TbChapter` 列对应；S2-07 范围内仅用 `Id` + 存在性，其余 4 字段签名先行冻结
- `Assets/Tests/EditMode/SceneManagement/LubanSceneMappingTests.cs` — 8 tests，4 组：
  - §1 ChapterData POCO 构造器 + 默认值 (2)
  - §2 Default 无 provider 走旧行为 (1)
  - §3 Provider 注册：已知通过 + 未知 fail-loud + RegisterChapterDataProvider(null) 重置 (3)
  - §4 DrainPending 路径：drain 已知通过 + drain 未知 fail-loud (2)

**改：**
- `Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs`：
  - 新增 `private System.Func<int, ChapterData> _chapterDataProvider;`（默认 null）
  - 新增 `public void RegisterChapterDataProvider(System.Func<int, ChapterData> provider)`（接受 null 重置）
  - 新增私有 `private bool TryResolveOrFail(int chapterId)` —— 柔和 fail-loud 入口（provider 未注册返 true 兼容；provider 返 null 派发 OnSceneLoadFailed + Error 返 false）
  - 在 `OnRequestSceneChange` Idle 不同章分支前插入 1 次 `TryResolveOrFail`（fail 时 return，不更新 current/inflight）
  - 在 `DrainPending` 启新过渡前插入 1 次 `TryResolveOrFail`（fail 时 return；pending 已被消费清空）

**evidence：**
- `production/qa/grep-no-hardcoded-scene-names-2026-04-29.md` — AC-2/AC-7 static scan 0 匹配 + CI gate 建议（Sprint 3 polish 阶段评估）

### 验证

- ReadLints ✅ 零错误（3 个改动文件全部）
- assets-refresh ✅；Console 0 Error / 0 Exception / 0 CS
- EditMode 手动 Run All **242/242 全绿**（baseline 234 + S2-07 8）；S2-05/S2-06 41 个测试零回归（柔和 fail-loud 设计成功）
- grep evidence：HotFix production code `Chapter_0[1-5]_` 与 `"Chapter_*"` 均 0 命中 ✅

### 遗留（Sprint 3 / 后续 story 接力）

| 项 | 触发时机 | 动作 |
|---|---|---|
| Luban `TbChapter.xlsx` + Schema 编辑 | Sprint 3 polish 或 S2-14 启动前 | Architect/Designer 协作出列定义；CodeGen 生成 `TbChapter.cs` + `.bytes` |
| Boot pipeline 注入真实 provider | S2-14（BeginTransition 实施） / GameApp.Entrance | `sceneManager.RegisterChapterDataProvider(id => ConfigSystem.Tables.TbChapter.Get(id));`（在 `Tables.Init()` 完成之后） |
| Control Manifest 增补 | Sprint 2 收尾 / S2-14 落地时 | §2.5 加规则"boot pipeline 必须 register ChapterDataProvider，未注册视为部署 bug" |
| AC-4 EmotionalWeight × fade | S2-14 BeginTransition 11 步 | 在 BeginTransition 内拉 ChapterData 后 set fade tween 时长 |
| AC-5 BgmAsset → OnSceneLoadComplete | S2-17 lifecycle senders | OnSceneLoadComplete sender 实施时从 ChapterData 取 BgmAsset |
| AC-6 OverlayColor → Transition Overlay | Transition Overlay UI epic（不在 Sprint 2） | UI epic 接入时从 ChapterData 取 OverlayColor

---

## Dependencies

- Depends on: Story 001 (state machine), Story 002 (BeginTransition uses resolved sceneName)
- Unlocks: No story is directly blocked by this — it refines Story 002's implementation with data-driven scene names
