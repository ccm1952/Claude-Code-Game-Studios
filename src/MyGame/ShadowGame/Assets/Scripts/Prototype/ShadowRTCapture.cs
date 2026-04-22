// 该文件由Cursor 自动生成
#if ENABLE_URP
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

namespace ShadowGame.Prototype
{
    /// <summary>
    /// Manages the ShadowSampleCamera and its Render Texture.
    /// Uses AsyncGPUReadback to read shadow data on CPU without stalling.
    /// </summary>
    public class ShadowRTCapture : MonoBehaviour
    {
        [Header("Shadow RT Settings")]
        [SerializeField] private Camera _shadowCamera;
        [SerializeField] private int _rtResolution = 1024;

        [Header("Debug Display")]
        [SerializeField] private bool _showDebugPreview = true;
        [SerializeField] private Vector2 _previewPosition = new(10, 140);
        [SerializeField] private float _previewSize = 200f;

        private RenderTexture _shadowRT;
        private NativeArray<byte> _lastReadbackData;
        private bool _readbackReady;
        private bool _readbackPending;
        private float _lastReadbackTimeMs;
        private int _readbackFrameLatency;
        private int _requestFrame;
        private bool _frozen;

        public RenderTexture ShadowRT => _shadowRT;
        public bool IsReadbackReady => _readbackReady;
        public float LastReadbackTimeMs => _lastReadbackTimeMs;
        public int ReadbackFrameLatency => _readbackFrameLatency;

        /// <summary>
        /// Returns the latest CPU-side shadow data (R8 grayscale).
        /// </summary>
        public NativeArray<byte> GetShadowData()
        {
            return _lastReadbackData;
        }

        private void Start()
        {
            SetupRT();
        }

        private void OnDestroy()
        {
            if (_lastReadbackData.IsCreated)
                _lastReadbackData.Dispose();

            if (_shadowRT != null)
            {
                _shadowCamera.targetTexture = null;
                _shadowRT.Release();
                Destroy(_shadowRT);
            }
        }

        private void SetupRT()
        {
            _shadowRT = new RenderTexture(_rtResolution, _rtResolution, 0, RenderTextureFormat.R8)
            {
                name = "ShadowRT_Runtime",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _shadowRT.Create();

            if (_shadowCamera != null)
            {
                _shadowCamera.targetTexture = _shadowRT;
                _shadowCamera.enabled = true;
            }
        }

        private void LateUpdate()
        {
            if (_frozen || _shadowRT == null || _readbackPending)
                return;

            _requestFrame = Time.frameCount;
            _readbackPending = true;

            AsyncGPUReadback.Request(_shadowRT, 0, TextureFormat.R8, OnReadbackComplete);
        }

        private void OnReadbackComplete(AsyncGPUReadbackRequest request)
        {
            _readbackPending = false;

            if (request.hasError)
            {
                Debug.LogWarning("[ShadowRTCapture] AsyncGPUReadback failed.");
                return;
            }

            float startTime = Time.realtimeSinceStartup;
            var data = request.GetData<byte>();

            if (!_lastReadbackData.IsCreated || _lastReadbackData.Length != data.Length)
            {
                if (_lastReadbackData.IsCreated)
                    _lastReadbackData.Dispose();
                _lastReadbackData = new NativeArray<byte>(data.Length, Allocator.Persistent);
            }

            NativeArray<byte>.Copy(data, _lastReadbackData);

            _readbackReady = true;
            _readbackFrameLatency = Time.frameCount - _requestFrame;
            _lastReadbackTimeMs = (Time.realtimeSinceStartup - startTime) * 1000f;
        }

        /// <summary>
        /// Freezes the shadow RT — stops rendering updates.
        /// </summary>
        public void Freeze()
        {
            _frozen = true;
            if (_shadowCamera != null)
                _shadowCamera.enabled = false;
        }

        /// <summary>
        /// Resumes shadow RT rendering.
        /// </summary>
        public void Unfreeze()
        {
            _frozen = false;
            if (_shadowCamera != null)
                _shadowCamera.enabled = true;
        }

        private void OnGUI()
        {
            if (!_showDebugPreview || _shadowRT == null)
                return;

            GUI.DrawTexture(
                new Rect(_previewPosition.x, _previewPosition.y, _previewSize, _previewSize),
                _shadowRT,
                ScaleMode.ScaleToFit);

            var style = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = Color.cyan } };
            GUI.Label(
                new Rect(_previewPosition.x, _previewPosition.y + _previewSize + 2, 300, 40),
                $"ShadowRT {_rtResolution}x{_rtResolution} | Readback: {_lastReadbackTimeMs:F2}ms | Latency: {_readbackFrameLatency}f",
                style);
        }
    }
}
#else
namespace ShadowGame.Prototype
{
    public class ShadowRTCapture : UnityEngine.MonoBehaviour
    {
        private void Awake()
        {
            UnityEngine.Debug.LogWarning("[ShadowRTCapture] URP not installed. ENABLE_URP is not defined.");
        }
    }
}
#endif
