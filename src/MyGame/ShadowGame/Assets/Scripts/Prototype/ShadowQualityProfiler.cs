// 该文件由Cursor 自动生成
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace ShadowGame.Prototype
{
    /// <summary>
    /// Collects per-frame performance data for shadow prototype validation.
    /// Supports CSV export for post-session analysis.
    /// </summary>
    public class ShadowQualityProfiler : MonoBehaviour
    {
        [Header("Profiling Settings")]
        [SerializeField] private bool _enableProfiling = true;
        [SerializeField] private int _maxSamples = 3600;
        [SerializeField] private ShadowRTCapture _shadowRTCapture;
        [SerializeField] private ShadowPrototypeManager _manager;

        private readonly List<FrameSample> _samples = new();
        private float _avgFps;
        private float _minFps = float.MaxValue;
        private int _frameCount;

        private struct FrameSample
        {
            public int Frame;
            public float DeltaTime;
            public float Fps;
            public long TotalAllocatedMemory;
            public long TotalReservedMemory;
            public float ReadbackMs;
            public int ReadbackLatency;
            public string QualityTier;
        }

        private void Update()
        {
            if (!_enableProfiling) return;
            RecordSample();
        }

        private void RecordSample()
        {
            _frameCount++;
            float fps = 1f / Time.unscaledDeltaTime;

            if (fps < _minFps && _frameCount > 10)
                _minFps = fps;

            _avgFps += (fps - _avgFps) / _frameCount;

            var sample = new FrameSample
            {
                Frame = Time.frameCount,
                DeltaTime = Time.unscaledDeltaTime * 1000f,
                Fps = fps,
                TotalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong(),
                TotalReservedMemory = Profiler.GetTotalReservedMemoryLong(),
                ReadbackMs = _shadowRTCapture != null ? _shadowRTCapture.LastReadbackTimeMs : 0f,
                ReadbackLatency = _shadowRTCapture != null ? _shadowRTCapture.ReadbackFrameLatency : 0,
                QualityTier = _manager != null ? _manager.CurrentTier.ToString() : "Unknown",
            };

            if (_samples.Count >= _maxSamples)
                _samples.RemoveAt(0);

            _samples.Add(sample);
        }

        public string GetStatsString()
        {
            if (_samples.Count == 0) return "No data";

            var latest = _samples[^1];
            float allocMB = latest.TotalAllocatedMemory / (1024f * 1024f);
            return $"Frame: {latest.DeltaTime:F1}ms | Avg: {_avgFps:F1} | Min: {_minFps:F1}\n" +
                   $"Mem: {allocMB:F1}MB | RB: {latest.ReadbackMs:F2}ms({latest.ReadbackLatency}f)\n" +
                   $"Samples: {_samples.Count}";
        }

        /// <summary>
        /// Exports collected samples to CSV. Returns the file path, or empty on failure.
        /// </summary>
        public string ExportCSV()
        {
            if (_samples.Count == 0) return string.Empty;

            string dir = Path.Combine(Application.persistentDataPath, "ShadowPrototype");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string filename = $"shadow_profiler_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string path = Path.Combine(dir, filename);

            var sb = new StringBuilder();
            sb.AppendLine("Frame,DeltaTimeMs,FPS,AllocMemBytes,ReservedMemBytes,ReadbackMs,ReadbackLatency,Quality");

            foreach (var s in _samples)
            {
                sb.AppendLine(
                    $"{s.Frame},{s.DeltaTime:F2},{s.Fps:F1},{s.TotalAllocatedMemory},{s.TotalReservedMemory}," +
                    $"{s.ReadbackMs:F3},{s.ReadbackLatency},{s.QualityTier}");
            }

            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[ShadowProfiler] Exported {_samples.Count} samples to {path}");
            return path;
        }
    }
}
