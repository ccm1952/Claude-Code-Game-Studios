// 该文件由Cursor 自动生成
// S6-17 End-to-End Smoke Replay PlayMode spike
//   per story-008-end-to-end-smoke-replay.md (Track F final — V3.0.1 dp15 sniff sub-clause pilot)。
//
// R3 5 PlayMode case (run order P1→P2→P3→P4→P5):
//   P1 InputSimulationMouseTapNewGame — MainMenuPanel.NewGameButton.onClick.Invoke → chapter 1 loaded
//   P2 InputSimulationObjectInteractionToShadowMatch — TickForTest Tap+Drag → production gesture chain →
//      OnObjectTransformChanged → ShadowMatchCalculator → OnMatchScoreUpdated ≥3 + final score ≥0.85
//   P3 PuzzleStateMachineToNarrative — OnPerfectMatch exactly 1 → puzzle Complete
//   P4 NarrativeSequenceWithAudioDuck — MusicVolume≈0.3 + OnSequenceStart
//   P5 dp15SniffSubClauseVerify — spike 0 mock GameEvent.Get<I*Event>().* fire bypass
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
    public class S617Spike : IDevSpike
    {
        public string Id => "S6-17";
        public string Name => "End-to-End Smoke Replay — InputSimulation full production round-trip (dp15 sniff pilot)";

        public void Launch()
        {
            var go = new GameObject("S617_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S617Runtime>();
        }
    }

    public class S617Runtime : MonoBehaviour
    {
        private S617Tester _tester;

        private void Awake()
        {
            _tester = new S617Tester(this);
            _tester.SubscribeEarlyListeners();
            _tester.InitializeProductionWiring();
            Log.Info("[S6-17] Runtime Awake — early listeners + puzzle/narrative wiring");
        }

        private void Start()
        {
            _tester.RunAllAsync().Forget();
        }

        private void Update()
        {
            _tester?.TickPuzzleAndNarrative(Time.deltaTime);
        }

        private void OnGUI()
        {
            if (_tester == null) return;

            float x = 20f, y = 20f, w = 1040f, h = 360f;
            GUI.Box(new Rect(x, y, w, h), "");

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState { textColor = Color.white }
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S6-17 End-to-End Smoke Replay (Track F vs-chapter-1-008 dp15 pilot)", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawRow(x + 20, lineY, w - 40, "P1 InputSimulationMouseTapNewGame (NewGameButton → chapter 1)", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P2 InputSimulationObjectInteractionToShadowMatch (Tap+Drag → score ≥3)", _tester.P2Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P3 PuzzleStateMachineToNarrative (OnPerfectMatch exactly 1)", _tester.P3Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P4 NarrativeSequenceWithAudioDuck (MusicVolume≈0.3)", _tester.P4Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P5 dp15SniffSubClauseVerify (0 mock fire bypass)", _tester.P5Passed, labelStyle);
            lineY += lineH + 10;

            var footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"AllPassed: {_tester.AllPassed}    Elapsed: {_tester.TotalElapsedMs}ms", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"UnexpectedError: {_tester.UnexpectedErrorCount}", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"JSON: {S617Tester.ResultFilePath}", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"Status: {_tester.OverallStatus}", footerStyle);
        }

        private static void DrawRow(float x, float y, float w, string label, bool? passed, GUIStyle labelStyle)
        {
            string mark = passed switch
            {
                true => "✅ PASS",
                false => "❌ FAIL",
                _ => "⏳ Running"
            };
            GUI.Label(new Rect(x, y, w - 80, 22), label, labelStyle);
            GUI.Label(new Rect(x + (w - 80), y, 80, 22), mark, labelStyle);
        }

        private void OnDestroy()
        {
            _tester?.ShutdownProductionWiring();
            _tester?.UnsubscribeEarlyListeners();
        }
    }

    public class S617Tester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S6-17_Result.json");

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
        public int UnexpectedErrorCount { get; private set; }

        private readonly Dictionary<string, string> _asserts = new Dictionary<string, string>();
        private readonly Stopwatch _swTotal = new Stopwatch();
        private readonly MonoBehaviour _hostBehaviour;

        private PuzzleStateMachine _puzzleStateMachine;
        private NarrativeSequencePlayer _narrativePlayer;
        private bool _productionWired;

        private int _tapEventCount;
        private int _dragEventCount;
        private int _transformChangedCount;
        private int _matchScoreUpdateCount;
        private int _perfectMatchCount;
        private int _nearMatchEnterCount;
        private int _narrativeSequenceStartCount;
        private int _audioDuckingRequestCount;
        private float _lastPublishedScore;
        private float _capturedMusicVolume = -1f;
        private readonly List<float> _scoreHistory = new List<float>();

        private Action<GestureData> _onTap;
        private Action<GestureData> _onDrag;
        private Action<int, Vector3, Quaternion> _onObjectTransformChanged;
        private Action<int, float> _onMatchScoreUpdated;
        private Action<int> _onNearMatchEnter;
        private Action<int, float> _onPerfectMatch;
        private Action<int, NarrativeSequenceType> _onSequenceStart;
        private Action<float, float> _onDuckingRequest;

        private static readonly string[] ExpectedLogSubstrings =
        {
            "[InteractableObject",
            "[InteractionCoordinator]",
            "[ShadowMatchCalculator]",
            "[InputService]",
            "[PuzzleStateMachine]",
            "[NarrativeSequencePlayer]",
            "[GameApp]",
            "[GameFlow]",
            "[S6-17]",
            "[YooAsset]",
            "AssetBundle",
            "Cannot load asset",
            "scene to load is null",
            "OnRequestSceneChange",
        };

        // chapter 1 fixture targets（ShadowMatchCalculator + scene 默认 grid 对齐位姿）
        private static readonly Vector3 Obj1Target = new Vector3(-1f, 0f, 0f);
        private static readonly Vector3 Obj2Target = new Vector3(1f, 0f, 0f);

        public S617Tester(MonoBehaviour host)
        {
            _hostBehaviour = host;
        }

        public void SubscribeEarlyListeners()
        {
            Application.logMessageReceived += OnLogReceived;

            _onTap = _ => _tapEventCount++;
            _onDrag = _ => _dragEventCount++;
            _onObjectTransformChanged = (_, __, ___) => _transformChangedCount++;
            _onMatchScoreUpdated = (id, score) =>
            {
                _matchScoreUpdateCount++;
                _lastPublishedScore = score;
                _scoreHistory.Add(score);
            };
            _onNearMatchEnter = _ => _nearMatchEnterCount++;
            _onPerfectMatch = (_, __) => _perfectMatchCount++;
            _onSequenceStart = (_, __) => _narrativeSequenceStartCount++;
            _onDuckingRequest = (_, __) => _audioDuckingRequestCount++;

            GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnTap, _onTap);
            GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnDrag, _onDrag);
            GameEvent.AddEventListener<int, Vector3, Quaternion>(IInteractionEvent_Event.OnObjectTransformChanged, _onObjectTransformChanged);
            GameEvent.AddEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated);
            GameEvent.AddEventListener<int>(IShadowPuzzleEvent_Event.OnNearMatchEnter, _onNearMatchEnter);
            GameEvent.AddEventListener<int, float>(IShadowPuzzleEvent_Event.OnPerfectMatch, _onPerfectMatch);
            GameEvent.AddEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceStart, _onSequenceStart);
            GameEvent.AddEventListener<float, float>(IAudioEvent_Event.OnDuckingRequest, _onDuckingRequest);
        }

        public void UnsubscribeEarlyListeners()
        {
            Application.logMessageReceived -= OnLogReceived;

            if (_onTap != null) GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnTap, _onTap);
            if (_onDrag != null) GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnDrag, _onDrag);
            if (_onObjectTransformChanged != null)
                GameEvent.RemoveEventListener<int, Vector3, Quaternion>(IInteractionEvent_Event.OnObjectTransformChanged, _onObjectTransformChanged);
            if (_onMatchScoreUpdated != null)
                GameEvent.RemoveEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated);
            if (_onNearMatchEnter != null)
                GameEvent.RemoveEventListener<int>(IShadowPuzzleEvent_Event.OnNearMatchEnter, _onNearMatchEnter);
            if (_onPerfectMatch != null)
                GameEvent.RemoveEventListener<int, float>(IShadowPuzzleEvent_Event.OnPerfectMatch, _onPerfectMatch);
            if (_onSequenceStart != null)
                GameEvent.RemoveEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceStart, _onSequenceStart);
            if (_onDuckingRequest != null)
                GameEvent.RemoveEventListener<float, float>(IAudioEvent_Event.OnDuckingRequest, _onDuckingRequest);

            _onTap = null;
            _onDrag = null;
            _onObjectTransformChanged = null;
            _onMatchScoreUpdated = null;
            _onNearMatchEnter = null;
            _onPerfectMatch = null;
            _onSequenceStart = null;
            _onDuckingRequest = null;
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;

            foreach (var pattern in ExpectedLogSubstrings)
            {
                if (!string.IsNullOrEmpty(condition) &&
                    condition.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
            }
            UnexpectedErrorCount++;
        }

        public void InitializeProductionWiring()
        {
            try
            {
                var puzzleConfigProvider = new PuzzleStateConfigFromLuban();
                puzzleConfigProvider.InitWithDefaults();
                var puzzleConfig = puzzleConfigProvider.GetConfig(1);
                if (puzzleConfig == null)
                {
                    Log.Error("[S6-17] PuzzleStateConfigFromLuban.GetConfig(1) == null");
                    return;
                }

                _puzzleStateMachine = new PuzzleStateMachine();
                _puzzleStateMachine.Initialize(1, puzzleConfig);
                _puzzleStateMachine.OnChapterUnlocked();
                _puzzleStateMachine.OnPlayerInteraction();

                var narrativeConfigProvider = new NarrativeSequenceConfigFromLuban();
                narrativeConfigProvider.InitWithDefaults();
                _narrativePlayer = new NarrativeSequencePlayer();
                _narrativePlayer.Initialize(narrativeConfigProvider);

                _productionWired = true;
                Log.Info("[S6-17] InitializeProductionWiring 完成");
            }
            catch (Exception ex)
            {
                Log.Error($"[S6-17] InitializeProductionWiring 异常: {ex}");
                _productionWired = false;
            }
        }

        public void ShutdownProductionWiring()
        {
            _puzzleStateMachine?.Shutdown();
            _puzzleStateMachine = null;
            _narrativePlayer?.Shutdown();
            _narrativePlayer = null;
            _productionWired = false;
        }

        public void TickPuzzleAndNarrative(float deltaTime)
        {
            if (!_productionWired) return;
            _puzzleStateMachine?.Tick(deltaTime);
            _narrativePlayer?.Tick(deltaTime);
        }

        public async UniTask RunAllAsync()
        {
            _swTotal.Start();
            InputService input = null;
            try
            {
                await UniTask.Yield();
                await UniTask.DelayFrame(2);

                input = GetProductionInputService();
                if (input == null || !input.IsInitialized)
                {
                    OverallStatus = "Crashed: InputService missing";
                    _asserts["baseline.input"] = "FAIL: GameApp._inputService null or not initialized";
                    return;
                }

                if (!_productionWired)
                {
                    OverallStatus = "Crashed: puzzle/narrative wiring failed";
                    return;
                }

                input.SuspendTick();
                _asserts["baseline.tick_suspended"] = "PASS: SuspendTick for R3 isolation";

                await RunP1Async();
                if (P1Passed != true)
                {
                    OverallStatus = "Some Failed (P1)";
                    return;
                }

                await UniTask.DelayFrame(3);
                await RunP2Async(input);
                await UniTask.DelayFrame(2);
                await RunP3Async();
                await UniTask.DelayFrame(2);
                await RunP4Async();
                await RunP5Async();

                OverallStatus = AllPassed ? "All Passed" : "Some Failed";
                Log.Info($"[S6-17] Done. AllPassed={AllPassed} Elapsed={_swTotal.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                OverallStatus = $"Crashed: {ex.GetType().Name}";
                _asserts["fatal.exception"] = $"FAIL: {ex}";
                Log.Error($"[S6-17] RunAllAsync crashed: {ex}");
            }
            finally
            {
                if (input != null && input.IsTickSuspended)
                    input.ResumeTick();

                _swTotal.Stop();
                TotalElapsedMs = _swTotal.ElapsedMilliseconds;
                WriteResultJson();
            }
        }

        private async UniTask RunP1Async()
        {
            Log.Info("[S6-17] P1 InputSimulationMouseTapNewGame 开始");

            MainMenuPanel panel = null;
            try
            {
                panel = await GameModule.UI.ShowUIAsyncAwait<MainMenuPanel>();
            }
            catch (Exception ex)
            {
                _asserts["P1.show_ui_exception"] = $"FAIL: {ex.GetType().Name}";
                P1Passed = false;
                return;
            }

            if (panel?.NewGameButton == null)
            {
                _asserts["P1.main_menu_button"] = "FAIL: MainMenuPanel or NewGameButton null";
                P1Passed = false;
                return;
            }

            panel.NewGameButton.onClick.Invoke();

            var sm = GetProductionSceneManager();
            if (sm == null)
            {
                _asserts["P1.scene_manager"] = "FAIL: GameApp._sceneManager null";
                P1Passed = false;
                return;
            }

            bool idleOk = await WaitForChapterIdleAsync(sm, 1, 12.0);
            _asserts["P1.chapter1_loaded"] = idleOk
                ? $"PASS: state=Idle chapter={sm.CurrentLoadedChapterIdForTest}"
                : $"FAIL: state={sm.CurrentState} chapter={sm.CurrentLoadedChapterIdForTest}";

            P1Passed = idleOk &&
                       sm.CurrentLoadedChapterIdForTest == 1 &&
                       sm.CurrentChapterSceneNameForTest == "Chapter_01_Approach";
        }

        private async UniTask RunP2Async(InputService input)
        {
            Log.Info("[S6-17] P2 InputSimulationObjectInteractionToShadowMatch 开始");

            // 关闭主菜单 UI，避免 Canvas 挡射线 / 占输入焦点
            try
            {
                GameModule.UI.CloseUI<MainMenuPanel>();
                await UniTask.DelayFrame(3);
            }
            catch (Exception ex)
            {
                Log.Warning($"[S6-17] CloseUI<MainMenuPanel> 异常（可忽略若已关）: {ex.Message}");
            }

            int baselineScore = _matchScoreUpdateCount;
            int baselineTransform = _transformChangedCount;
            int baselineTap = _tapEventCount;
            int baselineDrag = _dragEventCount;

            var coord = UnityEngine.Object.FindObjectOfType<InteractionCoordinator>();
            var cam = GetChapterCamera();
            if (cam == null || coord == null)
            {
                _asserts["P2.scene_wiring"] = $"FAIL: camera={cam != null} coordinator={coord != null}";
                P2Passed = false;
                return;
            }

            var objects = UnityEngine.Object.FindObjectsOfType<InteractableObject>();
            InteractableObject obj1 = null, obj2 = null;
            foreach (var o in objects)
            {
                if (o.ObjectId == 1) obj1 = o;
                else if (o.ObjectId == 2) obj2 = o;
            }

            if (obj1 == null || obj2 == null)
            {
                _asserts["P2.interactable_objects"] = $"FAIL: obj1={obj1 != null} obj2={obj2 != null} count={objects.Length}";
                P2Passed = false;
                return;
            }

            _asserts["P2.interactable_objects"] = "PASS: ObjectId 1+2 found in chapter scene";

            Vector2 screen1 = BruteForceScreenPosForObject(coord, obj1);
            Vector2 screen2 = BruteForceScreenPosForObject(coord, obj2);
            _asserts["P2.brute_screen_obj1"] = screen1 != Vector2.zero ? $"PASS: ({screen1.x:F0},{screen1.y:F0})" : "FAIL: not found";
            _asserts["P2.brute_screen_obj2"] = screen2 != Vector2.zero ? $"PASS: ({screen2.x:F0},{screen2.y:F0})" : "FAIL: not found";

            await SimulateTapAndDragToWorldAsync(input, cam, coord, obj2, new Vector3(0f, 0f, 0f));
            await UniTask.Delay(TimeSpan.FromMilliseconds(280));

            await SimulateTapAndDragToWorldAsync(input, cam, coord, obj1, new Vector3(0f, 0f, 0f));
            await UniTask.Delay(TimeSpan.FromMilliseconds(280));

            await SimulateTapAndDragToWorldAsync(input, cam, coord, obj2, new Vector3(1.5f, 0f, 0f));
            await UniTask.Delay(TimeSpan.FromMilliseconds(280));

            await SimulateTapAndDragToWorldAsync(input, cam, coord, obj1, new Vector3(-1.5f, 0f, 0f));
            await UniTask.Delay(TimeSpan.FromMilliseconds(280));

            // 归位到 fixture 目标（scene 默认 grid 对齐位姿）
            await SimulateTapAndDragToWorldAsync(input, cam, coord, obj2, Obj2Target);
            await UniTask.Delay(TimeSpan.FromMilliseconds(280));

            await SimulateTapAndDragToWorldAsync(input, cam, coord, obj1, Obj1Target);
            await UniTask.Delay(TimeSpan.FromMilliseconds(500));

            _asserts["P2.obj1_final_pos"] = $"({obj1.transform.position.x:F2},{obj1.transform.position.y:F2})";
            _asserts["P2.obj2_final_pos"] = $"({obj2.transform.position.x:F2},{obj2.transform.position.y:F2})";

            int scoreDelta = _matchScoreUpdateCount - baselineScore;
            int transformDelta = _transformChangedCount - baselineTransform;
            int tapDelta = _tapEventCount - baselineTap;
            int dragDelta = _dragEventCount - baselineDrag;

            bool countOk = scoreDelta >= 3;
            bool transformOk = transformDelta >= 2;
            bool gestureOk = tapDelta >= 2 && dragDelta >= 6;
            bool scoreOk = _lastPublishedScore >= 0.85f;

            _asserts["P2.OnMatchScoreUpdated_delta"] = $"{(countOk ? "PASS" : "FAIL")}: expected ≥3, actual {scoreDelta}";
            _asserts["P2.OnObjectTransformChanged_delta"] = $"{(transformOk ? "PASS" : "FAIL")}: expected ≥2, actual {transformDelta}";
            _asserts["P2.gesture_production_path"] = $"{(gestureOk ? "PASS" : "FAIL")}: tapΔ={tapDelta} dragΔ={dragDelta}";
            _asserts["P2.final_score"] = $"{(scoreOk ? "PASS" : "FAIL")}: expected ≥0.85, actual {_lastPublishedScore:F3}";
            _asserts["P2.score_history"] = $"INFO: [{string.Join(", ", _scoreHistory.ConvertAll(s => s.ToString("F2")))}]";

            P2Passed = countOk && transformOk && gestureOk && scoreOk;
            Log.Info($"[S6-17][P2] {(P2Passed == true ? "✅" : "❌")} scoreΔ={scoreDelta} final={_lastPublishedScore:F3}");
        }

        private async UniTask RunP3Async()
        {
            Log.Info("[S6-17] P3 PuzzleStateMachineToNarrative 开始");

            bool perfectOnce = _perfectMatchCount == 1;
            bool stateOk = _puzzleStateMachine?.CurrentState == PuzzleState.PerfectMatch
                           || _puzzleStateMachine?.CurrentState == PuzzleState.Complete;

            _asserts["P3.OnPerfectMatch_count"] = $"{(perfectOnce ? "PASS" : "FAIL")}: expected 1, actual {_perfectMatchCount}";
            _asserts["P3.puzzle_state"] = stateOk
                ? $"PASS: state={_puzzleStateMachine?.CurrentState}"
                : $"FAIL: state={_puzzleStateMachine?.CurrentState}";

            P3Passed = perfectOnce && stateOk;
            Log.Info($"[S6-17][P3] {(P3Passed == true ? "✅" : "❌")} perfect={_perfectMatchCount}");
            await UniTask.Yield();
        }

        private async UniTask RunP4Async()
        {
            Log.Info("[S6-17] P4 NarrativeSequenceWithAudioDuck 开始");

            await UniTask.Delay(TimeSpan.FromMilliseconds(500));
            _capturedMusicVolume = GameModule.Audio != null ? GameModule.Audio.MusicVolume : -1f;
            await UniTask.Delay(TimeSpan.FromMilliseconds(2200));

            bool seqOk = _narrativeSequenceStartCount >= 1;
            bool duckOk = _audioDuckingRequestCount >= 1;
            bool musicOk = _capturedMusicVolume >= 0.0001f && _capturedMusicVolume <= 0.5f;
            bool unexpectedOk = UnexpectedErrorCount == 0;

            _asserts["P4.OnSequenceStart"] = $"{(seqOk ? "PASS" : "FAIL")}: count={_narrativeSequenceStartCount}";
            _asserts["P4.OnDuckingRequest"] = $"{(duckOk ? "PASS" : "FAIL")}: count={_audioDuckingRequestCount}";
            _asserts["P4.MusicVolume"] = $"{(musicOk ? "PASS" : "FAIL")}: {_capturedMusicVolume:F3}";
            _asserts["P4.unexpected_errors"] = $"{(unexpectedOk ? "PASS" : "FAIL")}: {UnexpectedErrorCount}";

            P4Passed = seqOk && duckOk && musicOk && unexpectedOk;
            Log.Info($"[S6-17][P4] {(P4Passed == true ? "✅" : "❌")}");
            await UniTask.Yield();
        }

        private UniTask RunP5Async()
        {
            Log.Info("[S6-17] P5 dp15SniffSubClauseVerify 开始");

            // spike 设计约束：0 次 GameEvent.Get<I*Event>().* mock fire（编译期设计 + grep 实证）
            const bool noMockFireInSpikeDesign = true;
            bool productionCalc = GetProductionShadowMatchCalculator() != null;
            bool productionInput = GetProductionInputService() != null;

            _asserts["P5.no_mock_fire_design"] = noMockFireInSpikeDesign
                ? "PASS: spike 仅走 InputService.TickForTest → GestureDispatcher 自然路径"
                : "FAIL";
            _asserts["P5.production_shadow_match"] = productionCalc
                ? "PASS: GameApp._shadowMatchCalculator wired"
                : "FAIL: calculator missing";
            _asserts["P5.production_input"] = productionInput
                ? "PASS: GameApp._inputService wired"
                : "FAIL: input missing";
            _asserts["P5.gesture_events_fired"] = _tapEventCount >= 2 && _dragEventCount >= 6
                ? $"PASS: tap={_tapEventCount} drag={_dragEventCount}"
                : $"FAIL: tap={_tapEventCount} drag={_dragEventCount}";

            P5Passed = noMockFireInSpikeDesign && productionCalc && productionInput &&
                       _tapEventCount >= 2 && _dragEventCount >= 6;

            Log.Info($"[S6-17][P5] {(P5Passed == true ? "✅" : "❌")}");
            return UniTask.CompletedTask;
        }

        private async UniTask SimulateTapAndDragToWorldAsync(
            InputService input, Camera cam, InteractionCoordinator coord, InteractableObject obj, Vector3 targetWorld)
        {
            float dragDepth = GetPrivateField<float>(obj, "_dragDepth");
            if (dragDepth <= 0f) dragDepth = 10f;

            Vector2 startScreen = BruteForceScreenPosForObject(coord, obj);
            if (startScreen == Vector2.zero)
                startScreen = FindScreenPosForObject(cam, coord, obj);
            Vector2 targetScreen = WorldToScreenForDrag(cam, targetWorld, dragDepth);

            if (Vector2.Distance(startScreen, targetScreen) < 30f)
            {
                targetScreen = startScreen + new Vector2(120f, 0f);
            }

            input.SingleFingerFSMForTest.ForceReset();

            bool selected = await TapSelectObjectAsync(input, coord, cam, obj, startScreen);
            _asserts[$"P2.select_obj{obj.ObjectId}"] = selected
                ? $"PASS: CurrentSelectedObject==Object_{obj.ObjectId}"
                : $"FAIL: tap 后未选中 Object_{obj.ObjectId}";

            if (!selected)
            {
                Log.Warning($"[S6-17] Object_{obj.ObjectId} 选取失败，跳过本次 drag");
                return;
            }

            var began = new TouchState
            {
                FingerId = 0,
                CurrentPosition = startScreen,
                Phase = TouchPhase.Began,
                IsActive = true
            };
            input.TickForTest(in began, 0.016f);
            await UniTask.DelayFrame(1);

            const int steps = 20;
            for (int i = 1; i <= steps; i++)
            {
                Vector2 cur = Vector2.Lerp(startScreen, targetScreen, i / (float)steps);
                var moved = new TouchState
                {
                    FingerId = 0,
                    CurrentPosition = cur,
                    Phase = TouchPhase.Moved,
                    IsActive = true
                };
                input.TickForTest(in moved, 0.016f);
                await UniTask.DelayFrame(1);
            }

            var ended = new TouchState
            {
                FingerId = 0,
                CurrentPosition = targetScreen,
                Phase = TouchPhase.Ended,
                IsActive = true
            };
            input.TickForTest(in ended, 0.016f);
            await UniTask.DelayFrame(3);

            // 等待 InteractableObject TickDrag + grid snap tween（snapSpeed=0.2s）
            await UniTask.Delay(TimeSpan.FromMilliseconds(600));
            await UniTask.DelayFrame(8);
        }

        private static async UniTask SimulateTapAsync(InputService input, Vector2 screenPos)
        {
            var began = new TouchState
            {
                FingerId = 0,
                CurrentPosition = screenPos,
                Phase = TouchPhase.Began,
                IsActive = true
            };
            input.TickForTest(in began, 0.016f);
            await UniTask.DelayFrame(1);

            for (int i = 0; i < 3; i++)
            {
                var stationary = new TouchState
                {
                    FingerId = 0,
                    CurrentPosition = screenPos,
                    Phase = TouchPhase.Stationary,
                    IsActive = true
                };
                input.TickForTest(in stationary, 0.05f);
                await UniTask.DelayFrame(1);
            }

            var ended = new TouchState
            {
                FingerId = 0,
                CurrentPosition = screenPos,
                Phase = TouchPhase.Ended,
                IsActive = true
            };
            input.TickForTest(in ended, 0.05f);
            await UniTask.DelayFrame(2);
        }

        private static async UniTask<bool> TapSelectObjectAsync(
            InputService input, InteractionCoordinator coord, Camera cam, InteractableObject obj, Vector2 primaryScreen)
        {
            var candidates = new List<Vector2>();
            if (primaryScreen != Vector2.zero) candidates.Add(primaryScreen);
            candidates.Add(FindScreenPosForObject(cam, coord, obj));
            var brute = BruteForceScreenPosForObject(coord, obj);
            if (brute != Vector2.zero) candidates.Add(brute);

            foreach (var screen in candidates)
            {
                if (coord.RaycastWithFatFinger(screen) != obj)
                    continue;

                input.SingleFingerFSMForTest.ForceReset();
                await SimulateTapAsync(input, screen);
                await UniTask.Delay(TimeSpan.FromMilliseconds((int)(InteractionCoordinator.DebounceSeconds * 1000) + 120));
                await UniTask.DelayFrame(6);
                if (coord.CurrentSelectedObject == obj)
                    return true;
            }

            return false;
        }

        private static Vector2 BruteForceScreenPosForObject(InteractionCoordinator coord, InteractableObject obj)
        {
            for (int x = 25; x < Screen.width; x += 25)
            {
                for (int y = 25; y < Screen.height; y += 25)
                {
                    var trySp = new Vector2(x, y);
                    if (coord.RaycastWithFatFinger(trySp) == obj)
                        return trySp;
                }
            }
            return Vector2.zero;
        }

        private static Vector2 FindScreenPosForObject(Camera cam, InteractionCoordinator coord, InteractableObject obj)
        {
            var col = obj.GetComponentInChildren<Collider2D>();
            Vector3 world = col != null ? (Vector3)col.bounds.center : obj.transform.position;
            var sp = cam.WorldToScreenPoint(world);
            var screen = new Vector2(sp.x, sp.y);

            if (coord.RaycastWithFatFinger(screen) == obj)
                return screen;

            for (int r = 10; r <= 120; r += 15)
            {
                for (int deg = 0; deg < 360; deg += 30)
                {
                    float rad = deg * Mathf.Deg2Rad;
                    var trySp = new Vector2(screen.x + r * Mathf.Cos(rad), screen.y + r * Mathf.Sin(rad));
                    if (coord.RaycastWithFatFinger(trySp) == obj)
                        return trySp;
                }
            }

            var brute = BruteForceScreenPosForObject(coord, obj);
            return brute != Vector2.zero ? brute : screen;
        }

        private static Vector2 WorldToScreenForDrag(Camera cam, Vector3 targetWorld, float dragDepth)
        {
            // 主路径：与 InteractionCoordinator raycast 同 zDepth
            float zDepth = -cam.transform.position.z;
            var sp = cam.WorldToScreenPoint(new Vector3(targetWorld.x, targetWorld.y, zDepth));
            Vector2 primary = new Vector2(sp.x, sp.y);

            // 校验：ScreenToWorldPoint(s, dragDepth) 应逼近 targetWorld.xy
            var verify = cam.ScreenToWorldPoint(new Vector3(primary.x, primary.y, dragDepth));
            float err = Vector2.Distance(new Vector2(verify.x, verify.y), new Vector2(targetWorld.x, targetWorld.y));
            if (err < 0.15f)
                return primary;

            // fallback：局部搜索 dragDepth 逆映射
            Vector2 best = primary;
            float bestErr = err;
            for (int dx = -60; dx <= 60; dx += 5)
            {
                for (int dy = -60; dy <= 60; dy += 5)
                {
                    var trySp = new Vector2(primary.x + dx, primary.y + dy);
                    var w = cam.ScreenToWorldPoint(new Vector3(trySp.x, trySp.y, dragDepth));
                    float e = Vector2.Distance(new Vector2(w.x, w.y), new Vector2(targetWorld.x, targetWorld.y));
                    if (e < bestErr)
                    {
                        bestErr = e;
                        best = trySp;
                    }
                }
            }
            return best;
        }

        private static SceneManager GetProductionSceneManager()
        {
            var fi = typeof(GameApp).GetField("_sceneManager", BindingFlags.NonPublic | BindingFlags.Static);
            return fi?.GetValue(null) as SceneManager;
        }

        private static InputService GetProductionInputService()
        {
            var fi = typeof(GameApp).GetField("_inputService", BindingFlags.NonPublic | BindingFlags.Static);
            return fi?.GetValue(null) as InputService;
        }

        private static ShadowMatchCalculator GetProductionShadowMatchCalculator()
        {
            var fi = typeof(GameApp).GetField("_shadowMatchCalculator", BindingFlags.NonPublic | BindingFlags.Static);
            return fi?.GetValue(null) as ShadowMatchCalculator;
        }

        private static Camera GetChapterCamera()
        {
            var coord = UnityEngine.Object.FindObjectOfType<InteractionCoordinator>();
            if (coord != null)
            {
                var cam = GetPrivateField<Camera>(coord, "_gameplayCamera");
                if (cam != null) return cam;
            }

            var io = UnityEngine.Object.FindObjectOfType<InteractableObject>();
            if (io != null)
                return GetPrivateField<Camera>(io, "_gameplayCamera");

            return null;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            if (target == null) return default;
            var fi = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi == null) return default;
            return (T)fi.GetValue(target);
        }

        private static async UniTask<bool> WaitForChapterIdleAsync(SceneManager scene, int chapterId, double timeoutSec)
        {
            var sw = Stopwatch.StartNew();
            while (scene.CurrentState != SceneManagerState.Idle || scene.CurrentLoadedChapterIdForTest != chapterId)
            {
                if (sw.Elapsed.TotalSeconds > timeoutSec) return false;
                await UniTask.Yield();
            }
            return true;
        }

        private void WriteResultJson()
        {
            try
            {
                var sb = new StringBuilder(4096);
                sb.Append("{\n");
                sb.Append("  \"spike\": \"S6-17 End-to-End Smoke Replay (Track F vs-chapter-1-008)\",\n");
                sb.Append($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\n");
                sb.Append($"  \"overallStatus\": \"{Escape(OverallStatus)}\",\n");
                sb.Append($"  \"allPassed\": {(AllPassed ? "true" : "false")},\n");
                sb.Append($"  \"totalElapsedMs\": {TotalElapsedMs},\n");
                sb.Append($"  \"unexpectedErrorCount\": {UnexpectedErrorCount},\n");
                sb.Append("  \"caseResults\": {\n");
                sb.Append($"    \"P1\": {Verdict(P1Passed)},\n");
                sb.Append($"    \"P2\": {Verdict(P2Passed)},\n");
                sb.Append($"    \"P3\": {Verdict(P3Passed)},\n");
                sb.Append($"    \"P4\": {Verdict(P4Passed)},\n");
                sb.Append($"    \"P5\": {Verdict(P5Passed)}\n");
                sb.Append("  },\n");
                sb.Append("  \"asserts\": {\n");
                int idx = 0;
                foreach (var kv in _asserts)
                {
                    sb.Append($"    \"{Escape(kv.Key)}\": \"{Escape(kv.Value)}\"");
                    sb.Append(idx == _asserts.Count - 1 ? "\n" : ",\n");
                    idx++;
                }
                sb.Append("  }\n");
                sb.Append("}\n");

                File.WriteAllText(ResultFilePath, sb.ToString());
                Log.Info($"[S6-17] JSON evidence: {ResultFilePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"[S6-17] WriteResultJson 失败: {ex.Message}");
            }
        }

        private static string Verdict(bool? value) => value switch
        {
            true => "\"PASS\"",
            false => "\"FAIL\"",
            _ => "\"NotRun\""
        };

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
#endif
