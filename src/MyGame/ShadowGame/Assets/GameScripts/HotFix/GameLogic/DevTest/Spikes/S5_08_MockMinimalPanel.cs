// 该文件由Cursor 自动生成
// S5-08 UIModule Setup (Sprint 5 narrow scope) — DevTest spike 专用 mock panel。
//
// 关联文档:
//   * production/epics/ui-system/story-001-uimodule-setup.md  (AC-4 + AC-5 + AC-8)
//   * R3 P2 ShowMockPanelViaShowUI / P3 UIWindowLifecycleVendorOrder / P4 ButtonOnClickPath
//
// 实施说明:
//   * 继承 GameLogic.UIWindow base class (UIWindow.cs:11 abstract class extends UIBase)
//   * [Window(UILayer.UI, fromResources: true, location: "UI/S5_08_MockMinimalPanel")] 模式
//     沿 vendor LogUI.cs:8 precedent (`[Window(UILayer.System, fromResources: true)]`)；
//     fromResources=true 让 vendor 走 Resources.Load path 避免 YooAsset bundle wire (decision Q1 [A])
//   * 7+2 lifecycle 全 override 配合 R3 P3 验完整 vendor lifecycle 顺序 (per R2.3 实证)
//   * 每个 lifecycle override 内 Debug.Log + 追加到 static LifecycleEvents (spike Tester 读取)
//   * OnCreate 内 GetComponentInChildren<Button>() 拿 prefab 内 Button reference (S5-02 main menu Button click path 前置)
//
// 重要约束 (per UIBase.cs):
//   * OnUpdate 默认实现内 `_hasOverrideUpdate = false;` — 不 override 会被 vendor 关掉 tick；
//     mock 必须 override 才能 tick > 1 帧验 OnUpdate × N
//
// 本文件仅在 UNITY_EDITOR || DEBUG 编译，Release 包零残留。

#if UNITY_EDITOR || DEBUG
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace GameLogic.DevTest.Spikes
{
    /// <summary>
    /// S5-08 spike 专用 mock UIWindow — 不入 production UI 路径。
    /// S5-02 dev-story 实施时改写为正式 minimal main menu panel (Start Chapter 1 + Next Chapter 2 Button)。
    /// </summary>
    [Window(UILayer.UI, fromResources: true, location: "UI/S5_08_MockMinimalPanel")]
    public class S5_08_MockMinimalPanel : UIWindow
    {
        // ------------------------------------------------------------------
        // 静态 lifecycle 事件追踪 (spike Tester 读取)
        // ------------------------------------------------------------------

        /// <summary>
        /// Lifecycle 事件记录条目 (供 spike Tester 验证 vendor 7+2 lifecycle 调用顺序)。
        /// </summary>
        public readonly struct LifecycleEvent
        {
            public readonly string Method;
            public readonly int Frame;
            public readonly float Time;

            public LifecycleEvent(string method, int frame, float time)
            {
                Method = method;
                Frame = frame;
                Time = time;
            }

            public override string ToString() => $"{Method}@frame={Frame}@t={Time:F3}";
        }

        /// <summary>
        /// 累计 lifecycle 事件 (vendor 调用顺序按时间排列)。spike Tester 读后可 Clear() 重置进入下一 case。
        /// </summary>
        public static readonly List<LifecycleEvent> LifecycleEvents = new List<LifecycleEvent>(32);

        /// <summary>
        /// 最近一次 OnCreate 实例的 panel reference (供 spike Tester P2/P3/P4 拿 Button reference / hierarchy 等)。
        /// vendor ShowUI<T>() 多次会保留同一 instance (per UIWindow vendor 复用约定)；如行为不同 R3 P3 会暴露。
        /// </summary>
        public static S5_08_MockMinimalPanel LastInstance { get; private set; }

        /// <summary>
        /// OnCreate 时拿到的 Button 子组件 reference (P4 ButtonOnClickPath 验 onClick.Invoke API 通路)。
        /// </summary>
        public Button ButtonRef { get; private set; }

        /// <summary>
        /// P4 ButtonOnClickPath 计数器 (spike Tester 验 onClick.Invoke 3 次 → _clickCount==3)。
        /// </summary>
        public int ClickCount { get; private set; }

        // ------------------------------------------------------------------
        // vendor 7 lifecycle method override (Init phase 3 + Lifecycle phase 4 per R2.3)
        // ------------------------------------------------------------------

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            Record("ScriptGenerator");
        }

        protected override void BindMemberProperty()
        {
            base.BindMemberProperty();
            Record("BindMemberProperty");
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            Record("RegisterEvent");
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Record("OnCreate");

            LastInstance = this;

            // 拿 prefab 内 Button 子组件 reference (per AC-8 Button onClick path verify)
            ButtonRef = transform != null ? transform.GetComponentInChildren<Button>() : null;
            if (ButtonRef == null)
            {
                Debug.LogWarning("[S5-08 mock] OnCreate: GetComponentInChildren<Button>() 返回 null — prefab 缺 Button 子节点？P4 case 会 fail");
            }
            else
            {
                ButtonRef.onClick.AddListener(OnButtonClicked);
            }
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            Record("OnRefresh");
        }

        // 必须 override OnUpdate — 否则 vendor 默认实现 `_hasOverrideUpdate = false;` 会关掉 tick
        // (per UIBase.cs:184-187)，R3 P3 验 "OnUpdate × N frame while visible" 会 fail。
        protected override void OnUpdate()
        {
            // 注意：不调用 base.OnUpdate() — base 内会 set _hasOverrideUpdate=false 关掉后续 tick
            Record("OnUpdate");
        }

        protected override void OnDestroy()
        {
            Record("OnDestroy");

            if (ButtonRef != null)
            {
                ButtonRef.onClick.RemoveListener(OnButtonClicked);
                ButtonRef = null;
            }

            base.OnDestroy();
        }

        // ------------------------------------------------------------------
        // UIWindow 额外 2 hook (UIWindow.cs:504/509 per R2.3)
        // ------------------------------------------------------------------

        protected override void Hide()
        {
            base.Hide();
            Record("Hide");
        }

        protected override void Close()
        {
            base.Close();
            Record("Close");
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Spike Tester 在每个 R3 case 入口调用 — 清空 lifecycle event 累计 + null LastInstance + 0 ClickCount。
        /// LastInstance 的 ClickCount 是 instance 字段，本静态方法只 null LastInstance reference，
        /// 不重置已活 panel 的 ClickCount（spike P4 case 前 capture startCount 减计算 delta）。
        /// </summary>
        public static void ResetForTest()
        {
            LifecycleEvents.Clear();
            LastInstance = null;
        }

        private static void Record(string method)
        {
            var ev = new LifecycleEvent(method, Time.frameCount, Time.realtimeSinceStartup);
            LifecycleEvents.Add(ev);
            Debug.Log($"[S5-08 mock] {ev}");
        }

        private void OnButtonClicked()
        {
            ClickCount++;
            Debug.Log($"[S5-08 mock] OnButtonClicked ClickCount={ClickCount}");
        }
    }
}
#endif
