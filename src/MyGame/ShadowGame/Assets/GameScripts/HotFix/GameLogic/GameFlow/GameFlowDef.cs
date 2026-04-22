// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 游戏流程常量定义。
    /// </summary>
    public static class GameFlowDef
    {
        /// <summary>
        /// 游戏主流程状态机名称。
        /// </summary>
        public const string FsmName = "GameFlow";

        /// <summary>
        /// FSM 数据键：当前要加载的关卡 ID。
        /// </summary>
        public const string DataKey_LevelId = "LevelId";

        /// <summary>
        /// FSM 数据键：关卡结算结果（true=胜利, false=失败）。
        /// </summary>
        public const string DataKey_LevelResult = "LevelResult";
    }

    /// <summary>
    /// 游戏流程状态基类。
    /// <para>所有游戏流程状态继承此类，Owner 为 IFsmModule（热更域中无需额外持有者）。</para>
    /// </summary>
    public abstract class GameFlowState : FsmState<IFsmModule>
    {
    }
}
