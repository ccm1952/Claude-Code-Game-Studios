// 该文件由Cursor 自动生成
using NUnit.Framework;
using UnityEngine;
using GameLogic;

namespace ShadowGame.Tests.EditMode.InputSystem
{
    [TestFixture]
    public class InputConfigFromLubanTests
    {
        private InputConfigFromLuban _config;

        [SetUp]
        public void SetUp()
        {
            _config = new InputConfigFromLuban();
            _config.InitWithDefaults();
        }

        // ────────── TC-006-02: DPI normalization — iPhone 13 Mini ──────────

        [Test]
        public void DragThreshold_IPhone476Dpi_CorrectPixels()
        {
            _config.SetScreenDpi(476f);
            // 3.0 * 476 / 25.4 ≈ 56.22 px
            Assert.AreEqual(56.22f, _config.DragThresholdPx, 0.5f,
                "iPhone 476 DPI: dragThreshold should be ~56.2px");
        }

        // ────────── TC-006-02b: iPad 264 DPI ──────────

        [Test]
        public void DragThreshold_IPad264Dpi_CorrectPixels()
        {
            _config.SetScreenDpi(264f);
            // 3.0 * 264 / 25.4 ≈ 31.18 px
            Assert.AreEqual(31.18f, _config.DragThresholdPx, 0.5f,
                "iPad 264 DPI: dragThreshold should be ~31.2px");
        }

        // ────────── TC-006-03: DPI = 0 fallback ──────────

        [Test]
        public void DragThreshold_ZeroDpi_UsesFallback160()
        {
            _config.SetScreenDpi(0f);
            // 3.0 * 160 / 25.4 ≈ 18.90 px
            Assert.AreEqual(18.90f, _config.DragThresholdPx, 0.5f,
                "Screen.dpi=0 should fallback to 160 DPI");
            Assert.IsFalse(float.IsNaN(_config.DragThresholdPx));
        }

        // ────────── TC-006-04: Sensitivity change immediate effect ──────────

        [Test]
        public void Sensitivity_HalfMultiplier_HalvesThreshold()
        {
            _config.SetScreenDpi(326f);
            float original = _config.DragThresholdPx;

            _config.OnSensitivityChanged(0.5f);

            Assert.AreEqual(original * 0.5f, _config.DragThresholdPx, 0.1f,
                "0.5x sensitivity should halve the threshold");
        }

        [Test]
        public void Sensitivity_DoubleMultiplier_DoublesThreshold()
        {
            _config.SetScreenDpi(326f);
            float original = _config.DragThresholdPx;

            _config.OnSensitivityChanged(2.0f);

            Assert.AreEqual(original * 2.0f, _config.DragThresholdPx, 0.1f,
                "2.0x sensitivity should double the threshold");
        }

        // ────────── TC-006-05: Sensitivity clamp ──────────

        [Test]
        public void Sensitivity_AboveMax_ClampedToTwo()
        {
            _config.OnSensitivityChanged(5.0f);
            Assert.AreEqual(2.0f, _config.SensitivityMultiplier, 0.001f);
        }

        [Test]
        public void Sensitivity_BelowMin_ClampedToHalf()
        {
            _config.OnSensitivityChanged(0.1f);
            Assert.AreEqual(0.5f, _config.SensitivityMultiplier, 0.001f);
        }

        // ────────── TC-006-07: rotateThreshold degree-to-radian conversion ──────────

        [Test]
        public void RotateThreshold_ConvertedToRadians()
        {
            // Default = 8° → 8 * π/180 ≈ 0.1396 rad
            Assert.AreEqual(8f * Mathf.Deg2Rad, _config.RotateThresholdRad, 0.001f,
                "rotateThreshold should be stored in radians");
        }

        // ────────── TC-006-07b: InitFromLuban sets values correctly ──────────

        [Test]
        public void InitFromLuban_AllValuesSet()
        {
            var config = new InputConfigFromLuban();
            config.InitFromLuban(
                baseDragThresholdMm: 4.0f,
                tapTimeout: 0.3f,
                rotateThresholdDeg: 10f,
                pinchThreshold: 0.1f,
                minFingerDistance: 25f,
                maxDeltaPerFrame: 120f,
                fallbackDpi: 200f
            );

            Assert.AreEqual(4.0f, config.BaseDragThresholdMm);
            Assert.AreEqual(0.3f, config.TapTimeoutSeconds);
            Assert.AreEqual(10f * Mathf.Deg2Rad, config.RotateThresholdRad, 0.001f);
            Assert.AreEqual(0.1f, config.PinchThreshold);
            Assert.AreEqual(25f, config.MinFingerDistance);
            Assert.AreEqual(120f, config.MaxDeltaPerFrame);
            Assert.AreEqual(200f, config.FallbackDpi);
        }

        // ────────── IInputConfig / IDualFingerConfig interface compliance ──────────

        [Test]
        public void ImplementsBothConfigInterfaces()
        {
            Assert.IsInstanceOf<IInputConfig>(_config);
            Assert.IsInstanceOf<IDualFingerConfig>(_config);
        }
    }
}
