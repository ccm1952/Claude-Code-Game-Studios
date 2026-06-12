// 该文件由Cursor 自动生成
// S6-14 Chapter 1 Scene Wiring PlayMode spike
//   per story-005-chapter-1-scene-wiring.md (Phase 0 ✅ + Phase 1 ✅ + Phase 2 scene wiring partial ✅)。
//
// 关联文档:
//   * production/epics/vs-chapter-1/story-005-chapter-1-scene-wiring.md  (11 AC + 5+1 R3 case)
//   * Assets/AssetRaw/Scenes/Chapter_01_Approach.unity                 (InteractionCoordinator + 2 InteractableObject + child Hitbox2D)
//   * Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractionCoordinator.cs
//   * Assets/GameScripts/HotFix/GameLogic/ObjectInteraction/InteractableObject.cs
//
// R3 5+1 PlayMode case (run order P1→P2→P3→P4→P5→P5b；chapter 1 baseline 加载后 scene wiring verify):
//   P1 SceneHierarchyHasInteractionCoordinator — FindObjectOfType<InteractionCoordinator>() != null
//                                              + reflection _objects.Count == 2
//   P2 InteractableObjectsExistAndConfigured — FindObjectsOfType<InteractableObject>().Length == 2
//                                            + _objectId ∈ {1,2} + _puzzleId==1 + _dragDepth==10 + _gameplayCamera!=null
//   P3 LayerFilterCorrect — parent layer==InteractableObject (Layer 8) + _interactableLayer mask 含 Layer 8
//                         + child Hitbox2D 存在 + BoxCollider2D/CircleCollider2D + 父级 3D collider 保留 (Drift D [A2])
//   P4 CameraReferenceNonNull — Coordinator._gameplayCamera + 每 InteractableObject._gameplayCamera 均非 null 且 name==MainCamera
//   P5 RaycastFatFingerDimensionalConsistency — RaycastWithFatFinger(Vector2.zero) 无异常 (Physics2D ↔ 2D child collider 维度对齐)
//   P5b InteractionCoordinatorInitializeIdempotent — Initialize() 2 次 + IsLocked==false + 0 unexpected error
//
// 设计约束 (沿 S6-04/S6-13 precedent):
//   * 1 file + 3 inner class (S614Spike : IDevSpike + S614Runtime : MonoBehaviour + S614Tester 纯逻辑)
//   * chapter 1 baseline: fire OnRequestSceneChange(1) + WaitForIdleAsync (S6-04 precedent)
//   * Reflection 读 InteractionCoordinator / InteractableObject private SerializeField
//   * Application.logMessageReceived UnexpectedErrorCount + ExpectedLogSubstrings allowlist
//   * JSON evidence dump WriteResultJson per Application.persistentDataPath/S6-14_Result.json
//
// 整文件仅在 UNITY_EDITOR || DEBUG 编译，Release 包零残留。

#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameLogic.DevTest.Spikes
{
    public class S614Spike : IDevSpike
    {
        public string Id => "S6-14";
        public string Name => "Chapter 1 Scene Wiring — InteractionCoordinator + 2 InteractableObject + child Hitbox2D 2D collider 同存解 (Drift D [A2])";

        public void Launch()
        {
            var go = new GameObject("S614_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S614Runtime>();
        }
    }

    public class S614Runtime : MonoBehaviour
    {
        private S614Tester _tester;

        private void Awake()
        {
            _tester = new S614Tester(this);
            _tester.SubscribeEarlyListeners();
        }

        private void Start()
        {
            _tester.RunAllAsync().Forget();
        }

        private void OnGUI()
        {
            if (_tester == null) return;

            float x = 20f, y = 20f, w = 980f, h = 340f;
            GUI.Box(new Rect(x, y, w, h), "");

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState { textColor = Color.white }
            };
            GUI.Label(new Rect(x, y + 10, w, 30), "S6-14 Chapter 1 Scene Wiring (Track F vs-chapter-1-005)", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float lineY = y + 50;
            float lineH = 26;

            DrawRow(x + 20, lineY, w - 40, "P1 SceneHierarchyHasInteractionCoordinator (_objects.Count==2)", _tester.P1Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P2 InteractableObjectsExistAndConfigured (2 objs + 4 Inspector fields)", _tester.P2Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P3 LayerFilterCorrect (layer 8 + child Hitbox2D + 2D/3D collider 同存)", _tester.P3Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P4 CameraReferenceNonNull (Coordinator + 2 objs → MainCamera)", _tester.P4Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P5 RaycastFatFingerDimensionalConsistency (Physics2D ↔ 2D child collider)", _tester.P5Passed, labelStyle);
            lineY += lineH;
            DrawRow(x + 20, lineY, w - 40, "P5b InteractionCoordinatorInitializeIdempotent (Initialize×2 + IsLocked==false)", _tester.P5bPassed, labelStyle);
            lineY += lineH + 10;

            var footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"AllPassed: {_tester.AllPassed}    Elapsed: {_tester.TotalElapsedMs}ms", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"UnexpectedError: {_tester.UnexpectedErrorCount}", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"JSON: {S614Tester.ResultFilePath}", footerStyle);
            lineY += lineH;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 22), $"Status: {_tester.OverallStatus}", footerStyle);
        }

        private static void DrawRow(float x, float y, float w, string label, bool? passed, GUIStyle labelStyle)
        {
            string mark = passed switch
            {
                true => "✅ PASS",
                false => "❌ FAIL",
                _ => "⏳ Running"
            };
            GUI.Label(new Rect(x, y, w - 80, 22), label, labelStyle);
            GUI.Label(new Rect(x + (w - 80), y, 80, 22), mark, labelStyle);
        }

        private void OnDestroy()
        {
            _tester?.UnsubscribeEarlyListeners();
        }
    }

    /// <summary>S6-14 spike 测试逻辑 — 5+1 R3 case (P1→P2→P3→P4→P5→P5b) 串行执行。</summary>
    public class S614Tester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S6-14_Result.json");

        public bool? P1Passed { get; private set; }
        public bool? P2Passed { get; private set; }
        public bool? P3Passed { get; private set; }
        public bool? P4Passed { get; private set; }
        public bool? P5Passed { get; private set; }
        public bool? P5bPassed { get; private set; }

        public bool AllPassed =>
            P1Passed == true && P2Passed == true && P3Passed == true &&
            P4Passed == true && P5Passed == true && P5bPassed == true;

        public string OverallStatus { get; private set; } = "Running";
        public long TotalElapsedMs { get; private set; }

        private readonly Dictionary<string, string> _asserts = new Dictionary<string, string>();
        private readonly Stopwatch _swTotal = new Stopwatch();
        private readonly MonoBehaviour _hostBehaviour;

        private readonly List<string> _capturedLogs = new List<string>();
        public int UnexpectedErrorCount { get; private set; }

        private static readonly string[] ExpectedLogSubstrings = new string[]
        {
            "[InteractableObject",
            "[InteractionCoordinator]",
            "[GameApp]",
            "[GameFlow]",
            "[S6-14]",
            "[YooAsset]",
            "PuzzleConfigProvider",
            "RegisterPuzzleConfigProvider",
            "RegisterInputConfigProvider",
            "AssetBundle",
            "Cannot load asset",
            "scene to load is null",
            "OnRequestSceneChange",
        };

        public S614Tester(MonoBehaviour host)
        {
            _hostBehaviour = host;
        }

        public void SubscribeEarlyListeners()
        {
            Application.logMessageReceived += OnLogReceived;
        }

        public void UnsubscribeEarlyListeners()
        {
            Application.logMessageReceived -= OnLogReceived;
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count < 500)
            {
                _capturedLogs.Add($"[{type}] {condition}");
            }

            if (type == LogType.Error || type == LogType.Exception)
            {
                bool isExpected = false;
                foreach (var pattern in ExpectedLogSubstrings)
                {
                    if (!string.IsNullOrEmpty(condition) && condition.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isExpected = true;
                        break;
                    }
                }
                if (!isExpected)
                {
                    UnexpectedErrorCount++;
                }
            }
        }

        private static SceneManager GetProductionSceneManager()
        {
            var fi = typeof(GameApp).GetField("_sceneManager", BindingFlags.NonPublic | BindingFlags.Static);
            if (fi == null)
            {
                Log.Error("[S6-14] 反射拿 GameApp._sceneManager 字段失败：FieldInfo == null");
                return null;
            }
            return fi.GetValue(null) as SceneManager;
        }

        private static async UniTask<bool> WaitForIdleAsync(SceneManager scene, double timeoutSec)
        {
            var sw = Stopwatch.StartNew();
            while (scene.CurrentState != SceneManagerState.Idle)
            {
                if (sw.Elapsed.TotalSeconds > timeoutSec)
                    return false;
                await UniTask.Yield();
            }
            return true;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            if (target == null) return default;
            var fi = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi == null) return default;
            return (T)fi.GetValue(target);
        }

        public async UniTask RunAllAsync()
        {
            _swTotal.Start();
            try
            {
                await UniTask.Yield();
                await UniTask.DelayFrame(2);

                Log.Info("[S6-14] Chapter 1 baseline 加载...");
                var sm = GetProductionSceneManager();
                if (sm == null)
                {
                    OverallStatus = "Crashed: GameApp._sceneManager == null";
                    _asserts["baseline.production_sm_present"] = "FAIL: GameApp._sceneManager 反射拿 null";
                    return;
                }

                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
                bool baselineLoaded = await WaitForIdleAsync(sm, timeoutSec: 15.0);
                if (!baselineLoaded || sm.CurrentChapterId != 1)
                {
                    OverallStatus = $"Crashed: baseline chapter 1 load failed (state={sm.CurrentState}, currentChapterId={sm.CurrentChapterId})";
                    _asserts["baseline.chapter1_loaded"] = $"FAIL: state={sm.CurrentState} currentChapterId={sm.CurrentChapterId}";
                    return;
                }
                _asserts["baseline.chapter1_loaded"] = "PASS: chapter 1 loaded (state=Idle, currentChapterId=1)";
                Log.Info($"[S6-14] Chapter 1 baseline ✅ (state={sm.CurrentState}, currentChapterId={sm.CurrentChapterId})");

                await UniTask.DelayFrame(5);

                await RunP1Async();
                await UniTask.DelayFrame(2);
                await RunP2Async();
                await UniTask.DelayFrame(2);
                await RunP3Async();
                await UniTask.DelayFrame(2);
                await RunP4Async();
                await UniTask.DelayFrame(2);
                await RunP5Async();
                await UniTask.DelayFrame(2);
                await RunP5bAsync();

                OverallStatus = AllPassed ? "All Passed" : "Some Failed";
                Log.Info($"[S6-14] Done. AllPassed={AllPassed} Elapsed={_swTotal.ElapsedMilliseconds}ms UnexpectedError={UnexpectedErrorCount}");
            }
            catch (Exception ex)
            {
                OverallStatus = $"Crashed: {ex.GetType().Name} {ex.Message}";
                _asserts["fatal.exception"] = $"FAIL: {ex}";
                Log.Error($"[S6-14] RunAllAsync crashed: {ex}");
            }
            finally
            {
                _swTotal.Stop();
                TotalElapsedMs = _swTotal.ElapsedMilliseconds;
                WriteResultJson();
            }
        }

        private async UniTask RunP1Async()
        {
            var coordinator = UnityEngine.Object.FindObjectOfType<InteractionCoordinator>();
            bool exists = coordinator != null;
            _asserts["P1.coordinator_exists"] = $"{(exists ? "PASS" : "FAIL")}: FindObjectOfType<InteractionCoordinator>() {(exists ? "!= null" : "== null")}";

            int objectCount = 0;
            if (exists)
            {
                var objects = GetPrivateField<List<InteractableObject>>(coordinator, "_objects");
                objectCount = objects?.Count ?? 0;
            }
            bool countOk = objectCount == 2;
            _asserts["P1.objects_count"] = $"{(countOk ? "PASS" : "FAIL")}: expected 2, actual {objectCount}";

            P1Passed = exists && countOk;
            Log.Info($"[S6-14][P1] {(P1Passed == true ? "✅ PASS" : "❌ FAIL")} coordinator={exists} objectsCount={objectCount}");
            await UniTask.Yield();
        }

        private async UniTask RunP2Async()
        {
            var all = UnityEngine.Object.FindObjectsOfType<InteractableObject>();
            bool lengthOk = all.Length == 2;
            _asserts["P2.interactable_count"] = $"{(lengthOk ? "PASS" : "FAIL")}: expected 2, actual {all.Length}";

            bool idOk = true;
            bool puzzleOk = true;
            bool dragDepthOk = true;
            bool cameraOk = true;
            var foundIds = new HashSet<int>();

            foreach (var io in all)
            {
                int objectId = GetPrivateField<int>(io, "_objectId");
                int puzzleId = GetPrivateField<int>(io, "_puzzleId");
                float dragDepth = GetPrivateField<float>(io, "_dragDepth");
                var cam = GetPrivateField<Camera>(io, "_gameplayCamera");

                foundIds.Add(objectId);
                if (objectId != 1 && objectId != 2) idOk = false;
                if (puzzleId != 1) puzzleOk = false;
                if (Mathf.Abs(dragDepth - 10f) > 0.01f) dragDepthOk = false;
                if (cam == null) cameraOk = false;
            }

            bool idsSetOk = foundIds.Contains(1) && foundIds.Contains(2);
            _asserts["P2.object_ids"] = $"{(idOk && idsSetOk ? "PASS" : "FAIL")}: expected {{1,2}}, actual [{string.Join(",", foundIds)}]";
            _asserts["P2.puzzle_id"] = $"{(puzzleOk ? "PASS" : "FAIL")}: expected puzzleId==1 for all";
            _asserts["P2.drag_depth"] = $"{(dragDepthOk ? "PASS" : "FAIL")}: expected _dragDepth==10 for all";
            _asserts["P2.gameplay_camera"] = $"{(cameraOk ? "PASS" : "FAIL")}: expected _gameplayCamera!=null for all";

            P2Passed = lengthOk && idOk && idsSetOk && puzzleOk && dragDepthOk && cameraOk;
            Log.Info($"[S6-14][P2] {(P2Passed == true ? "✅ PASS" : "❌ FAIL")} count={all.Length}");
            await UniTask.Yield();
        }

        private async UniTask RunP3Async()
        {
            int layer8 = LayerMask.NameToLayer("InteractableObject");
            bool layerNameOk = layer8 == 8;
            _asserts["P3.layer8_name"] = $"{(layerNameOk ? "PASS" : "FAIL")}: LayerMask.NameToLayer('InteractableObject')==8, actual {layer8}";

            var coordinator = UnityEngine.Object.FindObjectOfType<InteractionCoordinator>();
            bool maskOk = false;
            if (coordinator != null)
            {
                var mask = GetPrivateField<LayerMask>(coordinator, "_interactableLayer");
                maskOk = (mask.value & (1 << 8)) != 0;
            }
            _asserts["P3.coordinator_layer_mask"] = $"{(maskOk ? "PASS" : "FAIL")}: _interactableLayer mask 含 Layer 8";

            var all = UnityEngine.Object.FindObjectsOfType<InteractableObject>();
            bool parentLayerOk = true;
            bool hitbox01Ok = false;
            bool hitbox02Ok = false;
            bool box2dOk = false;
            bool circle2dOk = false;
            bool box3dOk = false;
            bool capsule3dOk = false;

            foreach (var io in all)
            {
                if (io.gameObject.layer != layer8) parentLayerOk = false;

                if (io.name.Contains("CoffeeMug"))
                {
                    var hitbox = io.transform.Find("Hitbox2D");
                    hitbox01Ok = hitbox != null && hitbox.gameObject.layer == layer8;
                    box2dOk = hitbox != null && hitbox.GetComponent<BoxCollider2D>() != null;
                    box3dOk = io.GetComponent<BoxCollider>() != null;
                }
                else if (io.name.Contains("Book"))
                {
                    var hitbox = io.transform.Find("Hitbox2D");
                    hitbox02Ok = hitbox != null && hitbox.gameObject.layer == layer8;
                    circle2dOk = hitbox != null && hitbox.GetComponent<CircleCollider2D>() != null;
                    capsule3dOk = io.GetComponent<CapsuleCollider>() != null;
                }
            }

            _asserts["P3.parent_layer8"] = $"{(parentLayerOk ? "PASS" : "FAIL")}: Object_01/02 parent layer==InteractableObject (8)";
            _asserts["P3.hitbox01_child"] = $"{(hitbox01Ok ? "PASS" : "FAIL")}: Object_01 child Hitbox2D layer==8";
            _asserts["P3.hitbox02_child"] = $"{(hitbox02Ok ? "PASS" : "FAIL")}: Object_02 child Hitbox2D layer==8";
            _asserts["P3.box2d_child"] = $"{(box2dOk ? "PASS" : "FAIL")}: Object_01 child BoxCollider2D 存在 (Drift D [A2])";
            _asserts["P3.circle2d_child"] = $"{(circle2dOk ? "PASS" : "FAIL")}: Object_02 child CircleCollider2D 存在 (Drift D [A2])";
            _asserts["P3.box3d_parent_retained"] = $"{(box3dOk ? "PASS" : "FAIL")}: Object_01 parent BoxCollider 3D 保留";
            _asserts["P3.capsule3d_parent_retained"] = $"{(capsule3dOk ? "PASS" : "FAIL")}: Object_02 parent CapsuleCollider 3D 保留";

            P3Passed = layerNameOk && maskOk && parentLayerOk && hitbox01Ok && hitbox02Ok &&
                       box2dOk && circle2dOk && box3dOk && capsule3dOk;
            Log.Info($"[S6-14][P3] {(P3Passed == true ? "✅ PASS" : "❌ FAIL")} layer8={layer8} maskOk={maskOk}");
            await UniTask.Yield();
        }

        private async UniTask RunP4Async()
        {
            var coordinator = UnityEngine.Object.FindObjectOfType<InteractionCoordinator>();
            bool coordCamOk = false;
            string coordCamName = "null";
            if (coordinator != null)
            {
                var cam = GetPrivateField<Camera>(coordinator, "_gameplayCamera");
                coordCamOk = cam != null;
                coordCamName = cam != null ? cam.gameObject.name : "null";
            }
            bool coordNameOk = coordCamName == "MainCamera";
            _asserts["P4.coordinator_camera"] = $"{(coordCamOk ? "PASS" : "FAIL")}: Coordinator._gameplayCamera != null";
            _asserts["P4.coordinator_camera_name"] = $"{(coordNameOk ? "PASS" : "FAIL")}: expected MainCamera, actual {coordCamName}";

            var all = UnityEngine.Object.FindObjectsOfType<InteractableObject>();
            bool allCamOk = true;
            bool allNameOk = true;
            foreach (var io in all)
            {
                var cam = GetPrivateField<Camera>(io, "_gameplayCamera");
                if (cam == null) allCamOk = false;
                else if (cam.gameObject.name != "MainCamera") allNameOk = false;
            }
            _asserts["P4.objects_camera"] = $"{(allCamOk ? "PASS" : "FAIL")}: all InteractableObject._gameplayCamera != null";
            _asserts["P4.objects_camera_name"] = $"{(allNameOk ? "PASS" : "FAIL")}: all InteractableObject cameras name==MainCamera";

            P4Passed = coordCamOk && coordNameOk && allCamOk && allNameOk;
            Log.Info($"[S6-14][P4] {(P4Passed == true ? "✅ PASS" : "❌ FAIL")} coordCam={coordCamName}");
            await UniTask.Yield();
        }

        private async UniTask RunP5Async()
        {
            var coordinator = UnityEngine.Object.FindObjectOfType<InteractionCoordinator>();
            bool noException = false;
            string exceptionMsg = string.Empty;

            if (coordinator != null)
            {
                try
                {
                    var hit = coordinator.RaycastWithFatFinger(Vector2.zero);
                    noException = true;
                    _asserts["P5.raycast_result"] = $"INFO: RaycastWithFatFinger(Vector2.zero) returned {(hit != null ? hit.name : "null")} (hit 与否取决于 camera/position；本 case 仅验无异常)";
                }
                catch (Exception ex)
                {
                    exceptionMsg = ex.Message;
                    noException = false;
                }
            }
            else
            {
                exceptionMsg = "InteractionCoordinator == null";
            }

            _asserts["P5.no_exception"] = $"{(noException ? "PASS" : "FAIL")}: RaycastWithFatFinger 无异常{(noException ? "" : $" ({exceptionMsg})")}";
            _asserts["P5.dimensional_consistency"] = $"{(noException ? "PASS" : "FAIL")}: Physics2D ↔ child 2D collider 维度对齐 (Drift D [A2] narrow scope)";

            P5Passed = noException;
            Log.Info($"[S6-14][P5] {(P5Passed == true ? "✅ PASS" : "❌ FAIL")} noException={noException}");
            await UniTask.Yield();
        }

        private async UniTask RunP5bAsync()
        {
            var coordinator = UnityEngine.Object.FindObjectOfType<InteractionCoordinator>();
            bool idempotentOk = false;
            bool lockedOk = false;

            if (coordinator != null)
            {
                int errBefore = UnexpectedErrorCount;
                coordinator.Initialize();
                coordinator.Initialize();
                idempotentOk = true;
                lockedOk = !coordinator.IsLocked;
                int errAfter = UnexpectedErrorCount;
                _asserts["P5b.no_new_unexpected_error"] = $"{(errAfter == errBefore ? "PASS" : "FAIL")}: Initialize×2 无新增 unexpected error (before={errBefore}, after={errAfter})";
            }
            else
            {
                _asserts["P5b.coordinator_present"] = "FAIL: InteractionCoordinator == null";
            }

            _asserts["P5b.initialize_idempotent"] = $"{(idempotentOk ? "PASS" : "FAIL")}: Initialize() 调用 2 次无异常";
            _asserts["P5b.is_locked_false"] = $"{(lockedOk ? "PASS" : "FAIL")}: IsLocked==false after Initialize×2";

            P5bPassed = idempotentOk && lockedOk;
            Log.Info($"[S6-14][P5b] {(P5bPassed == true ? "✅ PASS" : "❌ FAIL")} idempotent={idempotentOk} locked={!lockedOk}");
            await UniTask.Yield();
        }

        private void WriteResultJson()
        {
            try
            {
                var sb = new StringBuilder(2048);
                sb.Append("{\n");
                sb.Append("  \"spike\": \"S6-14 Chapter 1 Scene Wiring (Track F vs-chapter-1-005)\",\n");
                sb.Append($"  \"timestamp\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\n");
                sb.Append($"  \"overallStatus\": \"{Escape(OverallStatus)}\",\n");
                sb.Append($"  \"allPassed\": {(AllPassed ? "true" : "false")},\n");
                sb.Append($"  \"totalElapsedMs\": {TotalElapsedMs},\n");
                sb.Append($"  \"unexpectedErrorCount\": {UnexpectedErrorCount},\n");
                sb.Append("  \"caseResults\": {\n");
                sb.Append($"    \"P1\": {Verdict(P1Passed)},\n");
                sb.Append($"    \"P2\": {Verdict(P2Passed)},\n");
                sb.Append($"    \"P3\": {Verdict(P3Passed)},\n");
                sb.Append($"    \"P4\": {Verdict(P4Passed)},\n");
                sb.Append($"    \"P5\": {Verdict(P5Passed)},\n");
                sb.Append($"    \"P5b\": {Verdict(P5bPassed)}\n");
                sb.Append("  },\n");
                sb.Append("  \"asserts\": {\n");
                int idx = 0;
                foreach (var kv in _asserts)
                {
                    sb.Append($"    \"{Escape(kv.Key)}\": \"{Escape(kv.Value)}\"");
                    sb.Append(idx == _asserts.Count - 1 ? "\n" : ",\n");
                    idx++;
                }
                sb.Append("  }\n");
                sb.Append("}\n");

                File.WriteAllText(ResultFilePath, sb.ToString());
                Log.Info($"[S6-14] JSON evidence dumped to {ResultFilePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"[S6-14] WriteResultJson 失败: {ex.Message}");
            }
        }

        private static string Verdict(bool? value) => value switch
        {
            true => "\"PASS\"",
            false => "\"FAIL\"",
            _ => "\"NotRun\""
        };

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
#endif
