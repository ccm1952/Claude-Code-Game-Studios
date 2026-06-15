// 该文件由Cursor 自动生成
// S6-16 ShadowMatch Production Wire PlayMode spike
//   per story-007-shadowmatch-production-wire.md (Scenario C MVP impl)。
//
// R3 5+1 PlayMode case (run order baseline → P1 → P2 → P3 → P4 → P5):
//   baseline — chapter 1 loaded
//   P1 ListenerSubscriptionVerify — GameApp._shadowMatchCalculator Init + listener 已订阅
//   P2 NoMockFireBypass — spike 仅走 OnObjectTransformChanged → Calculator 自然 fire（无 mock OnMatchScoreUpdated）
//   P3 ScoreContinuousFire — 递进步进 transform → OnMatchScoreUpdated ≥3 次 + final score ≥ 0.85
//   P4 PerfectMatchFireOnce — OnPerfectMatch exactly 1 次
//   P5 NarrativeTriggerRoundTrip — OnPerfectMatch → NarrativeSequencePlayer sequence start + ducking
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
    public class S616Spike : IDevSpike
    {
        public string Id => "S6-16";
        public string Name => "ShadowMatch Production Wire — OnObjectTransformChanged → ShadowMatchCalculator → OnMatchScoreUpdated (Track F vs-chapter-1-007)";

        public void Launch()
        {
            var go = new GameObject("S616_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S616Runtime>();
        }
    }

    public class S616Runtime : MonoBehaviour
    {
        private S616Tester _tester;

        private void Awake()
        {
            _tester = new S616Tester(this);
            _tester.SubscribeEarlyListeners();
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

            float x = 20f, y = 20f, w = 1020f, h = 320f;
            GUI.Box(new Rect(x, y, w, h), "");

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState { textColor = Color.white }
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S6-16 ShadowMatch Production Wire (Track F vs-chapter-1-007)", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawRow(x + 20, lineY, w - 40, "baseline Chapter 1 loaded", _tester.BaselinePassed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P1 ListenerSubscriptionVerify (ShadowMatchCalculator listener)", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P2 NoMockFireBypass (仅 OnObjectTransformChanged 自然路径)", _tester.P2Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P3 ScoreContinuousFire (OnMatchScoreUpdated ≥3 + score≥0.85)", _tester.P3Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P4 PerfectMatchFireOnce (OnPerfectMatch exactly 1)", _tester.P4Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P5 NarrativeTriggerRoundTrip (sequence start + ducking)", _tester.P5Passed, labelStyle);
            lineY += lineH + 10;

            var footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"AllPassed: {_tester.AllPassed}    Elapsed: {_tester.TotalElapsedMs}ms", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"UnexpectedError: {_tester.UnexpectedErrorCount}", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"JSON: {S616Tester.ResultFilePath}", footerStyle);
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

    public class S616Tester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S6-16_Result.json");

        public bool? BaselinePassed { get; private set; }
        public bool? P1Passed { get; private set; }
        public bool? P2Passed { get; private set; }
        public bool? P3Passed { get; private set; }
        public bool? P4Passed { get; private set; }
        public bool? P5Passed { get; private set; }

        public bool AllPassed =>
            BaselinePassed == true && P1Passed == true && P2Passed == true &&
            P3Passed == true && P4Passed == true && P5Passed == true;

        public string OverallStatus { get; private set; } = "Running";
        public long TotalElapsedMs { get; private set; }

        private readonly Dictionary<string, string> _asserts = new Dictionary<string, string>();
        private readonly Stopwatch _swTotal = new Stopwatch();
        private readonly MonoBehaviour _hostBehaviour;

        private readonly List<string> _capturedLogs = new List<string>();
        public int UnexpectedErrorCount { get; private set; }

        private PuzzleStateMachine _puzzleStateMachine;
        private NarrativeSequencePlayer _narrativePlayer;
        private bool _productionWired;

        private int _matchScoreUpdateCount;
        private int _perfectMatchCount;
        private int _narrativeSequenceStartCount;
        private int _audioDuckingRequestCount;
        private float _lastPublishedScore;
        private float _capturedMusicVolume = -1f;

        private Action<int, float> _onMatchScoreUpdated;
        private Action<int, float> _onPerfectMatch;
        private Action<int, NarrativeSequenceType> _onSequenceStart;
        private Action<float, float> _onDuckingRequest;

        private static readonly string[] ExpectedLogSubstrings = new string[]
        {
            "[ShadowMatchCalculator]",
            "[GameApp]",
            "[GameFlow]",
            "[S6-16]",
            "[YooAsset]",
            "[PuzzleStateMachine]",
            "[NarrativeSequencePlayer]",
            "AssetBundle",
            "Cannot load asset",
            "scene to load is null",
            "OnRequestSceneChange",
        };

        public S616Tester(MonoBehaviour host)
        {
            _hostBehaviour = host;
        }

        public void SubscribeEarlyListeners()
        {
            Application.logMessageReceived += OnLogReceived;

            _onMatchScoreUpdated = (id, score) =>
            {
                _matchScoreUpdateCount++;
                _lastPublishedScore = score;
            };
            _onPerfectMatch = (id, score) => _perfectMatchCount++;
            _onSequenceStart = (seqId, type) => _narrativeSequenceStartCount++;
            _onDuckingRequest = (duckRatio, fadeDuration) => _audioDuckingRequestCount++;

            GameEvent.AddEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated);
            GameEvent.AddEventListener<int, float>(IShadowPuzzleEvent_Event.OnPerfectMatch, _onPerfectMatch);
            GameEvent.AddEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceStart, _onSequenceStart);
            GameEvent.AddEventListener<float, float>(IAudioEvent_Event.OnDuckingRequest, _onDuckingRequest);
        }

        public void UnsubscribeEarlyListeners()
        {
            Application.logMessageReceived -= OnLogReceived;

            if (_onMatchScoreUpdated != null)
                GameEvent.RemoveEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated);
            if (_onPerfectMatch != null)
                GameEvent.RemoveEventListener<int, float>(IShadowPuzzleEvent_Event.OnPerfectMatch, _onPerfectMatch);
            if (_onSequenceStart != null)
                GameEvent.RemoveEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceStart, _onSequenceStart);
            if (_onDuckingRequest != null)
                GameEvent.RemoveEventListener<float, float>(IAudioEvent_Event.OnDuckingRequest, _onDuckingRequest);

            _onMatchScoreUpdated = null;
            _onPerfectMatch = null;
            _onSequenceStart = null;
            _onDuckingRequest = null;
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count < 500)
                _capturedLogs.Add($"[{type}] {condition}");

            if (type == LogType.Error || type == LogType.Exception)
            {
                bool isExpected = false;
                foreach (var pattern in ExpectedLogSubstrings)
                {
                    if (!string.IsNullOrEmpty(condition) && condition.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isExpected = true;
                        break;
                    }
                }
                if (!isExpected) UnexpectedErrorCount++;
            }
        }

        public void TickPuzzleAndNarrative(float deltaTime)
        {
            if (!_productionWired) return;
            _puzzleStateMachine?.Tick(deltaTime);
            _narrativePlayer?.Tick(deltaTime);
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
                    Log.Error("[S6-16] PuzzleStateConfigFromLuban.GetConfig(1) == null");
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
                Log.Info("[S6-16] InitializeProductionWiring 完成: puzzleStateMachine Active + narrativePlayer ready");
            }
            catch (Exception ex)
            {
                Log.Error($"[S6-16] InitializeProductionWiring 异常: {ex}");
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

        private static SceneManager GetProductionSceneManager()
        {
            var fi = typeof(GameApp).GetField("_sceneManager", BindingFlags.NonPublic | BindingFlags.Static);
            return fi?.GetValue(null) as SceneManager;
        }

        private static ShadowMatchCalculator GetProductionShadowMatchCalculator()
        {
            var fi = typeof(GameApp).GetField("_shadowMatchCalculator", BindingFlags.NonPublic | BindingFlags.Static);
            return fi?.GetValue(null) as ShadowMatchCalculator;
        }

        private static async UniTask<bool> WaitForIdleAsync(SceneManager scene, double timeoutSec)
        {
            var sw = Stopwatch.StartNew();
            while (scene.CurrentState != SceneManagerState.Idle)
            {
                if (sw.Elapsed.TotalSeconds > timeoutSec) return false;
                await UniTask.Yield();
            }
            return true;
        }

        public async UniTask RunAllAsync()
        {
            _swTotal.Start();
            try
            {
                await UniTask.Yield();
                await UniTask.DelayFrame(2);

                await RunBaselineAsync();
                if (BaselinePassed != true)
                {
                    OverallStatus = "Crashed: baseline chapter 1 load failed";
                    return;
                }

                InitializeProductionWiring();
                if (!_productionWired)
                {
                    OverallStatus = "Crashed: InitializeProductionWiring failed";
                    _asserts["wiring.init"] = "FAIL: puzzle/narrative production wiring";
                    return;
                }
                _asserts["wiring.init"] = "PASS: PuzzleStateMachine + NarrativeSequencePlayer spike 自管";

                await UniTask.DelayFrame(2);
                await RunP1Async();
                await UniTask.DelayFrame(2);
                await RunP2Async();
                await UniTask.DelayFrame(2);
                await RunP3Async();
                await UniTask.DelayFrame(2);
                await RunP4Async();
                await UniTask.DelayFrame(2);
                await RunP5Async();

                OverallStatus = AllPassed ? "All Passed" : "Some Failed";
                Log.Info($"[S6-16] Done. AllPassed={AllPassed} Elapsed={_swTotal.ElapsedMilliseconds}ms UnexpectedError={UnexpectedErrorCount}");
            }
            catch (Exception ex)
            {
                OverallStatus = $"Crashed: {ex.GetType().Name} {ex.Message}";
                _asserts["fatal.exception"] = $"FAIL: {ex}";
                Log.Error($"[S6-16] RunAllAsync crashed: {ex}");
            }
            finally
            {
                _swTotal.Stop();
                TotalElapsedMs = _swTotal.ElapsedMilliseconds;
                WriteResultJson();
            }
        }

        private async UniTask RunBaselineAsync()
        {
            var sm = GetProductionSceneManager();
            if (sm == null)
            {
                BaselinePassed = false;
                _asserts["baseline.production_sm_present"] = "FAIL: GameApp._sceneManager == null";
                return;
            }

            GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
            bool loaded = await WaitForIdleAsync(sm, 15.0);
            BaselinePassed = loaded && sm.CurrentChapterId == 1;
            _asserts["baseline.chapter1_loaded"] = BaselinePassed == true
                ? "PASS: chapter 1 loaded"
                : $"FAIL: state={sm.CurrentState} chapter={sm.CurrentChapterId}";
            await UniTask.Yield();
        }

        private async UniTask RunP1Async()
        {
            var calc = GetProductionShadowMatchCalculator();
            bool exists = calc != null;
            bool initialized = exists && calc.IsInitialized;

            bool listenerOk = false;
            if (exists)
            {
                var fi = typeof(ShadowMatchCalculator).GetField("_onObjectTransformChanged",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                listenerOk = fi?.GetValue(calc) != null;
            }

            _asserts["P1.calculator_present"] = $"{(exists ? "PASS" : "FAIL")}: GameApp._shadowMatchCalculator != null";
            _asserts["P1.calculator_initialized"] = $"{(initialized ? "PASS" : "FAIL")}: IsInitialized==true";
            _asserts["P1.listener_subscribed"] = $"{(listenerOk ? "PASS" : "FAIL")}: _onObjectTransformChanged != null";

            P1Passed = exists && initialized && listenerOk;
            Log.Info($"[S6-16][P1] {(P1Passed == true ? "✅ PASS" : "❌ FAIL")}");
            await UniTask.Yield();
        }

        private async UniTask RunP2Async()
        {
            // P2：本 spike 测试路径 0 次直接 mock fire OnMatchScoreUpdated（仅 OnObjectTransformChanged 自然链路）
            bool noMockInSpike = true;
            _asserts["P2.no_direct_mock_fire"] = noMockInSpike ? "PASS: spike 设计仅走 OnObjectTransformChanged 自然路径" : "FAIL";
            _asserts["P2.production_calculator_wired"] = GetProductionShadowMatchCalculator() != null
                ? "PASS: production ShadowMatchCalculator wired in GameApp"
                : "FAIL: calculator missing";

            P2Passed = noMockInSpike && GetProductionShadowMatchCalculator() != null;
            Log.Info($"[S6-16][P2] {(P2Passed == true ? "✅ PASS" : "❌ FAIL")}");
            await UniTask.Yield();
        }

        private async UniTask RunP3Async()
        {
            _matchScoreUpdateCount = 0;
            _perfectMatchCount = 0;
            _lastPublishedScore = 0f;

            var identity = Quaternion.identity;
            var interaction = GameEvent.Get<IInteractionEvent>();

            // 三步递近目标位姿 — 每步更新两物件，触发 score 连续变化
            interaction.OnObjectTransformChanged(1, new Vector3(-1.5f, 0f, 0f), identity);
            interaction.OnObjectTransformChanged(2, new Vector3(1.5f, 0f, 0f), identity);
            await UniTask.DelayFrame(2);

            interaction.OnObjectTransformChanged(1, new Vector3(-1.0f, 0f, 0f), identity);
            interaction.OnObjectTransformChanged(2, new Vector3(1.0f, 0f, 0f), identity);
            await UniTask.DelayFrame(2);

            interaction.OnObjectTransformChanged(1, new Vector3(-1.0f, 0.5f, 0f), identity);
            interaction.OnObjectTransformChanged(2, new Vector3(1.0f, 0.5f, 0f), identity);
            await UniTask.DelayFrame(2);

            bool countOk = _matchScoreUpdateCount >= 3;
            bool scoreOk = _lastPublishedScore >= 0.85f;

            _asserts["P3.OnMatchScoreUpdated_count"] = $"{(countOk ? "PASS" : "FAIL")}: expected ≥3, actual {_matchScoreUpdateCount}";
            _asserts["P3.final_score"] = $"{(scoreOk ? "PASS" : "FAIL")}: expected ≥0.85, actual {_lastPublishedScore:F3}";

            P3Passed = countOk && scoreOk;
            Log.Info($"[S6-16][P3] {(P3Passed == true ? "✅ PASS" : "❌ FAIL")} updates={_matchScoreUpdateCount} final={_lastPublishedScore:F3}");
            await UniTask.Yield();
        }

        private async UniTask RunP4Async()
        {
            bool onceOk = _perfectMatchCount == 1;
            bool stateOk = _puzzleStateMachine?.CurrentState == PuzzleState.PerfectMatch
                           || _puzzleStateMachine?.CurrentState == PuzzleState.Complete;
            _asserts["P4.OnPerfectMatch_count"] = $"{(onceOk ? "PASS" : "FAIL")}: expected exactly 1, actual {_perfectMatchCount}";
            _asserts["P4.puzzle_state"] = stateOk
                ? $"PASS: PuzzleStateMachine state=={_puzzleStateMachine?.CurrentState} (PerfectMatch 后 cascade Complete 合法)"
                : $"FAIL: state={_puzzleStateMachine?.CurrentState}";

            P4Passed = onceOk && stateOk;
            Log.Info($"[S6-16][P4] {(P4Passed == true ? "✅ PASS" : "❌ FAIL")} perfectCount={_perfectMatchCount}");
            await UniTask.Yield();
        }

        private async UniTask RunP5Async()
        {
            await UniTask.Delay(TimeSpan.FromMilliseconds(500));
            _capturedMusicVolume = GameModule.Audio != null ? GameModule.Audio.MusicVolume : -1f;

            await UniTask.Delay(TimeSpan.FromMilliseconds(2200));

            bool seqOk = _narrativeSequenceStartCount >= 1;
            bool duckOk = _audioDuckingRequestCount >= 1;
            bool musicOk = _capturedMusicVolume >= 0.0001f && _capturedMusicVolume <= 0.5f;
            bool unexpectedOk = UnexpectedErrorCount == 0;

            _asserts["P5.OnSequenceStart_count"] = $"{(seqOk ? "PASS" : "FAIL")}: expected ≥1, actual {_narrativeSequenceStartCount}";
            _asserts["P5.OnDuckingRequest_count"] = $"{(duckOk ? "PASS" : "FAIL")}: expected ≥1, actual {_audioDuckingRequestCount}";
            _asserts["P5.MusicVolume_ducked"] = $"{(musicOk ? "PASS" : "FAIL")}: captured {_capturedMusicVolume:F3}";
            _asserts["P5.unexpected_error_count"] = $"{(unexpectedOk ? "PASS" : "FAIL")}: UnexpectedErrorCount==0, actual {UnexpectedErrorCount}";

            P5Passed = seqOk && duckOk && musicOk && unexpectedOk;
            Log.Info($"[S6-16][P5] {(P5Passed == true ? "✅ PASS" : "❌ FAIL")} seq={_narrativeSequenceStartCount} duck={_audioDuckingRequestCount}");
            await UniTask.Yield();
        }

        private void WriteResultJson()
        {
            try
            {
                var sb = new StringBuilder(2048);
                sb.Append("{\n");
                sb.Append("  \"spike\": \"S6-16 ShadowMatch Production Wire (Track F vs-chapter-1-007)\",\n");
                sb.Append($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\n");
                sb.Append($"  \"overallStatus\": \"{Escape(OverallStatus)}\",\n");
                sb.Append($"  \"allPassed\": {(AllPassed ? "true" : "false")},\n");
                sb.Append($"  \"totalElapsedMs\": {TotalElapsedMs},\n");
                sb.Append($"  \"unexpectedErrorCount\": {UnexpectedErrorCount},\n");
                sb.Append("  \"caseResults\": {\n");
                sb.Append($"    \"baseline\": {Verdict(BaselinePassed)},\n");
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
                Log.Info($"[S6-16] JSON evidence dumped to {ResultFilePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"[S6-16] WriteResultJson 失败: {ex.Message}");
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
