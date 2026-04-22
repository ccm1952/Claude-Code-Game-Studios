// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 关卡加载状态。
    /// <para>职责：根据关卡 ID 加载对应场景和资源，展示 Loading UI。加载完成后切换到 GameplayState。</para>
    /// </summary>
    public class LevelLoadingState : GameFlowState
    {
        private int _levelId;

        protected override void OnEnter(IFsm<IFsmModule> fsm)
        {
            base.OnEnter(fsm);

            _levelId = fsm.GetData<int>(GameFlowDef.DataKey_LevelId);
            Log.Info($"[GameFlow] 进入 LevelLoadingState —— 加载关卡 {_levelId}");

            // TODO: 打开 Loading UI
            // TODO: 异步加载关卡场景和资源，加载完成后调用 OnLoadComplete
            OnLoadComplete(fsm);
        }

        protected override void OnLeave(IFsm<IFsmModule> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);

            // TODO: 关闭 Loading UI

            Log.Info("[GameFlow] 离开 LevelLoadingState");
        }

        /// <summary>
        /// 关卡资源加载完成，切换到游戏进行状态。
        /// </summary>
        private void OnLoadComplete(IFsm<IFsmModule> fsm)
        {
            ChangeState<GameplayState>(fsm);
        }
    }
}
