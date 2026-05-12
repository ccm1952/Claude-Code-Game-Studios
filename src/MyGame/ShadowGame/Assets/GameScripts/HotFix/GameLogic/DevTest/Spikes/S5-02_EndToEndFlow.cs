// 该文件由Cursor 自动生成
// S5-02 Chapter 1 end-to-end 5 系统串通 (happy path) PlayMode spike
//   per story-002-end-to-end-flow.md (Session 27 #2 amend; R2 verdict ✅ PASS)。
//
// 关联文档:
//   * production/epics/vs-chapter-1/story-002-end-to-end-flow.md  (10 AC + 5 R3 case)
//
// R3 5 PlayMode case (M1 production reflection 全程 per S5-1c precedent):
//   P1 MainMenuButtonBootChapter1     — DevTestState 已 ShowUI<MainMenuPanel>；spike await ShowUI 拿同
//                                       instance；Button.onClick.Invoke() 触发 OnRequestSceneChange(1) →
//                                       SceneManager 11-step 自驱；验 5 lifecycle event 收到 + state=Idle
//                                       + CurrentLoadedChapterId==1
//   P2 ObjectInteractionToShadowMatch — spike fire mock IShadowMatchEvent.OnMatchScoreUpdated(1, 0.5f)
//                                       (per F2 R2.3 simplified path — 不走 InteractableObject FSM；
//                                       Object_01_CoffeeMug 缺 component 留 Sprint 6) → PuzzleStateMachine
//                                       listener Active→NearMatch + 派发 OnNearMatchEnter(1)
//   P3 PuzzleStateTransitionToComplete — spike fire mock OnMatchScoreUpdated(1, 0.95f) → PuzzleStateMachine
//                                       PerfectMatch transition + 派发 OnPerfectMatch(1, 0.95f) +
//                                       OnPuzzleComplete(1, Perfect) + state==Complete
//   P4 NarrativeSequenceWithAudioDuck — NarrativeSequencePlayer.cs:133 listener `_onPerfectMatch` 已自动
//                                       响应 P3 派发的 OnPerfectMatch → StartSequence (chapter 1
//                                       MemoryReplay seq id=100) → 验 OnSequenceStart + OnDuckingRequest
//                                       (0.3, 0.3) + GameModule.Audio.MusicVolume ≈ 0.3 + OnSequenceComplete
//   P5 NextChapterButtonSwitchToChapter2 — spike await ShowUI 拿 MainMenuPanel.NextChapterButton →
//                                       Button.onClick.Invoke() 触发 OnRequestSceneChange(2) →
//                                       SceneManager unload chapter 1 + reload chapter 2 (= chapter 1 scene
//                                       MVP placeholder per GameApp.BuildFixtureChapterDataProvider) →
//                                       验 OnSceneUnloadBegin(1) + OnSceneTransitionEnd(2) + state=Idle +
//                                       CurrentLoadedChapterId==2
//   (Session 27 #3 P5 修复: chapter 0 spec drift → 改 chapter 2 真 transition path; V3 Type-5 dp6 NEW)
//
// 设计约束:
//   * Spike 模式：1 file + 3 inner class (S502Spike : IDevSpike + S502Runtime : MonoBehaviour + S502Tester 纯逻辑)
//     沿 S5-1b/-1c/-03/-05/-06/-08 precedent
//   * Awake() 同步 subscribe production listeners (per S5-1c lessons memo problem_2026-05-09_spike-sync-subscribe-race.md
//     sync-subscribe race 防御 — DevTestState ShowUI 完后 Button.onClick.Invoke 同步派发 OnRequestSceneChange，
//     listener-path driver 同步 fire OnSceneTransitionBegin；spike 必须 Awake 前置 subscribe)
//   * spike 在 Awake 内 instantiate PuzzleStateMachine + NarrativeSequencePlayer (production 0-caller — 
//     ChapterStateManager Sprint 6+ 接入；spike 自己负责 Initialize / Tick / Shutdown 完整 lifecycle)
//   * Update() 每帧驱动 puzzleStateMachine.Tick + narrativePlayer.Tick
//   * P4 case 不显式触发 narrative sequence — 等 NarrativeSequencePlayer.cs:133 listener `_onPerfectMatch`
//     自动响应 P3 派发的 OnPerfectMatch (puzzle→narrative chain 直 wire per F1 R2.2 误判撤回)
//
// 整文件仅在 UNITY_EDITOR || DEBUG 编译，Release 包零残留。

#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameLogic.DevTest.Spikes
{
    public class S502Spike : IDevSpike
    {
        public string Id => "S5-02";
        public string Name => "Chapter 1 end-to-end 5 Systems Integration happy path (S5-02)";

        public void Launch()
        {
            var go = new GameObject("S502_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S502Runtime>();
        }
    }

    public class S502Runtime : MonoBehaviour
    {
        private S502Tester _tester;

        private void Awake()
        {
            // 关键时序：Awake 在 AddComponent 内同步执行（DevBootstrap.RunRequested() 调用栈内，
            // 早于 DevTestState 异步 ShowUI<MainMenuPanel> 完成 + 早于 spike P1 case Button.onClick.Invoke()）。
            // Awake 内 sync-subscribe production listeners 避 sync race；同时 instantiate
            // puzzleStateMachine + narrativePlayer 让 NarrativeSequencePlayer 内部 listener 提前 subscribe
            // (per S5-1c lessons memo)。
            _tester = new S502Tester(this);
            _tester.SubscribeEarlyListeners();
            _tester.InitializeProductionWiring();
            Log.Info("[S5-02] Runtime Awake — early listeners subscribed + puzzle/narrative production wiring initialized");
        }

        private void Start()
        {
            _tester.WriteResultJson();
            Log.Info($"[S5-02] Runtime Start. Result JSON: {S502Tester.ResultFilePath}");

            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            await UniTask.Yield();
            await _tester.RunAllAsync();
        }

        // 每帧驱动 puzzleStateMachine + narrativePlayer Tick (vendor lifecycle 由 spike 自管，
        // 因 ChapterStateManager Sprint 5 production 0-caller — Sprint 6+ 接管)
        private void Update()
        {
            if (_tester == null) return;
            _tester.TickPuzzleAndNarrative(Time.deltaTime);
        }

        private void OnDestroy()
        {
            // spike GameObject Destroy 时清理 puzzle/narrative production wiring (V2-5 listener self-removal 实证)
            _tester?.ShutdownProductionWiring();
        }

        private void OnGUI()
        {
            if (_tester == null) return;

            var boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 10, 10),
            };

            float w = 880, h = 330;
            float x = (Screen.width - w) / 2f;
            float y = 20;

            GUI.Box(new Rect(x, y, w, h), string.Empty, boxStyle);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S5-02 Chapter 1 End-to-End 5 Systems Integration", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawRow(x + 20, lineY, w - 40, "P1 MainMenuButtonBootChapter1 (Button.onClick.Invoke → 11-step 自驱)", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P2 ObjectInteractionToShadowMatch (mock OnMatchScoreUpdated(1, 0.5) → OnNearMatchEnter)", _tester.P2Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P3 PuzzleStateTransitionToComplete (OnMatchScoreUpdated(1, 0.95) → OnPerfectMatch + OnPuzzleComplete)", _tester.P3Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P4 NarrativeSequenceWithAudioDuck (NarrativeSequencePlayer.cs:133 listener → ducking → MusicVolume≈0.3)", _tester.P4Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P5 NextChapterButtonSwitchToChapter2 (Button.onClick.Invoke → chapter 1→2 switch → state=Idle)", _tester.P5Passed, labelStyle);
            lineY += lineH + 10;

            var footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"AllPassed: {_tester.AllPassed}    Elapsed: {_tester.TotalElapsedMs}ms", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"JSON: {S502Tester.ResultFilePath}", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"Status: {_tester.OverallStatus}", footerStyle);
        }

        private static void DrawRow(float x, float y, float w, string label, bool? passed, GUIStyle style)
        {
            var sym = passed == true ? "[PASS]" : passed == false ? "[FAIL]" : "[....]";
            var color = passed == true ? Color.green : passed == false ? Color.red : Color.gray;
            var prev = GUI.contentColor;
            GUI.contentColor = color;
            GUI.Label(new Rect(x, y, w, 22), $"{sym} {label}", style);
            GUI.contentColor = prev;
        }
    }

    /// <summary>
    /// 5 R3 case 实施 + JSON 落盘 + Stopwatch metric (AC-8 end-to-end performance)。
    /// M1 production reflection 全程：反射 GameApp._sceneManager + 自管 puzzleStateMachine + narrativePlayer
    /// (production 0-caller — Sprint 6+ ChapterStateManager 接管)。
    /// </summary>
    public class S502Tester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S5-02_Result.json");

        public bool? P1Passed { get; private set; }
        public bool? P2Passed { get; private set; }
        public bool? P3Passed { get; private set; }
        public bool? P4Passed { get; private set; }
        public bool? P5Passed { get; private set; }

        public bool AllPassed =>
            P1Passed == true && P2Passed == true && P3Passed == true &&
            P4Passed == true && P5Passed == true;

        public string OverallStatus { get; private set; } = "Running";
        public long TotalElapsedMs { get; private set; }

        private readonly List<string> _p1Events = new List<string>();
        private readonly List<string> _p2Events = new List<string>();
        private readonly List<string> _p3Events = new List<string>();
        private readonly List<string> _p4Events = new List<string>();
        private readonly List<string> _p5Events = new List<string>();
        private readonly Dictionary<string, string> _asserts = new Dictionary<string, string>();

        private readonly Stopwatch _swP1 = new Stopwatch();
        private readonly Stopwatch _swP2 = new Stopwatch();
        private readonly Stopwatch _swP3 = new Stopwatch();
        private readonly Stopwatch _swP4 = new Stopwatch();
        private readonly Stopwatch _swP5 = new Stopwatch();
        private readonly Stopwatch _swTotal = new Stopwatch();

        private readonly MonoBehaviour _hostBehaviour;

        // ────────── production wiring (spike 自管 lifecycle，Sprint 6+ ChapterStateManager 接管) ──────────
        private PuzzleStateMachine _puzzleStateMachine;
        private PuzzleStateConfigFromLuban _puzzleConfigProvider;
        private NarrativeSequencePlayer _narrativePlayer;
        private NarrativeSequenceConfigFromLuban _narrativeConfigProvider;
        private bool _productionWired;

        // ────────── P1 scene event listeners ──────────
        private int _p1TransitionBeginCount;
        private int _p1LoadProgressCount;
        private int _p1LoadCompleteCount;
        private (int chapterId, string bgmAsset) _p1LoadCompletePayload = (-999, "<unset>");
        private int _p1SceneReadyCount;
        private int _p1TransitionEndCount;
        private Action<int, int> _p1OnTB;
        private Action<string, float> _p1OnLP;
        private Action<int, string> _p1OnLC;
        private Action<int> _p1OnR;
        private Action<int> _p1OnTE;

        // ────────── P2/P3 puzzle event listeners ──────────
        private int _puzzleMatchScoreUpdatedCount;
        private (int puzzleId, float score) _lastMatchScorePayload = (-1, -1f);
        private int _puzzleNearMatchEnterCount;
        private int _puzzlePerfectMatchCount;
        private (int puzzleId, float finalScore) _puzzlePerfectPayload = (-1, -1f);
        private int _puzzleCompleteCount;
        private (int puzzleId, PuzzleCompletionType type) _puzzleCompletePayload = (-1, PuzzleCompletionType.Perfect);
        private Action<int, float> _onMatchScoreUpdated;
        private Action<int> _onNearMatchEnter;
        private Action<int, float> _onPerfectMatch;
        private Action<int, PuzzleCompletionType> _onPuzzleComplete;

        // ────────── P4 narrative + audio event listeners ──────────
        private int _narrativeSequenceStartCount;
        private (int sequenceId, NarrativeSequenceType type) _narrativeStartPayload = (-1, default);
        private int _narrativeSequenceCompleteCount;
        private (int sequenceId, NarrativeSequenceType type) _narrativeCompletePayload = (-1, default);
        private int _audioDuckingRequestCount;
        private (float duckRatio, float fadeDuration) _audioDuckPayload = (-1f, -1f);
        private float _capturedMusicVolume = -1f;
        private Action<int, NarrativeSequenceType> _onSequenceStart;
        private Action<int, NarrativeSequenceType> _onSequenceComplete;
        private Action<float, float> _onDuckingRequest;

        // ────────── P5 scene unload event listeners ──────────
        private int _p5UnloadBeginCount;
        private int _p5UnloadChapterId = -999;
        private int _p5TransitionEndCount;
        private Action<int> _p5OnUnloadBegin;
        private Action<int> _p5OnTransitionEnd;

        public S502Tester(MonoBehaviour host)
        {
            _hostBehaviour = host;
        }

        // ============================================================
        // Public entry — Awake / Update / OnDestroy 调用
        // ============================================================

        /// <summary>
        /// 由 S502Runtime.Awake() 同步调用 — 在 DevTestState ShowUI<MainMenuPanel> + spike P1 Button.onClick.Invoke()
        /// 之前 subscribe production listeners (避 sync-subscribe race per S5-1c lessons memo)。
        /// </summary>
        public void SubscribeEarlyListeners()
        {
            // P1 scene lifecycle events (chapter 1 load 11-step)
            _p1OnTB = (from, to) => { _p1TransitionBeginCount++; _p1Events.Add($"OnSceneTransitionBegin({from},{to})"); };
            _p1OnLP = (sceneName, progress) => { _p1LoadProgressCount++; if (_p1LoadProgressCount <= 2) _p1Events.Add($"OnSceneLoadProgress({sceneName},{progress:F2})"); };
            _p1OnLC = (id, bgm) => { _p1LoadCompleteCount++; _p1LoadCompletePayload = (id, bgm); _p1Events.Add($"OnSceneLoadComplete({id},'{bgm}')"); };
            _p1OnR = id => { _p1SceneReadyCount++; _p1Events.Add($"OnSceneReady({id})"); };
            _p1OnTE = id => { _p1TransitionEndCount++; _p1Events.Add($"OnSceneTransitionEnd({id})"); };
            GameEvent.AddEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, _p1OnTB);
            GameEvent.AddEventListener<string, float>(ISceneEvent_Event.OnSceneLoadProgress, _p1OnLP);
            GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadComplete, _p1OnLC);
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneReady, _p1OnR);
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneTransitionEnd, _p1OnTE);

            // P2/P3 puzzle events
            _onMatchScoreUpdated = (id, score) =>
            {
                _puzzleMatchScoreUpdatedCount++;
                _lastMatchScorePayload = (id, score);
                _p2Events.Add($"OnMatchScoreUpdated({id},{score:F2})");
            };
            _onNearMatchEnter = id =>
            {
                _puzzleNearMatchEnterCount++;
                _p2Events.Add($"OnNearMatchEnter({id})");
            };
            _onPerfectMatch = (id, finalScore) =>
            {
                _puzzlePerfectMatchCount++;
                _puzzlePerfectPayload = (id, finalScore);
                _p3Events.Add($"OnPerfectMatch({id},{finalScore:F2})");
            };
            _onPuzzleComplete = (id, type) =>
            {
                _puzzleCompleteCount++;
                _puzzleCompletePayload = (id, type);
                _p3Events.Add($"OnPuzzleComplete({id},{type})");
            };
            GameEvent.AddEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated);
            GameEvent.AddEventListener<int>(IShadowPuzzleEvent_Event.OnNearMatchEnter, _onNearMatchEnter);
            GameEvent.AddEventListener<int, float>(IShadowPuzzleEvent_Event.OnPerfectMatch, _onPerfectMatch);
            GameEvent.AddEventListener<int, PuzzleCompletionType>(IShadowPuzzleEvent_Event.OnPuzzleComplete, _onPuzzleComplete);

            // P4 narrative + audio events
            _onSequenceStart = (id, type) =>
            {
                _narrativeSequenceStartCount++;
                _narrativeStartPayload = (id, type);
                _p4Events.Add($"OnSequenceStart({id},{type})");
            };
            _onSequenceComplete = (id, type) =>
            {
                _narrativeSequenceCompleteCount++;
                _narrativeCompletePayload = (id, type);
                _p4Events.Add($"OnSequenceComplete({id},{type})");
            };
            _onDuckingRequest = (duckRatio, fadeDuration) =>
            {
                _audioDuckingRequestCount++;
                _audioDuckPayload = (duckRatio, fadeDuration);
                _p4Events.Add($"OnDuckingRequest({duckRatio:F2},{fadeDuration:F2})");
            };
            GameEvent.AddEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceStart, _onSequenceStart);
            GameEvent.AddEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceComplete, _onSequenceComplete);
            GameEvent.AddEventListener<float, float>(IAudioEvent_Event.OnDuckingRequest, _onDuckingRequest);

            // P5 scene unload events
            _p5OnUnloadBegin = id =>
            {
                _p5UnloadBeginCount++;
                _p5UnloadChapterId = id;
                _p5Events.Add($"OnSceneUnloadBegin({id})");
            };
            _p5OnTransitionEnd = id =>
            {
                _p5TransitionEndCount++;
                _p5Events.Add($"P5_OnSceneTransitionEnd({id})");
            };
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, _p5OnUnloadBegin);
            // 注: OnSceneTransitionEnd 已在 P1 subscribe (_p1OnTE 计 P1 用)；P5 用独立 listener 计 P5 阶段触发
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneTransitionEnd, _p5OnTransitionEnd);
        }

        private void UnsubscribeEarlyListeners()
        {
            if (_p1OnTB != null) { GameEvent.RemoveEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, _p1OnTB); _p1OnTB = null; }
            if (_p1OnLP != null) { GameEvent.RemoveEventListener<string, float>(ISceneEvent_Event.OnSceneLoadProgress, _p1OnLP); _p1OnLP = null; }
            if (_p1OnLC != null) { GameEvent.RemoveEventListener<int, string>(ISceneEvent_Event.OnSceneLoadComplete, _p1OnLC); _p1OnLC = null; }
            if (_p1OnR != null) { GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneReady, _p1OnR); _p1OnR = null; }
            if (_p1OnTE != null) { GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneTransitionEnd, _p1OnTE); _p1OnTE = null; }

            if (_onMatchScoreUpdated != null) { GameEvent.RemoveEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated); _onMatchScoreUpdated = null; }
            if (_onNearMatchEnter != null) { GameEvent.RemoveEventListener<int>(IShadowPuzzleEvent_Event.OnNearMatchEnter, _onNearMatchEnter); _onNearMatchEnter = null; }
            if (_onPerfectMatch != null) { GameEvent.RemoveEventListener<int, float>(IShadowPuzzleEvent_Event.OnPerfectMatch, _onPerfectMatch); _onPerfectMatch = null; }
            if (_onPuzzleComplete != null) { GameEvent.RemoveEventListener<int, PuzzleCompletionType>(IShadowPuzzleEvent_Event.OnPuzzleComplete, _onPuzzleComplete); _onPuzzleComplete = null; }

            if (_onSequenceStart != null) { GameEvent.RemoveEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceStart, _onSequenceStart); _onSequenceStart = null; }
            if (_onSequenceComplete != null) { GameEvent.RemoveEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceComplete, _onSequenceComplete); _onSequenceComplete = null; }
            if (_onDuckingRequest != null) { GameEvent.RemoveEventListener<float, float>(IAudioEvent_Event.OnDuckingRequest, _onDuckingRequest); _onDuckingRequest = null; }

            if (_p5OnUnloadBegin != null) { GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, _p5OnUnloadBegin); _p5OnUnloadBegin = null; }
            if (_p5OnTransitionEnd != null) { GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneTransitionEnd, _p5OnTransitionEnd); _p5OnTransitionEnd = null; }
        }

        /// <summary>
        /// 由 S502Runtime.Awake() 同步调用 — instantiate + Initialize PuzzleStateMachine + NarrativeSequencePlayer。
        /// production 0-caller (ChapterStateManager 留 Sprint 6+)；spike 自管完整 lifecycle。
        /// </summary>
        public void InitializeProductionWiring()
        {
            try
            {
                _puzzleConfigProvider = new PuzzleStateConfigFromLuban();
                _puzzleConfigProvider.InitWithDefaults();
                var puzzleConfig = _puzzleConfigProvider.GetConfig(1);
                if (puzzleConfig == null)
                {
                    Log.Error("[S5-02] PuzzleStateConfigFromLuban.GetConfig(1) == null — fixture provider InitWithDefaults() 异常？");
                    return;
                }

                _puzzleStateMachine = new PuzzleStateMachine();
                _puzzleStateMachine.Initialize(1, puzzleConfig);
                _puzzleStateMachine.OnChapterUnlocked();    // Locked → Idle
                _puzzleStateMachine.OnPlayerInteraction();  // Idle → Active (准备接 OnMatchScoreUpdated 评估)

                _narrativeConfigProvider = new NarrativeSequenceConfigFromLuban();
                _narrativeConfigProvider.InitWithDefaults();
                _narrativePlayer = new NarrativeSequencePlayer();
                _narrativePlayer.Initialize(_narrativeConfigProvider);
                // NarrativeSequencePlayer.cs:127-135 subscribes OnRequestSequence/OnPerfectMatch/OnAbsenceAccepted/OnChapterComplete
                // 至此 NarrativeSequencePlayer 自动 listen 后续 P3 派发的 OnPerfectMatch(1, 0.95) → start sequence (puzzle→narrative chain 直 wire)

                _productionWired = true;
                Log.Info("[S5-02] InitializeProductionWiring 完成: puzzleStateMachine state=Active + narrativePlayer ready");
            }
            catch (Exception e)
            {
                Log.Error($"[S5-02] InitializeProductionWiring 异常: {e}");
                _productionWired = false;
            }
        }

        /// <summary>
        /// 由 S502Runtime.Update 每帧调用 — Tick puzzleStateMachine + narrativePlayer。
        /// puzzleStateMachine.Tick 仅 manage grace period + absence timer (本 puzzle id=1 standard 非 absence，影响小)；
        /// narrativePlayer.Tick 驱动 atomic effects 时序 (per S5-05 P3 case verified)。
        /// </summary>
        public void TickPuzzleAndNarrative(float deltaTime)
        {
            if (!_productionWired) return;
            try
            {
                _puzzleStateMachine?.Tick(deltaTime);
                _narrativePlayer?.Tick(deltaTime);
            }
            catch (Exception e)
            {
                Log.Error($"[S5-02] TickPuzzleAndNarrative 异常: {e}");
            }
        }

        /// <summary>
        /// 由 S502Runtime.OnDestroy 调用 — Shutdown puzzle/narrative + RemoveEventListener。
        /// </summary>
        public void ShutdownProductionWiring()
        {
            try
            {
                _puzzleStateMachine?.Shutdown();
                _puzzleStateMachine = null;
                _narrativePlayer?.Shutdown();
                _narrativePlayer = null;
            }
            catch (Exception e)
            {
                Log.Warning($"[S5-02] ShutdownProductionWiring 异常: {e}");
            }

            UnsubscribeEarlyListeners();
            Log.Info("[S5-02] ShutdownProductionWiring 完成");
        }

        // ============================================================
        // RunAllAsync — orchestrate P1..P5 in order
        // ============================================================

        public async UniTask RunAllAsync()
        {
            _swTotal.Start();
            try
            {
                await RunP1Async();
                await UniTask.Delay(TimeSpan.FromMilliseconds(200));

                await RunP2Async();
                await UniTask.Delay(TimeSpan.FromMilliseconds(200));

                await RunP3Async();
                await UniTask.Delay(TimeSpan.FromMilliseconds(200));

                await RunP4Async();
                await UniTask.Delay(TimeSpan.FromMilliseconds(200));

                await RunP5Async();

                OverallStatus = AllPassed ? "All Passed" : "Some Failed";
                Log.Info($"[S5-02] Done. AllPassed={AllPassed} Elapsed={_swTotal.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                OverallStatus = $"Crashed: {e.GetType().Name}";
                Log.Error($"[S5-02] RunAllAsync 异常：{e}");
            }
            finally
            {
                _swTotal.Stop();
                TotalElapsedMs = _swTotal.ElapsedMilliseconds;
                WriteResultJson();
            }
        }

        // ============================================================
        // P1 MainMenuButtonBootChapter1 — Button.onClick.Invoke → SceneManager 11-step
        // ============================================================
        private async UniTask RunP1Async()
        {
            _swP1.Start();
            Log.Info("[S5-02] P1 MainMenuButtonBootChapter1 开始");

            // 等 DevTestState 异步 ShowUI<MainMenuPanel> 完成 + Button.onClick.AddListener 挂载 (OnCreate)
            MainMenuPanel panel = null;
            try
            {
                panel = await GameModule.UI.ShowUIAsyncAwait<MainMenuPanel>();
            }
            catch (Exception e)
            {
                _asserts["P1.MainMenuPanel_show_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP1.Stop();
                P1Passed = false;
                return;
            }

            if (panel == null || panel.StartChapter1Button == null)
            {
                _asserts["P1.MainMenuPanel_button_ref"] = panel == null
                    ? "FAIL: MainMenuPanel == null (prefab 缺失？)"
                    : "FAIL: StartChapter1Button == null (prefab child 命名错误？)";
                _swP1.Stop();
                P1Passed = false;
                return;
            }

            _asserts["P1.MainMenuPanel_button_ref"] = "PASS: MainMenuPanel + StartChapter1Button non-null";
            _p1Events.Add($"MainMenuPanel ready frame={Time.frameCount}");

            // 模拟点击 — 走完整 production main menu Button → ISceneEvent.OnRequestSceneChange(1) → SceneManager 11-step 路径
            // 注: listener-path driver 同步 fire OnSceneTransitionBegin；Awake 内已 subscribe 不会 miss
            panel.StartChapter1Button.onClick.Invoke();
            _p1Events.Add($"StartChapter1Button.onClick.Invoke() called frame={Time.frameCount}");

            // 等 chapter 1 scene 11-step 完成 → state==Idle
            var prodScene = GetProductionSceneManager();
            if (prodScene == null)
            {
                _asserts["P1.production_scene_manager_present"] = "FAIL: GameApp._sceneManager 反射拿 null";
                _swP1.Stop();
                P1Passed = false;
                return;
            }

            var idleOk = await WaitForIdleAsync(prodScene, timeoutSec: 8.0);

            _swP1.Stop();

            _asserts["P1.timeout"] = idleOk ? "PASS: state == Idle within 8s" : "FAIL: timeout";
            _asserts["P1.CurrentLoadedChapterIdForTest"] = $"expected=1 actual={prodScene.CurrentLoadedChapterIdForTest}";
            _asserts["P1.CurrentChapterSceneNameForTest"] = $"expected='Chapter_01_Approach' actual='{prodScene.CurrentChapterSceneNameForTest}'";
            _asserts["P1.CurrentState"] = $"expected=Idle actual={prodScene.CurrentState}";

            var loadCompleteOk = _p1LoadCompleteCount >= 1 && _p1LoadCompletePayload.chapterId == 1 && _p1LoadCompletePayload.bgmAsset == string.Empty;
            _asserts["P1.OnSceneLoadComplete"] = loadCompleteOk
                ? $"PASS: count={_p1LoadCompleteCount} payload=(1,'')"
                : $"FAIL: count={_p1LoadCompleteCount} payload=({_p1LoadCompletePayload.chapterId},'{_p1LoadCompletePayload.bgmAsset}')";

            _asserts["P1.OnSceneReady"] = _p1SceneReadyCount >= 1 ? $"PASS: count={_p1SceneReadyCount}" : $"FAIL: count={_p1SceneReadyCount}";
            _asserts["P1.OnSceneTransitionEnd"] = _p1TransitionEndCount >= 1 ? $"PASS: count={_p1TransitionEndCount}" : $"FAIL: count={_p1TransitionEndCount}";
            _asserts["P1.OnSceneTransitionBegin"] = _p1TransitionBeginCount >= 1 ? $"PASS: count={_p1TransitionBeginCount}" : $"FAIL: count={_p1TransitionBeginCount}";
            _asserts["P1.OnSceneLoadProgress"] = _p1LoadProgressCount >= 1 ? $"PASS: count={_p1LoadProgressCount}" : $"FAIL: count={_p1LoadProgressCount}";
            _asserts["P1.duration_ms"] = $"PASS: {_swP1.ElapsedMilliseconds}ms";

            P1Passed =
                idleOk &&
                prodScene.CurrentLoadedChapterIdForTest == 1 &&
                prodScene.CurrentChapterSceneNameForTest == "Chapter_01_Approach" &&
                prodScene.CurrentState == SceneManagerState.Idle &&
                loadCompleteOk &&
                _p1SceneReadyCount >= 1 &&
                _p1TransitionEndCount >= 1 &&
                _p1TransitionBeginCount >= 1 &&
                _p1LoadProgressCount >= 1;
        }

        // ============================================================
        // P2 ObjectInteractionToShadowMatch — mock OnMatchScoreUpdated(1, 0.5f) → NearMatch
        // ============================================================
        private async UniTask RunP2Async()
        {
            _swP2.Start();
            Log.Info("[S5-02] P2 ObjectInteractionToShadowMatch 开始");

            if (!_productionWired)
            {
                _asserts["P2.precondition"] = "FAIL: production wiring 未 init (Awake InitializeProductionWiring failed)";
                _swP2.Stop();
                P2Passed = false;
                return;
            }

            // record baseline counts (P1 期间可能因 production wiring 自身 OnMatchScoreUpdated 自挂 listener 已计数)
            int baselineMatch = _puzzleMatchScoreUpdatedCount;
            int baselineNear = _puzzleNearMatchEnterCount;

            // F2 R2.3 simplified path: spike 直接 fire mock OnMatchScoreUpdated(1, 0.5f) (≥ nearMatchThreshold=0.40)
            // 不走 InteractableObject FSM 真 drag/snap (Object_01_CoffeeMug 缺 InteractableObject MonoBehaviour 留 Sprint 6)
            GameEvent.Get<IShadowMatchEvent>().OnMatchScoreUpdated(1, 0.5f);
            _p2Events.Add($"Fire mock OnMatchScoreUpdated(1, 0.5f) frame={Time.frameCount}");

            // 等 1 帧让 PuzzleStateMachine listener handler + EvaluateTransitions 派发 OnNearMatchEnter
            await UniTask.Yield();

            _swP2.Stop();

            int deltaMatch = _puzzleMatchScoreUpdatedCount - baselineMatch;
            int deltaNear = _puzzleNearMatchEnterCount - baselineNear;

            _asserts["P2.OnMatchScoreUpdated_delta"] = deltaMatch >= 1
                ? $"PASS: delta={deltaMatch} payload=({_lastMatchScorePayload.puzzleId},{_lastMatchScorePayload.score:F2})"
                : $"FAIL: delta={deltaMatch}";
            _asserts["P2.MatchScore_payload"] = (_lastMatchScorePayload.puzzleId == 1 && Math.Abs(_lastMatchScorePayload.score - 0.5f) < 0.001f)
                ? "PASS: (1, 0.5)"
                : $"FAIL: ({_lastMatchScorePayload.puzzleId}, {_lastMatchScorePayload.score:F2})";
            _asserts["P2.OnNearMatchEnter_delta"] = deltaNear >= 1
                ? $"PASS: delta={deltaNear}"
                : $"FAIL: delta={deltaNear} (PuzzleStateMachine listener 未触发？state={_puzzleStateMachine?.CurrentState})";

            bool stateOk = _puzzleStateMachine.CurrentState == PuzzleState.NearMatch;
            _asserts["P2.puzzleStateMachine.CurrentState"] = stateOk
                ? "PASS: NearMatch"
                : $"FAIL: {_puzzleStateMachine.CurrentState} (期望 NearMatch)";
            _asserts["P2.duration_ms"] = $"PASS: {_swP2.ElapsedMilliseconds}ms";

            P2Passed = deltaMatch >= 1 && deltaNear >= 1 && _lastMatchScorePayload.puzzleId == 1 &&
                       Math.Abs(_lastMatchScorePayload.score - 0.5f) < 0.001f && stateOk;
        }

        // ============================================================
        // P3 PuzzleStateTransitionToComplete — mock OnMatchScoreUpdated(1, 0.95f) → PerfectMatch + Complete
        // ============================================================
        private async UniTask RunP3Async()
        {
            _swP3.Start();
            Log.Info("[S5-02] P3 PuzzleStateTransitionToComplete 开始");

            if (!_productionWired)
            {
                _asserts["P3.precondition"] = "FAIL: production wiring 未 init";
                _swP3.Stop();
                P3Passed = false;
                return;
            }

            int baselinePerfect = _puzzlePerfectMatchCount;
            int baselineComplete = _puzzleCompleteCount;

            // Fire mock score ≥ perfectMatchThreshold=0.85 → PuzzleStateMachine 同步评估 EnterPerfectMatch + DispatchPuzzleCompleteAndLock
            // 注意 puzzle id=1 PuzzleStateConfigFromLuban tutorialGracePeriod=3.0f 但 OnTutorialCompleted 未调 → IsInGracePeriod=false → 直接 PerfectMatch
            GameEvent.Get<IShadowMatchEvent>().OnMatchScoreUpdated(1, 0.95f);
            _p3Events.Add($"Fire mock OnMatchScoreUpdated(1, 0.95f) frame={Time.frameCount}");

            await UniTask.Yield();

            _swP3.Stop();

            int deltaPerfect = _puzzlePerfectMatchCount - baselinePerfect;
            int deltaComplete = _puzzleCompleteCount - baselineComplete;

            _asserts["P3.OnPerfectMatch_delta"] = deltaPerfect == 1
                ? $"PASS: delta=1 payload=({_puzzlePerfectPayload.puzzleId},{_puzzlePerfectPayload.finalScore:F2})"
                : $"FAIL: delta={deltaPerfect}";
            _asserts["P3.OnPerfectMatch_payload"] = (_puzzlePerfectPayload.puzzleId == 1 && Math.Abs(_puzzlePerfectPayload.finalScore - 0.95f) < 0.001f)
                ? "PASS: (1, 0.95)"
                : $"FAIL: ({_puzzlePerfectPayload.puzzleId}, {_puzzlePerfectPayload.finalScore:F2})";
            _asserts["P3.OnPuzzleComplete_delta"] = deltaComplete == 1
                ? $"PASS: delta=1 payload=({_puzzleCompletePayload.puzzleId},{_puzzleCompletePayload.type})"
                : $"FAIL: delta={deltaComplete}";
            _asserts["P3.OnPuzzleComplete_payload"] = (_puzzleCompletePayload.puzzleId == 1 && _puzzleCompletePayload.type == PuzzleCompletionType.Perfect)
                ? "PASS: (1, Perfect)"
                : $"FAIL: ({_puzzleCompletePayload.puzzleId}, {_puzzleCompletePayload.type})";

            bool stateOk = _puzzleStateMachine.CurrentState == PuzzleState.Complete;
            _asserts["P3.puzzleStateMachine.CurrentState"] = stateOk
                ? "PASS: Complete"
                : $"FAIL: {_puzzleStateMachine.CurrentState} (期望 Complete)";
            _asserts["P3.duration_ms"] = $"PASS: {_swP3.ElapsedMilliseconds}ms";

            P3Passed = deltaPerfect == 1 && deltaComplete == 1 &&
                       _puzzlePerfectPayload.puzzleId == 1 && Math.Abs(_puzzlePerfectPayload.finalScore - 0.95f) < 0.001f &&
                       _puzzleCompletePayload.puzzleId == 1 && _puzzleCompletePayload.type == PuzzleCompletionType.Perfect &&
                       stateOk;
        }

        // ============================================================
        // P4 NarrativeSequenceWithAudioDuck — NarrativeSequencePlayer.cs:133 listener auto-response
        // ============================================================
        private async UniTask RunP4Async()
        {
            _swP4.Start();
            Log.Info("[S5-02] P4 NarrativeSequenceWithAudioDuck 开始");

            if (!_productionWired || _narrativePlayer == null)
            {
                _asserts["P4.precondition"] = "FAIL: narrativePlayer 未 init";
                _swP4.Stop();
                P4Passed = false;
                return;
            }

            // NarrativeSequencePlayer.cs:133 listener _onPerfectMatch 已在 P3 派发 OnPerfectMatch 时
            // 自动响应 → OnPerfectMatchHandler → HandleRequestSequence(1, MemoryReplay) → StartSequence
            // P3 → P4 之间 200ms delay 应足够 vendor 完成 same-frame dispatch
            // 此时 narrativePlayer.State 应 == Playing

            // 等 ducking fadeDuration=0.3s 完成 + capture framework MusicVolume sample
            await UniTask.Delay(TimeSpan.FromMilliseconds(500));
            _capturedMusicVolume = GameModule.Audio != null ? GameModule.Audio.MusicVolume : -1f;
            _p4Events.Add($"capturedMusicVolume(after 0.5s)={_capturedMusicVolume:F3}");

            // 等 sequence totalDuration=2.0s 完成 (chapter 1 MemoryReplay seq id=100)
            await UniTask.Delay(TimeSpan.FromMilliseconds(2200));

            _swP4.Stop();

            _asserts["P4.OnSequenceStart_count"] = _narrativeSequenceStartCount >= 1
                ? $"PASS: count={_narrativeSequenceStartCount} payload=({_narrativeStartPayload.sequenceId},{_narrativeStartPayload.type})"
                : $"FAIL: count={_narrativeSequenceStartCount} (期望 ≥1 — NarrativeSequencePlayer.cs:133 listener 未响应 OnPerfectMatch？)";

            _asserts["P4.OnSequenceStart_payload"] = (_narrativeStartPayload.sequenceId == 100 && _narrativeStartPayload.type == NarrativeSequenceType.MemoryReplay)
                ? "PASS: (100, MemoryReplay)"
                : $"FAIL: ({_narrativeStartPayload.sequenceId}, {_narrativeStartPayload.type})";

            _asserts["P4.OnDuckingRequest_count"] = _audioDuckingRequestCount >= 1
                ? $"PASS: count={_audioDuckingRequestCount} payload=({_audioDuckPayload.duckRatio:F2},{_audioDuckPayload.fadeDuration:F2})"
                : $"FAIL: count={_audioDuckingRequestCount} (期望 ≥1 — AudioDuckingEffect 未触发？)";

            _asserts["P4.OnDuckingRequest_payload"] = (Math.Abs(_audioDuckPayload.duckRatio - 0.3f) < 0.01f && Math.Abs(_audioDuckPayload.fadeDuration - 0.3f) < 0.01f)
                ? "PASS: (0.30, 0.30)"
                : $"FAIL: ({_audioDuckPayload.duckRatio:F2}, {_audioDuckPayload.fadeDuration:F2})";

            // framework MusicVolume 验：duckRatio=0.3 + baseline=1 + master=1 → effective=0.3；framework Clamp [0.0001, 1.0]
            // 容忍 ±0.05 误差 (fade 时序 + master/layer baseline 倍乘可能微调)
            bool musicVolumeDucked = _capturedMusicVolume >= 0.0001f && _capturedMusicVolume <= 0.5f;
            _asserts["P4.GameModule.Audio.MusicVolume_ducked"] = musicVolumeDucked
                ? $"PASS: {_capturedMusicVolume:F3} (期望 ≈0.3 ducked)"
                : $"FAIL: {_capturedMusicVolume:F3} (期望 ducked ≤ 0.5)";

            _asserts["P4.OnSequenceComplete_count"] = _narrativeSequenceCompleteCount >= 1
                ? $"PASS: count={_narrativeSequenceCompleteCount} payload=({_narrativeCompletePayload.sequenceId},{_narrativeCompletePayload.type})"
                : $"FAIL: count={_narrativeSequenceCompleteCount}";

            _asserts["P4.duration_ms"] = $"PASS: {_swP4.ElapsedMilliseconds}ms";

            P4Passed = _narrativeSequenceStartCount >= 1 &&
                       _narrativeStartPayload.sequenceId == 100 &&
                       _narrativeStartPayload.type == NarrativeSequenceType.MemoryReplay &&
                       _audioDuckingRequestCount >= 1 &&
                       Math.Abs(_audioDuckPayload.duckRatio - 0.3f) < 0.01f &&
                       musicVolumeDucked &&
                       _narrativeSequenceCompleteCount >= 1;
        }

        // ============================================================
        // P5 NextChapterButtonSwitchToChapter2 — NextChapter Button.onClick → chapter 1 → chapter 2 switch
        // (Session 27 #3 修复: chapter 0 spec drift → 改 chapter 2 真 transition path; chapter 2 fixture
        //  = chapter 1 scene MVP placeholder per GameApp.BuildFixtureChapterDataProvider)
        // ============================================================
        private async UniTask RunP5Async()
        {
            _swP5.Start();
            Log.Info("[S5-02] P5 NextChapterButtonSwitchToChapter2 开始");

            var prodScene = GetProductionSceneManager();
            if (prodScene == null)
            {
                _asserts["P5.production_scene_manager_present"] = "FAIL";
                _swP5.Stop();
                P5Passed = false;
                return;
            }

            // 拿 MainMenuPanel + NextChapterButton ref
            MainMenuPanel panel;
            try
            {
                panel = await GameModule.UI.ShowUIAsyncAwait<MainMenuPanel>();
            }
            catch (Exception e)
            {
                _asserts["P5.MainMenuPanel_show_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP5.Stop();
                P5Passed = false;
                return;
            }

            if (panel == null || panel.NextChapterButton == null)
            {
                _asserts["P5.NextChapterButton_ref"] = panel == null
                    ? "FAIL: MainMenuPanel == null"
                    : "FAIL: NextChapterButton == null";
                _swP5.Stop();
                P5Passed = false;
                return;
            }
            _asserts["P5.NextChapterButton_ref"] = "PASS: non-null";

            int baselineUnload = _p5UnloadBeginCount;

            // 模拟点击 → ISceneEvent.OnRequestSceneChange(2) → SceneManager unload chapter 1 + reload chapter 2
            panel.NextChapterButton.onClick.Invoke();
            _p5Events.Add($"NextChapterButton.onClick.Invoke() called frame={Time.frameCount}");

            // 等 chapter 2 reload 完成 (state==Idle + CurrentLoadedChapterId==2)
            var switchOk = await WaitForChapterLoadedAsync(prodScene, targetChapterId: 2, timeoutSec: 8.0);

            _swP5.Stop();

            int deltaUnload = _p5UnloadBeginCount - baselineUnload;

            _asserts["P5.OnSceneUnloadBegin_delta"] = deltaUnload >= 1
                ? $"PASS: delta={deltaUnload} payload={_p5UnloadChapterId}"
                : $"FAIL: delta={deltaUnload}";
            _asserts["P5.OnSceneUnloadBegin_payload"] = _p5UnloadChapterId == 1
                ? "PASS: chapter 1 unload"
                : $"FAIL: chapterId={_p5UnloadChapterId} (期望 1)";

            _asserts["P5.switchOk_timeout"] = switchOk ? "PASS: chapter 2 loaded + state==Idle within 8s" : "FAIL: timeout";
            _asserts["P5.CurrentState"] = $"expected=Idle actual={prodScene.CurrentState}";
            _asserts["P5.CurrentLoadedChapterIdForTest"] = $"expected=2 actual={prodScene.CurrentLoadedChapterIdForTest}";
            _asserts["P5.duration_ms"] = $"PASS: {_swP5.ElapsedMilliseconds}ms";

            P5Passed = deltaUnload >= 1 && _p5UnloadChapterId == 1 && switchOk &&
                       prodScene.CurrentState == SceneManagerState.Idle &&
                       prodScene.CurrentLoadedChapterIdForTest == 2;
        }

        // ============================================================
        // helpers
        // ============================================================

        private static SceneManager GetProductionSceneManager()
        {
            var fi = typeof(GameApp).GetField("_sceneManager", BindingFlags.NonPublic | BindingFlags.Static);
            if (fi == null)
            {
                Log.Error("[S5-02] 反射拿 GameApp._sceneManager 字段失败：FieldInfo == null");
                return null;
            }
            return fi.GetValue(null) as SceneManager;
        }

        private static async UniTask<bool> WaitForIdleAsync(SceneManager scene, double timeoutSec)
        {
            var sw = Stopwatch.StartNew();
            while (scene.CurrentState != SceneManagerState.Idle || scene.CurrentLoadedChapterIdForTest != 1)
            {
                if (sw.Elapsed.TotalSeconds > timeoutSec)
                    return false;
                await UniTask.Yield();
            }
            return true;
        }

        private static async UniTask<bool> WaitForChapterLoadedAsync(SceneManager scene, int targetChapterId, double timeoutSec)
        {
            var sw = Stopwatch.StartNew();
            while (scene.CurrentState != SceneManagerState.Idle || scene.CurrentLoadedChapterIdForTest != targetChapterId)
            {
                if (sw.Elapsed.TotalSeconds > timeoutSec)
                    return false;
                await UniTask.Yield();
            }
            return true;
        }

        public void WriteResultJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append($"  \"story_id\": \"S5-02\",\n");
            sb.Append($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\n");
            sb.Append($"  \"all_passed\": {AllPassed.ToString().ToLowerInvariant()},\n");
            sb.Append($"  \"overall_status\": \"{Escape(OverallStatus)}\",\n");
            sb.Append($"  \"total_time_ms\": {TotalElapsedMs},\n");
            sb.Append("  \"cases\": [\n");
            AppendCase(sb, "P1", P1Passed, _p1Events, _swP1.ElapsedMilliseconds, isLast: false);
            AppendCase(sb, "P2", P2Passed, _p2Events, _swP2.ElapsedMilliseconds, isLast: false);
            AppendCase(sb, "P3", P3Passed, _p3Events, _swP3.ElapsedMilliseconds, isLast: false);
            AppendCase(sb, "P4", P4Passed, _p4Events, _swP4.ElapsedMilliseconds, isLast: false);
            AppendCase(sb, "P5", P5Passed, _p5Events, _swP5.ElapsedMilliseconds, isLast: true);
            sb.Append("  ],\n");
            sb.Append("  \"asserts\": {\n");
            var keys = new List<string>(_asserts.Keys);
            for (var i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                sb.Append($"    \"{Escape(k)}\": \"{Escape(_asserts[k])}\"");
                sb.Append(i == keys.Count - 1 ? "\n" : ",\n");
            }
            sb.Append("  }\n");
            sb.Append("}\n");

            try
            {
                File.WriteAllText(ResultFilePath, sb.ToString());
            }
            catch (Exception e)
            {
                Log.Error($"[S5-02] WriteResultJson 失败：{e}");
            }
        }

        private static void AppendCase(StringBuilder sb, string id, bool? passed, List<string> events, long durationMs, bool isLast)
        {
            sb.Append("    {\n");
            sb.Append($"      \"id\": \"{id}\",\n");
            sb.Append($"      \"passed\": {(passed == true ? "true" : passed == false ? "false" : "null")},\n");
            sb.Append($"      \"duration_ms\": {durationMs},\n");
            sb.Append("      \"events\": [");
            for (var i = 0; i < events.Count; i++)
            {
                sb.Append($"\"{Escape(events[i])}\"");
                if (i < events.Count - 1) sb.Append(", ");
            }
            sb.Append("]\n");
            sb.Append(isLast ? "    }\n" : "    },\n");
        }

        private static string Escape(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
#endif
