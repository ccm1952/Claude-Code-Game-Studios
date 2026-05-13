// 该文件由Cursor 自动生成
// S6-08 Mock Panels Prefab Generator (Editor only, Sprint 6 spike fixture)
//
// 关联文档:
//   * production/epics/ui-system/story-008-ui-layer-strategy.md  (AC-1~AC-9 + R3 P1~P5)
//   * Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S6_08_MockPanels.cs  (7 mock class)
//
// 用途:
//   一键批量生成 spike 用的 7 个 mock panel prefab 到 Assets/Resources/UI/：
//     - MockTopPanel.prefab / MockTopPanel2.prefab  (UILayer.Top)
//     - MockTipsPanelA.prefab / MockTipsPanelB.prefab / MockTipsPanelC.prefab  (UILayer.Tips)
//     - MockBottomPanel.prefab  (UILayer.Bottom)
//     - MockSystemPanel.prefab  (UILayer.System)
//
//   每个 prefab hierarchy: Root (Canvas + GraphicRaycaster + Image) — 仅 vendor InternalLoad 最低需求,
//   无 Button child (S6-08 spike 直接调 CloseUI<T>() 不依赖 user click 触发)。
//
// vendor wire:
//   `[Window(UILayer.X, fromResources: true, location: "UI/MockXxx")]` →
//   UIModule 内部走 `Resources.Load<GameObject>("UI/MockXxx")` + Instantiate 到 UIRoot
//
// 使用:
//   菜单 Tools → S6-08 → Generate Mock Panel Prefabs (All)；幂等可重复执行 (覆盖旧 prefab)
//
// 本文件不进 Build (Assets/Editor/ 自动 Editor-only 程序集)。

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Editor.DevTest
{
    public static class S6_08_MockPanelsGenerator
    {
        private const string PrefabRoot = "Assets/Resources/UI/";

        private static readonly (string Name, Color BgColor)[] Panels = new[]
        {
            ("MockTopPanel",     new Color(0.85f, 0.30f, 0.30f, 0.50f)), // Top      — 红
            ("MockTopPanel2",    new Color(0.90f, 0.50f, 0.30f, 0.50f)), // Top      — 橙
            ("MockTipsPanelA",   new Color(0.30f, 0.85f, 0.30f, 0.50f)), // Tips     — 绿
            ("MockTipsPanelB",   new Color(0.30f, 0.85f, 0.55f, 0.50f)), // Tips     — 青绿
            ("MockTipsPanelC",   new Color(0.55f, 0.85f, 0.30f, 0.50f)), // Tips     — 黄绿
            ("MockBottomPanel",  new Color(0.30f, 0.30f, 0.85f, 0.50f)), // Bottom   — 蓝
            ("MockSystemPanel",  new Color(0.85f, 0.85f, 0.30f, 0.50f)), // System   — 黄
        };

        [MenuItem("Tools/S6-08/Generate Mock Panel Prefabs (All)")]
        public static void GenerateAll()
        {
            EnsureDirectoryExists(PrefabRoot);

            int success = 0;
            int failed = 0;
            foreach (var (name, color) in Panels)
            {
                if (GenerateOne(name, color))
                {
                    success++;
                }
                else
                {
                    failed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[S6-08 Prefab] Generate done: success={success} failed={failed} total={Panels.Length}");
            EditorUtility.FocusProjectWindow();
        }

        private static bool GenerateOne(string panelName, Color bgColor)
        {
            string prefabPath = PrefabRoot + panelName + ".prefab";

            var root = BuildHierarchy(panelName, bgColor);
            bool success = false;
            try
            {
                var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
                if (success && saved != null)
                {
                    Debug.Log($"[S6-08 Prefab] Saved: {prefabPath}");
                }
                else
                {
                    Debug.LogError($"[S6-08 Prefab] SaveAsPrefabAsset failed for {panelName}: success={success}");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            return success;
        }

        private static GameObject BuildHierarchy(string panelName, Color bgColor)
        {
            // vendor 强制要求 panel root 含 Canvas component (UIWindow.cs:484-488)
            // GraphicRaycaster 仅为 vendor expected component 完整性（本 S6-08 mock 无 user click）
            var root = new GameObject(panelName);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            root.AddComponent<Canvas>();
            root.AddComponent<GraphicRaycaster>();

            var bg = root.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = true;

            return root;
        }

        private static void EnsureDirectoryExists(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath)) return;
            if (Directory.Exists(dirPath)) return;

            Directory.CreateDirectory(dirPath);
            AssetDatabase.Refresh();
        }
    }
}
