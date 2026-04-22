using System.Collections.Generic;
using System.Reflection;
using GameLogic;
#if ENABLE_OBFUZ
using Obfuz;
#endif
using TEngine;
#pragma warning disable CS0436


/// <summary>
/// 游戏App。
/// </summary>
#if ENABLE_OBFUZ
[ObfuzIgnore(ObfuzScope.TypeName | ObfuzScope.MethodName)]
#endif
public partial class GameApp
{
    private static List<Assembly> _hotfixAssembly;

    /// <summary>
    /// 热更域App主入口。
    /// </summary>
    /// <param name="objects"></param>
    public static void Entrance(object[] objects)
    {
        GameEventHelper.Init();
        _hotfixAssembly = (List<Assembly>)objects[0];
        Log.Info("======= GameApp Entrance =======");
        ConfigSystem.Instance.Load();
        Utility.Unity.AddDestroyListener(Release);
        StartGameLogic();
    }
    
    private static void StartGameLogic()
    {
        Log.Info("======= StartGameLogic =======");

        var fsm = GameModule.Fsm.CreateFsm(
            GameFlowDef.FsmName,
            GameModule.Fsm,
            new GameLoadingState(),
            new GameLobbyState(),
            new LevelLoadingState(),
            new GameplayState(),
            new LevelEndState()
        );
        fsm.Start<GameLoadingState>();
    }
    
    private static void Release()
    {
        GameModule.Fsm.DestroyFsm<IFsmModule>(GameFlowDef.FsmName);
        SingletonSystem.Release();
        Log.Warning("======= Release GameApp =======");
    }
}