// 该文件由Cursor 自动生成
#if ENABLE_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShadowGame.Prototype
{
    /// <summary>
    /// Shadow prototype scene manager.
    /// Provides on-screen touch buttons for quality switching and CSV export.
    /// </summary>
    public class ShadowPrototypeManager : MonoBehaviour
    {
        [Header("URP Assets (assign in Inspector)")]
        [SerializeField] private UniversalRenderPipelineAsset _urpLow;
        [SerializeField] private UniversalRenderPipelineAsset _urpMedium;
        [SerializeField] private UniversalRenderPipelineAsset _urpHigh;

        [Header("References")]
        [SerializeField] private ShadowRTCapture _shadowRTCapture;
        [SerializeField] private ShadowQualityProfiler _profiler;

        private QualityTier _currentTier = QualityTier.Medium;
        private GUIStyle _statsStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _btnActiveStyle;
        private string _exportMsg;
        private float _exportMsgTimer;

        public enum QualityTier { Low, Medium, High }

        public QualityTier CurrentTier => _currentTier;

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
            ApplyTier(_currentTier);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) ApplyTier(QualityTier.Low);
            if (Input.GetKeyDown(KeyCode.Alpha2)) ApplyTier(QualityTier.Medium);
            if (Input.GetKeyDown(KeyCode.Alpha3)) ApplyTier(QualityTier.High);
            if (Input.GetKeyDown(KeyCode.F5)) DoExport();

            if (_exportMsgTimer > 0f)
                _exportMsgTimer -= Time.unscaledDeltaTime;
        }

        public void ApplyTier(QualityTier tier)
        {
            _currentTier = tier;
            UniversalRenderPipelineAsset target = tier switch
            {
                QualityTier.Low => _urpLow,
                QualityTier.Medium => _urpMedium,
                QualityTier.High => _urpHigh,
                _ => _urpMedium
            };

            if (target != null)
            {
                GraphicsSettings.defaultRenderPipeline = target;
                QualitySettings.renderPipeline = target;
                Debug.Log($"[ShadowPrototype] Switched to {tier} quality tier.");
            }
        }

        private void DoExport()
        {
            if (_profiler == null) return;
            string path = _profiler.ExportCSV();
            _exportMsg = string.IsNullOrEmpty(path) ? "Export failed" : $"Saved: {path}";
            _exportMsgTimer = 4f;
        }

        private void EnsureStyles()
        {
            if (_statsStyle != null) return;

            int scaledFont = Mathf.Max(14, Screen.height / 50);
            int btnFont = Mathf.Max(18, Screen.height / 36);

            _statsStyle = new GUIStyle
            {
                fontSize = scaledFont,
                normal = { textColor = Color.white },
                padding = new RectOffset(8, 8, 8, 8)
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = btnFont,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _btnStyle.normal.textColor = Color.white;
            _btnStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.85f));
            _btnStyle.active.background = MakeTex(2, 2, new Color(0.4f, 0.4f, 0.4f, 0.9f));
            _btnStyle.padding = new RectOffset(12, 12, 12, 12);

            _btnActiveStyle = new GUIStyle(_btnStyle);
            _btnActiveStyle.normal.textColor = Color.yellow;
            _btnActiveStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.35f, 0.1f, 0.9f));
        }

        private void OnGUI()
        {
            EnsureStyles();

            float sw = Screen.width;
            float sh = Screen.height;
            float btnW = sw * 0.2f;
            float btnH = sh * 0.08f;
            float gap = sw * 0.015f;
            float bottomY = sh - btnH - sh * 0.03f;
            float startX = (sw - (4f * btnW + 3f * gap)) * 0.5f;

            float fps = 1f / Time.unscaledDeltaTime;
            string stats = $"[{_currentTier}]  FPS: {fps:F1}";
            if (_profiler != null)
                stats += $"\n{_profiler.GetStatsString()}";
            GUI.Label(new Rect(10, 10, sw * 0.6f, sh * 0.2f), stats, _statsStyle);

            if (GUI.Button(new Rect(startX, bottomY, btnW, btnH), "LOW",
                _currentTier == QualityTier.Low ? _btnActiveStyle : _btnStyle))
                ApplyTier(QualityTier.Low);

            if (GUI.Button(new Rect(startX + btnW + gap, bottomY, btnW, btnH), "MEDIUM",
                _currentTier == QualityTier.Medium ? _btnActiveStyle : _btnStyle))
                ApplyTier(QualityTier.Medium);

            if (GUI.Button(new Rect(startX + 2f * (btnW + gap), bottomY, btnW, btnH), "HIGH",
                _currentTier == QualityTier.High ? _btnActiveStyle : _btnStyle))
                ApplyTier(QualityTier.High);

            if (GUI.Button(new Rect(startX + 3f * (btnW + gap), bottomY, btnW, btnH), "EXPORT CSV", _btnStyle))
                DoExport();

            if (_exportMsgTimer > 0f && !string.IsNullOrEmpty(_exportMsg))
            {
                var toastStyle = new GUIStyle(_statsStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.green }
                };
                float tw = sw * 0.7f;
                GUI.Label(new Rect((sw - tw) * 0.5f, bottomY - btnH - 10, tw, btnH), _exportMsg, toastStyle);
            }
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
#else
namespace ShadowGame.Prototype
{
    public class ShadowPrototypeManager : UnityEngine.MonoBehaviour
    {
        private void Awake()
        {
            UnityEngine.Debug.LogWarning("[ShadowPrototype] URP not installed. ENABLE_URP is not defined.");
        }
    }
}
#endif
