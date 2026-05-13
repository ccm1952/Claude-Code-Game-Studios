// 该文件由Cursor 自动生成
// S6-07 dev-story Phase 2 (2026-05-14) — production polish 4 button group main menu UIWindow
// (S5-02 dev-story Phase 2.3c 2026-05-12 minimal inline 2 button base 完整替换升级)
//
// 关联文档:
//   * production/epics/ui-system/story-006-main-menu.md  (S6-07 narrow scope [A] decision)
//   * production/epics/ui-system/story-006b-main-menu-bgm-asset.md  (Sprint 7+ BGM asset backlog)
//
// 设计说明 (per V3.0.1 vendor reality compliant + Phase 2.0 R2.6/R2.8 [D] mixed strategy closure):
//   * S6-07 narrow scope [A]: 4 button group (NewGame / Continue placeholder / Settings placeholder / Quit)
//     + 完整 vendor 7+2 lifecycle protected override (V3.0.1 dp7 NEW — `protected virtual` vendor
//       签名兼容；`public override` 触发 CS0507)
//     + fade-in animation (CanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad))
//     + BGM start hook (AudioManager.Instance.PlayMusic("main_menu_bgm", 1.0f))
//   * Phase 2.0 R2.8 [D] closure: BGM hook 完整保留 OnRefresh 调用走 PlayMusic fail-safe
//     (Log.Warning + no-op — main_menu_bgm AudioConfig 缺失不影响 main menu show 路径)；真 BGM
//     playback 留 ui-system-006b Sprint 7+ backlog (asset add + AudioConfigFromLuban entry add 后 0 code change in 本 file)
//   * UIWindow vendor 路径: vendor `Handle_Completed` 内会自动 set _canvas overrideSorting/sortingOrder，
//     prefab 必须有 root Canvas component (UIWindow.cs:484-488)
//   * `[Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]` — 沿 LogUI.cs:8 +
//     S5_08_MockMinimalPanel + S5-02 inline base precedent (fromResources=true 避 YooAsset bundle wire；
//     prefab 放 Assets/Resources/UI/MainMenuPanel.prefab)
//   * Button child 由 prefab 提供 (NewGameButton + ContinueButton + SettingsButton + QuitButton 命名固定)；
//     OnCreate 内 transform.Find 拿 reference + AddListener；OnDestroy 内 RemoveListener + null-out
//     (per ADR-027 §5 handler null-out + null-check guard pattern)
//   * Spike 测试访问：4 button 字段为 internal — 同一 HotFix.GameLogic assembly 内 spike 可直接
//     `Button.onClick.Invoke()` 模拟点击 (无需 reflection)；Sprint 7+ polish phase 改 private
//   * CanvasGroup fade-in: prefab root 必须含 CanvasGroup component (alpha=0 起始 — 由 Generator 设置)；
//     OnDestroy 内 DOTween Kill 防止 panel 销毁后 Tween 仍持引用 NRE
//   * S5-02 spike P5 case (NextChapterButton chapter switch verify) [A] decision delete — chapter
//     switch verify scope 留 Sprint 7+ ChapterStateManager + ChapterSelect epic 单独 spike

using System;
using DG.Tweening;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// Main Menu UIWindow — S6-07 dev-story Phase 2 production polish 4 button group impl
    /// (替换 S5-02 dev-story Phase 2.3c minimal inline 2 button base)。
    /// </summary>
    /// <remarks>
    /// <para>4 button: <c>NewGameButton</c> (派 ISceneEvent.OnRequestSceneChange(1) chapter 1 start)；
    ///   <c>ContinueButton</c> (placeholder 灰态 — Sprint 7+ SaveSystem epic 接入 ISaveService)；
    ///   <c>SettingsButton</c> (placeholder 灰态 — Sprint 7+ SettingsPanel ui-system-007 起步)；
    ///   <c>QuitButton</c> (Application.Quit direct call — Procedure stage 多处 production wired precedent)。</para>
    /// <para>Vendor 7+2 lifecycle 完整 override 全 `protected override` (V3.0.1 dp7 NEW visibility modifier
    /// 强制；`public override` 触发 CS0507)。</para>
    /// <para>Fade-in: OnRefresh 内 CanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad)；prefab CanvasGroup
    /// root component alpha=0 起始 (Generator 设置)；OnDestroy 内 DOTween Kill 防 NRE。</para>
    /// <para>BGM hook: OnRefresh 内 try-catch AudioManager.Instance.PlayMusic("main_menu_bgm", 1.0f)；
    /// PlayMusic 内置 fail-safe (clipId 不存在 → Log.Warning + return 不抛 exception；不影响 main menu show)；
    /// 真 BGM playback 留 ui-system-006b Sprint 7+ backlog (asset + Luban entry add 后 0 code change in 本 file)。</para>
    /// </remarks>
    [Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]
    public class MainMenuPanel : UIWindow
    {
        // ==== Test hooks: 4 button field internal access (spike 测试访问) ====

        /// <summary>NewGame Button — onClick 派 ISceneEvent.OnRequestSceneChange(1) chapter 1 start.</summary>
        internal Button NewGameButton { get; private set; }

        /// <summary>Continue Button — placeholder 灰态 (Sprint 7+ SaveSystem epic ISaveService 接入)；
        /// onClick handler 0-line body 防 vendor 误触 / Editor Inspector 误调用.</summary>
        internal Button ContinueButton { get; private set; }

        /// <summary>Settings Button — placeholder 灰态 (Sprint 7+ ui-system-007 SettingsPanel 起步)；
        /// onClick handler 0-line body 同 Continue.</summary>
        internal Button SettingsButton { get; private set; }

        /// <summary>Quit Button — onClick 调 Application.Quit (Editor 模式由 Unity 自身 handle PlayMode stop).</summary>
        internal Button QuitButton { get; private set; }

        // ==== Named delegate cache fields (ADR-027 §5 防 raw double-remove) ====

        private UnityEngine.Events.UnityAction _onNewGameClicked;
        private UnityEngine.Events.UnityAction _onContinueClicked;
        private UnityEngine.Events.UnityAction _onSettingsClicked;
        private UnityEngine.Events.UnityAction _onQuitClicked;

        // ==== Fade-in CanvasGroup state ====

        private CanvasGroup _canvasGroup;
        private Tween _fadeInTween;

        // ==== Vendor 7 lifecycle override (全 protected override per V3.0.1 dp7 NEW) ====

        protected override void OnCreate()
        {
            base.OnCreate();

            if (transform == null)
            {
                Log.Error("[MainMenuPanel] OnCreate: transform == null — prefab 资源加载失败？");
                return;
            }

            // CanvasGroup root component for fade-in (prefab Generator 已挂；alpha=0 起始)
            _canvasGroup = transform.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Log.Warning("[MainMenuPanel] OnCreate: 未找到 root CanvasGroup component — fade-in 跳过 (prefab Generator 配置错误？)");
            }
            else
            {
                // 强制起始 alpha=0 (即使 prefab 未设；防 first frame flash full-alpha)
                _canvasGroup.alpha = 0f;
            }

            // 4 button transform.Find + AddListener
            BindButton("NewGameButton", out var newGame, OnNewGameClicked, out _onNewGameClicked);
            NewGameButton = newGame;

            BindButton("ContinueButton", out var continueBtn, OnContinueClicked, out _onContinueClicked);
            ContinueButton = continueBtn;

            BindButton("SettingsButton", out var settings, OnSettingsClicked, out _onSettingsClicked);
            SettingsButton = settings;

            BindButton("QuitButton", out var quit, OnQuitClicked, out _onQuitClicked);
            QuitButton = quit;
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();

            // (a) Continue / Settings placeholder 灰态 (Sprint 7+ SaveSystem + SettingsPanel 接入后改 true)
            if (ContinueButton != null) ContinueButton.interactable = false;
            if (SettingsButton != null) SettingsButton.interactable = false;

            // (b) BGM start hook (per Phase 2.0 R2.8 [D] closure — fail-safe Log.Warning+no-op；真 BGM playback 留 ui-system-006b Sprint 7+)
            try
            {
                AudioManager.Instance.PlayMusic("main_menu_bgm", crossfadeDuration: 1.0f);
            }
            catch (Exception e)
            {
                // AudioManager.PlayMusic 内置 fail-safe (Log.Warning + return)；本 try-catch 仅防御 AudioManager.Instance 自身访问异常 (理论不应发生 per GameApp.cs:40 init order)
                Log.Warning($"[MainMenuPanel] OnRefresh BGM hook 异常 (AudioManager.Instance 访问失败？): {e.Message}");
            }

            // (c) Fade-in animation (CanvasGroup alpha 0→1 over 0.3s ease OutQuad)
            if (_canvasGroup != null)
            {
                _fadeInTween?.Kill(); // 防 OnRefresh 二次调用时叠加 Tween (vendor second show 'destroy-and-recreate' 模式下不会触发，但防御编程)
                // Ease 显式 qualify 防 TEngine.Ease vs DG.Tweening.Ease 名称歧义 (V3.0 §V3-1.b style 沿 InteractableObject.cs:336/383/475 precedent — 使用 `DG.Tweening.Ease.OutQuad` full qualified)
                _fadeInTween = _canvasGroup.DOFade(endValue: 1f, duration: 0.3f).SetEase(DG.Tweening.Ease.OutQuad);
            }
        }

        protected override void OnDestroy()
        {
            // DOTween Kill 防 panel 销毁后 Tween 仍持 _canvasGroup 引用 NRE
            _fadeInTween?.Kill();
            _fadeInTween = null;

            // 4 button RemoveListener + null-out + null-check guard (ADR-027 §5 framework knowledge fact)
            UnbindButton(NewGameButton, _onNewGameClicked);
            _onNewGameClicked = null;
            NewGameButton = null;

            UnbindButton(ContinueButton, _onContinueClicked);
            _onContinueClicked = null;
            ContinueButton = null;

            UnbindButton(SettingsButton, _onSettingsClicked);
            _onSettingsClicked = null;
            SettingsButton = null;

            UnbindButton(QuitButton, _onQuitClicked);
            _onQuitClicked = null;
            QuitButton = null;

            _canvasGroup = null;

            base.OnDestroy();
        }

        // ==== Button bind / unbind helpers (DRY) ====

        private void BindButton(string childName, out Button button, UnityEngine.Events.UnityAction handler, out UnityEngine.Events.UnityAction cachedDelegate)
        {
            button = null;
            cachedDelegate = null;

            var child = transform.Find(childName);
            if (child == null)
            {
                Log.Error($"[MainMenuPanel] BindButton: 未找到 prefab child '{childName}' — prefab 节点命名错误？");
                return;
            }

            button = child.GetComponent<Button>();
            if (button == null)
            {
                Log.Error($"[MainMenuPanel] BindButton: '{childName}' child 缺 Button 组件 — prefab 配置错误？");
                return;
            }

            cachedDelegate = handler;
            button.onClick.AddListener(cachedDelegate);
        }

        private void UnbindButton(Button button, UnityEngine.Events.UnityAction cachedDelegate)
        {
            if (button != null && cachedDelegate != null)
            {
                button.onClick.RemoveListener(cachedDelegate);
            }
        }

        // ==== 4 button onClick handlers ====

        private void OnNewGameClicked()
        {
            Log.Info("[MainMenuPanel] NewGameButton clicked → dispatch ISceneEvent.OnRequestSceneChange(1)");
            try
            {
                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
            }
            catch (Exception e)
            {
                Log.Error($"[MainMenuPanel] NewGameButton onClick 派发异常: {e}");
            }
        }

        private void OnContinueClicked()
        {
            // Placeholder per AC-8: 0-line body — Sprint 7+ SaveSystem epic ISaveService 接入后实施真路径
            // (button.interactable=false 灰态阻止点击；handler 0-line body 防 vendor 误触 / Editor Inspector 误调用 onClick)
            Log.Info("[MainMenuPanel] ContinueButton clicked (placeholder — Sprint 7+ SaveSystem)");
        }

        private void OnSettingsClicked()
        {
            // Placeholder per AC-8: 0-line body — Sprint 7+ ui-system-007 SettingsPanel 起步后实施真路径
            Log.Info("[MainMenuPanel] SettingsButton clicked (placeholder — Sprint 7+ SettingsPanel)");
        }

        private void OnQuitClicked()
        {
            Log.Info("[MainMenuPanel] QuitButton clicked → Application.Quit()");
            try
            {
                Application.Quit();
                // Editor mode: Unity 自身 handle PlayMode stop 行为 (Procedure stage 多处 production wired
                // 不需要显式 EditorApplication.isPlaying = false wrapper per Phase 1 R2.9 verify)
            }
            catch (Exception e)
            {
                Log.Error($"[MainMenuPanel] QuitButton onClick Application.Quit 异常: {e}");
            }
        }
    }
}
