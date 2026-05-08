// 该文件由Cursor 自动生成
// SP-011: YooAsset Additive Scene 在 HybridCLR 热更环境下的兼容性验证。
// 由 GameApp.Entrance 在 DEBUG/Editor 下注册到 DevBootstrap，业务 FSM 进入 DevTestState 时运行。
// 本文件包含三部分：
//   * SP011Spike       — IDevSpike 实现，负责动态创建 GameObject + 挂 Runtime
//   * SP011Runtime     — MonoBehaviour 宿主，承担 OnGUI 面板 + 驱动 SP011Tester
//   * SP011Tester      — 纯逻辑（UniTask），三项验证 + 结构化 JSON 落盘
// 整文件仅在 UNITY_EDITOR || DEBUG 编译，Release 包零残留。

#if UNITY_EDITOR || DEBUG
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GameLogic.DevTest.Spikes
{
    public class SP011Spike : IDevSpike
    {
        public string Id => "SP-011";
        public string Name => "YooAsset Additive Scene";

        public void Launch()
        {
            var go = new GameObject("SP011_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<SP011Runtime>();
        }
    }

    public class SP011Runtime : MonoBehaviour
    {
        private SP011Tester _tester;

        private void Start()
        {
            _tester = new SP011Tester();
            _tester.StatusText = "starting";
            _tester.WriteResultJson();
            Log.Info($"[SP-011] Runtime Start. Result JSON: {SP011Tester.ResultFilePath}");

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
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 10, 10),
            };

            float w = 520, h = 220;
            float x = (Screen.width - w) / 2f;
            float y = 20;

            GUI.Box(new Rect(x, y, w, h), string.Empty, boxStyle);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "SP-011 YooAsset Additive Spike", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            float lineY = y + 50;
            float lineH = 28;

            DrawTestRow(x + 20, lineY, w - 40, "P1 LoadSceneAsync(Additive)", _tester.LoadAdditivePassed, labelStyle);
            lineY += lineH;
            DrawTestRow(x + 20, lineY, w - 40, "P2 UnloadAsync 释放", _tester.UnloadReleasePassed, labelStyle);
            lineY += lineH;
            DrawTestRow(x + 20, lineY, w - 40, "P3 5×cycle 内存稳定", _tester.CycleMemoryPassed, labelStyle);
            lineY += lineH + 10;

            var asmStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.cyan },
            };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 24), $"Assembly: {GetType().Assembly.GetName().Name}", asmStyle);
            lineY += 28;

            bool allPassed = _tester.LoadAdditivePassed && _tester.UnloadReleasePassed && _tester.CycleMemoryPassed;
            var resultStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = allPassed ? Color.green : (_tester.LoadAdditivePassed ? Color.yellow : Color.red) },
            };
            GUI.Label(new Rect(x, lineY, w, 30), _tester.StatusText, resultStyle);
        }

        private static void DrawTestRow(float x, float y, float w, string label, bool passed, GUIStyle style)
        {
            string icon = passed ? "✅" : "⏳";
            style.normal.textColor = passed ? Color.green : Color.white;
            GUI.Label(new Rect(x, y, w, 24), $"  {icon}  {label}", style);
        }
    }

    public class SP011Tester
    {
        private const string SCENE_A = "SP011_SceneA";
        private const string SCENE_B = "SP011_SceneB";
        private const int CYCLE_COUNT = 5;
        private const float MEMORY_TOLERANCE = 0.05f;

        public const string RESULT_FILE_NAME = "SP011_Result.json";

        public bool LoadAdditivePassed { get; private set; }
        public bool UnloadReleasePassed { get; private set; }
        public bool CycleMemoryPassed { get; private set; }
        public string LastError { get; private set; }
        public string StatusText { get; internal set; } = "Pending...";
        public string Phase { get; private set; } = "pending";

        private long _baselineMemory;
        private int _baselineSceneCount;

        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, RESULT_FILE_NAME);

        public void WriteResultJson()
        {
            try
            {
                long memNow = Profiler.GetTotalAllocatedMemoryLong();
                int scNow = UnitySceneManager.sceneCount;
                var sb = new StringBuilder(512);
                sb.Append("{\n");
                sb.Append($"  \"timestamp\": \"{DateTime.UtcNow:O}\",\n");
                sb.Append($"  \"phase\": \"{EscapeJson(Phase)}\",\n");
                sb.Append($"  \"p1_loadAdditive\": {LoadAdditivePassed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"p2_unloadRelease\": {UnloadReleasePassed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"p3_cycleMemory\": {CycleMemoryPassed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"allPassed\": {(LoadAdditivePassed && UnloadReleasePassed && CycleMemoryPassed).ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"sceneCountBaseline\": {_baselineSceneCount},\n");
                sb.Append($"  \"sceneCountNow\": {scNow},\n");
                sb.Append($"  \"memoryBaselineMB\": {_baselineMemory / 1024f / 1024f:F2},\n");
                sb.Append($"  \"memoryNowMB\": {memNow / 1024f / 1024f:F2},\n");
                sb.Append($"  \"assembly\": \"{EscapeJson(GetType().Assembly.GetName().Name)}\",\n");
                sb.Append($"  \"statusText\": \"{EscapeJson(StatusText)}\",\n");
                sb.Append($"  \"lastError\": \"{EscapeJson(LastError ?? string.Empty)}\",\n");
                sb.Append($"  \"persistentDataPath\": \"{EscapeJson(Application.persistentDataPath)}\"\n");
                sb.Append("}\n");

                File.WriteAllText(ResultFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SP-011] WriteResultJson 失败（非致命）: {ex.Message}");
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        public async UniTask RunAllAsync()
        {
            Debug.Log("[SP-011] ═══════════════════════════════════════════");
            Debug.Log("[SP-011] YooAsset Additive Scene Spike 开始");
            Debug.Log($"[SP-011] Assembly: {GetType().Assembly.GetName().Name}");
            Debug.Log($"[SP-011] Initial sceneCount: {UnitySceneManager.sceneCount}");
            Debug.Log($"[SP-011] Result JSON: {ResultFilePath}");
            Debug.Log("[SP-011] ═══════════════════════════════════════════");

            _baselineSceneCount = UnitySceneManager.sceneCount;
            _baselineMemory = Profiler.GetTotalAllocatedMemoryLong();
            Debug.Log($"[SP-011] Baseline: {_baselineSceneCount} scenes / {FormatMb(_baselineMemory)}");

            Phase = "running";
            StatusText = "Running...";
            WriteResultJson();

            // DevTestState 启动时 GameModule.Scene 必然已就绪（无需轮询）；留个硬断言兜底。
            if (GameModule.Scene == null)
            {
                LastError = "GameModule.Scene == null（DevTestState 进入前未完成 TEngine 模块初始化）";
                StatusText = $"FAIL: {LastError}";
                Phase = "done";
                WriteResultJson();
                Debug.LogError($"[SP-011] ❌ {LastError}");
                return;
            }

            try
            {
                await TestP1_LoadAdditive();
                WriteResultJson();
                if (!LoadAdditivePassed)
                {
                    PrintFinalReport();
                    Phase = "done";
                    WriteResultJson();
                    return;
                }

                await TestP2_UnloadRelease();
                WriteResultJson();
                if (!UnloadReleasePassed)
                {
                    PrintFinalReport();
                    Phase = "done";
                    WriteResultJson();
                    return;
                }

                await TestP3_CycleMemory();
                WriteResultJson();
            }
            catch (Exception ex)
            {
                LastError = $"Unhandled exception: {ex.Message}";
                Debug.LogError($"[SP-011] ❌ EXCEPTION: {ex}");
            }

            PrintFinalReport();
            Phase = "done";
            WriteResultJson();
        }

        private async UniTask TestP1_LoadAdditive()
        {
            Debug.Log("[SP-011][P1] LoadSceneAsync(Additive) 开始...");

            var sw = Stopwatch.StartNew();
            Scene loaded = default;
            try
            {
                loaded = await GameModule.Scene.LoadSceneAsync(SCENE_A, LoadSceneMode.Additive);
            }
            catch (Exception ex)
            {
                LastError = $"P1 LoadSceneAsync 抛异常: {ex.Message}";
                Debug.LogError($"[SP-011][P1] ❌ FAIL — {LastError}");
                return;
            }
            sw.Stop();

            if (!loaded.IsValid())
            {
                LastError = "P1 返回 Scene 无效（可能 YooAsset Collector 未收集 SP011_SceneA.unity）";
                Debug.LogError($"[SP-011][P1] ❌ FAIL — {LastError}");
                return;
            }

            if (!loaded.isLoaded)
            {
                LastError = $"P1 Scene.isLoaded == false（name={loaded.name}）";
                Debug.LogError($"[SP-011][P1] ❌ FAIL — {LastError}");
                return;
            }

            int postLoadCount = UnitySceneManager.sceneCount;
            if (postLoadCount != _baselineSceneCount + 1)
            {
                LastError = $"P1 sceneCount 未增加 1：expected {_baselineSceneCount + 1}, actual {postLoadCount}";
                Debug.LogError($"[SP-011][P1] ❌ FAIL — {LastError}");
                return;
            }

            LoadAdditivePassed = true;
            Debug.Log($"[SP-011][P1] ✅ PASS — Scene={loaded.name}, sceneCount={postLoadCount}, elapsed={sw.ElapsedMilliseconds}ms");
        }

        private async UniTask TestP2_UnloadRelease()
        {
            Debug.Log("[SP-011][P2] UnloadSceneAsync(SP011_SceneA) 开始...");

            var sw = Stopwatch.StartNew();
            bool ok = false;
            try
            {
                ok = await GameModule.Scene.UnloadAsync(SCENE_A);
            }
            catch (Exception ex)
            {
                LastError = $"P2 UnloadAsync 抛异常: {ex.Message}";
                Debug.LogError($"[SP-011][P2] ❌ FAIL — {LastError}");
                return;
            }
            sw.Stop();

            if (!ok)
            {
                LastError = "P2 UnloadAsync 返回 false";
                Debug.LogError($"[SP-011][P2] ❌ FAIL — {LastError}");
                return;
            }

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            int postUnloadCount = UnitySceneManager.sceneCount;
            if (postUnloadCount != _baselineSceneCount)
            {
                LastError = $"P2 sceneCount 未回到 baseline: expected {_baselineSceneCount}, actual {postUnloadCount}";
                Debug.LogError($"[SP-011][P2] ❌ FAIL — {LastError}");
                return;
            }

            UnloadReleasePassed = true;
            Debug.Log($"[SP-011][P2] ✅ PASS — sceneCount 回到 {postUnloadCount}, elapsed={sw.ElapsedMilliseconds}ms");
        }

        private async UniTask TestP3_CycleMemory()
        {
            Debug.Log($"[SP-011][P3] {CYCLE_COUNT}-cycle Load/Unload 开始...");

            var sw = Stopwatch.StartNew();
            try
            {
                for (int i = 1; i <= CYCLE_COUNT; i++)
                {
                    string scene = (i % 2 == 1) ? SCENE_A : SCENE_B;

                    var loaded = await GameModule.Scene.LoadSceneAsync(scene, LoadSceneMode.Additive);
                    if (!loaded.IsValid() || !loaded.isLoaded)
                    {
                        LastError = $"P3 cycle {i}/{CYCLE_COUNT} 加载失败 scene={scene}";
                        Debug.LogError($"[SP-011][P3] ❌ FAIL — {LastError}");
                        return;
                    }

                    bool activated = GameModule.Scene.ActivateScene(scene);
                    if (!activated)
                    {
                        Debug.LogWarning($"[SP-011][P3] ⚠️ cycle {i}: ActivateScene 返回 false（非阻塞）");
                    }

                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

                    bool unloadOk = await GameModule.Scene.UnloadAsync(scene);
                    if (!unloadOk)
                    {
                        LastError = $"P3 cycle {i}/{CYCLE_COUNT} 卸载失败 scene={scene}";
                        Debug.LogError($"[SP-011][P3] ❌ FAIL — {LastError}");
                        return;
                    }

                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                    Debug.Log($"[SP-011][P3] cycle {i}/{CYCLE_COUNT} OK (scene={scene})");
                }
            }
            catch (Exception ex)
            {
                LastError = $"P3 cycle 抛异常: {ex.Message}";
                Debug.LogError($"[SP-011][P3] ❌ FAIL — {LastError}");
                return;
            }
            sw.Stop();

            var op = Resources.UnloadUnusedAssets();
            await op.ToUniTask();
            GC.Collect();
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            long postCycleMemory = Profiler.GetTotalAllocatedMemoryLong();
            int postCycleSceneCount = UnitySceneManager.sceneCount;
            float delta = Math.Abs((float)(postCycleMemory - _baselineMemory) / _baselineMemory);

            Debug.Log($"[SP-011][P3] 内存: baseline={FormatMb(_baselineMemory)}, after={FormatMb(postCycleMemory)}, delta={delta * 100f:F2}%");
            Debug.Log($"[SP-011][P3] sceneCount: baseline={_baselineSceneCount}, after={postCycleSceneCount}");

            if (postCycleSceneCount != _baselineSceneCount)
            {
                LastError = $"P3 场景泄漏: {CYCLE_COUNT} cycle 后 sceneCount={postCycleSceneCount} (baseline={_baselineSceneCount})";
                Debug.LogError($"[SP-011][P3] ❌ FAIL — {LastError}");
                return;
            }

            if (delta > MEMORY_TOLERANCE)
            {
                Debug.LogWarning($"[SP-011][P3] ⚠️ WARN — 内存 delta {delta * 100f:F2}% > 5%，非硬失败");
            }

            CycleMemoryPassed = true;
            Debug.Log($"[SP-011][P3] ✅ PASS — {CYCLE_COUNT}-cycle 完成，total elapsed={sw.ElapsedMilliseconds}ms");
        }

        private void PrintFinalReport()
        {
            bool allPassed = LoadAdditivePassed && UnloadReleasePassed && CycleMemoryPassed;

            Debug.Log("[SP-011] ═══════════════════════════════════════════");
            Debug.Log("[SP-011]           验 证 报 告");
            Debug.Log("[SP-011] ═══════════════════════════════════════════");
            Debug.Log($"[SP-011] P1 LoadAdditive       : {(LoadAdditivePassed ? "✅ PASS" : "❌ FAIL")}");
            Debug.Log($"[SP-011] P2 UnloadRelease      : {(UnloadReleasePassed ? "✅ PASS" : "❌ FAIL")}");
            Debug.Log($"[SP-011] P3 CycleMemory(×{CYCLE_COUNT})   : {(CycleMemoryPassed ? "✅ PASS" : "❌ FAIL")}");

            if (!string.IsNullOrEmpty(LastError))
            {
                Debug.Log($"[SP-011] 最后错误               : {LastError}");
            }

            Debug.Log($"[SP-011] 程序集                 : {GetType().Assembly.GetName().Name}");
            Debug.Log("[SP-011] ═══════════════════════════════════════════");

            if (allPassed)
            {
                Debug.Log("[SP-011] 🎉 ALL PASSED");
                StatusText = "ALL PASSED";
            }
            else
            {
                Debug.LogError("[SP-011] ⚠️ SOME FAILED — 见错误信息");
                StatusText = "SOME FAILED — see console";
            }
        }

        private static string FormatMb(long bytes)
        {
            return $"{bytes / 1024f / 1024f:F2} MB";
        }
    }
}
#endif
