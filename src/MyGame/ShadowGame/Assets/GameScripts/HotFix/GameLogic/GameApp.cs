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

    // S5-1b: SceneManager production instance (ADR-009 boot pipeline 接入；GameApp 拥有生命周期，Release 内 Dispose)。
    private static GameLogic.SceneManager _sceneManager;

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

        // S5-1b SceneManager boot pipeline 接入（ADR-009 §_chapterDataProvider 注入 + ADR-007 Luban access pattern）。
        // fixture provider 仅 chapter 1（其余 id 返 null fail-loud，与未来 Luban TbChapter.Get 真接入行为一致）；
        // RegisterFadeOverlay 显式注入 NoOp（即使 default fallback 也是 NoOp，显式让 wire 路径 grep-able）。
        _sceneManager = new GameLogic.SceneManager();
        _sceneManager.Init();
        _sceneManager.RegisterChapterDataProvider(BuildFixtureChapterDataProvider());
        _sceneManager.RegisterFadeOverlay(new GameLogic.NoOpFadeOverlay());
        Log.Info("[GameApp] SceneManager production wire-up done (chapter 1 fixture provider + NoOp fade overlay)");

#if UNITY_EDITOR || DEBUG
        RegisterDevSpikes();
#endif

        StartGameLogic();
    }

    // S5-1b fixture ChapterDataProvider — Luban TbChapter 真接入 deferred to post-VS (user decision 2026-05-09)。
    // 签名 Func<int, ChapterData> 与未来 ConfigSystem.Tables.TbChapter.Get(id) 真接入 100% 一致；
    // migration 仅 1 lambda swap：id => ConfigSystem.Tables.TbChapter.Get(id)。
    //
    // S5-02 (2026-05-12 Session 27 #3 P5 修复): 加 chapter 2 = 复用 chapter 1 scene 作 MVP placeholder
    //   ('Next Chapter' Button click → OnRequestSceneChange(2) → unload chapter 1 + reload Chapter_01_Approach scene);
    //   Sprint 6 polish 时换真 chapter 2 art asset / 移除 chapter 2 fixture (Type-5 dp6 NEW spec drift —
    //   story 原写 chapter 0 = main menu return 但 ISceneEvent.cs:24 仅支持 1..5)。
    private static System.Func<int, GameLogic.ChapterData> BuildFixtureChapterDataProvider() => id => id switch
    {
        1 => new GameLogic.ChapterData(
            id: 1,
            sceneId: "Chapter_01_Approach",
            bgmAsset: string.Empty,                       // chapter 1 暂无 BGM；audio 系统 S5-02 接入
            emotionalWeight: 1.0f,                        // ADR-009 默认值
            overlayColor: "#3A3530"                       // art-bible.md line 53 + scene-management.md line 443
        ),
        // chapter 2 MVP placeholder: 复用 chapter 1 scene asset；仅为 S5-02 P5 'Next Chapter' Button click
        // 走完整 11-step unload+reload 路径；Sprint 6 art-asset polish 时替换真 chapter 2 scene asset
        2 => new GameLogic.ChapterData(
            id: 2,
            sceneId: "Chapter_01_Approach",
            bgmAsset: string.Empty,
            emotionalWeight: 1.0f,
            overlayColor: "#3A3530"
        ),
        _ => null                                          // 未知 id 同 Luban TbChapter.Get fail-loud 行为
    };

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
        // S5-1c 已在 Sprint 5 ✅ DONE（2026-05-09 PlayMode 5/5 PASSED 24/24 asserts — ADR-009 listener-path driver + F4 stub 永久移除），不再每次启动并发跑。
        // S5-08 已在 Sprint 5 ✅ DONE（2026-05-11 PlayMode 4/4 PASSED 29/29 asserts — UIModule + UIWindow vendor lifecycle + Button.onClick path），不再每次启动并发跑。
        // S5-02 已在 Sprint 5 ✅ DONE（2026-05-12 Session 27 #3 第 2 跑 PlayMode 5/5 PASSED — chapter 1 end-to-end 5 systems integration happy path；evidence: production/qa/playmode-end-to-end-flow-2026-05-12.md），不再每次启动并发跑。
        //   注意：S5-02 spike P5 case (NextChapterButtonSwitchToChapter2) 在 Sprint 6 S6-07 dev-story Phase 2 [A] decision delete (chapter switch verify scope shrink → Sprint 7+ ChapterStateManager + ChapterSelect epic single spike)；
        //   S5-02 spike 现 4 case (P1-P4 chapter 1 happy path) — 历史 Sprint 5 5 case PASS 不丢。
        // S6-07 已在 Sprint 6 ✅ DONE（2026-05-13 PlayMode 5/5 PASSED 27/27 asserts first-attempt-after-DevTestState-fix —
        //   main menu UIWindow polish 4 button group + vendor 7+2 lifecycle protected override + fade-in + BGM hook
        //   fail-safe；evidence: production/qa/playmode-main-menu-polish-2026-05-13.md），不再每次启动并发跑。
        // S6-08 已在 Sprint 6 ✅ DONE（2026-05-13 Session 28 PlayMode 5/5 PASSED 36/36 asserts —
        //   UIModule auto inputblocker sender-side Top/Tips + popup queue + sorting + pause/resume/clear；
        //   evidence: production/qa/playmode-popup-auto-blocker-2026-05-13.md），不再每次启动并发跑。
        // S6-04 当前 active spike（Sprint 6 Track C — vs-chapter-1 epic / story-003 error/restart path narrow
        //   scope [A] 0 production code change + spike only）：
        //   验证 SceneManager AC-1/-2/-9/-10 error-path 行为 — TryResolveOrFail(99) → OnSceneLoadFailed +
        //   state=Error；transition 中 newest-wins pending (AC-9 _pendingTargetChapterId)；isolated local
        //   SceneManager + bad sceneId fixture chapter 99 → 2 retry exhaust → OnSceneLoadFailed；
        //   Error 状态下 fire(1) → AC-10 silent drop (Log.Warning + no state change)；RecoverToIdle() →
        //   Idle；re-fire(1) same currentChapterId → AC-8 silent OnSceneReady (no transition)。
        //   5 R3 case (P1 TryResolveOrFail + P2 NewestWinsPendingDuringTransition + P3 RetryExhaust isolated
        //   local + P4 ErrorRecovery + RestartSameTarget + P5 RapidNewestWinsOverwrite)；
        //   listener spy 5 ISceneEvent (LoadFailed + LoadComplete + TransitionBegin + TransitionEnd +
        //   SceneReady)；reflection 拿 GameApp._sceneManager (S5-1b precedent)；ChapterDataProvider
        //   fixture chapter 99 → "NotExistScene_Chapter99" trigger asset load exception。
        //   Expected vendor warning/error allowlist 过滤 UnexpectedErrorCount (TryResolveOrFail Warning +
        //   Load attempt failed Warning + all attempts exhausted Error + AC-10 silent drop Warning)。
        GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S604Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S608Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S607Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S502Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S508Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S51cSpike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S51bSpike());
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
        // S5-1b SceneManager Dispose 必须先于 FSM Destroy（FSM 销毁时 GameLogic listener bus 仍在；
        // SceneManager.Dispose 内 RemoveEventListener 需要 listener bus alive）。
        if (_sceneManager != null)
        {
            _sceneManager.Dispose();
            _sceneManager = null;
            Log.Info("[GameApp] SceneManager disposed");
        }

        GameModule.Fsm.DestroyFsm<IFsmModule>(GameFlowDef.FsmName);
        SingletonSystem.Release();
        Log.Warning("======= Release GameApp =======");
    }
}