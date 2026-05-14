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

    // Sprint 6 emergent fix Track F vs-chapter-1-004: InputService production instance (ADR-010 Driver 层补齐；
    // Sprint 2 SP-013 sprint backlog placeholder wording drift — V3.0.1 dp11 candidate；
    // Editor Mouse → SingleFingerFSM → GestureDispatcher.Dispatch → IGestureEvent.OnTap/OnDrag fire 完整 round-trip)。
    private static GameLogic.InputService _inputService;

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

        // Sprint 6 emergent fix Track F vs-chapter-1-004: InputService boot pipeline 接入 (ADR-010 Driver 层补齐)。
        // Init 内 InputConfigFromLuban.InitWithDefaults() 暂用 GDD defaults (Luban TbInputConfig 留 Sprint 7+);
        // Editor Play 走 MouseToTouchAdapter 模拟 Touch (V3.0.1 dp16 candidate "ADR spec gap re Editor-only path"
        // 实战触发 Phase 2 closure；ADR-010 §Implementation Guidelines Step 9 amend 5-10 行 spec wording);
        // Player Build #else branch explicit empty (Sprint 7+ Touch 真机 testing 接入入口)。
        _inputService = new GameLogic.InputService();
        _inputService.Init();
        Log.Info("[GameApp] InputService production wire-up done (Sprint 2 SP-013 partial fsm-only-driver-pending closure)");

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
        // S6-04 已在 Sprint 6 ✅ DONE（2026-05-13 Session 30 PlayMode 5/5 PASSED 37/37 asserts —
        //   chapter 1 error/restart path R3：TryResolveOrFail / newest-wins pending / asset load fail retry exhaust /
        //   Error→RecoverToIdle / RapidNewestWinsOverwrite；evidence: production/qa/playmode-error-restart-path-2026-05-13.md），
        //   不再每次启动并发跑。如需复跑 R3，临时注释 S601PlaytestSpike + 取消 S604Spike 注释行。
        //
        // S6-13 Input Pipeline Wiring (2026-05-14 Session 32 Phase 2) 当前 active spike — Track F vs-chapter-1-004 R3 PlayMode probe
        //   (per story-004-input-pipeline-wiring.md R1+R2+R3 readiness gate ⚠️ DEFICIENCY-FLAGGED PASS)。
        //   5 R3 case (P1 MouseSingleTap → P2 MouseDragThreePhase → P3 TapVsDragThresholdBoundary →
        //   P4 SingleFingerFSMStateTransitionVerify → P5 NoMockFireBypassVerify) — V3.0.1 dp15 candidate
        //   "EditMode green ≠ production wired" sniff sub-clause 试点 第 1 个 production caller hit > 0 修复 case；
        //   reflection 拿 GameApp._inputService private static field + InputService.TickForTest 注入 TouchState 绕
        //   MouseToTouchAdapter Mouse hardware 依赖；JSON evidence WriteResultJson Application.persistentDataPath/S6-13_Result.json。
        //
        //   V3.0.1 dp16 candidate "ADR spec gap re Editor-only path" 实战触发 (R2.5 NEW finding：ADR-010 §Decision
        //   Layer 1 仅 cover Touch / 没 cover Editor Mouse pipeline) — Phase 2 closure ADR-010 §Implementation
        //   Guidelines Step 9 'Editor Mouse Adapter' amend 5-10 行 spec wording 落档。
        //
        //   V3.0.1 dp8 candidate "DevTestState [main-menu] mode 复用阈值阶进" — 加入 S6-13 后 [main-menu] mode
        //   HasSpike list 现 5 spike (S5-02 + S6-07 + S6-08 + S6-04 + S6-13 + S6-01-playtest)，远超原阈值 4，
        //   Sprint 6 retro 强制评估 V3.1 trigger pattern (central mode-dispatch refactor 候选)。
        //
        //   manual playtest session 复跑入口（保留 S6-01-playtest spike 注释行作 manual playtest 模式切换）：
        //   注释 S613Spike + 取消 S601PlaytestSpike 注释行即可切回 manual playtest hold mode。
        GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S613Spike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S601PlaytestSpike());
        // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S604Spike());
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
        // Sprint 6 emergent fix Track F vs-chapter-1-004: InputService Dispose 必须先于 FSM Destroy
        // (Utility.Unity.RemoveUpdateListener 需要 update driver alive；listener-path driver 反向操作)。
        if (_inputService != null)
        {
            _inputService.Dispose();
            _inputService = null;
            Log.Info("[GameApp] InputService disposed");
        }

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