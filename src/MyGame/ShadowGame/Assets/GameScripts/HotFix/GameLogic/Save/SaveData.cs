// 该文件由Cursor 自动生成
using System;

namespace GameLogic
{
    /// <summary>
    /// Root save file schema. Single-slot JSON, versioned for migration.
    /// Settings are stored in PlayerPrefs — NOT here (ADR-008).
    /// </summary>
    [Serializable]
    public class SaveData : IChapterProgress
    {
        public const int CURRENT_VERSION = 1;

        public int version = CURRENT_VERSION;
        public string playerId = Guid.NewGuid().ToString();
        public long lastSaveTimestampUtc;
        public ChapterSaveData chapterProgress = new ChapterSaveData();

        // --- IChapterProgress implementation ---
        int[] IChapterProgress.UnlockedChapterIds => chapterProgress.unlockedChapterIds;
        int[] IChapterProgress.CompletedChapterIds => chapterProgress.completedChapterIds;
        PuzzleProgressEntry[] IChapterProgress.PuzzleEntries => chapterProgress.puzzleEntries;
    }

    /// <summary>
    /// Chapter progress block within the save file.
    /// Arrays default to empty — new-game state.
    /// </summary>
    [Serializable]
    public class ChapterSaveData
    {
        public int[] unlockedChapterIds = Array.Empty<int>();
        public int[] completedChapterIds = Array.Empty<int>();
        public PuzzleProgressEntry[] puzzleEntries = Array.Empty<PuzzleProgressEntry>();
    }
}
