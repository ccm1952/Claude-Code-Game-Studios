// 该文件由Cursor 自动生成
using NUnit.Framework;
using GameLogic;

namespace ShadowGame.Tests.EditMode.InputSystem
{
    [TestFixture]
    public class InputFilterTests
    {
        private InputFilter _filter;

        [SetUp]
        public void SetUp()
        {
            _filter = new InputFilter();
        }

        // --- TC-005-01: Basic whitelist filtering ---
        [Test]
        public void PushFilter_TapOnly_BlocksDrag()
        {
            _filter.PushFilter(new[] { GestureType.Tap });

            Assert.IsTrue(_filter.IsAllowed(GestureType.Tap));
            Assert.IsFalse(_filter.IsAllowed(GestureType.Drag));
        }

        // --- TC-005-02: Whitelisted gesture passes ---
        [Test]
        public void PushFilter_DragOnly_AllowsDrag_BlocksTap()
        {
            _filter.PushFilter(new[] { GestureType.Drag });

            Assert.IsTrue(_filter.IsAllowed(GestureType.Drag));
            Assert.IsFalse(_filter.IsAllowed(GestureType.Tap));
        }

        // --- TC-005-03: PushFilter overwrites previous ---
        [Test]
        public void PushFilter_Overwrites_PreviousFilter()
        {
            _filter.PushFilter(new[] { GestureType.Tap });
            _filter.PushFilter(new[] { GestureType.Rotate, GestureType.Pinch });

            Assert.IsFalse(_filter.IsAllowed(GestureType.Tap), "Tap should be blocked after overwrite");
            Assert.IsTrue(_filter.IsAllowed(GestureType.Rotate));
            Assert.IsTrue(_filter.IsAllowed(GestureType.Pinch));

            var active = _filter.ActiveFilterGestures;
            Assert.AreEqual(2, active.Length);
        }

        // --- TC-005-04: PopFilter restores all gestures ---
        [Test]
        public void PopFilter_RestoresAllGestures()
        {
            _filter.PushFilter(new[] { GestureType.Tap });
            _filter.PopFilter();

            Assert.IsFalse(_filter.IsFiltered);
            Assert.IsTrue(_filter.IsAllowed(GestureType.Drag));
            Assert.IsTrue(_filter.IsAllowed(GestureType.Tap));
            Assert.IsTrue(_filter.IsAllowed(GestureType.Rotate));
            Assert.AreEqual(0, _filter.ActiveFilterGestures.Length);
        }

        // --- TC-005-06: Deep copy protection ---
        [Test]
        public void PushFilter_DeepCopies_AllowedGestures()
        {
            var arr = new[] { GestureType.Tap };
            _filter.PushFilter(arr);

            arr[0] = GestureType.Drag;

            Assert.AreEqual(GestureType.Tap, _filter.ActiveFilterGestures[0],
                "External mutation must not affect the internal filter");
        }

        // --- TC-005-07: Empty whitelist blocks everything ---
        [Test]
        public void PushFilter_EmptyArray_BlocksAllGestures()
        {
            _filter.PushFilter(new GestureType[0]);

            Assert.IsTrue(_filter.IsFiltered);
            Assert.IsFalse(_filter.IsAllowed(GestureType.Tap));
            Assert.IsFalse(_filter.IsAllowed(GestureType.Drag));
            Assert.IsFalse(_filter.IsAllowed(GestureType.Rotate));
            Assert.IsFalse(_filter.IsAllowed(GestureType.Pinch));
            Assert.IsFalse(_filter.IsAllowed(GestureType.LightDrag));
        }

        // --- TC-005-08: Full interop sequence (Filter + Blocker) ---
        [Test]
        public void FullInterop_Filter_And_Blocker_Sequence()
        {
            var blocker = new InputBlocker();

            // Step 1: PushFilter([Drag])
            _filter.PushFilter(new[] { GestureType.Drag });
            Assert.IsTrue(_filter.IsFiltered);
            Assert.IsTrue(_filter.IsAllowed(GestureType.Drag));
            Assert.IsFalse(_filter.IsAllowed(GestureType.Tap));

            // Step 2: PushBlocker → all input suppressed
            blocker.PushBlocker("Narr");
            Assert.IsTrue(blocker.IsBlocked);

            // Step 3: Even whitelisted Drag is blocked when Blocker active
            // (at pipeline level, Blocker gate runs first)

            // Step 4: PopBlocker → Filter still active
            blocker.PopBlocker("Narr");
            Assert.IsFalse(blocker.IsBlocked);
            Assert.IsTrue(_filter.IsFiltered);
            Assert.IsTrue(_filter.IsAllowed(GestureType.Drag));
            Assert.IsFalse(_filter.IsAllowed(GestureType.Tap));

            // Step 5: PopFilter → all gestures pass
            _filter.PopFilter();
            Assert.IsFalse(_filter.IsFiltered);
            Assert.IsTrue(_filter.IsAllowed(GestureType.Tap));
            Assert.IsTrue(_filter.IsAllowed(GestureType.Drag));
        }

        // --- Additional: Initial state ---
        [Test]
        public void InitialState_NotFiltered_AllGesturesAllowed()
        {
            Assert.IsFalse(_filter.IsFiltered);
            Assert.IsTrue(_filter.IsAllowed(GestureType.Tap));
            Assert.IsTrue(_filter.IsAllowed(GestureType.Drag));
            Assert.IsTrue(_filter.IsAllowed(GestureType.Rotate));
            Assert.IsTrue(_filter.IsAllowed(GestureType.Pinch));
            Assert.IsTrue(_filter.IsAllowed(GestureType.LightDrag));
        }

        // --- Additional: Null allowedGestures treated as empty ---
        [Test]
        public void PushFilter_Null_TreatedAsEmptyFilter()
        {
            _filter.PushFilter(null);

            Assert.IsTrue(_filter.IsFiltered);
            Assert.IsFalse(_filter.IsAllowed(GestureType.Tap));
        }
    }
}
