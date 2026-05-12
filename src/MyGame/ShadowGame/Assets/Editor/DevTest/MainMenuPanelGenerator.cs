// 该文件由Cursor 自动生成
// S5-02 dev-story Phase 2.3d (2026-05-12) — Main Menu Panel Prefab Generator (Editor only)
//
// 关联文档:
//   * production/epics/vs-chapter-1/story-002-end-to-end-flow.md  (AC-1 + AC-2 + AC-7 + R3 P1 + P5)
//   * Assets/GameScripts/HotFix/GameLogic/UI/MainMenuPanel.cs
//
// 用途:
//   一键生成 minimal main menu prefab 到 Assets/Resources/UI/MainMenuPanel.prefab。
//   生成内容:
//     - Root GameObject "MainMenuPanel" (RectTransform 全屏 anchored) + Canvas + GraphicRaycaster + Image 半透明背景
//     - Child "StartChapter1Button" (RectTransform + Image + Button) — S5-02 R3 P1 case onClick.Invoke 目标
//       + Grandchild "Text" ("Start Chapter 1") — Button 显示文本
//     - Child "NextChapterButton" (RectTransform + Image + Button) — S5-02 R3 P5 case onClick.Invoke 目标
//       + Grandchild "Text" ("Next Chapter") — Button 显示文本
//
// vendor wire:
//   `[Window(UILayer.UI, fromResources: true, location: "UI/MainMenuPanel")]` →
//   UIModule 内部走 `Resources.Load<GameObject>("UI/MainMenuPanel")` + Instantiate 到 UIRoot
//
// 使用:
//   菜单 Tools → S5-02 → Generate Main Menu Panel Prefab；幂等可重复执行 (覆盖旧 prefab)
//
// 本文件不进 Build (Assets/Editor/ 自动 Editor-only 程序集)。

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Editor.DevTest
{
    public static class MainMenuPanelGenerator
    {
        private const string PrefabPath = "Assets/Resources/UI/MainMenuPanel.prefab";

        [MenuItem("Tools/S5-02/Generate Main Menu Panel Prefab")]
        public static void Generate()
        {
            EnsureDirectoryExists(PrefabPath);

            var root = BuildHierarchy();
            try
            {
                var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (success && saved != null)
                {
                    Debug.Log($"[S5-02 Prefab] Saved: {PrefabPath}");
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                }
                else
                {
                    Debug.LogError($"[S5-02 Prefab] SaveAsPrefabAsset failed: success={success}");
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
            // ---------- Root ----------
            // vendor 强制要求 panel root 含 Canvas component (UIWindow.cs:484-488)。
            // GraphicRaycaster 是 Button.onClick path raycast 前置 (P1/P5 case 必需)。
            var root = new GameObject("MainMenuPanel");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            root.AddComponent<Canvas>();
            root.AddComponent<GraphicRaycaster>();

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.10f, 0.12f, 0.95f);
            bg.raycastTarget = true;

            // ---------- StartChapter1 Button (上方) ----------
            BuildButton(
                parent: root,
                name: "StartChapter1Button",
                anchoredPosition: new Vector2(0f, 60f),
                labelText: "Start Chapter 1",
                normalColor: new Color(0.30f, 0.55f, 0.85f, 1.0f));

            // ---------- NextChapter Button (下方) ----------
            BuildButton(
                parent: root,
                name: "NextChapterButton",
                anchoredPosition: new Vector2(0f, -60f),
                labelText: "Next Chapter",
                normalColor: new Color(0.55f, 0.45f, 0.75f, 1.0f));

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
            btnRect.sizeDelta = new Vector2(320f, 88f);
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
            text.fontSize = 26;
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
                Debug.LogWarning("[S5-02 Prefab] LegacyRuntime.ttf / Arial.ttf builtin font 不可用；Text 可能不显示");
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
