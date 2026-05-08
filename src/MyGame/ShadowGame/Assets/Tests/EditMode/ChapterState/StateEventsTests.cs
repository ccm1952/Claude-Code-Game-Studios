// 该文件由Cursor 自动生成
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GameLogic;
using TEngine;

namespace ShadowGame.Tests.EditMode.ChapterState
{
    /// <summary>
    /// S2-03 — GameEvent Dispatch for Chapter/Puzzle State Changes (ADR-027).
    /// Validates: (1) <see cref="IChapterStateEvent"/> carries
    /// <see cref="EventInterfaceAttribute"/>; (2) the TEngine Source Generator
    /// produces <c>IChapterStateEvent_Gen</c> + <c>IChapterStateEvent_Event</c>;
    /// (3) <see cref="ChapterStateEventBridge"/> forwards all four
    /// <see cref="ChapterStateManager"/> callback hooks to the TEngine dispatcher
    /// so downstream listeners receive the expected interface method calls.
    ///
    /// <para>Setup follows TENGINE-SG-002 precedent: reset <c>GameEvent.EventMgr</c>
    /// and manually instantiate <c>IChapterStateEvent_Gen</c> to sidestep the
    /// empty <c>GameEventHelper.g.cs</c> produced in the test asmdef.</para>
    /// </summary>
    [TestFixture]
    public class StateEventsTests
    {
        private ChapterStateManager _mgr;

        // Captured events (ordered)
        private List<(int chapterId, int puzzleId, PuzzleStateEnum state)> _puzzleStateChanges;
        private List<int> _chapterCompletes;
        private List<int> _chapterUnlocks;
        private int _gameCompletes;

        // ────────── Helpers ──────────

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

        private sealed class FakeSave : IChapterProgress
        {
            public int[] UnlockedChapterIds { get; set; } = Array.Empty<int>();
            public int[] CompletedChapterIds { get; set; } = Array.Empty<int>();
            public PuzzleProgressEntry[] PuzzleEntries { get; set; } = Array.Empty<PuzzleProgressEntry>();
        }

        // ────────── Handlers ──────────

        private void OnPuzzleStateChangedHandler(int chapterId, int puzzleId, PuzzleStateEnum newState)
            => _puzzleStateChanges.Add((chapterId, puzzleId, newState));

        private void OnChapterCompleteHandler(int chapterId)
            => _chapterCompletes.Add(chapterId);

        private void OnChapterUnlockedHandler(int chapterId)
            => _chapterUnlocks.Add(chapterId);

        private void OnGameCompleteHandler()
            => _gameCompletes++;

        // ────────── Fixture lifecycle ──────────

        [SetUp]
        public void SetUp()
        {
            // TENGINE-SG-002 workaround — see GestureDispatchTests.cs for rationale.
            GameEvent.EventMgr.Init();
            _ = new IChapterStateEvent_Gen(GameEvent.EventMgr.GetDispatcher());

            _puzzleStateChanges = new List<(int, int, PuzzleStateEnum)>();
            _chapterCompletes = new List<int>();
            _chapterUnlocks = new List<int>();
            _gameCompletes = 0;

            GameEvent.AddEventListener<int, int, PuzzleStateEnum>(
                IChapterStateEvent_Event.OnPuzzleStateChanged, OnPuzzleStateChangedHandler);
            GameEvent.AddEventListener<int>(
                IChapterStateEvent_Event.OnChapterComplete, OnChapterCompleteHandler);
            GameEvent.AddEventListener<int>(
                IChapterStateEvent_Event.OnChapterUnlocked, OnChapterUnlockedHandler);
            GameEvent.AddEventListener(
                IChapterStateEvent_Event.OnGameComplete, OnGameCompleteHandler);

            _mgr = new ChapterStateManager();
            ChapterStateEventBridge.Attach(_mgr);
        }

        [TearDown]
        public void TearDown()
        {
            GameEvent.RemoveEventListener<int, int, PuzzleStateEnum>(
                IChapterStateEvent_Event.OnPuzzleStateChanged, OnPuzzleStateChangedHandler);
            GameEvent.RemoveEventListener<int>(
                IChapterStateEvent_Event.OnChapterComplete, OnChapterCompleteHandler);
            GameEvent.RemoveEventListener<int>(
                IChapterStateEvent_Event.OnChapterUnlocked, OnChapterUnlockedHandler);
            GameEvent.RemoveEventListener(
                IChapterStateEvent_Event.OnGameComplete, OnGameCompleteHandler);
        }

        // ────────── AC-1 / AC-2 structural ──────────

        [Test]
        public void AC1_IChapterStateEvent_HasEventInterfaceAttribute_GroupLogic()
        {
            var attr = typeof(IChapterStateEvent).GetCustomAttribute<EventInterfaceAttribute>();

            Assert.IsNotNull(attr,
                "IChapterStateEvent must be annotated with [EventInterface] (ADR-027 §1).");
            Assert.AreEqual(EEventGroup.GroupLogic, attr.EventGroup,
                "IChapterStateEvent belongs to GroupLogic (gameplay-layer broadcast).");
        }

        [Test]
        public void AC2_SourceGenerator_ProducesIChapterStateEvent_Gen()
        {
            var genType = typeof(IChapterStateEvent).Assembly
                .GetType("GameLogic.IChapterStateEvent_Gen");

            Assert.IsNotNull(genType,
                "SG must emit GameLogic.IChapterStateEvent_Gen (dispatcher forwarder).");
            Assert.IsTrue(typeof(IChapterStateEvent).IsAssignableFrom(genType),
                "_Gen must implement IChapterStateEvent.");
        }

        [Test]
        public void AC2_SourceGenerator_EmitsEventIdsForAllFourMethods()
        {
            var evtType = typeof(IChapterStateEvent).Assembly
                .GetType("GameLogic.IChapterStateEvent_Event");

            Assert.IsNotNull(evtType, "SG must emit GameLogic.IChapterStateEvent_Event static class.");

            foreach (var name in new[] {
                "OnPuzzleStateChanged", "OnChapterComplete", "OnChapterUnlocked", "OnGameComplete"
            })
            {
                var field = evtType.GetField(name, BindingFlags.Public | BindingFlags.Static);
                Assert.IsNotNull(field, $"IChapterStateEvent_Event.{name} (event id) must exist.");
                Assert.AreEqual(typeof(int), field.FieldType,
                    $"{name} must be a public static int id.");
            }
        }

        // ────────── AC-3 OnPuzzleStateChanged fires on every real change ──────────

        [Test]
        public void AC3_Init_FirstPuzzleIdlePromotion_DispatchesForChapter1Only()
        {
            _mgr.Init(new FakeSave(), BuildMeta(3, 3, 3, 3, 3));

            // Only Chapter 1 is unlocked at Init (no save-data unlocks beyond Ch1).
            Assert.AreEqual(1, _puzzleStateChanges.Count,
                "Exactly one Locked→Idle promotion at Init (first puzzle of Ch.1).");
            Assert.AreEqual((1, 1, PuzzleStateEnum.Idle), _puzzleStateChanges[0]);
        }

        [Test]
        public void AC3_SetPuzzleState_SuccessfulTransition_Dispatches()
        {
            _mgr.Init(new FakeSave(), BuildMeta(3, 3, 3, 3, 3));
            _puzzleStateChanges.Clear();

            bool applied = _mgr.SetPuzzleState(1, 1, PuzzleStateEnum.Active);

            Assert.IsTrue(applied);
            Assert.AreEqual(1, _puzzleStateChanges.Count);
            Assert.AreEqual((1, 1, PuzzleStateEnum.Active), _puzzleStateChanges[0]);
        }

        [Test]
        public void AC3_SetPuzzleState_SameValue_NoDispatch_ReturnsTrue()
        {
            _mgr.Init(new FakeSave(), BuildMeta(3, 3, 3, 3, 3));
            // Puzzle 1 is Idle after Init. Same-value assignment should no-op.
            _puzzleStateChanges.Clear();

            bool applied = _mgr.SetPuzzleState(1, 1, PuzzleStateEnum.Idle);

            Assert.IsTrue(applied, "Same-value assignment must return true (AC-3 silent no-op).");
            Assert.AreEqual(0, _puzzleStateChanges.Count,
                "Same-value assignment must NOT dispatch OnPuzzleStateChanged.");
        }

        [Test]
        public void AC3_OnPuzzleComplete_DispatchesCompleteThenNextIdleInOrder()
        {
            _mgr.Init(new FakeSave(), BuildMeta(3, 3, 3, 3, 3));
            _puzzleStateChanges.Clear();

            _mgr.OnPuzzleComplete(1, 1);

            Assert.AreEqual(2, _puzzleStateChanges.Count,
                "One Complete dispatch + one Locked→Idle promotion for next puzzle.");
            Assert.AreEqual((1, 1, PuzzleStateEnum.Complete), _puzzleStateChanges[0],
                "First dispatch is the current puzzle → Complete.");
            Assert.AreEqual((1, 2, PuzzleStateEnum.Idle), _puzzleStateChanges[1],
                "Second dispatch is the next puzzle → Idle (order preserved).");
        }

        [Test]
        public void AC3_OnPuzzleComplete_LastPuzzle_DispatchesCompleteOnly()
        {
            _mgr.Init(new FakeSave(), BuildMeta(2, 3, 3, 3, 3));
            _mgr.OnPuzzleComplete(1, 1);
            _puzzleStateChanges.Clear();

            _mgr.OnPuzzleComplete(1, 2); // last puzzle of Ch.1

            Assert.AreEqual(1, _puzzleStateChanges.Count,
                "Last puzzle of chapter: only Complete dispatch, no further Idle promotion.");
            Assert.AreEqual((1, 2, PuzzleStateEnum.Complete), _puzzleStateChanges[0]);
        }

        // ────────── AC-4 OnChapterComplete exactly once (idempotent) ──────────

        [Test]
        public void AC4_ChapterComplete_FiresExactlyOnce()
        {
            _mgr.Init(new FakeSave(), BuildMeta(2, 3, 3, 3, 3));

            _mgr.OnPuzzleComplete(1, 1);
            _mgr.OnPuzzleComplete(1, 2); // chapter 1 now complete

            Assert.AreEqual(1, _chapterCompletes.Count,
                "OnChapterComplete fires exactly once.");
            Assert.AreEqual(1, _chapterCompletes[0]);

            // Re-checking an already-completed chapter must NOT re-fire.
            _mgr.CheckChapterCompletion(1);

            Assert.AreEqual(1, _chapterCompletes.Count,
                "Re-checking a completed chapter does not re-dispatch.");
        }

        // ────────── AC-5 OnChapterUnlocked once, idempotent ──────────

        [Test]
        public void AC5_ChapterUnlocked_FiresOnce_NotOnReAttempt()
        {
            _mgr.Init(new FakeSave(), BuildMeta(1, 3, 3, 3, 3));

            _mgr.OnPuzzleComplete(1, 1); // Ch1 complete → Ch2 unlock

            Assert.AreEqual(1, _chapterUnlocks.Count);
            Assert.AreEqual(2, _chapterUnlocks[0]);

            // Calling TryUnlockChapter again on an already-unlocked chapter is a no-op.
            bool re = _mgr.TryUnlockChapter(2);
            Assert.IsFalse(re, "Re-unlocking an already-unlocked chapter returns false.");
            Assert.AreEqual(1, _chapterUnlocks.Count,
                "OnChapterUnlocked must not re-fire for an idempotent re-attempt.");
        }

        // ────────── AC-6 OnGameComplete (Ch.5 only; no Ch.6 unlock) ──────────

        [Test]
        public void AC6_Chapter5Complete_FiresGameComplete_NoChapter6Unlocked()
        {
            var save = new FakeSave
            {
                UnlockedChapterIds = new[] { 1, 2, 3, 4, 5 },
                CompletedChapterIds = new[] { 1, 2, 3, 4 }
            };
            _mgr.Init(save, BuildMeta(3, 3, 3, 3, 2));
            _chapterCompletes.Clear();
            _chapterUnlocks.Clear();
            _gameCompletes = 0;

            _mgr.OnPuzzleComplete(5, 1);
            _mgr.OnPuzzleComplete(5, 2);

            Assert.AreEqual(1, _chapterCompletes.Count, "Ch.5 completion fires once.");
            Assert.AreEqual(5, _chapterCompletes[0]);
            Assert.AreEqual(1, _gameCompletes, "OnGameComplete fires exactly once on Ch.5 completion.");
            Assert.AreEqual(0, _chapterUnlocks.Count,
                "OnChapterUnlocked must NOT fire after Ch.5 (no Chapter 6).");
        }

        // ────────── AC-8 Bridge Attach/Detach lifecycle ──────────

        [Test]
        public void AC8_Detach_StopsAllDispatch()
        {
            _mgr.Init(new FakeSave(), BuildMeta(2, 3, 3, 3, 3));
            _mgr.OnPuzzleComplete(1, 1); // accumulate a baseline
            Assert.Greater(_puzzleStateChanges.Count, 0,
                "Baseline: Attach was active and dispatched normally.");

            ChapterStateEventBridge.Detach(_mgr);

            _puzzleStateChanges.Clear();
            _chapterCompletes.Clear();
            _chapterUnlocks.Clear();
            _gameCompletes = 0;

            _mgr.OnPuzzleComplete(1, 2); // would normally complete chapter + unlock Ch2

            Assert.AreEqual(0, _puzzleStateChanges.Count,
                "After Detach: no OnPuzzleStateChanged dispatch.");
            Assert.AreEqual(0, _chapterCompletes.Count,
                "After Detach: no OnChapterComplete dispatch.");
            Assert.AreEqual(0, _chapterUnlocks.Count,
                "After Detach: no OnChapterUnlocked dispatch.");
            Assert.AreEqual(0, _gameCompletes,
                "After Detach: no OnGameComplete dispatch.");
        }

        [Test]
        public void AC8_Attach_IsNoOpForNullManager()
        {
            Assert.DoesNotThrow(() => ChapterStateEventBridge.Attach(null));
            Assert.DoesNotThrow(() => ChapterStateEventBridge.Detach(null));
        }

        // ────────── Integration: full Chapter 1 playthrough sequence ──────────

        [Test]
        public void Integration_Chapter1Playthrough_DispatchesExpectedSequenceInOrder()
        {
            _mgr.Init(new FakeSave(), BuildMeta(3, 3, 3, 3, 3));
            // Init already dispatched (1,1,Idle). Keep it.

            _mgr.OnPuzzleComplete(1, 1); // dispatches: (1,1,Complete), (1,2,Idle)
            _mgr.OnPuzzleComplete(1, 2); // dispatches: (1,2,Complete), (1,3,Idle)
            _mgr.OnPuzzleComplete(1, 3); // dispatches: (1,3,Complete)
                                          // then ChapterComplete(1) → UnlockChapter(2)
                                          // bridge must ALSO fire OnChapterUnlocked(2).

            var expected = new List<(int, int, PuzzleStateEnum)>
            {
                (1, 1, PuzzleStateEnum.Idle),     // Init first-puzzle promotion
                (1, 1, PuzzleStateEnum.Complete), // OnPuzzleComplete(1,1) — complete
                (1, 2, PuzzleStateEnum.Idle),     // OnPuzzleComplete(1,1) — next Idle
                (1, 2, PuzzleStateEnum.Complete), // OnPuzzleComplete(1,2) — complete
                (1, 3, PuzzleStateEnum.Idle),     // OnPuzzleComplete(1,2) — next Idle
                (1, 3, PuzzleStateEnum.Complete), // OnPuzzleComplete(1,3) — complete
            };

            CollectionAssert.AreEqual(expected, _puzzleStateChanges,
                "PuzzleStateChanged dispatch order must match the playthrough.");

            Assert.AreEqual(1, _chapterCompletes.Count);
            Assert.AreEqual(1, _chapterCompletes[0]);
            Assert.AreEqual(1, _chapterUnlocks.Count);
            Assert.AreEqual(2, _chapterUnlocks[0]);
            Assert.AreEqual(0, _gameCompletes, "Ch.1 completion must not trigger OnGameComplete.");
        }
    }
}
