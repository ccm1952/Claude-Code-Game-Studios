// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Bridges FSM output to TEngine <see cref="GameEvent"/>. 按 <see cref="GestureType"/>
    /// 选择 <see cref="IGestureEvent"/> 上对应的方法并通过 Source Generator 派发。
    /// <para>
    /// Separated from <c>InputService</c> to keep the service class thin and to
    /// allow unit-testing FSMs without pulling in GameEvent infrastructure.
    /// </para>
    /// <para>协议来源：ADR-027（取代 ADR-006 §1 的 const int 分发方式）</para>
    /// </summary>
    public static class GestureDispatcher
    {
        /// <summary>
        /// Dispatch a gesture through TEngine GameEvent interface protocol.
        /// Skips dispatch if <see cref="GestureType.None"/> or unknown type.
        /// </summary>
        public static void Dispatch(in GestureData gesture)
        {
            switch (gesture.Type)
            {
                case GestureType.Tap:
                    GameEvent.Get<IGestureEvent>().OnTap(gesture);
                    break;
                case GestureType.Drag:
                    GameEvent.Get<IGestureEvent>().OnDrag(gesture);
                    break;
                case GestureType.Rotate:
                    GameEvent.Get<IGestureEvent>().OnRotate(gesture);
                    break;
                case GestureType.Pinch:
                    GameEvent.Get<IGestureEvent>().OnPinch(gesture);
                    break;
                case GestureType.LightDrag:
                    GameEvent.Get<IGestureEvent>().OnLightDrag(gesture);
                    break;
                case GestureType.None:
                default:
                    return;
            }
        }
    }
}
