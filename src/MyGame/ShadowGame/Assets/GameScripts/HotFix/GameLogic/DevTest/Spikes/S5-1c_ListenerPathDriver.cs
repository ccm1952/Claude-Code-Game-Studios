// 该文件由Cursor 自动生成
// S5-1c: ADR-009 production listener-path driver 接入 PlayMode spike — 验证 SceneManager.OnRequestSceneChange
//   handler 内部 DriveTransitionAsync(targetChapterId).Forget() 真接管 11-step（S5-1b F4 dev-only stub 已移除） +
//   5 R3 case (M1 双层模式复用 S5-1b precedent)：
//     P1 ListenerPathFirstBoot       — 反射读 GameApp._sceneManager 验 listener 自驱 11-step + 8 lifecycle event 顺序
//     P2 SameChapterDedupeIdleNoDriver — 派 OnRequestSceneChange(1) 同章 → OnSceneReady 立即触发 + 0 OnSceneTransitionBegin
//                                       关键 invariant: DriveTransitionAsync 严格放在 Idle 不同章 path 末尾不被 dedupe 误触
//     P3 UnknownChapterFailLoudLocal  — spike-local SceneManager（不 Init 避 listener 冲突）；
//                                       直调 BeginTransitionAsync(99) 走 LoadChapterSceneAsync ChapterData null fail-loud
//     P4 ErrorStateDrop_RecoverToIdle — production reflection；4 step round-trip:
//                                       (a) 派 99 → fail-loud Error → (b) 派 3 → Error guard drop →
//                                       (c) RecoverToIdle → state=Idle → (d) 派 1 → 同章 OnSceneReady;
//                                       全程 listener 观察 0 次新 OnSceneTransitionBegin（4 guard path 都不进 driver）
//     P5 ListenerSelfRemoval5Cycles   — production reflection Dispose + Init 5 cycle；每 cycle 后派 1 验 listener 仍触发；
//                                       V2-5 framework boundary probe 第 4 次累计（S5-03/S5-05/S5-06/S5-1b precedent）
//   M1 双层关键约束：
//     * P1/P2/P4/P5 反射拿 production GameApp._sceneManager；避免双 LoadSceneAsync 撞 YooAsset 锁
//     * P3 spike-local 不调 Init() 不订阅 OnRequestSceneChange，直接 BeginTransitionAsync 进 fail-loud 路径
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
    public class S51cSpike : IDevSpike
    {
        public string Id => "S5-1c";
        public string Name => "ADR-009 Production Listener-Path Driver (S5-1c)";

        public void Launch()
        {
            var go = new GameObject("S51c_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S51cRuntime>();
        }
    }

    public class S51cRuntime : MonoBehaviour
    {
        private S51cTester _tester;

        private void Awake()
        {
            // 关键时序：Awake 在 AddComponent 内同步执行（DevBootstrap.RunRequested() 调用栈内），
            // 早于 DevTestState 后续派 OnRequestSceneChange(1)；P1 listeners 在此 subscribe 即可
            // capture listener-path driver 同步 fire 的 OnSceneTransitionBegin。
            _tester = new S51cTester();
            _tester.SubscribeP1ListenersEarly();
            Log.Info("[S5-1c] Runtime Awake — P1 listeners pre-subscribed");
        }

        private void Start()
        {
            _tester.WriteResultJson();
            Log.Info($"[S5-1c] Runtime Start. Result JSON: {S51cTester.ResultFilePath}");

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

            float w = 820, h = 320;
            float x = (Screen.width - w) / 2f;
            float y = 20;

            GUI.Box(new Rect(x, y, w, h), string.Empty, boxStyle);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S5-1c ADR-009 Listener-Path Driver", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawRow(x + 20, lineY, w - 40, "P1 ListenerPathFirstBoot (反射 production；listener 自驱 11-step + 8 events)", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P2 SameChapterDedupeIdle (同章 → OnSceneReady + 0 OnSceneTransitionBegin)", _tester.P2Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P3 UnknownChapterFailLoud (spike-local；ChapterData null)", _tester.P3Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P4 ErrorStateDrop_RecoverToIdle (4-step round-trip)", _tester.P4Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P5 ListenerSelfRemoval (Dispose+Init × 5 cycles)", _tester.P5Passed, labelStyle);
            lineY += lineH + 10;

            var footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"AllPassed: {_tester.AllPassed}    JSON: {S51cTester.ResultFilePath}", footerStyle);
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
    /// P1/P2/P4/P5 反射拿 GameApp._sceneManager + per-event listener subscribe production senders；
    /// P3 spike-local 不调 Init 直接 BeginTransitionAsync 走 fail-loud 路径。
    /// </summary>
    public class S51cTester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S5-1c_Result.json");

        public bool? P1Passed { get; private set; }
        public bool? P2Passed { get; private set; }
        public bool? P3Passed { get; private set; }
        public bool? P4Passed { get; private set; }
        public bool? P5Passed { get; private set; }

        public bool AllPassed =>
            P1Passed == true && P2Passed == true && P3Passed == true &&
            P4Passed == true && P5Passed == true;

        public string OverallStatus { get; private set; } = "Running";

        private readonly List<string> _p1Events = new List<string>();
        private readonly List<string> _p2Events = new List<string>();
        private readonly List<string> _p3Events = new List<string>();
        private readonly List<string> _p4Events = new List<string>();
        private readonly List<string> _p5Events = new List<string>();
        private readonly Dictionary<string, string> _asserts = new Dictionary<string, string>();

        // P1 sync-subscribe state (持久化到 Tester field 让 RunP1Async 直接读，不需重新 subscribe)
        private int _p1TransitionBeginCount;
        private int _p1LoadCompleteCount;
        private (int chapterId, string bgmAsset) _p1LoadCompletePayload = (-999, "<unset>");
        private int _p1SceneReadyCount;
        private int _p1TransitionEndCount;
        private int _p1LoadProgressCount;
        private bool _p1ListenersSubscribed;
        private Action<int, int> _p1OnTB;
        private Action<string, float> _p1OnLP;
        private Action<int, string> _p1OnLC;
        private Action<int> _p1OnR;
        private Action<int> _p1OnTE;

        /// <summary>
        /// 由 S51cRuntime.Awake() 同步调用 — 在 DevTestState 派 OnRequestSceneChange(1) 之前
        /// subscribe P1 lifecycle event listeners，避免 listener-path driver 同步 fire OnSceneTransitionBegin
        /// 时 spike 还没 subscribe（S5-1b F4 stub 800ms delay 掩盖的 race，此处显式解决）。
        /// </summary>
        public void SubscribeP1ListenersEarly()
        {
            if (_p1ListenersSubscribed) return;

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

            _p1ListenersSubscribed = true;
        }

        private void UnsubscribeP1Listeners()
        {
            if (!_p1ListenersSubscribed) return;
            GameEvent.RemoveEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, _p1OnTB);
            GameEvent.RemoveEventListener<string, float>(ISceneEvent_Event.OnSceneLoadProgress, _p1OnLP);
            GameEvent.RemoveEventListener<int, string>(ISceneEvent_Event.OnSceneLoadComplete, _p1OnLC);
            GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneReady, _p1OnR);
            GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneTransitionEnd, _p1OnTE);
            _p1ListenersSubscribed = false;
        }

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
                Log.Info($"[S5-1c] Done. AllPassed={AllPassed}");
            }
            catch (Exception e)
            {
                OverallStatus = $"Crashed: {e.GetType().Name}";
                Log.Error($"[S5-1c] RunAllAsync 异常：{e}");
            }
            finally
            {
                WriteResultJson();
            }
        }

        // ------------------------------------------------------------------
        // P1 ListenerPathFirstBoot — 反射 production；listener 自驱 11-step + 8 lifecycle event 顺序
        // P1 listeners 已在 SubscribeP1ListenersEarly()（spike Awake，DevTestState dispatch 之前）subscribe，
        // 本方法仅负责等 production Idle 完成 + 计算 asserts + 在 finally unsubscribe。
        // ------------------------------------------------------------------
        private async UniTask RunP1Async()
        {
            Log.Info("[S5-1c] P1 ListenerPathFirstBoot 开始");
            var prodScene = GetProductionSceneManager();
            if (prodScene == null)
            {
                _asserts["P1.production_scene_manager_present"] = "FAIL: GameApp._sceneManager 反射拿 null";
                P1Passed = false;
                UnsubscribeP1Listeners();
                return;
            }

            try
            {
                var idleOk = await WaitForIdleAsync(prodScene, timeoutSec: 5.0);

                _asserts["P1.timeout"] = idleOk ? "PASS: state == Idle within 5s" : "FAIL: timeout";
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
            finally
            {
                UnsubscribeP1Listeners();
            }
        }

        // ------------------------------------------------------------------
        // P2 SameChapterDedupeIdleNoDriver — 派 OnRequestSceneChange(1) 同章 → OnSceneReady + 0 OnSceneTransitionBegin
        // ------------------------------------------------------------------
        private async UniTask RunP2Async()
        {
            Log.Info("[S5-1c] P2 SameChapterDedupeIdleNoDriver 开始");
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
                // 派同章请求；production handler line 326-333 Idle 同章 path → OnSceneReady 立即派
                // **关键 invariant**：DriveTransitionAsync.Forget() 严格放在 line 346 (Idle 不同章 path 末尾)，
                // 不被 line 326-333 dedupe 路径误触 → spike 应观察 0 OnSceneTransitionBegin
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
                await UniTask.Delay(TimeSpan.FromMilliseconds(300));

                _asserts["P2.OnSceneTransitionBegin_should_be_zero"] = transitionBeginCount == 0
                    ? "PASS: 0 (driver 不被 dedupe 路径误触)"
                    : $"FAIL: {transitionBeginCount} (期望 0 — DriveTransitionAsync 误进 Idle 同章 path)";
                _asserts["P2.OnSceneReady_should_be_one_or_more"] = sceneReadyCount >= 1
                    ? $"PASS: {sceneReadyCount}"
                    : $"FAIL: {sceneReadyCount} (期望 ≥1 — Idle 同章 path 应立即触发)";
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
        // P3 UnknownChapterFailLoudLocal — spike-local；BeginTransitionAsync(99) → ChapterData null fail-loud
        // ------------------------------------------------------------------
        private async UniTask RunP3Async()
        {
            Log.Info("[S5-1c] P3 UnknownChapterFailLoudLocal 开始");

            // spike-local SceneManager 不调 Init() — 不订阅 OnRequestSceneChange，直接 BeginTransitionAsync
            var local = new SceneManager();
            local.RegisterChapterDataProvider(id => id == 1
                ? new ChapterData(id: 1, sceneId: "Chapter_01_Approach", bgmAsset: "", emotionalWeight: 1.0f, overlayColor: "#3A3530")
                : null);
            local.RegisterFadeOverlay(new NoOpFadeOverlay());

            var loadFailedCount = 0;
            var loadFailedPayload = (chapterId: -999, error: "<unset>");

            Action<int, string> onLF = (id, err) => { loadFailedCount++; loadFailedPayload = (id, err); _p3Events.Add($"OnSceneLoadFailed({id},'{err}')"); };
            GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, onLF);

            try
            {
                Debug.Log("[S5-1c][P3] expected Debug.LogError below: ChapterData null for id=99");
                await local.BeginTransitionAsync(99);
                await UniTask.Delay(TimeSpan.FromMilliseconds(100));

                _asserts["P3.OnSceneLoadFailed_count"] = loadFailedCount >= 1
                    ? $"PASS: count={loadFailedCount}"
                    : $"FAIL: count={loadFailedCount}";
                _asserts["P3.OnSceneLoadFailed_chapterId"] = loadFailedPayload.chapterId == 99
                    ? $"PASS: chapterId=99"
                    : $"FAIL: chapterId={loadFailedPayload.chapterId}";
                _asserts["P3.CurrentState"] = local.CurrentState == SceneManagerState.Error
                    ? "PASS: Error"
                    : $"FAIL: {local.CurrentState}";
                _asserts["P3.CurrentLoadedChapterIdForTest"] = local.CurrentLoadedChapterIdForTest == GameLogic.SceneManager.NoChapterId
                    ? "PASS: NoChapterId（不污染）"
                    : $"FAIL: {local.CurrentLoadedChapterIdForTest}";

                P3Passed =
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
        // P4 ErrorStateDrop_RecoverToIdle — production 4-step round-trip; 0 新 OnSceneTransitionBegin 全程
        // ------------------------------------------------------------------
        private async UniTask RunP4Async()
        {
            Log.Info("[S5-1c] P4 ErrorStateDrop_RecoverToIdle 开始");
            var prodScene = GetProductionSceneManager();
            if (prodScene == null)
            {
                _asserts["P4.production_scene_manager_present"] = "FAIL";
                P4Passed = false;
                return;
            }
            if (prodScene.CurrentLoadedChapterIdForTest != 1 || prodScene.CurrentState != SceneManagerState.Idle)
            {
                _asserts["P4.precondition"] = $"FAIL: CurrentLoadedChapterId={prodScene.CurrentLoadedChapterIdForTest} state={prodScene.CurrentState}";
                P4Passed = false;
                return;
            }

            var transitionBeginCount = 0;
            var loadFailedCount = 0;
            var loadFailedPayload = (chapterId: -999, error: "<unset>");
            var sceneReadyCount = 0;

            Action<int, int> onTB = (from, to) => { transitionBeginCount++; _p4Events.Add($"OnSceneTransitionBegin({from},{to})"); };
            Action<int, string> onLF = (id, err) => { loadFailedCount++; loadFailedPayload = (id, err); _p4Events.Add($"OnSceneLoadFailed({id},'{err}')"); };
            Action<int> onR = id => { sceneReadyCount++; _p4Events.Add($"OnSceneReady({id})"); };

            GameEvent.AddEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, onTB);
            GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, onLF);
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneReady, onR);

            try
            {
                // step (a) 派 OnRequestSceneChange(99) → handler line 336 TryResolveOrFail → fail-loud
                // 注：production handler 走 TryResolveOrFail (line 259 Log.Warning，不是 Log.Error)；
                // 而 spike-local P3 直调 BeginTransitionAsync 走 LoadChapterSceneAsync (line 463 Log.Error)。
                // 两条 fail-loud 路径产生不同 reason 字符串 (TryResolveOrFail 'not found in TbChapter.' vs
                // LoadChapterSceneAsync 'ChapterData null for id=...')，asserts 仅校 OnSceneLoadFailed 收到 + chapterId.
                Debug.Log("[S5-1c][P4][a] expected Log.Warning below: Chapter ID 99 not found in TbChapter.");
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99);
                await UniTask.Delay(TimeSpan.FromMilliseconds(150));
                bool aOk = prodScene.CurrentState == SceneManagerState.Error && loadFailedCount >= 1 && loadFailedPayload.chapterId == 99;
                _asserts["P4.a_state_after_99"] = aOk
                    ? $"PASS: state=Error + OnSceneLoadFailed(99) count={loadFailedCount}"
                    : $"FAIL: state={prodScene.CurrentState} loadFailed={loadFailedCount} chapterId={loadFailedPayload.chapterId}";

                // step (b) 派 OnRequestSceneChange(3) → handler line 312 Error 分支 drop
                int beforeBCount = transitionBeginCount;
                int beforeBLoadFailed = loadFailedCount;
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(3);
                await UniTask.Delay(TimeSpan.FromMilliseconds(150));
                bool bOk =
                    transitionBeginCount == beforeBCount &&
                    loadFailedCount == beforeBLoadFailed &&
                    prodScene.CurrentState == SceneManagerState.Error;
                _asserts["P4.b_drop_in_error"] = bOk
                    ? "PASS: 0 新 OnSceneTransitionBegin + 0 新 OnSceneLoadFailed + state=Error 不变"
                    : $"FAIL: tbDelta={transitionBeginCount - beforeBCount} lfDelta={loadFailedCount - beforeBLoadFailed} state={prodScene.CurrentState}";

                // step (c) reflection 调 RecoverToIdle()
                prodScene.RecoverToIdle();
                bool cOk = prodScene.CurrentState == SceneManagerState.Idle;
                _asserts["P4.c_recover_to_idle"] = cOk
                    ? "PASS: state=Idle"
                    : $"FAIL: state={prodScene.CurrentState}";

                // step (d) 派 OnRequestSceneChange(1) → 同章 OnSceneReady (chapter 1 仍 loaded by P1)
                int beforeDReady = sceneReadyCount;
                int beforeDTB = transitionBeginCount;
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
                await UniTask.Delay(TimeSpan.FromMilliseconds(150));
                bool dOk =
                    sceneReadyCount > beforeDReady &&
                    transitionBeginCount == beforeDTB &&
                    prodScene.CurrentState == SceneManagerState.Idle;
                _asserts["P4.d_same_chapter_idle_after_recover"] = dOk
                    ? $"PASS: OnSceneReady delta={sceneReadyCount - beforeDReady} + 0 新 OnSceneTransitionBegin"
                    : $"FAIL: readyDelta={sceneReadyCount - beforeDReady} tbDelta={transitionBeginCount - beforeDTB} state={prodScene.CurrentState}";

                _asserts["P4.no_new_transition_begin_total"] = transitionBeginCount == 0
                    ? "PASS: 0 全程新 OnSceneTransitionBegin (4 guard path 都不进 driver)"
                    : $"FAIL: count={transitionBeginCount} (期望 0)";

                P4Passed = aOk && bOk && cOk && dOk && transitionBeginCount == 0;
            }
            finally
            {
                GameEvent.RemoveEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, onTB);
                GameEvent.RemoveEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, onLF);
                GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneReady, onR);
            }
        }

        // ------------------------------------------------------------------
        // P5 ListenerSelfRemoval5Cycles — production reflection Dispose+Init × 5；V2-5 第 4 次实战
        // ------------------------------------------------------------------
        private async UniTask RunP5Async()
        {
            Log.Info("[S5-1c] P5 ListenerSelfRemoval5Cycles 开始");
            var prodScene = GetProductionSceneManager();
            if (prodScene == null)
            {
                _asserts["P5.production_scene_manager_present"] = "FAIL";
                P5Passed = false;
                return;
            }

            var sceneReadyCountTotal = 0;
            Action<int> onR = id => { sceneReadyCountTotal++; _p5Events.Add($"cycle_OnSceneReady({id})"); };
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneReady, onR);

            try
            {
                bool allCyclesOk = true;
                int exceptionCount = 0;

                for (int cycle = 1; cycle <= 5; cycle++)
                {
                    int beforeCycleReady = sceneReadyCountTotal;
                    try
                    {
                        prodScene.Dispose();
                        prodScene.Init();
                    }
                    catch (Exception e)
                    {
                        exceptionCount++;
                        _p5Events.Add($"cycle{cycle}_dispose_init_exception:{e.GetType().Name}");
                        allCyclesOk = false;
                        continue;
                    }

                    GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
                    await UniTask.Delay(TimeSpan.FromMilliseconds(120));

                    bool cycleOk = sceneReadyCountTotal > beforeCycleReady;
                    _p5Events.Add($"cycle{cycle}_listener_triggered:{cycleOk} (delta={sceneReadyCountTotal - beforeCycleReady})");
                    if (!cycleOk) allCyclesOk = false;
                }

                _asserts["P5.5_cycle_listener_self_removal"] = allCyclesOk && exceptionCount == 0
                    ? $"PASS: 5/5 cycle listener triggered + 0 exception"
                    : $"FAIL: allCyclesOk={allCyclesOk} exceptionCount={exceptionCount}";
                _asserts["P5.total_OnSceneReady_count"] = sceneReadyCountTotal >= 5
                    ? $"PASS: count={sceneReadyCountTotal} (≥5 期望)"
                    : $"FAIL: count={sceneReadyCountTotal} (<5)";

                P5Passed = allCyclesOk && exceptionCount == 0 && sceneReadyCountTotal >= 5;
            }
            finally
            {
                GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneReady, onR);
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
                Log.Error("[S5-1c] 反射拿 GameApp._sceneManager 字段失败：FieldInfo == null");
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
            sb.Append($"  \"story_id\": \"S5-1c\",\n");
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
                Log.Error($"[S5-1c] WriteResultJson 失败：{e}");
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
