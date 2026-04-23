// 该文件由Cursor 自动生成
using NUnit.Framework;
using GameLogic;

namespace ShadowGame.Tests.EditMode.Rendering
{
    [TestFixture]
    public class ShadowRTConfigTests
    {
        // --- AC-4: Cascade counts per tier ---
        [Test]
        public void High_CascadeCount_Is4()
        {
            Assert.AreEqual(4, ShadowRTConfig.High.CascadeCount);
        }

        [Test]
        public void Medium_CascadeCount_Is2()
        {
            Assert.AreEqual(2, ShadowRTConfig.Medium.CascadeCount);
        }

        [Test]
        public void Low_CascadeCount_Is1()
        {
            Assert.AreEqual(1, ShadowRTConfig.Low.CascadeCount);
        }

        // --- AC-2: Resolution per tier ---
        [Test]
        public void High_Resolution_Is2048()
        {
            Assert.AreEqual(2048, ShadowRTConfig.High.Resolution);
        }

        [Test]
        public void Medium_Resolution_Is1024()
        {
            Assert.AreEqual(1024, ShadowRTConfig.Medium.Resolution);
        }

        [Test]
        public void Low_Resolution_Is512()
        {
            Assert.AreEqual(512, ShadowRTConfig.Low.Resolution);
        }

        // --- Soft shadows: enabled for High/Medium, disabled for Low ---
        [Test]
        public void Low_SoftShadows_Disabled()
        {
            Assert.IsFalse(ShadowRTConfig.Low.SoftShadows);
        }

        [Test]
        public void High_SoftShadows_Enabled()
        {
            Assert.IsTrue(ShadowRTConfig.High.SoftShadows);
        }

        // --- Shadow distance is 8m across all tiers ---
        [Test]
        public void AllTiers_ShadowDistance_Is8()
        {
            Assert.AreEqual(8f, ShadowRTConfig.High.ShadowDistance);
            Assert.AreEqual(8f, ShadowRTConfig.Medium.ShadowDistance);
            Assert.AreEqual(8f, ShadowRTConfig.Low.ShadowDistance);
        }
    }
}
