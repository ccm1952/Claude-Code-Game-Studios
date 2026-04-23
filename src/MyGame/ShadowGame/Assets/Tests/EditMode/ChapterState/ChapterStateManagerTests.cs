// 该文件由Cursor 自动生成
using System;
using NUnit.Framework;
using GameLogic;

namespace ShadowGame.Tests.EditMode.ChapterState
{
    [TestFixture]
    public class ChapterStateManagerTests
    {
        private ChapterStateManager _mgr;
        private static readonly int[] DefaultPuzzleCounts = { 3, 3, 3, 3, 3 };

        [SetUp]
        public void SetUp()
        {
            _mgr = new ChapterStateManager();
        }

        // --- AC-1: Fresh init — Chapter 1 unlocked, others locked ---
        [Test]
        public void Init_NullSaveData_Chapter1Unlocked_OthersLocked()
        {
            _mgr.Init(null, DefaultPuzzleCounts);

            var ch1 = _mgr.GetChapterProgress(1);
            Assert.IsTrue(ch1.IsUnlocked);
            Assert.IsFalse(ch1.IsCompleted);

            for (int i = 2; i <= 5; i++)
            {
                var ch = _mgr.GetChapterProgress(i);
                Assert.IsFalse(ch.IsUnlocked, $"Chapter {i} should be locked");
            }
        }

        [Test]
        public void Init_NullSaveData_AllPuzzlesLocked()
        {
            _mgr.Init(null, DefaultPuzzleCounts);

            for (int c = 1; c <= 5; c++)
            {
                for (int p = 1; p <= 3; p++)
                {
                    Assert.AreEqual(PuzzleStateEnum.Locked, _mgr.GetPuzzleState(c, p),
                        $"Ch{c} Puzzle{p} should be Locked");
                }
            }
        }

        // --- AC-2: Save data overrides ---
        [Test]
        public void Init_SaveData_AppliesUnlockedAndCompleted()
        {
            var save = new StubChapterProgress
            {
                UnlockedChapterIds = new[] { 1, 2 },
                CompletedChapterIds = new[] { 1 },
                PuzzleEntries = new[]
                {
                    new PuzzleProgressEntry { ChapterId = 1, PuzzleId = 1, StateOrdinal = (int)PuzzleStateEnum.Complete },
                    new PuzzleProgressEntry { ChapterId = 1, PuzzleId = 2, StateOrdinal = (int)PuzzleStateEnum.PerfectMatch }
                }
            };

            _mgr.Init(save, DefaultPuzzleCounts);

            Assert.IsTrue(_mgr.GetChapterProgress(1).IsCompleted);
            Assert.IsTrue(_mgr.GetChapterProgress(2).IsUnlocked);
            Assert.IsFalse(_mgr.GetChapterProgress(3).IsUnlocked);

            Assert.AreEqual(PuzzleStateEnum.Complete, _mgr.GetPuzzleState(1, 1));
            Assert.AreEqual(PuzzleStateEnum.PerfectMatch, _mgr.GetPuzzleState(1, 2));
            Assert.AreEqual(PuzzleStateEnum.Locked, _mgr.GetPuzzleState(1, 3));
        }

        // --- AC-2 edge case: invalid chapter ID silently ignored ---
        [Test]
        public void Init_SaveData_InvalidChapterId_SilentlyIgnored()
        {
            var save = new StubChapterProgress
            {
                UnlockedChapterIds = new[] { 99 },
                CompletedChapterIds = Array.Empty<int>(),
                PuzzleEntries = Array.Empty<PuzzleProgressEntry>()
            };

            Assert.DoesNotThrow(() => _mgr.Init(save, DefaultPuzzleCounts));
            Assert.IsTrue(_mgr.GetChapterProgress(1).IsUnlocked);
        }

        // --- AC-4: GetChapterProgress returns null for invalid IDs ---
        [Test]
        public void GetChapterProgress_InvalidId_ReturnsNull()
        {
            _mgr.Init(null, DefaultPuzzleCounts);

            Assert.IsNull(_mgr.GetChapterProgress(0));
            Assert.IsNull(_mgr.GetChapterProgress(6));
            Assert.IsNull(_mgr.GetChapterProgress(-1));
        }

        // --- AC-5: GetPuzzleState returns Locked for invalid IDs ---
        [Test]
        public void GetPuzzleState_InvalidIds_ReturnsLocked()
        {
            _mgr.Init(null, DefaultPuzzleCounts);

            Assert.AreEqual(PuzzleStateEnum.Locked, _mgr.GetPuzzleState(99, 1));
            Assert.AreEqual(PuzzleStateEnum.Locked, _mgr.GetPuzzleState(1, 99));
        }

        // --- Init idempotent: calling twice resets to save data ---
        [Test]
        public void Init_CalledTwice_ResetsState()
        {
            var save = new StubChapterProgress
            {
                UnlockedChapterIds = new[] { 1, 2, 3 },
                CompletedChapterIds = Array.Empty<int>(),
                PuzzleEntries = Array.Empty<PuzzleProgressEntry>()
            };
            _mgr.Init(save, DefaultPuzzleCounts);
            Assert.IsTrue(_mgr.GetChapterProgress(3).IsUnlocked);

            _mgr.Init(null, DefaultPuzzleCounts);
            Assert.IsFalse(_mgr.GetChapterProgress(3).IsUnlocked, "Second init should reset");
        }

        // --- PuzzleProgressEntry.State property maps correctly ---
        [Test]
        public void PuzzleProgressEntry_StateProperty_MapsOrdinal()
        {
            var entry = new PuzzleProgressEntry { StateOrdinal = 6 };
            Assert.AreEqual(PuzzleStateEnum.Complete, entry.State);

            entry.State = PuzzleStateEnum.NearMatch;
            Assert.AreEqual(3, entry.StateOrdinal);
        }

        // --- Invalid ordinal in save → defaults to Locked ---
        [Test]
        public void Init_InvalidPuzzleStateOrdinal_DefaultsToLocked()
        {
            var save = new StubChapterProgress
            {
                UnlockedChapterIds = Array.Empty<int>(),
                CompletedChapterIds = Array.Empty<int>(),
                PuzzleEntries = new[]
                {
                    new PuzzleProgressEntry { ChapterId = 1, PuzzleId = 1, StateOrdinal = 255 }
                }
            };

            _mgr.Init(save, DefaultPuzzleCounts);
            Assert.AreEqual(PuzzleStateEnum.Locked, _mgr.GetPuzzleState(1, 1));
        }

        // --- puzzlesPerChapter validation ---
        [Test]
        public void Init_WrongPuzzleCountLength_Throws()
        {
            Assert.Throws<ArgumentException>(() => _mgr.Init(null, new[] { 3, 3 }));
        }

        // --- Stub ---
        private class StubChapterProgress : IChapterProgress
        {
            public int[] UnlockedChapterIds { get; set; }
            public int[] CompletedChapterIds { get; set; }
            public PuzzleProgressEntry[] PuzzleEntries { get; set; }
        }
    }
}
