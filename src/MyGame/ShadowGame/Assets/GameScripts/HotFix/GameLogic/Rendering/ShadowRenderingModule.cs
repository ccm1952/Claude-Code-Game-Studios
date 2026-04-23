// 该文件由Cursor 自动生成
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Manages the ShadowRT, ShadowSampleCamera, and WallReceiver material interface.
    /// Lifecycle: Init() at module startup, Dispose() at module shutdown.
    /// All shadow map resolution / cascade params come from <see cref="ShadowRTConfig"/>.
    /// </summary>
    public class ShadowRenderingModule
    {
        private RenderTexture _shadowRT;
        private Material _wallMaterial;
        private Camera _shadowCamera;
        private bool _initialized;

        private static readonly int PropShadowRT = Shader.PropertyToID("_ShadowRT");
        private static readonly int PropShadowIntensity = Shader.PropertyToID("_ShadowIntensity");
        private static readonly int PropShadowColor = Shader.PropertyToID("_ShadowColor");
        private static readonly int PropShadowContrast = Shader.PropertyToID("_ShadowContrast");
        private static readonly int PropWallColor = Shader.PropertyToID("_WallColor");
        private static readonly int PropGlowIntensity = Shader.PropertyToID("_GlowIntensity");
        private static readonly int PropGlowColor = Shader.PropertyToID("_GlowColor");
        private static readonly int PropEdgeSoftness = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int PropColorTemperature = Shader.PropertyToID("_ColorTemperature");

        public RenderTexture ShadowRT => _shadowRT;
        public bool IsInitialized => _initialized;

        /// <summary>
        /// Create ShadowRT and configure ShadowSampleCamera.
        /// <paramref name="config"/> should come from Luban TbRenderConfig.
        /// <paramref name="wallMaterial"/> is the material using ShadowGame/WallReceiver shader.
        /// <paramref name="shadowCamera"/> is the orthographic camera targeting the wall.
        /// </summary>
        public void Init(ShadowRTConfig config, Material wallMaterial, Camera shadowCamera)
        {
            if (_initialized)
            {
                Debug.LogWarning("[ShadowRenderingModule] Already initialized, skipping");
                return;
            }

            _shadowRT = new RenderTexture(config.Resolution, config.Resolution, 0, RenderTextureFormat.R8)
            {
                name = "ShadowRT",
                filterMode = FilterMode.Bilinear
            };
            _shadowRT.Create();

            _wallMaterial = wallMaterial;
            _shadowCamera = shadowCamera;

            if (_shadowCamera != null)
            {
                _shadowCamera.targetTexture = _shadowRT;
                _shadowCamera.orthographic = true;
            }

            if (_wallMaterial != null)
                _wallMaterial.SetTexture(PropShadowRT, _shadowRT);

            _initialized = true;
        }

        /// <summary>Release ShadowRT — no RenderTexture leak (ADR-002).</summary>
        public void Dispose()
        {
            if (_shadowRT != null)
            {
                _shadowRT.Release();
                Object.DestroyImmediate(_shadowRT);
                _shadowRT = null;
            }
            _wallMaterial = null;
            _shadowCamera = null;
            _initialized = false;
        }

        // --- Glow API (AC-5, AC-6) ---

        /// <summary>
        /// Activate NearMatch glow on the wall surface.
        /// Glow is additive in main camera only — does NOT affect ShadowRT pixels.
        /// </summary>
        public void SetShadowGlow(float intensity, Color color)
        {
            if (_wallMaterial == null) return;
            _wallMaterial.SetFloat(PropGlowIntensity, Mathf.Clamp01(intensity));
            _wallMaterial.SetColor(PropGlowColor, color);
        }

        /// <summary>Clear glow effect.</summary>
        public void ClearShadowGlow()
        {
            if (_wallMaterial == null) return;
            _wallMaterial.SetFloat(PropGlowIntensity, 0f);
        }

        // --- Style API (chapter transition) ---

        public void SetShadowColor(Color color)
        {
            _wallMaterial?.SetColor(PropShadowColor, color);
        }

        public void SetShadowIntensity(float intensity)
        {
            _wallMaterial?.SetFloat(PropShadowIntensity, Mathf.Clamp01(intensity));
        }

        public void SetShadowContrast(float contrast)
        {
            _wallMaterial?.SetFloat(PropShadowContrast, Mathf.Clamp(contrast, 1f, 3f));
        }

        public void SetEdgeSoftness(float softness)
        {
            _wallMaterial?.SetFloat(PropEdgeSoftness, Mathf.Clamp01(softness));
        }

        public void SetColorTemperature(float temperature)
        {
            _wallMaterial?.SetFloat(PropColorTemperature, Mathf.Clamp(temperature, -1f, 1f));
        }

        public void SetWallColor(Color color)
        {
            _wallMaterial?.SetColor(PropWallColor, color);
        }
    }
}
