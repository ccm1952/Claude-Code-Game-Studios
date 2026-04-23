// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// ShadowRT 像素数据更新事件（AsyncGPUReadback → 匹配打分）。
    /// <para>Sender: <see cref="ShadowRTReadback"/>（运行在主线程 AsyncGPUReadback 回调中）</para>
    /// <para>Listener: ShadowPuzzleSystem（匹配打分 / 智能提示取样）</para>
    /// <para>Payload: <see cref="ShadowRTData"/>（包含 byte[] Pixels + 维度 + stale cache 标志）</para>
    /// <para>Cascade depth: 1（可能触发 <c>IPuzzleEvent.OnMatchScoreChanged</c> 等后续事件）</para>
    /// <para>协议来源：ADR-027 取代 ADR-006 §1 `Evt_ShadowRT_Updated`；iOS AOT 需 `HybridCLR → Generate → AOT Generic References` 预注册 <see cref="ShadowRTData"/> 泛型</para>
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IShadowRTEvent
    {
        /// <summary>
        /// ShadowRT CPU 端数据就绪。如果 <c>data.IsStaleCache == true</c>，
        /// 表示本帧 readback 失败，监听者应复用上一帧的判定结果而不是用该 buffer 重新打分。
        /// </summary>
        void OnShadowRTUpdated(ShadowRTData data);
    }
}
