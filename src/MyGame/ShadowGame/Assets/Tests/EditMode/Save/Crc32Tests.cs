// 该文件由Cursor 自动生成
using System.Text;
using NUnit.Framework;
using GameLogic;

namespace ShadowGame.Tests.EditMode.Save
{
    [TestFixture]
    public class Crc32Tests
    {
        [Test]
        public void ComputeHex_EmptyArray_ReturnsValidHex()
        {
            string hex = Crc32.ComputeHex(new byte[0]);
            Assert.AreEqual(8, hex.Length);
            Assert.AreEqual("00000000", hex);
        }

        [Test]
        public void ComputeHex_Null_ReturnsValidHex()
        {
            string hex = Crc32.ComputeHex(null);
            Assert.AreEqual(8, hex.Length);
            Assert.AreEqual("00000000", hex);
        }

        [Test]
        public void ComputeHex_KnownInput_MatchesExpected()
        {
            // "123456789" → CRC32 = CBF43926 (IEEE standard test vector)
            byte[] data = Encoding.ASCII.GetBytes("123456789");
            string hex = Crc32.ComputeHex(data);
            Assert.AreEqual("CBF43926", hex);
        }

        [Test]
        public void ComputeHex_JsonSample_Returns8CharHex()
        {
            byte[] data = Encoding.UTF8.GetBytes("{\"version\":1}");
            string hex = Crc32.ComputeHex(data);
            Assert.AreEqual(8, hex.Length);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(hex, "^[0-9A-F]{8}$"));
        }

        [Test]
        public void Compute_ReturnsUint_SameAsHex()
        {
            byte[] data = Encoding.ASCII.GetBytes("123456789");
            uint raw = Crc32.Compute(data);
            string hex = raw.ToString("X8");
            Assert.AreEqual(Crc32.ComputeHex(data), hex);
        }

        [Test]
        public void ComputeHex_DifferentInputs_DifferentResults()
        {
            string a = Crc32.ComputeHex(Encoding.UTF8.GetBytes("hello"));
            string b = Crc32.ComputeHex(Encoding.UTF8.GetBytes("world"));
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void ComputeHex_SameInput_Deterministic()
        {
            byte[] data = Encoding.UTF8.GetBytes("deterministic test");
            string first = Crc32.ComputeHex(data);
            string second = Crc32.ComputeHex(data);
            Assert.AreEqual(first, second);
        }
    }
}
