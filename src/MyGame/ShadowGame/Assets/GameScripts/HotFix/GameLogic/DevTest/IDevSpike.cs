// 该文件由Cursor 自动生成
// 开发测试（Spike / 诊断）统一接口。整文件仅在 UNITY_EDITOR || DEBUG 编译。
// 设计要点：
//   * Spike 自己决定是否要挂 MonoBehaviour、挂几个、是否 DontDestroyOnLoad、何时销毁
//   * DevBootstrap 只负责调 Launch()，不关心内部生命周期
//   * 调用时机：GameApp.Entrance 跑完 → 业务 FSM 进入 DevTestState → DevBootstrap.RunRequested()

#if UNITY_EDITOR || DEBUG
namespace GameLogic.DevTest
{
    public interface IDevSpike
    {
        string Id { get; }
        string Name { get; }
        void Launch();
    }
}
#endif
