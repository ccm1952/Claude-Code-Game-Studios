// 该文件由Cursor 自动生成
// S5-1b: SceneManager Boot Pipeline Integration PlayMode spike — 验证 GameApp.Entrance 内 _sceneManager 真接入 +
//   DevTestState OnEnter auto trigger OnRequestSceneChange(1) → 11-step BeginTransitionAsync 真跑通 +
//   5 R3 case (M1 双层模式 sign-off 2026-05-09)：
//     P1 ProductionFirstBoot — 反射读 GameApp._sceneManager 验 chapter 1 加载完成 + 8 lifecycle event 顺序
//     P2 SameChapterDedupe   — 复用 production；二次 OnRequestSceneChange(1) 立即 OnSceneReady 不二次 transition
//     P3 ProductionUnload    — 复用 production；await UnloadCurrentChapterAsync 后状态归零
//     P4 UnknownChapterFail  — spike-local SceneManager；BeginTransitionAsync(99) → ResolveChapterData null → fail-loud
//     P5 ProviderNullFail    — spike-local SceneManager；不 RegisterChapterDataProvider → BeginTransitionAsync(1) fail-loud
//   M1 双层关键约束：
//     * P1/P2/P3 反射拿 production GameApp._sceneManager，避免双 LoadSceneAsync 撞 YooAsset 锁
//     * P4/P5 自构建 spike-local SceneManager 不调 Init()，直接 await BeginTransitionAsync 进入 LoadChapterSceneAsync 入口 fail-loud 路径
//   GameApp 注册时保持其余 spike 全部注释（type-3 race 防御）。
//   整文件仅在 UNITY_EDITOR || DEBUG 编译，Release 包零残留。

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
    public class S51bSpike : IDevSpike
    {
        public string Id => "S5-1b";
        public string Name => "SceneManager Boot Pipeline Integration (S5-1b)";

        public void Launch()
        {
            var go = new GameObject("S51b_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S51bRuntime>();
        }
    }

    public class S51bRuntime : MonoBehaviour
    {
        private S51bTester _tester;

        private void Start()
        {
            _tester = new S51bTester();
            _tester.WriteResultJson();
            Log.Info($"[S5-1b] Runtime Start. Result JSON: {S51bTester.ResultFilePath}");

            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            await UniTask.Yield();
            await _tester.RunAllAsync();
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

            float w = 760, h = 320;
            float x = (Screen.width - w) / 2f;
            float y = 20;

            GUI.Box(new Rect(x, y, w, h), string.Empty, boxStyle);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S5-1b SceneManager Boot Pipeline Integration", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawRow(x + 20, lineY, w - 40, "P1 ProductionFirstBoot (反射 production；8 lifecycle event 顺序)", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P2 SameChapterDedupe (复用 production；OnSceneReady 立即触发)", _tester.P2Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P3 ProductionUnload (await UnloadCurrentChapterAsync)", _tester.P3Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P4 UnknownChapterFail (spike-local；ResolveChapterData null)", _tester.P4Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P5 ProviderNullFail (spike-local；ChapterDataProvider not registered)", _tester.P5Passed, labelStyle);
            lineY += lineH + 10;

            var footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"AllPassed: {_tester.AllPassed}    JSON: {S51bTester.ResultFilePath}", footerStyle);
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
    /// 5 case 实施 + JSON 落盘。M1 双层模式：
    /// P1/P2/P3 反射 GameApp._sceneManager + per-event listener subscribe production senders；
    /// P4/P5 自构建 spike-local SceneManager 直调 BeginTransitionAsync 走 fail-loud 路径。
    /// </summary>
    public class S51bTester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S5-1b_Result.json");

        public bool? P1Passed { get; private set; }
        public bool? P2Passed { get; private set; }
        public bool? P3Passed { get; private set; }
        public bool? P4Passed { get; private set; }
        public bool? P5Passed { get; private set; }

        public bool AllPassed =>
            P1Passed == true && P2Passed == true && P3Passed == true &&
            P4Passed == true && P5Passed == true;

        public string OverallStatus { get; private set; } = "Running";

        // case 详细 evidence — 写入 JSON 的 events / asserts list
        private readonly List<string> _p1Events = new List<string>();
        private readonly List<string> _p2Events = new List<string>();
        private readonly List<string> _p3Events = new List<string>();
        private readonly List<string> _p4Events = new List<string>();
        private readonly List<string> _p5Events = new List<string>();
        private readonly Dictionary<string, string> _asserts = new Dictionary<string, string>();

        public async UniTask RunAllAsync()
        {
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
                Log.Info($"[S5-1b] Done. AllPassed={AllPassed}");
            }
            catch (Exception e)
            {
                OverallStatus = $"Crashed: {e.GetType().Name}";
                Log.Error($"[S5-1b] RunAllAsync 异常：{e}");
            }
            finally
            {
                WriteResultJson();
            }
        }

        // ------------------------------------------------------------------
        // P1 ProductionFirstBoot — 反射 production；8 lifecycle event 顺序
        // ------------------------------------------------------------------
        private async UniTask RunP1Async()
        {
            Log.Info("[S5-1b] P1 ProductionFirstBoot 开始");
            var prodScene = GetProductionSceneManager();
            if (prodScene == null)
            {
                _asserts["P1.production_scene_manager_present"] = "FAIL: GameApp._sceneManager 反射拿 null";
                P1Passed = false;
                return;
            }

            // listener subscribe production senders（per-event mode；teardown 必须自摘）
            var transitionBeginCount = 0;
            var loadCompleteCount = 0;
            var loadCompletePayload = (chapterId: -999, bgmAsset: "<unset>");
            var sceneReadyCount = 0;
            var transitionEndCount = 0;
            var loadProgressCount = 0;

            Action<int, int> onTB = (from, to) => { transitionBeginCount++; _p1Events.Add($"OnSceneTransitionBegin({from},{to})"); };
            Action<string, float> onLP = (sceneName, progress) => { loadProgressCount++; if (loadProgressCount <= 2) _p1Events.Add($"OnSceneLoadProgress({sceneName},{progress:F2})"); };
            Action<int, string> onLC = (id, bgm) => { loadCompleteCount++; loadCompletePayload = (id, bgm); _p1Events.Add($"OnSceneLoadComplete({id},'{bgm}')"); };
            Action<int> onR = id => { sceneReadyCount++; _p1Events.Add($"OnSceneReady({id})"); };
            Action<int> onTE = id => { transitionEndCount++; _p1Events.Add($"OnSceneTransitionEnd({id})"); };

            GameEvent.AddEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, onTB);
            GameEvent.AddEventListener<string, float>(ISceneEvent_Event.OnSceneLoadProgress, onLP);
            GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadComplete, onLC);
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneReady, onR);
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneTransitionEnd, onTE);

            try
            {
                // 等 production transition 完成（DevTestState OnEnter 已派发 OnRequestSceneChange(1) 在 spike Launch 之前）
                var idleOk = await WaitForIdleAsync(prodScene, timeoutSec: 5.0);

                // assert state
                _asserts["P1.timeout"] = idleOk ? "PASS: state == Idle within 5s" : "FAIL: timeout";
                _asserts["P1.CurrentLoadedChapterIdForTest"] = $"expected=1 actual={prodScene.CurrentLoadedChapterIdForTest}";
                _asserts["P1.CurrentChapterSceneNameForTest"] = $"expected='Chapter_01_Approach' actual='{prodScene.CurrentChapterSceneNameForTest}'";
                _asserts["P1.CurrentState"] = $"expected=Idle actual={prodScene.CurrentState}";

                var loadCompleteOk = loadCompleteCount >= 1 && loadCompletePayload.chapterId == 1 && loadCompletePayload.bgmAsset == string.Empty;
                _asserts["P1.OnSceneLoadComplete"] = loadCompleteOk
                    ? $"PASS: count={loadCompleteCount} payload=(1,'')"
                    : $"FAIL: count={loadCompleteCount} payload=({loadCompletePayload.chapterId},'{loadCompletePayload.bgmAsset}')";

                _asserts["P1.OnSceneReady"] = sceneReadyCount >= 1 ? $"PASS: count={sceneReadyCount}" : $"FAIL: count={sceneReadyCount}";
                _asserts["P1.OnSceneTransitionEnd"] = transitionEndCount >= 1 ? $"PASS: count={transitionEndCount}" : $"FAIL: count={transitionEndCount}";
                // first-boot OnSceneTransitionBegin 期望 ≥1（DevTestState 派发）；OnSceneLoadProgress 期望 ≥1（11-step 内 sender）
                _asserts["P1.OnSceneTransitionBegin"] = transitionBeginCount >= 1 ? $"PASS: count={transitionBeginCount}" : $"FAIL: count={transitionBeginCount} (DevTestState 触发后期望 ≥1)";
                _asserts["P1.OnSceneLoadProgress"] = loadProgressCount >= 1 ? $"PASS: count={loadProgressCount}" : $"FAIL: count={loadProgressCount}";

                P1Passed =
                    idleOk &&
                    prodScene.CurrentLoadedChapterIdForTest == 1 &&
                    prodScene.CurrentChapterSceneNameForTest == "Chapter_01_Approach" &&
                    prodScene.CurrentState == SceneManagerState.Idle &&
                    loadCompleteOk &&
                    sceneReadyCount >= 1 &&
                    transitionEndCount >= 1;
            }
            finally
            {
                GameEvent.RemoveEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, onTB);
                GameEvent.RemoveEventListener<string, float>(ISceneEvent_Event.OnSceneLoadProgress, onLP);
                GameEvent.RemoveEventListener<int, string>(ISceneEvent_Event.OnSceneLoadComplete, onLC);
                GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneReady, onR);
                GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneTransitionEnd, onTE);
            }
        }

        // ------------------------------------------------------------------
        // P2 SameChapterDedupe — 复用 production；OnSceneReady 立即触发，无 OnSceneTransitionBegin
        // ------------------------------------------------------------------
        private async UniTask RunP2Async()
        {
            Log.Info("[S5-1b] P2 SameChapterDedupe 开始");
            var prodScene = GetProductionSceneManager();
            if (prodScene == null)
            {
                _asserts["P2.production_scene_manager_present"] = "FAIL";
                P2Passed = false;
                return;
            }
            // 前置：P1 完成后 production CurrentLoadedChapterId == 1，state == Idle
            if (prodScene.CurrentLoadedChapterIdForTest != 1 || prodScene.CurrentState != SceneManagerState.Idle)
            {
                _asserts["P2.precondition"] = $"FAIL: CurrentLoadedChapterId={prodScene.CurrentLoadedChapterIdForTest} state={prodScene.CurrentState}";
                P2Passed = false;
                return;
            }

            var transitionBeginCount = 0;
            var sceneReadyCount = 0;

            Action<int, int> onTB = (from, to) => { transitionBeginCount++; _p2Events.Add($"OnSceneTransitionBegin({from},{to})"); };
            Action<int> onR = id => { sceneReadyCount++; _p2Events.Add($"OnSceneReady({id})"); };

            GameEvent.AddEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, onTB);
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneReady, onR);

            try
            {
                // 派发同章请求；production listener 立即派 OnSceneReady (per S2-05 dedupe 规则)
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
                await UniTask.Delay(TimeSpan.FromMilliseconds(300));

                _asserts["P2.OnSceneTransitionBegin_should_be_zero"] = transitionBeginCount == 0
                    ? "PASS: 0 (无二次 transition)"
                    : $"FAIL: {transitionBeginCount} (期望 0)";
                _asserts["P2.OnSceneReady_should_be_one_or_more"] = sceneReadyCount >= 1
                    ? $"PASS: {sceneReadyCount}"
                    : $"FAIL: {sceneReadyCount} (期望 ≥1)";
                _asserts["P2.CurrentState"] = prodScene.CurrentState == SceneManagerState.Idle
                    ? "PASS: Idle"
                    : $"FAIL: {prodScene.CurrentState}";
                _asserts["P2.InflightChapterIdForTest"] = prodScene.InflightChapterIdForTest == GameLogic.SceneManager.NoChapterId
                    ? $"PASS: NoChapterId({GameLogic.SceneManager.NoChapterId})"
                    : $"FAIL: {prodScene.InflightChapterIdForTest}";

                P2Passed =
                    transitionBeginCount == 0 &&
                    sceneReadyCount >= 1 &&
                    prodScene.CurrentState == SceneManagerState.Idle &&
                    prodScene.InflightChapterIdForTest == GameLogic.SceneManager.NoChapterId;
            }
            finally
            {
                GameEvent.RemoveEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, onTB);
                GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneReady, onR);
            }
        }

        // ------------------------------------------------------------------
        // P3 ProductionUnload — await UnloadCurrentChapterAsync 后状态归零
        // ------------------------------------------------------------------
        private async UniTask RunP3Async()
        {
            Log.Info("[S5-1b] P3 ProductionUnload 开始");
            var prodScene = GetProductionSceneManager();
            if (prodScene == null)
            {
                _asserts["P3.production_scene_manager_present"] = "FAIL";
                P3Passed = false;
                return;
            }
            if (prodScene.CurrentLoadedChapterIdForTest != 1)
            {
                _asserts["P3.precondition"] = $"FAIL: CurrentLoadedChapterId={prodScene.CurrentLoadedChapterIdForTest} (期望 1)";
                P3Passed = false;
                return;
            }

            var unloadBeginCount = 0;
            Action<int> onUB = id => { unloadBeginCount++; _p3Events.Add($"OnSceneUnloadBegin({id})"); };
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, onUB);

            try
            {
                await prodScene.UnloadCurrentChapterAsync();

                _asserts["P3.CurrentLoadedChapterIdForTest"] = prodScene.CurrentLoadedChapterIdForTest == GameLogic.SceneManager.NoChapterId
                    ? $"PASS: NoChapterId"
                    : $"FAIL: {prodScene.CurrentLoadedChapterIdForTest}";
                _asserts["P3.CurrentChapterSceneNameForTest"] = prodScene.CurrentChapterSceneNameForTest == null
                    ? "PASS: null"
                    : $"FAIL: '{prodScene.CurrentChapterSceneNameForTest}'";
                _asserts["P3.OnSceneUnloadBegin"] = unloadBeginCount >= 1
                    ? $"PASS: count={unloadBeginCount}"
                    : $"FAIL: count={unloadBeginCount}";

                P3Passed =
                    prodScene.CurrentLoadedChapterIdForTest == GameLogic.SceneManager.NoChapterId &&
                    prodScene.CurrentChapterSceneNameForTest == null &&
                    unloadBeginCount >= 1;
            }
            finally
            {
                GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, onUB);
            }
        }

        // ------------------------------------------------------------------
        // P4 UnknownChapterFail — spike-local；ResolveChapterData null fail-loud
        // ------------------------------------------------------------------
        private async UniTask RunP4Async()
        {
            Log.Info("[S5-1b] P4 UnknownChapterFail 开始");

            // spike-local SceneManager 不调 Init() — 不订阅 OnRequestSceneChange，直接 BeginTransitionAsync
            var local = new SceneManager();
            local.RegisterChapterDataProvider(id => id == 1
                ? new ChapterData(id: 1, sceneId: "Chapter_01_Approach", bgmAsset: "", emotionalWeight: 1.0f, overlayColor: "#3A3530")
                : null);
            local.RegisterFadeOverlay(new NoOpFadeOverlay());

            var loadFailedCount = 0;
            var loadFailedPayload = (chapterId: -999, error: "<unset>");

            Action<int, string> onLF = (id, err) => { loadFailedCount++; loadFailedPayload = (id, err); _p4Events.Add($"OnSceneLoadFailed({id},'{err}')"); };
            GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, onLF);

            try
            {
                Debug.Log("[S5-1b][P4] expected Debug.LogError below: ChapterDataProvider returned null for unknown chapter 99");
                await local.BeginTransitionAsync(99);
                await UniTask.Delay(TimeSpan.FromMilliseconds(100));

                _asserts["P4.OnSceneLoadFailed_count"] = loadFailedCount >= 1
                    ? $"PASS: count={loadFailedCount}"
                    : $"FAIL: count={loadFailedCount}";
                _asserts["P4.OnSceneLoadFailed_chapterId"] = loadFailedPayload.chapterId == 99
                    ? $"PASS: chapterId=99"
                    : $"FAIL: chapterId={loadFailedPayload.chapterId}";
                _asserts["P4.CurrentState"] = local.CurrentState == SceneManagerState.Error
                    ? "PASS: Error"
                    : $"FAIL: {local.CurrentState}";
                _asserts["P4.CurrentLoadedChapterIdForTest"] = local.CurrentLoadedChapterIdForTest == GameLogic.SceneManager.NoChapterId
                    ? "PASS: NoChapterId（不污染）"
                    : $"FAIL: {local.CurrentLoadedChapterIdForTest}";

                P4Passed =
                    loadFailedCount >= 1 &&
                    loadFailedPayload.chapterId == 99 &&
                    local.CurrentState == SceneManagerState.Error &&
                    local.CurrentLoadedChapterIdForTest == GameLogic.SceneManager.NoChapterId;
            }
            finally
            {
                GameEvent.RemoveEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, onLF);
            }
        }

        // ------------------------------------------------------------------
        // P5 ProviderNullFail — spike-local；不 RegisterChapterDataProvider；BeginTransitionAsync(1) fail-loud
        // ------------------------------------------------------------------
        private async UniTask RunP5Async()
        {
            Log.Info("[S5-1b] P5 ProviderNullFail 开始");

            // spike-local SceneManager 不调 Init() 不 RegisterChapterDataProvider — 模拟 boot pipeline 忘 register
            var local = new SceneManager();
            local.RegisterFadeOverlay(new NoOpFadeOverlay());     // FadeOverlay 注入与否不影响 fail-loud 路径

            var loadFailedCount = 0;
            var loadFailedError = "<unset>";

            Action<int, string> onLF = (id, err) => { loadFailedCount++; loadFailedError = err; _p5Events.Add($"OnSceneLoadFailed({id},'{err}')"); };
            GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, onLF);

            try
            {
                Debug.Log("[S5-1b][P5] expected Debug.LogError below: ChapterDataProvider not registered");
                await local.BeginTransitionAsync(1);
                await UniTask.Delay(TimeSpan.FromMilliseconds(100));

                _asserts["P5.OnSceneLoadFailed_count"] = loadFailedCount >= 1
                    ? $"PASS: count={loadFailedCount}"
                    : $"FAIL: count={loadFailedCount}";
                _asserts["P5.OnSceneLoadFailed_error_contains_not_registered"] = loadFailedError != null && loadFailedError.IndexOf("not registered", StringComparison.OrdinalIgnoreCase) >= 0
                    ? $"PASS: error='{loadFailedError}'"
                    : $"FAIL: error='{loadFailedError}'";
                _asserts["P5.CurrentState"] = local.CurrentState == SceneManagerState.Error
                    ? "PASS: Error"
                    : $"FAIL: {local.CurrentState}";

                P5Passed =
                    loadFailedCount >= 1 &&
                    loadFailedError != null &&
                    loadFailedError.IndexOf("not registered", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    local.CurrentState == SceneManagerState.Error;
            }
            finally
            {
                GameEvent.RemoveEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, onLF);
            }
        }

        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------
        private static SceneManager GetProductionSceneManager()
        {
            var fi = typeof(GameApp).GetField("_sceneManager", BindingFlags.NonPublic | BindingFlags.Static);
            if (fi == null)
            {
                Log.Error("[S5-1b] 反射拿 GameApp._sceneManager 字段失败：FieldInfo == null");
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

        public void WriteResultJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append($"  \"story_id\": \"S5-1b\",\n");
            sb.Append($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\n");
            sb.Append($"  \"all_passed\": {AllPassed.ToString().ToLowerInvariant()},\n");
            sb.Append($"  \"overall_status\": \"{Escape(OverallStatus)}\",\n");
            sb.Append("  \"cases\": [\n");
            AppendCase(sb, "P1", P1Passed, _p1Events, isLast: false);
            AppendCase(sb, "P2", P2Passed, _p2Events, isLast: false);
            AppendCase(sb, "P3", P3Passed, _p3Events, isLast: false);
            AppendCase(sb, "P4", P4Passed, _p4Events, isLast: false);
            AppendCase(sb, "P5", P5Passed, _p5Events, isLast: true);
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
                Log.Error($"[S5-1b] WriteResultJson 失败：{e}");
            }
        }

        private static void AppendCase(StringBuilder sb, string id, bool? passed, List<string> events, bool isLast)
        {
            sb.Append("    {\n");
            sb.Append($"      \"id\": \"{id}\",\n");
            sb.Append($"      \"passed\": {(passed == true ? "true" : passed == false ? "false" : "null")},\n");
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
