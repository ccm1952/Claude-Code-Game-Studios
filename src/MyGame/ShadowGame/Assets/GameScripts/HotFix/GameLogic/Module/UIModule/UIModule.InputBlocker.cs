// 该文件由Cursor 自动生成
using System;
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// UIModule InputBlocker auto-fire helper（S6-08 ui-system-008 sender-side narrow scope [A]）。
    /// <para>对 Top(2) / Tips(3) layer panel 的 ShowUIImp / CloseUI / HideUI 调用自动 fire
    /// <see cref="IInputBlockerEvent.OnPushBlocker"/> / <see cref="IInputBlockerEvent.OnPopBlocker"/>
    /// 广播，token = <c>type.FullName</c>（e.g. <c>"GameLogic.MockTopPanel"</c>）。</para>
    /// <para>UI(1) / Bottom(0) / System(4) layer panel 不 fire（HUD pass-through to game per TR-ui-004；
    /// Bottom 是 background 非交互；System 是 always-on-top 系统通讯非用户交互态阻塞）。</para>
    /// <para>Listener-side (InputBlocker singleton / InputManager class wiring) 留 Sprint 7+ ADR-010
    /// InputManager epic — 本 partial class 仅 sender 侧广播。</para>
    /// <para>Sender precedent: <c>NarrativeSequencePlayer</c> S5-05 已 token = <c>"narrative_seq_&lt;id&gt;"</c>
    /// 模式 fire；本类与之并行 dual-source 同向贡献 IInputBlockerEvent token stack。</para>
    /// </summary>
    public sealed partial class UIModule
    {
        /// <summary>
        /// HideUI 路径已 fire pop 但 window 还在 stack 中（延迟 close 模式），用于避免后续
        /// timer 到期 CloseUI 触发的二次 fire pop。
        /// <para>vendor HideUI HideTimeToClose &gt; 0 路径触发 timer → CloseUI(type)；HideUI 内已 fire 一次 pop，
        /// CloseUI 内再 fire 会导致 spike listener 计数失衡 + 未来 InputBlocker listener 接入后 orphan pop warning。</para>
        /// <para>HideUI 路径加入 set；CloseUI 路径若命中 set 则跳过 fire + 同步 remove；
        /// re-show（TryGetWindow Pop+Push）路径也 remove（hidden→shown 后由 push 重新激活 lifecycle）。</para>
        /// </summary>
        private readonly HashSet<Type> _inputBlockerPoppedByHide = new HashSet<Type>();

        /// <summary>
        /// 对 Top / Tips layer window fire <see cref="IInputBlockerEvent.OnPushBlocker"/>；其他 layer no-op。
        /// 调用方需确保 window state 已 init（<c>WindowLayer</c> 已通过 <c>Init</c> 设置）。
        /// </summary>
        private void TryFireInputBlockerPush(UIWindow window)
        {
            if (!ShouldFireInputBlocker(window))
            {
                return;
            }

            Type type = window.GetType();
            _inputBlockerPoppedByHide.Remove(type);

            GameEvent.Get<IInputBlockerEvent>().OnPushBlocker(type.FullName);
        }

        /// <summary>
        /// 对 Top / Tips layer window fire <see cref="IInputBlockerEvent.OnPopBlocker"/>；其他 layer no-op。
        /// </summary>
        /// <param name="window">被关闭 / 隐藏的 UIWindow。</param>
        /// <param name="fromHideUI">
        /// <c>true</c> 表示来自 HideUI 延迟 close 路径（HideTimeToClose &gt; 0）；
        /// 此时 fire 一次 pop 后 mark type 进入 <see cref="_inputBlockerPoppedByHide"/>，
        /// 后续 timer 到期 CloseUI 命中 set 时跳过 fire 避免双 fire。
        /// </param>
        private void TryFireInputBlockerPop(UIWindow window, bool fromHideUI = false)
        {
            if (!ShouldFireInputBlocker(window))
            {
                return;
            }

            Type type = window.GetType();

            if (!fromHideUI && _inputBlockerPoppedByHide.Contains(type))
            {
                _inputBlockerPoppedByHide.Remove(type);
                return;
            }

            GameEvent.Get<IInputBlockerEvent>().OnPopBlocker(type.FullName);

            if (fromHideUI)
            {
                _inputBlockerPoppedByHide.Add(type);
            }
        }

        private static bool ShouldFireInputBlocker(UIWindow window)
        {
            if (window == null)
            {
                return false;
            }

            int layer = window.WindowLayer;
            return layer == (int)UILayer.Top || layer == (int)UILayer.Tips;
        }
    }
}
