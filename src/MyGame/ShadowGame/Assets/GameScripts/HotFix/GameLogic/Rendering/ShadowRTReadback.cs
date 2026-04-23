// 该文件由Cursor 自动生成
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Manages AsyncGPUReadback of ShadowRT and dispatches pixel data via GameEvent.
    /// SP-007 verified: AsyncGPUReadback works in HybridCLR hot-update assembly.
    /// <para>
    /// Call <see cref="Update"/> once per frame from the rendering module.
    /// Quality tier controls readback frequency: every frame (Medium/High) or
    /// every other frame (Low).
    /// </para>
    /// </summary>
    public class ShadowRTReadback
    {
        private RenderTexture _shadowRT;
        private bool _readbackPending;
        private byte[] _cachedPixels;
        private int _resolution;
        private int _skipFrameInterval;
        private int _frameCounter;
        private bool _disposed;

        /// <summary>Number of completed readbacks since Init.</summary>
        public int CompletedReadbackCount { get; private set; }
        /// <summary>Number of readback errors since Init.</summary>
        public int ErrorCount { get; private set; }
        public bool IsReadbackPending => _readbackPending;

        /// <summary>
        /// Initialise the readback pipeline.
        /// </summary>
        /// <param name="shadowRT">The R8 RenderTexture to read back.</param>
        /// <param name="tier">Quality tier — Low = every other frame, Medium/High = every frame.</param>
        public void Init(RenderTexture shadowRT, QualityTier tier)
        {
            _shadowRT = shadowRT;
            _resolution = shadowRT != null ? shadowRT.width : 512;
            _cachedPixels = new byte[_resolution * _resolution];
            _skipFrameInterval = (tier == QualityTier.Low) ? 2 : 1;
            _frameCounter = 0;
            _readbackPending = false;
            _disposed = false;
            CompletedReadbackCount = 0;
            ErrorCount = 0;
        }

        /// <summary>
        /// Call once per frame. Issues a new readback request if the previous
        /// one has completed and the frame interval is met.
        /// </summary>
        public void Update()
        {
            if (_disposed || _shadowRT == null || !_shadowRT.IsCreated())
                return;

            _frameCounter++;

            if (_readbackPending)
                return;

            if (_frameCounter % _skipFrameInterval != 0)
                return;

            _readbackPending = true;
            AsyncGPUReadback.Request(_shadowRT, 0, TextureFormat.R8, OnReadbackComplete);
        }

        /// <summary>
        /// Change readback frequency at runtime (e.g. quality tier switch).
        /// </summary>
        public void SetQualityTier(QualityTier tier)
        {
            _skipFrameInterval = (tier == QualityTier.Low) ? 2 : 1;
        }

        public void Dispose()
        {
            _disposed = true;
            _shadowRT = null;
            _cachedPixels = null;
        }

        private void OnReadbackComplete(AsyncGPUReadbackRequest request)
        {
            _readbackPending = false;

            if (_disposed) return;

            UnityEngine.Profiling.Profiler.BeginSample("ShadowRT.Readback.CPU");

            ShadowRTData payload;

            if (request.hasError)
            {
                ErrorCount++;
                Debug.LogWarning("[ShadowRTReadback] Readback failed, using cached data");
                payload = new ShadowRTData
                {
                    Pixels = _cachedPixels,
                    Width = _resolution,
                    Height = _resolution,
                    FrameNumber = Time.frameCount,
                    IsStaleCache = true
                };
            }
            else
            {
                NativeArray<byte> data = request.GetData<byte>();
                if (_cachedPixels == null || _cachedPixels.Length != data.Length)
                    _cachedPixels = new byte[data.Length];
                NativeArray<byte>.Copy(data, _cachedPixels, data.Length);

                CompletedReadbackCount++;
                payload = new ShadowRTData
                {
                    Pixels = _cachedPixels,
                    Width = _resolution,
                    Height = _resolution,
                    FrameNumber = Time.frameCount,
                    IsStaleCache = false
                };
            }

            GameEvent.Get<IShadowRTEvent>().OnShadowRTUpdated(payload);

            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}
