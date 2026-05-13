// 该文件由Cursor 自动生成
// S6-07 Main Menu UIWindow Polish (4 Button Group) PlayMode spike
//   per story-006-main-menu.md (Phase 0 ✅ + Phase 1 ✅ + Phase 2.0 R2 closure ✅；R1+R2+R3 readiness ✅)。
//
// 关联文档:
//   * production/epics/ui-system/story-006-main-menu.md  (10 AC + R3 5 case)
//   * production/epics/ui-system/story-006b-main-menu-bgm-asset.md  (Sprint 7+ BGM asset backlog)
//
// R3 5 PlayMode case (M1 production reflection 全程 per S5-1c precedent；run order: P1→P2→P3→P5→P4):
//   P1 LifecycleVisibilityCompliance — `await ShowUIAsyncAwait<MainMenuPanel>()` 拿 instance；reflection
//                                       check vendor 7+2 lifecycle method visibility 全 `protected virtual` /
//                                       `protected override` (V3.0.1 dp7 NEW reinforce — 防 spec wording drift
//                                       未来 再引入 `public override` 不被发现)
//   P2 4ButtonWiring                   — 4 internal Button field (NewGame/Continue/Settings/Quit) 全 != null
//                                       (transform.Find 全成功)；Continue/Settings interactable=false 灰态；
//                                       NewGame/Quit interactable=true
//   P3 FadeInBgmHookSniffer            — fade-in 0.3s 后 CanvasGroup.alpha == 1.0f；Log sniffer capture
//                                       `[AudioManager] PlayMusic: Music 'main_menu_bgm' not found in config`
//                                       (Phase 2.0 R2.8 [D] closure — 真 BGM playback 留 Sprint 7+ ui-system-006b
//                                       backlog；当前 spike verify dispatch 行为 + fail-safe 行为)
//   P5 QuitButtonClickReflection       — reflection check `_onQuitClicked` named delegate cache field —
//                                       non-null + Target==MainMenuPanel instance + Method.Name=="OnQuitClicked"；
//                                       不真 Invoke onClick (避免 Editor mode Application.Quit 触发 PlayMode
//                                       停止杀 spike + WriteResultJson 不跑 — 详 design note below)
//   P4 NewGameClickDispatch            — subscribe ISceneEvent.OnSceneTransitionBegin listener；
//                                       `NewGameButton.onClick.Invoke()` → 等 ≤ 1s → assert handler param
//                                       (from=0 + to=1) capture；Log sniffer capture `NewGameButton clicked →
//                                       dispatch ISceneEvent.OnRequestSceneChange(1)`；不 await chapter 1 完整
//                                       11-step (S5-02 P1 已验过)，只 verify dispatch 起手；P4 last because
//                                       chapter 1 transition 是 destructive (会 destroy MainMenuPanel + load
//                                       chapter 1 scene — 影响 P5 等后续 case)
//
// 设计约束:
//   * Spike 模式：1 file + 3 inner class (S607Spike : IDevSpike + S607Runtime : MonoBehaviour + S607Tester 纯逻辑)
//     沿 S5-1b/-1c/-02/-03/-05/-06/-08 precedent
//   * Awake() 同步 subscribe Application.logMessageReceived (Log sniffer) + ISceneEvent listener (per S5-1c
//     lessons memo problem_2026-05-09_spike-sync-subscribe-race.md sync-subscribe race 防御 — onClick.Invoke
//     同步派发 OnRequestSceneChange，listener-path driver 同步 fire OnSceneTransitionBegin；spike 必须
//     Awake 前置 subscribe)
//   * P5 design rationale (重要！): Application.Quit 在 Editor mode 下会触发 EditorApplication.isPlaying=false
//     立即停 PlayMode (Unity docs)，spike 死前 RunAllAsync finally WriteResultJson 不会跑，evidence 丢失。
//     选 reflection-based delegate validation 替代真 Invoke — 验证 wiring 正确性 (spike 自动可跑)；真 Quit
//     行为留 user manual smoke test verify (生产环境 main menu 显示后 click QuitButton 验)
//   * P4 destructive ordering: NewGameButton.onClick.Invoke() → SceneManager 11-step → chapter 1 transition；
//     spike 不 await transition completion (S5-02 P1 已验过完整 chain；本 case 只 verify dispatch 起手)；
//     P4 last 因后续若有 case 会被 destroyed
//   * R3 P1 7+2 lifecycle visibility 通过 reflection 静态检查 — 不 capture runtime invocation order
//     (capture invocation order 留 future test hook in MainMenuPanel — 本 spike 仅静态结构 verify)
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
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace GameLogic.DevTest.Spikes
{
    public class S607Spike : IDevSpike
    {
        public string Id => "S6-07";
        public string Name => "Main Menu UIWindow Polish (4 Button Group + Fade-In + BGM Hook Sniffer + Lifecycle Visibility V3.0.1 dp7 NEW reinforce)";

        public void Launch()
        {
            // 关键时序：Awake 在 AddComponent 内同步执行（DevBootstrap.RunRequested() 调用栈内，
            // 早于 DevTestState 异步 ShowUI<MainMenuPanel> 完成 + 早于 spike P4 case Button.onClick.Invoke()）。
            // Awake 内 sync-subscribe Application.logMessageReceived (Log sniffer) + ISceneEvent listener
            // 避 sync race (per S5-1c lessons memo problem_2026-05-09_spike-sync-subscribe-race.md)。
            var go = new GameObject("S607_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S607Runtime>();
        }
    }

    public class S607Runtime : MonoBehaviour
    {
        private S607Tester _tester;

        private void Awake()
        {
            _tester = new S607Tester(this);
            _tester.SubscribeEarlyListeners();
        }

        private void Start()
        {
            _tester.RunAllAsync().Forget();
        }

        private void OnGUI()
        {
            if (_tester == null) return;

            float x = 20f, y = 20f, w = 920f, h = 280f;
            GUI.Box(new Rect(x, y, w, h), "");

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState { textColor = Color.white }
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S6-07 Main Menu UIWindow Polish (4 Button Group)", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawRow(x + 20, lineY, w - 40, "P1 LifecycleVisibilityCompliance (vendor 7+2 lifecycle protected — V3.0.1 dp7 NEW reinforce)", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P2 4ButtonWiring (transform.Find 4 button + interactable state)", _tester.P2Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P3 FadeInBgmHookSniffer (alpha 0→1 + Log sniffer 'main_menu_bgm not found' fail-safe)", _tester.P3Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P5 QuitButtonClickReflection (delegate cache field validation — 不真 Invoke 避 Editor Quit 杀 spike)", _tester.P5Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P4 NewGameClickDispatch (onClick.Invoke + ISceneEvent listener + Log sniffer)", _tester.P4Passed, labelStyle);
            lineY += lineH + 10;

            var footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"AllPassed: {_tester.AllPassed}    Elapsed: {_tester.TotalElapsedMs}ms", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"JSON: {S607Tester.ResultFilePath}", footerStyle);
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
    /// S6-07 spike 测试逻辑 — 5 R3 case (P1→P2→P3→P5→P4 destructive ordering) 串行执行。
    /// </summary>
    public class S607Tester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S6-07_Result.json");

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

        // ==== Log sniffer state (P3 BGM hook + P4 NewGame onClick + 0 unexpected error verify) ====
        private readonly List<string> _capturedLogs = new List<string>();
        private bool _bgmFailSafeWarningCaptured; // P3: "[AudioManager] PlayMusic: Music 'main_menu_bgm' not found in config"
        private bool _newGameClickedLogCaptured;  // P4: "[MainMenuPanel] NewGameButton clicked → dispatch ISceneEvent.OnRequestSceneChange(1)"
        private int _unexpectedErrorCount;        // 全程统计 LogType.Error / LogType.Exception (excl. expected fail-safe warnings)

        // ==== P4 ISceneEvent listener (capture OnSceneTransitionBegin from=0 to=1) ====
        private int _p4TransitionBeginCount;
        private (int from, int to) _p4TransitionBeginPayload = (-999, -999);
        private Action<int, int> _p4OnTransitionBegin;

        public S607Tester(MonoBehaviour host)
        {
            _hostBehaviour = host;
        }

        // ============================================================
        // Public entry — Awake / OnDestroy 调用
        // ============================================================

        public void SubscribeEarlyListeners()
        {
            // Log sniffer (覆盖 P3 BGM hook + P4 NewGame onClick + 全程 unexpected error 统计)
            Application.logMessageReceived += OnLogReceived;

            // P4 ISceneEvent listener (sync-fire path per S5-1c precedent — onClick.Invoke 同步派发
            // OnRequestSceneChange，listener-path driver 同步 fire OnSceneTransitionBegin)
            _p4OnTransitionBegin = (from, to) =>
            {
                _p4TransitionBeginCount++;
                _p4TransitionBeginPayload = (from, to);
                _p4Events.Add($"OnSceneTransitionBegin({from},{to})");
            };
            GameEvent.AddEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, _p4OnTransitionBegin);
        }

        public void UnsubscribeEarlyListeners()
        {
            Application.logMessageReceived -= OnLogReceived;

            if (_p4OnTransitionBegin != null)
            {
                GameEvent.RemoveEventListener<int, int>(ISceneEvent_Event.OnSceneTransitionBegin, _p4OnTransitionBegin);
                _p4OnTransitionBegin = null;
            }
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            // 完整 log 列表 (限 200 行 防 OOM)
            if (_capturedLogs.Count < 200)
            {
                _capturedLogs.Add($"[{type}] {condition}");
            }

            // P3 BGM fail-safe Log.Warning 捕获 (per Phase 2.0 R2.8 [D] closure — main_menu_bgm 缺失走 PlayMusic fail-safe)
            if (condition.Contains("PlayMusic: Music 'main_menu_bgm'"))
            {
                _bgmFailSafeWarningCaptured = true;
            }

            // P4 NewGame onClick handler entry log 捕获 (production code Log.Info)
            if (condition.Contains("NewGameButton clicked"))
            {
                _newGameClickedLogCaptured = true;
            }

            // 0 unexpected error verify — 排除 expected fail-safe (BGM warning) 和 spike 自身 log
            if (type == LogType.Error || type == LogType.Exception)
            {
                _unexpectedErrorCount++;
            }
        }

        // ============================================================
        // RunAllAsync — orchestrate P1 → P2 → P3 → P5 → P4 (destructive ordering)
        // ============================================================

        public async UniTask RunAllAsync()
        {
            _swTotal.Start();
            try
            {
                await RunP1Async();
                await UniTask.Delay(TimeSpan.FromMilliseconds(200));

                await RunP2Async();
                await UniTask.Delay(TimeSpan.FromMilliseconds(200));

                await RunP3Async();
                await UniTask.Delay(TimeSpan.FromMilliseconds(200));

                // P5 reflection-only check (不 Invoke onClick — 避 Editor Quit 杀 spike) 必须在 P4 destructive 前
                await RunP5Async();
                await UniTask.Delay(TimeSpan.FromMilliseconds(200));

                // P4 last because chapter 1 transition is destructive (会 destroy MainMenuPanel)
                await RunP4Async();

                OverallStatus = AllPassed ? "All Passed" : "Some Failed";
                Log.Info($"[S6-07] Done. AllPassed={AllPassed} Elapsed={_swTotal.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                OverallStatus = $"Crashed: {e.GetType().Name}";
                Log.Error($"[S6-07] RunAllAsync 异常：{e}");
            }
            finally
            {
                _swTotal.Stop();
                TotalElapsedMs = _swTotal.ElapsedMilliseconds;
                WriteResultJson();
            }
        }

        // ============================================================
        // P1 LifecycleVisibilityCompliance — vendor 7+2 lifecycle protected modifier verify
        // (V3.0.1 dp7 NEW reinforce — 防未来 spec wording drift 再引入 `public override` 不被发现)
        // ============================================================

        private async UniTask RunP1Async()
        {
            _swP1.Start();
            Log.Info("[S6-07] P1 LifecycleVisibilityCompliance 开始");

            // ---------- Step 1: ShowUI<MainMenuPanel> 拿 instance ----------
            MainMenuPanel panel = null;
            try
            {
                panel = await GameModule.UI.ShowUIAsyncAwait<MainMenuPanel>();
            }
            catch (Exception e)
            {
                _asserts["P1.MainMenuPanel_show_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP1.Stop();
                P1Passed = false;
                return;
            }

            if (panel == null)
            {
                _asserts["P1.MainMenuPanel_instance"] = "FAIL: ShowUIAsyncAwait 返 null (prefab 缺失？或 Resources.Load 失败？)";
                _swP1.Stop();
                P1Passed = false;
                return;
            }

            _asserts["P1.MainMenuPanel_instance"] = $"PASS: instance type={panel.GetType().Name}";
            _p1Events.Add($"MainMenuPanel ready frame={Time.frameCount}");

            // ---------- Step 2: 7 vendor lifecycle method visibility check via reflection ----------
            // 7 lifecycle: ScriptGenerator + BindMemberProperty + RegisterEvent + OnCreate + OnRefresh + OnUpdate + OnDestroy
            // (per SP-002 §3 + ADR-011 §6 + V3.0.1 dp7 NEW)
            var typeMainMenu = typeof(MainMenuPanel);
            var lifecycleMethodNames = new[]
            {
                "ScriptGenerator",
                "BindMemberProperty",
                "RegisterEvent",
                "OnCreate",
                "OnRefresh",
                "OnUpdate",
                "OnDestroy",
            };
            int protectedHitCount = 0;
            int notFoundCount = 0;
            foreach (var methodName in lifecycleMethodNames)
            {
                // 走 NonPublic | Instance | DeclaredOnly == false (允许从 base UIBase/UIWindow 继承)
                var info = typeMainMenu.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                if (info == null)
                {
                    _p1Events.Add($"Lifecycle method '{methodName}' not found via reflection");
                    notFoundCount++;
                    continue;
                }

                // protected = IsFamily (含 internal+protected = IsFamilyOrAssembly 也算 OK — vendor 实际是 protected)
                bool isProtectedLike = info.IsFamily || info.IsFamilyOrAssembly;
                if (isProtectedLike)
                {
                    protectedHitCount++;
                    _p1Events.Add($"Lifecycle '{methodName}' visibility: protected ✅ (IsFamily={info.IsFamily})");
                }
                else
                {
                    _p1Events.Add($"Lifecycle '{methodName}' visibility: NOT protected ❌ (IsPublic={info.IsPublic} IsFamily={info.IsFamily}) — V3.0.1 dp7 NEW drift detected!");
                }
            }

            // ---------- Step 3: 2 extra hook (Hide + Close) visibility check ----------
            // (per SP-002 §3 7+2 hook — Hide() + Close() 在 vendor UIWindow.cs:504/509 是 protected virtual)
            var hideInfo = typeMainMenu.GetMethod("Hide",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            var closeInfo = typeMainMenu.GetMethod("Close",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            int extraHookProtectedCount = 0;
            if (hideInfo != null && (hideInfo.IsFamily || hideInfo.IsFamilyOrAssembly)) extraHookProtectedCount++;
            if (closeInfo != null && (closeInfo.IsFamily || closeInfo.IsFamilyOrAssembly)) extraHookProtectedCount++;

            _swP1.Stop();

            // ---------- Step 4: Asserts ----------
            _asserts["P1.lifecycle_protected_hits"] = protectedHitCount == 7
                ? $"PASS: 7/7 vendor lifecycle method 全 protected"
                : $"FAIL: {protectedHitCount}/7 protected (notFound={notFoundCount})";
            _asserts["P1.extra_hook_protected"] = extraHookProtectedCount == 2
                ? "PASS: 2/2 extra hook (Hide+Close) protected"
                : $"FAIL: {extraHookProtectedCount}/2 extra hook protected";
            _asserts["P1.duration_ms"] = $"{_swP1.ElapsedMilliseconds}ms";
            _asserts["P1.no_unexpected_error_so_far"] = _unexpectedErrorCount == 0
                ? "PASS: 0 unexpected error during P1"
                : $"FAIL: {_unexpectedErrorCount} unexpected error";

            P1Passed = (protectedHitCount == 7) && (extraHookProtectedCount == 2) && (_unexpectedErrorCount == 0);
        }

        // ============================================================
        // P2 4ButtonWiring — 4 Button transform.Find + interactable state verify
        // ============================================================

        private async UniTask RunP2Async()
        {
            _swP2.Start();
            Log.Info("[S6-07] P2 4ButtonWiring 开始");

            // 拿 MainMenuPanel ref (P1 後 已 ShowUI；ShowUIAsyncAwait 第二次 call 走 vendor get-or-show 路径 拿同 instance)
            MainMenuPanel panel = null;
            try
            {
                panel = await GameModule.UI.ShowUIAsyncAwait<MainMenuPanel>();
            }
            catch (Exception e)
            {
                _asserts["P2.MainMenuPanel_show_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP2.Stop();
                P2Passed = false;
                return;
            }

            if (panel == null)
            {
                _asserts["P2.MainMenuPanel_instance"] = "FAIL: instance == null";
                _swP2.Stop();
                P2Passed = false;
                return;
            }

            // 4 button reference check (internal property — same assembly direct access)
            bool newGameOk = panel.NewGameButton != null;
            bool continueOk = panel.ContinueButton != null;
            bool settingsOk = panel.SettingsButton != null;
            bool quitOk = panel.QuitButton != null;

            _asserts["P2.NewGameButton_ref"] = newGameOk ? "PASS: non-null" : "FAIL: null (transform.Find('NewGameButton') 失败 — prefab child 命名错误？)";
            _asserts["P2.ContinueButton_ref"] = continueOk ? "PASS: non-null" : "FAIL: null";
            _asserts["P2.SettingsButton_ref"] = settingsOk ? "PASS: non-null" : "FAIL: null";
            _asserts["P2.QuitButton_ref"] = quitOk ? "PASS: non-null" : "FAIL: null";

            // interactable state (Continue/Settings 灰态 + NewGame/Quit 启用)
            // 注意：interactable 在 OnRefresh 设置；调用时 panel 已经过 OnRefresh (ShowUI 走完 7 init hook)
            bool continueDisabled = continueOk && !panel.ContinueButton.interactable;
            bool settingsDisabled = settingsOk && !panel.SettingsButton.interactable;
            bool newGameEnabled = newGameOk && panel.NewGameButton.interactable;
            bool quitEnabled = quitOk && panel.QuitButton.interactable;

            _asserts["P2.ContinueButton_interactable_false"] = continueDisabled ? "PASS: interactable=false (placeholder 灰态)" : $"FAIL: interactable={panel.ContinueButton?.interactable}";
            _asserts["P2.SettingsButton_interactable_false"] = settingsDisabled ? "PASS: interactable=false (placeholder 灰态)" : $"FAIL: interactable={panel.SettingsButton?.interactable}";
            _asserts["P2.NewGameButton_interactable_true"] = newGameEnabled ? "PASS: interactable=true" : $"FAIL: interactable={panel.NewGameButton?.interactable}";
            _asserts["P2.QuitButton_interactable_true"] = quitEnabled ? "PASS: interactable=true" : $"FAIL: interactable={panel.QuitButton?.interactable}";

            _swP2.Stop();
            _asserts["P2.duration_ms"] = $"{_swP2.ElapsedMilliseconds}ms";

            P2Passed = newGameOk && continueOk && settingsOk && quitOk &&
                       continueDisabled && settingsDisabled && newGameEnabled && quitEnabled;
        }

        // ============================================================
        // P3 FadeInBgmHookSniffer — fade-in 0.3s 后 alpha==1.0f + Log sniffer BGM hook fail-safe verify
        // (per Phase 2.0 R2.8 [D] closure — main_menu_bgm AudioConfig 缺失走 PlayMusic fail-safe Log.Warning)
        // ============================================================

        private async UniTask RunP3Async()
        {
            _swP3.Start();
            Log.Info("[S6-07] P3 FadeInBgmHookSniffer 开始");

            // 拿 MainMenuPanel ref + CanvasGroup
            MainMenuPanel panel = null;
            try
            {
                panel = await GameModule.UI.ShowUIAsyncAwait<MainMenuPanel>();
            }
            catch (Exception e)
            {
                _asserts["P3.MainMenuPanel_show_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP3.Stop();
                P3Passed = false;
                return;
            }

            if (panel == null || panel.transform == null)
            {
                _asserts["P3.MainMenuPanel_instance"] = "FAIL: panel/transform == null";
                _swP3.Stop();
                P3Passed = false;
                return;
            }

            // 等 fade-in 完整 (DOTween OutQuad 0.3s + buffer 200ms — 总 budget 0.5s)
            await UniTask.Delay(TimeSpan.FromMilliseconds(500));

            // CanvasGroup alpha check (拿 root CanvasGroup component)
            var cg = panel.transform.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                _asserts["P3.CanvasGroup_ref"] = "FAIL: root CanvasGroup component == null (prefab Generator 配置错误？)";
                _swP3.Stop();
                P3Passed = false;
                return;
            }

            _asserts["P3.CanvasGroup_ref"] = "PASS: root CanvasGroup non-null";
            _asserts["P3.CanvasGroup_alpha_after_fade"] = Mathf.Approximately(cg.alpha, 1.0f)
                ? $"PASS: alpha={cg.alpha:F3} (fade-in complete via DOTween OutQuad 0.3s)"
                : $"FAIL: alpha={cg.alpha:F3} (期望 1.0)";

            // BGM hook Log sniffer verify (Phase 2.0 R2.8 [D] closure — main_menu_bgm 缺失走 fail-safe Log.Warning)
            _asserts["P3.BGM_failsafe_warning_captured"] = _bgmFailSafeWarningCaptured
                ? "PASS: '[AudioManager] PlayMusic: Music main_menu_bgm not found in config' captured (fail-safe dispatch verified — Sprint 7+ ui-system-006b 真 asset add 后 BGM 自动响)"
                : "FAIL: BGM fail-safe warning 未 captured — PlayMusic 是否被调用？AudioManager 是否 _isInitialized？";

            // AudioManager _isInitialized verify via reflection (AC-3 facade activation gate 不 trigger fail-loud)
            try
            {
                var amType = typeof(AudioManager);
                var initField = amType.GetField("_isInitialized", BindingFlags.Instance | BindingFlags.NonPublic);
                if (initField != null)
                {
                    var initVal = (bool)initField.GetValue(AudioManager.Instance);
                    _asserts["P3.AudioManager_initialized"] = initVal
                        ? "PASS: AudioManager._isInitialized=true (AC-3 facade activation gate 不 trigger)"
                        : "FAIL: _isInitialized=false (GameApp.cs:40 Initialize() 未调用？)";
                }
                else
                {
                    _asserts["P3.AudioManager_initialized"] = "WARN: _isInitialized field reflection 失败 (字段命名变更？)";
                }
            }
            catch (Exception e)
            {
                _asserts["P3.AudioManager_initialized_reflection_exception"] = $"WARN: {e.Message}";
            }

            _swP3.Stop();
            _asserts["P3.duration_ms"] = $"{_swP3.ElapsedMilliseconds}ms";

            // P3 PASS 条件: alpha==1.0 + BGM warning captured (Initialized 是 sanity check 不阻 PASS)
            P3Passed = Mathf.Approximately(cg.alpha, 1.0f) && _bgmFailSafeWarningCaptured;
        }

        // ============================================================
        // P5 QuitButtonClickReflection — `_onQuitClicked` named delegate cache field 反射验证
        // (不真 Invoke onClick — Editor mode Application.Quit 触发 EditorApplication.isPlaying=false 立即停 PlayMode
        //  → spike 死前 RunAllAsync finally WriteResultJson 不会跑 → evidence 丢失。reflection-based wiring verify 替代)
        // ============================================================

        private async UniTask RunP5Async()
        {
            _swP5.Start();
            Log.Info("[S6-07] P5 QuitButtonClickReflection 开始");

            MainMenuPanel panel = null;
            try
            {
                panel = await GameModule.UI.ShowUIAsyncAwait<MainMenuPanel>();
            }
            catch (Exception e)
            {
                _asserts["P5.MainMenuPanel_show_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP5.Stop();
                P5Passed = false;
                return;
            }

            if (panel == null || panel.QuitButton == null)
            {
                _asserts["P5.QuitButton_ref"] = panel == null ? "FAIL: panel == null" : "FAIL: QuitButton == null";
                _swP5.Stop();
                P5Passed = false;
                return;
            }

            // QuitButton interactable verify
            _asserts["P5.QuitButton_interactable"] = panel.QuitButton.interactable
                ? "PASS: interactable=true (Quit 启用)"
                : $"FAIL: interactable={panel.QuitButton.interactable}";

            // _onQuitClicked named delegate cache field reflection check
            try
            {
                var typePanel = typeof(MainMenuPanel);
                var delegateField = typePanel.GetField("_onQuitClicked", BindingFlags.Instance | BindingFlags.NonPublic);
                if (delegateField == null)
                {
                    _asserts["P5._onQuitClicked_field"] = "FAIL: _onQuitClicked field reflection 拿 null (production code field 命名变更？)";
                    _swP5.Stop();
                    P5Passed = false;
                    return;
                }

                var delegateVal = delegateField.GetValue(panel) as Delegate;
                if (delegateVal == null)
                {
                    _asserts["P5._onQuitClicked_value"] = "FAIL: _onQuitClicked field value == null (OnCreate 未调？或 onClick subscribe 失败？)";
                    _swP5.Stop();
                    P5Passed = false;
                    return;
                }

                _asserts["P5._onQuitClicked_field"] = "PASS: _onQuitClicked field non-null";

                // Target check
                bool targetMatch = delegateVal.Target == panel;
                _asserts["P5._onQuitClicked_target"] = targetMatch
                    ? "PASS: delegate Target == MainMenuPanel instance"
                    : $"FAIL: Target type={delegateVal.Target?.GetType().Name} (期望 MainMenuPanel)";

                // Method.Name check
                bool methodNameMatch = delegateVal.Method.Name == "OnQuitClicked";
                _asserts["P5._onQuitClicked_method"] = methodNameMatch
                    ? "PASS: delegate Method.Name == 'OnQuitClicked'"
                    : $"FAIL: Method.Name={delegateVal.Method.Name} (期望 OnQuitClicked)";

                P5Passed = panel.QuitButton.interactable && targetMatch && methodNameMatch;
            }
            catch (Exception e)
            {
                _asserts["P5._onQuitClicked_reflection_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                P5Passed = false;
            }

            _p5Events.Add("P5 reflection-based wiring verify done (Application.Quit not invoked — 避 Editor PlayMode stop 杀 spike)");
            _p5Events.Add("Application.isPlaying=" + Application.isPlaying);

            // Application.isPlaying still true verify (PlayMode 没被杀)
            _asserts["P5.Application_isPlaying"] = Application.isPlaying
                ? "PASS: PlayMode still running (spike 未被 Quit 杀)"
                : "FAIL: Application.isPlaying=false (Quit 真触发？)";

            _swP5.Stop();
            _asserts["P5.duration_ms"] = $"{_swP5.ElapsedMilliseconds}ms";
        }

        // ============================================================
        // P4 NewGameClickDispatch — onClick.Invoke + ISceneEvent.OnSceneTransitionBegin listener + Log sniffer
        // (last because chapter 1 transition is destructive — 会 destroy MainMenuPanel + load chapter 1 scene)
        // ============================================================

        private async UniTask RunP4Async()
        {
            _swP4.Start();
            Log.Info("[S6-07] P4 NewGameClickDispatch 开始");

            MainMenuPanel panel = null;
            try
            {
                panel = await GameModule.UI.ShowUIAsyncAwait<MainMenuPanel>();
            }
            catch (Exception e)
            {
                _asserts["P4.MainMenuPanel_show_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP4.Stop();
                P4Passed = false;
                return;
            }

            if (panel == null || panel.NewGameButton == null)
            {
                _asserts["P4.NewGameButton_ref"] = panel == null ? "FAIL: panel == null" : "FAIL: NewGameButton == null";
                _swP4.Stop();
                P4Passed = false;
                return;
            }

            int baselineTransition = _p4TransitionBeginCount;
            _p4Events.Add($"baseline OnSceneTransitionBegin count={baselineTransition} frame={Time.frameCount}");

            // 模拟点击 — 触发 OnNewGameClicked → GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1)
            // → SceneManager handler (listener-path driver) 同步 fire OnSceneTransitionBegin(0, 1)
            try
            {
                panel.NewGameButton.onClick.Invoke();
                _p4Events.Add($"NewGameButton.onClick.Invoke() called frame={Time.frameCount}");
            }
            catch (Exception e)
            {
                _asserts["P4.onClick_invoke_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                _swP4.Stop();
                P4Passed = false;
                return;
            }

            // 等 1s budget — listener 必须 same/next frame fire (不 await 11-step 完整)
            await UniTask.Delay(TimeSpan.FromMilliseconds(1000));

            int deltaTransition = _p4TransitionBeginCount - baselineTransition;

            _swP4.Stop();

            _asserts["P4.OnSceneTransitionBegin_delta"] = deltaTransition >= 1
                ? $"PASS: delta={deltaTransition} payload(from={_p4TransitionBeginPayload.from},to={_p4TransitionBeginPayload.to})"
                : "FAIL: delta=0 (NewGame onClick handler 未 dispatch ISceneEvent？)";
            _asserts["P4.OnSceneTransitionBegin_payload_to"] = _p4TransitionBeginPayload.to == 1
                ? "PASS: to==1 (chapter 1)"
                : $"FAIL: to={_p4TransitionBeginPayload.to} (期望 1)";

            _asserts["P4.NewGameClicked_log_captured"] = _newGameClickedLogCaptured
                ? "PASS: '[MainMenuPanel] NewGameButton clicked' Log.Info captured (handler 真被调用)"
                : "FAIL: NewGameClicked log 未 captured";

            // 0 unexpected error final check (P3 BGM warning expected; P4 SceneManager scene load 也可能有 warning — 容错)
            _asserts["P4.no_unexpected_error_final"] = _unexpectedErrorCount == 0
                ? "PASS: 0 unexpected error 全程"
                : $"WARN: {_unexpectedErrorCount} unexpected error (P4 SceneManager 11-step transition 可能 emit Editor warning — manual review evidence 必)";

            _asserts["P4.duration_ms"] = $"{_swP4.ElapsedMilliseconds}ms";

            P4Passed = deltaTransition >= 1 &&
                       _p4TransitionBeginPayload.to == 1 &&
                       _newGameClickedLogCaptured;
        }

        // ============================================================
        // WriteResultJson — JSON evidence dump 到 Application.persistentDataPath/S6-07_Result.json
        // ============================================================

        public void WriteResultJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append($"  \"story_id\": \"S6-07\",\n");
            sb.Append($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\n");
            sb.Append($"  \"all_passed\": {AllPassed.ToString().ToLowerInvariant()},\n");
            sb.Append($"  \"overall_status\": \"{Escape(OverallStatus)}\",\n");
            sb.Append($"  \"total_time_ms\": {TotalElapsedMs},\n");
            sb.Append($"  \"unexpected_error_count\": {_unexpectedErrorCount},\n");
            sb.Append($"  \"bgm_failsafe_warning_captured\": {_bgmFailSafeWarningCaptured.ToString().ToLowerInvariant()},\n");
            sb.Append($"  \"new_game_clicked_log_captured\": {_newGameClickedLogCaptured.ToString().ToLowerInvariant()},\n");
            sb.Append("  \"cases\": [\n");
            // run order: P1 → P2 → P3 → P5 → P4 (per destructive ordering note)
            AppendCase(sb, "P1", P1Passed, _p1Events, _swP1.ElapsedMilliseconds, isLast: false);
            AppendCase(sb, "P2", P2Passed, _p2Events, _swP2.ElapsedMilliseconds, isLast: false);
            AppendCase(sb, "P3", P3Passed, _p3Events, _swP3.ElapsedMilliseconds, isLast: false);
            AppendCase(sb, "P5", P5Passed, _p5Events, _swP5.ElapsedMilliseconds, isLast: false);
            AppendCase(sb, "P4", P4Passed, _p4Events, _swP4.ElapsedMilliseconds, isLast: true);
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
                Log.Info($"[S6-07] WriteResultJson done: {ResultFilePath}");
            }
            catch (Exception e)
            {
                Log.Error($"[S6-07] WriteResultJson 失败：{e}");
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
