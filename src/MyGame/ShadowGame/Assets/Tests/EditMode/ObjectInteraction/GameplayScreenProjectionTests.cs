// 该文件由Cursor 自动生成
using GameLogic;
using NUnit.Framework;
using UnityEngine;

namespace ShadowGame.Tests.EditMode.ObjectInteraction
{
    [TestFixture]
    public class GameplayScreenProjectionTests
    {
        private GameObject _cameraGo;
        private Camera _camera;

        [TearDown]
        public void TearDown()
        {
            if (_cameraGo != null)
                Object.DestroyImmediate(_cameraGo);
        }

        [Test]
        public void ComputeVisibleBounds_Chapter1PerspectiveCamera_MinYNearTableHeight()
        {
            _cameraGo = new GameObject("chapter1-cam");
            _camera = _cameraGo.AddComponent<Camera>();
            _camera.orthographic = false;
            _camera.fieldOfView = 60f;
            _camera.aspect = 16f / 9f;
            _cameraGo.transform.position = new Vector3(0f, 1.7f, -3f);

            Assert.IsTrue(
                GameplayScreenProjection.TryComputeVisibleBoundsOnZPlane(_camera, 0f, 0.15f, out var bounds),
                "Chapter 1 透视相机应能求出 Z=0 平面可见范围");

            Assert.That(bounds.MinY, Is.GreaterThan(-0.25f), "可见下缘应接近桌面高度 y≈0，不应落到屏幕外负值");
            Assert.That(bounds.MaxX, Is.LessThan(3.5f));
            Assert.That(bounds.MinX, Is.GreaterThan(-3.5f));
        }

        [Test]
        public void EffectiveBounds_IntersectsWideConfigWithCameraVisibleEnvelope()
        {
            _cameraGo = new GameObject("chapter1-cam");
            _camera = _cameraGo.AddComponent<Camera>();
            _camera.orthographic = false;
            _camera.fieldOfView = 60f;
            _camera.aspect = 16f / 9f;
            _cameraGo.transform.position = new Vector3(0f, 1.7f, -3f);

            var wideConfig = new InteractionBounds(-10f, 10f, -10f, 10f);
            Assert.IsTrue(
                GameplayScreenProjection.TryComputeVisibleBoundsOnZPlane(_camera, 0f, 0.15f, out var visible));

            var effective = InteractionBounds.Intersect(wideConfig, visible);
            Assert.IsTrue(effective.IsValid);
            Assert.That(effective.MinY, Is.GreaterThan(wideConfig.MinY));
            Assert.That(effective.MaxX, Is.LessThan(wideConfig.MaxX));
        }
    }
}
