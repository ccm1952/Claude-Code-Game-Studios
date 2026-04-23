// 该文件由Cursor 自动生成
using System;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using GameLogic;

namespace ShadowGame.Tests.EditMode.Save
{
    [TestFixture]
    public class SaveDataSchemaTests
    {
        // --- AC-1: Serializes to valid JSON < 10KB ---
        [Test]
        public void SaveData_EmptySave_SerializesToValidJson()
        {
            var save = new SaveData();
            string json = JsonUtility.ToJson(save);

            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json);
            Assert.IsTrue(json.Contains("\"version\""));
        }

        [Test]
        public void SaveData_FullPlaythrough_LessThan10KB()
        {
            var save = new SaveData();
            save.chapterProgress.unlockedChapterIds = new[] { 1, 2, 3, 4, 5 };
            save.chapterProgress.completedChapterIds = new[] { 1, 2, 3, 4, 5 };

            var entries = new PuzzleProgressEntry[15];
            for (int c = 1; c <= 5; c++)
            {
                for (int p = 0; p < 3; p++)
                {
                    entries[(c - 1) * 3 + p] = new PuzzleProgressEntry
                    {
                        ChapterId = c,
                        PuzzleId = p + 1,
                        StateOrdinal = (int)PuzzleStateEnum.Complete
                    };
                }
            }
            save.chapterProgress.puzzleEntries = entries;

            string json = JsonUtility.ToJson(save);
            int bytes = Encoding.UTF8.GetByteCount(json);

            Assert.Less(bytes, 10240, $"Save JSON is {bytes} bytes, must be < 10KB");
        }

        // --- AC-2: Version defaults to CURRENT_VERSION ---
        [Test]
        public void SaveData_DefaultVersion_EqualsCurrent()
        {
            var save = new SaveData();
            Assert.AreEqual(SaveData.CURRENT_VERSION, save.version);
            Assert.GreaterOrEqual(SaveData.CURRENT_VERSION, 1);
        }

        // --- AC-3: PlayerId generated automatically ---
        [Test]
        public void SaveData_PlayerId_IsValidGuid()
        {
            var save = new SaveData();
            Assert.IsTrue(Guid.TryParse(save.playerId, out _),
                "playerId should be a valid GUID");
        }

        [Test]
        public void SaveData_TwoInstances_DifferentPlayerIds()
        {
            var a = new SaveData();
            var b = new SaveData();
            Assert.AreNotEqual(a.playerId, b.playerId);
        }

        // --- AC-4: ISaveMigration interface compiles ---
        [Test]
        public void ISaveMigration_CanBeImplemented()
        {
            ISaveMigration migration = new StubMigration();
            Assert.AreEqual(0, migration.FromVersion);

            var data = new SaveData { version = 0 };
            var result = migration.Migrate(data);
            Assert.AreEqual(1, result.version);
        }

        // --- AC-5: SaveData implements IChapterProgress ---
        [Test]
        public void SaveData_ImplementsIChapterProgress()
        {
            var save = new SaveData();
            save.chapterProgress.unlockedChapterIds = new[] { 1, 2 };

            IChapterProgress progress = save;
            Assert.AreEqual(2, progress.UnlockedChapterIds.Length);
            Assert.AreEqual(1, progress.UnlockedChapterIds[0]);
        }

        // --- AC-6: Empty arrays on new instance ---
        [Test]
        public void SaveData_NewInstance_EmptyArrays()
        {
            var save = new SaveData();
            Assert.IsNotNull(save.chapterProgress.unlockedChapterIds);
            Assert.AreEqual(0, save.chapterProgress.unlockedChapterIds.Length);
            Assert.AreEqual(0, save.chapterProgress.completedChapterIds.Length);
            Assert.AreEqual(0, save.chapterProgress.puzzleEntries.Length);
        }

        // --- AC: Roundtrip serialization ---
        [Test]
        public void SaveData_JsonRoundtrip_PreservesData()
        {
            var save = new SaveData();
            save.version = 1;
            save.lastSaveTimestampUtc = 1713700000L;
            save.chapterProgress.unlockedChapterIds = new[] { 1, 2 };
            save.chapterProgress.puzzleEntries = new[]
            {
                new PuzzleProgressEntry { ChapterId = 1, PuzzleId = 1, StateOrdinal = 6 }
            };

            string json = JsonUtility.ToJson(save);
            var loaded = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(save.version, loaded.version);
            Assert.AreEqual(save.playerId, loaded.playerId);
            Assert.AreEqual(save.lastSaveTimestampUtc, loaded.lastSaveTimestampUtc);
            Assert.AreEqual(2, loaded.chapterProgress.unlockedChapterIds.Length);
            Assert.AreEqual(6, loaded.chapterProgress.puzzleEntries[0].StateOrdinal);
        }

        // --- Stub migration ---
        private class StubMigration : ISaveMigration
        {
            public int FromVersion => 0;
            public SaveData Migrate(SaveData data)
            {
                data.version = 1;
                return data;
            }
        }
    }
}
