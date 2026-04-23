// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 手势事件接口（Input System → ObjectInteraction / Tutorial / LightSource）。
    /// <para>Sender: InputService → GestureDispatcher</para>
    /// <para>Listener: ObjectInteractionSystem, TutorialSystem, LightSourceController</para>
    /// <para>Payload: <see cref="GestureData"/>（值类型 struct，hot-path 零 GC）</para>
    /// <para>Cascade depth: 1（监听者可触发 Evt_ObjectSelected 等后续事件）</para>
    /// <para>协议来源：ADR-027 取代 ADR-006 §1/§2；listener 生命周期与 re-entrancy 规则继承 ADR-006 §3/§5</para>
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IGestureEvent
    {
        /// <summary>
        /// 单指 Tap。<c>data.Phase == Ended</c>，<c>data.TapCount == 1</c>（双击待扩展）。
        /// </summary>
        void OnTap(GestureData data);

        /// <summary>
        /// 单指 Drag。<c>data.Phase</c> 取 Began/Updated/Ended/Cancelled。
        /// </summary>
        void OnDrag(GestureData data);

        /// <summary>
        /// 双指旋转。<c>data.AngleDelta</c> 单位为弧度/帧，正值 = 逆时针。
        /// </summary>
        void OnRotate(GestureData data);

        /// <summary>
        /// 双指缩放。<c>data.ScaleDelta</c> 表示本帧缩放比率（&gt;1 放大）。
        /// </summary>
        void OnPinch(GestureData data);

        /// <summary>
        /// 光源拖拽（语义同 Drag，由上层 Object Interaction context 区分）。
        /// MVP 暂未直接派发。
        /// </summary>
        void OnLightDrag(GestureData data);
    }
}
