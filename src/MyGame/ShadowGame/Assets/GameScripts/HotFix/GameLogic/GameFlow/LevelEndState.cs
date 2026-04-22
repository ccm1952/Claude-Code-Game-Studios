// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 关卡结算状态。
    /// <para>职责：展示结算界面（胜利/失败）、发放奖励、上报数据。
    /// 结算完成后可返回大厅或直接进入下一关。</para>
    /// </summary>
    public class LevelEndState : GameFlowState
    {
        private int _levelId;
        private bool _isVictory;

        protected override void OnEnter(IFsm<IFsmModule> fsm)
        {
            base.OnEnter(fsm);

            _levelId = fsm.GetData<int>(GameFlowDef.DataKey_LevelId);
            _isVictory = fsm.GetData<bool>(GameFlowDef.DataKey_LevelResult);
            Log.Info($"[GameFlow] 进入 LevelEndState —— 关卡 {_levelId} 结算（{(_isVictory ? "胜利" : "失败")}）");

            // TODO: 打开结算 UI，展示结算数据
            // TODO: 发放奖励、上报结算数据
        }

        protected override void OnLeave(IFsm<IFsmModule> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);

            // TODO: 关闭结算 UI
            // TODO: 卸载关卡场景资源

            Log.Info("[GameFlow] 离开 LevelEndState");
        }

        /// <summary>
        /// 返回大厅。由结算 UI 回调触发。
        /// </summary>
        public void BackToLobby(IFsm<IFsmModule> fsm)
        {
            ChangeState<GameLobbyState>(fsm);
        }

        /// <summary>
        /// 重玩当前关卡。由结算 UI 回调触发。
        /// </summary>
        public void ReplayLevel(IFsm<IFsmModule> fsm)
        {
            // LevelId 保持不变，直接重新进入加载
            ChangeState<LevelLoadingState>(fsm);
        }

        /// <summary>
        /// 进入下一关。由结算 UI 回调触发。
        /// </summary>
        /// <param name="fsm">状态机实例。</param>
        /// <param name="nextLevelId">下一关 ID。</param>
        public void NextLevel(IFsm<IFsmModule> fsm, int nextLevelId)
        {
            fsm.SetData(GameFlowDef.DataKey_LevelId, nextLevelId);
            ChangeState<LevelLoadingState>(fsm);
        }
    }
}
