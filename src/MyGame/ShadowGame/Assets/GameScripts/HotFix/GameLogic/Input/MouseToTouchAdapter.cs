// 该文件由Cursor 自动生成
// Sprint 6 emergent fix Track F vs-chapter-1-004 — Editor Mouse → TouchState adapter。
//
// V3.0.1 dp16 candidate "ADR spec gap re Editor-only path" 实战触发：
//   ADR-010 §Decision Layer 1 仅 cover Touch (`Input.GetTouch(i)`)，未 cover Editor Mouse pipeline；
//   Phase 2.0 ADR-010 §Implementation Guidelines amend Step 9 "Editor Mouse Adapter" 5-10 行 spec wording 落档。
//
// 设计要点:
//   * 整文件 #if UNITY_EDITOR guard — Player Build 完全不参与编译，0 残留
//   * 单指 (FingerId=0) Mouse Button 0 (LMB) → TouchState；多指 (Pinch/Rotate Mouse 模拟) 留 Sprint 7+
//   * 0 GC allocation (TouchState struct 值类型；no-alloc on hot path)
//   * Sprint 7+ Touch 真机 testing 时 InputService.Tick #else branch 接 Touch sampling，本 adapter Editor only

#if UNITY_EDITOR
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Editor Play (development workflow only) Mouse → <see cref="TouchState"/> adapter
    /// (ADR-010 §Implementation Guidelines Step 9 "Editor Mouse Adapter" 实施)。
    /// <para>把 Editor Play Mode 下的 Mouse Button 0 (LMB) 翻译成 <see cref="TouchState"/> 喂给
    /// <see cref="SingleFingerFSM"/>，让开发期 Mouse drag 能驱动同一条 gesture pipeline，
    /// 避免 Editor playtest 与真机 Touch pipeline 行为偏离。</para>
    /// <para>每帧 <see cref="SampleMouse"/> 返回当前 TouchState：
    /// <list type="bullet">
    ///   <item>Mouse Down (frame 0): Phase = Began, IsActive = true</item>
    ///   <item>Mouse Held (frame 1..N-1): Phase = Moved (有 delta) / Stationary (无 delta), IsActive = true</item>
    ///   <item>Mouse Up (frame N): Phase = Ended, IsActive = true (FSM 看到 Ended 后清理)</item>
    ///   <item>Mouse Idle (Up 后): Phase = Canceled, IsActive = false</item>
    /// </list></para>
    /// <para>Sprint 7+ Touch 真机 testing 接入时 disable adapter (InputService.Tick #else branch 接 Touch sampling)。</para>
    /// </summary>
    internal sealed class MouseToTouchAdapter
    {
        private const float MovementEpsilonSqr = 0.01f;

        private bool _wasDown;
        private Vector2 _lastPos;

        public TouchState SampleMouse()
        {
            bool down = Input.GetMouseButton(0);
            Vector2 pos = (Vector2)Input.mousePosition;

            TouchPhase phase;
            // 让 Mouse Up 一帧仍然 IsActive=true，让 FSM 看到 Phase=Ended 之后再 idle (与 Touch lifecycle 一致)。
            bool isActive = down || _wasDown;

            if (down && !_wasDown)
            {
                phase = TouchPhase.Began;
            }
            else if (!down && _wasDown)
            {
                phase = TouchPhase.Ended;
            }
            else if (down)
            {
                phase = (pos - _lastPos).sqrMagnitude > MovementEpsilonSqr
                    ? TouchPhase.Moved
                    : TouchPhase.Stationary;
            }
            else
            {
                phase = TouchPhase.Canceled;
            }

            var ts = new TouchState
            {
                FingerId = 0,
                CurrentPosition = pos,
                Phase = phase,
                IsActive = isActive
            };

            _wasDown = down;
            _lastPos = pos;
            return ts;
        }
    }
}
#endif
