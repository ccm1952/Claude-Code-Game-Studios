// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 游戏初始加载状态。
    /// <para>职责：初始数据加载、登录服务器、获取玩家信息。完成后自动切换到大厅状态。</para>
    /// </summary>
    public class GameLoadingState : GameFlowState
    {
        protected override void OnEnter(IFsm<IFsmModule> fsm)
        {
            base.OnEnter(fsm);
            Log.Info("[GameFlow] 进入 GameLoadingState —— 开始初始加载");

            // TODO: 初始数据加载、登录服务器、获取玩家存档等
            // 示例：加载完成后切换到大厅
            OnLoadingComplete(fsm);
        }

        protected override void OnLeave(IFsm<IFsmModule> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            Log.Info("[GameFlow] 离开 GameLoadingState");
        }

        /// <summary>
        /// 初始加载完成。
        /// <para>DEBUG / Editor 走 DevTestState（开发测试模式）；Release 暂停留在本状态，
        /// 等业务 UI 接入后再推进 GameLobbyState（这是 v1 期暂态，业务接入后改回 ChangeState&lt;GameLobbyState&gt;）。</para>
        /// </summary>
        private void OnLoadingComplete(IFsm<IFsmModule> fsm)
        {
            Log.Info("[GameFlow] 初始加载完成");
#if UNITY_EDITOR || DEBUG
            ChangeState<DevTestState>(fsm);
#else
            Log.Info("[GameFlow] Release 路径暂停留在 GameLoadingState（等业务 UI 就绪后接入 GameLobbyState）");
#endif
        }
    }
}
