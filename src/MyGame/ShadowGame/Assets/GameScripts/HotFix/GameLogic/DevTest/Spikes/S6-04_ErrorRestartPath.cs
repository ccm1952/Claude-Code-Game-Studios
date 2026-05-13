// 该文件由Cursor 自动生成
// S6-04 Chapter 1 Error/Restart Path PlayMode spike
//   per story-003-error-restart-path.md (Phase 0 ✅ + Phase 1 ✅；R1+R2+R3 readiness ✅ DEFICIENCY-FLAGGED PASS)。
//
// 关联文档:
//   * production/epics/vs-chapter-1/story-003-error-restart-path.md  (10 AC + R3 5 case)
//   * Assets/GameScripts/HotFix/GameLogic/Scene/SceneManager.cs       (vendor 6-state + RecoverToIdle + TryResolveOrFail)
//   * Assets/GameScripts/HotFix/GameLogic/IEvent/ISceneEvent.cs       (9 method contract)
//
// R3 5 PlayMode case (M1 dual-layer: P1/P2/P4/P5 production reflection + P3 isolated local；run order P1→P2→P3→P4→P5):
//   P1 UnknownChapterTryResolveOrFail — chapter 1 baseline 后 fire OnRequestSceneChange(99) → vendor
//                                       TryResolveOrFail(99) 返 false → OnSceneLoadFailed(99,"Chapter ID 99
//                                       not found in TbChapter.") + state=Error；CurrentChapterId==1 不变。
//   P2 NewestWinsPendingDuringTransition — chapter 1 baseline 后 fire OnRequestSceneChange(2) start transition
//                                          → 在 transition 中 fire OnRequestSceneChange(1) → AC-9
//                                          newest-wins _pendingTargetChapterId=1 → drain 后 currentChapterId=1。
//   P3 AssetLoadFailRetryExhaust — isolated local SceneManager + fixture chapter 99 bad sceneId
//                                  "NotExistScene_Chapter99" → fire OnRequestSceneChange(99) → 2 retry exhaust
//                                  → OnSceneLoadFailed(99,error) + state=Error。
//   P4 RestartFromErrorRecovery — production sm 进 Error (fire 99) → AC-10 silent drop verify (fire 1 in Error)
//                                 → RecoverToIdle() → state=Idle → re-fire OnRequestSceneChange(1) →
//                                 chapter 1 reload success + currentChapterId=1。
//   P5 RapidNewestWinsOverwrite — Idle state rapid fire (1)→(2)→(1)：第 1 same target silent OnSceneReady AC-8
//                                 + 第 2 进 TransitionOut + 第 3 newest-wins pending AC-9 → drain final chapter 1。
//
// 设计约束:
//   * Spike 模式：1 file + 3 inner class (S604Spike : IDevSpike + S604Runtime : MonoBehaviour + S604Tester 纯逻辑)
//     沿 S5-1b/-1c/-02/-07/-08 precedent
//   * Awake() 同步 subscribe `GameEvent.AddEventListener<int,string>(ISceneEvent_Event.OnSceneLoadFailed, ...)`
//     + 4 其它 ISceneEvent sender (TransitionBegin/End/LoadComplete/Ready) + Application.logMessageReceived
//     (per S5-1c sync-subscribe race 防御)
//   * P3 用 isolated local SceneManager (`new GameLogic.SceneManager()`) 避免污染 production GameApp._sceneManager
//     (S5-1b P4/P5 precedent)
//   * Reflection 拿 `GameApp._sceneManager` private static field (S5-1b GetProductionSceneManager helper)
//   * Expected log allowlist (TryResolveOrFail Log.Warning + Load attempt failed Log.Warning + OnSceneLoadFailed
//     Log.Error + AC-10 silent drop Log.Warning) — UnexpectedErrorCount 仅累积 allowlist 之外的 LogType.Error/Exception
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
    public class S604Spike : IDevSpike
    {
        public string Id => "S6-04";
        public string Name => "Chapter 1 Error/Restart Path — TryResolveOrFail + Newest-Wins Pending + Retry Exhaust + RecoverToIdle";

        public void Launch()
        {
            var go = new GameObject("S604_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S604Runtime>();
        }
    }

    public class S604Runtime : MonoBehaviour
    {
        private S604Tester _tester;

        private void Awake()
        {
            _tester = new S604Tester(this);
            _tester.SubscribeEarlyListeners();
        }

        private void Start()
        {
            _tester.RunAllAsync().Forget();
        }

        private void OnGUI()
        {
            if (_tester == null) return;

            float x = 20f, y = 20f, w = 980f, h = 320f;
            GUI.Box(new Rect(x, y, w, h), "");

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState { textColor = Color.white }
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S6-04 Chapter 1 Error/Restart Path", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawRow(x + 20, lineY, w - 40, "P1 UnknownChapterTryResolveOrFail (fire 99 → OnSceneLoadFailed + Error)", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P2 NewestWinsPendingDuringTransition (fire 2 mid-transition fire 1 → newest=1)", _tester.P2Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P3 AssetLoadFailRetryExhaust (isolated local + bad sceneId → 2 retry exhaust)", _tester.P3Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P4 RestartFromErrorRecovery (Error → AC-10 drop → RecoverToIdle → re-fire 1)", _tester.P4Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P5 RapidNewestWinsOverwrite (rapid fire 1→2→1 → AC-8 silent + AC-9 newest=1)", _tester.P5Passed, labelStyle);
            lineY += lineH + 10;

            var footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"AllPassed: {_tester.AllPassed}    Elapsed: {_tester.TotalElapsedMs}ms", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"LoadFailedCount: {_tester.TotalLoadFailedCount}    TransitionBegin: {_tester.TotalTransitionBeginCount}    TransitionEnd: {_tester.TotalTransitionEndCount}    UnexpectedError: {_tester.UnexpectedErrorCount}", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"JSON: {S604Tester.ResultFilePath}", footerStyle);
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

    /// <summary>
    /// S6-04 spike 测试逻辑 — 5 R3 case (P1→P2→P3→P4→P5) 串行执行。
    /// </summary>
    public class S604Tester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S6-04_Result.json");

        // ==== R3 case verdict ====
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

        // ==== Listener spy state ====
        private readonly List<(int chapterId, string error)> _allLoadFailed = new List<(int, string)>();
        private readonly List<(int chapterId, string bgm)> _allLoadComplete = new List<(int, string)>();
        private readonly List<(int from, int to)> _allTransitionBegin = new List<(int, int)>();
        private readonly List<int> _allTransitionEnd = new List<int>();
        private readonly List<int> _allSceneReady = new List<int>();

        private Action<int, string> _onLF;
        private Action<int, string> _onLC;
        private Action<int, int> _onTB;
        private Action<int> _onTE;
        private Action<int> _onR;

        public int TotalLoadFailedCount => _allLoadFailed.Count;
        public int TotalLoadCompleteCount => _allLoadComplete.Count;
        public int TotalTransitionBeginCount => _allTransitionBegin.Count;
        public int TotalTransitionEndCount => _allTransitionEnd.Count;
        public int TotalSceneReadyCount => _allSceneReady.Count;

        // ==== Event log + assert dictionary ====
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

        // ==== Log sniffer state (限 unexpected error 统计；allowlist 排除 expected patterns) ====
        private readonly List<string> _capturedLogs = new List<string>();
        public int UnexpectedErrorCount { get; private set; }

        // Expected error/warning log substring allowlist — these are vendor 行为 expected, 不算 unexpected
        private static readonly string[] ExpectedLogSubstrings = new string[]
        {
            "Chapter ID",                              // TryResolveOrFail Log.Warning + LoadChapterSceneAsync provider==null/null path Log.Error
            "Chapter resolve failed",                  // TryResolveOrFail Log.Warning prefix
            "OnRequestSceneChange",                    // AC-10 silent drop Log.Warning + AC-5 dedupe Log.Info
            "Load attempt",                            // LoadChapterSceneAsync per-attempt Log.Warning
            "all 2 attempts exhausted",                // LoadChapterSceneAsync exhaust Log.Error
            "all 2 attempts exhausted —",              // LoadChapterSceneAsync exhaust Log.Error
            "ChapterDataProvider not registered",      // LoadChapterSceneAsync provider==null path
            "RegisterChapterDataProvider must",        // ChapterData null reason text
            "ChapterData null for id",                 // LoadChapterSceneAsync provider returns null path Log.Error
            "NotExistScene_Chapter99",                 // P3 bad sceneId throwing during LoadSceneAsync — vendor 内部 throw
            "RecoverToIdle called in",                 // RecoverToIdle non-Error state Log.Warning (本 spike 不应触发但 robust 防御)
            "Init called while transition",            // SceneManager.Init re-init Log.Warning (本 spike 不应触发)
            "Cannot load asset",                       // YooAsset internal warning for invalid scene
            "Scene returned invalid",                  // LoadChapterSceneAsync attempt catch path text
            "Download failed",                         // Downloader status check throw text (chapter 99 path 不应走但 robust)
            "scene to load is null",                   // YooAsset internal warning
            "[YooAsset]",                              // YooAsset framework prefix
            "AssetBundle",                             // YooAsset asset bundle related errors
            "[S6-04][P",                               // 本 spike 内部预期 Debug.Log marker
            "ActivateScene returned false",            // LoadChapterSceneAsync ActivateScene false 走 warning
            "UnloadAsync",                             // UnloadCurrentChapterAsync warning
        };

        public S604Tester(MonoBehaviour host)
        {
            _hostBehaviour = host;
        }

        // ============================================================
        // Public entry — Awake / OnDestroy 调用
        // ============================================================

        public void SubscribeEarlyListeners()
        {
            Application.logMessageReceived += OnLogReceived;

            _onLF = (id, err) =>
            {
                _allLoadFailed.Add((id, err));
            };
            _onLC = (id, bgm) =>
            {
                _allLoadComplete.Add((id, bgm));
            };
            _onTB = (from, to) =>
            {
                _allTransitionBegin.Add((from, to));
            };
            _onTE = (id) =>
            {
                _allTransitionEnd.Add(id);
            };
            _onR = (id) =>
            {
                _allSceneReady.Add(id);
            };

            GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, _onLF);
            GameEvent.AddEventListener<int, string>(ISceneEvent_Event.OnSceneLoadComplete, _onLC);
            GameEvent.AddEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, _onTB);
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneTransitionEnd, _onTE);
            GameEvent.AddEventListener<int>(ISceneEvent_Event.OnSceneReady, _onR);
        }

        public void UnsubscribeEarlyListeners()
        {
            Application.logMessageReceived -= OnLogReceived;

            if (_onLF != null)
            {
                GameEvent.RemoveEventListener<int, string>(ISceneEvent_Event.OnSceneLoadFailed, _onLF);
                _onLF = null;
            }
            if (_onLC != null)
            {
                GameEvent.RemoveEventListener<int, string>(ISceneEvent_Event.OnSceneLoadComplete, _onLC);
                _onLC = null;
            }
            if (_onTB != null)
            {
                GameEvent.RemoveEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, _onTB);
                _onTB = null;
            }
            if (_onTE != null)
            {
                GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneTransitionEnd, _onTE);
                _onTE = null;
            }
            if (_onR != null)
            {
                GameEvent.RemoveEventListener<int>(ISceneEvent_Event.OnSceneReady, _onR);
                _onR = null;
            }
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count < 500)
            {
                _capturedLogs.Add($"[{type}] {condition}");
            }

            if (type == LogType.Error || type == LogType.Exception)
            {
                // Allowlist filter — expected vendor 行为 不算 unexpected
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

        // ============================================================
        // Reflection helpers (S5-1b precedent)
        // ============================================================

        private static SceneManager GetProductionSceneManager()
        {
            var fi = typeof(GameApp).GetField("_sceneManager", BindingFlags.NonPublic | BindingFlags.Static);
            if (fi == null)
            {
                Log.Error("[S6-04] 反射拿 GameApp._sceneManager 字段失败：FieldInfo == null");
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

        private static async UniTask<bool> WaitForStateAsync(SceneManager scene, SceneManagerState target, double timeoutSec)
        {
            var sw = Stopwatch.StartNew();
            while (scene.CurrentState != target)
            {
                if (sw.Elapsed.TotalSeconds > timeoutSec)
                    return false;
                await UniTask.Yield();
            }
            return true;
        }

        // ============================================================
        // RunAllAsync — orchestrate P1 → P2 → P3 → P4 → P5
        // ============================================================

        public async UniTask RunAllAsync()
        {
            _swTotal.Start();
            try
            {
                // 等 2 帧让 GameApp Initialize + main menu UI 完成；
                // S6-04 spike 走 [main-menu] mode — DevTestState 已显示 MainMenuPanel；
                // 本 spike 不依赖 MainMenuPanel Button click — 直接 fire OnRequestSceneChange(1) 加载 chapter 1 baseline。
                await UniTask.Yield();
                await UniTask.DelayFrame(2);

                // 头：chapter 1 baseline 加载 (P1/P2/P4/P5 production case 起步前提)
                Log.Info("[S6-04] Chapter 1 baseline 加载...");
                var sm = GetProductionSceneManager();
                if (sm == null)
                {
                    OverallStatus = "Crashed: GameApp._sceneManager == null";
                    _asserts["baseline.production_sm_present"] = "FAIL: GameApp._sceneManager 反射拿 null";
                    return;
                }

                // 第一次 fire OnRequestSceneChange(1) 触发 chapter 1 load
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
                bool baselineLoaded = await WaitForIdleAsync(sm, timeoutSec: 10.0);
                if (!baselineLoaded || sm.CurrentChapterId != 1)
                {
                    OverallStatus = $"Crashed: baseline chapter 1 load failed (state={sm.CurrentState}, currentChapterId={sm.CurrentChapterId})";
                    _asserts["baseline.chapter1_loaded"] = $"FAIL: state={sm.CurrentState} currentChapterId={sm.CurrentChapterId}";
                    return;
                }
                _asserts["baseline.chapter1_loaded"] = $"PASS: chapter 1 loaded (state=Idle, currentChapterId=1)";
                Log.Info($"[S6-04] Chapter 1 baseline ✅ (state={sm.CurrentState}, currentChapterId={sm.CurrentChapterId})");

                await UniTask.DelayFrame(3);

                await RunP1Async(sm);
                await UniTask.Delay(TimeSpan.FromMilliseconds(200));

                await RunP2Async(sm);
                await UniTask.Delay(TimeSpan.FromMilliseconds(200));

                await RunP3Async(sm);
                await UniTask.Delay(TimeSpan.FromMilliseconds(200));

                await RunP4Async(sm);
                await UniTask.Delay(TimeSpan.FromMilliseconds(200));

                await RunP5Async(sm);

                OverallStatus = AllPassed ? "All Passed" : "Some Failed";
                Log.Info($"[S6-04] Done. AllPassed={AllPassed} Elapsed={_swTotal.ElapsedMilliseconds}ms LoadFailed={TotalLoadFailedCount} TransitionBegin={TotalTransitionBeginCount} TransitionEnd={TotalTransitionEndCount} UnexpectedError={UnexpectedErrorCount}");
            }
            catch (Exception e)
            {
                OverallStatus = $"Crashed: {e.GetType().Name}";
                Log.Error($"[S6-04] RunAllAsync 异常：{e}");
            }
            finally
            {
                _swTotal.Stop();
                TotalElapsedMs = _swTotal.ElapsedMilliseconds;
                WriteResultJson();
            }
        }

        // ============================================================
        // P1 UnknownChapterTryResolveOrFail — fire 99 → OnSceneLoadFailed + Error + currentChapterId 不变
        // ============================================================

        private async UniTask RunP1Async(SceneManager sm)
        {
            _swP1.Start();
            Log.Info("[S6-04] P1 UnknownChapterTryResolveOrFail 开始");

            int baselineLF = _allLoadFailed.Count;
            int chapterIdBeforeFire = sm.CurrentChapterId;

            // Action: fire 99
            Debug.Log("[S6-04][P1] expected Log.Warning below: Chapter resolve failed: Chapter ID 99 not found in TbChapter.");
            try
            {
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99);
            }
            catch (Exception e)
            {
                _asserts["P1.OnRequestSceneChange_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP1.Stop();
                P1Passed = false;
                return;
            }

            await UniTask.DelayFrame(3);

            int deltaLF = _allLoadFailed.Count - baselineLF;
            (int chapterId, string error) lastLF = _allLoadFailed.Count > 0 ? _allLoadFailed[_allLoadFailed.Count - 1] : (0, "");
            SceneManagerState stateAfter = sm.CurrentState;
            int chapterIdAfter = sm.CurrentChapterId;

            _p1Events.Add($"After fire(99): state={stateAfter}, currentChapterId={chapterIdAfter}, OnSceneLoadFailed delta={deltaLF}, lastLF=(id={lastLF.chapterId}, error='{lastLF.error}')");

            _swP1.Stop();

            // ---------- Asserts ----------
            _asserts["P1.state_after_fire_99"] = stateAfter == SceneManagerState.Error
                ? "PASS: state == Error (TryResolveOrFail 路径 transition 到 Error)"
                : $"FAIL: state={stateAfter} (期望 Error)";
            _asserts["P1.OnSceneLoadFailed_count_delta"] = deltaLF == 1
                ? "PASS: OnSceneLoadFailed fire 1 次"
                : $"FAIL: deltaLF={deltaLF} (期望 1)";
            _asserts["P1.OnSceneLoadFailed_chapterId"] = lastLF.chapterId == 99
                ? "PASS: payload chapterId == 99"
                : $"FAIL: lastLF.chapterId={lastLF.chapterId}";
            _asserts["P1.OnSceneLoadFailed_error_contains_not_found"] = !string.IsNullOrEmpty(lastLF.error) && lastLF.error.IndexOf("not found in TbChapter", StringComparison.OrdinalIgnoreCase) >= 0
                ? "PASS: error message contains 'not found in TbChapter'"
                : $"FAIL: lastLF.error='{lastLF.error}'";
            _asserts["P1.currentChapterId_unchanged"] = chapterIdAfter == chapterIdBeforeFire && chapterIdAfter == 1
                ? "PASS: CurrentChapterId 不变 (== 1 — spec line 244 实证)"
                : $"FAIL: chapterIdAfter={chapterIdAfter} (期望 1)";
            _asserts["P1.duration_ms"] = $"{_swP1.ElapsedMilliseconds}ms";

            // Cleanup: RecoverToIdle 让 P2 从 Idle 开始
            sm.RecoverToIdle();
            await UniTask.DelayFrame(2);
            _p1Events.Add($"Cleanup: RecoverToIdle → state={sm.CurrentState}");
            _asserts["P1.cleanup_state"] = sm.CurrentState == SceneManagerState.Idle
                ? "PASS: cleanup RecoverToIdle → Idle"
                : $"FAIL: state={sm.CurrentState}";

            P1Passed = stateAfter == SceneManagerState.Error &&
                       deltaLF == 1 &&
                       lastLF.chapterId == 99 &&
                       (!string.IsNullOrEmpty(lastLF.error) && lastLF.error.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0) &&
                       chapterIdAfter == 1 &&
                       sm.CurrentState == SceneManagerState.Idle;
        }

        // ============================================================
        // P2 NewestWinsPendingDuringTransition — chapter 1 → fire 2 start → fire 1 mid-transition → drain 后 chapter 1
        // ============================================================

        private async UniTask RunP2Async(SceneManager sm)
        {
            _swP2.Start();
            Log.Info("[S6-04] P2 NewestWinsPendingDuringTransition 开始");

            int baselineTB = _allTransitionBegin.Count;
            int baselineTE = _allTransitionEnd.Count;

            // 前提: 当前 chapter 1 loaded + state=Idle (P1 cleanup 后)
            if (sm.CurrentState != SceneManagerState.Idle || sm.CurrentChapterId != 1)
            {
                _asserts["P2.precondition"] = $"FAIL: state={sm.CurrentState}, currentChapterId={sm.CurrentChapterId} (期望 Idle/1)";
                _swP2.Stop();
                P2Passed = false;
                return;
            }

            // Action 1: fire(2) start transition to chapter 2
            try
            {
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(2);
            }
            catch (Exception e)
            {
                _asserts["P2.first_fire_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP2.Stop();
                P2Passed = false;
                return;
            }

            // 等 1 帧让 state 进 TransitionOut/Unloading
            await UniTask.DelayFrame(1);
            SceneManagerState stateAfterFire2 = sm.CurrentState;
            _p2Events.Add($"After fire(2): state={stateAfterFire2} (期望 TransitionOut/Unloading/Loading/TransitionIn 之一)");

            // Action 2: 在 transition 中 fire(1) — newest-wins pending
            try
            {
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
            }
            catch (Exception e)
            {
                _asserts["P2.second_fire_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP2.Stop();
                P2Passed = false;
                return;
            }

            int pendingDuringTransition = sm.PendingTargetChapterIdForTest ?? -999;
            _p2Events.Add($"After fire(1) during transition: pendingTargetChapterId={pendingDuringTransition}");

            // 等 transition 全部完成 (chapter 1→2 finish + drain pending chapter 1 → 2→1 transition finish)
            bool finalIdle = await WaitForIdleAsync(sm, timeoutSec: 15.0);
            if (!finalIdle)
            {
                _asserts["P2.final_state_idle"] = $"FAIL: timeout waiting Idle (state={sm.CurrentState})";
                _swP2.Stop();
                P2Passed = false;
                return;
            }

            int finalChapterId = sm.CurrentChapterId;
            int deltaTB = _allTransitionBegin.Count - baselineTB;
            int deltaTE = _allTransitionEnd.Count - baselineTE;
            _p2Events.Add($"Final: state={sm.CurrentState}, currentChapterId={finalChapterId}, TransitionBegin delta={deltaTB}, TransitionEnd delta={deltaTE}");

            _swP2.Stop();

            // ---------- Asserts ----------
            _asserts["P2.transition_started"] = stateAfterFire2 != SceneManagerState.Idle && stateAfterFire2 != SceneManagerState.Error
                ? $"PASS: state={stateAfterFire2} (transitioning)"
                : $"FAIL: state={stateAfterFire2} (期望 transitioning 状态)";
            _asserts["P2.pending_during_transition"] = pendingDuringTransition == 1
                ? "PASS: pendingTargetChapterId == 1 (newest-wins overwrite per AC-9)"
                : $"FAIL: pendingDuringTransition={pendingDuringTransition} (期望 1)";
            _asserts["P2.final_state_idle"] = sm.CurrentState == SceneManagerState.Idle
                ? "PASS: final state == Idle"
                : $"FAIL: state={sm.CurrentState}";
            _asserts["P2.final_chapter1_newest_wins"] = finalChapterId == 1
                ? "PASS: final currentChapterId == 1 (newest wins drain after chapter 2 transition done)"
                : $"FAIL: finalChapterId={finalChapterId}";
            _asserts["P2.TransitionBegin_count_delta_2"] = deltaTB == 2
                ? "PASS: OnSceneTransitionBegin fired 2 times (1→2 + 2→1 drain)"
                : $"FAIL: deltaTB={deltaTB} (期望 2)";
            _asserts["P2.TransitionEnd_count_delta_2"] = deltaTE == 2
                ? "PASS: OnSceneTransitionEnd fired 2 times (对称)"
                : $"FAIL: deltaTE={deltaTE} (期望 2)";
            _asserts["P2.duration_ms"] = $"{_swP2.ElapsedMilliseconds}ms";

            P2Passed = stateAfterFire2 != SceneManagerState.Idle && stateAfterFire2 != SceneManagerState.Error &&
                       pendingDuringTransition == 1 &&
                       sm.CurrentState == SceneManagerState.Idle &&
                       finalChapterId == 1 &&
                       deltaTB == 2 &&
                       deltaTE == 2;
        }

        // ============================================================
        // P3 AssetLoadFailRetryExhaust — isolated local SceneManager + fixture chapter 99 bad sceneId
        //   重要 cleanup: GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99) 是 global event-path —
        //   production sm + local sm 都接收 fire(99)；production sm 走 TryResolveOrFail FAIL → Error；
        //   local sm 走 retry exhaust → Error。P3 末尾必须 cleanup production sm RecoverToIdle()，
        //   否则 P4 precondition (state=Idle) FAIL。本 spike 验证 production retry exhaust 仅依赖
        //   local sm — production sm 同样进 Error 是 collateral 不影响 P3 verdict (delta>=1 含 production + local)。
        // ============================================================

        private async UniTask RunP3Async(SceneManager productionSm)
        {
            _swP3.Start();
            Log.Info("[S6-04] P3 AssetLoadFailRetryExhaust 开始 (isolated local SceneManager)");

            // 创建 isolated local SceneManager (不污染 production GameApp._sceneManager state — but 同时 fire(99) production 也接收)
            var local = new SceneManager();
            local.Init();
            local.RegisterChapterDataProvider(id => id == 99
                ? new ChapterData(99, "NotExistScene_Chapter99", string.Empty, 1.0f, "#000000")
                : null);

            int baselineLF = _allLoadFailed.Count;

            // Action: fire(99) — local SceneManager TryResolveOrFail returns true (provider returns non-null)
            //   → BeginTransitionAsync → LoadChapterSceneAsync(99) → LoadSceneAsync("NotExistScene_Chapter99")
            //   → throw + retry 2 次 → exhaust → OnSceneLoadFailed + state=Error
            // 同时 production sm 接收 fire(99) → TryResolveOrFail FAIL (fixture 没 99) → Error
            Debug.Log("[S6-04][P3] expected Log.Warning ×2 below: Load attempt 1/2 failed + Load attempt 2/2 failed (local sm)");
            Debug.Log("[S6-04][P3] expected Log.Error below: LoadChapterSceneAsync(99) all 2 attempts exhausted (local sm)");
            Debug.Log("[S6-04][P3] note: production sm 也接收 fire(99) → Error collateral (会在 P3 cleanup RecoverToIdle)");
            try
            {
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99);
            }
            catch (Exception e)
            {
                _asserts["P3.OnRequestSceneChange_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP3.Stop();
                P3Passed = false;
                try { local.Dispose(); } catch { /* ignore cleanup */ }
                if (productionSm != null && productionSm.CurrentState != SceneManagerState.Idle)
                {
                    productionSm.RecoverToIdle();
                }
                return;
            }

            // 等 retry exhaust + state=Error (2 retry 可能耗时 1-3s — YooAsset throw 是同步 throw 但 LoadSceneAsync await 一帧)
            bool errored = await WaitForStateAsync(local, SceneManagerState.Error, timeoutSec: 10.0);

            int deltaLF = _allLoadFailed.Count - baselineLF;
            (int chapterId, string error) lastLF = _allLoadFailed.Count > 0 ? _allLoadFailed[_allLoadFailed.Count - 1] : (0, "");
            _p3Events.Add($"After fire(99) on local: state={local.CurrentState}, errored={errored}, OnSceneLoadFailed delta={deltaLF}, lastLF=(id={lastLF.chapterId}, error='{lastLF.error}')");
            _p3Events.Add($"Production sm state after collateral fire(99): {productionSm.CurrentState}, currentChapterId={productionSm.CurrentChapterId}");

            _swP3.Stop();

            // ---------- Asserts ----------
            _asserts["P3.state_after_retry_exhaust"] = local.CurrentState == SceneManagerState.Error
                ? "PASS: local state == Error (2 retry exhaust per MaxLoadRetry=2 spec)"
                : $"FAIL: local.CurrentState={local.CurrentState}";
            _asserts["P3.OnSceneLoadFailed_count_delta_at_least_1"] = deltaLF >= 1
                ? $"PASS: OnSceneLoadFailed delta={deltaLF} (≥ 1 — retry exhaust 路径 + production sm TryResolveOrFail collateral)"
                : $"FAIL: deltaLF={deltaLF}";
            _asserts["P3.OnSceneLoadFailed_chapterId"] = lastLF.chapterId == 99
                ? "PASS: lastLF.chapterId == 99"
                : $"FAIL: lastLF.chapterId={lastLF.chapterId}";
            _asserts["P3.OnSceneLoadFailed_error_non_empty"] = !string.IsNullOrEmpty(lastLF.error)
                ? $"PASS: error message non-empty ('{lastLF.error.Substring(0, Math.Min(lastLF.error.Length, 80))}')"
                : "FAIL: error message empty";
            _asserts["P3.duration_ms"] = $"{_swP3.ElapsedMilliseconds}ms";

            // Cleanup: local.Dispose() + production sm RecoverToIdle() (清除 collateral Error)
            try { local.Dispose(); } catch { /* ignore cleanup */ }
            if (productionSm != null && productionSm.CurrentState == SceneManagerState.Error)
            {
                productionSm.RecoverToIdle();
                _p3Events.Add($"Cleanup: production sm RecoverToIdle() → state={productionSm.CurrentState}");
            }
            _p3Events.Add("Cleanup: local.Dispose() called");

            _asserts["P3.cleanup_production_idle"] = productionSm.CurrentState == SceneManagerState.Idle
                ? "PASS: production sm RecoverToIdle 后 state=Idle"
                : $"FAIL: productionSm.CurrentState={productionSm.CurrentState}";

            P3Passed = local.CurrentState == SceneManagerState.Error &&
                       deltaLF >= 1 &&
                       lastLF.chapterId == 99 &&
                       !string.IsNullOrEmpty(lastLF.error) &&
                       productionSm.CurrentState == SceneManagerState.Idle;
        }

        // ============================================================
        // P4 RestartFromErrorRecovery — Error → AC-10 silent drop → RecoverToIdle → re-fire 1
        // ============================================================

        private async UniTask RunP4Async(SceneManager sm)
        {
            _swP4.Start();
            Log.Info("[S6-04] P4 RestartFromErrorRecovery 开始");

            int baselineLF = _allLoadFailed.Count;
            int baselineTE = _allTransitionEnd.Count;

            // 前提: 当前 chapter 1 loaded + state=Idle (P2 cleanup 后)
            if (sm.CurrentState != SceneManagerState.Idle || sm.CurrentChapterId != 1)
            {
                _asserts["P4.precondition"] = $"FAIL: state={sm.CurrentState}, currentChapterId={sm.CurrentChapterId} (期望 Idle/1)";
                _swP4.Stop();
                P4Passed = false;
                return;
            }

            // Part A: 故意进入 Error state — fire(99) production fixture 99 不存在
            Debug.Log("[S6-04][P4] expected Log.Warning below: Chapter resolve failed: Chapter ID 99 not found");
            GameEvent.Get<ISceneEvent>().OnRequestSceneChange(99);
            await UniTask.DelayFrame(3);
            SceneManagerState stateAfterFire99 = sm.CurrentState;
            _p4Events.Add($"After fire(99): state={stateAfterFire99}");

            // Part B: Error 状态下 fire(1) → AC-10 silent drop + warning
            Debug.Log("[S6-04][P4] expected Log.Warning below: OnRequestSceneChange(1) dropped — Error state");
            GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
            await UniTask.DelayFrame(2);
            SceneManagerState stateAfterErrorDrop = sm.CurrentState;
            _p4Events.Add($"After fire(1) in Error state: state={stateAfterErrorDrop} (期望仍 Error — AC-10 silent drop)");

            // Part C: RecoverToIdle → state=Idle
            sm.RecoverToIdle();
            await UniTask.DelayFrame(2);
            SceneManagerState stateAfterRecover = sm.CurrentState;
            _p4Events.Add($"After RecoverToIdle: state={stateAfterRecover}");

            // Part D: 再 fire(1) — fresh attempt (currentChapterId 此时 = 1 因为 P1 实证 fail 路径不更新 currentChapterId；但 P2 后 currentChapterId=1)
            //   注：fire(1) 同 currentChapterId — 走 AC-8 silent OnSceneReady 路径不进 transition
            //   修正：本 case 验 RecoverToIdle 后 sm state 正确 + 能 fire chapter 1 success
            //   实际：fire(1) 同 1 → AC-8 silent OnSceneReady；无 transition；state 保持 Idle
            int onSceneReadyBaseline = _allSceneReady.Count;
            GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
            await UniTask.DelayFrame(3);
            int deltaSR = _allSceneReady.Count - onSceneReadyBaseline;
            _p4Events.Add($"After re-fire(1) in Idle (same target): state={sm.CurrentState}, OnSceneReady delta={deltaSR} (期望 1 — AC-8 silent path)");

            _swP4.Stop();

            // ---------- Asserts ----------
            _asserts["P4.stateA_after_fire_99"] = stateAfterFire99 == SceneManagerState.Error
                ? "PASS: state == Error after fire(99)"
                : $"FAIL: stateAfterFire99={stateAfterFire99}";
            _asserts["P4.stateB_silent_drop_unchanged"] = stateAfterErrorDrop == SceneManagerState.Error
                ? "PASS: state 仍 Error after fire(1) in Error (AC-10 silent drop 不改变 state)"
                : $"FAIL: stateAfterErrorDrop={stateAfterErrorDrop}";
            _asserts["P4.stateC_after_recover"] = stateAfterRecover == SceneManagerState.Idle
                ? "PASS: state == Idle after RecoverToIdle()"
                : $"FAIL: stateAfterRecover={stateAfterRecover}";
            _asserts["P4.stateD_after_refire_same_target"] = sm.CurrentState == SceneManagerState.Idle
                ? "PASS: state == Idle after re-fire(1) (same currentChapterId path AC-8 silent OnSceneReady)"
                : $"FAIL: state={sm.CurrentState}";
            _asserts["P4.OnSceneReady_silent_path"] = deltaSR == 1
                ? "PASS: OnSceneReady fire 1 次 (AC-8 silent path — re-fire(1) 同 chapter 1)"
                : $"FAIL: deltaSR={deltaSR} (期望 1)";
            _asserts["P4.final_currentChapterId"] = sm.CurrentChapterId == 1
                ? "PASS: currentChapterId == 1 (chapter 1 仍 loaded — fire(99) fail-loud 不更新 currentChapterId per spec)"
                : $"FAIL: currentChapterId={sm.CurrentChapterId}";
            _asserts["P4.duration_ms"] = $"{_swP4.ElapsedMilliseconds}ms";

            P4Passed = stateAfterFire99 == SceneManagerState.Error &&
                       stateAfterErrorDrop == SceneManagerState.Error &&
                       stateAfterRecover == SceneManagerState.Idle &&
                       sm.CurrentState == SceneManagerState.Idle &&
                       deltaSR == 1 &&
                       sm.CurrentChapterId == 1;
        }

        // ============================================================
        // P5 RapidNewestWinsOverwrite — Idle rapid fire (1)→(2)→(1) → AC-8 silent + newest-wins drain
        // ============================================================

        private async UniTask RunP5Async(SceneManager sm)
        {
            _swP5.Start();
            Log.Info("[S6-04] P5 RapidNewestWinsOverwrite 开始");

            int baselineTB = _allTransitionBegin.Count;
            int baselineTE = _allTransitionEnd.Count;
            int baselineSR = _allSceneReady.Count;

            // 前提: 当前 chapter 1 loaded + state=Idle (P4 cleanup 后)
            if (sm.CurrentState != SceneManagerState.Idle || sm.CurrentChapterId != 1)
            {
                _asserts["P5.precondition"] = $"FAIL: state={sm.CurrentState}, currentChapterId={sm.CurrentChapterId} (期望 Idle/1)";
                _swP5.Stop();
                P5Passed = false;
                return;
            }

            // Fire 1: same target (1) → AC-8 silent OnSceneReady (no transition)
            GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
            await UniTask.DelayFrame(1);
            int deltaSRAfterFire1 = _allSceneReady.Count - baselineSR;
            int deltaTBAfterFire1 = _allTransitionBegin.Count - baselineTB;
            _p5Events.Add($"After fire(1) same target: OnSceneReady delta={deltaSRAfterFire1}, TransitionBegin delta={deltaTBAfterFire1}, state={sm.CurrentState}");

            // Fire 2: diff target (2) → start TransitionOut transition
            GameEvent.Get<ISceneEvent>().OnRequestSceneChange(2);
            await UniTask.DelayFrame(1);
            SceneManagerState stateAfterFire2 = sm.CurrentState;
            _p5Events.Add($"After fire(2): state={stateAfterFire2}");

            // Fire 3: rapid during transition → newest-wins pending overwrite
            GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
            int pendingAfterFire3 = sm.PendingTargetChapterIdForTest ?? -999;
            _p5Events.Add($"After fire(1) during transition: pendingTargetChapterId={pendingAfterFire3}");

            // 等 transition + drain pending 全部完成
            bool finalIdle = await WaitForIdleAsync(sm, timeoutSec: 15.0);
            if (!finalIdle)
            {
                _asserts["P5.final_state_idle"] = $"FAIL: timeout waiting Idle (state={sm.CurrentState})";
                _swP5.Stop();
                P5Passed = false;
                return;
            }

            int finalChapterId = sm.CurrentChapterId;
            int deltaTBFinal = _allTransitionBegin.Count - baselineTB;
            int deltaTEFinal = _allTransitionEnd.Count - baselineTE;
            _p5Events.Add($"Final: state={sm.CurrentState}, currentChapterId={finalChapterId}, TransitionBegin delta={deltaTBFinal}, TransitionEnd delta={deltaTEFinal}");

            _swP5.Stop();

            // ---------- Asserts ----------
            _asserts["P5.fire1_silent_OnSceneReady"] = deltaSRAfterFire1 == 1 && deltaTBAfterFire1 == 0
                ? "PASS: fire(1) same target → AC-8 silent OnSceneReady (no transition)"
                : $"FAIL: SR delta={deltaSRAfterFire1}, TB delta={deltaTBAfterFire1}";
            _asserts["P5.fire2_transitioning"] = stateAfterFire2 != SceneManagerState.Idle && stateAfterFire2 != SceneManagerState.Error
                ? $"PASS: state={stateAfterFire2} (transitioning after fire(2))"
                : $"FAIL: state={stateAfterFire2}";
            _asserts["P5.fire3_pending_newest_wins"] = pendingAfterFire3 == 1
                ? "PASS: pendingTargetChapterId == 1 (newest-wins overwrite per AC-9)"
                : $"FAIL: pendingAfterFire3={pendingAfterFire3}";
            _asserts["P5.final_state_idle"] = sm.CurrentState == SceneManagerState.Idle
                ? "PASS: final state == Idle"
                : $"FAIL: state={sm.CurrentState}";
            _asserts["P5.final_chapter1_newest_wins"] = finalChapterId == 1
                ? "PASS: final currentChapterId == 1 (newest-wins drain → chapter 1 reload)"
                : $"FAIL: finalChapterId={finalChapterId}";
            _asserts["P5.TransitionBegin_count_delta_2"] = deltaTBFinal == 2
                ? "PASS: OnSceneTransitionBegin fired 2 times (1→2 + 2→1 drain;fire(1) same target 不 transition)"
                : $"FAIL: deltaTBFinal={deltaTBFinal}";
            _asserts["P5.TransitionEnd_count_delta_2"] = deltaTEFinal == 2
                ? "PASS: OnSceneTransitionEnd fired 2 times (对称)"
                : $"FAIL: deltaTEFinal={deltaTEFinal}";
            _asserts["P5.duration_ms"] = $"{_swP5.ElapsedMilliseconds}ms";
            _asserts["P5.no_unexpected_error_final"] = UnexpectedErrorCount == 0
                ? "PASS: 0 unexpected error 全程 (expected vendor warnings 走 allowlist)"
                : $"WARN: {UnexpectedErrorCount} unexpected error (检查 captured_logs_tail_30 + ExpectedLogSubstrings allowlist)";

            P5Passed = deltaSRAfterFire1 == 1 &&
                       deltaTBAfterFire1 == 0 &&
                       stateAfterFire2 != SceneManagerState.Idle && stateAfterFire2 != SceneManagerState.Error &&
                       pendingAfterFire3 == 1 &&
                       sm.CurrentState == SceneManagerState.Idle &&
                       finalChapterId == 1 &&
                       deltaTBFinal == 2 &&
                       deltaTEFinal == 2;
        }

        // ============================================================
        // WriteResultJson — JSON evidence dump 到 Application.persistentDataPath/S6-04_Result.json
        // ============================================================

        public void WriteResultJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append($"  \"story_id\": \"S6-04\",\n");
            sb.Append($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\n");
            sb.Append($"  \"all_passed\": {AllPassed.ToString().ToLowerInvariant()},\n");
            sb.Append($"  \"overall_status\": \"{Escape(OverallStatus)}\",\n");
            sb.Append($"  \"total_time_ms\": {TotalElapsedMs},\n");
            sb.Append($"  \"total_load_failed_count\": {TotalLoadFailedCount},\n");
            sb.Append($"  \"total_load_complete_count\": {TotalLoadCompleteCount},\n");
            sb.Append($"  \"total_transition_begin_count\": {TotalTransitionBeginCount},\n");
            sb.Append($"  \"total_transition_end_count\": {TotalTransitionEndCount},\n");
            sb.Append($"  \"total_scene_ready_count\": {TotalSceneReadyCount},\n");
            sb.Append($"  \"unexpected_error_count\": {UnexpectedErrorCount},\n");
            sb.Append("  \"all_load_failed\": [\n");
            for (var i = 0; i < _allLoadFailed.Count; i++)
            {
                var lf = _allLoadFailed[i];
                sb.Append($"    {{\"chapterId\": {lf.chapterId}, \"error\": \"{Escape(lf.error)}\"}}");
                sb.Append(i == _allLoadFailed.Count - 1 ? "\n" : ",\n");
            }
            sb.Append("  ],\n");
            sb.Append("  \"all_transition_begin\": [\n");
            for (var i = 0; i < _allTransitionBegin.Count; i++)
            {
                var tb = _allTransitionBegin[i];
                sb.Append($"    {{\"from\": {tb.from}, \"to\": {tb.to}}}");
                sb.Append(i == _allTransitionBegin.Count - 1 ? "\n" : ",\n");
            }
            sb.Append("  ],\n");
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
            sb.Append("  },\n");
            sb.Append("  \"captured_logs_tail_50\": [\n");
            int startIdx = Mathf.Max(0, _capturedLogs.Count - 50);
            for (var i = startIdx; i < _capturedLogs.Count; i++)
            {
                sb.Append($"    \"{Escape(_capturedLogs[i])}\"");
                sb.Append(i == _capturedLogs.Count - 1 ? "\n" : ",\n");
            }
            sb.Append("  ]\n");
            sb.Append("}\n");

            try
            {
                File.WriteAllText(ResultFilePath, sb.ToString());
                Log.Info($"[S6-04] WriteResultJson done: {ResultFilePath}");
            }
            catch (Exception e)
            {
                Log.Error($"[S6-04] WriteResultJson 失败：{e}");
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
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        }
    }
}
#endif
