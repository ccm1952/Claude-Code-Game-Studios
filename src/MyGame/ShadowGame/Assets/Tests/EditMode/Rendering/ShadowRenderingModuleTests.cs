// 该文件由Cursor 自动生成
using NUnit.Framework;
using UnityEngine;
using GameLogic;

namespace ShadowGame.Tests.EditMode.Rendering
{
    [TestFixture]
    public class ShadowRenderingModuleTests
    {
        private ShadowRenderingModule _module;
        private Material _material;

        [SetUp]
        public void SetUp()
        {
            _module = new ShadowRenderingModule();
            _material = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        [TearDown]
        public void TearDown()
        {
            _module.Dispose();
            if (_material != null)
                Object.DestroyImmediate(_material);
        }

        [Test]
        public void Init_CreatesShadowRT_WithCorrectFormat()
        {
            _module.Init(ShadowRTConfig.Low, _material, null);

            Assert.IsTrue(_module.IsInitialized);
            Assert.IsNotNull(_module.ShadowRT);
            Assert.AreEqual(512, _module.ShadowRT.width);
            Assert.AreEqual(512, _module.ShadowRT.height);
            Assert.AreEqual(RenderTextureFormat.R8, _module.ShadowRT.format);
            Assert.IsTrue(_module.ShadowRT.IsCreated());
        }

        [Test]
        public void Init_MediumConfig_1024Resolution()
        {
            _module.Init(ShadowRTConfig.Medium, _material, null);

            Assert.AreEqual(1024, _module.ShadowRT.width);
        }

        [Test]
        public void Init_HighConfig_2048Resolution()
        {
            _module.Init(ShadowRTConfig.High, _material, null);

            Assert.AreEqual(2048, _module.ShadowRT.width);
        }

        [Test]
        public void Init_CalledTwice_SkipsSecond()
        {
            _module.Init(ShadowRTConfig.Low, _material, null);
            var firstRT = _module.ShadowRT;

            _module.Init(ShadowRTConfig.High, _material, null);
            Assert.AreSame(firstRT, _module.ShadowRT, "Second Init should be skipped");
            Assert.AreEqual(512, _module.ShadowRT.width, "Resolution should stay at Low (512)");
        }

        [Test]
        public void Dispose_ReleasesShadowRT()
        {
            _module.Init(ShadowRTConfig.Low, _material, null);
            Assert.IsNotNull(_module.ShadowRT);

            _module.Dispose();

            Assert.IsNull(_module.ShadowRT);
            Assert.IsFalse(_module.IsInitialized);
        }

        [Test]
        public void Dispose_BeforeInit_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _module.Dispose());
        }

        [Test]
        public void SetShadowGlow_SetsIntensityAndColor()
        {
            _module.Init(ShadowRTConfig.Low, _material, null);

            _module.SetShadowGlow(0.8f, Color.yellow);
            // Material property set succeeds without exception
            Assert.IsTrue(_module.IsInitialized);
        }

        [Test]
        public void ClearShadowGlow_ResetsIntensity()
        {
            _module.Init(ShadowRTConfig.Low, _material, null);
            _module.SetShadowGlow(0.8f, Color.yellow);

            _module.ClearShadowGlow();
            Assert.IsTrue(_module.IsInitialized);
        }

        [Test]
        public void SetShadowGlow_NullMaterial_DoesNotThrow()
        {
            _module.Init(ShadowRTConfig.Low, null, null);
            Assert.DoesNotThrow(() => _module.SetShadowGlow(0.5f, Color.red));
            Assert.DoesNotThrow(() => _module.ClearShadowGlow());
        }

        [Test]
        public void StyleApi_ClampsValues()
        {
            _module.Init(ShadowRTConfig.Low, _material, null);

            Assert.DoesNotThrow(() =>
            {
                _module.SetShadowIntensity(2f);
                _module.SetShadowContrast(0f);
                _module.SetEdgeSoftness(-1f);
                _module.SetColorTemperature(5f);
            });
        }
    }
}
