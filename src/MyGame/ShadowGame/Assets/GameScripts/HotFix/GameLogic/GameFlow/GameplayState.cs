// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 关卡进行中状态。
    /// <para>职责：运行关卡游戏逻辑。关卡结束时携带结算结果切换到 LevelEndState。</para>
    /// </summary>
    public class GameplayState : GameFlowState
    {
        private int _levelId;

        protected override void OnEnter(IFsm<IFsmModule> fsm)
        {
            base.OnEnter(fsm);

            _levelId = fsm.GetData<int>(GameFlowDef.DataKey_LevelId);
            Log.Info($"[GameFlow] 进入 GameplayState —— 关卡 {_levelId} 开始");

            // TODO: 初始化关卡逻辑控制器
            // TODO: 打开战斗 UI
        }

        protected override void OnUpdate(IFsm<IFsmModule> fsm, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

            // TODO: 关卡运行中的帧逻辑（通常由关卡控制器自行驱动，此处可轮询结束条件）
        }

        protected override void OnLeave(IFsm<IFsmModule> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);

            // TODO: 清理关卡逻辑控制器
            // TODO: 关闭战斗 UI

            Log.Info("[GameFlow] 离开 GameplayState");
        }

        /// <summary>
        /// 关卡结束。由关卡逻辑触发。
        /// </summary>
        /// <param name="fsm">状态机实例。</param>
        /// <param name="isVictory">是否胜利。</param>
        public void EndLevel(IFsm<IFsmModule> fsm, bool isVictory)
        {
            fsm.SetData(GameFlowDef.DataKey_LevelResult, isVictory);
            ChangeState<LevelEndState>(fsm);
        }
    }
}
