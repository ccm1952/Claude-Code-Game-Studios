// 该文件由Cursor 自动生成
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.IO;

namespace ShadowGame.Editor.Prototype
{
    /// <summary>
    /// One-click setup for the URP Shadow Prototype.
    /// Creates URP Pipeline Assets, configures Graphics/Quality settings,
    /// and builds the shadow test scene.
    /// </summary>
    public static class ShadowPrototypeSetup
    {
        private const string URPAssetDir = "Assets/Settings/URP";
        private const string SceneDir = "Assets/Scenes";
        private const string ScenePath = "Assets/Scenes/ShadowPrototype.unity";

        #region Menu Items

        [MenuItem("ShadowGame/Prototype/1. Setup URP Pipeline", priority = 100)]
        public static void SetupURPPipeline()
        {
            CreateURPAssets();
            ConfigureGraphicsSettings();
            ConfigureQualitySettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ShadowPrototype] URP Pipeline setup complete.");
        }

        [MenuItem("ShadowGame/Prototype/2. Build Shadow Test Scene", priority = 101)]
        public static void BuildShadowTestScene()
        {
            var scene = CreateTestScene();
            BuildRoom(scene);
            BuildLights();
            BuildInteractableObjects();
            BuildShadowSampleCamera();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[ShadowPrototype] Test scene saved to {ScenePath}");
        }

        [MenuItem("ShadowGame/Prototype/3. Setup Interaction System", priority = 102)]
        public static void SetupInteractionSystem()
        {
            SetupInteractableLayer();
            AttachInteractionComponents();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShadowPrototype] Interaction system setup complete.");
        }

        [MenuItem("ShadowGame/Prototype/Run Full Setup (1+2+3)", priority = 200)]
        public static void RunFullSetup()
        {
            SetupURPPipeline();
            BuildShadowTestScene();
            SetupInteractionSystem();
            Debug.Log("[ShadowPrototype] Full setup complete. Enter Play Mode to test.");
        }

        #endregion

        #region Phase 1 — URP Assets & Pipeline Config

        private static void CreateURPAssets()
        {
            if (!Directory.Exists(URPAssetDir))
                Directory.CreateDirectory(URPAssetDir);

            CreateURPAsset("ShadowGame_URP_Low", low: true);
            CreateURPAsset("ShadowGame_URP_Medium", medium: true);
            CreateURPAsset("ShadowGame_URP_High", high: true);
        }

        private static void CreateURPAsset(string name, bool low = false, bool medium = false, bool high = false)
        {
            string path = $"{URPAssetDir}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path) != null)
            {
                Debug.Log($"[ShadowPrototype] URP Asset already exists: {path}");
                return;
            }

            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            string rendererPath = $"{URPAssetDir}/{name}_Renderer.asset";
            AssetDatabase.CreateAsset(rendererData, rendererPath);

            var asset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(asset, path);

            asset.renderScale = 1.0f;
            asset.supportsCameraOpaqueTexture = false;
            asset.supportsCameraDepthTexture = true;
            asset.supportsHDR = false;
            asset.msaaSampleCount = 1;

            var so = new SerializedObject(asset);

            so.FindProperty("m_MainLightRenderingMode").intValue = (int)LightRenderingMode.PerPixel;
            so.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            so.FindProperty("m_AdditionalLightsRenderingMode").intValue = (int)LightRenderingMode.PerPixel;
            so.FindProperty("m_AdditionalLightShadowsSupported").boolValue = true;

            if (low)
            {
                so.FindProperty("m_MainLightShadowmapResolution").intValue = 1024;
                so.FindProperty("m_AdditionalLightsShadowmapResolution").intValue = 512;
                asset.shadowCascadeCount = 1;
                asset.shadowDistance = 6f;
                so.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = 2;
                so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            }
            else if (medium)
            {
                so.FindProperty("m_MainLightShadowmapResolution").intValue = 2048;
                so.FindProperty("m_AdditionalLightsShadowmapResolution").intValue = 1024;
                asset.shadowCascadeCount = 2;
                asset.shadowDistance = 6f;
                so.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = 4;
                so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            }
            else if (high)
            {
                so.FindProperty("m_MainLightShadowmapResolution").intValue = 4096;
                so.FindProperty("m_AdditionalLightsShadowmapResolution").intValue = 2048;
                asset.shadowCascadeCount = 4;
                asset.shadowDistance = 6f;
                so.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = 4;
                so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            Debug.Log($"[ShadowPrototype] Created URP Asset: {path}");
        }

        private static void ConfigureGraphicsSettings()
        {
            var mediumAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                $"{URPAssetDir}/ShadowGame_URP_Medium.asset");

            if (mediumAsset == null)
            {
                Debug.LogError("[ShadowPrototype] Medium URP Asset not found. Run asset creation first.");
                return;
            }

            GraphicsSettings.defaultRenderPipeline = mediumAsset;
            Debug.Log("[ShadowPrototype] GraphicsSettings: default pipeline set to Medium.");
        }

        private static void ConfigureQualitySettings()
        {
            var lowAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                $"{URPAssetDir}/ShadowGame_URP_Low.asset");
            var mediumAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                $"{URPAssetDir}/ShadowGame_URP_Medium.asset");
            var highAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                $"{URPAssetDir}/ShadowGame_URP_High.asset");

            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);

                switch (names[i])
                {
                    case "Very Low":
                    case "Low":
                        QualitySettings.renderPipeline = lowAsset;
                        break;
                    case "Medium":
                        QualitySettings.renderPipeline = mediumAsset;
                        break;
                    case "High":
                    case "Very High":
                    case "Ultra":
                        QualitySettings.renderPipeline = highAsset;
                        break;
                    default:
                        QualitySettings.renderPipeline = mediumAsset;
                        break;
                }
            }

            Debug.Log("[ShadowPrototype] QualitySettings: URP Assets bound to all tiers.");
        }

        #endregion

        #region Phase 2 — Build Test Scene

        private static Scene CreateTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ShadowPrototype";

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.15f, 0.15f, 0.17f, 1f);

            return scene;
        }

        private static void BuildRoom(Scene scene)
        {
            var roomRoot = new GameObject("Room");

            var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "ProjectionWall";
            wall.transform.SetParent(roomRoot.transform);
            wall.transform.localPosition = new Vector3(0f, 2f, 3f);
            wall.transform.localScale = new Vector3(6f, 4f, 1f);
            wall.transform.localRotation = Quaternion.identity;
            SetupReceiverMaterial(wall, new Color(0.96f, 0.94f, 0.91f)); // #F5F0E8

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(roomRoot.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(1f, 1f, 0.6f);
            SetupReceiverMaterial(floor, new Color(0.85f, 0.82f, 0.78f));

            var leftWall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            leftWall.name = "LeftWall";
            leftWall.transform.SetParent(roomRoot.transform);
            leftWall.transform.localPosition = new Vector3(-3f, 2f, 1.5f);
            leftWall.transform.localScale = new Vector3(3f, 4f, 1f);
            leftWall.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            SetupReceiverMaterial(leftWall, new Color(0.9f, 0.88f, 0.85f));

            var rightWall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            rightWall.name = "RightWall";
            rightWall.transform.SetParent(roomRoot.transform);
            rightWall.transform.localPosition = new Vector3(3f, 2f, 1.5f);
            rightWall.transform.localScale = new Vector3(3f, 4f, 1f);
            rightWall.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            SetupReceiverMaterial(rightWall, new Color(0.9f, 0.88f, 0.85f));
        }

        private static void SetupReceiverMaterial(GameObject go, Color baseColor)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_Metallic", 0f);
            renderer.sharedMaterial = mat;

            string matDir = "Assets/Settings/URP/Materials";
            if (!Directory.Exists(matDir))
                Directory.CreateDirectory(matDir);

            string matPath = $"{matDir}/{go.name}_Mat.mat";
            AssetDatabase.CreateAsset(mat, matPath);
        }

        private static void BuildLights()
        {
            var lightRoot = new GameObject("Lights");

            var dirLightGO = new GameObject("MainDirectionalLight");
            dirLightGO.transform.SetParent(lightRoot.transform);
            dirLightGO.transform.position = new Vector3(0f, 4f, 0f);
            dirLightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var dirLight = dirLightGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1f, 0.96f, 0.88f);
            dirLight.intensity = 1.2f;
            dirLight.shadows = LightShadows.Soft;
            dirLight.shadowBias = 0.05f;
            dirLight.shadowNormalBias = 0.4f;
            dirLight.shadowResolution = LightShadowResolution.FromQualitySettings;

            var spotLightGO = new GameObject("SpotLight_Lamp");
            spotLightGO.transform.SetParent(lightRoot.transform);
            spotLightGO.transform.position = new Vector3(-1.5f, 2.5f, 1f);
            spotLightGO.transform.rotation = Quaternion.Euler(45f, 30f, 0f);
            var spotLight = spotLightGO.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = new Color(1f, 0.92f, 0.75f);
            spotLight.intensity = 2.5f;
            spotLight.range = 6f;
            spotLight.spotAngle = 60f;
            spotLight.shadows = LightShadows.Soft;
            spotLight.shadowBias = 0.05f;
            spotLight.shadowNormalBias = 0.4f;

            var mainCamGO = new GameObject("MainCamera");
            mainCamGO.tag = "MainCamera";
            mainCamGO.transform.position = new Vector3(0f, 3.2f, -1.5f);
            mainCamGO.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
            var cam = mainCamGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;

            mainCamGO.AddComponent<UniversalAdditionalCameraData>();
        }

        private static void BuildInteractableObjects()
        {
            var objRoot = new GameObject("InteractableObjects");

            CreateShadowCaster(PrimitiveType.Cube, "Cup",
                new Vector3(-0.5f, 0.75f, 1.5f), new Vector3(0.4f, 0.6f, 0.4f), objRoot.transform);

            CreateShadowCaster(PrimitiveType.Cylinder, "LampBase",
                new Vector3(0.5f, 0.5f, 1.8f), new Vector3(0.3f, 0.5f, 0.3f), objRoot.transform);

            CreateShadowCaster(PrimitiveType.Sphere, "Apple",
                new Vector3(0f, 0.35f, 1.2f), new Vector3(0.35f, 0.35f, 0.35f), objRoot.transform);

            CreateShadowCaster(PrimitiveType.Capsule, "Vase",
                new Vector3(1f, 0.6f, 2f), new Vector3(0.25f, 0.6f, 0.25f), objRoot.transform);

            CreateShadowCaster(PrimitiveType.Cube, "Book",
                new Vector3(-1f, 0.15f, 1.8f), new Vector3(0.6f, 0.08f, 0.4f), objRoot.transform);

            CreateShadowCaster(PrimitiveType.Cube, "Chair",
                new Vector3(-1.5f, 0.45f, 1.0f), new Vector3(0.5f, 0.9f, 0.5f), objRoot.transform);

            CreateShadowCaster(PrimitiveType.Capsule, "Umbrella",
                new Vector3(1.5f, 0.7f, 1.3f), new Vector3(0.15f, 0.7f, 0.15f), objRoot.transform);

            CreateShadowCaster(PrimitiveType.Cube, "PhotoFrame",
                new Vector3(0.8f, 0.55f, 1.0f), new Vector3(0.5f, 0.4f, 0.06f), objRoot.transform);

            CreateShadowCaster(PrimitiveType.Sphere, "LampShade",
                new Vector3(-0.8f, 1.0f, 2.0f), new Vector3(0.5f, 0.3f, 0.5f), objRoot.transform);

            CreateShadowCaster(PrimitiveType.Cylinder, "Candle",
                new Vector3(0.2f, 0.35f, 2.2f), new Vector3(0.12f, 0.35f, 0.12f), objRoot.transform);
        }

        private static void CreateShadowCaster(PrimitiveType type, string name,
            Vector3 position, Vector3 scale, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", Random.ColorHSV(0f, 1f, 0.3f, 0.6f, 0.5f, 0.8f));
            mat.SetFloat("_Smoothness", 0.3f);
            mat.SetFloat("_Metallic", 0f);
            renderer.sharedMaterial = mat;

            string matDir = "Assets/Settings/URP/Materials";
            string matPath = $"{matDir}/{name}_Mat.mat";
            AssetDatabase.CreateAsset(mat, matPath);
        }

        private static void BuildShadowSampleCamera()
        {
            var camGO = new GameObject("ShadowSampleCamera");
            camGO.transform.position = new Vector3(0f, 2f, 2.9f);
            camGO.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 2f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 0.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.white;
            cam.depth = -10;
            cam.enabled = false;

            var additionalData = camGO.AddComponent<UniversalAdditionalCameraData>();
            additionalData.renderPostProcessing = false;

            var rt = new RenderTexture(1024, 1024, 0, RenderTextureFormat.R8);
            rt.name = "ShadowRT";
            string rtPath = $"{URPAssetDir}/ShadowRT.renderTexture";
            AssetDatabase.CreateAsset(rt, rtPath);
            cam.targetTexture = rt;

            Debug.Log($"[ShadowPrototype] ShadowSampleCamera created with RT at {rtPath}");
        }

        #endregion

        #region Phase 3 — Interaction System Setup

        private static void SetupInteractableLayer()
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
            SerializedProperty layers = tagManager.FindProperty("layers");

            bool found = false;
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == "InteractableObject")
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                for (int i = 8; i < layers.arraySize; i++)
                {
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                    {
                        layers.GetArrayElementAtIndex(i).stringValue = "InteractableObject";
                        tagManager.ApplyModifiedProperties();
                        Debug.Log($"[ShadowPrototype] Created layer 'InteractableObject' at index {i}");
                        break;
                    }
                }
            }
        }

        private static void AttachInteractionComponents()
        {
            int interactableLayer = LayerMask.NameToLayer("InteractableObject");
            if (interactableLayer < 0)
            {
                Debug.LogError("[ShadowPrototype] InteractableObject layer not found. Run SetupInteractableLayer first.");
                return;
            }

            string configDir = "Assets/Settings/Prototype";
            if (!Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);

            string configPath = $"{configDir}/InteractionConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<ShadowGame.Prototype.InteractionConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ShadowGame.Prototype.InteractionConfig>();
                AssetDatabase.CreateAsset(config, configPath);
                Debug.Log($"[ShadowPrototype] Created InteractionConfig at {configPath}");
            }

            var objRoot = GameObject.Find("InteractableObjects");
            if (objRoot == null)
            {
                Debug.LogError("[ShadowPrototype] InteractableObjects root not found. Build test scene first.");
                return;
            }

            foreach (Transform child in objRoot.transform)
            {
                child.gameObject.layer = interactableLayer;

                if (child.GetComponent<Collider>() == null)
                    child.gameObject.AddComponent<BoxCollider>();

                var interactable = child.GetComponent<ShadowGame.Prototype.InteractableObject>();
                if (interactable == null)
                    interactable = child.gameObject.AddComponent<ShadowGame.Prototype.InteractableObject>();

                EditorUtility.SetDirty(child.gameObject);
            }

            var gridGO = GameObject.Find("InteractionSystem");
            if (gridGO == null)
            {
                gridGO = new GameObject("InteractionSystem");
            }

            var gridSystem = gridGO.GetComponent<ShadowGame.Prototype.GridSystem>();
            if (gridSystem == null)
                gridSystem = gridGO.AddComponent<ShadowGame.Prototype.GridSystem>();

            var gridSO = new SerializedObject(gridSystem);
            gridSO.FindProperty("_config").objectReferenceValue = config;
            gridSO.ApplyModifiedProperties();

            var controller = gridGO.GetComponent<ShadowGame.Prototype.InteractionController>();
            if (controller == null)
                controller = gridGO.AddComponent<ShadowGame.Prototype.InteractionController>();

            var cam = GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
            var controllerSO = new SerializedObject(controller);
            controllerSO.FindProperty("_mainCamera").objectReferenceValue = cam;
            controllerSO.FindProperty("_gridSystem").objectReferenceValue = gridSystem;
            controllerSO.FindProperty("_interactableMask").intValue = 1 << interactableLayer;
            controllerSO.ApplyModifiedProperties();

            var debugPanel = gridGO.GetComponent<ShadowGame.Prototype.InteractionDebugPanel>();
            if (debugPanel == null)
                debugPanel = gridGO.AddComponent<ShadowGame.Prototype.InteractionDebugPanel>();

            var debugSO = new SerializedObject(debugPanel);
            debugSO.FindProperty("_controller").objectReferenceValue = controller;
            debugSO.FindProperty("_gridSystem").objectReferenceValue = gridSystem;
            debugSO.FindProperty("_config").objectReferenceValue = config;
            debugSO.ApplyModifiedProperties();

            foreach (Transform child in objRoot.transform)
            {
                var interactable = child.GetComponent<ShadowGame.Prototype.InteractableObject>();
                if (interactable != null)
                {
                    var objSO = new SerializedObject(interactable);
                    var radiusProp = objSO.FindProperty("_colliderRadius");
                    if (radiusProp != null)
                    {
                        var col = child.GetComponent<Collider>();
                        if (col is BoxCollider box)
                            radiusProp.floatValue = Mathf.Max(box.size.x, box.size.z) * child.lossyScale.x * 0.5f;
                        else if (col is SphereCollider sphere)
                            radiusProp.floatValue = sphere.radius * child.lossyScale.x;
                        else
                            radiusProp.floatValue = 0.15f;
                        objSO.ApplyModifiedProperties();
                    }
                }
            }

            EditorUtility.SetDirty(gridGO);
            Debug.Log($"[ShadowPrototype] Interaction components attached to {objRoot.transform.childCount} objects.");
        }

        #endregion
    }
}
#endif
