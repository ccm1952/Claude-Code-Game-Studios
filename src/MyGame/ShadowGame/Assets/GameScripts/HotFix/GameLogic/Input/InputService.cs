// 该文件由Cursor 自动生成
// Sprint 6 emergent fix Track F vs-chapter-1-004 — Input Pipeline Driver layer (Sprint 2 SP-013 partial closure)。
//
// Driver layer 补齐 ADR-010 三层架构 Layer 1 Raw Touch Sampling Driver 的"Driver"部分：
//   * FSM 层 ✅ 已 Sprint 2 SP-013 完成 (SingleFingerFSM/DualFingerFSM/GestureDispatcher/InputConfigFromLuban
//     + 8 file 共 10)
//   * Driver 层 ❌ 0 production caller (Sprint 2 SP-013 sprint backlog placeholder wording drift —
//     V3.0.1 dp11 同根 candidate；sprint-status.yaml Sprint 2 SP-013 标 'DONE' 实际 partial)
//
// V3.0.1 dp15 sniff sub-clause 试点第 1 个 production caller hit > 0 修复 case：
//   本 InputService.Tick 内 ≥1 GestureDispatcher.Dispatch caller，rg verify production caller chain
//   `InputService.Tick → SingleFingerFSM.Update → GetGesture → GestureDispatcher.Dispatch → IGestureEvent.OnTap/OnDrag fire`
//   完整 round-trip。
//
// Editor Mouse pipeline (V3.0.1 dp16 candidate "ADR spec gap re Editor-only path" Phase 2 closure 第 1 项)：
//   ADR-010 §Implementation Guidelines Step 9 "Editor Mouse Adapter" Phase 2.0 amend；本 service 在
//   #if UNITY_EDITOR 下持有 MouseToTouchAdapter，Player Build 此分支编译为 explicit empty branch (Sprint 7+
//   Touch real device wiring 接入)。
//
// 关联 reference:
//   * production/epics/vs-chapter-1/story-004-input-pipeline-wiring.md (10 AC + 5 R3 case + Implementation Notes)
//   * docs/architecture/adr-010-input-abstraction.md (§Implementation Guidelines + Step 9 amend pending)
//   * Assets/GameScripts/HotFix/GameLogic/Input/SingleFingerFSM.cs (Update/GetGesture/ForceReset 接口)
//   * Assets/GameScripts/HotFix/GameLogic/Input/InputConfigFromLuban.cs (InitWithDefaults 占位 Luban)
//   * Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs (plain class + Init/Dispose lifecycle precedent)

using System;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 输入服务 — Driver 层补齐 (Sprint 2 SP-013 partial fsm-only-driver-pending closure)。
    /// <para>持 <see cref="SingleFingerFSM"/> + <see cref="InputConfigFromLuban"/> + (Editor Play only)
    /// <c>MouseToTouchAdapter</c>，Tick loop 通过 <see cref="Utility.Unity.AddUpdateListener"/> 注入 —
    /// 与 <see cref="SceneManager"/> listener-path driver pattern 一致 (plain class，由 <see cref="GameApp"/>
    /// 显式持生命周期，Release 内 Dispose 释放 listener 引用)。</para>
    /// <para>Sprint 6 OUT_OF_SCOPE: PC Mouse 真生产、DualFingerFSM wiring、InputBlocker listener-side、
    /// Luban TbInputConfig 接入、Touch 真机 testing → 留 Sprint 7+ ADR-010 V2 amendment epic。</para>
    /// </summary>
    public class InputService
    {
        private InputConfigFromLuban _config;
        private SingleFingerFSM _singleFingerFSM;
#if UNITY_EDITOR
        // V3.0.1 dp16 candidate "ADR spec gap re Editor-only path" closure 第 1 项 — ADR-010 §Implementation
        // Guidelines Step 9 'Editor Mouse Adapter' Phase 2.0 amend；adapter 不进入 production runtime build。
        private MouseToTouchAdapter _mouseAdapter;
#endif
        private Action _tickCached;
        private bool _initialized;
        // R3 spike 注入 TouchState 时需要暂停 production Tick（MouseToTouchAdapter SampleMouse 噪声污染 FSM state）。
        // S6-13 R3 第 1 跑 P1/P3/P4 FAIL 实证：spike TickForTest 注入 Began 后 yield 一帧，期间 production Tick
        // 用 mouse 真实位置计算 frameDist 远大于 DragThresholdPx → FSM 从 Pending 跳进 Dragging 污染所有 Tap case。
        private bool _tickSuspended;

        /// <summary>当前 SingleFingerFSM 引用 — R3 spike 通过 reflection 注入 TouchState 时使用。</summary>
        public SingleFingerFSM SingleFingerFSMForTest => _singleFingerFSM;

        /// <summary>当前 InputConfig 引用 — R3 spike 读 DragThresholdPx 等 derived value 时使用。</summary>
        public InputConfigFromLuban ConfigForTest => _config;

        public bool IsInitialized => _initialized;

        /// <summary>R3 spike 是否暂停了 production Tick（spike RunAllAsync 期间 true，结束 false）。</summary>
        public bool IsTickSuspended => _tickSuspended;

        /// <summary>
        /// 暂停 production Tick — R3 spike `RunAllAsync` 起步调用，避免 MouseToTouchAdapter 噪声污染
        /// spike 注入的 FSM state。配对 <see cref="ResumeTick"/> 必须 try/finally 调用以保证 manual playtest 复跑。
        /// </summary>
        public void SuspendTick() => _tickSuspended = true;

        /// <summary>恢复 production Tick — R3 spike `RunAllAsync` finally 块调用。</summary>
        public void ResumeTick() => _tickSuspended = false;

        public void Init()
        {
            if (_initialized)
            {
                Log.Warning("[InputService] Init called twice — ignored.");
                return;
            }

            _config = new InputConfigFromLuban();
            _config.InitWithDefaults();

            _singleFingerFSM = new SingleFingerFSM(_config);
            _singleFingerFSM.ComputeDragThreshold(Screen.dpi);

#if UNITY_EDITOR
            _mouseAdapter = new MouseToTouchAdapter();
#endif

            _tickCached = Tick;
            Utility.Unity.AddUpdateListener(_tickCached);
            _initialized = true;

            Log.Info($"[InputService] Init done (Sprint 6 emergent fix Track F vs-chapter-1-004). " +
                     $"dragThresholdPx={_config.DragThresholdPx:F2}, tapTimeoutSeconds={_config.TapTimeoutSeconds:F2}, " +
                     $"screenDpi={Screen.dpi:F1}, fallbackDpi={_config.FallbackDpi:F1}");
        }

        public void Dispose()
        {
            if (!_initialized) return;

            if (_tickCached != null)
            {
                Utility.Unity.RemoveUpdateListener(_tickCached);
                _tickCached = null;
            }
            _singleFingerFSM?.ForceReset();
            _singleFingerFSM = null;
#if UNITY_EDITOR
            _mouseAdapter = null;
#endif
            _config = null;
            _initialized = false;
            _tickSuspended = false;

            Log.Info("[InputService] Disposed");
        }

        /// <summary>
        /// 每帧 Tick — 由 <see cref="Utility.Unity.AddUpdateListener"/> 调用。
        /// <para>Editor Play: 通过 <c>MouseToTouchAdapter</c> 把 Mouse Button 0 翻译成 <see cref="TouchState"/> →
        /// <see cref="SingleFingerFSM.Update"/> → <see cref="GestureDispatcher.Dispatch"/>；</para>
        /// <para>Player Build: 暂留空 explicit branch (Sprint 7+ 真 Touch 接入入口；当前 Out_of_scope 一致)。</para>
        /// </summary>
        private void Tick()
        {
            if (!_initialized) return;
            if (_tickSuspended) return;   // R3 spike 期间 explicit suspend；manual playtest 不影响（spike 跑完即 ResumeTick）

#if UNITY_EDITOR
            if (_mouseAdapter == null) return;
            var touch = _mouseAdapter.SampleMouse();
            if (_singleFingerFSM.Update(in touch, Time.unscaledDeltaTime))
            {
                var gesture = _singleFingerFSM.GetGesture();
                GestureDispatcher.Dispatch(in gesture);
            }
#else
            // Sprint 7+ Touch 真机 testing 接入入口 — 当前 Out_of_scope (story-004 narrow scope)。
            // 此处 explicit empty branch 让 grep "InputService.Tick non-editor 0 caller" 看到
            // V3.0.1 dp15 sniff sub-clause 留观察 hook (Player Build 0 input 响应是 expected 行为，待 Sprint 7+ 接入)。
#endif
        }

        /// <summary>
        /// R3 spike 用 — 直接喂入 TouchState 驱动 SingleFingerFSM (绕过 MouseToTouchAdapter Mouse hardware 依赖)。
        /// 仅 Editor / Debug 使用；production runtime 不应调用此方法。
        /// </summary>
        /// <param name="touch">注入的 TouchState (FingerId/Phase/CurrentPosition/IsActive)</param>
        /// <param name="unscaledDeltaTime">注入帧 unscaledDeltaTime (R3 case 内可控测试时间窗)</param>
        /// <returns>FSM 是否本帧 emit gesture</returns>
        public bool TickForTest(in TouchState touch, float unscaledDeltaTime)
        {
            if (!_initialized) return false;
            if (_singleFingerFSM == null) return false;

            if (_singleFingerFSM.Update(in touch, unscaledDeltaTime))
            {
                var gesture = _singleFingerFSM.GetGesture();
                GestureDispatcher.Dispatch(in gesture);
                return true;
            }
            return false;
        }
    }
}
