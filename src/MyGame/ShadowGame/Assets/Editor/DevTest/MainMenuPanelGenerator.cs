// 该文件由Cursor 自动生成
// S6-07 dev-story Phase 2 (2026-05-14) — Main Menu Panel Prefab Generator (Editor only)
// (S5-02 dev-story Phase 2.3d 2026-05-12 inline 2 button base 完整替换升级)
//
// 关联文档:
//   * production/epics/ui-system/story-006-main-menu.md  (S6-07 narrow scope [A] 4 button group)
//   * Assets/GameScripts/HotFix/GameLogic/UI/MainMenuPanel.cs
//
// 用途:
//   一键生成 polish main menu prefab 到 Assets/Resources/UI/MainMenuPanel.prefab。
//   生成内容:
//     - Root GameObject "MainMenuPanel" (RectTransform 全屏 anchored) + Canvas + GraphicRaycaster +
//       Image 半透明背景 + **CanvasGroup (alpha=0 起始 — fade-in animation OnRefresh DOTween 0.3s 起手 from invisible)**
//     - Child "NewGameButton"  (RectTransform + Image + Button) — 主操作 chapter 1 start (绿主调强调)
//     - Child "ContinueButton" (RectTransform + Image + Button) — placeholder 灰 (Sprint 7+ SaveSystem)
//     - Child "SettingsButton" (RectTransform + Image + Button) — placeholder 灰 (Sprint 7+ ui-system-007 SettingsPanel)
//     - Child "QuitButton"     (RectTransform + Image + Button) — 退出 (橙警示)
//       (每 button + Grandchild "Text" — Button 显示文本)
//
// vendor wire:
//   `[Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]` →
//   UIModule 内部走 `Resources.Load<GameObject>("UI/MainMenuPanel")` + Instantiate 到 UIRoot
//
// 使用:
//   菜单 Tools → S6-07 → Generate Main Menu Panel Prefab；幂等可重复执行 (覆盖旧 prefab)
//
// 本文件不进 Build (Assets/Editor/ 自动 Editor-only 程序集)。
//
// Sprint 5 → Sprint 6 changes:
//   * 2 button (StartChapter1Button + NextChapterButton) → 4 button (NewGame + Continue + Settings + Quit)
//   * Button size 320x88 → 320x56 (紧凑 4 button vertical layout, 间距 64px, 总高度 ~440)
//   * Root +CanvasGroup component (fade-in baseline alpha=0)
//   * Continue + Settings button image color 设 gray (与 OnRefresh interactable=false 灰态视觉一致)
//   * MenuItem path: Tools/S5-02/... → Tools/S6-07/...

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Editor.DevTest
{
    public static class MainMenuPanelGenerator
    {
        private const string PrefabPath = "Assets/Resources/UI/MainMenuPanel.prefab";

        // 4 button vertical layout — 间距 64px，从中心上下排列
        private const float ButtonY_NewGame = 96f;
        private const float ButtonY_Continue = 32f;
        private const float ButtonY_Settings = -32f;
        private const float ButtonY_Quit = -96f;

        [MenuItem("Tools/S6-07/Generate Main Menu Panel Prefab")]
        public static void Generate()
        {
            EnsureDirectoryExists(PrefabPath);

            var root = BuildHierarchy();
            try
            {
                var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (success && saved != null)
                {
                    Debug.Log($"[S6-07 Prefab] Saved: {PrefabPath}");
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                }
                else
                {
                    Debug.LogError($"[S6-07 Prefab] SaveAsPrefabAsset failed: success={success}");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject BuildHierarchy()
        {
            var root = new GameObject("MainMenuPanel");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            // vendor 强制要求 panel root 含 Canvas component (UIWindow.cs:484-488)。
            // GraphicRaycaster 是 Button.onClick path raycast 前置 (4 button case 必需)。
            root.AddComponent<Canvas>();
            root.AddComponent<GraphicRaycaster>();

            // CanvasGroup — fade-in animation baseline (alpha=0 起始；MainMenuPanel.OnRefresh 内
            // CanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad) 推到 alpha=1)
            var canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.10f, 0.12f, 0.95f);
            bg.raycastTarget = true;

            // ---------- 4 button vertical group ----------

            // 1) NewGame Button — 主操作 chapter 1 start (绿主调强调)
            BuildButton(
                parent: root,
                name: "NewGameButton",
                anchoredPosition: new Vector2(0f, ButtonY_NewGame),
                labelText: "New Game",
                normalColor: new Color(0.30f, 0.65f, 0.40f, 1.0f));

            // 2) Continue Button — placeholder 灰 (Sprint 7+ SaveSystem epic ISaveService 接入)
            //   prefab image 直接设 gray 与 OnRefresh interactable=false 灰态视觉一致
            BuildButton(
                parent: root,
                name: "ContinueButton",
                anchoredPosition: new Vector2(0f, ButtonY_Continue),
                labelText: "Continue",
                normalColor: new Color(0.45f, 0.45f, 0.45f, 1.0f));

            // 3) Settings Button — placeholder 灰 (Sprint 7+ ui-system-007 SettingsPanel 起步)
            BuildButton(
                parent: root,
                name: "SettingsButton",
                anchoredPosition: new Vector2(0f, ButtonY_Settings),
                labelText: "Settings",
                normalColor: new Color(0.45f, 0.45f, 0.45f, 1.0f));

            // 4) Quit Button — 退出 (橙警示)
            BuildButton(
                parent: root,
                name: "QuitButton",
                anchoredPosition: new Vector2(0f, ButtonY_Quit),
                labelText: "Quit",
                normalColor: new Color(0.85f, 0.45f, 0.30f, 1.0f));

            return root;
        }

        private static void BuildButton(GameObject parent, string name, Vector2 anchoredPosition, string labelText, Color normalColor)
        {
            var buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent.transform, worldPositionStays: false);

            var btnRect = buttonGO.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            // S6-07 4 button vertical layout — size 紧凑 (S5-02 88px → 56px) 给 4 button group 间距
            btnRect.sizeDelta = new Vector2(320f, 56f);
            btnRect.anchoredPosition = anchoredPosition;

            var btnImage = buttonGO.AddComponent<Image>();
            btnImage.color = normalColor;

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = btnImage;

            var colors = button.colors;
            colors.normalColor = normalColor;
            var highlighted = normalColor;
            highlighted.r = Mathf.Min(1f, highlighted.r + 0.10f);
            highlighted.g = Mathf.Min(1f, highlighted.g + 0.10f);
            highlighted.b = Mathf.Min(1f, highlighted.b + 0.10f);
            colors.highlightedColor = highlighted;
            var pressed = normalColor;
            pressed.r = Mathf.Max(0f, pressed.r - 0.10f);
            pressed.g = Mathf.Max(0f, pressed.g - 0.10f);
            pressed.b = Mathf.Max(0f, pressed.b - 0.10f);
            colors.pressedColor = pressed;
            colors.selectedColor = highlighted;
            // disabledColor 用于 Continue/Settings interactable=false 灰态视觉
            var disabled = normalColor;
            disabled.r = Mathf.Max(0f, disabled.r * 0.6f);
            disabled.g = Mathf.Max(0f, disabled.g * 0.6f);
            disabled.b = Mathf.Max(0f, disabled.b * 0.6f);
            disabled.a = 0.5f;
            colors.disabledColor = disabled;
            button.colors = colors;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, worldPositionStays: false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.pivot = new Vector2(0.5f, 0.5f);

            var text = textGO.AddComponent<Text>();
            text.text = labelText;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;

            // Unity 2022 起 Arial.ttf builtin 被移除；LegacyRuntime.ttf 是 fallback
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            if (font != null)
            {
                text.font = font;
            }
            else
            {
                Debug.LogWarning("[S6-07 Prefab] LegacyRuntime.ttf / Arial.ttf builtin font 不可用；Text 可能不显示");
            }
        }

        private static void EnsureDirectoryExists(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(dir)) return;
            if (Directory.Exists(dir)) return;

            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }
    }
}
