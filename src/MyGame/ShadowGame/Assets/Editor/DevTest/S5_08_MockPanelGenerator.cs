// 该文件由Cursor 自动生成
// S5-08 Mock Minimal Panel Prefab Generator (Editor only, Sprint 5 spike fixture)
//
// 关联文档:
//   * production/epics/ui-system/story-001-uimodule-setup.md  (AC-4 + AC-8 + R3 P2 + P4)
//   * Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5_08_MockMinimalPanel.cs
//
// 用途:
//   一键生成 spike 用的 mock panel prefab 到 Assets/Resources/UI/S5_08_MockMinimalPanel.prefab。
//   生成内容:
//     - Root GameObject "S5_08_MockMinimalPanel" (RectTransform 全屏 anchored) + Image 半透明背景
//     - Child "MockButton" (RectTransform + Image + Button) — spike P4 case onClick.Invoke 目标
//     - Grandchild "Text" (RectTransform + Text "S5-08 Mock Button") — Button 显示文本
//
// vendor wire:
//   `[Window(UILayer.UI, fromResources: true, location: "UI/S5_08_MockMinimalPanel")]` →
//   UIModule 内部走 `Resources.Load<GameObject>("UI/S5_08_MockMinimalPanel")` + Instantiate 到 UIRoot
//
// 使用:
//   菜单 Tools → S5-08 → Generate Mock Panel Prefab；幂等可重复执行 (覆盖旧 prefab)
//
// 本文件不进 Build (Assets/Editor/ 自动 Editor-only 程序集)。

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Editor.DevTest
{
    public static class S5_08_MockPanelGenerator
    {
        private const string PrefabPath = "Assets/Resources/UI/S5_08_MockMinimalPanel.prefab";

        [MenuItem("Tools/S5-08/Generate Mock Panel Prefab")]
        public static void Generate()
        {
            EnsureDirectoryExists(PrefabPath);

            var root = BuildHierarchy();
            try
            {
                var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (success && saved != null)
                {
                    Debug.Log($"[S5-08 Prefab] Saved: {PrefabPath}");
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                }
                else
                {
                    Debug.LogError($"[S5-08 Prefab] SaveAsPrefabAsset failed: success={success}");
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
            // vendor 强制要求 panel root 含 Canvas component (UIWindow.cs:484-488:
            // `_canvas = _panel.GetComponent<Canvas>(); if (_canvas == null) throw Exception("Not found Canvas in panel ...")`)
            // GraphicRaycaster 是 Button.onClick path raycast 前置 (P4 case 必需)
            var root = new GameObject("S5_08_MockMinimalPanel");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            // sub-Canvas (vendor 用 overrideSorting=true 接 UIRoot 主 Canvas 下；本 prefab 仅 AddComponent,
            // vendor InternalLoad → Handle_Completed 内会 set overrideSorting + sortingOrder)
            root.AddComponent<Canvas>();
            root.AddComponent<GraphicRaycaster>();

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.12f, 0.15f, 0.75f);
            bg.raycastTarget = true; // 阻断点透防 P4 mis-click

            // ---------- Button child ----------
            var buttonGO = new GameObject("MockButton");
            buttonGO.transform.SetParent(root.transform, worldPositionStays: false);

            var btnRect = buttonGO.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(280f, 80f);
            btnRect.anchoredPosition = Vector2.zero;

            var btnImage = buttonGO.AddComponent<Image>();
            btnImage.color = new Color(0.25f, 0.55f, 0.85f, 1.0f);

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = btnImage;

            // pressed/highlight colors 微调便于 PlayMode 视觉验证
            var colors = button.colors;
            colors.normalColor = new Color(0.25f, 0.55f, 0.85f, 1.0f);
            colors.highlightedColor = new Color(0.35f, 0.65f, 0.95f, 1.0f);
            colors.pressedColor = new Color(0.18f, 0.40f, 0.70f, 1.0f);
            colors.selectedColor = new Color(0.30f, 0.60f, 0.90f, 1.0f);
            button.colors = colors;

            // ---------- Text grandchild ----------
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, worldPositionStays: false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.pivot = new Vector2(0.5f, 0.5f);

            var text = textGO.AddComponent<Text>();
            text.text = "S5-08 Mock Button";
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
                Debug.LogWarning("[S5-08 Prefab] LegacyRuntime.ttf / Arial.ttf builtin font 不可用；Text 可能不显示 (mock spike P4 不依赖 font 渲染)");
            }

            return root;
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
