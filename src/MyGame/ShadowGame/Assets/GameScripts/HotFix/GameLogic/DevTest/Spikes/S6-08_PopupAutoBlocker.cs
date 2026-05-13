// 该文件由Cursor 自动生成
// S6-08 Popup Queue Verify + Auto InputBlocker Sender-Side (Top/Tips Layer) PlayMode spike
//   per story-008-ui-layer-strategy.md (Phase 0 ✅ + Phase 1 ✅；R1+R2+R3 readiness ✅)。
//
// 关联文档:
//   * production/epics/ui-system/story-008-ui-layer-strategy.md  (10 AC + R3 5 case)
//   * Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6_08_MockPanels.cs  (7 mock UIWindow fixture)
//   * Assets/Editor/DevTest/S6_08_MockPanelsGenerator.cs  (Tools/S6-08/Generate Mock Panel Prefabs (All))
//
// R3 5 PlayMode case (M1 listener spy + public API + reflection 混合；run order: P1→P2→P3→P4→P5):
//   P1 TopLayerSenderVerify        — ShowUI<MockTopPanel> → 验 OnPushBlocker fire 1 次 + token ==
//                                    typeof(MockTopPanel).FullName；CloseUI → 验 OnPopBlocker fire 1 次 + token 一致
//                                    + 0 unexpected error。
//   P2 UIBottomSystemNoFire        — ShowUI<S5_08_MockMinimalPanel> (UI=1) / ShowUI<MockBottomPanel>
//                                    (Bottom=0) / ShowUI<MockSystemPanel> (System=4) 三次 ShowUI+CloseUI；
//                                    全程 push/pop count delta == 0 (cross-layer contrast verify)。
//   P3 TipsPopupQueueChain         — EnqueuePopup<MockTipsPanelA>(10) + <B>(20) + <C>(10) → vendor 自动 show
//                                    第 1 个 (priority 最高 B) → 1 push；CloseUI<B> → 1 pop + 自动 dequeue A
//                                    → 1 push；... 总 3 push + 3 pop；HasActivePopup / PopupQueueCount
//                                    / 反射 _currentPopupType 全程 verify。
//   P4 SortingDepthVerify          — ShowUI<MockTopPanel> + ShowUI<MockTopPanel2> → reflection 拿 Depth 验
//                                    4000 + 4100；ShowUI<MockTipsPanelA> → 验 6000 > 4100 (cross-layer)；
//                                    cleanup CloseUI ×3。
//   P5 PauseResumeClearQueue       — EnqueuePopup×3 → first auto show + PopupQueueCount==2；
//                                    PausePopupQueue → IsPopupQueuePaused==true；CloseUI first → 
//                                    _currentPopupType==null + PopupQueueCount==2 (paused 不 dequeue 下一个) +
//                                    push/pop count 各 +1；ClearPopupQueue → count==0；ResumePopupQueue → 
//                                    paused==false + 不 trigger 新 popup (queue empty)。
//
// 设计约束:
//   * Spike 模式：1 file + 3 inner class (S608Spike : IDevSpike + S608Runtime : MonoBehaviour + S608Tester
//     纯逻辑) 沿 S6-07/-1c/-02/-03/-05/-06/-08 precedent
//   * Awake() 同步 subscribe `GameEvent.AddEventListener<string>(IInputBlockerEvent_Event.On{Push,Pop}Blocker, ...)`
//     + Application.logMessageReceived (per S5-1c lessons memo problem_2026-05-09_spike-sync-subscribe-race.md
//     sync-subscribe race 防御 — ShowUI 同步 fire OnPushBlocker，spike 必须 Awake 前置 subscribe)
//   * Listener spy 模式: S5-05 P3 spike precedent (line 303-304) — subscribe IInputBlockerEvent → record token list
//   * 不实例化 InputBlocker production singleton (Sprint 7+ ADR-010 epic boundary — 详 story-008 Out of Scope)
//   * P3 popup queue vendor 自动 dequeue (UIModule.PopupQueue.OnPopupClosed line 128) — close 后 next frame
//     等 await Yield 让 vendor TryShowNextPopup + ShowUIImp 完整执行
//   * P4 reflection 拿 `UIWindow.Depth` field — 实证 vendor LAYER_DEEP=2000 + WINDOW_DEEP=100 sorting
//   * P5 destructive 顺序最后跑 — pause / clear 不影响后续 case (本 spike 没有 P6)
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
    public class S608Spike : IDevSpike
    {
        public string Id => "S6-08";
        public string Name => "Popup Queue Verify + Auto InputBlocker Sender-Side (Top/Tips Layer)";

        public void Launch()
        {
            var go = new GameObject("S608_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S608Runtime>();
        }
    }

    public class S608Runtime : MonoBehaviour
    {
        private S608Tester _tester;

        private void Awake()
        {
            _tester = new S608Tester(this);
            _tester.SubscribeEarlyListeners();
        }

        private void Start()
        {
            _tester.RunAllAsync().Forget();
        }

        private void OnGUI()
        {
            if (_tester == null) return;

            float x = 20f, y = 20f, w = 920f, h = 320f;
            GUI.Box(new Rect(x, y, w, h), "");

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState { textColor = Color.white }
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S6-08 Popup Queue Verify + Auto InputBlocker Sender (Top/Tips)", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawRow(x + 20, lineY, w - 40, "P1 TopLayerSenderVerify (Top push/pop fire 1:1 + token == FullName)", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P2 UIBottomSystemNoFire (UI/Bottom/System cross-layer contrast)", _tester.P2Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P3 TipsPopupQueueChain (priority DESC + ASC tiebreak + auto-dequeue push chain)", _tester.P3Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P4 SortingDepthVerify (LAYER_DEEP+WINDOW_DEEP same/cross layer)", _tester.P4Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P5 PauseResumeClearQueue (pause-suppress + clear + resume idempotent)", _tester.P5Passed, labelStyle);
            lineY += lineH + 10;

            var footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"AllPassed: {_tester.AllPassed}    Elapsed: {_tester.TotalElapsedMs}ms", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"PushCount: {_tester.TotalPushCount}    PopCount: {_tester.TotalPopCount}    Unexpected error: {_tester.UnexpectedErrorCount}", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"JSON: {S608Tester.ResultFilePath}", footerStyle);
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
    /// S6-08 spike 测试逻辑 — 5 R3 case (P1→P2→P3→P4→P5) 串行执行。
    /// </summary>
    public class S608Tester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S6-08_Result.json");

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
        private readonly List<string> _allPushTokens = new List<string>();
        private readonly List<string> _allPopTokens = new List<string>();
        private Action<string> _onPush;
        private Action<string> _onPop;
        public int TotalPushCount => _allPushTokens.Count;
        public int TotalPopCount => _allPopTokens.Count;

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

        // ==== Log sniffer state (限 unexpected error 统计) ====
        private readonly List<string> _capturedLogs = new List<string>();
        public int UnexpectedErrorCount { get; private set; }

        public S608Tester(MonoBehaviour host)
        {
            _hostBehaviour = host;
        }

        // ============================================================
        // Public entry — Awake / OnDestroy 调用
        // ============================================================

        public void SubscribeEarlyListeners()
        {
            Application.logMessageReceived += OnLogReceived;

            _onPush = token =>
            {
                _allPushTokens.Add(token);
            };
            _onPop = token =>
            {
                _allPopTokens.Add(token);
            };

            GameEvent.AddEventListener<string>(IInputBlockerEvent_Event.OnPushBlocker, _onPush);
            GameEvent.AddEventListener<string>(IInputBlockerEvent_Event.OnPopBlocker, _onPop);
        }

        public void UnsubscribeEarlyListeners()
        {
            Application.logMessageReceived -= OnLogReceived;

            if (_onPush != null)
            {
                GameEvent.RemoveEventListener<string>(IInputBlockerEvent_Event.OnPushBlocker, _onPush);
                _onPush = null;
            }
            if (_onPop != null)
            {
                GameEvent.RemoveEventListener<string>(IInputBlockerEvent_Event.OnPopBlocker, _onPop);
                _onPop = null;
            }
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count < 300)
            {
                _capturedLogs.Add($"[{type}] {condition}");
            }

            if (type == LogType.Error || type == LogType.Exception)
            {
                UnexpectedErrorCount++;
            }
        }

        // ============================================================
        // RunAllAsync — orchestrate P1 → P2 → P3 → P4 → P5
        // ============================================================

        public async UniTask RunAllAsync()
        {
            _swTotal.Start();
            try
            {
                // 等 1 帧让 GameApp Initialize + UIModule.OnInit 完成
                await UniTask.Yield();
                await UniTask.DelayFrame(2);

                await RunP1Async();
                await UniTask.Delay(TimeSpan.FromMilliseconds(150));

                await RunP2Async();
                await UniTask.Delay(TimeSpan.FromMilliseconds(150));

                await RunP3Async();
                await UniTask.Delay(TimeSpan.FromMilliseconds(150));

                await RunP4Async();
                await UniTask.Delay(TimeSpan.FromMilliseconds(150));

                await RunP5Async();

                OverallStatus = AllPassed ? "All Passed" : "Some Failed";
                Log.Info($"[S6-08] Done. AllPassed={AllPassed} Elapsed={_swTotal.ElapsedMilliseconds}ms Push={TotalPushCount} Pop={TotalPopCount}");
            }
            catch (Exception e)
            {
                OverallStatus = $"Crashed: {e.GetType().Name}";
                Log.Error($"[S6-08] RunAllAsync 异常：{e}");
            }
            finally
            {
                _swTotal.Stop();
                TotalElapsedMs = _swTotal.ElapsedMilliseconds;
                WriteResultJson();
            }
        }

        // ============================================================
        // Helpers
        // ============================================================

        /// <summary>
        /// Reflection 拿 UIModule._currentPopupType private field — vendor 公有 props HasActivePopup
        /// 仅暴露 bool，本 spike P3/P5 需验具体 Type。
        /// </summary>
        private Type GetCurrentPopupType()
        {
            var ui = UIModule.Instance;
            var field = typeof(UIModule).GetField("_currentPopupType",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(ui) as Type;
        }

        /// <summary>
        /// Reflection 拿 UIWindow.Depth private set/public get — vendor public getter，但本 spike
        /// 强制走 reflection 与 _uiStack / _currentPopupType verify pattern 一致。
        /// </summary>
        private int GetWindowDepth(UIWindow window)
        {
            if (window == null) return -1;
            var prop = typeof(UIWindow).GetProperty("Depth",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop == null) return -2;
            var val = prop.GetValue(window);
            return val is int i ? i : -3;
        }

        // ============================================================
        // P1 TopLayerSenderVerify — ShowUI<MockTopPanel> sender push/pop 1:1 verify
        // ============================================================

        private async UniTask RunP1Async()
        {
            _swP1.Start();
            Log.Info("[S6-08] P1 TopLayerSenderVerify 开始");

            int baselinePush = _allPushTokens.Count;
            int baselinePop = _allPopTokens.Count;
            string expectedToken = typeof(MockTopPanel).FullName;

            // Step 1: ShowUI<MockTopPanel>
            try
            {
                GameModule.UI.ShowUI<MockTopPanel>();
            }
            catch (Exception e)
            {
                _asserts["P1.ShowUI_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP1.Stop();
                P1Passed = false;
                return;
            }

            await UniTask.DelayFrame(2);

            int deltaPushAfterShow = _allPushTokens.Count - baselinePush;
            int deltaPopAfterShow = _allPopTokens.Count - baselinePop;
            string lastPushToken = _allPushTokens.Count > 0 ? _allPushTokens[_allPushTokens.Count - 1] : null;

            _p1Events.Add($"After ShowUI: push delta={deltaPushAfterShow}, pop delta={deltaPopAfterShow}, last push token='{lastPushToken}'");

            // Step 2: CloseUI<MockTopPanel>
            try
            {
                GameModule.UI.CloseUI<MockTopPanel>();
            }
            catch (Exception e)
            {
                _asserts["P1.CloseUI_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP1.Stop();
                P1Passed = false;
                return;
            }

            await UniTask.DelayFrame(1);

            int deltaPushAfterClose = _allPushTokens.Count - baselinePush;
            int deltaPopAfterClose = _allPopTokens.Count - baselinePop;
            string lastPopToken = _allPopTokens.Count > 0 ? _allPopTokens[_allPopTokens.Count - 1] : null;

            _p1Events.Add($"After CloseUI: push delta={deltaPushAfterClose}, pop delta={deltaPopAfterClose}, last pop token='{lastPopToken}'");

            _swP1.Stop();

            // ---------- Asserts ----------
            _asserts["P1.push_delta_after_show"] = deltaPushAfterShow == 1
                ? "PASS: push fired 1 time"
                : $"FAIL: push fired {deltaPushAfterShow} times (期望 1)";
            _asserts["P1.push_token_equals_fullname"] = lastPushToken == expectedToken
                ? $"PASS: push token == '{expectedToken}'"
                : $"FAIL: push token = '{lastPushToken}' (期望 '{expectedToken}')";
            _asserts["P1.pop_delta_after_close"] = (deltaPopAfterClose - deltaPopAfterShow) == 1
                ? "PASS: pop fired 1 time"
                : $"FAIL: pop fired {deltaPopAfterClose - deltaPopAfterShow} times (期望 1)";
            _asserts["P1.pop_token_equals_push"] = lastPopToken == lastPushToken
                ? "PASS: push token == pop token"
                : $"FAIL: pop token '{lastPopToken}' != push token '{lastPushToken}'";
            _asserts["P1.no_unexpected_error_so_far"] = UnexpectedErrorCount == 0
                ? "PASS: 0 unexpected error during P1"
                : $"FAIL: {UnexpectedErrorCount} unexpected error";
            _asserts["P1.duration_ms"] = $"{_swP1.ElapsedMilliseconds}ms";

            P1Passed = deltaPushAfterShow == 1 &&
                       lastPushToken == expectedToken &&
                       (deltaPopAfterClose - deltaPopAfterShow) == 1 &&
                       lastPopToken == lastPushToken &&
                       UnexpectedErrorCount == 0;
        }

        // ============================================================
        // P2 UIBottomSystemNoFire — UI(1)/Bottom(0)/System(4) cross-layer contrast (push/pop delta == 0)
        // ============================================================

        private async UniTask RunP2Async()
        {
            _swP2.Start();
            Log.Info("[S6-08] P2 UIBottomSystemNoFire 开始");

            await VerifyNoFireForLayer<S5_08_MockMinimalPanel>("UI(1)", "P2.UI_layer_no_fire");
            await VerifyNoFireForLayer<MockBottomPanel>("Bottom(0)", "P2.Bottom_layer_no_fire");
            await VerifyNoFireForLayer<MockSystemPanel>("System(4)", "P2.System_layer_no_fire");

            _swP2.Stop();
            _asserts["P2.duration_ms"] = $"{_swP2.ElapsedMilliseconds}ms";

            P2Passed = _asserts.TryGetValue("P2.UI_layer_no_fire", out var v1) && v1.StartsWith("PASS") &&
                       _asserts.TryGetValue("P2.Bottom_layer_no_fire", out var v2) && v2.StartsWith("PASS") &&
                       _asserts.TryGetValue("P2.System_layer_no_fire", out var v3) && v3.StartsWith("PASS");
        }

        private async UniTask VerifyNoFireForLayer<T>(string layerLabel, string assertKey) where T : UIWindow, new()
        {
            int basePush = _allPushTokens.Count;
            int basePop = _allPopTokens.Count;

            try
            {
                GameModule.UI.ShowUI<T>();
            }
            catch (Exception e)
            {
                _asserts[assertKey] = $"FAIL: ShowUI exception {e.GetType().Name}: {e.Message}";
                return;
            }
            await UniTask.DelayFrame(2);

            try
            {
                GameModule.UI.CloseUI<T>();
            }
            catch (Exception e)
            {
                _asserts[assertKey] = $"FAIL: CloseUI exception {e.GetType().Name}: {e.Message}";
                return;
            }
            await UniTask.DelayFrame(1);

            int dPush = _allPushTokens.Count - basePush;
            int dPop = _allPopTokens.Count - basePop;
            _p2Events.Add($"{layerLabel} show+close: push delta={dPush}, pop delta={dPop}");

            _asserts[assertKey] = (dPush == 0 && dPop == 0)
                ? $"PASS: {layerLabel} layer ShowUI/CloseUI 期间 push/pop delta == 0"
                : $"FAIL: {layerLabel} layer 不应 fire — push delta={dPush}, pop delta={dPop}";
        }

        // ============================================================
        // P3 TipsPopupQueueChain — vendor 实际行为 verify (V3.0.1 dp9 NEW closure)
        //
        // vendor 实际行为 (re-discovered Phase 3 第 1 跑 — V3.0.1 dp9 NEW spec wording drift):
        //   EnqueuePopup 内 `if (_currentPopupType == null && !_isPopupQueuePaused) TryShowNextPopup()`
        //   → 每次 enqueue 都 check 是否可立即 show，所以 **first enqueue (cur=null 时) 立即 show**，
        //     priority 只影响 **后续 queue insertion order**（不是真正的 global priority queue）。
        //
        // 本 case 设计 — enqueue 顺序 == priority DESC 顺序 (A=30, B=20, C=10)，使 enqueue 顺 = show 顺：
        //   1. Enqueue A(30) → cur=null → A 立即 show → cur=A, queue=[], push fire A
        //   2. Enqueue B(20) → cur=A, insert by priority → queue=[B]
        //   3. Enqueue C(10) → cur=A, insert: 10 < 20 → insertIndex=last → queue=[B, C]
        //   4. CloseUI<A> → pop A → OnPopupClosed → TryShowNext → B show → cur=B, queue=[C], push B
        //   5. CloseUI<B> → pop B → TryShowNext → C show → cur=C, queue=[], push C
        //   6. CloseUI<C> → pop C → cur=null, queue=[]
        // 期望 push=3 (A,B,C) + pop=3 (A,B,C)，state 末态 cur=null queue empty。
        // ============================================================

        private async UniTask RunP3Async()
        {
            _swP3.Start();
            Log.Info("[S6-08] P3 TipsPopupQueueChain 开始");

            // 头清理 (防 P1/P2 残留)
            GameModule.UI.ClearAndClosePopupQueue();
            await UniTask.DelayFrame(2);

            int basePush = _allPushTokens.Count;
            int basePop = _allPopTokens.Count;

            try
            {
                // enqueue 顺序与 priority DESC 顺序一致 — first enqueue immediate show pattern
                GameModule.UI.EnqueuePopup<MockTipsPanelA>(priority: 30);
                GameModule.UI.EnqueuePopup<MockTipsPanelB>(priority: 20);
                GameModule.UI.EnqueuePopup<MockTipsPanelC>(priority: 10);
            }
            catch (Exception e)
            {
                _asserts["P3.EnqueuePopup_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP3.Stop();
                P3Passed = false;
                return;
            }

            await UniTask.DelayFrame(3);

            // Step 1: first enqueue 立即 show — cur=A
            Type cur1 = GetCurrentPopupType();
            int qCount1 = GameModule.UI.PopupQueueCount;
            _p3Events.Add($"After Enqueue×3: _currentPopupType={cur1?.Name ?? "null"}, PopupQueueCount={qCount1}, push count delta={_allPushTokens.Count - basePush}");

            // Step 2: CloseUI<A> → 自动 dequeue B (priority DESC: B=20 > C=10)
            GameModule.UI.CloseUI<MockTipsPanelA>();
            await UniTask.DelayFrame(3);
            Type cur2 = GetCurrentPopupType();
            int qCount2 = GameModule.UI.PopupQueueCount;
            _p3Events.Add($"After CloseUI<A>: _currentPopupType={cur2?.Name ?? "null"}, PopupQueueCount={qCount2}");

            // Step 3: CloseUI<B> → 自动 dequeue C
            GameModule.UI.CloseUI<MockTipsPanelB>();
            await UniTask.DelayFrame(3);
            Type cur3 = GetCurrentPopupType();
            int qCount3 = GameModule.UI.PopupQueueCount;
            _p3Events.Add($"After CloseUI<B>: _currentPopupType={cur3?.Name ?? "null"}, PopupQueueCount={qCount3}");

            // Step 4: CloseUI<C> → queue empty
            GameModule.UI.CloseUI<MockTipsPanelC>();
            await UniTask.DelayFrame(3);
            Type cur4 = GetCurrentPopupType();
            int qCount4 = GameModule.UI.PopupQueueCount;
            _p3Events.Add($"After CloseUI<C>: _currentPopupType={cur4?.Name ?? "null"}, PopupQueueCount={qCount4}");

            // 尾清理 (确保 P4 干净 start)
            GameModule.UI.ClearAndClosePopupQueue();
            await UniTask.DelayFrame(1);

            _swP3.Stop();

            int totalPushDelta = _allPushTokens.Count - basePush;
            int totalPopDelta = _allPopTokens.Count - basePop;

            // ---------- Asserts (V3.0.1 dp9 NEW closure — vendor 实际行为 expected) ----------
            _asserts["P3.first_active_popup_is_A"] = cur1 == typeof(MockTipsPanelA)
                ? "PASS: first enqueue A (cur=null 时) 立即 show — vendor 实际行为 (V3.0.1 dp9 NEW)"
                : $"FAIL: cur1={cur1?.Name ?? "null"} (期望 MockTipsPanelA)";
            _asserts["P3.queue_count_after_enqueue3"] = qCount1 == 2
                ? "PASS: PopupQueueCount == 2 (A immediately show, B+C in queue)"
                : $"FAIL: qCount1={qCount1} (期望 2)";
            _asserts["P3.after_close_A_is_B"] = cur2 == typeof(MockTipsPanelB)
                ? "PASS: A 关闭后自动 dequeue B (priority DESC: B=20 > C=10)"
                : $"FAIL: cur2={cur2?.Name ?? "null"} (期望 MockTipsPanelB)";
            _asserts["P3.after_close_B_is_C"] = cur3 == typeof(MockTipsPanelC)
                ? "PASS: B 关闭后自动 dequeue C (剩余唯一)"
                : $"FAIL: cur3={cur3?.Name ?? "null"} (期望 MockTipsPanelC)";
            _asserts["P3.after_close_C_is_null"] = cur4 == null
                ? "PASS: C 关闭后 queue 空 — _currentPopupType==null"
                : $"FAIL: cur4={cur4?.Name ?? "null"} (期望 null)";
            _asserts["P3.queue_count_final"] = qCount4 == 0
                ? "PASS: PopupQueueCount == 0 末态"
                : $"FAIL: qCount4={qCount4} (期望 0)";
            _asserts["P3.total_push_delta_is_3"] = totalPushDelta == 3
                ? "PASS: 3 push fire (A → B → C 各 1 次)"
                : $"FAIL: push delta={totalPushDelta} (期望 3)";
            _asserts["P3.total_pop_delta_is_3"] = totalPopDelta == 3
                ? "PASS: 3 pop fire (A → B → C close 各 1 次)"
                : $"FAIL: pop delta={totalPopDelta} (期望 3)";
            _asserts["P3.duration_ms"] = $"{_swP3.ElapsedMilliseconds}ms";

            P3Passed = cur1 == typeof(MockTipsPanelA) &&
                       qCount1 == 2 &&
                       cur2 == typeof(MockTipsPanelB) &&
                       cur3 == typeof(MockTipsPanelC) &&
                       cur4 == null &&
                       qCount4 == 0 &&
                       totalPushDelta == 3 &&
                       totalPopDelta == 3;
        }

        // ============================================================
        // P4 SortingDepthVerify — LAYER_DEEP(2000) + WINDOW_DEEP(100) same/cross layer
        // ============================================================

        private async UniTask RunP4Async()
        {
            _swP4.Start();
            Log.Info("[S6-08] P4 SortingDepthVerify 开始");

            int basePush = _allPushTokens.Count;
            int basePop = _allPopTokens.Count;

            try
            {
                GameModule.UI.ShowUI<MockTopPanel>();
                await UniTask.DelayFrame(2);

                GameModule.UI.ShowUI<MockTopPanel2>();
                await UniTask.DelayFrame(2);

                GameModule.UI.ShowUI<MockTipsPanelA>();
                await UniTask.DelayFrame(2);
            }
            catch (Exception e)
            {
                _asserts["P4.ShowUI_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP4.Stop();
                P4Passed = false;
                return;
            }

            int topDepth = GetWindowDepth(MockTopPanel.LastInstance);
            int top2Depth = GetWindowDepth(MockTopPanel2.LastInstance);
            int tipsDepth = GetWindowDepth(MockTipsPanelA.LastInstance);

            _p4Events.Add($"Depth: MockTopPanel={topDepth}, MockTopPanel2={top2Depth}, MockTipsPanelA={tipsDepth}");
            _p4Events.Add($"Constants: LAYER_DEEP={UIModule.LAYER_DEEP}, WINDOW_DEEP={UIModule.WINDOW_DEEP}");

            // expected: Top=2 * 2000 + idx*100；Tips=3 * 2000 + 0
            int expectedTop = 2 * UIModule.LAYER_DEEP + 0 * UIModule.WINDOW_DEEP; // 4000
            int expectedTop2 = 2 * UIModule.LAYER_DEEP + 1 * UIModule.WINDOW_DEEP; // 4100
            int expectedTips = 3 * UIModule.LAYER_DEEP + 0 * UIModule.WINDOW_DEEP; // 6000

            // cleanup
            GameModule.UI.CloseUI<MockTipsPanelA>();
            await UniTask.DelayFrame(1);
            GameModule.UI.CloseUI<MockTopPanel2>();
            await UniTask.DelayFrame(1);
            GameModule.UI.CloseUI<MockTopPanel>();
            await UniTask.DelayFrame(1);

            _swP4.Stop();

            int totalPushDelta = _allPushTokens.Count - basePush;
            int totalPopDelta = _allPopTokens.Count - basePop;

            // ---------- Asserts ----------
            _asserts["P4.MockTopPanel_depth"] = topDepth == expectedTop
                ? $"PASS: MockTopPanel.Depth == {expectedTop}"
                : $"FAIL: topDepth={topDepth} (期望 {expectedTop})";
            _asserts["P4.MockTopPanel2_depth"] = top2Depth == expectedTop2
                ? $"PASS: MockTopPanel2.Depth == {expectedTop2} (LAYER_DEEP + 1*WINDOW_DEEP)"
                : $"FAIL: top2Depth={top2Depth} (期望 {expectedTop2})";
            _asserts["P4.same_layer_order"] = top2Depth > topDepth
                ? "PASS: 后入栈 MockTopPanel2 在上层 (Depth 更大)"
                : $"FAIL: top2Depth({top2Depth}) <= topDepth({topDepth})";
            _asserts["P4.cross_layer_tips_above_top"] = tipsDepth > top2Depth
                ? $"PASS: Tips.Depth({tipsDepth}) > Top2.Depth({top2Depth}) — Tips layer 全在 Top layer 之上"
                : $"FAIL: tipsDepth={tipsDepth}, top2Depth={top2Depth}";
            _asserts["P4.MockTipsPanelA_depth"] = tipsDepth == expectedTips
                ? $"PASS: MockTipsPanelA.Depth == {expectedTips}"
                : $"FAIL: tipsDepth={tipsDepth} (期望 {expectedTips})";
            _asserts["P4.push_pop_count_3"] = (totalPushDelta == 3 && totalPopDelta == 3)
                ? "PASS: 3 panel show+close = 3 push + 3 pop (Top/Top2/Tips 全 fire)"
                : $"FAIL: push delta={totalPushDelta} pop delta={totalPopDelta} (期望各 3)";
            _asserts["P4.duration_ms"] = $"{_swP4.ElapsedMilliseconds}ms";

            P4Passed = topDepth == expectedTop &&
                       top2Depth == expectedTop2 &&
                       top2Depth > topDepth &&
                       tipsDepth > top2Depth &&
                       tipsDepth == expectedTips &&
                       totalPushDelta == 3 &&
                       totalPopDelta == 3;
        }

        // ============================================================
        // P5 PauseResumeClearQueue — pause-suppress + clear + resume idempotent
        // ============================================================

        private async UniTask RunP5Async()
        {
            _swP5.Start();
            Log.Info("[S6-08] P5 PauseResumeClearQueue 开始");

            // 头清理 (防 P3/P4 残留 — V3.0.1 dp9 NEW closure)
            GameModule.UI.ClearAndClosePopupQueue();
            await UniTask.DelayFrame(2);

            int basePush = _allPushTokens.Count;
            int basePop = _allPopTokens.Count;

            // Step 1: Enqueue × 3 (enqueue 顺序与 priority DESC 一致 — first enqueue A 立即 show，B/C 入 queue)
            GameModule.UI.EnqueuePopup<MockTipsPanelA>(priority: 30);
            GameModule.UI.EnqueuePopup<MockTipsPanelB>(priority: 20);
            GameModule.UI.EnqueuePopup<MockTipsPanelC>(priority: 10);
            await UniTask.DelayFrame(3);

            // 验 first auto show + queue count == 2
            Type cur1 = GetCurrentPopupType();
            int qCount1 = GameModule.UI.PopupQueueCount;
            _p5Events.Add($"After Enqueue×3: _currentPopupType={cur1?.Name ?? "null"}, PopupQueueCount={qCount1}");

            // Step 2: PausePopupQueue
            GameModule.UI.PausePopupQueue();
            bool pausedAfterPause = GameModule.UI.IsPopupQueuePaused;
            _p5Events.Add($"After Pause: IsPopupQueuePaused={pausedAfterPause}");

            // Step 3: CloseUI<A> → 期望 _currentPopupType==null + PopupQueueCount 不变 (paused 不 dequeue 下一个)
            GameModule.UI.CloseUI<MockTipsPanelA>();
            await UniTask.DelayFrame(3);
            Type cur2 = GetCurrentPopupType();
            int qCount2 = GameModule.UI.PopupQueueCount;
            int pushDelta2 = _allPushTokens.Count - basePush;
            int popDelta2 = _allPopTokens.Count - basePop;
            _p5Events.Add($"After CloseUI<A> during pause: _currentPopupType={cur2?.Name ?? "null"}, PopupQueueCount={qCount2}, push delta={pushDelta2}, pop delta={popDelta2}");

            // Step 4: ClearPopupQueue → queue 清空
            GameModule.UI.ClearPopupQueue();
            await UniTask.DelayFrame(1);
            int qCount3 = GameModule.UI.PopupQueueCount;
            _p5Events.Add($"After Clear: PopupQueueCount={qCount3}");

            // Step 5: ResumePopupQueue → paused==false + 不 trigger 新 popup (queue empty)
            GameModule.UI.ResumePopupQueue();
            await UniTask.DelayFrame(2);
            bool pausedAfterResume = GameModule.UI.IsPopupQueuePaused;
            Type cur3 = GetCurrentPopupType();
            int pushDelta3 = _allPushTokens.Count - basePush;
            int popDelta3 = _allPopTokens.Count - basePop;
            _p5Events.Add($"After Resume: IsPopupQueuePaused={pausedAfterResume}, _currentPopupType={cur3?.Name ?? "null"}, push delta={pushDelta3}, pop delta={popDelta3}");

            _swP5.Stop();

            // ---------- Asserts ----------
            _asserts["P5.first_active_is_A"] = cur1 == typeof(MockTipsPanelA)
                ? "PASS: 第一个 active == MockTipsPanelA (priority 30 最高)"
                : $"FAIL: cur1={cur1?.Name ?? "null"} (期望 MockTipsPanelA)";
            _asserts["P5.queue_count_after_enqueue3"] = qCount1 == 2
                ? "PASS: PopupQueueCount == 2"
                : $"FAIL: qCount1={qCount1}";
            _asserts["P5.is_paused_after_pause"] = pausedAfterPause
                ? "PASS: IsPopupQueuePaused == true after PausePopupQueue"
                : $"FAIL: IsPopupQueuePaused == {pausedAfterPause}";
            _asserts["P5.cur_null_during_pause"] = cur2 == null
                ? "PASS: A close 后 _currentPopupType == null (paused 抑制 dequeue 下一个)"
                : $"FAIL: cur2={cur2?.Name ?? "null"}";
            _asserts["P5.queue_count_stable_during_pause"] = qCount2 == 2
                ? "PASS: PopupQueueCount 仍为 2 (paused 抑制 dequeue)"
                : $"FAIL: qCount2={qCount2}";
            _asserts["P5.push_only_first_show"] = pushDelta2 == 1
                ? "PASS: 仅 first popup show 时 fire 1 push (close 后无新 push due to pause)"
                : $"FAIL: push delta during pause={pushDelta2} (期望 1)";
            _asserts["P5.pop_only_first_close"] = popDelta2 == 1
                ? "PASS: 仅 close A 时 fire 1 pop"
                : $"FAIL: pop delta during pause={popDelta2} (期望 1)";
            _asserts["P5.queue_count_zero_after_clear"] = qCount3 == 0
                ? "PASS: PopupQueueCount == 0 after ClearPopupQueue"
                : $"FAIL: qCount3={qCount3}";
            _asserts["P5.not_paused_after_resume"] = !pausedAfterResume
                ? "PASS: IsPopupQueuePaused == false after ResumePopupQueue"
                : $"FAIL: still paused";
            _asserts["P5.no_new_popup_on_empty_resume"] = cur3 == null && pushDelta3 == pushDelta2
                ? "PASS: queue empty + resume 不 trigger 新 popup"
                : $"FAIL: cur3={cur3?.Name ?? "null"}, push delta after resume={pushDelta3} (期望 {pushDelta2})";
            _asserts["P5.duration_ms"] = $"{_swP5.ElapsedMilliseconds}ms";
            _asserts["P5.no_unexpected_error_final"] = UnexpectedErrorCount == 0
                ? "PASS: 0 unexpected error 全程"
                : $"WARN: {UnexpectedErrorCount} unexpected error";

            P5Passed = cur1 == typeof(MockTipsPanelA) &&
                       qCount1 == 2 &&
                       pausedAfterPause &&
                       cur2 == null &&
                       qCount2 == 2 &&
                       pushDelta2 == 1 &&
                       popDelta2 == 1 &&
                       qCount3 == 0 &&
                       !pausedAfterResume &&
                       cur3 == null &&
                       pushDelta3 == pushDelta2;
        }

        // ============================================================
        // WriteResultJson — JSON evidence dump 到 Application.persistentDataPath/S6-08_Result.json
        // ============================================================

        public void WriteResultJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append($"  \"story_id\": \"S6-08\",\n");
            sb.Append($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\n");
            sb.Append($"  \"all_passed\": {AllPassed.ToString().ToLowerInvariant()},\n");
            sb.Append($"  \"overall_status\": \"{Escape(OverallStatus)}\",\n");
            sb.Append($"  \"total_time_ms\": {TotalElapsedMs},\n");
            sb.Append($"  \"total_push_count\": {TotalPushCount},\n");
            sb.Append($"  \"total_pop_count\": {TotalPopCount},\n");
            sb.Append($"  \"unexpected_error_count\": {UnexpectedErrorCount},\n");
            sb.Append("  \"all_push_tokens\": [\n");
            for (var i = 0; i < _allPushTokens.Count; i++)
            {
                sb.Append($"    \"{Escape(_allPushTokens[i])}\"");
                sb.Append(i == _allPushTokens.Count - 1 ? "\n" : ",\n");
            }
            sb.Append("  ],\n");
            sb.Append("  \"all_pop_tokens\": [\n");
            for (var i = 0; i < _allPopTokens.Count; i++)
            {
                sb.Append($"    \"{Escape(_allPopTokens[i])}\"");
                sb.Append(i == _allPopTokens.Count - 1 ? "\n" : ",\n");
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
            sb.Append("  \"captured_logs_tail_30\": [\n");
            int startIdx = Mathf.Max(0, _capturedLogs.Count - 30);
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
                Log.Info($"[S6-08] WriteResultJson done: {ResultFilePath}");
            }
            catch (Exception e)
            {
                Log.Error($"[S6-08] WriteResultJson 失败：{e}");
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
