using System.Collections.Generic;
using System.Reflection;
using GameLogic;

#if ENABLE_OBFUZ
using Obfuz;
#endif
using TEngine;
using UnityEngine;
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

        // S5-06 audio system facade activation — TEngine AudioModule.OnInit() 已自动 Initialize(Settings.AudioSetting.audioGroupConfigs)
        // (per AudioModule.cs:322-326)；项目层 AudioManager 仅订阅 IAudioEvent / ISettingsEvent listeners 并 baseline framework volume。
        AudioManager.Instance.Initialize();

#if UNITY_EDITOR || DEBUG
        RegisterDevSpikes();
#endif

        StartGameLogic();
    }

#if UNITY_EDITOR || DEBUG
    // Spike / 开发测试注册入口（仅 Editor / Debug 编译）。
    // 红线：main.unity 只挂 GameEntry；所有热更域测试必须通过 DevBootstrap 注册，在 DevTestState 动态挂载。
    private static void RegisterDevSpikes()
    {
        // SP-011 已在 Sprint 2 PASS（Sprint 2 retro 已存档），不再每次启动并发跑。
        // S3-01 已在 Sprint 3 ✅ DONE（2026-04-30 PlayMode CORE PASSED），不再每次启动并发跑。
        // S3-02 已在 Sprint 3 ✅ DONE（2026-04-30 dusk PlayMode CORE PASSED 6/6, P6 delta 1.36% << 5%），不再每次启动并发跑。
        // S3-03 已在 Sprint 3 ✅ DONE（2026-04-30 dusk PlayMode CORE 5/5 PASSED），不再每次启动并发跑。
        // S4-07 已在 Sprint 4 ✅ DONE（2026-05-06 PlayMode 4/4 PASSED — DOTween + Raycast + fat-finger + 10obj perf），不再每次启动并发跑。
        // S5-03 已在 Sprint 5 ✅ DONE（2026-05-06 PlayMode 8/8 PASSED — Puzzle State Machine R3 + V2-5），不再每次启动并发跑。
        // 如需复跑，临时取消相应注释行。注意：DevBootstrap 当前并发 Launch 所有 spike，多 spike
        // 同时调 LoadSceneAsync 会撞 YooAsset 内部 "while loading" 锁（type-3 drift 防御）。
        // S5-05 已在 Sprint 5 ✅ DONE（2026-05-08 PlayMode 10/10 PASSED — Narrative Sequence Engine R3 + V2-5），不再每次启动并发跑。
        // S5-06 已在 Sprint 5 ✅ DONE（2026-05-08 PlayMode 10/10 PASSED first-run — Audio Manager Init R3 + V2-5；evidence: production/qa/playmode-audio-mix-architecture-2026-05-08.md），不再每次启动并发跑。
        // Sprint 5 Track B 三个 P1 ADR production code dev-stories 全部 ✅ DONE — 当前无活跃 spike（如需添加新 spike 在此 Register）。
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.SP011Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S301Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S302Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S303Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S407Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S503Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S505Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S506Spike());
    }
#endif

    private static void StartGameLogic()
    {
        Log.Info("======= StartGameLogic =======");

        var states = new List<FsmState<IFsmModule>>
        {
            new GameLoadingState(),
            new GameLobbyState(),
            new LevelLoadingState(),
            new GameplayState(),
            new LevelEndState(),
        };
#if UNITY_EDITOR || DEBUG
        states.Add(new DevTestState());
#endif

        var fsm = GameModule.Fsm.CreateFsm(
            GameFlowDef.FsmName,
            GameModule.Fsm,
            states
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