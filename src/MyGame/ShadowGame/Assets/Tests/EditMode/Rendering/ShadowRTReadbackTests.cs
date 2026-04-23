// 该文件由Cursor 自动生成
using NUnit.Framework;
using GameLogic;
using TEngine;

namespace ShadowGame.Tests.EditMode.Rendering
{
    /// <summary>
    /// ADR-027 接口事件协议下的 <see cref="ShadowRTReadback"/> / <see cref="ShadowRTData"/> 验收测试。
    /// 取代基于 <c>EventId.Evt_ShadowRT_Updated</c> 常量范围 / 冲突检测的旧测试；
    /// 改为验证 <see cref="IShadowRTEvent_Event"/> 生成 ID 与其他接口 ID 无碰撞。
    /// </summary>
    [TestFixture]
    public class ShadowRTReadbackTests
    {
        [SetUp]
        public void SetUp()
        {
            GameEventHelper.Init();
        }

        // --- ShadowRTData struct ---
        [Test]
        public void ShadowRTData_DefaultIsStaleCache_IsFalse()
        {
            var data = new ShadowRTData();
            Assert.IsFalse(data.IsStaleCache);
        }

        [Test]
        public void ShadowRTData_PixelsCanBeAssigned()
        {
            var pixels = new byte[512 * 512];
            pixels[0] = 128;
            var data = new ShadowRTData
            {
                Pixels = pixels,
                Width = 512,
                Height = 512,
                FrameNumber = 42,
                IsStaleCache = false
            };
            Assert.AreEqual(512, data.Width);
            Assert.AreEqual(512, data.Height);
            Assert.AreEqual(512 * 512, data.Pixels.Length);
            Assert.AreEqual(128, data.Pixels[0]);
            Assert.AreEqual(42, data.FrameNumber);
        }

        [Test]
        public void ShadowRTData_StaleCache_FlagWorks()
        {
            var data = new ShadowRTData { IsStaleCache = true };
            Assert.IsTrue(data.IsStaleCache);
        }

        // --- IShadowRTEvent source-generated ID uniqueness (ADR-027) ---
        [Test]
        public void IShadowRTEvent_GeneratedId_DoesNotCollideWithGesture()
        {
            int shadowId = (int)IShadowRTEvent_Event.OnShadowRTUpdated;
            Assert.AreNotEqual(shadowId, (int)IGestureEvent_Event.OnTap);
            Assert.AreNotEqual(shadowId, (int)IGestureEvent_Event.OnDrag);
            Assert.AreNotEqual(shadowId, (int)IGestureEvent_Event.OnRotate);
            Assert.AreNotEqual(shadowId, (int)IGestureEvent_Event.OnPinch);
            Assert.AreNotEqual(shadowId, (int)IGestureEvent_Event.OnLightDrag);
        }

        [Test]
        public void IShadowRTEvent_GeneratedId_DoesNotCollideWithSettings()
        {
            int shadowId = (int)IShadowRTEvent_Event.OnShadowRTUpdated;
            Assert.AreNotEqual(shadowId, (int)ISettingsEvent_Event.OnSettingChanged);
            Assert.AreNotEqual(shadowId, (int)ISettingsEvent_Event.OnTouchSensitivityChanged);
        }

        // --- ShadowRTReadback state machine ---
        [Test]
        public void Readback_InitialState_NotPending()
        {
            var readback = new ShadowRTReadback();
            Assert.IsFalse(readback.IsReadbackPending);
            Assert.AreEqual(0, readback.CompletedReadbackCount);
            Assert.AreEqual(0, readback.ErrorCount);
        }

        [Test]
        public void Readback_Update_WithNullRT_DoesNotThrow()
        {
            var readback = new ShadowRTReadback();
            readback.Init(null, QualityTier.Medium);
            Assert.DoesNotThrow(() => readback.Update());
            Assert.IsFalse(readback.IsReadbackPending);
        }

        [Test]
        public void Readback_Dispose_ClearsState()
        {
            var readback = new ShadowRTReadback();
            readback.Init(null, QualityTier.High);
            readback.Dispose();
            Assert.DoesNotThrow(() => readback.Update());
        }

        [Test]
        public void Readback_SetQualityTier_DoesNotThrow()
        {
            var readback = new ShadowRTReadback();
            readback.Init(null, QualityTier.High);
            Assert.DoesNotThrow(() => readback.SetQualityTier(QualityTier.Low));
            Assert.DoesNotThrow(() => readback.SetQualityTier(QualityTier.Medium));
        }
    }
}
