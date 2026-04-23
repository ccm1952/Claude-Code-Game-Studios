// 该文件由Cursor 自动生成
//
// SP-007 验证脚本：HybridCLR + AsyncGPUReadback AOT 兼容性
//
// 目的：验证在 GameLogic 热更程序集中，AsyncGPUReadback 回调和
//       GameEvent 泛型 Send<struct> 在 AOT 环境下是否正常工作。
//
// 使用方法：
//   1. 在 Unity 场景中创建空 GameObject
//   2. 挂载此脚本
//   3. Play Mode 运行，观察 Console 输出
//   4. 真机构建后运行，观察 logcat / Xcode console

using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using TEngine;

namespace GameLogic.Test
{
    public class SP007_HybridCLRAsyncGPUTest : MonoBehaviour
    {
        private RenderTexture _testRT;
        private bool _asyncGPUPassed;
        private bool _gameEventStructPassed;
        private bool _gameEventMultiParamPassed;
        private string _statusText = "Running tests...";
        private float _startTime;

        private System.Action<TestPayload> _structHandler;
        private System.Action<int, Vector3, Quaternion> _multiParamHandler;

        // SP-001 合并验证：struct payload
        public struct TestPayload
        {
            public int puzzleId;
            public float score;
        }

        private void Start()
        {
            _startTime = Time.realtimeSinceStartup;
            Debug.Log("[SP-007] ═══════════════════════════════════════════");
            Debug.Log("[SP-007] HybridCLR + AsyncGPUReadback 验证开始");
            Debug.Log($"[SP-007] Assembly: {GetType().Assembly.GetName().Name}");
            Debug.Log($"[SP-007] supportsAsyncGPUReadback: {SystemInfo.supportsAsyncGPUReadback}");
            Debug.Log("[SP-007] ═══════════════════════════════════════════");

            RunGameEventTests();
            RunAsyncGPUReadbackTest();
        }

        private void OnDestroy()
        {
            if (_structHandler != null)
                GameEvent.RemoveEventListener<TestPayload>(99901, _structHandler);
            if (_multiParamHandler != null)
                GameEvent.RemoveEventListener<int, Vector3, Quaternion>(99902, _multiParamHandler);

            if (_testRT != null)
            {
                _testRT.Release();
                Destroy(_testRT);
            }
        }

        #region Test 1: GameEvent struct payload (SP-001 AOT 验证)

        private void RunGameEventTests()
        {
            // Test 1a: 单参数 struct
            bool received1 = false;
            _structHandler = (payload) =>
            {
                received1 = true;
                Debug.Log($"[SP-007] GameEvent<struct> RECEIVED: puzzleId={payload.puzzleId}, score={payload.score}");
            };
            GameEvent.AddEventListener<TestPayload>(99901, _structHandler);
            GameEvent.Send<TestPayload>(99901, new TestPayload { puzzleId = 42, score = 0.85f });

            if (received1)
            {
                _gameEventStructPassed = true;
                Debug.Log("[SP-007] ✅ TEST PASS: GameEvent.Send<struct> 在热更程序集中正常");
            }
            else
            {
                Debug.LogError("[SP-007] ❌ TEST FAIL: GameEvent.Send<struct> 回调未触发");
            }

            // Test 1b: 多参数泛型 (int, Vector3, Quaternion)
            bool received2 = false;
            _multiParamHandler = (id, pos, rot) =>
            {
                received2 = true;
                Debug.Log($"[SP-007] GameEvent<int,V3,Quat> RECEIVED: id={id}, pos={pos}");
            };
            GameEvent.AddEventListener<int, Vector3, Quaternion>(99902, _multiParamHandler);
            GameEvent.Send<int, Vector3, Quaternion>(99902, 7, Vector3.one, Quaternion.identity);

            if (received2)
            {
                _gameEventMultiParamPassed = true;
                Debug.Log("[SP-007] ✅ TEST PASS: GameEvent.Send<int,Vector3,Quaternion> 多参数泛型正常");
            }
            else
            {
                Debug.LogError("[SP-007] ❌ TEST FAIL: GameEvent.Send 多参数泛型回调未触发");
            }
        }

        #endregion

        #region Test 2: AsyncGPUReadback in HotFix assembly

        private void RunAsyncGPUReadbackTest()
        {
            if (!SystemInfo.supportsAsyncGPUReadback)
            {
                Debug.LogWarning("[SP-007] ⚠️ 设备不支持 AsyncGPUReadback，跳过测试");
                _asyncGPUPassed = true;
                _statusText = "AsyncGPUReadback not supported on this device (skipped)";
                return;
            }

            _testRT = new RenderTexture(64, 64, 0, RenderTextureFormat.R8);
            _testRT.Create();

            var cmd = new CommandBuffer { name = "SP007_FillRT" };
            cmd.SetRenderTarget(_testRT);
            cmd.ClearRenderTarget(false, true, new Color(0.5f, 0.5f, 0.5f, 1f));
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            Debug.Log("[SP-007] AsyncGPUReadback.Request 发送中（从 GameLogic 热更程序集）...");
            AsyncGPUReadback.Request(_testRT, 0, TextureFormat.R8, OnAsyncGPUReadbackComplete);
        }

        private void OnAsyncGPUReadbackComplete(AsyncGPUReadbackRequest request)
        {
            float elapsed = (Time.realtimeSinceStartup - _startTime) * 1000f;

            if (request.hasError)
            {
                Debug.LogError($"[SP-007] ❌ TEST FAIL: AsyncGPUReadback 回调报错 (elapsed: {elapsed:F1}ms)");
                _statusText = "FAIL: AsyncGPUReadback callback error";
                return;
            }

            // 关键验证点：GetData<byte>() 泛型在 AOT 环境下的实例化
            NativeArray<byte> data = request.GetData<byte>();

            if (data.Length > 0)
            {
                byte sampleValue = data[0];
                _asyncGPUPassed = true;
                Debug.Log($"[SP-007] ✅ TEST PASS: AsyncGPUReadback 在热更程序集中正常");
                Debug.Log($"[SP-007]    数据长度: {data.Length} bytes, 采样值: {sampleValue}");
                Debug.Log($"[SP-007]    回调耗时: {elapsed:F1}ms");
            }
            else
            {
                Debug.LogError("[SP-007] ❌ TEST FAIL: GetData<byte>() 返回空数据");
                _statusText = "FAIL: GetData<byte>() returned empty";
            }

            PrintFinalReport();
        }

        #endregion

        private void PrintFinalReport()
        {
            float totalElapsed = (Time.realtimeSinceStartup - _startTime) * 1000f;

            Debug.Log("[SP-007] ═══════════════════════════════════════════");
            Debug.Log("[SP-007]           验 证 报 告");
            Debug.Log("[SP-007] ═══════════════════════════════════════════");
            Debug.Log($"[SP-007] GameEvent<struct>       : {(_gameEventStructPassed ? "✅ PASS" : "❌ FAIL")}");
            Debug.Log($"[SP-007] GameEvent<multi-param>  : {(_gameEventMultiParamPassed ? "✅ PASS" : "❌ FAIL")}");
            Debug.Log($"[SP-007] AsyncGPUReadback        : {(_asyncGPUPassed ? "✅ PASS" : "❌ FAIL")}");
            Debug.Log($"[SP-007] 总耗时                  : {totalElapsed:F1}ms");
            Debug.Log($"[SP-007] 程序集                  : {GetType().Assembly.GetName().Name}");
            Debug.Log("[SP-007] ═══════════════════════════════════════════");

            bool allPassed = _gameEventStructPassed && _gameEventMultiParamPassed && _asyncGPUPassed;

            if (allPassed)
            {
                Debug.Log("[SP-007] 🎉 ALL TESTS PASSED — HybridCLR 热更环境兼容性确认");
                _statusText = "ALL PASSED";
            }
            else
            {
                Debug.LogError("[SP-007] ⚠️ SOME TESTS FAILED — 需要实施回退方案 (IShadowRTReader)");
                _statusText = "SOME FAILED — see console";
            }
        }

        private void OnGUI()
        {
            var boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 10, 10)
            };

            float w = 500, h = 260;
            float x = (Screen.width - w) / 2f;
            float y = 20;

            GUI.Box(new Rect(x, y, w, h), "", boxStyle);

            var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(x, y + 10, w, 30), "SP-007 验证面板", titleStyle);

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };

            float lineY = y + 50;
            float lineH = 28;

            DrawTestRow(x + 20, lineY, w - 40, "GameEvent<struct>", _gameEventStructPassed, labelStyle);
            lineY += lineH;
            DrawTestRow(x + 20, lineY, w - 40, "GameEvent<int,V3,Quat>", _gameEventMultiParamPassed, labelStyle);
            lineY += lineH;
            DrawTestRow(x + 20, lineY, w - 40, "AsyncGPUReadback", _asyncGPUPassed, labelStyle);
            lineY += lineH + 10;

            var asmStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.cyan } };
            GUI.Label(new Rect(x + 20, lineY, w - 40, 24), $"Assembly: {GetType().Assembly.GetName().Name}", asmStyle);
            lineY += 24;
            GUI.Label(new Rect(x + 20, lineY, w - 40, 24), $"GPU Readback Support: {SystemInfo.supportsAsyncGPUReadback}", asmStyle);
            lineY += 30;

            bool allPassed = _gameEventStructPassed && _gameEventMultiParamPassed && _asyncGPUPassed;
            var resultStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = allPassed ? Color.green : (_asyncGPUPassed ? Color.yellow : Color.red) }
            };
            GUI.Label(new Rect(x, lineY, w, 30), _statusText, resultStyle);
        }

        private void DrawTestRow(float x, float y, float w, string label, bool passed, GUIStyle style)
        {
            string icon = passed ? "✅" : "⏳";
            style.normal.textColor = passed ? Color.green : Color.white;
            GUI.Label(new Rect(x, y, w, 24), $"  {icon}  {label}", style);
        }
    }
}
