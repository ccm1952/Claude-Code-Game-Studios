// 该文件由Cursor 自动生成
// 开发测试 FSM 状态。整文件仅在 UNITY_EDITOR || DEBUG 编译。
// 生命周期：
//   * 由 GameLoadingState.OnLoadingComplete（DEBUG 分支）切入
//   * OnEnter 启动所有已注册 Spike；有 Spike → 不推进（停留，保留 OnGUI）
//   * 无 Spike → 回落到 GameLobbyState 保持路径可达

#if UNITY_EDITOR || DEBUG
using TEngine;

namespace GameLogic
{
    public class DevTestState : GameFlowState
    {
        protected override void OnEnter(IFsm<IFsmModule> fsm)
        {
            base.OnEnter(fsm);
            Log.Info("[GameFlow] 进入 DevTestState —— 开发测试模式");

            if (DevTest.DevBootstrap.PendingCount == 0)
            {
                Log.Info("[GameFlow] 未注册任何 Spike，直接进入 GameLobbyState");
                ChangeState<GameLobbyState>(fsm);
                return;
            }

            DevTest.DevBootstrap.RunRequested();
            Log.Info("[GameFlow] DevTestState 停留：等待 Spike 结果（手动停 PlayMode 结束）");
        }

        protected override void OnLeave(IFsm<IFsmModule> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            Log.Info("[GameFlow] 离开 DevTestState");
        }
    }
}
#endif
