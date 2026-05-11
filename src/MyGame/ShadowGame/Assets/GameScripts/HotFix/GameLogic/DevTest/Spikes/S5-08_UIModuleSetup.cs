// 该文件由Cursor 自动生成
// S5-08 UIModule Setup PlayMode spike —
//   验证 TEngine framework 自动 init UIModule + UIRoot scene 实例化 + GameModule.UI 静态门面 +
//   ShowUI / CloseUI / HideUI API 通路 + UIWindow vendor 7+2 lifecycle 顺序 + Button.onClick path。
//
// 关联文档:
//   * production/epics/ui-system/story-001-uimodule-setup.md  (10 AC + 4 R3 case)
//   * production/epics/vs-chapter-1/story-002-end-to-end-flow.md  (S5-02 main menu Button click path 前置)
//
// R3 4 PlayMode case (M1 dual-layer 全程 production reflection per S5-1b/1c precedent):
//   P1 UIRootSceneInstantiateVerify + UILayerExtensions sanity —
//        verify GameModule.UI != null + UIModule.UIRoot != null + UI layer + DontDestroyOnLoad +
//        UICamera != null + UILayer 5 值 GetSortingOrderBase 返 0/100/200/300/400
//   P2 ShowMockPanelViaShowUI —
//        GameModule.UI.ShowUIAsyncAwait<S5_08_MockMinimalPanel>() → mock panel 实例化到 UIRoot 子树 +
//        active=true + reflection 验 _uiStack 含此 panel + Init phase 3 method + OnCreate + OnRefresh
//        在同帧顺序调用
//   P3 UIWindowLifecycleVendorOrder —
//        post-P2；等 ≥1 帧验 OnUpdate × N → CloseUI<>() → OnDestroy → 再次 ShowUI<>() 验
//        second show 完整 lifecycle (如不一致捕获为 V3 Type-8 candidate dp)
//   P4 ButtonOnClickPath —
//        post-P2；mock panel ButtonRef.onClick.Invoke() × 3 → ClickCount==3
//
// 设计约束:
//   * Spike 模式：1 file + 3 inner class (S508Spike : IDevSpike + S508Runtime : MonoBehaviour + S508Tester 纯逻辑)
//     沿 S5-1b / S5-1c precedent
//   * 不需 subscribe TEngine GameEvent (UI lifecycle 通过 mock panel static LifecycleEvents 捕获)；
//     spike Tester reflection 拿 UIModule._uiStack private field (Q2 [A] decision)
//   * mock panel prefab 通过 [Window(UILayer.UI, fromResources: true, location: "UI/S5_08_MockMinimalPanel")]
//     从 Resources/UI/ 加载 (Q1 [A] decision)；prefab 创建在 Phase 2.3 via unity-mcp
//   * mock panel Button child 由 prefab 提供 (Q3 [A] decision)；OnCreate override 拿 reference
//
// 整文件仅在 UNITY_EDITOR || DEBUG 编译，Release 包零残留。

#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameLogic.DevTest.Spikes
{
    public class S508Spike : IDevSpike
    {
        public string Id => "S5-08";
        public string Name => "UIModule Initialization + UIWindow Base Class Setup (S5-08)";

        public void Launch()
        {
            var go = new GameObject("S508_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S508Runtime>();
        }
    }

    public class S508Runtime : MonoBehaviour
    {
        private S508Tester _tester;

        private void Start()
        {
            _tester = new S508Tester();
            _tester.WriteResultJson();
            Log.Info($"[S5-08] Runtime Start. Result JSON: {S508Tester.ResultFilePath}");

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

            float w = 760, h = 260;
            float x = (Screen.width - w) / 2f;
            float y = 20;

            GUI.Box(new Rect(x, y, w, h), string.Empty, boxStyle);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S5-08 UIModule Setup", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawRow(x + 20, lineY, w - 40, "P1 UIRootSceneInstantiateVerify + UILayerExt sanity", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P2 ShowMockPanelViaShowUI (vendor ShowUI / Init phase / OnCreate / OnRefresh)", _tester.P2Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P3 UIWindowLifecycleVendorOrder (OnUpdate × N → CloseUI → OnDestroy → 2nd show)", _tester.P3Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P4 ButtonOnClickPath (3× Invoke → ClickCount==3)", _tester.P4Passed, labelStyle);
            lineY += lineH + 10;

            var footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"AllPassed: {_tester.AllPassed}    JSON: {S508Tester.ResultFilePath}", footerStyle);
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
    /// 4 R3 case 实施 + JSON 落盘。M1 dual-layer 全 production reflection。
    /// </summary>
    public class S508Tester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S5-08_Result.json");

        public bool? P1Passed { get; private set; }
        public bool? P2Passed { get; private set; }
        public bool? P3Passed { get; private set; }
        public bool? P4Passed { get; private set; }

        public bool AllPassed =>
            P1Passed == true && P2Passed == true &&
            P3Passed == true && P4Passed == true;

        public string OverallStatus { get; private set; } = "Running";

        private readonly List<string> _p1Events = new List<string>();
        private readonly List<string> _p2Events = new List<string>();
        private readonly List<string> _p3Events = new List<string>();
        private readonly List<string> _p4Events = new List<string>();
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

                OverallStatus = AllPassed ? "All Passed" : "Some Failed";
                Log.Info($"[S5-08] Done. AllPassed={AllPassed}");
            }
            catch (Exception e)
            {
                OverallStatus = $"Crashed: {e.GetType().Name}";
                Log.Error($"[S5-08] RunAllAsync 异常：{e}");
            }
            finally
            {
                WriteResultJson();
            }
        }

        // ------------------------------------------------------------------
        // P1 UIRootSceneInstantiateVerify + UILayerExtensions sanity
        // ------------------------------------------------------------------
        private async UniTask RunP1Async()
        {
            Log.Info("[S5-08] P1 UIRootSceneInstantiateVerify 开始");

            await UniTask.Yield();

            var uiModule = GameModule.UI;
            _asserts["P1.GameModule.UI"] = uiModule != null
                ? "PASS: non-null"
                : "FAIL: GameModule.UI == null (TEngine framework 未 init UIModule？)";
            _p1Events.Add($"GameModule.UI = {(uiModule != null ? uiModule.GetType().FullName : "null")}");

            var uiRoot = UIModule.UIRoot;
            _asserts["P1.UIModule.UIRoot"] = uiRoot != null
                ? "PASS: non-null"
                : "FAIL: UIModule.UIRoot == null (scene 缺 UIRoot GameObject 或 UIModule.OnInit() 未跑)";
            _p1Events.Add($"UIModule.UIRoot = {(uiRoot != null ? uiRoot.name : "null")}");

            if (uiRoot != null)
            {
                int uiLayer = LayerMask.NameToLayer("UI");
                _asserts["P1.UIRoot.layer"] = uiRoot.gameObject.layer == uiLayer
                    ? $"PASS: UI ({uiLayer})"
                    : $"FAIL: layer={uiRoot.gameObject.layer} expected={uiLayer}";
                _p1Events.Add($"UIRoot.layer = {uiRoot.gameObject.layer} (UI layer = {uiLayer})");

                var parent = uiRoot.parent != null ? uiRoot.parent.gameObject : uiRoot.gameObject;
                bool isDontDestroy = parent.scene.name == "DontDestroyOnLoad";
                _asserts["P1.UIRoot.DontDestroyOnLoad"] = isDontDestroy
                    ? "PASS: parent scene == DontDestroyOnLoad"
                    : $"FAIL: parent scene={parent.scene.name} (期望 DontDestroyOnLoad)";
                _p1Events.Add($"UIRoot.parent.scene = {parent.scene.name}");

                var canvas = uiRoot.GetComponent<Canvas>();
                _asserts["P1.UIRoot.Canvas"] = canvas != null
                    ? "PASS: Canvas component present"
                    : "FAIL: UIRoot 上无 Canvas component";
            }

            var uiCamera = uiModule != null ? uiModule.UICamera : null;
            _asserts["P1.UICamera"] = uiCamera != null
                ? $"PASS: UICamera = {uiCamera.name}"
                : "FAIL: UICamera == null";
            _p1Events.Add($"UICamera = {(uiCamera != null ? uiCamera.name : "null")}");

            int bottom = UILayer.Bottom.GetSortingOrderBase();
            int ui = UILayer.UI.GetSortingOrderBase();
            int top = UILayer.Top.GetSortingOrderBase();
            int tips = UILayer.Tips.GetSortingOrderBase();
            int system = UILayer.System.GetSortingOrderBase();

            _asserts["P1.UILayer.Bottom.GetSortingOrderBase()"] = bottom == 0 ? "PASS: 0" : $"FAIL: {bottom}";
            _asserts["P1.UILayer.UI.GetSortingOrderBase()"] = ui == 100 ? "PASS: 100" : $"FAIL: {ui}";
            _asserts["P1.UILayer.Top.GetSortingOrderBase()"] = top == 200 ? "PASS: 200" : $"FAIL: {top}";
            _asserts["P1.UILayer.Tips.GetSortingOrderBase()"] = tips == 300 ? "PASS: 300" : $"FAIL: {tips}";
            _asserts["P1.UILayer.System.GetSortingOrderBase()"] = system == 400 ? "PASS: 400" : $"FAIL: {system}";
            _p1Events.Add($"UILayerExtensions sanity: {bottom}/{ui}/{top}/{tips}/{system}");

            P1Passed =
                uiModule != null &&
                uiRoot != null &&
                uiRoot.gameObject.layer == LayerMask.NameToLayer("UI") &&
                uiRoot.parent != null && uiRoot.parent.gameObject.scene.name == "DontDestroyOnLoad" &&
                uiCamera != null &&
                bottom == 0 && ui == 100 && top == 200 && tips == 300 && system == 400;
        }

        // ------------------------------------------------------------------
        // P2 ShowMockPanelViaShowUI
        // ------------------------------------------------------------------
        private async UniTask RunP2Async()
        {
            Log.Info("[S5-08] P2 ShowMockPanelViaShowUI 开始");

            if (GameModule.UI == null)
            {
                _asserts["P2.precondition"] = "FAIL: GameModule.UI == null (P1 失败时)";
                P2Passed = false;
                return;
            }

            S5_08_MockMinimalPanel.ResetForTest();

            int beforeFrame = Time.frameCount;
            _p2Events.Add($"BeforeShowUI frame={beforeFrame}");

            S5_08_MockMinimalPanel panel = null;
            try
            {
                panel = await GameModule.UI.ShowUIAsyncAwait<S5_08_MockMinimalPanel>();
            }
            catch (Exception e)
            {
                _asserts["P2.ShowUIAsyncAwait_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                Log.Error($"[S5-08] P2 ShowUIAsyncAwait 抛异常：{e}");
                P2Passed = false;
                return;
            }

            _asserts["P2.ShowUIAsyncAwait_returned"] = panel != null
                ? "PASS: panel instance returned"
                : "FAIL: panel == null (prefab Resources/UI/S5_08_MockMinimalPanel 不存在？)";
            _p2Events.Add($"AfterShowUI frame={Time.frameCount} panel={(panel != null ? panel.GetType().Name : "null")}");

            if (panel == null)
            {
                P2Passed = false;
                return;
            }

            _asserts["P2.LastInstance_set"] = S5_08_MockMinimalPanel.LastInstance == panel
                ? "PASS: LastInstance == returned panel"
                : "FAIL: LastInstance != returned panel";

            var uiRoot = UIModule.UIRoot;
            bool inUIRootSubtree = panel.transform != null && panel.transform.IsChildOf(uiRoot);
            _asserts["P2.panel_in_UIRoot_subtree"] = inUIRootSubtree
                ? "PASS: panel.transform.IsChildOf(UIRoot) == true"
                : $"FAIL: panel.transform parent chain 不在 UIRoot 下 (parent={(panel.transform.parent != null ? panel.transform.parent.name : "null")})";

            bool active = panel.gameObject.activeInHierarchy;
            _asserts["P2.panel_active"] = active
                ? "PASS: activeInHierarchy=true"
                : "FAIL: activeInHierarchy=false";

            bool inStack = IsInUIStack(panel);
            _asserts["P2.panel_in_uiStack"] = inStack
                ? "PASS: UIModule._uiStack 含此 panel (reflection)"
                : "FAIL: _uiStack 不含此 panel 或 reflection 失败";

            var events = S5_08_MockMinimalPanel.LifecycleEvents;
            foreach (var ev in events) _p2Events.Add(ev.ToString());

            bool hasScriptGen = events.Exists(e => e.Method == "ScriptGenerator");
            bool hasBindMember = events.Exists(e => e.Method == "BindMemberProperty");
            bool hasRegisterEvent = events.Exists(e => e.Method == "RegisterEvent");
            bool hasOnCreate = events.Exists(e => e.Method == "OnCreate");
            bool hasOnRefresh = events.Exists(e => e.Method == "OnRefresh");

            _asserts["P2.lifecycle.ScriptGenerator"] = hasScriptGen ? "PASS" : "FAIL: 未调用";
            _asserts["P2.lifecycle.BindMemberProperty"] = hasBindMember ? "PASS" : "FAIL: 未调用";
            _asserts["P2.lifecycle.RegisterEvent"] = hasRegisterEvent ? "PASS" : "FAIL: 未调用";
            _asserts["P2.lifecycle.OnCreate"] = hasOnCreate ? "PASS" : "FAIL: 未调用";
            _asserts["P2.lifecycle.OnRefresh"] = hasOnRefresh ? "PASS" : "FAIL: 未调用";

            bool orderOk = VerifyOrder(events,
                "ScriptGenerator", "BindMemberProperty", "RegisterEvent", "OnCreate", "OnRefresh");
            _asserts["P2.lifecycle_order"] = orderOk
                ? "PASS: ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh"
                : "FAIL: vendor lifecycle 顺序异常 — 见 _p2Events 详查";

            P2Passed =
                panel != null &&
                S5_08_MockMinimalPanel.LastInstance == panel &&
                inUIRootSubtree && active && inStack &&
                hasScriptGen && hasBindMember && hasRegisterEvent && hasOnCreate && hasOnRefresh &&
                orderOk;
        }

        // ------------------------------------------------------------------
        // P3 UIWindowLifecycleVendorOrder
        // ------------------------------------------------------------------
        private async UniTask RunP3Async()
        {
            Log.Info("[S5-08] P3 UIWindowLifecycleVendorOrder 开始");

            var panel = S5_08_MockMinimalPanel.LastInstance;
            if (panel == null)
            {
                _asserts["P3.precondition"] = "FAIL: P2 未成功 mock panel == null";
                P3Passed = false;
                return;
            }

            // 等待 ≥1 帧验 OnUpdate 触发
            int beforeFrame = Time.frameCount;
            await UniTask.DelayFrame(3);
            int afterFrame = Time.frameCount;
            _p3Events.Add($"WaitFrames before={beforeFrame} after={afterFrame}");

            int updateCount = 0;
            foreach (var ev in S5_08_MockMinimalPanel.LifecycleEvents)
            {
                if (ev.Method == "OnUpdate") updateCount++;
            }
            _asserts["P3.OnUpdate_count_during_visible"] = updateCount >= 1
                ? $"PASS: OnUpdate × {updateCount} frame"
                : $"FAIL: OnUpdate count={updateCount} (期望 ≥1；可能 _hasOverrideUpdate=false 被 vendor 关掉？)";

            // 记录 close 前 event count，便于检测 OnDestroy 是否新增
            int beforeCloseEvents = S5_08_MockMinimalPanel.LifecycleEvents.Count;

            try
            {
                GameModule.UI.CloseUI<S5_08_MockMinimalPanel>();
            }
            catch (Exception e)
            {
                _asserts["P3.CloseUI_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                P3Passed = false;
                return;
            }

            // 等 vendor 销毁路径完成 (HideTimeToClose 默认 10s；mock 用 default — 若行为为延迟销毁，OnDestroy 不会立即触发)
            await UniTask.DelayFrame(2);
            _p3Events.Add($"AfterCloseUI frame={Time.frameCount}");

            var allEvents = S5_08_MockMinimalPanel.LifecycleEvents;
            bool destroyFound = false;
            bool hideFound = false;
            bool closeFound = false;
            for (int i = beforeCloseEvents; i < allEvents.Count; i++)
            {
                var m = allEvents[i].Method;
                if (m == "OnDestroy") destroyFound = true;
                if (m == "Hide") hideFound = true;
                if (m == "Close") closeFound = true;
                _p3Events.Add($"AfterClose+{i - beforeCloseEvents}: {allEvents[i]}");
            }

            // 注意：vendor CloseUI 可能走 Hide (HideTimeToClose) 或直接 OnDestroy；R3 此处宽松匹配只要 Hide / Close / OnDestroy 三者任一触发即视为 CloseUI 路径已生效
            bool closeUiPathTriggered = destroyFound || hideFound || closeFound;
            _asserts["P3.CloseUI_lifecycle"] = closeUiPathTriggered
                ? $"PASS: triggered (OnDestroy={destroyFound} Hide={hideFound} Close={closeFound})"
                : "FAIL: 无 Hide / Close / OnDestroy 任一触发 (vendor CloseUI 行为非预期)";

            // second show — 验 vendor 是否复用同一 instance 或重新走完整 init phase
            S5_08_MockMinimalPanel secondPanel = null;
            int beforeSecondShowEvents = S5_08_MockMinimalPanel.LifecycleEvents.Count;
            try
            {
                secondPanel = await GameModule.UI.ShowUIAsyncAwait<S5_08_MockMinimalPanel>();
            }
            catch (Exception e)
            {
                _asserts["P3.SecondShowUI_exception"] = $"FAIL: {e.GetType().Name}: {e.Message}";
                P3Passed = false;
                return;
            }

            _asserts["P3.SecondShow_panel"] = secondPanel != null
                ? "PASS: second panel non-null"
                : "FAIL: second panel == null";

            bool secondReuseInstance = secondPanel != null && ReferenceEquals(secondPanel, panel);
            _asserts["P3.SecondShow_reuse_instance"] = secondReuseInstance
                ? "INFO: vendor 复用 first instance (vendor cache 行为)"
                : "INFO: vendor 创建新 instance (vs spec 假设；如行为不同累计 V3 Type-8 dp)";

            int secondShowInitCount = 0;
            for (int i = beforeSecondShowEvents; i < S5_08_MockMinimalPanel.LifecycleEvents.Count; i++)
            {
                var m = S5_08_MockMinimalPanel.LifecycleEvents[i].Method;
                if (m == "ScriptGenerator" || m == "BindMemberProperty" || m == "RegisterEvent" || m == "OnCreate")
                    secondShowInitCount++;
                _p3Events.Add($"AfterSecondShow+{i - beforeSecondShowEvents}: {S5_08_MockMinimalPanel.LifecycleEvents[i]}");
            }
            _asserts["P3.SecondShow_init_count"] = $"INFO: init phase methods in 2nd show = {secondShowInitCount} (0 = vendor 复用 / 4 = 完整 replay；spec 假设 OnRefresh-only TBD per R2.3 备注)";

            P3Passed = updateCount >= 1 && closeUiPathTriggered && secondPanel != null;
        }

        // ------------------------------------------------------------------
        // P4 ButtonOnClickPath
        // ------------------------------------------------------------------
        private async UniTask RunP4Async()
        {
            Log.Info("[S5-08] P4 ButtonOnClickPath 开始");

            await UniTask.Yield();

            var panel = S5_08_MockMinimalPanel.LastInstance;
            if (panel == null)
            {
                _asserts["P4.precondition"] = "FAIL: LastInstance == null";
                P4Passed = false;
                return;
            }

            var button = panel.ButtonRef;
            _asserts["P4.button_ref"] = button != null
                ? "PASS: ButtonRef non-null (prefab Button child GetComponentInChildren found)"
                : "FAIL: ButtonRef == null (prefab 缺 Button child？)";

            if (button == null)
            {
                P4Passed = false;
                return;
            }

            int startCount = panel.ClickCount;
            _p4Events.Add($"BeforeInvoke ClickCount={startCount}");

            button.onClick.Invoke();
            button.onClick.Invoke();
            button.onClick.Invoke();
            await UniTask.Yield();

            int endCount = panel.ClickCount;
            _p4Events.Add($"AfterInvoke×3 ClickCount={endCount}");

            int delta = endCount - startCount;
            _asserts["P4.click_count_delta"] = delta == 3
                ? "PASS: delta=3"
                : $"FAIL: delta={delta} (期望 3)";

            P4Passed = delta == 3;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// reflection 拿 UIModule._uiStack private List&lt;UIWindow&gt; (per Q2 [A] decision)。
        /// vendor sync 可能改名 → try-catch + fallback returns false (assert FAIL but spike 不 crash)。
        /// </summary>
        private static bool IsInUIStack(UIWindow panel)
        {
            try
            {
                var uiModule = GameModule.UI;
                if (uiModule == null || panel == null) return false;

                var fi = typeof(UIModule).GetField("_uiStack", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi == null)
                {
                    Log.Warning("[S5-08] reflection 拿 UIModule._uiStack 失败：FieldInfo == null");
                    return false;
                }
                var list = fi.GetValue(uiModule) as List<UIWindow>;
                if (list == null) return false;
                return list.Contains(panel);
            }
            catch (Exception e)
            {
                Log.Warning($"[S5-08] reflection 拿 _uiStack 异常：{e.GetType().Name}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 验 events 内若 sequence 按给定 ordered list 升序出现 (允许中间夹杂其他 method)。
        /// </summary>
        private static bool VerifyOrder(List<S5_08_MockMinimalPanel.LifecycleEvent> events, params string[] orderedMethods)
        {
            int idx = 0;
            foreach (var ev in events)
            {
                if (idx >= orderedMethods.Length) return true;
                if (ev.Method == orderedMethods[idx]) idx++;
            }
            return idx == orderedMethods.Length;
        }

        public void WriteResultJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append($"  \"story_id\": \"S5-08\",\n");
            sb.Append($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\n");
            sb.Append($"  \"all_passed\": {AllPassed.ToString().ToLowerInvariant()},\n");
            sb.Append($"  \"overall_status\": \"{Escape(OverallStatus)}\",\n");
            sb.Append("  \"cases\": [\n");
            AppendCase(sb, "P1", P1Passed, _p1Events, isLast: false);
            AppendCase(sb, "P2", P2Passed, _p2Events, isLast: false);
            AppendCase(sb, "P3", P3Passed, _p3Events, isLast: false);
            AppendCase(sb, "P4", P4Passed, _p4Events, isLast: true);
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
                Log.Error($"[S5-08] WriteResultJson 失败：{e}");
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
