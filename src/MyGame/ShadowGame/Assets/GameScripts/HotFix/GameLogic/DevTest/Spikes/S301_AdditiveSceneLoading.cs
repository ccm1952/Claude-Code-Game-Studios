// 该文件由Cursor 自动生成
// S3-01: Additive Scene Loading PlayMode spike — 验证 LoadChapterSceneAsync 11 步流程 Step 8-10。
// 由 GameApp.Entrance 在 DEBUG/Editor 下注册到 DevBootstrap，业务 FSM 进入 DevTestState 时运行。
// 5 case + 1 ADVISORY：
//   * S301Spike    — IDevSpike 实现，挂 GameObject + Runtime
//   * S301Runtime  — MonoBehaviour 宿主，OnGUI 报告 + 驱动 S301Tester
//   * S301Tester   — 纯逻辑，5 case (P1/P2/P3/P4) + AC-10 + ADVISORY note + JSON 落盘
// 与 SP-011 复用 SP011_SceneA / SP011_SceneB 作为章节场景资源（YooAsset Collector 已收集）；
// 整文件仅在 UNITY_EDITOR || DEBUG 编译，Release 包零残留。

#if UNITY_EDITOR || DEBUG
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GameLogic.DevTest.Spikes
{
    public class S301Spike : IDevSpike
    {
        public string Id => "S3-01";
        public string Name => "Additive Scene Loading (S3-01 patch v2)";

        public void Launch()
        {
            var go = new GameObject("S301_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S301Runtime>();
        }
    }

    public class S301Runtime : MonoBehaviour
    {
        private S301Tester _tester;

        private void Start()
        {
            _tester = new S301Tester();
            _tester.WriteResultJson();
            Log.Info($"[S3-01] Runtime Start. Result JSON: {S301Tester.ResultFilePath}");

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

            float w = 600, h = 320;
            float x = (Screen.width - w) / 2f;
            float y = 20;

            GUI.Box(new Rect(x, y, w, h), string.Empty, boxStyle);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S3-01 Additive Scene Loading Spike (patch v2)", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawTestRow(x + 20, lineY, w - 40, "P1 LoadChapter(1) Single Load + Listeners", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawTestRow(x + 20, lineY, w - 40, "P2 Switch 1→2 + AC-10 sceneCount baseline+1", _tester.P2Passed, labelStyle);
            lineY += lineH;
            DrawDownloadRow(x + 20, lineY, w - 40, _tester, labelStyle);
            lineY += lineH;
            DrawTestRow(x + 20, lineY, w - 40, "P4 Invalid sceneName → OnSceneLoadFailed (AC-4)", _tester.P4Passed, labelStyle);
            lineY += lineH;
            DrawAdvisoryRow(x + 20, lineY, w - 40, "P5 Retry mechanism (AC-9)", labelStyle);
            lineY += lineH + 6;

            var asmStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.cyan },
            };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 20), $"Assembly: {GetType().Assembly.GetName().Name}", asmStyle);
            lineY += 22;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 20),
                $"Listeners — DL:{_tester.DownloadProgressCount} LP:{_tester.LoadProgressCount} OK:{_tester.LoadCompleteCount} FAIL:{_tester.LoadFailedCount}",
                asmStyle);
            lineY += 24;

            bool allCorePassed = _tester.P1Passed && _tester.P2Passed && _tester.P4Passed;
            var resultStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = allCorePassed ? Color.green : Color.red },
            };
            GUI.Label(new Rect(x, lineY, w, 26), _tester.StatusText, resultStyle);
        }

        private static void DrawTestRow(float x, float y, float w, string label, bool passed, GUIStyle style)
        {
            string icon = passed ? "PASS" : "PEND";
            style.normal.textColor = passed ? Color.green : Color.white;
            GUI.Label(new Rect(x, y, w, 22), $"  [{icon}]  {label}", style);
        }

        private static void DrawDownloadRow(float x, float y, float w, S301Tester tester, GUIStyle style)
        {
            string label = $"P3 Download branch (AC-5)";
            string icon;
            switch (tester.P3Status)
            {
                case S301Tester.DownloadStatus.Pending:
                    icon = "PEND"; style.normal.textColor = Color.white; break;
                case S301Tester.DownloadStatus.Pass:
                    icon = "PASS"; style.normal.textColor = Color.green; break;
                case S301Tester.DownloadStatus.SkipAdvisory:
                    icon = "SKIP"; style.normal.textColor = Color.yellow; break;
                default:
                    icon = "FAIL"; style.normal.textColor = Color.red; break;
            }
            GUI.Label(new Rect(x, y, w, 22), $"  [{icon}]  {label} (status={tester.P3Status})", style);
        }

        private static void DrawAdvisoryRow(float x, float y, float w, string label, GUIStyle style)
        {
            style.normal.textColor = Color.gray;
            GUI.Label(new Rect(x, y, w, 22), $"  [ADVS] {label} — code-review only (D1=[b] no stub)", style);
        }
    }

    public class S301Tester
    {
        public enum DownloadStatus
        {
            Pending,
            Pass,          // 真实下载分支跑通
            SkipAdvisory,  // 缓存命中（TotalDownloadCount==0）→ 标 ADVISORY 不算 fail
            Fail,
        }

        // 与 SP-011 复用相同的 chapter scene 资产（YooAsset Collector 已收集）
        private const int CHAPTER_1 = 101;       // 测试 chapterId（避开 1..5 生产范围，防 Story 003 cleanup 干扰）
        private const int CHAPTER_2 = 102;
        private const int CHAPTER_3 = 103;
        private const int CHAPTER_INVALID = 999;
        private const string SCENE_A = "SP011_SceneA";
        private const string SCENE_B = "SP011_SceneB";
        private const string SCENE_C = "SP011_SceneA"; // P3 下载分支也用 A，依赖缓存状态
        private const string SCENE_INVALID = "Chapter_999_NotInManifest";

        public const string RESULT_FILE_NAME = "S301_Result.json";

        public bool P1Passed { get; private set; }
        public bool P2Passed { get; private set; }
        public DownloadStatus P3Status { get; private set; } = DownloadStatus.Pending;
        public bool P4Passed { get; private set; }
        public string LastError { get; private set; }
        public string StatusText { get; private set; } = "Pending...";
        public string Phase { get; private set; } = "pending";

        // ISceneEvent listener counters
        public int DownloadProgressCount { get; private set; }
        public int LoadProgressCount { get; private set; }
        public int LoadCompleteCount { get; private set; }
        public int LoadFailedCount { get; private set; }
        private string _lastFailReason;
        private int _lastFailedChapterId;
        private string _lastCompletedBgmAsset;
        private int _lastCompletedChapterId;

        private SceneManager _sceneManager;
        private int _baselineSceneCount;

        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, RESULT_FILE_NAME);

        public void WriteResultJson()
        {
            try
            {
                int scNow = UnitySceneManager.sceneCount;
                var sb = new StringBuilder(1024);
                sb.Append("{\n");
                sb.Append($"  \"timestamp\": \"{DateTime.UtcNow:O}\",\n");
                sb.Append($"  \"phase\": \"{EscapeJson(Phase)}\",\n");
                sb.Append($"  \"p1_passed\": {P1Passed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"p2_passed\": {P2Passed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"p3_status\": \"{P3Status}\",\n");
                sb.Append($"  \"p4_passed\": {P4Passed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"p5_advisory\": \"requires_iSceneLoader_seam_deferred\",\n");
                sb.Append($"  \"allCorePassed\": {(P1Passed && P2Passed && P4Passed).ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"sceneCountBaseline\": {_baselineSceneCount},\n");
                sb.Append($"  \"sceneCountNow\": {scNow},\n");
                sb.Append($"  \"listenerCounts\": {{\n");
                sb.Append($"    \"OnSceneDownloadProgress\": {DownloadProgressCount},\n");
                sb.Append($"    \"OnSceneLoadProgress\": {LoadProgressCount},\n");
                sb.Append($"    \"OnSceneLoadComplete\": {LoadCompleteCount},\n");
                sb.Append($"    \"OnSceneLoadFailed\": {LoadFailedCount}\n");
                sb.Append($"  }},\n");
                sb.Append($"  \"lastCompletedChapterId\": {_lastCompletedChapterId},\n");
                sb.Append($"  \"lastCompletedBgmAsset\": \"{EscapeJson(_lastCompletedBgmAsset ?? string.Empty)}\",\n");
                sb.Append($"  \"lastFailedChapterId\": {_lastFailedChapterId},\n");
                sb.Append($"  \"lastFailReason\": \"{EscapeJson(_lastFailReason ?? string.Empty)}\",\n");
                sb.Append($"  \"assembly\": \"{EscapeJson(GetType().Assembly.GetName().Name)}\",\n");
                sb.Append($"  \"statusText\": \"{EscapeJson(StatusText)}\",\n");
                sb.Append($"  \"lastError\": \"{EscapeJson(LastError ?? string.Empty)}\",\n");
                sb.Append($"  \"persistentDataPath\": \"{EscapeJson(Application.persistentDataPath)}\"\n");
                sb.Append("}\n");

                File.WriteAllText(ResultFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[S3-01] WriteResultJson 失败（非致命）: {ex.Message}");
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        public async UniTask RunAllAsync()
        {
            Debug.Log("[S3-01] ═══════════════════════════════════════════");
            Debug.Log("[S3-01] Additive Scene Loading Spike (patch v2) 开始");
            Debug.Log($"[S3-01] Assembly: {GetType().Assembly.GetName().Name}");
            Debug.Log($"[S3-01] Initial sceneCount: {UnitySceneManager.sceneCount}");
            Debug.Log("[S3-01] ═══════════════════════════════════════════");

            _baselineSceneCount = UnitySceneManager.sceneCount;
            Phase = "running";
            StatusText = "Running...";
            WriteResultJson();

            if (GameModule.Scene == null)
            {
                LastError = "GameModule.Scene == null（DevTestState 进入前未完成 TEngine 模块初始化）";
                StatusText = $"FAIL: {LastError}";
                Phase = "done";
                WriteResultJson();
                Debug.LogError($"[S3-01] {LastError}");
                return;
            }

            // 注册 4 个 ISceneEvent listener（per-event 模式 — ADR-027 + conventions.md §Listener）
            GameEvent.AddEventListener<float, long, long>(
                ISceneEvent_Event.OnSceneDownloadProgress, OnDownloadProgress);
            GameEvent.AddEventListener<string, float>(
                ISceneEvent_Event.OnSceneLoadProgress, OnLoadProgress);
            GameEvent.AddEventListener<int, string>(
                ISceneEvent_Event.OnSceneLoadComplete, OnLoadComplete);
            GameEvent.AddEventListener<int, string>(
                ISceneEvent_Event.OnSceneLoadFailed, OnLoadFailed);

            try
            {
                _sceneManager = new SceneManager();
                _sceneManager.Init();
                _sceneManager.RegisterChapterDataProvider(ResolveChapterData);

                await TestP1_SingleLoad();
                WriteResultJson();

                await TestP2_SwitchAndAC10();
                WriteResultJson();

                await TestP3_DownloadBranch();
                WriteResultJson();

                await TestP4_InvalidSceneFailLoud();
                WriteResultJson();
            }
            catch (Exception ex)
            {
                LastError = $"Unhandled exception: {ex.Message}";
                Debug.LogError($"[S3-01] EXCEPTION: {ex}");
            }
            finally
            {
                _sceneManager?.Dispose();
                _sceneManager = null;

                GameEvent.RemoveEventListener<float, long, long>(
                    ISceneEvent_Event.OnSceneDownloadProgress, OnDownloadProgress);
                GameEvent.RemoveEventListener<string, float>(
                    ISceneEvent_Event.OnSceneLoadProgress, OnLoadProgress);
                GameEvent.RemoveEventListener<int, string>(
                    ISceneEvent_Event.OnSceneLoadComplete, OnLoadComplete);
                GameEvent.RemoveEventListener<int, string>(
                    ISceneEvent_Event.OnSceneLoadFailed, OnLoadFailed);
            }

            PrintFinalReport();
            Phase = "done";
            WriteResultJson();
        }

        // ChapterData provider — 5 个测试 chapter（避开生产 1..5）
        private ChapterData ResolveChapterData(int chapterId)
        {
            switch (chapterId)
            {
                case CHAPTER_1: return new ChapterData(CHAPTER_1, SCENE_A, "bgm_test_a");
                case CHAPTER_2: return new ChapterData(CHAPTER_2, SCENE_B, "bgm_test_b");
                case CHAPTER_3: return new ChapterData(CHAPTER_3, SCENE_C, "bgm_test_c");
                case CHAPTER_INVALID: return new ChapterData(CHAPTER_INVALID, SCENE_INVALID, "bgm_invalid");
                default: return null;
            }
        }

        private void OnDownloadProgress(float progress, long current, long total)
        {
            DownloadProgressCount++;
            Debug.Log($"[S3-01] OnSceneDownloadProgress: {progress:F2} ({current}/{total} bytes)");
        }

        private void OnLoadProgress(string sceneName, float progress)
        {
            LoadProgressCount++;
            // 仅每 0.2 进度打一次（避免 log flood）
            if (Math.Abs(progress * 5 - Mathf.Round(progress * 5)) < 0.01f)
            {
                Debug.Log($"[S3-01] OnSceneLoadProgress: {sceneName} {progress:F2}");
            }
        }

        private void OnLoadComplete(int chapterId, string bgmAsset)
        {
            LoadCompleteCount++;
            _lastCompletedChapterId = chapterId;
            _lastCompletedBgmAsset = bgmAsset;
            Debug.Log($"[S3-01] OnSceneLoadComplete: chapter={chapterId}, bgm={bgmAsset}");
        }

        private void OnLoadFailed(int chapterId, string error)
        {
            LoadFailedCount++;
            _lastFailedChapterId = chapterId;
            _lastFailReason = error;
            Debug.Log($"[S3-01] OnSceneLoadFailed: chapter={chapterId}, error={error}");
        }

        private async UniTask TestP1_SingleLoad()
        {
            Debug.Log("[S3-01][P1] 单次加载 LoadChapterSceneAsync(101)...");

            int loadCompleteBefore = LoadCompleteCount;
            int loadProgressBefore = LoadProgressCount;
            var sw = Stopwatch.StartNew();

            await _sceneManager.LoadChapterSceneAsync(CHAPTER_1);
            sw.Stop();

            // AC-7: OnSceneLoadComplete 派发 1 次
            if (LoadCompleteCount != loadCompleteBefore + 1)
            {
                LastError = $"P1 OnSceneLoadComplete count mismatch: expected {loadCompleteBefore + 1}, got {LoadCompleteCount}";
                Debug.LogError($"[S3-01][P1] FAIL — {LastError}");
                return;
            }

            // AC-7: payload chapterId + bgmAsset 正确
            if (_lastCompletedChapterId != CHAPTER_1 || _lastCompletedBgmAsset != "bgm_test_a")
            {
                LastError = $"P1 OnSceneLoadComplete payload mismatch: chapterId={_lastCompletedChapterId}, bgm={_lastCompletedBgmAsset}";
                Debug.LogError($"[S3-01][P1] FAIL — {LastError}");
                return;
            }

            // AC-2: _currentChapterSceneName 已设
            if (_sceneManager.CurrentChapterSceneNameForTest != SCENE_A)
            {
                LastError = $"P1 _currentChapterSceneName mismatch: expected {SCENE_A}, got {_sceneManager.CurrentChapterSceneNameForTest}";
                Debug.LogError($"[S3-01][P1] FAIL — {LastError}");
                return;
            }

            // AC-1 / AC-10: sceneCount baseline+1
            int countNow = UnitySceneManager.sceneCount;
            if (countNow != _baselineSceneCount + 1)
            {
                LastError = $"P1 sceneCount mismatch: expected {_baselineSceneCount + 1}, got {countNow}";
                Debug.LogError($"[S3-01][P1] FAIL — {LastError}");
                return;
            }

            // AC-3: ActivateScene 已调（active scene name 与 sceneName 匹配）
            string activeName = UnitySceneManager.GetActiveScene().name;
            if (activeName != SCENE_A)
            {
                Debug.LogWarning($"[S3-01][P1] ActivateScene 弱断言 warning: active={activeName}, expected={SCENE_A}（非阻塞）");
            }

            // AC-6: OnSceneLoadProgress 至少派发 1 次（progressCallBack 路径）
            if (LoadProgressCount <= loadProgressBefore)
            {
                Debug.LogWarning($"[S3-01][P1] OnSceneLoadProgress 0 派发 — progressCallBack 可能未触发（非阻塞但 advisory）");
            }

            P1Passed = true;
            Debug.Log($"[S3-01][P1] PASS — chapter={_lastCompletedChapterId}, scene={_sceneManager.CurrentChapterSceneNameForTest}, sceneCount={countNow}, elapsed={sw.ElapsedMilliseconds}ms");
        }

        private async UniTask TestP2_SwitchAndAC10()
        {
            if (!P1Passed)
            {
                Debug.LogWarning("[S3-01][P2] SKIP — P1 未通过");
                return;
            }
            Debug.Log("[S3-01][P2] 切换 chapter 1 → 2 + AC-10 sceneCount 守恒...");

            // 手动 unload chapter 1（Story 003 cleanup 实装前的占位）
            bool unloaded = await GameModule.Scene.UnloadAsync(SCENE_A);
            if (!unloaded)
            {
                LastError = "P2 GameModule.Scene.UnloadAsync(SP011_SceneA) returned false";
                Debug.LogError($"[S3-01][P2] FAIL — {LastError}");
                return;
            }
            _sceneManager.ClearCurrentChapterSceneName();

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            int countAfterUnload = UnitySceneManager.sceneCount;
            if (countAfterUnload != _baselineSceneCount)
            {
                LastError = $"P2 sceneCount after unload mismatch: expected {_baselineSceneCount}, got {countAfterUnload}";
                Debug.LogError($"[S3-01][P2] FAIL — {LastError}");
                return;
            }

            // 推进状态机回到 Idle（释放 inflight）— 让 SceneManager 准备接 chapter 2
            _sceneManager.AdvanceStateForTest(SceneManagerState.Idle);

            // 加载 chapter 2
            int loadCompleteBefore = LoadCompleteCount;
            await _sceneManager.LoadChapterSceneAsync(CHAPTER_2);

            if (LoadCompleteCount != loadCompleteBefore + 1)
            {
                LastError = $"P2 OnSceneLoadComplete count mismatch after switch";
                Debug.LogError($"[S3-01][P2] FAIL — {LastError}");
                return;
            }

            if (_lastCompletedChapterId != CHAPTER_2 || _sceneManager.CurrentChapterSceneNameForTest != SCENE_B)
            {
                LastError = $"P2 chapter 2 payload mismatch: id={_lastCompletedChapterId}, scene={_sceneManager.CurrentChapterSceneNameForTest}";
                Debug.LogError($"[S3-01][P2] FAIL — {LastError}");
                return;
            }

            // AC-10: sceneCount 仍然 baseline+1
            int countAfterChapter2 = UnitySceneManager.sceneCount;
            if (countAfterChapter2 != _baselineSceneCount + 1)
            {
                LastError = $"P2 AC-10 violated: sceneCount={countAfterChapter2}, expected={_baselineSceneCount + 1}";
                Debug.LogError($"[S3-01][P2] FAIL — {LastError}");
                return;
            }

            P2Passed = true;
            Debug.Log($"[S3-01][P2] PASS — switch 1→2 OK, sceneCount={countAfterChapter2} (baseline+1)");
        }

        private async UniTask TestP3_DownloadBranch()
        {
            if (!P2Passed)
            {
                Debug.LogWarning("[S3-01][P3] SKIP — P2 未通过");
                return;
            }
            Debug.Log("[S3-01][P3] Download branch (AC-5)...");

            // 先 unload chapter 2 + reset
            bool unloaded = await GameModule.Scene.UnloadAsync(SCENE_B);
            if (!unloaded)
            {
                Debug.LogWarning("[S3-01][P3] UnloadAsync(SP011_SceneB) returned false (non-blocking)");
            }
            _sceneManager.ClearCurrentChapterSceneName();
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            _sceneManager.AdvanceStateForTest(SceneManagerState.Idle);

            // 检查是否有未缓存依赖（Editor 通常 cache 命中）
            var checkDownloader = GameModule.Resource.CreateResourceDownloader(SceneManager.DefaultPackage);
            int totalDownloadCount = checkDownloader?.TotalDownloadCount ?? 0;
            int dlBefore = DownloadProgressCount;

            await _sceneManager.LoadChapterSceneAsync(CHAPTER_3);

            int dlAfter = DownloadProgressCount;
            int dlDelta = dlAfter - dlBefore;

            if (totalDownloadCount > 0)
            {
                // 真有下载：必须 OnSceneDownloadProgress ≥ 1 次
                if (dlDelta < 1)
                {
                    LastError = $"P3 download branch FAIL: TotalDownloadCount={totalDownloadCount} but OnSceneDownloadProgress dispatch={dlDelta}";
                    Debug.LogError($"[S3-01][P3] FAIL — {LastError}");
                    P3Status = DownloadStatus.Fail;
                    return;
                }
                P3Status = DownloadStatus.Pass;
                Debug.Log($"[S3-01][P3] PASS — download branch covered, dlEvents={dlDelta}");
            }
            else
            {
                // 缓存命中 — ADVISORY skip (不算 fail)
                P3Status = DownloadStatus.SkipAdvisory;
                Debug.Log($"[S3-01][P3] SKIP-ADVISORY — TotalDownloadCount=0 (cache hit); download branch coverage requires cache clear in pre-test phase");
            }
        }

        private async UniTask TestP4_InvalidSceneFailLoud()
        {
            Debug.Log("[S3-01][P4] Invalid sceneName fail-loud (AC-4)...");

            // 先确保 chapter 3 unload + 状态 Idle
            if (_sceneManager.CurrentChapterSceneNameForTest != null)
            {
                await GameModule.Scene.UnloadAsync(_sceneManager.CurrentChapterSceneNameForTest);
                _sceneManager.ClearCurrentChapterSceneName();
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            }
            // P3 之后状态机可能在 Loading/Error；P4 尝试 invalid sceneName 也会进 Error
            // SceneManager.RecoverToIdle 仅处理 Error → Idle；非 Error 状态走 AdvanceStateForTest
            if (_sceneManager.CurrentState == SceneManagerState.Error)
            {
                _sceneManager.RecoverToIdle();
            }
            else
            {
                _sceneManager.AdvanceStateForTest(SceneManagerState.Idle);
            }

            int failedBefore = LoadFailedCount;
            int completeBefore = LoadCompleteCount;

            await _sceneManager.LoadChapterSceneAsync(CHAPTER_INVALID);

            // AC-4: OnSceneLoadFailed 派发 1 次 + chapter id 匹配
            if (LoadFailedCount != failedBefore + 1)
            {
                LastError = $"P4 OnSceneLoadFailed count mismatch: expected {failedBefore + 1}, got {LoadFailedCount}";
                Debug.LogError($"[S3-01][P4] FAIL — {LastError}");
                return;
            }

            if (_lastFailedChapterId != CHAPTER_INVALID)
            {
                LastError = $"P4 OnSceneLoadFailed chapterId mismatch: expected {CHAPTER_INVALID}, got {_lastFailedChapterId}";
                Debug.LogError($"[S3-01][P4] FAIL — {LastError}");
                return;
            }

            if (string.IsNullOrEmpty(_lastFailReason))
            {
                LastError = "P4 OnSceneLoadFailed reason empty — expected exception message from retry exhaust";
                Debug.LogError($"[S3-01][P4] FAIL — {LastError}");
                return;
            }

            // 不应该 OnSceneLoadComplete
            if (LoadCompleteCount != completeBefore)
            {
                LastError = $"P4 unexpected OnSceneLoadComplete dispatched during invalid scene path";
                Debug.LogError($"[S3-01][P4] FAIL — {LastError}");
                return;
            }

            // 状态机进 Error
            if (_sceneManager.CurrentState != SceneManagerState.Error)
            {
                LastError = $"P4 state machine not in Error: {_sceneManager.CurrentState}";
                Debug.LogError($"[S3-01][P4] FAIL — {LastError}");
                return;
            }

            P4Passed = true;
            Debug.Log($"[S3-01][P4] PASS — fail-loud chapterId={_lastFailedChapterId}, reason='{_lastFailReason}'");
        }

        private void PrintFinalReport()
        {
            bool allCorePassed = P1Passed && P2Passed && P4Passed;

            Debug.Log("[S3-01] ═══════════════════════════════════════════");
            Debug.Log("[S3-01]           验 证 报 告");
            Debug.Log("[S3-01] ═══════════════════════════════════════════");
            Debug.Log($"[S3-01] P1 SingleLoad        : {(P1Passed ? "PASS" : "FAIL")}");
            Debug.Log($"[S3-01] P2 Switch+AC10       : {(P2Passed ? "PASS" : "FAIL")}");
            Debug.Log($"[S3-01] P3 DownloadBranch    : {P3Status}");
            Debug.Log($"[S3-01] P4 InvalidSceneFail  : {(P4Passed ? "PASS" : "FAIL")}");
            Debug.Log($"[S3-01] P5 RetryMechanism    : ADVISORY (D1=[b] no stub seam)");
            Debug.Log($"[S3-01] Listeners            : DL={DownloadProgressCount} LP={LoadProgressCount} OK={LoadCompleteCount} FAIL={LoadFailedCount}");

            if (!string.IsNullOrEmpty(LastError))
            {
                Debug.Log($"[S3-01] 最后错误             : {LastError}");
            }

            Debug.Log($"[S3-01] 程序集               : {GetType().Assembly.GetName().Name}");
            Debug.Log("[S3-01] ═══════════════════════════════════════════");

            if (allCorePassed)
            {
                Debug.Log("[S3-01] CORE 4 PASSED (P5 ADVISORY)");
                StatusText = $"CORE PASSED (P3={P3Status}; P5 advisory)";
            }
            else
            {
                Debug.LogError("[S3-01] SOME FAILED — 见错误信息");
                StatusText = "SOME FAILED — see console";
            }
        }
    }
}
#endif
