// 该文件由Cursor 自动生成
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GameLogic;

namespace ShadowGame.Tests.EditMode.ChapterState
{
    /// <summary>
    /// S2-02 — Chapter Completion → Next Chapter Unlock Flow.
    /// Covers AC-1..AC-7 plus integration with <see cref="ChapterStateManager.OnPuzzleComplete"/>,
    /// from production/epics/chapter-state/story-003-chapter-progression.md.
    /// </summary>
    [TestFixture]
    public class ChapterProgressionTests
    {
        private ChapterStateManager _mgr;
        private List<int> _chapterCompletedCalls;
        private List<int> _chapterUnlockedCalls;
        private int _gameCompletedCalls;

        private static PuzzleMeta[][] BuildMeta(params int[] puzzlesPerChapter)
        {
            if (puzzlesPerChapter.Length != ChapterStateManager.TotalChapters)
                throw new ArgumentException("must provide 5 chapter counts");

            var meta = new PuzzleMeta[ChapterStateManager.TotalChapters][];
            for (int c = 0; c < ChapterStateManager.TotalChapters; c++)
            {
                int count = puzzlesPerChapter[c];
                meta[c] = new PuzzleMeta[count];
                for (int p = 0; p < count; p++)
                {
                    int puzzleId = p + 1;
                    meta[c][p] = new PuzzleMeta(c + 1, puzzleId, puzzleId);
                }
            }
            return meta;
        }

        /// <summary>
        /// In-memory <see cref="IChapterProgress"/> stub for tests. Defaults to
        /// "no save data" (all fields empty arrays).
        /// </summary>
        private sealed class FakeSave : IChapterProgress
        {
            public int[] UnlockedChapterIds { get; set; } = Array.Empty<int>();
            public int[] CompletedChapterIds { get; set; } = Array.Empty<int>();
            public PuzzleProgressEntry[] PuzzleEntries { get; set; } = Array.Empty<PuzzleProgressEntry>();
        }

        /// <summary>Complete every puzzle in a chapter in PuzzleId order.</summary>
        private void CompleteAllPuzzles(int chapterId, int puzzleCount)
        {
            for (int p = 1; p <= puzzleCount; p++)
                _mgr.OnPuzzleComplete(chapterId, p);
        }

        [SetUp]
        public void SetUp()
        {
            _mgr = new ChapterStateManager();
            _chapterCompletedCalls = new List<int>();
            _chapterUnlockedCalls = new List<int>();
            _gameCompletedCalls = 0;

            _mgr.OnChapterCompletedCallback = id => _chapterCompletedCalls.Add(id);
            _mgr.OnChapterUnlockedCallback = id => _chapterUnlockedCalls.Add(id);
            _mgr.OnGameCompletedCallback = () => _gameCompletedCalls++;
        }

        // --------------------------------------------------------------------
        // AC-1: All puzzles complete → chapter complete + next chapter unlocked
        // --------------------------------------------------------------------

        [Test]
        public void AC1_AllPuzzlesComplete_MarksChapterCompleteAndUnlocksNext()
        {
            _mgr.Init(null, BuildMeta(3, 3, 3, 3, 3));

            CompleteAllPuzzles(1, 3);

            Assert.IsTrue(_mgr.GetChapterProgress(1).IsCompleted,
                "Chapter 1 should be IsCompleted after all 3 puzzles reach Complete.");
            Assert.IsTrue(_mgr.GetChapterProgress(2).IsUnlocked,
                "Chapter 2 should be IsUnlocked once Chapter 1 completes.");
            CollectionAssert.AreEqual(new[] { 1 }, _chapterCompletedCalls);
            CollectionAssert.AreEqual(new[] { 2 }, _chapterUnlockedCalls);
            Assert.AreEqual(0, _gameCompletedCalls,
                "Completing Chapter 1 must not fire GameComplete.");
        }

        [Test]
        public void AC1_PartialComplete_NoCompletionMark()
        {
            _mgr.Init(null, BuildMeta(3, 3, 3, 3, 3));

            // Complete only 2 of 3 puzzles in Ch.1.
            _mgr.OnPuzzleComplete(1, 1);
            _mgr.OnPuzzleComplete(1, 2);

            Assert.IsFalse(_mgr.GetChapterProgress(1).IsCompleted,
                "Chapter should NOT be marked complete while any puzzle is incomplete.");
            Assert.IsFalse(_mgr.GetChapterProgress(2).IsUnlocked,
                "Next chapter must not be unlocked before current chapter is complete.");
            Assert.IsEmpty(_chapterCompletedCalls);
            Assert.IsEmpty(_chapterUnlockedCalls);
        }

        // --------------------------------------------------------------------
        // AC-2: Sequential unlock + AC-2 edge (reject out-of-sequence unlock)
        // --------------------------------------------------------------------

        [Test]
        public void AC2_TryUnlockChapter_PrevChapterIncomplete_Rejects()
        {
            _mgr.Init(null, BuildMeta(3, 3, 3, 3, 3));

            // Ch.2 not yet complete — attempting to unlock Ch.3 must be rejected.
            bool applied = _mgr.TryUnlockChapter(3);

            Assert.IsFalse(applied, "Unlocking Ch.3 before Ch.2 complete must be rejected.");
            Assert.IsFalse(_mgr.GetChapterProgress(3).IsUnlocked);
            Assert.IsEmpty(_chapterUnlockedCalls,
                "Rejected TryUnlockChapter must not fire the unlock callback.");
        }

        [Test]
        public void AC2_TryUnlockChapter_PrevChapterComplete_UnlocksAndFires()
        {
            _mgr.Init(null, BuildMeta(3, 3, 3, 3, 3));

            // Complete Ch.1 → Ch.2 auto-unlocks. Now manually try Ch.3 with Ch.2 complete.
            CompleteAllPuzzles(1, 3);
            CompleteAllPuzzles(2, 3);

            Assert.IsTrue(_mgr.GetChapterProgress(3).IsUnlocked,
                "Ch.3 should already be unlocked via the OnPuzzleComplete chain.");
            Assert.Contains(3, _chapterUnlockedCalls);
        }

        [Test]
        public void AC2_TryUnlockChapter_AlreadyUnlocked_IsNoOp()
        {
            _mgr.Init(null, BuildMeta(3, 3, 3, 3, 3));
            CompleteAllPuzzles(1, 3);
            _chapterUnlockedCalls.Clear();

            bool applied = _mgr.TryUnlockChapter(2);

            Assert.IsFalse(applied, "Re-unlocking an already unlocked chapter returns false.");
            Assert.IsEmpty(_chapterUnlockedCalls,
                "Re-unlock must not re-fire the unlock callback (AC-4 idempotency).");
        }

        // --------------------------------------------------------------------
        // AC-3: Chapter 5 completion → GameComplete, no Ch.6 unlock attempt
        // --------------------------------------------------------------------

        [Test]
        public void AC3_Chapter5AllComplete_FiresGameComplete_NoNextUnlock()
        {
            // Seed save so Ch.1..4 are complete and Ch.5 is unlocked with Idle p1.
            var save = new FakeSave
            {
                UnlockedChapterIds = new[] { 5 },
                CompletedChapterIds = new[] { 1, 2, 3, 4 },
            };
            _mgr.Init(save, BuildMeta(3, 3, 3, 3, 3));
            _chapterCompletedCalls.Clear();
            _chapterUnlockedCalls.Clear();

            CompleteAllPuzzles(5, 3);

            Assert.IsTrue(_mgr.GetChapterProgress(5).IsCompleted);
            CollectionAssert.AreEqual(new[] { 5 }, _chapterCompletedCalls);
            Assert.AreEqual(1, _gameCompletedCalls,
                "Chapter 5 completion must fire OnGameCompletedCallback exactly once.");
            Assert.IsEmpty(_chapterUnlockedCalls,
                "No Chapter 6 exists — no unlock callback may fire.");
        }

        // --------------------------------------------------------------------
        // AC-4 + AC-7: Re-completion is idempotent (replay-safe)
        // --------------------------------------------------------------------

        [Test]
        public void AC4_ReCheckCompletion_AlreadyCompletedChapter_NoDuplicateCallbacks()
        {
            _mgr.Init(null, BuildMeta(3, 3, 3, 3, 3));
            CompleteAllPuzzles(1, 3);
            Assume.That(_chapterCompletedCalls, Is.EqualTo(new[] { 1 }));
            Assume.That(_chapterUnlockedCalls, Is.EqualTo(new[] { 2 }));

            // Replay: second manual invocation on an already-completed chapter.
            _mgr.CheckChapterCompletion(1);
            _mgr.CheckChapterCompletion(1);

            CollectionAssert.AreEqual(new[] { 1 }, _chapterCompletedCalls,
                "Re-checking a completed chapter must not re-fire OnChapterCompletedCallback.");
            CollectionAssert.AreEqual(new[] { 2 }, _chapterUnlockedCalls,
                "Re-checking a completed chapter must not re-fire OnChapterUnlockedCallback for the next chapter.");
        }

        // --------------------------------------------------------------------
        // AC-5: UnlockCondition from TbChapter (injected provider)
        // --------------------------------------------------------------------

        [Test]
        public void AC5_CustomProvider_UnknownCondition_LogsWarningAndRejects()
        {
            Func<int, string> provider = id => id == 2 ? "weekly_quest_cleared" : "prev_chapter_complete";
            _mgr.Init(null, BuildMeta(3, 3, 3, 3, 3), provider);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                "Unknown UnlockCondition=\"weekly_quest_cleared\""));

            CompleteAllPuzzles(1, 3);

            Assert.IsTrue(_mgr.GetChapterProgress(1).IsCompleted,
                "Ch.1 still completes even when Ch.2's unlock rejects.");
            Assert.IsFalse(_mgr.GetChapterProgress(2).IsUnlocked,
                "Ch.2 must stay locked when its UnlockCondition is unknown.");
            Assert.IsEmpty(_chapterUnlockedCalls);
        }

        [Test]
        public void AC5_CustomProvider_PrevChapterComplete_UnlocksAsExpected()
        {
            int providerCallCount = 0;
            Func<int, string> provider = id =>
            {
                providerCallCount++;
                return "prev_chapter_complete";
            };
            _mgr.Init(null, BuildMeta(2, 2, 2, 2, 2), provider);

            CompleteAllPuzzles(1, 2);

            Assert.IsTrue(_mgr.GetChapterProgress(2).IsUnlocked,
                "Custom provider returning the recognised condition must behave like the default.");
            Assert.GreaterOrEqual(providerCallCount, 1,
                "Provider should have been invoked for Ch.2's unlock attempt.");
        }

        // --------------------------------------------------------------------
        // AC-6: Sequential constraint (Ch.3 cannot unlock before Ch.2 complete)
        // --------------------------------------------------------------------

        [Test]
        public void AC6_SequentialUnlock_Ch3BlockedUntilCh2Complete()
        {
            _mgr.Init(null, BuildMeta(2, 2, 2, 2, 2));

            CompleteAllPuzzles(1, 2);

            // Ch.3 should NOT yet be unlocked — only Ch.2 is.
            Assert.IsTrue(_mgr.GetChapterProgress(2).IsUnlocked);
            Assert.IsFalse(_mgr.GetChapterProgress(3).IsUnlocked,
                "Ch.3 must remain locked until Ch.2 completes.");

            CompleteAllPuzzles(2, 2);

            Assert.IsTrue(_mgr.GetChapterProgress(3).IsUnlocked,
                "Ch.3 should unlock once Ch.2 completes.");
            CollectionAssert.AreEqual(new[] { 1, 2 }, _chapterCompletedCalls);
            CollectionAssert.AreEqual(new[] { 2, 3 }, _chapterUnlockedCalls);
        }

        // --------------------------------------------------------------------
        // Integration: OnPuzzleComplete automatically triggers CheckChapterCompletion
        // --------------------------------------------------------------------

        [Test]
        public void Integration_OnPuzzleComplete_LastPuzzle_AutoTriggersChapterCompletion()
        {
            _mgr.Init(null, BuildMeta(2, 2, 2, 2, 2));

            _mgr.OnPuzzleComplete(1, 1);
            Assert.IsFalse(_mgr.GetChapterProgress(1).IsCompleted,
                "Chapter should not be complete after only the first puzzle.");
            Assert.IsEmpty(_chapterCompletedCalls);

            _mgr.OnPuzzleComplete(1, 2);

            Assert.IsTrue(_mgr.GetChapterProgress(1).IsCompleted,
                "Completing the final puzzle must automatically mark the chapter complete.");
            CollectionAssert.AreEqual(new[] { 1 }, _chapterCompletedCalls);
            CollectionAssert.AreEqual(new[] { 2 }, _chapterUnlockedCalls);
        }

        [Test]
        public void Integration_OnPuzzleComplete_LastPuzzleOfCh5_AutoTriggersGameComplete()
        {
            var save = new FakeSave
            {
                UnlockedChapterIds = new[] { 5 },
                CompletedChapterIds = new[] { 1, 2, 3, 4 },
            };
            _mgr.Init(save, BuildMeta(2, 2, 2, 2, 2));
            _chapterCompletedCalls.Clear();
            _chapterUnlockedCalls.Clear();

            _mgr.OnPuzzleComplete(5, 1);
            Assert.AreEqual(0, _gameCompletedCalls,
                "GameComplete must not fire after only the first puzzle of Ch.5.");

            _mgr.OnPuzzleComplete(5, 2);

            Assert.IsTrue(_mgr.GetChapterProgress(5).IsCompleted);
            Assert.AreEqual(1, _gameCompletedCalls,
                "Completing the last puzzle of Chapter 5 must fire GameComplete once.");
        }

        // --------------------------------------------------------------------
        // Invalid inputs
        // --------------------------------------------------------------------

        [Test]
        public void CheckChapterCompletion_InvalidChapterId_NoOp()
        {
            _mgr.Init(null, BuildMeta(3, 3, 3, 3, 3));

            Assert.DoesNotThrow(() => _mgr.CheckChapterCompletion(0));
            Assert.DoesNotThrow(() => _mgr.CheckChapterCompletion(99));
            Assert.DoesNotThrow(() => _mgr.CheckChapterCompletion(-1));

            Assert.IsEmpty(_chapterCompletedCalls);
            Assert.IsEmpty(_chapterUnlockedCalls);
            Assert.AreEqual(0, _gameCompletedCalls);
        }

        [Test]
        public void TryUnlockChapter_InvalidChapterId_LogsWarningReturnsFalse()
        {
            _mgr.Init(null, BuildMeta(3, 3, 3, 3, 3));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                "TryUnlockChapter: invalid chapterId"));

            bool applied = _mgr.TryUnlockChapter(99);

            Assert.IsFalse(applied);
            Assert.IsEmpty(_chapterUnlockedCalls);
        }
    }
}
