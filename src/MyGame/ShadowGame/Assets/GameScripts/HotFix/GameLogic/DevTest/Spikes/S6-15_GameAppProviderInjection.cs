// 该文件由Cursor 自动生成
// S6-15 GameApp Provider Injection PlayMode spike
//   per story-006-gameapp-provider-injection.md (Phase 0 ✅ + Phase 1 ✅ READY)。
//
// 关联文档:
//   * production/epics/vs-chapter-1/story-006-gameapp-provider-injection.md  (10 AC + 5 R3 case)
//   * Assets/GameScripts/HotFix/GameLogic/GameApp.cs                         (RegisterPuzzleConfigProvider + RegisterInputConfigProvider)
//   * Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractableObject.cs
//   * Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractionCoordinator.cs
//
// R3 5 PlayMode case (run order baseline → P1 → P2 → P3 → P4 → P5):
//   baseline — OnRequestSceneChange(1) + WaitForIdleAsync → state=Idle, currentChapterId=1
//   P1 StaticPuzzleConfigProviderRegistered — reflection _puzzleConfigProvider != null; invoke(1) 非 null
//   P2 StaticInputConfigProviderRegistered — reflection _inputConfigProvider != null; invoke() 非 null IInputConfig
//   P3 InteractableObjectPuzzleConfigResolved — 每实例 _puzzleConfig != null; Id==1; bounds MinX=-10 MaxX=10
//   P4 CoordinatorInputConfigResolved — IsLocked==false; _inputConfig != null; FatFingerMarginMm==8 (InitWithDefaults)
//   P5 NoFailLoudProviderErrors — 0 Log.Error 含 PuzzleConfigProvider 未注册 / InputConfigProvider 未注册; UnexpectedErrorCount==0
//
// 设计约束 (沿 S6-13/S6-14 precedent):
//   * 1 file + 3 inner class (S615Spike : IDevSpike + S615Runtime : MonoBehaviour + S615Tester 纯逻辑)
//   * chapter 1 baseline: fire OnRequestSceneChange(1) + WaitForIdleAsync (S6-04 precedent)
//   * Reflection 读 static provider + instance _puzzleConfig / _inputConfig
//   * Application.logMessageReceived UnexpectedErrorCount + ExpectedLogSubstrings allowlist
//   * JSON evidence dump WriteResultJson per Application.persistentDataPath/S6-15_Result.json
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
    public class S615Spike : IDevSpike
    {
        public string Id => "S6-15";
        public string Name => "GameApp Provider Injection — RegisterPuzzleConfigProvider + RegisterInputConfigProvider (Track F vs-chapter-1-006)";

        public void Launch()
        {
            var go = new GameObject("S615_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S615Runtime>();
        }
    }

    public class S615Runtime : MonoBehaviour
    {
        private S615Tester _tester;

        private void Awake()
        {
            _tester = new S615Tester(this);
            _tester.SubscribeEarlyListeners();
        }

        private void Start()
        {
            _tester.RunAllAsync().Forget();
        }

        private void OnGUI()
        {
            if (_tester == null) return;

            float x = 20f, y = 20f, w = 980f, h = 300f;
            GUI.Box(new Rect(x, y, w, h), "");

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState { textColor = Color.white }
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S6-15 GameApp Provider Injection (Track F vs-chapter-1-006)", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawRow(x + 20, lineY, w - 40, "baseline Chapter 1 loaded (Idle, currentChapterId=1)", _tester.BaselinePassed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P1 StaticPuzzleConfigProviderRegistered (provider(1) 非 null)", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P2 StaticInputConfigProviderRegistered (provider() 非 null)", _tester.P2Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P3 InteractableObjectPuzzleConfigResolved (_puzzleConfig Id==1 bounds -10..10)", _tester.P3Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P4 CoordinatorInputConfigResolved (IsLocked==false FatFingerMarginMm==8)", _tester.P4Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P5 NoFailLoudProviderErrors (0 provider 未注册 Log.Error)", _tester.P5Passed, labelStyle);
            lineY += lineH + 10;

            var footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"AllPassed: {_tester.AllPassed}    Elapsed: {_tester.TotalElapsedMs}ms", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"UnexpectedError: {_tester.UnexpectedErrorCount}    FailLoudProviderErrors: {_tester.FailLoudProviderErrorCount}", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"JSON: {S615Tester.ResultFilePath}", footerStyle);
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
            _tester?.UnsubscribeEarlyListeners();
        }
    }

    /// <summary>S6-15 spike 测试逻辑 — 5 R3 case (baseline → P1 → P2 → P3 → P4 → P5) 串行执行。</summary>
    public class S615Tester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S6-15_Result.json");

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
        public int FailLoudProviderErrorCount { get; private set; }

        private static readonly string[] ExpectedLogSubstrings = new string[]
        {
            "[InputService]",
            "[GameApp]",
            "[GameFlow]",
            "[S6-15]",
            "[YooAsset]",
            "[InteractableObject",
            "[InteractionCoordinator]",
            "AssetBundle",
            "Cannot load asset",
            "scene to load is null",
            "OnRequestSceneChange",
        };

        private const string PuzzleProviderFailLoud = "PuzzleConfigProvider 未注册";
        private const string InputProviderFailLoud = "InputConfigProvider 未注册";

        public S615Tester(MonoBehaviour host)
        {
            _hostBehaviour = host;
        }

        public void SubscribeEarlyListeners()
        {
            Application.logMessageReceived += OnLogReceived;
        }

        public void UnsubscribeEarlyListeners()
        {
            Application.logMessageReceived -= OnLogReceived;
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count < 500)
            {
                _capturedLogs.Add($"[{type}] {condition}");
            }

            if (type == LogType.Error || type == LogType.Exception)
            {
                if (!string.IsNullOrEmpty(condition))
                {
                    if (condition.IndexOf(PuzzleProviderFailLoud, StringComparison.Ordinal) >= 0 ||
                        condition.IndexOf(InputProviderFailLoud, StringComparison.Ordinal) >= 0)
                    {
                        FailLoudProviderErrorCount++;
                    }
                }

                bool isExpected = false;
                foreach (var pattern in ExpectedLogSubstrings)
                {
                    if (!string.IsNullOrEmpty(condition) && condition.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isExpected = true;
                        break;
                    }
                }
                if (!isExpected)
                {
                    UnexpectedErrorCount++;
                }
            }
        }

        private static SceneManager GetProductionSceneManager()
        {
            var fi = typeof(GameApp).GetField("_sceneManager", BindingFlags.NonPublic | BindingFlags.Static);
            if (fi == null)
            {
                Log.Error("[S6-15] 反射拿 GameApp._sceneManager 字段失败：FieldInfo == null");
                return null;
            }
            return fi.GetValue(null) as SceneManager;
        }

        private static async UniTask<bool> WaitForIdleAsync(SceneManager scene, double timeoutSec)
        {
            var sw = Stopwatch.StartNew();
            while (scene.CurrentState != SceneManagerState.Idle)
            {
                if (sw.Elapsed.TotalSeconds > timeoutSec)
                    return false;
                await UniTask.Yield();
            }
            return true;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            if (target == null) return default;
            var fi = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi == null) return default;
            return (T)fi.GetValue(target);
        }

        private static Func<int, PuzzleConfig> GetPuzzleConfigProvider()
        {
            var fi = typeof(InteractableObject).GetField("_puzzleConfigProvider", BindingFlags.NonPublic | BindingFlags.Static);
            return fi?.GetValue(null) as Func<int, PuzzleConfig>;
        }

        private static Func<IInputConfig> GetInputConfigProvider()
        {
            var fi = typeof(InteractionCoordinator).GetField("_inputConfigProvider", BindingFlags.NonPublic | BindingFlags.Static);
            return fi?.GetValue(null) as Func<IInputConfig>;
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
                Log.Info($"[S6-15] Done. AllPassed={AllPassed} Elapsed={_swTotal.ElapsedMilliseconds}ms UnexpectedError={UnexpectedErrorCount} FailLoudProviderErrors={FailLoudProviderErrorCount}");
            }
            catch (Exception ex)
            {
                OverallStatus = $"Crashed: {ex.GetType().Name} {ex.Message}";
                _asserts["fatal.exception"] = $"FAIL: {ex}";
                Log.Error($"[S6-15] RunAllAsync crashed: {ex}");
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
            Log.Info("[S6-15] Chapter 1 baseline 加载...");
            var sm = GetProductionSceneManager();
            if (sm == null)
            {
                BaselinePassed = false;
                _asserts["baseline.production_sm_present"] = "FAIL: GameApp._sceneManager 反射拿 null";
                return;
            }

            GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
            bool loaded = await WaitForIdleAsync(sm, timeoutSec: 15.0);
            bool chapterOk = loaded && sm.CurrentChapterId == 1;
            BaselinePassed = chapterOk;
            _asserts["baseline.chapter1_loaded"] = chapterOk
                ? "PASS: chapter 1 loaded (state=Idle, currentChapterId=1)"
                : $"FAIL: state={sm.CurrentState} currentChapterId={sm.CurrentChapterId} loaded={loaded}";
            Log.Info($"[S6-15] baseline {(BaselinePassed == true ? "✅ PASS" : "❌ FAIL")} (state={sm.CurrentState}, currentChapterId={sm.CurrentChapterId})");
            await UniTask.Yield();
        }

        private async UniTask RunP1Async()
        {
            var provider = GetPuzzleConfigProvider();
            bool providerOk = provider != null;
            _asserts["P1.provider_non_null"] = $"{(providerOk ? "PASS" : "FAIL")}: _puzzleConfigProvider != null";

            bool invokeOk = false;
            int puzzleId = 0;
            if (providerOk)
            {
                var cfg = provider(1);
                invokeOk = cfg != null;
                puzzleId = cfg?.Id ?? 0;
            }
            bool idOk = puzzleId == 1;
            _asserts["P1.invoke_returns_non_null"] = $"{(invokeOk ? "PASS" : "FAIL")}: provider(1) 返非 null PuzzleConfig";
            _asserts["P1.invoke_id"] = $"{(idOk ? "PASS" : "FAIL")}: provider(1).Id==1, actual {puzzleId}";

            P1Passed = providerOk && invokeOk && idOk;
            Log.Info($"[S6-15][P1] {(P1Passed == true ? "✅ PASS" : "❌ FAIL")} providerOk={providerOk} invokeOk={invokeOk}");
            await UniTask.Yield();
        }

        private async UniTask RunP2Async()
        {
            var provider = GetInputConfigProvider();
            bool providerOk = provider != null;
            _asserts["P2.provider_non_null"] = $"{(providerOk ? "PASS" : "FAIL")}: _inputConfigProvider != null";

            bool invokeOk = false;
            string typeName = "null";
            if (providerOk)
            {
                var cfg = provider();
                invokeOk = cfg != null;
                typeName = cfg?.GetType().Name ?? "null";
            }
            bool typeOk = typeName == "InputConfigFromLuban";
            _asserts["P2.invoke_returns_non_null"] = $"{(invokeOk ? "PASS" : "FAIL")}: provider() 返非 null IInputConfig";
            _asserts["P2.invoke_type"] = $"{(typeOk ? "PASS" : "FAIL")}: expected InputConfigFromLuban, actual {typeName}";

            P2Passed = providerOk && invokeOk && typeOk;
            Log.Info($"[S6-15][P2] {(P2Passed == true ? "✅ PASS" : "❌ FAIL")} type={typeName}");
            await UniTask.Yield();
        }

        private async UniTask RunP3Async()
        {
            var all = UnityEngine.Object.FindObjectsOfType<InteractableObject>();
            bool countOk = all.Length == 2;
            _asserts["P3.interactable_count"] = $"{(countOk ? "PASS" : "FAIL")}: expected 2 InteractableObject, actual {all.Length}";

            bool allResolved = true;
            bool allIdOk = true;
            bool allBoundsOk = true;

            foreach (var io in all)
            {
                var cfg = GetPrivateField<PuzzleConfig>(io, "_puzzleConfig");
                if (cfg == null)
                {
                    allResolved = false;
                    continue;
                }
                if (cfg.Id != 1) allIdOk = false;
                var b = cfg.InteractionBounds;
                bool boundsOk = Mathf.Approximately(b.MinX, -10f) && Mathf.Approximately(b.MaxX, 10f);
                if (!boundsOk) allBoundsOk = false;
            }

            _asserts["P3.puzzle_config_resolved"] = $"{(allResolved ? "PASS" : "FAIL")}: 每实例 _puzzleConfig != null";
            _asserts["P3.puzzle_id"] = $"{(allIdOk ? "PASS" : "FAIL")}: 每实例 PuzzleConfig.Id==1";
            _asserts["P3.interaction_bounds"] = $"{(allBoundsOk ? "PASS" : "FAIL")}: InteractionBounds MinX=-10 MaxX=10 (fixture)";

            P3Passed = countOk && allResolved && allIdOk && allBoundsOk;
            Log.Info($"[S6-15][P3] {(P3Passed == true ? "✅ PASS" : "❌ FAIL")} count={all.Length}");
            await UniTask.Yield();
        }

        private async UniTask RunP4Async()
        {
            var coordinator = UnityEngine.Object.FindObjectOfType<InteractionCoordinator>();
            bool exists = coordinator != null;
            _asserts["P4.coordinator_exists"] = $"{(exists ? "PASS" : "FAIL")}: FindObjectOfType<InteractionCoordinator>() != null";

            bool lockedOk = false;
            bool inputOk = false;
            bool marginOk = false;
            float margin = 0f;

            if (exists)
            {
                lockedOk = !coordinator.IsLocked;
                var inputCfg = GetPrivateField<IInputConfig>(coordinator, "_inputConfig");
                inputOk = inputCfg != null;
                if (inputCfg != null)
                {
                    margin = inputCfg.FatFingerMarginMm;
                    marginOk = margin > 0f && Mathf.Approximately(margin, 8f);
                }
            }

            _asserts["P4.is_locked_false"] = $"{(lockedOk ? "PASS" : "FAIL")}: IsLocked==false";
            _asserts["P4.input_config_non_null"] = $"{(inputOk ? "PASS" : "FAIL")}: _inputConfig != null";
            _asserts["P4.fat_finger_margin"] = $"{(marginOk ? "PASS" : "FAIL")}: FatFingerMarginMm==8 (InitWithDefaults), actual {margin}";

            P4Passed = exists && lockedOk && inputOk && marginOk;
            Log.Info($"[S6-15][P4] {(P4Passed == true ? "✅ PASS" : "❌ FAIL")} locked={!lockedOk} margin={margin}");
            await UniTask.Yield();
        }

        private async UniTask RunP5Async()
        {
            int puzzleFailCount = 0;
            int inputFailCount = 0;
            foreach (var line in _capturedLogs)
            {
                if (line.IndexOf(PuzzleProviderFailLoud, StringComparison.Ordinal) >= 0)
                    puzzleFailCount++;
                if (line.IndexOf(InputProviderFailLoud, StringComparison.Ordinal) >= 0)
                    inputFailCount++;
            }

            bool puzzleOk = puzzleFailCount == 0;
            bool inputOk = inputFailCount == 0;
            bool unexpectedOk = UnexpectedErrorCount == 0;
            bool failLoudOk = FailLoudProviderErrorCount == 0;

            _asserts["P5.no_puzzle_provider_fail_loud"] = $"{(puzzleOk ? "PASS" : "FAIL")}: 0 Log.Error 含 '{PuzzleProviderFailLoud}', actual {puzzleFailCount}";
            _asserts["P5.no_input_provider_fail_loud"] = $"{(inputOk ? "PASS" : "FAIL")}: 0 Log.Error 含 '{InputProviderFailLoud}', actual {inputFailCount}";
            _asserts["P5.unexpected_error_count"] = $"{(unexpectedOk ? "PASS" : "FAIL")}: UnexpectedErrorCount==0, actual {UnexpectedErrorCount}";
            _asserts["P5.fail_loud_counter"] = $"{(failLoudOk ? "PASS" : "FAIL")}: FailLoudProviderErrorCount==0, actual {FailLoudProviderErrorCount}";

            P5Passed = puzzleOk && inputOk && unexpectedOk && failLoudOk;
            Log.Info($"[S6-15][P5] {(P5Passed == true ? "✅ PASS" : "❌ FAIL")} puzzleFail={puzzleFailCount} inputFail={inputFailCount} unexpected={UnexpectedErrorCount}");
            await UniTask.Yield();
        }

        private void WriteResultJson()
        {
            try
            {
                var sb = new StringBuilder(2048);
                sb.Append("{\n");
                sb.Append("  \"spike\": \"S6-15 GameApp Provider Injection (Track F vs-chapter-1-006)\",\n");
                sb.Append($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\n");
                sb.Append($"  \"overallStatus\": \"{Escape(OverallStatus)}\",\n");
                sb.Append($"  \"allPassed\": {(AllPassed ? "true" : "false")},\n");
                sb.Append($"  \"totalElapsedMs\": {TotalElapsedMs},\n");
                sb.Append($"  \"unexpectedErrorCount\": {UnexpectedErrorCount},\n");
                sb.Append($"  \"failLoudProviderErrorCount\": {FailLoudProviderErrorCount},\n");
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
                Log.Info($"[S6-15] JSON evidence dumped to {ResultFilePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"[S6-15] WriteResultJson 失败: {ex.Message}");
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
