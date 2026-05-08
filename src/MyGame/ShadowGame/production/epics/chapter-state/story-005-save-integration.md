// 该文件由Cursor 自动生成

# Story 005: Bidirectional Integration with Save System (IChapterProgress)

> **Epic**: Chapter State
> **Status**: Complete
> **Layer**: Core
> **Type**: Integration
> **Manifest Version**: 2026-04-22
> **Completed**: 2026-04-23 — 12 NUnit tests GREEN in `SaveIntegrationTests.cs` (EditMode). Test evidence: Test Runner 187/187 passing with the 5 `[UnityTest] IEnumerator + UniTask.ToCoroutine` disk round-trip tests after Unity restart cleared a stale TestRunner channel.
>
> **Revision note (2026-04-23)**: 由 ADR-006（Superseded）+ 过时事件命名（`Evt_PuzzleStateChanged` / `Evt_ChapterComplete`）刷新为 ADR-027 接口事件协议。`ChapterStateManager.Init(...)` 签名与现实代码对齐（`Init(IChapterProgress, PuzzleMeta[][])`，由 S2-02 引入）。`SaveData : IChapterProgress` 直接就是接口（无 `.ChapterProgress` 字段路径）。本 story 新建 API：`ChapterStateManager.GetProgressSnapshot()` / `ChapterProgressSnapshot` / `SaveManager.RegisterProgressProvider` / `AutoSaveTrigger`。Puzzle 结构按 S2-01/S2-02 先例沿用 stub 模式（硬编码 puzzleMeta，Luban 接入留给后续 story）。

## Context

**GDD**: `design/gdd/chapter-state-and-save.md`
**Requirement**: `TR-save-005`
*(IChapterProgress interface decoupling between Chapter State and Save System)*

**ADR Governing Implementation**: ADR-008: Save System + ADR-027: GameEvent Interface Protocol + ADR-001: TEngine 6.0 Framework
**ADR Decision Summary**: `IChapterProgress`（Story 001 已定义）是 Chapter State 与 Save System 之间的**唯一**数据契约。启动序：`SaveSystem.Init() → SaveManager.LoadAsync() → ChapterStateManager.Init(saveData, puzzleMeta)`；运行时 Chapter State 通过 `GetProgressSnapshot(): IChapterProgress` 暴露只读快照，SaveManager 通过 `RegisterProgressProvider(Func<IChapterProgress>)` 持有该 delegate 并在 `SaveAsync` 时调用——**零**直接类型引用。Auto-save 的触发由独立的 `AutoSaveTrigger` 订阅 `IChapterStateEvent.OnChapterComplete / OnGameComplete` 接口事件实现（ADR-027 §1，listener 单一职责），不与 S2-03 的 `ChapterStateEventBridge` 冲突（bridge 管 sender 端，trigger 管 listener 端）。

**Engine**: Unity 2022.3.62f2 LTS + UniTask 2.5.10 | **Risk**: MEDIUM
**Engine Notes**: Init order is critical (ADR-008)：`SaveManager.LoadAsync() → ChapterStateManager.Init(saveData, puzzleMeta)`；由 boot Step 11 (SaveSystem) → Step 12 (ChapterState) 保证，均在 `ProcedureMain` 主线程完成，先于 gameplay。`GetProgressSnapshot` 预算 < 1ms（读 in-memory 数据，LINQ 可用，无 I/O）。Auto-save 触发由 `IChapterStateEvent.OnChapterComplete`（每章节一次）+ `OnGameComplete`（最终一次）驱动；**不**订阅 `OnPuzzleStateChanged`（每帧可能多次，避免 I/O 抖动）。

**Control Manifest Rules (this layer)**:
- Required: `Init order: SaveManager.LoadAsync() → ChapterStateManager.Init(saveData, puzzleMeta)`（ADR-008）
- Required: `All async I/O via UniTask`（ADR-001）
- Required: `Cross-module state broadcasts via [EventInterface] interface events`（ADR-027 §1 取代 ADR-006 §1/§2）
- Required: `Listener 在 Init/构造时 AddEventListener，在 Dispose 时 RemoveEventListener`（ADR-027 §3 继承 ADR-006 §3）
- Required: `IChapterProgress 是 Chapter State ↔ Save System 的唯一数据契约`（ADR-008 §4）
- Required: `Save System 通过 RegisterProgressProvider(Func<IChapterProgress>) delegate 拿 snapshot，零直接 ChapterStateManager 引用`（ADR-008 §4 解耦原则）
- Forbidden: `禁止使用 Coroutines 做异步 I/O`（ADR-008）
- Forbidden: `禁止把 Settings 存进 save file`（ADR-008，Settings 走 PlayerPrefs）
- Forbidden: `禁止 SaveManager 直接引用 ChapterStateManager 或反向`（ADR-008 §4 的硬性约束）
- Guardrail: `GetProgressSnapshot 单次调用 ≤ 1ms（in-memory only，无 I/O）`
- Guardrail: `Auto-save 触发防抖：同一章节同一帧内多次 OnChapterComplete 合并为单次 SaveAsync`（边界幂等）

---

## Acceptance Criteria

*From GDD `design/gdd/chapter-state-and-save.md`, re-aligned to ADR-027 + 现有实现 by 2026-04-23 revision:*

- [ ] **AC-1 Snapshot API**：`ChapterStateManager.GetProgressSnapshot()` 返回一个 `IChapterProgress` 实现（`ChapterProgressSnapshot`），其 `UnlockedChapterIds` / `CompletedChapterIds` / `PuzzleEntries` 准确反映当前 runtime 状态。方法**同步**，**无** I/O，单次 < 1ms。
- [ ] **AC-2 Provider 注册**：`SaveManager.RegisterProgressProvider(Func<IChapterProgress> provider)` 接受一个 delegate；`SaveAsync(...)` 内部调用 delegate 取得最新 `IChapterProgress` 并将其三字段写入 `SaveData`。delegate 可为 `null`——此时 save file 的 chapter 区段保持上一次加载的值（非空，不清空）。
- [ ] **AC-3 解耦**：`grep` `ChapterStateManager` 在 `Assets/GameScripts/HotFix/GameLogic/Save/*.cs` 下 **0** 命中；`grep` `SaveManager` 在 `Assets/GameScripts/HotFix/GameLogic/ChapterState/*.cs` 下 **0** 命中。双向零直接引用。
- [ ] **AC-4 Round-trip**：Chapter State 某状态 → `GetProgressSnapshot()` → `SaveManager.SaveAsync(sd)` → 文件 → `SaveManager.LoadAsync() → ChapterStateManager.Init(loaded, sameMeta)` → 结果 `IsUnlocked` / `IsCompleted` / `PuzzleState` 每字段与原 runtime 一致。
- [ ] **AC-5 Init 幂等语义**：
  - (a) `Init(null, meta)` 等价于 `Init(emptyIChapterProgress, meta)`——Chapter 1 解锁、首 puzzle Idle、其余皆 Locked。
  - (b) 对同一 `(save, meta)` 输入两次调用 `Init`，第二次不再 throw、不保留前一次的事件状态（覆盖式重新初始化是合法的）；两次调用后的 `GetProgressSnapshot()` 结果相等（字段 by 字段）。
- [ ] **AC-6 事件驱动 Auto-save**：新 `AutoSaveTrigger` 类订阅 `IChapterStateEvent.OnChapterComplete` 与 `OnGameComplete`。收到事件时调用 `SaveManager.SaveAsync(...)` 一次；**不**订阅 `OnPuzzleStateChanged`（太频繁）与 `OnChapterUnlocked`（由 `OnChapterComplete` 级联，已覆盖）。订阅在构造时 Add、在 `Dispose` 时 Remove（ADR-027 §3）。
- [ ] **AC-7 Boot 顺序契约**：新增的集成测试驱动如下序列：构造 `SaveManager` → `await LoadAsync()` → 构造 `ChapterStateManager` → `Init(loadedData, meta)` → `saveManager.RegisterProgressProvider(() => mgr.GetProgressSnapshot())` → 订阅 auto-save。反序（先 Init 后 Load）或跳步骤（未 Register 就 SaveAsync）的场景由测试断言其降级行为（前者：Init 拿到 null → 新游戏；后者：save file 的 chapter 段 = 前次 load 值）。
- [ ] **AC-8 Provider 早调用安全**：在 `ChapterStateManager.Init` 之前调用 `GetProgressSnapshot()` 返回**非 null** 的 "empty snapshot"（三数组皆空）——不抛 `NullReferenceException`。与 AC-7 末尾分支一致。

---

## Implementation Notes

*Aligned to ADR-027 §1/§3 + ADR-008 §4 + ADR-001 §UniTask:*

### 1. 新 `ChapterProgressSnapshot`（放 `Assets/GameScripts/HotFix/GameLogic/ChapterState/`）

```csharp
// Assets/GameScripts/HotFix/GameLogic/ChapterState/ChapterProgressSnapshot.cs
using System;

namespace GameLogic
{
    /// <summary>
    /// Immutable, read-only projection of ChapterStateManager's internal state,
    /// suitable for serialization by SaveManager. Sole implementation of
    /// <see cref="IChapterProgress"/> that ChapterState module exposes to
    /// the rest of the game (ADR-008 §4 — interface-only boundary).
    /// </summary>
    public sealed class ChapterProgressSnapshot : IChapterProgress
    {
        public int[] UnlockedChapterIds { get; }
        public int[] CompletedChapterIds { get; }
        public PuzzleProgressEntry[] PuzzleEntries { get; }

        public ChapterProgressSnapshot(
            int[] unlockedChapterIds,
            int[] completedChapterIds,
            PuzzleProgressEntry[] puzzleEntries)
        {
            UnlockedChapterIds = unlockedChapterIds ?? Array.Empty<int>();
            CompletedChapterIds = completedChapterIds ?? Array.Empty<int>();
            PuzzleEntries = puzzleEntries ?? Array.Empty<PuzzleProgressEntry>();
        }

        /// <summary>
        /// AC-8 empty snapshot — returned when caller invokes GetProgressSnapshot
        /// before Init. All three arrays are non-null, zero-length.
        /// </summary>
        public static ChapterProgressSnapshot Empty { get; } = new ChapterProgressSnapshot(
            Array.Empty<int>(), Array.Empty<int>(), Array.Empty<PuzzleProgressEntry>());
    }
}
```

### 2. `ChapterStateManager.GetProgressSnapshot()` 新方法（D2=[B] LINQ）

```csharp
using System.Linq; // 新增 using

// 在 ChapterStateManager 内部
/// <summary>
/// Story-005 AC-1 / AC-8: build a read-only snapshot of current runtime
/// progress for Save System serialization. Synchronous, in-memory only
/// (zero I/O), ≤ 1ms for the MVP 5×3 puzzle layout.
/// Returns <see cref="ChapterProgressSnapshot.Empty"/> before Init (AC-8).
/// </summary>
public IChapterProgress GetProgressSnapshot()
{
    if (!_initialized || _chapters == null)
        return ChapterProgressSnapshot.Empty;

    var unlocked = _chapters.Where(c => c.IsUnlocked).Select(c => c.ChapterId).ToArray();
    var completed = _chapters.Where(c => c.IsCompleted).Select(c => c.ChapterId).ToArray();
    var puzzles = _chapters
        .Where(c => c.Puzzles != null)
        .SelectMany(c => c.Puzzles.Where(p => p != null).Select(p => new PuzzleProgressEntry
        {
            ChapterId = c.ChapterId,
            PuzzleId = p.PuzzleId,
            StateOrdinal = (int)p.State,
        }))
        .ToArray();

    return new ChapterProgressSnapshot(unlocked, completed, puzzles);
}
```

注意：
- `PuzzleProgressEntry` 使用 `StateOrdinal`（int）而非 `State`（enum）以匹配现有序列化层字段名（见 `SaveData.cs` 第 35 行）。
- LINQ 一次性分配三个小数组，命中"单次 < 1ms"且不在 hot path，符合 Guardrail。

### 3. `SaveManager.RegisterProgressProvider` + `SaveAsync` hook（D1=[A] SaveManager 自持）

```csharp
// SaveManager.cs 追加字段
private Func<IChapterProgress> _chapterProgressProvider;

/// <summary>
/// Story-005 AC-2: register a delegate supplying the current
/// <see cref="IChapterProgress"/>. Called during <see cref="SaveAsync"/> to
/// populate the chapter segment of <see cref="SaveData"/>. Passing <c>null</c>
/// clears the provider (subsequent saves keep the previously-loaded chapter
/// data; do NOT zero the segment).
/// </summary>
public void RegisterProgressProvider(Func<IChapterProgress> provider)
{
    _chapterProgressProvider = provider;
}

// SaveAsync(SaveData data) 头部追加：
if (_chapterProgressProvider != null)
{
    var snap = _chapterProgressProvider.Invoke() ?? ChapterProgressSnapshot.Empty;
    data.chapterProgress.unlockedChapterIds = snap.UnlockedChapterIds;
    data.chapterProgress.completedChapterIds = snap.CompletedChapterIds;
    data.chapterProgress.puzzleEntries = snap.PuzzleEntries;
}
```

注意：将现有 `SaveAsync(SaveData data)` 的 `data.lastSaveTimestampUtc = ...` 一行**之前**插入上述 provider 注入——确保时间戳反映"即将写入的"这份数据。

### 4. 新 `AutoSaveTrigger`（放 `Assets/GameScripts/HotFix/GameLogic/ChapterState/`，D4=[C] 独立类）

```csharp
// Assets/GameScripts/HotFix/GameLogic/ChapterState/AutoSaveTrigger.cs
using System;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Story-005 AC-6: listens to <see cref="IChapterStateEvent.OnChapterComplete"/>
    /// and <see cref="IChapterStateEvent.OnGameComplete"/> and triggers
    /// <see cref="SaveManager.SaveAsync"/>. Single-responsibility listener;
    /// independent from <see cref="ChapterStateEventBridge"/> (which owns the
    /// sender side of IChapterStateEvent).
    /// </summary>
    public sealed class AutoSaveTrigger : IDisposable
    {
        private readonly SaveManager _saveManager;
        private readonly Func<SaveData> _currentSaveDataProvider;
        private bool _disposed;

        public AutoSaveTrigger(SaveManager saveManager, Func<SaveData> currentSaveDataProvider)
        {
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            _currentSaveDataProvider = currentSaveDataProvider
                ?? throw new ArgumentNullException(nameof(currentSaveDataProvider));

            GameEvent.AddEventListener<int>(
                IChapterStateEvent_Event.OnChapterComplete, OnChapterComplete);
            GameEvent.AddEventListener(
                IChapterStateEvent_Event.OnGameComplete, OnGameComplete);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            GameEvent.RemoveEventListener<int>(
                IChapterStateEvent_Event.OnChapterComplete, OnChapterComplete);
            GameEvent.RemoveEventListener(
                IChapterStateEvent_Event.OnGameComplete, OnGameComplete);
        }

        private void OnChapterComplete(int chapterId) => TriggerSave($"Chapter {chapterId} complete");

        private void OnGameComplete() => TriggerSave("Game complete");

        private void TriggerSave(string reason)
        {
            var data = _currentSaveDataProvider.Invoke();
            if (data == null)
            {
                Debug.LogWarning($"[AutoSaveTrigger] No SaveData available to persist ({reason}).");
                return;
            }
            _saveManager.SaveAsync(data).Forget();
        }
    }
}
```

注意：
- `_currentSaveDataProvider` 提供当前 in-memory `SaveData` 对象（不是快照）—— 触发时会被 `SaveAsync` 内部用 provider 更新 chapter 段并落盘。
- `.Forget()` 让 auto-save fire-and-forget，不阻塞玩家操作；`SaveManager.SaveAsync` 已有 `SemaphoreSlim` 串行写，避免并发 corruption。

### 5. Boot wire-up 伪代码（由集成测试或 `ProcedureMain` 消费）

```csharp
// Step 11: Save System
var saveManager = new SaveManager();
var loaded = await saveManager.LoadAsync(); // null on fresh game

// Step 12: Chapter State
var chapterMgr = new ChapterStateManager();
chapterMgr.Init(loaded, BuildPuzzleMetaStub());

// Wire: provider (SaveManager ← ChapterState)
saveManager.RegisterProgressProvider(() => chapterMgr.GetProgressSnapshot());

// Wire: sender (ChapterStateManager → IChapterStateEvent) — S2-03
ChapterStateEventBridge.Attach(chapterMgr);

// Wire: listener (IChapterStateEvent → SaveManager)
var autoSave = new AutoSaveTrigger(saveManager, () => loaded ?? new SaveData());
```

`BuildPuzzleMetaStub()`：与 S2-01/S2-02 一致的硬编码 MVP 5 章节 × 3 puzzle 布局；Luban 接入留给后续 story。

### 6. `using System.Linq` 单次引入

`ChapterStateManager.cs` 顶部 using 区追加 `using System.Linq;`。仅 `GetProgressSnapshot` 内使用。

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- `ProcedureMain` 正式 boot 步骤挂接（由 Application / Boot epic 负责；本 story 仅在集成测试里用伪代码演示顺序契约）。
- Save System Story 001：JSON schema 定义（`SaveData.cs` 已落地）。
- Save System Story 002：Atomic write + CRC32（S1-10 ✅）。
- Save System Story 004（Auto-save 的防抖 / 节流 / retry 策略）：本 story 仅保证"事件触发一次 → SaveAsync 一次"；更复杂的节流留给专用 story。
- Luban `TbChapter` / `TbPuzzle` 接入：puzzleMeta 继续 stub（S2-01/02 先例）。

---

## QA Test Cases

所有测试位于 `Assets/Tests/EditMode/ChapterState/SaveIntegrationTests.cs`。复用 S2-03 的 TENGINE-SG-002 fixture 模式。

- **AC-1/AC-8 Snapshot 基本**：
  - Given: `new ChapterStateManager()` 未 Init
  - When: `GetProgressSnapshot()`
  - Then: 返回 non-null，三数组均零长度（`ChapterProgressSnapshot.Empty`）
  - Given: Init(null, meta 5x3) + `OnPuzzleComplete(1,1)`
  - When: `GetProgressSnapshot()`
  - Then: `UnlockedChapterIds = [1]`, `CompletedChapterIds = []`, `PuzzleEntries` 含 15 条，`(1,1,Complete) / (1,2,Idle) / (1,3,Locked) / (2..5, *, Locked)`

- **AC-2 Provider 注册 + SaveAsync 填字段**：
  - Given: `SaveManager` + `SaveData sd = new SaveData()` + `mgr.OnPuzzleComplete(1,1)`
  - When: `saveManager.RegisterProgressProvider(() => mgr.GetProgressSnapshot()); await saveManager.SaveAsync(sd)`
  - Then: 读回磁盘的 `sd.chapterProgress.unlockedChapterIds = [1]` 等；delegate 未注册时 SaveAsync 不覆盖字段（保持前次值）
  - Edge: provider 返回 `null` → SaveAsync 注入 `Empty`（三数组零长度）但**不**抛

- **AC-3 解耦 grep 静态检查**（可以用 reflection/文件扫描做）：
  - Given: `typeof(SaveManager).Assembly` 下所有 `Save/` 目录 `.cs` 源
  - When: scan
  - Then: 不含 `ChapterStateManager` 标识符字符串；反向同理
  - （作为测试很难；退一步可做编译期检查——若 `SaveManager` 添加 `using` 的 ChapterStateManager namespace 则编译失败）。**本 AC 改由 story 验收时人工 grep 验证；测试文件可省略。**

- **AC-4 Round-trip**（核心测试）：
  - Given: mgr Init + 完成 Ch.1 全部 puzzle + Ch.2 P1 complete
  - When: snapshot → save file → 新 mgr2 + 新 saveMgr2.LoadAsync() → mgr2.Init(loaded, sameMeta) → snapshot2
  - Then: snapshot2 字段 by 字段等于 snapshot1（`CollectionAssert.AreEqual`）

- **AC-5a 新游戏语义**：
  - Given: `mgr.Init(null, meta)` vs `mgr2.Init(new SaveData(), meta)`
  - When: 两者 `GetProgressSnapshot()`
  - Then: 字段 by 字段相等（Ch.1 unlocked; Ch.1 P1 Idle; 其余 Locked）

- **AC-5b Init 幂等覆盖**：
  - Given: mgr.Init + OnPuzzleComplete(1,1) + Init(sameSave, sameMeta)（再 Init 一次，模拟 hot reload）
  - When: `GetProgressSnapshot()`
  - Then: Ch.1 P1 回到 save 里记录的值（save 为空 → Idle）；第二次 Init 不 throw

- **AC-6 AutoSaveTrigger 监听**：
  - Given: `SaveManager` + `new AutoSaveTrigger(sm, () => sd)`（sd 为一个 `SaveData` 实例）
  - When: `GameEvent.Send<int>(IChapterStateEvent_Event.OnChapterComplete, 1)`
  - Then: `SaveAsync` 被触发一次（通过订阅 `SaveAsync` 内部的 side-effect 标记，如 `sd.lastSaveTimestampUtc != 0`，或用 mock `SaveManager` 子类记录 call count）
  - When: OnPuzzleStateChanged 被 Send
  - Then: `SaveAsync` **不**被触发（只订阅了 ChapterComplete/GameComplete）
  - When: `trigger.Dispose()` + 再 Send OnChapterComplete
  - Then: `SaveAsync` 不再被触发

- **AC-7 Boot 顺序集成**（end-to-end）：
  - Given: 伪 boot pipeline（`SaveManager` → `LoadAsync()` → `ChapterStateManager.Init(loaded, meta)` → `RegisterProgressProvider` → `AutoSaveTrigger` 构造）
  - When: 模拟完成 Ch.1 → `OnChapterComplete(1)` 派发（通过 `ChapterStateEventBridge.Attach` + `mgr.OnPuzzleComplete(1, lastPuzzle)`）
  - Then: Auto-save 触发；文件可被新 SaveManager `LoadAsync` 回读；回读后 Init 的新 mgr 状态 = 原 mgr 状态

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- `Assets/Tests/EditMode/ChapterState/SaveIntegrationTests.cs` — must exist and pass

**Status**: [ ] Not yet created

**Performance**: `GetProgressSnapshot` ≤ 1ms（MVP 5×3 布局，LINQ 一次性分配 3 个数组；无 I/O）。Auto-save `SaveAsync` 的 I/O 耗时由 S1-10 负责，不在本 story 预算范围。

---

## Dependencies

- **Depends on**:
  - Story 001（S1-11）`IChapterProgress` 接口 + `PuzzleProgressEntry` ✅
  - Story 002/003（S2-01/S2-02）`ChapterStateManager` + `PuzzleMeta` + callback hooks ✅
  - Story 004（S2-03）`IChapterStateEvent` + `ChapterStateEventBridge` ✅（本 story 的 `AutoSaveTrigger` 是 listener 端）
  - Save System S1-09/S1-10/S1-13 ✅（`SaveManager` + `SaveData : IChapterProgress` + `Async/Load/Save`）
  - ADR-008 Proposed（按 S2-01/02/03 先例 Proposed ADR 可驱动实现）
- **Unlocks**:
  - 全 chapter-state epic MVP 闭环（存/读回/进度恢复全打通）
  - 后续 story：`ProcedureMain` boot 步骤正式挂接；Luban `TbChapter`/`TbPuzzle` 接入
