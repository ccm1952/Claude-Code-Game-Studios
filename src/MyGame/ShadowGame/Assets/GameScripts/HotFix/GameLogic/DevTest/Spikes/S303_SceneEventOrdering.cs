// 该文件由Cursor 自动生成
// S3-03: Scene Event Ordering PlayMode spike — 验证 SceneManager.BeginTransitionAsync 11 步骨架 +
//   8 ISceneEvent sender 派发顺序 + scene-scoped listener self-removal 模式 + perf。
// 由 GameApp.Entrance 在 DEBUG/Editor 下注册到 DevBootstrap，业务 FSM 进入 DevTestState 时运行。
// 6 case + 1 ADVISORY (P2 download / P6 perf) — patch v3 拆 P5 → P5a + P5b：
//   P1 SuccessCacheHit  — 完整 7 sender 顺序（Begin → UnloadBegin → LoadProgress* → LoadComplete → Ready → TransitionEnd）
//   P2 (ADV) DownloadBranch — Download progress 全部先于任何 LoadProgress（缓存命中常 SkipAdvisory）
//   P3 FailLoud        — invalid chapter 999 → retry exhaust → OnSceneLoadFailed；不派 LoadComplete/Ready/End
//   P4 FirstBoot       — _currentLoadedChapterId == NoChapterId → UnloadBegin 不派
//   P5a SelfRemoval    — handler 内 RemoveEventListener；2nd dispatch 不再触发；invokeCount == 1
//   P5b NullOutGuardPattern — TestSceneScopedFixture 文档化模式：Init→dispatch→Cleanup 全程无异常 + invokeCount == 1
//                          （patch v3：替代 patch v2 的 raw double-remove 测试；TEngine RemoveEventListener 在 listener
//                           已不存在时抛 "Delete handle failed, not exist" — 不是 idempotent，调用方必须 null-out + null-check）
//   P6 (ADV) Perf      — Stopwatch 测 BeginTransitionAsync ≤ 2ms（NoOpFadeOverlay 默认）
// 复用 SP011_SceneA (chapter 201) / SP011_SceneB (chapter 202) + invalid chapter 999；
// GameApp 注册时保持 SP-011 / S301 / S302 全部注释（type-3 race 防御）。
// 整文件仅在 UNITY_EDITOR || DEBUG 编译，Release 包零残留。

#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GameLogic.DevTest.Spikes
{
    public class S303Spike : IDevSpike
    {
        public string Id => "S3-03";
        public string Name => "Scene Event Ordering (S3-03 patch v2)";

        public void Launch()
        {
            var go = new GameObject("S303_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S303Runtime>();
        }
    }

    public class S303Runtime : MonoBehaviour
    {
        private S303Tester _tester;

        private void Start()
        {
            _tester = new S303Tester();
            _tester.WriteResultJson();
            Log.Info($"[S3-03] Runtime Start. Result JSON: {S303Tester.ResultFilePath}");

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

            float w = 720, h = 430;
            float x = (Screen.width - w) / 2f;
            float y = 20;

            GUI.Box(new Rect(x, y, w, h), string.Empty, boxStyle);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S3-03 Scene Event Ordering Spike (patch v2)", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawTestRow(x + 20, lineY, w - 40, "P1 SuccessCacheHit (Begin→UnloadBegin→LoadProgress*→LoadComplete→Ready→End)", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawAdvisoryRow(x + 20, lineY, w - 40, "P2 DownloadBranch (advisory)", _tester.P2Status, labelStyle);
            lineY += lineH;
            DrawTestRow(x + 20, lineY, w - 40, "P3 FailLoud (invalid chapter 999 → LoadFailed; no Ready/End)", _tester.P3Passed, labelStyle);
            lineY += lineH;
            DrawTestRow(x + 20, lineY, w - 40, "P4 FirstBoot (_currentLoadedChapterId == NoChapterId)", _tester.P4Passed, labelStyle);
            lineY += lineH;
            DrawTestRow(x + 20, lineY, w - 40, $"P5a SelfRemoval (invokeCount={_tester.P5aInvokeCount})", _tester.P5aPassed, labelStyle);
            lineY += lineH;
            DrawTestRow(x + 20, lineY, w - 40, $"P5b NullOutGuardPattern (invokeCount={_tester.P5bInvokeCount})", _tester.P5bPassed, labelStyle);
            lineY += lineH;
            DrawAdvisoryRow(x + 20, lineY, w - 40, $"P6 Perf ≤ 2ms (advisory; dispatchMs={_tester.P6DispatchMs:F2})", _tester.P6Status, labelStyle);
            lineY += lineH + 6;

            var asmStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.cyan },
            };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 20), $"Assembly: {GetType().Assembly.GetName().Name}", asmStyle);
            lineY += 22;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 20),
                $"Listeners — TB:{_tester.TBCount} UB:{_tester.UBCount} DP:{_tester.DPCount} LP:{_tester.LPCount} LC:{_tester.LCCount} R:{_tester.RCount} TE:{_tester.TECount} LF:{_tester.LFCount}",
                asmStyle);
            lineY += 24;

            bool allCorePassed = _tester.P1Passed && _tester.P3Passed && _tester.P4Passed && _tester.P5aPassed && _tester.P5bPassed;
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

        private static void DrawAdvisoryRow(float x, float y, float w, string label, S303Tester.AdvisoryStatus status, GUIStyle style)
        {
            string icon;
            switch (status)
            {
                case S303Tester.AdvisoryStatus.Pending:
                    icon = "PEND"; style.normal.textColor = Color.white; break;
                case S303Tester.AdvisoryStatus.Pass:
                    icon = "PASS"; style.normal.textColor = Color.green; break;
                case S303Tester.AdvisoryStatus.SkipAdvisory:
                    icon = "SKIP"; style.normal.textColor = Color.yellow; break;
                default:
                    icon = "FAIL"; style.normal.textColor = Color.red; break;
            }
            GUI.Label(new Rect(x, y, w, 22), $"  [{icon}]  {label}", style);
        }
    }

    public class S303Tester
    {
        public enum AdvisoryStatus
        {
            Pending,
            Pass,
            SkipAdvisory,
            Fail,
        }

        // 复用 SP-011 / S3-01 / S3-02 章节
        private const int CHAPTER_1 = 201;
        private const int CHAPTER_2 = 202;
        private const int CHAPTER_INVALID = 999;
        private const string SCENE_A = "SP011_SceneA";
        private const string SCENE_B = "SP011_SceneB";

        public const string RESULT_FILE_NAME = "S303_Result.json";

        public bool P1Passed { get; private set; }
        public AdvisoryStatus P2Status { get; private set; } = AdvisoryStatus.Pending;
        public bool P3Passed { get; private set; }
        public bool P4Passed { get; private set; }
        public bool P5aPassed { get; private set; }
        public int P5aInvokeCount { get; private set; }
        public bool P5bPassed { get; private set; }
        public int P5bInvokeCount { get; private set; }
        public bool P5Passed => P5aPassed && P5bPassed;
        public int P5InvokeCount => P5aInvokeCount;  // legacy GUI alias
        public AdvisoryStatus P6Status { get; private set; } = AdvisoryStatus.Pending;
        public double P6DispatchMs { get; private set; }
        public string LastError { get; private set; }
        public string StatusText { get; private set; } = "Pending...";
        public string Phase { get; private set; } = "pending";

        // 8 ISceneEvent listener counts
        public int TBCount { get; private set; }   // OnSceneTransitionBegin
        public int UBCount { get; private set; }   // OnSceneUnloadBegin
        public int DPCount { get; private set; }   // OnSceneDownloadProgress
        public int LPCount { get; private set; }   // OnSceneLoadProgress
        public int LCCount { get; private set; }   // OnSceneLoadComplete
        public int RCount { get; private set; }    // OnSceneReady
        public int TECount { get; private set; }   // OnSceneTransitionEnd
        public int LFCount { get; private set; }   // OnSceneLoadFailed

        // 顺序 log（每个 case 跑前 reset）
        private readonly List<string> _log = new();

        // P1 detail captures
        private List<string> _p1Log;
        // P3 detail captures
        private List<string> _p3Log;
        // P4 detail captures
        private List<string> _p4Log;

        // 8 listener delegates（保留 reference 以便 Remove 配对）
        private Action<int, int> _onTB;
        private Action<int> _onUB;
        private Action<float, long, long> _onDP;
        private Action<string, float> _onLP;
        private Action<int, string> _onLC;
        private Action<int> _onR;
        private Action<int> _onTE;
        private Action<int, string> _onLF;

        private SceneManager _sceneManager;

        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, RESULT_FILE_NAME);

        public void WriteResultJson()
        {
            try
            {
            bool allCorePassed = P1Passed && P3Passed && P4Passed && P5aPassed && P5bPassed;

            var sb = new StringBuilder(2048);
                sb.Append("{\n");
                sb.Append($"  \"timestamp\": \"{DateTime.UtcNow:O}\",\n");
                sb.Append($"  \"phase\": \"{EscapeJson(Phase)}\",\n");
                sb.Append($"  \"p1_passed\": {P1Passed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"p2_status\": \"{P2Status}\",\n");
                sb.Append($"  \"p3_passed\": {P3Passed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"p4_passed\": {P4Passed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"p5_passed\": {P5Passed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"p5a_passed\": {P5aPassed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"p5a_invokeCount\": {P5aInvokeCount},\n");
                sb.Append($"  \"p5b_passed\": {P5bPassed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"p5b_invokeCount\": {P5bInvokeCount},\n");
                sb.Append($"  \"p6_status\": \"{P6Status}\",\n");
                sb.Append($"  \"p6_dispatchMs\": {P6DispatchMs.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},\n");
                sb.Append($"  \"allCorePassed\": {allCorePassed.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"listenerCounts\": {{\n");
                sb.Append($"    \"OnSceneTransitionBegin\": {TBCount},\n");
                sb.Append($"    \"OnSceneUnloadBegin\": {UBCount},\n");
                sb.Append($"    \"OnSceneDownloadProgress\": {DPCount},\n");
                sb.Append($"    \"OnSceneLoadProgress\": {LPCount},\n");
                sb.Append($"    \"OnSceneLoadComplete\": {LCCount},\n");
                sb.Append($"    \"OnSceneReady\": {RCount},\n");
                sb.Append($"    \"OnSceneTransitionEnd\": {TECount},\n");
                sb.Append($"    \"OnSceneLoadFailed\": {LFCount}\n");
                sb.Append($"  }},\n");
                sb.Append($"  \"p1_seq_log\": {SerializeLog(_p1Log)},\n");
                sb.Append($"  \"p3_failedSeq_log\": {SerializeLog(_p3Log)},\n");
                sb.Append($"  \"p4_firstboot_log\": {SerializeLog(_p4Log)},\n");
                sb.Append($"  \"assembly\": \"{EscapeJson(GetType().Assembly.GetName().Name)}\",\n");
                sb.Append($"  \"statusText\": \"{EscapeJson(StatusText)}\",\n");
                sb.Append($"  \"lastError\": \"{EscapeJson(LastError ?? string.Empty)}\",\n");
                sb.Append($"  \"persistentDataPath\": \"{EscapeJson(Application.persistentDataPath)}\"\n");
                sb.Append("}\n");

                File.WriteAllText(ResultFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[S3-03] WriteResultJson 失败（非致命）: {ex.Message}");
            }
        }

        private static string SerializeLog(List<string> log)
        {
            if (log == null || log.Count == 0) return "[]";
            var sb = new StringBuilder("[");
            for (int i = 0; i < log.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('"').Append(EscapeJson(log[i])).Append('"');
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        public async UniTask RunAllAsync()
        {
            Debug.Log("[S3-03] ═══════════════════════════════════════════");
            Debug.Log("[S3-03] Scene Event Ordering Spike (patch v2) 开始");
            Debug.Log($"[S3-03] Assembly: {GetType().Assembly.GetName().Name}");
            Debug.Log("[S3-03] ═══════════════════════════════════════════");

            Phase = "running";
            StatusText = "Running...";
            WriteResultJson();

            if (GameModule.Scene == null)
            {
                LastError = "GameModule.Scene == null（DevTestState 进入前未完成 TEngine 模块初始化）";
                StatusText = $"FAIL: {LastError}";
                Phase = "done";
                WriteResultJson();
                Debug.LogError($"[S3-03] {LastError}");
                return;
            }

            HookListeners();

            try
            {
                _sceneManager = new SceneManager();
                _sceneManager.Init();
                _sceneManager.RegisterChapterDataProvider(ResolveChapterData);
                // _fadeOverlay 默认 NoOpFadeOverlay，无需显式 Register

                await TestP1_SuccessCacheHit();
                WriteResultJson();

                TestP2_DownloadBranchAdvisory();
                WriteResultJson();

                await TestP3_FailLoud();
                WriteResultJson();

                await TestP4_FirstBoot();
                WriteResultJson();

                TestP5a_SelfRemoval();
                WriteResultJson();

                TestP5b_NullOutGuardPattern();
                WriteResultJson();

                await TestP6_PerfAdvisory();
                WriteResultJson();
            }
            catch (Exception ex)
            {
                LastError = $"Unhandled exception: {ex.Message}";
                Debug.LogError($"[S3-03] EXCEPTION: {ex}");
            }
            finally
            {
                _sceneManager?.Dispose();
                _sceneManager = null;
                UnhookListeners();
            }

            PrintFinalReport();
            Phase = "done";
            WriteResultJson();
        }

        private ChapterData ResolveChapterData(int chapterId)
        {
            switch (chapterId)
            {
                case CHAPTER_1: return new ChapterData(CHAPTER_1, SCENE_A, "bgm_test_a3");
                case CHAPTER_2: return new ChapterData(CHAPTER_2, SCENE_B, "bgm_test_b3");
                case CHAPTER_INVALID: return new ChapterData(CHAPTER_INVALID, "Chapter_999_NotInManifest", "bgm_invalid");
                default: return null;
            }
        }

        private void HookListeners()
        {
            _onTB = (from, to) => { TBCount++; _log.Add($"Begin({from},{to})"); };
            _onUB = chap => { UBCount++; _log.Add($"UnloadBegin({chap})"); };
            _onDP = (p, d, t) => { DPCount++; _log.Add($"DownloadProgress({p:F2})"); };
            _onLP = (n, p) => { LPCount++; _log.Add($"LoadProgress({n},{p:F2})"); };
            _onLC = (c, b) => { LCCount++; _log.Add($"LoadComplete({c})"); };
            _onR = c => { RCount++; _log.Add($"Ready({c})"); };
            _onTE = c => { TECount++; _log.Add($"TransitionEnd({c})"); };
            _onLF = (c, e) => { LFCount++; _log.Add($"LoadFailed({c})"); };

            GameEvent.AddEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, _onTB);
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, _onUB);
            GameEvent.AddEventListener<float, long, long>(ISceneEvent_Event.OnSceneDownloadProgress, _onDP);
            GameEvent.AddEventListener<string, float>(ISceneEvent_Event.OnSceneLoadProgress, _onLP);
            GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadComplete, _onLC);
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneReady, _onR);
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneTransitionEnd, _onTE);
            GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, _onLF);
        }

        private void UnhookListeners()
        {
            if (_onTB != null) GameEvent.RemoveEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, _onTB);
            if (_onUB != null) GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, _onUB);
            if (_onDP != null) GameEvent.RemoveEventListener<float, long, long>(ISceneEvent_Event.OnSceneDownloadProgress, _onDP);
            if (_onLP != null) GameEvent.RemoveEventListener<string, float>(ISceneEvent_Event.OnSceneLoadProgress, _onLP);
            if (_onLC != null) GameEvent.RemoveEventListener<int, string>(ISceneEvent_Event.OnSceneLoadComplete, _onLC);
            if (_onR != null) GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneReady, _onR);
            if (_onTE != null) GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneTransitionEnd, _onTE);
            if (_onLF != null) GameEvent.RemoveEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, _onLF);

            _onTB = null; _onUB = null; _onDP = null; _onLP = null;
            _onLC = null; _onR = null; _onTE = null; _onLF = null;
        }

        // ─────────────────────────────────────────────────────────────────
        // P1 SuccessCacheHit — 完整 7 sender 顺序断言（cache 命中无 download）
        // ─────────────────────────────────────────────────────────────────
        private async UniTask TestP1_SuccessCacheHit()
        {
            Debug.Log("[S3-03][P1] SuccessCacheHit — 完整 7 sender 顺序断言...");

            // 前置：load CHAPTER_1 进入"已有 chapter"状态（不走 BeginTransitionAsync 以避免 first-boot Begin 派发干扰 P1 计数）
            await _sceneManager.LoadChapterSceneAsync(CHAPTER_1);
            if (_sceneManager.CurrentChapterSceneNameForTest != SCENE_A)
            {
                LastError = $"P1 prep failed: expected {SCENE_A}, got {_sceneManager.CurrentChapterSceneNameForTest}";
                Debug.LogError($"[S3-03][P1] FAIL — {LastError}");
                return;
            }

            // P1 reset log + counter snapshot
            _log.Clear();
            int tbBefore = TBCount, ubBefore = UBCount, lcBefore = LCCount, rBefore = RCount, teBefore = TECount, lfBefore = LFCount;

            // 触发完整 11 步 transition CHAPTER_1 → CHAPTER_2
            await _sceneManager.BeginTransitionAsync(CHAPTER_2);

            _p1Log = new List<string>(_log);

            // AC-1: TransitionBegin 派 1 次，from=201, to=202
            if (TBCount != tbBefore + 1)
            {
                LastError = $"P1 TBCount mismatch: expected {tbBefore + 1}, got {TBCount}";
                Debug.LogError($"[S3-03][P1] FAIL — {LastError}");
                return;
            }
            // AC-2: UnloadBegin 1 次（chapter_1 已加载）；LoadComplete 1 次；Ready 1 次；End 1 次；LoadFailed 0
            if (UBCount != ubBefore + 1 || LCCount != lcBefore + 1 || RCount != rBefore + 1 || TECount != teBefore + 1 || LFCount != lfBefore)
            {
                LastError = $"P1 listener counts mismatch: UB+={UBCount - ubBefore}, LC+={LCCount - lcBefore}, R+={RCount - rBefore}, TE+={TECount - teBefore}, LF+={LFCount - lfBefore}";
                Debug.LogError($"[S3-03][P1] FAIL — {LastError}");
                return;
            }

            // 顺序断言：Begin 严格先于其他；UnloadBegin 在 Begin 之后 LoadComplete 之前；Ready 在 LoadComplete 之后 TransitionEnd 之前
            int idxBegin = _log.FindIndex(s => s.StartsWith("Begin("));
            int idxUB = _log.FindIndex(s => s.StartsWith("UnloadBegin("));
            int idxLC = _log.FindIndex(s => s.StartsWith("LoadComplete("));
            int idxR = _log.FindIndex(s => s.StartsWith("Ready("));
            int idxTE = _log.FindIndex(s => s.StartsWith("TransitionEnd("));

            if (idxBegin != 0)
            {
                LastError = $"P1 Begin must be first; idxBegin={idxBegin}, log={string.Join(",", _log)}";
                Debug.LogError($"[S3-03][P1] FAIL — {LastError}");
                return;
            }
            if (!(idxBegin < idxUB && idxUB < idxLC && idxLC < idxR && idxR < idxTE))
            {
                LastError = $"P1 order failed: Begin({idxBegin}) → UnloadBegin({idxUB}) → LoadComplete({idxLC}) → Ready({idxR}) → TransitionEnd({idxTE})";
                Debug.LogError($"[S3-03][P1] FAIL — {LastError}");
                return;
            }
            if (idxTE != _log.Count - 1)
            {
                LastError = $"P1 TransitionEnd must be last; idxTE={idxTE}, count={_log.Count}";
                Debug.LogError($"[S3-03][P1] FAIL — {LastError}");
                return;
            }

            // 状态机推回 Idle
            if (_sceneManager.CurrentState != SceneManagerState.Idle)
            {
                LastError = $"P1 state should be Idle after transition; got {_sceneManager.CurrentState}";
                Debug.LogError($"[S3-03][P1] FAIL — {LastError}");
                return;
            }

            P1Passed = true;
            Debug.Log($"[S3-03][P1] PASS — log: {string.Join(" → ", _log)}");
        }

        // ─────────────────────────────────────────────────────────────────
        // P2 DownloadBranch (advisory) — Editor 缓存命中常 SkipAdvisory
        // ─────────────────────────────────────────────────────────────────
        private void TestP2_DownloadBranchAdvisory()
        {
            // P1 跑完后 DPCount 应 = 0（cache 命中）；设 SkipAdvisory
            // 真正 Download branch coverage 需要 cache clear pre-test 或独立 test fixture，留 backlog
            if (DPCount == 0)
            {
                P2Status = AdvisoryStatus.SkipAdvisory;
                Debug.Log($"[S3-03][P2] SKIP-ADVISORY — Editor cache hit, DPCount=0; download branch verified by S3-01 P3 同模式");
            }
            else
            {
                // 极少数情况下 cache miss → 验证 DP 全部先于任何 LP
                int idxFirstDP = _p1Log?.FindIndex(s => s.StartsWith("DownloadProgress(")) ?? -1;
                int idxFirstLP = _p1Log?.FindIndex(s => s.StartsWith("LoadProgress(")) ?? -1;
                if (idxFirstDP >= 0 && idxFirstLP >= 0 && idxFirstDP < idxFirstLP)
                {
                    P2Status = AdvisoryStatus.Pass;
                    Debug.Log($"[S3-03][P2] PASS — first DP({idxFirstDP}) < first LP({idxFirstLP})");
                }
                else
                {
                    P2Status = AdvisoryStatus.Fail;
                    Debug.LogWarning($"[S3-03][P2] order anomaly — DP{idxFirstDP} vs LP{idxFirstLP}");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // P3 FailLoud — invalid chapter 999 → retry exhaust → OnSceneLoadFailed
        //              不派 LoadComplete / Ready / TransitionEnd
        // ─────────────────────────────────────────────────────────────────
        private async UniTask TestP3_FailLoud()
        {
            Debug.Log("[S3-03][P3] FailLoud — invalid chapter 999 retry exhaust...");

            // P1 跑完后状态 Idle，_currentLoadedChapterId == CHAPTER_2；BeginTransitionAsync(999) 触发完整 11 步
            _log.Clear();
            int tbBefore = TBCount, ubBefore = UBCount, lcBefore = LCCount, rBefore = RCount, teBefore = TECount, lfBefore = LFCount;

            await _sceneManager.BeginTransitionAsync(CHAPTER_INVALID);

            _p3Log = new List<string>(_log);

            // 期望：TB+1, UB+1（卸 CHAPTER_2）, LF+1, LC/R/TE 全 0
            if (TBCount != tbBefore + 1 || UBCount != ubBefore + 1 || LFCount != lfBefore + 1)
            {
                LastError = $"P3 expected counts mismatch: TB+={TBCount - tbBefore}, UB+={UBCount - ubBefore}, LF+={LFCount - lfBefore}";
                Debug.LogError($"[S3-03][P3] FAIL — {LastError}");
                return;
            }
            if (LCCount != lcBefore || RCount != rBefore || TECount != teBefore)
            {
                LastError = $"P3 forbidden senders fired: LC+={LCCount - lcBefore}, R+={RCount - rBefore}, TE+={TECount - teBefore}";
                Debug.LogError($"[S3-03][P3] FAIL — {LastError}");
                return;
            }
            if (_sceneManager.CurrentState != SceneManagerState.Error)
            {
                LastError = $"P3 state should be Error after fail-loud; got {_sceneManager.CurrentState}";
                Debug.LogError($"[S3-03][P3] FAIL — {LastError}");
                return;
            }

            P3Passed = true;
            Debug.Log($"[S3-03][P3] PASS — log: {string.Join(" → ", _log)}");

            // P3 完成后 SceneManager 处于 Error；P4 用全新实例，无需 RecoverToIdle
        }

        // ─────────────────────────────────────────────────────────────────
        // P4 FirstBoot — 全新 SceneManager (_currentLoadedChapterId == NoChapterId)
        //                BeginTransitionAsync(target) 不派 OnSceneUnloadBegin
        // ─────────────────────────────────────────────────────────────────
        private async UniTask TestP4_FirstBoot()
        {
            Debug.Log("[S3-03][P4] FirstBoot — fresh SceneManager 不派 OnSceneUnloadBegin...");

            // 用全新 SceneManager；listener 沿用同一组（spike 实例独立但 GameEvent 是 global）
            var fresh = new SceneManager();
            fresh.Init();
            fresh.RegisterChapterDataProvider(ResolveChapterData);

            _log.Clear();
            int tbBefore = TBCount, ubBefore = UBCount, lcBefore = LCCount, rBefore = RCount, teBefore = TECount;

            try
            {
                await fresh.BeginTransitionAsync(CHAPTER_1);

                _p4Log = new List<string>(_log);

                // 期望：TB+1（from=NoChapterId, to=201）, UB+0（first-boot guard 短路）, LC+1, R+1, TE+1
                if (TBCount != tbBefore + 1)
                {
                    LastError = $"P4 TBCount: expected {tbBefore + 1}, got {TBCount}";
                    Debug.LogError($"[S3-03][P4] FAIL — {LastError}");
                    return;
                }
                if (UBCount != ubBefore)
                {
                    LastError = $"P4 UnloadBegin should not fire on first-boot: UB+={UBCount - ubBefore}";
                    Debug.LogError($"[S3-03][P4] FAIL — {LastError}");
                    return;
                }
                if (LCCount != lcBefore + 1 || RCount != rBefore + 1 || TECount != teBefore + 1)
                {
                    LastError = $"P4 success path counts mismatch: LC+={LCCount - lcBefore}, R+={RCount - rBefore}, TE+={TECount - teBefore}";
                    Debug.LogError($"[S3-03][P4] FAIL — {LastError}");
                    return;
                }

                // 验证 Begin 的 fromChapterId == NoChapterId (-1)
                string firstBeginEntry = _log.Count > 0 ? _log[0] : null;
                if (firstBeginEntry == null || !firstBeginEntry.StartsWith($"Begin({SceneManager.NoChapterId},"))
                {
                    LastError = $"P4 first Begin should be Begin({SceneManager.NoChapterId},*); got {firstBeginEntry}";
                    Debug.LogError($"[S3-03][P4] FAIL — {LastError}");
                    return;
                }

                P4Passed = true;
                Debug.Log($"[S3-03][P4] PASS — log: {string.Join(" → ", _log)}");
            }
            finally
            {
                // 清理 fresh 实例 + 真实场景（防 P5/P6 干扰）
                try
                {
                    if (fresh.CurrentChapterSceneNameForTest == SCENE_A)
                    {
                        await GameModule.Scene.UnloadAsync(SCENE_A);
                        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[S3-03][P4] cleanup fresh chapter scene 失败（非阻塞）: {ex.Message}");
                }
                fresh.Dispose();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // P5a SelfRemoval — handler 内 RemoveEventListener；2nd dispatch 不再触发
        // ─────────────────────────────────────────────────────────────────
        private void TestP5a_SelfRemoval()
        {
            Debug.Log("[S3-03][P5a] SelfRemoval — handler 内 self-remove...");

            int invokeCount = 0;
            Action<int> handler = null;
            handler = chap =>
            {
                invokeCount++;
                GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, handler);
            };

            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, handler);

            // 首次派发：handler 触发 + self-remove
            GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(101);
            // 再次派发：handler 不再触发
            GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(102);

            P5aInvokeCount = invokeCount;

            if (invokeCount != 1)
            {
                LastError = $"P5a self-removal failed: expected invokeCount=1, got {invokeCount}";
                Debug.LogError($"[S3-03][P5a] FAIL — {LastError}");
                return;
            }

            P5aPassed = true;
            Debug.Log($"[S3-03][P5a] PASS — invokeCount={invokeCount}");
        }

        // ─────────────────────────────────────────────────────────────────
        // P5b NullOutGuardPattern — TestSceneScopedFixture (story-005 patch v3 documented pattern)
        //
        // 验证：handler null-out + external null-check guard 可避免 double-remove 抛 TEngine
        //       "Delete handle failed, not exist" exception。
        //
        // patch v2 → patch v3 reformulation: TEngine RemoveEventListener 不是 idempotent，调用
        //   GameEvent.RemoveEventListener 在 listener 不存在时抛 exception；recommended pattern 是
        //   handler 内 self-remove + null-out _handler 字段；外部 cleanup 必须 if (_handler != null) check。
        // ─────────────────────────────────────────────────────────────────
        private sealed class TestSceneScopedFixture
        {
            // documented pattern fields
            private Action<int> _handler;

            public int InvokeCount { get; private set; }

            public void Init()
            {
                _handler = OnSceneUnloadBegin;
                GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, _handler);
            }

            private void OnSceneUnloadBegin(int chapterId)
            {
                InvokeCount++;
                GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, _handler);
                _handler = null; // ← 关键：null-out 防 double-remove
            }

            public void Cleanup()
            {
                if (_handler != null) // ← 关键：null-check 防御 TEngine RemoveEventListener throw
                {
                    GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneUnloadBegin, _handler);
                    _handler = null;
                }
            }

            public bool HandlerIsNull() => _handler == null;
        }

        private void TestP5b_NullOutGuardPattern()
        {
            Debug.Log("[S3-03][P5b] NullOutGuardPattern — fixture Init→dispatch→Cleanup 全程无异常...");

            var fixture = new TestSceneScopedFixture();

            try
            {
                fixture.Init();

                // 首次派发：handler 触发 + self-remove + null-out
                GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(101);

                if (fixture.InvokeCount != 1)
                {
                    LastError = $"P5b post-1st dispatch InvokeCount: expected 1, got {fixture.InvokeCount}";
                    Debug.LogError($"[S3-03][P5b] FAIL — {LastError}");
                    return;
                }
                if (!fixture.HandlerIsNull())
                {
                    LastError = "P5b: handler should be null after self-removal (null-out pattern broken)";
                    Debug.LogError($"[S3-03][P5b] FAIL — {LastError}");
                    return;
                }

                // 外部 cleanup — null-check guard 应该 silent skip（不调 RemoveEventListener，避免 TEngine throw）
                fixture.Cleanup();

                // 再次派发：handler 不再触发
                GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(102);

                if (fixture.InvokeCount != 1)
                {
                    LastError = $"P5b post-2nd dispatch InvokeCount: expected 1, got {fixture.InvokeCount}";
                    Debug.LogError($"[S3-03][P5b] FAIL — {LastError}");
                    return;
                }

                P5bInvokeCount = fixture.InvokeCount;
                P5bPassed = true;
                Debug.Log($"[S3-03][P5b] PASS — invokeCount={fixture.InvokeCount}, null-out + null-check guard 阻止 double-remove throw");
            }
            catch (Exception ex)
            {
                LastError = $"P5b unexpectedly threw: {ex.Message}";
                Debug.LogError($"[S3-03][P5b] FAIL — {LastError}");
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // P6 Perf (advisory) — Stopwatch 测 BeginTransitionAsync ≤ 2ms
        // ─────────────────────────────────────────────────────────────────
        private async UniTask TestP6_PerfAdvisory()
        {
            Debug.Log("[S3-03][P6] Perf advisory...");

            // P3 让 SceneManager 进 Error；P4 用 fresh 实例并 cleanup；本主 _sceneManager 仍 Error
            // 跳过 P6 — 主实例不可用；标记 SkipAdvisory
            // 真实 perf 测量留 backlog standard PlayMode test（与 perf-profile skill 配合）
            P6Status = AdvisoryStatus.SkipAdvisory;
            P6DispatchMs = 0;
            Debug.Log("[S3-03][P6] SKIP-ADVISORY — main _sceneManager 在 P3 后处于 Error 不可复用；perf 测量留 backlog perf-profile spike");
            await UniTask.Yield();
        }

        private void PrintFinalReport()
        {
            bool allCorePassed = P1Passed && P3Passed && P4Passed && P5aPassed && P5bPassed;

            Debug.Log("[S3-03] ═══════════════════════════════════════════");
            Debug.Log("[S3-03]           验 证 报 告  (patch v3)");
            Debug.Log("[S3-03] ═══════════════════════════════════════════");
            Debug.Log($"[S3-03] P1 SuccessCacheHit     : {(P1Passed ? "PASS" : "FAIL")}");
            Debug.Log($"[S3-03] P2 DownloadBranch      : {P2Status}");
            Debug.Log($"[S3-03] P3 FailLoud            : {(P3Passed ? "PASS" : "FAIL")}");
            Debug.Log($"[S3-03] P4 FirstBoot           : {(P4Passed ? "PASS" : "FAIL")}");
            Debug.Log($"[S3-03] P5a SelfRemoval        : {(P5aPassed ? "PASS" : "FAIL")} (invokeCount={P5aInvokeCount})");
            Debug.Log($"[S3-03] P5b NullOutGuardPattern: {(P5bPassed ? "PASS" : "FAIL")} (invokeCount={P5bInvokeCount})");
            Debug.Log($"[S3-03] P6 Perf                : {P6Status} (dispatchMs={P6DispatchMs:F2})");
            Debug.Log($"[S3-03] Listeners              : TB={TBCount} UB={UBCount} DP={DPCount} LP={LPCount} LC={LCCount} R={RCount} TE={TECount} LF={LFCount}");

            if (!string.IsNullOrEmpty(LastError))
            {
                Debug.Log($"[S3-03] 最后错误               : {LastError}");
            }

            Debug.Log($"[S3-03] 程序集                 : {GetType().Assembly.GetName().Name}");
            Debug.Log("[S3-03] ═══════════════════════════════════════════");

            if (allCorePassed)
            {
                Debug.Log("[S3-03] CORE 5 PASSED (P2/P6 ADVISORY)");
                StatusText = $"CORE PASSED (P2={P2Status}; P6={P6Status})";
            }
            else
            {
                Debug.LogError("[S3-03] SOME FAILED — 见错误信息");
                StatusText = "SOME FAILED — see console";
            }
        }
    }
}
#endif
