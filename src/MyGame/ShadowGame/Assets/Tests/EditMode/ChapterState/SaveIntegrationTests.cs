// 该文件由Cursor 自动生成
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GameLogic;
using TEngine;

namespace ShadowGame.Tests.EditMode.ChapterState
{
    /// <summary>
    /// S2-04 — Bidirectional Integration between Chapter State and Save System
    /// (ADR-008 §4 + ADR-027 §1/§3).
    /// Validates:
    /// <list type="bullet">
    /// <item>AC-1 / AC-8: <see cref="ChapterStateManager.GetProgressSnapshot"/>
    /// before / after <c>Init</c>.</item>
    /// <item>AC-2: <see cref="SaveManager.RegisterProgressProvider"/> +
    /// <see cref="SaveManager.SaveAsync"/> provider hook semantics.</item>
    /// <item>AC-4: Full Save → Load → Init round-trip preserves every field.</item>
    /// <item>AC-5: Init idempotency (null ≡ empty, double-Init safe).</item>
    /// <item>AC-6: <see cref="AutoSaveTrigger"/> only fires on
    /// <c>OnChapterComplete</c> / <c>OnGameComplete</c>; Dispose blocks further
    /// dispatches.</item>
    /// <item>AC-7: Boot-order pipeline (SaveManager → LoadAsync →
    /// ChapterStateManager.Init → RegisterProvider → AutoSaveTrigger) survives
    /// a full simulated chapter-complete flow.</item>
    /// </list>
    ///
    /// <para>Setup follows TENGINE-SG-002 precedent (see
    /// <see cref="StateEventsTests"/>) to keep the Source Generator dispatchers
    /// wired without booting <c>GameEventHelper.Init()</c>.</para>
    /// </summary>
    [TestFixture]
    public class SaveIntegrationTests
    {
        private ChapterStateManager _mgr;
        private SaveManager _saveMgr;

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

        private static PuzzleMeta[][] DefaultMeta() => BuildMeta(3, 3, 3, 3, 3);

        private sealed class FakeSave : IChapterProgress
        {
            public int[] UnlockedChapterIds { get; set; } = Array.Empty<int>();
            public int[] CompletedChapterIds { get; set; } = Array.Empty<int>();
            public PuzzleProgressEntry[] PuzzleEntries { get; set; } = Array.Empty<PuzzleProgressEntry>();
        }

        [SetUp]
        public void SetUp()
        {
            // TENGINE-SG-002 workaround: reset + re-wire event dispatcher so
            // AutoSaveTrigger / event-driven tests work without GameEventHelper.
            GameEvent.EventMgr.Init();
            _ = new IChapterStateEvent_Gen(GameEvent.EventMgr.GetDispatcher());

            _mgr = new ChapterStateManager();
            _saveMgr = new SaveManager();

            // Ensure a clean persistent-data state between tests — AC-4 / AC-7
            // do real disk round-trips.
            _saveMgr.DeleteSave();
        }

        [TearDown]
        public void TearDown()
        {
            _saveMgr?.DeleteSave();
        }

        // ────────── AC-1 / AC-8: snapshot basics ──────────

        [Test]
        public void AC8_GetSnapshot_BeforeInit_ReturnsEmptyNonNull()
        {
            var snap = _mgr.GetProgressSnapshot();

            Assert.IsNotNull(snap, "AC-8: snapshot before Init must be non-null.");
            Assert.IsNotNull(snap.UnlockedChapterIds);
            Assert.IsNotNull(snap.CompletedChapterIds);
            Assert.IsNotNull(snap.PuzzleEntries);
            Assert.AreEqual(0, snap.UnlockedChapterIds.Length);
            Assert.AreEqual(0, snap.CompletedChapterIds.Length);
            Assert.AreEqual(0, snap.PuzzleEntries.Length);
        }

        [Test]
        public void AC1_GetSnapshot_AfterInit_ReflectsRuntimeState()
        {
            _mgr.Init((IChapterProgress)null, DefaultMeta());
            _mgr.OnPuzzleComplete(1, 1);

            var snap = _mgr.GetProgressSnapshot();

            CollectionAssert.AreEqual(new[] { 1 }, snap.UnlockedChapterIds);
            CollectionAssert.AreEqual(Array.Empty<int>(), snap.CompletedChapterIds);
            Assert.AreEqual(15, snap.PuzzleEntries.Length, "5×3 MVP layout.");

            // Spot-check a few key puzzles.
            var byKey = new Dictionary<(int ch, int pz), PuzzleStateEnum>();
            foreach (var e in snap.PuzzleEntries)
                byKey[(e.ChapterId, e.PuzzleId)] = e.State;

            Assert.AreEqual(PuzzleStateEnum.Complete, byKey[(1, 1)]);
            Assert.AreEqual(PuzzleStateEnum.Idle, byKey[(1, 2)]);
            Assert.AreEqual(PuzzleStateEnum.Locked, byKey[(1, 3)]);
            Assert.AreEqual(PuzzleStateEnum.Locked, byKey[(2, 1)]);
            Assert.AreEqual(PuzzleStateEnum.Locked, byKey[(5, 3)]);
        }

        // ────────── AC-2: provider hook in SaveAsync ──────────

        [UnityTest]
        public IEnumerator AC2_RegisterProvider_PopulatesSaveDataFields() => UniTask.ToCoroutine(async () =>
        {
            _mgr.Init((IChapterProgress)null, DefaultMeta());
            _mgr.OnPuzzleComplete(1, 1);

            _saveMgr.RegisterProgressProvider(() => _mgr.GetProgressSnapshot());

            var sd = new SaveData();
            await _saveMgr.SaveAsync(sd);

            CollectionAssert.AreEqual(new[] { 1 }, sd.chapterProgress.unlockedChapterIds);
            Assert.AreEqual(15, sd.chapterProgress.puzzleEntries.Length);
            Assert.AreNotEqual(0L, sd.lastSaveTimestampUtc, "SaveAsync must stamp the time.");
        });

        [UnityTest]
        public IEnumerator AC2_NullProvider_KeepsExistingChapterSegment() => UniTask.ToCoroutine(async () =>
        {
            // Seed SaveData with pre-existing chapter values that the test
            // expects to survive a save with no provider registered.
            var sd = new SaveData();
            sd.chapterProgress.unlockedChapterIds = new[] { 1, 2 };
            sd.chapterProgress.completedChapterIds = new[] { 1 };
            sd.chapterProgress.puzzleEntries = new[]
            {
                new PuzzleProgressEntry { ChapterId = 1, PuzzleId = 1, StateOrdinal = (int)PuzzleStateEnum.Complete }
            };

            // No RegisterProgressProvider call.
            await _saveMgr.SaveAsync(sd);

            CollectionAssert.AreEqual(new[] { 1, 2 }, sd.chapterProgress.unlockedChapterIds,
                "null provider must not zero the chapter segment (AC-2).");
            CollectionAssert.AreEqual(new[] { 1 }, sd.chapterProgress.completedChapterIds);
            Assert.AreEqual(1, sd.chapterProgress.puzzleEntries.Length);
        });

        [UnityTest]
        public IEnumerator AC2_ProviderReturningNull_WritesEmptySegments() => UniTask.ToCoroutine(async () =>
        {
            var sd = new SaveData();
            sd.chapterProgress.unlockedChapterIds = new[] { 1, 2, 3 }; // should be cleared

            _saveMgr.RegisterProgressProvider(() => null);
            await _saveMgr.SaveAsync(sd);

            Assert.AreEqual(0, sd.chapterProgress.unlockedChapterIds.Length);
            Assert.AreEqual(0, sd.chapterProgress.completedChapterIds.Length);
            Assert.AreEqual(0, sd.chapterProgress.puzzleEntries.Length);
        });

        // ────────── AC-4: round-trip through real disk ──────────

        [UnityTest]
        public IEnumerator AC4_Roundtrip_SaveLoadInit_PreservesAllFields() => UniTask.ToCoroutine(async () =>
        {
            _mgr.Init((IChapterProgress)null, DefaultMeta());

            // Drive state: complete all of chapter 1 + first puzzle of chapter 2.
            _mgr.OnPuzzleComplete(1, 1);
            _mgr.OnPuzzleComplete(1, 2);
            _mgr.OnPuzzleComplete(1, 3);
            _mgr.OnPuzzleComplete(2, 1);

            _saveMgr.RegisterProgressProvider(() => _mgr.GetProgressSnapshot());

            var original = new SaveData();
            await _saveMgr.SaveAsync(original);

            var originalSnap = _mgr.GetProgressSnapshot();

            // Reload: fresh manager + fresh save-manager pair pulling from disk.
            var mgr2 = new ChapterStateManager();
            var saveMgr2 = new SaveManager();
            var loaded = await saveMgr2.LoadAsync();

            Assert.IsNotNull(loaded, "SaveAsync → LoadAsync round-trip must yield non-null data.");

            mgr2.Init(loaded, DefaultMeta());
            var reloadedSnap = mgr2.GetProgressSnapshot();

            CollectionAssert.AreEqual(originalSnap.UnlockedChapterIds, reloadedSnap.UnlockedChapterIds);
            CollectionAssert.AreEqual(originalSnap.CompletedChapterIds, reloadedSnap.CompletedChapterIds);

            Assert.AreEqual(originalSnap.PuzzleEntries.Length, reloadedSnap.PuzzleEntries.Length);
            // Compare ordinals pair-wise (both arrays are built in same deterministic order).
            for (int i = 0; i < originalSnap.PuzzleEntries.Length; i++)
            {
                var a = originalSnap.PuzzleEntries[i];
                var b = reloadedSnap.PuzzleEntries[i];
                Assert.AreEqual(a.ChapterId, b.ChapterId);
                Assert.AreEqual(a.PuzzleId, b.PuzzleId);
                Assert.AreEqual(a.StateOrdinal, b.StateOrdinal,
                    $"Puzzle ({a.ChapterId},{a.PuzzleId}) state drifted across round-trip.");
            }
        });

        // ────────── AC-5: Init idempotency ──────────

        [Test]
        public void AC5a_InitNull_EquivalentTo_InitEmptySave()
        {
            var mgrA = new ChapterStateManager();
            mgrA.Init((IChapterProgress)null, DefaultMeta());

            var mgrB = new ChapterStateManager();
            mgrB.Init(new FakeSave(), DefaultMeta());

            var snapA = mgrA.GetProgressSnapshot();
            var snapB = mgrB.GetProgressSnapshot();

            CollectionAssert.AreEqual(snapA.UnlockedChapterIds, snapB.UnlockedChapterIds);
            CollectionAssert.AreEqual(snapA.CompletedChapterIds, snapB.CompletedChapterIds);
            Assert.AreEqual(snapA.PuzzleEntries.Length, snapB.PuzzleEntries.Length);
            for (int i = 0; i < snapA.PuzzleEntries.Length; i++)
                Assert.AreEqual(snapA.PuzzleEntries[i].StateOrdinal, snapB.PuzzleEntries[i].StateOrdinal);
        }

        [Test]
        public void AC5b_InitTwice_SameInput_SameSnapshot()
        {
            var save = new FakeSave
            {
                UnlockedChapterIds = new[] { 1, 2 },
                CompletedChapterIds = new[] { 1 },
                PuzzleEntries = new[]
                {
                    new PuzzleProgressEntry { ChapterId = 1, PuzzleId = 1, StateOrdinal = (int)PuzzleStateEnum.Complete },
                    new PuzzleProgressEntry { ChapterId = 1, PuzzleId = 2, StateOrdinal = (int)PuzzleStateEnum.Complete },
                    new PuzzleProgressEntry { ChapterId = 1, PuzzleId = 3, StateOrdinal = (int)PuzzleStateEnum.Complete },
                }
            };
            var meta = DefaultMeta();

            _mgr.Init(save, meta);
            var snap1 = _mgr.GetProgressSnapshot();

            Assert.DoesNotThrow(() => _mgr.Init(save, meta), "Second Init must not throw.");
            var snap2 = _mgr.GetProgressSnapshot();

            CollectionAssert.AreEqual(snap1.UnlockedChapterIds, snap2.UnlockedChapterIds);
            CollectionAssert.AreEqual(snap1.CompletedChapterIds, snap2.CompletedChapterIds);
            Assert.AreEqual(snap1.PuzzleEntries.Length, snap2.PuzzleEntries.Length);
            for (int i = 0; i < snap1.PuzzleEntries.Length; i++)
                Assert.AreEqual(snap1.PuzzleEntries[i].StateOrdinal, snap2.PuzzleEntries[i].StateOrdinal);
        }

        // ────────── AC-6: AutoSaveTrigger ──────────

        [Test]
        public void AC6_AutoSaveTrigger_FiresOn_ChapterComplete_AndGameComplete_Only()
        {
            int providerCalls = 0;
            var trigger = new AutoSaveTrigger(_saveMgr, () =>
            {
                providerCalls++;
                return null; // skip disk; we only care about trigger firing.
            });

            try
            {
                // Expect warning logs for each "provider returned null" path.
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

                GameEvent.Send<int>(IChapterStateEvent_Event.OnChapterComplete, 1);
                Assert.AreEqual(1, providerCalls, "OnChapterComplete must trigger one save attempt.");

                GameEvent.Send<int>(IChapterStateEvent_Event.OnChapterComplete, 2);
                Assert.AreEqual(2, providerCalls);

                GameEvent.Send(IChapterStateEvent_Event.OnGameComplete);
                Assert.AreEqual(3, providerCalls, "OnGameComplete must also trigger.");

                // These must NOT fire the trigger (per AC-6 explicit scope).
                GameEvent.Send<int, int, PuzzleStateEnum>(
                    IChapterStateEvent_Event.OnPuzzleStateChanged, 1, 1, PuzzleStateEnum.Complete);
                GameEvent.Send<int>(IChapterStateEvent_Event.OnChapterUnlocked, 3);
                Assert.AreEqual(3, providerCalls,
                    "OnPuzzleStateChanged / OnChapterUnlocked must not trigger auto-save.");
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
                trigger.Dispose();
            }
        }

        [Test]
        public void AC6_AutoSaveTrigger_Dispose_StopsFurtherDispatches()
        {
            int providerCalls = 0;
            var trigger = new AutoSaveTrigger(_saveMgr, () =>
            {
                providerCalls++;
                return null;
            });

            try
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

                GameEvent.Send<int>(IChapterStateEvent_Event.OnChapterComplete, 1);
                Assert.AreEqual(1, providerCalls);

                trigger.Dispose();

                GameEvent.Send<int>(IChapterStateEvent_Event.OnChapterComplete, 2);
                GameEvent.Send(IChapterStateEvent_Event.OnGameComplete);

                Assert.AreEqual(1, providerCalls, "Dispose must unsubscribe all handlers.");

                // Double-dispose should be a no-op.
                Assert.DoesNotThrow(trigger.Dispose);
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }
        }

        [Test]
        public void AC6_AutoSaveTrigger_NullArgs_Throw()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AutoSaveTrigger(null, () => null));
            Assert.Throws<ArgumentNullException>(
                () => new AutoSaveTrigger(_saveMgr, null));
        }

        // ────────── AC-7: Full boot-order E2E ──────────

        [UnityTest]
        public IEnumerator AC7_BootPipeline_EndToEnd_SavesAndReloadsCorrectly() => UniTask.ToCoroutine(async () =>
        {
            // === Boot step 11: SaveManager + LoadAsync (no prior save → null) ===
            var initialLoaded = await _saveMgr.LoadAsync();
            Assert.IsNull(initialLoaded, "Fresh test env should start with no save file.");

            // === Boot step 12: ChapterState.Init with loaded save ===
            _mgr.Init(initialLoaded, DefaultMeta());

            // === Wire: provider + sender bridge + listener auto-save ===
            _saveMgr.RegisterProgressProvider(() => _mgr.GetProgressSnapshot());
            ChapterStateEventBridge.Attach(_mgr);

            var currentSave = new SaveData();
            var trigger = new AutoSaveTrigger(_saveMgr, () => currentSave);

            try
            {
                // Simulate Chapter 1 completion playthrough.
                _mgr.OnPuzzleComplete(1, 1);
                _mgr.OnPuzzleComplete(1, 2);
                _mgr.OnPuzzleComplete(1, 3); // fires OnChapterComplete(1) → AutoSaveTrigger → SaveAsync

                // SaveAsync is fired via .Forget(); wait for the write lock to
                // flush by issuing another SaveAsync and awaiting it.
                await _saveMgr.SaveAsync(currentSave);

                // === Reload in a fresh pipeline ===
                var saveMgr2 = new SaveManager();
                var reloaded = await saveMgr2.LoadAsync();
                Assert.IsNotNull(reloaded, "Chapter-complete auto-save must have produced a file.");

                var mgr2 = new ChapterStateManager();
                mgr2.Init(reloaded, DefaultMeta());

                Assert.IsTrue(mgr2.GetChapterProgress(1).IsCompleted, "Chapter 1 must be complete after reload.");
                Assert.IsTrue(mgr2.GetChapterProgress(2).IsUnlocked, "Chapter 2 must be unlocked after reload.");
                Assert.AreEqual(PuzzleStateEnum.Complete, mgr2.GetPuzzleState(1, 1));
                Assert.AreEqual(PuzzleStateEnum.Idle, mgr2.GetPuzzleState(2, 1),
                    "Chapter 2 first puzzle must be promoted to Idle on re-Init.");
            }
            finally
            {
                trigger.Dispose();
                ChapterStateEventBridge.Detach(_mgr);
            }
        });
    }
}
