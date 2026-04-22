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
        /// 初始加载完成，切换到大厅。
        /// </summary>
        private void OnLoadingComplete(IFsm<IFsmModule> fsm)
        {
            // TODO: 关闭加载 UI
            ChangeState<GameLobbyState>(fsm);
        }
    }
}
