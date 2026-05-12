// 该文件由Cursor 自动生成
// S5-02 dev-story Phase 2.3c (2026-05-12) — minimal inline main menu UIWindow
//
// 关联文档:
//   * production/epics/vs-chapter-1/story-002-end-to-end-flow.md  (AC-1 + AC-2 + AC-7)
//
// 设计说明:
//   * MVP minimal inline impl (per S5-02 q_panel_placement [A] decision)：production path
//     `Assets/GameScripts/HotFix/GameLogic/UI/MainMenuPanel.cs`，2 Button (Start Chapter 1 + Next Chapter)
//     + onClick callback 派发 `ISceneEvent.OnRequestSceneChange(1)` / `(0)`；Release + DEBUG 共用
//   * 不实施 ui-system-006 完整 main menu UIWindow (留 Sprint 6 polish — 配色 / 字体 / 动画 / 多语言)
//   * UIWindow vendor 路径: vendor `Handle_Completed` 内会自动 set _canvas overrideSorting/sortingOrder，
//     prefab 必须有 root Canvas component (UIWindow.cs:484-488)
//   * `[Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]` — 沿 LogUI.cs:8 +
//     S5_08_MockMinimalPanel precedent (fromResources=true 避 YooAsset bundle wire；prefab 放
//     Assets/Resources/UI/MainMenuPanel.prefab)
//   * Button child 由 prefab 提供 (StartChapter1Button + NextChapterButton 命名固定)；OnCreate 内
//     transform.Find 拿 reference + AddListener；OnDestroy 内 RemoveListener + null-out (per ADR-027 §5
//     handler null-out + null-check guard pattern)
//   * Spike 测试访问：StartChapter1Button / NextChapterButton 字段为 internal — 同一 HotFix.GameLogic
//     assembly 内 spike 可直接 Button.onClick.Invoke() 模拟点击 (无需 reflection)；Sprint 6 polish 改 private

using System;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// Chapter 1 入口 main menu UIWindow — S5-02 dev-story 阶段 minimal inline impl。
    /// 含 2 Button: "Start Chapter 1" 派发 OnRequestSceneChange(1)；"Next Chapter" 派发 OnRequestSceneChange(2)
    /// (chapter 2 MVP placeholder 复用 chapter 1 scene；Sprint 6 polish 替换真 chapter 2 art asset)。
    /// </summary>
    [Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]
    public class MainMenuPanel : UIWindow
    {
        /// <summary>
        /// Start Chapter 1 Button — onClick 派发 ISceneEvent.OnRequestSceneChange(1) 触发 SceneManager 11-step。
        /// internal: 仅同一 HotFix.GameLogic assembly 可见 (spike 测试用)；Sprint 6 polish 改 private。
        /// </summary>
        internal Button StartChapter1Button { get; private set; }

        /// <summary>
        /// Next Chapter Button — onClick 派发 ISceneEvent.OnRequestSceneChange(0) 触发 chapter 1 unload。
        /// internal: 仅同一 HotFix.GameLogic assembly 可见 (spike 测试用)；Sprint 6 polish 改 private。
        /// </summary>
        internal Button NextChapterButton { get; private set; }

        // ADR-027 §5 framework knowledge fact — handler 用 named delegate cache 防 raw double-remove
        private UnityEngine.Events.UnityAction _onStartChapter1Clicked;
        private UnityEngine.Events.UnityAction _onNextChapterClicked;

        protected override void OnCreate()
        {
            base.OnCreate();

            if (transform == null)
            {
                Log.Error("[MainMenuPanel] OnCreate: transform == null — prefab 资源加载失败？");
                return;
            }

            var startChild = transform.Find("StartChapter1Button");
            if (startChild != null)
            {
                StartChapter1Button = startChild.GetComponent<Button>();
            }
            if (StartChapter1Button == null)
            {
                Log.Error("[MainMenuPanel] OnCreate: 未找到 'StartChapter1Button' child + Button 组件 — prefab 节点命名错误？");
            }
            else
            {
                _onStartChapter1Clicked = OnStartChapter1ButtonClicked;
                StartChapter1Button.onClick.AddListener(_onStartChapter1Clicked);
            }

            var nextChild = transform.Find("NextChapterButton");
            if (nextChild != null)
            {
                NextChapterButton = nextChild.GetComponent<Button>();
            }
            if (NextChapterButton == null)
            {
                Log.Error("[MainMenuPanel] OnCreate: 未找到 'NextChapterButton' child + Button 组件 — prefab 节点命名错误？");
            }
            else
            {
                _onNextChapterClicked = OnNextChapterButtonClicked;
                NextChapterButton.onClick.AddListener(_onNextChapterClicked);
            }
        }

        protected override void OnDestroy()
        {
            if (StartChapter1Button != null && _onStartChapter1Clicked != null)
            {
                StartChapter1Button.onClick.RemoveListener(_onStartChapter1Clicked);
                _onStartChapter1Clicked = null;
                StartChapter1Button = null;
            }

            if (NextChapterButton != null && _onNextChapterClicked != null)
            {
                NextChapterButton.onClick.RemoveListener(_onNextChapterClicked);
                _onNextChapterClicked = null;
                NextChapterButton = null;
            }

            base.OnDestroy();
        }

        private void OnStartChapter1ButtonClicked()
        {
            Log.Info("[MainMenuPanel] StartChapter1Button clicked → dispatch OnRequestSceneChange(1)");
            try
            {
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
            }
            catch (Exception e)
            {
                Log.Error($"[MainMenuPanel] StartChapter1Button onClick 派发异常: {e}");
            }
        }

        private void OnNextChapterButtonClicked()
        {
            // S5-02 P5 修复 (Session 27 #3): 派 OnRequestSceneChange(2) 走 chapter 1 → chapter 2 切换路径
            // chapter 2 fixture = chapter 1 scene placeholder (MVP simplification per GameApp.BuildFixtureChapterDataProvider)
            Log.Info("[MainMenuPanel] NextChapterButton clicked → dispatch OnRequestSceneChange(2)");
            try
            {
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(2);
            }
            catch (Exception e)
            {
                Log.Error($"[MainMenuPanel] NextChapterButton onClick 派发异常: {e}");
            }
        }
    }
}
