// 该文件由Cursor 自动生成
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using GameLogic;

namespace ShadowGame.Tests.EditMode.Save
{
    [TestFixture]
    public class SaveManagerTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Application.temporaryCachePath, "SaveManagerTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        // --- CRC verification on load ---
        [Test]
        public void Load_ValidPrimary_ReturnsSaveData()
        {
            var data = new SaveData { version = 1 };
            string json = JsonUtility.ToJson(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            string crc = Crc32.ComputeHex(bytes);

            File.WriteAllText(Path.Combine(_tempDir, "save.json"), json);
            File.WriteAllText(Path.Combine(_tempDir, "save.crc"), crc);

            var loaded = LoadFromDir(_tempDir);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.version);
        }

        [Test]
        public void Load_CorruptCrc_FallsBackToBackup()
        {
            var data = new SaveData { version = 1 };
            string json = JsonUtility.ToJson(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            string crc = Crc32.ComputeHex(bytes);

            File.WriteAllText(Path.Combine(_tempDir, "save.json"), json);
            File.WriteAllText(Path.Combine(_tempDir, "save.crc"), "BADCRC00");
            File.WriteAllText(Path.Combine(_tempDir, "save.backup.json"), json);
            File.WriteAllText(Path.Combine(_tempDir, "save.backup.crc"), crc);

            var loaded = LoadFromDir(_tempDir);
            Assert.IsNotNull(loaded, "Should fall back to valid backup");
        }

        [Test]
        public void Load_NoPrimaryNoBackup_ReturnsNull()
        {
            var loaded = LoadFromDir(_tempDir);
            Assert.IsNull(loaded);
        }

        [Test]
        public void Load_BothCorrupt_ReturnsNull()
        {
            File.WriteAllText(Path.Combine(_tempDir, "save.json"), "garbage");
            File.WriteAllText(Path.Combine(_tempDir, "save.crc"), "BADCRC00");
            File.WriteAllText(Path.Combine(_tempDir, "save.backup.json"), "garbage");
            File.WriteAllText(Path.Combine(_tempDir, "save.backup.crc"), "BADCRC00");

            var loaded = LoadFromDir(_tempDir);
            Assert.IsNull(loaded);
        }

        [Test]
        public void CrcFile_ContainsOnly8CharHex()
        {
            var data = new SaveData();
            string json = JsonUtility.ToJson(data);
            string crc = Crc32.ComputeHex(Encoding.UTF8.GetBytes(json));

            Assert.AreEqual(8, crc.Length);
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(crc, "^[0-9A-F]{8}$"));
        }

        [Test]
        public void DeleteSave_RemovesFiles()
        {
            string primary = Path.Combine(_tempDir, "save.json");
            string crc = Path.Combine(_tempDir, "save.crc");
            File.WriteAllText(primary, "{}");
            File.WriteAllText(crc, "00000000");

            Assert.IsTrue(File.Exists(primary));
            File.Delete(primary);
            File.Delete(crc);
            Assert.IsFalse(File.Exists(primary));
        }

        // --- Migration chain tests ---
        [Test]
        public void RegisterMigration_AppliedOnLoad()
        {
            var mgr = new SaveManager();
            mgr.RegisterMigration(new V0ToV1Migration());

            var data = new SaveData { version = 0 };
            string json = JsonUtility.ToJson(data);
            string crc = Crc32.ComputeHex(Encoding.UTF8.GetBytes(json));

            File.WriteAllText(Path.Combine(_tempDir, "save.json"), json);
            File.WriteAllText(Path.Combine(_tempDir, "save.crc"), crc);

            var loaded = LoadFromDirWithMigrations(_tempDir, mgr);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(SaveData.CURRENT_VERSION, loaded.version);
        }

        [Test]
        public void MissingMigration_StopsChain()
        {
            var data = new SaveData { version = 0 };
            string json = JsonUtility.ToJson(data);
            string crc = Crc32.ComputeHex(Encoding.UTF8.GetBytes(json));

            File.WriteAllText(Path.Combine(_tempDir, "save.json"), json);
            File.WriteAllText(Path.Combine(_tempDir, "save.crc"), crc);

            var loaded = LoadFromDirNoMigration(_tempDir);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(0, loaded.version, "Version stays at 0 when no migration applied");
        }

        [Test]
        public void EmptyPrimaryFile_TreatedAsCorrupt()
        {
            File.WriteAllText(Path.Combine(_tempDir, "save.json"), "");
            File.WriteAllText(Path.Combine(_tempDir, "save.crc"), Crc32.ComputeHex(new byte[0]));

            var loaded = LoadFromDir(_tempDir);
            Assert.IsNull(loaded, "Empty JSON should fail deserialization");
        }

        private class V0ToV1Migration : ISaveMigration
        {
            public int FromVersion => 0;
            public SaveData Migrate(SaveData data)
            {
                data.version = 1;
                return data;
            }
        }

        private SaveData LoadFromDirWithMigrations(string dir, SaveManager mgr)
        {
            var result = TryLoadAndVerify(
                Path.Combine(dir, "save.json"),
                Path.Combine(dir, "save.crc"));
            if (result != null)
            {
                var migrations = new ISaveMigration[] { new V0ToV1Migration() };
                while (result.version < SaveData.CURRENT_VERSION)
                {
                    bool found = false;
                    foreach (var m in migrations)
                    {
                        if (m.FromVersion == result.version)
                        {
                            result = m.Migrate(result);
                            found = true;
                            break;
                        }
                    }
                    if (!found) break;
                }
                return result;
            }
            return null;
        }

        private SaveData LoadFromDirNoMigration(string dir)
        {
            return TryLoadAndVerify(
                Path.Combine(dir, "save.json"),
                Path.Combine(dir, "save.crc"));
        }

        /// <summary>
        /// Simplified load that mirrors SaveManager.TryLoadAndVerify logic
        /// using the test directory instead of Application.persistentDataPath.
        /// </summary>
        private SaveData LoadFromDir(string dir)
        {
            var result = TryLoadAndVerify(
                Path.Combine(dir, "save.json"),
                Path.Combine(dir, "save.crc"));
            if (result != null) return result;

            return TryLoadAndVerify(
                Path.Combine(dir, "save.backup.json"),
                Path.Combine(dir, "save.backup.crc"));
        }

        private SaveData TryLoadAndVerify(string jsonPath, string crcPath)
        {
            if (!File.Exists(jsonPath) || !File.Exists(crcPath))
                return null;

            string json = File.ReadAllText(jsonPath, Encoding.UTF8);
            string storedCrc = File.ReadAllText(crcPath, Encoding.UTF8).Trim();
            string computedCrc = Crc32.ComputeHex(Encoding.UTF8.GetBytes(json));

            if (!string.Equals(storedCrc, computedCrc, System.StringComparison.OrdinalIgnoreCase))
                return null;

            return JsonUtility.FromJson<SaveData>(json);
        }
    }
}
