// 该文件由Cursor 自动生成
using System;
using System.Linq;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Single Source of Truth for chapter and puzzle progress at runtime.
    /// Initialised from save data, reads static config from Luban TbChapter/TbPuzzle
    /// (stubbed as parameters until Luban integration in a later story).
    /// </summary>
    public class ChapterStateManager
    {
        public const int TotalChapters = 5;

        private ChapterProgress[] _chapters;
        private bool _initialized;
        private Func<int, string> _unlockConditionProvider;

        public bool IsInitialized => _initialized;

        /// <summary>
        /// Story-003 wire-up hook (S2-03 connects this to the TEngine
        /// <c>IChapterStateEvent</c> dispatcher). Invoked once per chapter,
        /// immediately after <see cref="ChapterProgress.IsCompleted"/> flips to true.
        /// <para>S2-02 itself never dispatches TEngine events — it only flips the
        /// data flag and invokes this callback. Keeping the callback as a plain
        /// <see cref="Action{T}"/> avoids coupling the data layer to TEngine's
        /// <c>[EventInterface]</c> pipeline (ADR-027) while still obeying
        /// "no cross-module C# events" (ADR-001) — this is explicit dependency
        /// injection inside a single module (ChapterState).</para>
        /// </summary>
        public Action<int> OnChapterCompletedCallback;

        /// <summary>
        /// Story-003 wire-up hook: invoked when a new chapter transitions from
        /// <see cref="ChapterProgress.IsUnlocked"/> <c>false</c> to <c>true</c>
        /// through <see cref="TryUnlockChapter"/>. Not fired for chapters
        /// already unlocked at <see cref="Init(IChapterProgress, PuzzleMeta[][])"/>
        /// time (AC-4 idempotency).
        /// </summary>
        public Action<int> OnChapterUnlockedCallback;

        /// <summary>
        /// Story-003 wire-up hook: invoked exactly once when Chapter 5 transitions
        /// to <see cref="ChapterProgress.IsCompleted"/>. No Chapter 6 unlock is
        /// attempted (AC-3).
        /// </summary>
        public Action OnGameCompletedCallback;

        /// <summary>
        /// Story-004 (S2-03) wire-up hook: fired on every <em>real</em> change
        /// of <see cref="PuzzleProgress.State"/>. Same-value assignments (e.g.
        /// <c>Active → Active</c>) are silently dropped at <see cref="SetPuzzleState"/>
        /// and do NOT invoke this callback (AC-3). Hook is routed to
        /// <see cref="IChapterStateEvent.OnPuzzleStateChanged"/> by
        /// <see cref="ChapterStateEventBridge"/>.
        /// <para>Fired on: (1) <see cref="Init(IChapterProgress, PuzzleMeta[][])"/>
        /// first-puzzle <c>Locked → Idle</c> promotion; (2) <see cref="SetPuzzleState"/>
        /// successful transitions; (3) <see cref="OnPuzzleComplete"/> both the
        /// <c>Complete</c> assignment and the next puzzle's <c>Locked → Idle</c>
        /// promotion.</para>
        /// </summary>
        public Action<int, int, PuzzleStateEnum> OnPuzzleStateChangedCallback;

        /// <summary>
        /// Legacy overload — <c>PuzzleOrder</c> defaults to <c>PuzzleId</c> (1..N per chapter).
        /// Kept for S1-11 compatibility; new callers should use the <see cref="PuzzleMeta"/> overload.
        /// </summary>
        public void Init(IChapterProgress saveData, int[] puzzlesPerChapter)
        {
            if (puzzlesPerChapter == null || puzzlesPerChapter.Length != TotalChapters)
                throw new ArgumentException(
                    $"puzzlesPerChapter must have exactly {TotalChapters} entries");

            var meta = new PuzzleMeta[TotalChapters][];
            for (int c = 0; c < TotalChapters; c++)
            {
                int count = puzzlesPerChapter[c];
                meta[c] = new PuzzleMeta[count];
                for (int p = 0; p < count; p++)
                {
                    int puzzleId = p + 1;
                    meta[c][p] = new PuzzleMeta(c + 1, puzzleId, puzzleId);
                }
            }

            Init(saveData, meta);
        }

        /// <summary>
        /// Story-003 overload: accept a custom unlock-condition provider
        /// (normally fed by <c>Tables.Instance.TbChapter.Get(id).UnlockCondition</c>
        /// in production, and by a plain <see cref="Dictionary{TKey,TValue}"/>
        /// or lambda in tests). Calls through to the main
        /// <see cref="Init(IChapterProgress, PuzzleMeta[][])"/> overload.
        /// <para>Passing <c>null</c> for <paramref name="unlockConditionProvider"/>
        /// is equivalent to the other overload: MVP default condition
        /// <c>"prev_chapter_complete"</c> applies to every chapter.</para>
        /// </summary>
        public void Init(
            IChapterProgress saveData,
            PuzzleMeta[][] puzzleMeta,
            Func<int, string> unlockConditionProvider)
        {
            _unlockConditionProvider = unlockConditionProvider ?? DefaultUnlockConditionProvider;
            Init(saveData, puzzleMeta);
        }

        /// <summary>
        /// Initialise runtime state using per-puzzle metadata sourced from Luban
        /// <c>TbPuzzle</c>. Call on the main thread only (SP-004).
        /// <para>
        /// Ordering rule: within each chapter, the puzzle with the smallest
        /// <see cref="PuzzleMeta.PuzzleOrder"/> starts in
        /// <see cref="PuzzleStateEnum.Idle"/> (if its chapter is unlocked),
        /// and every subsequent puzzle starts in <see cref="PuzzleStateEnum.Locked"/>.
        /// Completing puzzle N unlocks puzzle N+1 (see <see cref="OnPuzzleComplete"/>).
        /// </para>
        /// </summary>
        public void Init(IChapterProgress saveData, PuzzleMeta[][] puzzleMeta)
        {
            if (puzzleMeta == null || puzzleMeta.Length != TotalChapters)
                throw new ArgumentException(
                    $"puzzleMeta must have exactly {TotalChapters} chapter rows");

            _unlockConditionProvider ??= DefaultUnlockConditionProvider;

            _chapters = new ChapterProgress[TotalChapters];

            for (int i = 0; i < TotalChapters; i++)
            {
                int chapterId = i + 1;
                var chapterMeta = puzzleMeta[i] ?? Array.Empty<PuzzleMeta>();

                var puzzles = new PuzzleProgress[chapterMeta.Length];
                for (int p = 0; p < chapterMeta.Length; p++)
                {
                    var m = chapterMeta[p];
                    puzzles[p] = new PuzzleProgress
                    {
                        PuzzleId = m.PuzzleId,
                        PuzzleOrder = m.PuzzleOrder,
                        State = PuzzleStateEnum.Locked
                    };
                }

                _chapters[i] = new ChapterProgress
                {
                    ChapterId = chapterId,
                    IsUnlocked = (chapterId == 1),
                    IsCompleted = false,
                    Puzzles = puzzles
                };
            }

            ApplySaveData(saveData);

            // AC-1: first puzzle of every UNLOCKED chapter starts Idle (if still Locked
            // after save-data override). Puzzles that save data has already advanced
            // (e.g. Complete / PerfectMatch) keep their loaded state.
            for (int i = 0; i < TotalChapters; i++)
            {
                var ch = _chapters[i];
                if (!ch.IsUnlocked) continue;
                var first = FindFirstByOrder(ch);
                if (first != null && first.State == PuzzleStateEnum.Locked)
                {
                    first.State = PuzzleStateEnum.Idle;
                    OnPuzzleStateChangedCallback?.Invoke(
                        ch.ChapterId, first.PuzzleId, PuzzleStateEnum.Idle);
                }
            }

            _initialized = true;
        }

        /// <summary>
        /// Returns the <see cref="ChapterProgress"/> for the given chapter ID (1-based).
        /// Returns null for invalid IDs instead of throwing.
        /// </summary>
        public ChapterProgress GetChapterProgress(int chapterId)
        {
            int idx = chapterId - 1;
            if (idx < 0 || idx >= TotalChapters || _chapters == null)
                return null;
            return _chapters[idx];
        }

        /// <summary>
        /// Returns the current <see cref="PuzzleStateEnum"/> for a specific puzzle.
        /// Returns <see cref="PuzzleStateEnum.Locked"/> for invalid IDs.
        /// </summary>
        public PuzzleStateEnum GetPuzzleState(int chapterId, int puzzleId)
        {
            var chapter = GetChapterProgress(chapterId);
            if (chapter?.Puzzles == null)
                return PuzzleStateEnum.Locked;

            var puzzle = FindPuzzle(chapter, puzzleId);
            return puzzle?.State ?? PuzzleStateEnum.Locked;
        }

        /// <summary>
        /// Story-005 AC-1 / AC-8: build a read-only <see cref="IChapterProgress"/>
        /// snapshot of current runtime state for Save System serialization.
        /// Synchronous, in-memory only (zero I/O); single call is ≤ 1ms for the
        /// MVP 5×3 puzzle layout (AC-1 perf budget).
        /// <para>Returns <see cref="ChapterProgressSnapshot.Empty"/> when called
        /// before <see cref="Init(IChapterProgress, int[])"/> /
        /// <see cref="Init(IChapterProgress, PuzzleMeta[][])"/> (AC-8 — no
        /// <c>NullReferenceException</c>).</para>
        /// </summary>
        public IChapterProgress GetProgressSnapshot()
        {
            if (!_initialized || _chapters == null)
                return ChapterProgressSnapshot.Empty;

            int[] unlocked = _chapters
                .Where(c => c != null && c.IsUnlocked)
                .Select(c => c.ChapterId)
                .ToArray();

            int[] completed = _chapters
                .Where(c => c != null && c.IsCompleted)
                .Select(c => c.ChapterId)
                .ToArray();

            PuzzleProgressEntry[] puzzles = _chapters
                .Where(c => c != null && c.Puzzles != null)
                .SelectMany(c => c.Puzzles
                    .Where(p => p != null)
                    .Select(p => new PuzzleProgressEntry
                    {
                        ChapterId = c.ChapterId,
                        PuzzleId = p.PuzzleId,
                        StateOrdinal = (int)p.State,
                    }))
                .ToArray();

            return new ChapterProgressSnapshot(unlocked, completed, puzzles);
        }

        /// <summary>
        /// Story-002 AC-5: returns the currently active puzzle of a chapter —
        /// the first puzzle (by <c>PuzzleOrder</c> ascending) in
        /// <see cref="PuzzleStateEnum.Idle"/>. Returns <c>null</c> when:
        /// chapter is missing, chapter is locked, or no puzzle is Idle
        /// (e.g. chapter complete, or player is mid-puzzle in Active/NearMatch).
        /// </summary>
        public PuzzleProgress GetActivePuzzle(int chapterId)
        {
            var chapter = GetChapterProgress(chapterId);
            if (chapter == null || !chapter.IsUnlocked || chapter.Puzzles == null)
                return null;

            PuzzleProgress active = null;
            for (int i = 0; i < chapter.Puzzles.Length; i++)
            {
                var p = chapter.Puzzles[i];
                if (p == null || p.State != PuzzleStateEnum.Idle) continue;
                if (active == null || p.PuzzleOrder < active.PuzzleOrder)
                    active = p;
            }
            return active;
        }

        /// <summary>
        /// Story-002 AC-2: authoritative entry point for puzzle completion.
        /// Transitions puzzle <paramref name="puzzleId"/> to <see cref="PuzzleStateEnum.Complete"/>
        /// and advances the next puzzle (by <c>PuzzleOrder+1</c>) from
        /// <see cref="PuzzleStateEnum.Locked"/> to <see cref="PuzzleStateEnum.Idle"/>.
        /// No-op (with warning) if the puzzle is already <c>Complete</c> — AC-3 irreversibility.
        /// </summary>
        public void OnPuzzleComplete(int chapterId, int puzzleId)
        {
            var chapter = GetChapterProgress(chapterId);
            if (chapter?.Puzzles == null)
            {
                Debug.LogWarning(
                    $"[ChapterState] OnPuzzleComplete: invalid chapterId={chapterId}");
                return;
            }

            var puzzle = FindPuzzle(chapter, puzzleId);
            if (puzzle == null)
            {
                Debug.LogWarning(
                    $"[ChapterState] OnPuzzleComplete: invalid puzzleId={puzzleId} in chapter {chapterId}");
                return;
            }

            if (puzzle.State == PuzzleStateEnum.Complete)
            {
                Debug.LogWarning(
                    $"[ChapterState] Puzzle {chapterId}/{puzzleId} already Complete — ignoring.");
                return;
            }

            puzzle.State = PuzzleStateEnum.Complete;
            OnPuzzleStateChangedCallback?.Invoke(
                chapterId, puzzleId, PuzzleStateEnum.Complete);

            int nextOrder = puzzle.PuzzleOrder + 1;
            var next = FindByOrder(chapter, nextOrder);
            if (next != null && next.State == PuzzleStateEnum.Locked)
            {
                next.State = PuzzleStateEnum.Idle;
                OnPuzzleStateChangedCallback?.Invoke(
                    chapterId, next.PuzzleId, PuzzleStateEnum.Idle);
            }

            CheckChapterCompletion(chapterId);
        }

        /// <summary>
        /// Story-003 AC-1 / AC-3 / AC-4 / AC-7: verify that every puzzle in
        /// <paramref name="chapterId"/> is <see cref="PuzzleStateEnum.Complete"/>.
        /// If so, flip <see cref="ChapterProgress.IsCompleted"/> to <c>true</c>,
        /// invoke <see cref="OnChapterCompletedCallback"/>, and attempt to unlock
        /// the next chapter (or fire <see cref="OnGameCompletedCallback"/> when
        /// chapter <see cref="TotalChapters"/> completes).
        /// <para>Called automatically from the last line of
        /// <see cref="OnPuzzleComplete"/>; external callers may invoke it directly
        /// (e.g. on save-data restoration). It is idempotent — re-calling on an
        /// already-<c>IsCompleted</c> chapter is a no-op (AC-4 / AC-7).</para>
        /// </summary>
        public void CheckChapterCompletion(int chapterId)
        {
            var chapter = GetChapterProgress(chapterId);
            if (chapter?.Puzzles == null) return;

            if (chapter.IsCompleted) return;

            for (int i = 0; i < chapter.Puzzles.Length; i++)
            {
                var p = chapter.Puzzles[i];
                if (p == null || p.State != PuzzleStateEnum.Complete) return;
            }

            chapter.IsCompleted = true;
            OnChapterCompletedCallback?.Invoke(chapterId);

            int nextId = chapterId + 1;
            if (nextId > TotalChapters)
            {
                OnGameCompletedCallback?.Invoke();
                return;
            }

            TryUnlockChapter(nextId);
        }

        /// <summary>
        /// Story-003 AC-2 / AC-5 / AC-6: attempt to unlock the given chapter
        /// according to its configured <c>UnlockCondition</c> (MVP:
        /// <c>"prev_chapter_complete"</c>). Already-unlocked chapters are a
        /// no-op (AC-4 idempotency); unknown condition strings are rejected
        /// with a warning (AC-5 edge); prev-chapter not yet complete is
        /// rejected silently (AC-2 edge — callers that try to "manually"
        /// unlock a chapter out of sequence get <c>false</c> back and no
        /// state change).
        /// <para>This is the single authoritative path that writes
        /// <see cref="ChapterProgress.IsUnlocked"/> to <c>true</c> for
        /// <paramref name="chapterId"/> &gt; 1 post-<see cref="Init(IChapterProgress, PuzzleMeta[][])"/>.
        /// Save-data restoration in <c>ApplySaveData</c> is the other writer.</para>
        /// </summary>
        /// <returns><c>true</c> if this call actually unlocked the chapter;
        /// <c>false</c> if it was a no-op or the condition rejected.</returns>
        public bool TryUnlockChapter(int chapterId)
        {
            var chapter = GetChapterProgress(chapterId);
            if (chapter == null)
            {
                Debug.LogWarning(
                    $"[ChapterState] TryUnlockChapter: invalid chapterId={chapterId}");
                return false;
            }

            if (chapter.IsUnlocked) return false;

            var provider = _unlockConditionProvider ?? DefaultUnlockConditionProvider;
            string condition = provider.Invoke(chapterId);
            if (!EvaluateUnlockCondition(chapterId, condition)) return false;

            chapter.IsUnlocked = true;
            OnChapterUnlockedCallback?.Invoke(chapterId);
            return true;
        }

        /// <summary>
        /// Story-003 AC-5: evaluate an <c>UnlockCondition</c> string against
        /// current chapter state. MVP recognises <c>"prev_chapter_complete"</c>
        /// only; unknown strings return <c>false</c> and log a warning.
        /// Future condition types (e.g. <c>"puzzle_count&gt;=3"</c>,
        /// <c>"narrative_flag:intro_seen"</c>) add new cases to this switch.
        /// </summary>
        private bool EvaluateUnlockCondition(int chapterId, string condition)
        {
            switch (condition)
            {
                case "prev_chapter_complete":
                    if (chapterId <= 1) return true;
                    var prev = GetChapterProgress(chapterId - 1);
                    return prev != null && prev.IsCompleted;

                default:
                    Debug.LogWarning(
                        $"[ChapterState] Unknown UnlockCondition=\"{condition}\" for chapter {chapterId} — refusing to unlock.");
                    return false;
            }
        }

        private static string DefaultUnlockConditionProvider(int chapterId) =>
            "prev_chapter_complete";

        /// <summary>
        /// Story-002 AC-3/AC-4: guarded state setter.
        /// Terminal states enforce irreversibility:
        /// <list type="bullet">
        ///   <item><see cref="PuzzleStateEnum.Complete"/> rejects every transition.</item>
        ///   <item><see cref="PuzzleStateEnum.PerfectMatch"/> and
        ///   <see cref="PuzzleStateEnum.AbsenceAccepted"/> may only advance to
        ///   <see cref="PuzzleStateEnum.Complete"/> (ADR-014).</item>
        /// </list>
        /// Any rejected call is a no-op and logs a warning.
        /// </summary>
        /// <returns>true if the transition was applied, false if rejected.</returns>
        public bool SetPuzzleState(int chapterId, int puzzleId, PuzzleStateEnum newState)
        {
            var chapter = GetChapterProgress(chapterId);
            if (chapter?.Puzzles == null)
            {
                Debug.LogWarning(
                    $"[ChapterState] SetPuzzleState: invalid chapterId={chapterId}");
                return false;
            }

            var puzzle = FindPuzzle(chapter, puzzleId);
            if (puzzle == null)
            {
                Debug.LogWarning(
                    $"[ChapterState] SetPuzzleState: invalid puzzleId={puzzleId} in chapter {chapterId}");
                return false;
            }

            if (!Enum.IsDefined(typeof(PuzzleStateEnum), newState))
            {
                Debug.LogWarning(
                    $"[ChapterState] SetPuzzleState: undefined state {(int)newState} rejected.");
                return false;
            }

            var current = puzzle.State;

            // Story-004 AC-3 (ADR-027): same-value assignment is a silent no-op.
            // No warning, no state change, no OnPuzzleStateChanged dispatch. Placed
            // BEFORE terminal checks so Complete → Complete is also silently accepted.
            if (current == newState) return true;

            if (current == PuzzleStateEnum.Complete)
            {
                Debug.LogWarning(
                    $"[ChapterState] Puzzle {chapterId}/{puzzleId} is Complete — state change to {newState} rejected (irreversible).");
                return false;
            }

            if (IsTerminalState(current) && newState != PuzzleStateEnum.Complete)
            {
                Debug.LogWarning(
                    $"[ChapterState] Puzzle {chapterId}/{puzzleId} is {current} — only transition to Complete is allowed (rejected {newState}).");
                return false;
            }

            puzzle.State = newState;
            OnPuzzleStateChangedCallback?.Invoke(chapterId, puzzleId, newState);
            return true;
        }

        /// <summary>
        /// Terminal states per ADR-014: once entered, the only legal exit is
        /// <see cref="PuzzleStateEnum.Complete"/>.
        /// </summary>
        private static bool IsTerminalState(PuzzleStateEnum state) =>
            state == PuzzleStateEnum.PerfectMatch ||
            state == PuzzleStateEnum.AbsenceAccepted ||
            state == PuzzleStateEnum.Complete;

        private static PuzzleProgress FindPuzzle(ChapterProgress chapter, int puzzleId)
        {
            var arr = chapter.Puzzles;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] != null && arr[i].PuzzleId == puzzleId)
                    return arr[i];
            }
            return null;
        }

        private static PuzzleProgress FindByOrder(ChapterProgress chapter, int order)
        {
            var arr = chapter.Puzzles;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] != null && arr[i].PuzzleOrder == order)
                    return arr[i];
            }
            return null;
        }

        private static PuzzleProgress FindFirstByOrder(ChapterProgress chapter)
        {
            if (chapter.Puzzles == null || chapter.Puzzles.Length == 0) return null;
            PuzzleProgress first = null;
            for (int i = 0; i < chapter.Puzzles.Length; i++)
            {
                var p = chapter.Puzzles[i];
                if (p == null) continue;
                if (first == null || p.PuzzleOrder < first.PuzzleOrder)
                    first = p;
            }
            return first;
        }

        private void ApplySaveData(IChapterProgress saveData)
        {
            if (saveData == null) return;

            if (saveData.UnlockedChapterIds != null)
            {
                foreach (int id in saveData.UnlockedChapterIds)
                {
                    var ch = GetChapterProgress(id);
                    if (ch != null) ch.IsUnlocked = true;
                }
            }

            if (saveData.CompletedChapterIds != null)
            {
                foreach (int id in saveData.CompletedChapterIds)
                {
                    var ch = GetChapterProgress(id);
                    if (ch != null)
                    {
                        ch.IsUnlocked = true;
                        ch.IsCompleted = true;
                    }
                }
            }

            if (saveData.PuzzleEntries != null)
            {
                foreach (var entry in saveData.PuzzleEntries)
                {
                    var ch = GetChapterProgress(entry.ChapterId);
                    if (ch?.Puzzles == null) continue;

                    var puzzle = FindPuzzle(ch, entry.PuzzleId);
                    if (puzzle == null) continue;

                    var state = entry.State;
                    if (!Enum.IsDefined(typeof(PuzzleStateEnum), state))
                        state = PuzzleStateEnum.Locked;

                    puzzle.State = state;
                }
            }
        }
    }
}
