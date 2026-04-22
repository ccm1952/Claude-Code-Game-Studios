// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 大厅/关卡选择状态。
    /// <para>职责：展示主界面、关卡选择列表。玩家选定关卡后携带关卡ID切换到加载状态。</para>
    /// </summary>
    public class GameLobbyState : GameFlowState
    {
        protected override void OnEnter(IFsm<IFsmModule> fsm)
        {
            base.OnEnter(fsm);
            Log.Info("[GameFlow] 进入 GameLobbyState —— 大厅/关卡选择");

            // TODO: 打开大厅 UI / 关卡选择 UI
            // GameModule.UI.ShowUIAsync<LobbyUI>();
        }

        protected override void OnLeave(IFsm<IFsmModule> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);

            if (!isShutdown)
            {
                // TODO: 关闭大厅 UI
                // GameModule.UI.CloseUI<LobbyUI>();
            }

            Log.Info("[GameFlow] 离开 GameLobbyState");
        }

        /// <summary>
        /// 选择关卡并进入。由 UI 回调触发。
        /// </summary>
        /// <param name="fsm">状态机实例。</param>
        /// <param name="levelId">关卡 ID。</param>
        public void EnterLevel(IFsm<IFsmModule> fsm, int levelId)
        {
            fsm.SetData(GameFlowDef.DataKey_LevelId, levelId);
            ChangeState<LevelLoadingState>(fsm);
        }
    }
}
