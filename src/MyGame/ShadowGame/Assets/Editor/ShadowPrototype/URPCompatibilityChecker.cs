// 该文件由Cursor 自动生成
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace ShadowGame.Editor.Prototype
{
    /// <summary>
    /// 扫描项目中的材质、场景相机和预制体，检测 Built-in 管线残留和 URP 配置遗漏。
    /// 菜单入口: ShadowGame > Tools > Check URP Compatibility
    /// </summary>
    public static class URPCompatibilityChecker
    {
        private static readonly HashSet<string> UrpShaderPrefixes = new()
        {
            "Universal Render Pipeline",
            "Shader Graphs",
            "Hidden/Universal",
            "Sprites/Default",
            "UI/Default",
            "TextMeshPro",
            "Hidden/Internal",
            "Hidden/InternalErrorShader",
        };

        private static readonly HashSet<string> BuiltinShaderNames = new()
        {
            "Standard",
            "Standard (Specular setup)",
            "Mobile/Diffuse",
            "Mobile/Bumped Diffuse",
            "Mobile/Bumped Specular",
            "Mobile/Unlit (Supports Lightmap)",
            "Mobile/VertexLit",
            "Legacy Shaders/Diffuse",
            "Legacy Shaders/Specular",
            "Legacy Shaders/Bumped Diffuse",
            "Legacy Shaders/Bumped Specular",
            "Legacy Shaders/Transparent/Diffuse",
            "Nature/Tree Soft Occlusion Bark",
            "Nature/Tree Soft Occlusion Leaves",
            "Particles/Standard Surface",
            "Particles/Standard Unlit",
        };

        [MenuItem("ShadowGame/Tools/Check URP Compatibility", priority = 300)]
        public static void RunFullCheck()
        {
            var report = new List<string>();
            int issueCount = 0;

            report.Add("═══════════════════════════════════════════");
            report.Add("  URP 兼容性检查报告");
            report.Add("═══════════════════════════════════════════");
            report.Add("");

            issueCount += CheckMaterials(report);
            issueCount += CheckSceneCameras(report);
            issueCount += CheckPrefabCameras(report);
            issueCount += CheckGraphicsSettings(report);

            report.Add("───────────────────────────────────────────");
            if (issueCount == 0)
                report.Add("✓ 检查完成：未发现 URP 兼容性问题。");
            else
                report.Add($"✗ 检查完成：发现 {issueCount} 个问题，请逐项修复。");
            report.Add("───────────────────────────────────────────");

            string fullReport = string.Join("\n", report);
            Debug.Log(fullReport);

            if (issueCount > 0)
                EditorUtility.DisplayDialog(
                    "URP 兼容性检查",
                    $"发现 {issueCount} 个问题。\n详情请查看 Console 日志。",
                    "确定");
            else
                EditorUtility.DisplayDialog(
                    "URP 兼容性检查",
                    "全部通过，未发现 Built-in 管线残留或 URP 配置遗漏。",
                    "确定");
        }

        private static int CheckMaterials(List<string> report)
        {
            report.Add("【1/4】材质 Shader 检查");
            report.Add("  扫描 Assets/ 下所有 .mat 文件...");

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            int issues = 0;
            int total = 0;

            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null) continue;
                total++;

                string shaderName = mat.shader.name;

                if (BuiltinShaderNames.Contains(shaderName))
                {
                    report.Add($"  ✗ Built-in Shader: {path}");
                    report.Add($"    Shader: {shaderName}");
                    report.Add($"    → 需要转换为 URP/Lit 或其他 URP Shader");
                    issues++;
                    continue;
                }

                if (shaderName == "Hidden/InternalErrorShader")
                {
                    report.Add($"  ✗ 错误 Shader（紫红色）: {path}");
                    report.Add($"    → Shader 引用丢失，需要重新指定");
                    issues++;
                    continue;
                }

                bool isKnownUrp = UrpShaderPrefixes.Any(p => shaderName.StartsWith(p));
                if (!isKnownUrp && !shaderName.StartsWith("Hidden/"))
                {
                    report.Add($"  ? 未识别 Shader: {path}");
                    report.Add($"    Shader: {shaderName}");
                    report.Add($"    → 请确认此 Shader 兼容 URP");
                    issues++;
                }
            }

            report.Add($"  共扫描 {total} 个材质，发现 {issues} 个问题。");
            report.Add("");
            return issues;
        }

        private static int CheckSceneCameras(List<string> report)
        {
            report.Add("【2/4】场景相机 URP 数据检查");

            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            int issues = 0;

            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".unity")) continue;

                report.Add($"  场景: {path}");

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var rootObjects = scene.GetRootGameObjects();

                var cameras = rootObjects
                    .SelectMany(go => go.GetComponentsInChildren<Camera>(true))
                    .ToArray();

                foreach (var cam in cameras)
                {
                    string camPath = GetHierarchyPath(cam.transform);
                    var urpData = cam.GetComponent<UniversalAdditionalCameraData>();

                    if (urpData == null)
                    {
                        report.Add($"    ✗ 缺少 UniversalAdditionalCameraData: {camPath}");
                        report.Add($"      → 添加组件或通过 Inspector 配置 Render Type");
                        issues++;
                    }
                    else
                    {
                        string renderType = urpData.renderType.ToString();
                        report.Add($"    ✓ {camPath} (RenderType: {renderType})");
                    }
                }

                if (cameras.Length == 0)
                    report.Add("    （无相机）");

                EditorSceneManager.CloseScene(scene, true);
            }

            report.Add($"  发现 {issues} 个相机缺少 URP 数据。");
            report.Add("");
            return issues;
        }

        private static int CheckPrefabCameras(List<string> report)
        {
            report.Add("【3/4】预制体相机 URP 数据检查");

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int issues = 0;
            int cameraPrefabs = 0;

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var cameras = prefab.GetComponentsInChildren<Camera>(true);
                if (cameras.Length == 0) continue;
                cameraPrefabs++;

                foreach (var cam in cameras)
                {
                    string camPath = GetHierarchyPath(cam.transform);
                    var urpData = cam.GetComponent<UniversalAdditionalCameraData>();

                    if (urpData == null)
                    {
                        report.Add($"  ✗ {path}");
                        report.Add($"    相机: {camPath} — 缺少 UniversalAdditionalCameraData");
                        issues++;
                    }
                    else
                    {
                        string renderType = urpData.renderType.ToString();
                        report.Add($"  ✓ {path} → {camPath} (RenderType: {renderType})");
                    }
                }
            }

            report.Add($"  共扫描 {cameraPrefabs} 个含相机的预制体，发现 {issues} 个问题。");
            report.Add("");
            return issues;
        }

        private static int CheckGraphicsSettings(List<string> report)
        {
            report.Add("【4/4】渲染管线全局配置检查");
            int issues = 0;

            var currentPipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            if (currentPipeline == null)
            {
                report.Add("  ✗ GraphicsSettings.defaultRenderPipeline 为空 — 未配置 URP");
                issues++;
            }
            else if (currentPipeline is not UniversalRenderPipelineAsset)
            {
                report.Add($"  ✗ defaultRenderPipeline 不是 URP 类型: {currentPipeline.GetType().Name}");
                issues++;
            }
            else
            {
                report.Add($"  ✓ defaultRenderPipeline: {currentPipeline.name}");
            }

            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                var qPipeline = QualitySettings.renderPipeline;
                string qName = QualitySettings.names[i];

                if (qPipeline == null)
                {
                    report.Add($"  ✗ QualityLevel [{qName}] — renderPipeline 为空");
                    issues++;
                }
                else if (qPipeline is not UniversalRenderPipelineAsset)
                {
                    report.Add($"  ✗ QualityLevel [{qName}] — 非 URP 类型: {qPipeline.GetType().Name}");
                    issues++;
                }
                else
                {
                    report.Add($"  ✓ QualityLevel [{qName}]: {qPipeline.name}");
                }
            }

            report.Add($"  发现 {issues} 个配置问题。");
            report.Add("");
            return issues;
        }

        private static string GetHierarchyPath(Transform t)
        {
            var parts = new List<string>();
            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
#endif
