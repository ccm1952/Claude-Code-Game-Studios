// 该文件由Cursor 自动生成
using NUnit.Framework;
using UnityEngine;
using GameLogic;
using TEngine;

namespace ShadowGame.Tests.EditMode.InputSystem
{
    /// <summary>
    /// ADR-027 接口事件协议下的 <see cref="GestureDispatcher"/> 验收测试。
    /// 取代基于 <c>EventId.Evt_Gesture_*</c> 常量的旧测试；验证 Dispatch 正确
    /// 调用 <see cref="IGestureEvent"/> 接口上对应的方法，且保留 <see cref="GestureType.None"/>
    /// 的 no-op 语义。
    /// <para>
    /// 本测试通过注册 <c>IGestureEvent_Event</c> handler 验证派发路径，间接证明
    /// Source Generator 生成的事件 ID 与 handler 链路连通。
    /// </para>
    /// </summary>
    [TestFixture]
    public class GestureDispatchTests
    {
        private bool _tapFired;
        private bool _dragFired;
        private bool _rotateFired;
        private bool _pinchFired;
        private bool _lightDragFired;

        private GestureData _capturedTap;
        private GestureData _capturedDrag;
        private GestureData _capturedRotate;
        private GestureData _capturedPinch;
        private GestureData _capturedLightDrag;

        [SetUp]
        public void SetUp()
        {
            GameEventHelper.Init();

            _tapFired = _dragFired = _rotateFired = _pinchFired = _lightDragFired = false;

            GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnTap, OnTap);
            GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnDrag, OnDrag);
            GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnRotate, OnRotate);
            GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnPinch, OnPinch);
            GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnLightDrag, OnLightDrag);
        }

        [TearDown]
        public void TearDown()
        {
            GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnTap, OnTap);
            GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnDrag, OnDrag);
            GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnRotate, OnRotate);
            GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnPinch, OnPinch);
            GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnLightDrag, OnLightDrag);
        }

        private void OnTap(GestureData d)       { _tapFired = true;       _capturedTap = d; }
        private void OnDrag(GestureData d)      { _dragFired = true;      _capturedDrag = d; }
        private void OnRotate(GestureData d)    { _rotateFired = true;    _capturedRotate = d; }
        private void OnPinch(GestureData d)     { _pinchFired = true;     _capturedPinch = d; }
        private void OnLightDrag(GestureData d) { _lightDragFired = true; _capturedLightDrag = d; }

        // ────────── Dispatch correctly routes each GestureType to the matching interface method ──────────

        [Test]
        public void Dispatch_Tap_InvokesOnTap()
        {
            var data = new GestureData { Type = GestureType.Tap, Phase = GesturePhase.Ended, TapCount = 1 };
            GestureDispatcher.Dispatch(data);

            Assert.IsTrue(_tapFired, "OnTap should fire for GestureType.Tap");
            Assert.IsFalse(_dragFired);
            Assert.IsFalse(_rotateFired);
            Assert.IsFalse(_pinchFired);
            Assert.IsFalse(_lightDragFired);
            Assert.AreEqual(GesturePhase.Ended, _capturedTap.Phase);
            Assert.AreEqual(1, _capturedTap.TapCount);
        }

        [Test]
        public void Dispatch_Drag_InvokesOnDrag()
        {
            var data = new GestureData { Type = GestureType.Drag, Phase = GesturePhase.Updated, Delta = new Vector2(1, 2) };
            GestureDispatcher.Dispatch(data);

            Assert.IsTrue(_dragFired);
            Assert.IsFalse(_tapFired);
            Assert.AreEqual(new Vector2(1, 2), _capturedDrag.Delta);
        }

        [Test]
        public void Dispatch_Rotate_InvokesOnRotate()
        {
            var data = new GestureData { Type = GestureType.Rotate, Phase = GesturePhase.Updated, AngleDelta = 0.1f };
            GestureDispatcher.Dispatch(data);

            Assert.IsTrue(_rotateFired);
            Assert.AreEqual(0.1f, _capturedRotate.AngleDelta, 1e-5f);
        }

        [Test]
        public void Dispatch_Pinch_InvokesOnPinch()
        {
            var data = new GestureData { Type = GestureType.Pinch, Phase = GesturePhase.Updated, ScaleDelta = 1.05f };
            GestureDispatcher.Dispatch(data);

            Assert.IsTrue(_pinchFired);
            Assert.AreEqual(1.05f, _capturedPinch.ScaleDelta, 1e-5f);
        }

        [Test]
        public void Dispatch_LightDrag_InvokesOnLightDrag()
        {
            var data = new GestureData { Type = GestureType.LightDrag, Phase = GesturePhase.Updated };
            GestureDispatcher.Dispatch(data);

            Assert.IsTrue(_lightDragFired);
        }

        // ────────── None / unknown GestureType is a no-op ──────────

        [Test]
        public void Dispatch_None_DoesNotFireAnyEvent()
        {
            var data = new GestureData { Type = GestureType.None };
            GestureDispatcher.Dispatch(data);

            Assert.IsFalse(_tapFired);
            Assert.IsFalse(_dragFired);
            Assert.IsFalse(_rotateFired);
            Assert.IsFalse(_pinchFired);
            Assert.IsFalse(_lightDragFired);
        }

        // ────────── Source-Generator-generated event IDs are unique ──────────

        [Test]
        public void IGestureEvent_GeneratedEventIds_AllUnique()
        {
            int[] ids =
            {
                (int)IGestureEvent_Event.OnTap,
                (int)IGestureEvent_Event.OnDrag,
                (int)IGestureEvent_Event.OnRotate,
                (int)IGestureEvent_Event.OnPinch,
                (int)IGestureEvent_Event.OnLightDrag,
            };
            var set = new System.Collections.Generic.HashSet<int>(ids);
            Assert.AreEqual(ids.Length, set.Count,
                "All IGestureEvent method hashes must be unique (Source Generator guarantees this).");
        }
    }
}
