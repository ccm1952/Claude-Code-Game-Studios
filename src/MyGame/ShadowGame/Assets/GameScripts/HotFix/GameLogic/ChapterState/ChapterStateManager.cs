// 该文件由Cursor 自动生成
using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Single Source of Truth for chapter and puzzle progress at runtime.
    /// Initialised from save data, reads static config from Luban TbChapter/TbPuzzle
    /// (stubbed as parameters until Luban integration in a later story).
    /// </summary>
    public class ChapterStateManager
    {
        public const int TotalChapters = 5;

        private ChapterProgress[] _chapters;
        private bool _initialized;

        public bool IsInitialized => _initialized;

        /// <summary>
        /// Initialise runtime state. Call on the main thread only.
        /// <paramref name="puzzlesPerChapter"/> provides per-chapter puzzle counts
        /// (from Luban TbPuzzle in production; passed as parameter for testability).
        /// </summary>
        public void Init(IChapterProgress saveData, int[] puzzlesPerChapter)
        {
            if (puzzlesPerChapter == null || puzzlesPerChapter.Length != TotalChapters)
                throw new ArgumentException(
                    $"puzzlesPerChapter must have exactly {TotalChapters} entries");

            _chapters = new ChapterProgress[TotalChapters];

            for (int i = 0; i < TotalChapters; i++)
            {
                int chapterId = i + 1;
                var puzzles = new PuzzleProgress[puzzlesPerChapter[i]];
                for (int p = 0; p < puzzles.Length; p++)
                {
                    puzzles[p] = new PuzzleProgress
                    {
                        PuzzleId = p + 1,
                        State = PuzzleStateEnum.Locked
                    };
                }

                _chapters[i] = new ChapterProgress
                {
                    ChapterId = chapterId,
                    IsUnlocked = (chapterId == 1),
                    IsCompleted = false,
                    Puzzles = puzzles
                };
            }

            ApplySaveData(saveData);
            _initialized = true;
        }

        /// <summary>
        /// Returns the <see cref="ChapterProgress"/> for the given chapter ID (1-based).
        /// Returns null for invalid IDs instead of throwing.
        /// </summary>
        public ChapterProgress GetChapterProgress(int chapterId)
        {
            int idx = chapterId - 1;
            if (idx < 0 || idx >= TotalChapters || _chapters == null)
                return null;
            return _chapters[idx];
        }

        /// <summary>
        /// Returns the current <see cref="PuzzleStateEnum"/> for a specific puzzle.
        /// Returns <see cref="PuzzleStateEnum.Locked"/> for invalid IDs.
        /// </summary>
        public PuzzleStateEnum GetPuzzleState(int chapterId, int puzzleId)
        {
            var chapter = GetChapterProgress(chapterId);
            if (chapter?.Puzzles == null)
                return PuzzleStateEnum.Locked;

            int idx = puzzleId - 1;
            if (idx < 0 || idx >= chapter.Puzzles.Length)
                return PuzzleStateEnum.Locked;

            return chapter.Puzzles[idx].State;
        }

        private void ApplySaveData(IChapterProgress saveData)
        {
            if (saveData == null) return;

            if (saveData.UnlockedChapterIds != null)
            {
                foreach (int id in saveData.UnlockedChapterIds)
                {
                    var ch = GetChapterProgress(id);
                    if (ch != null) ch.IsUnlocked = true;
                }
            }

            if (saveData.CompletedChapterIds != null)
            {
                foreach (int id in saveData.CompletedChapterIds)
                {
                    var ch = GetChapterProgress(id);
                    if (ch != null)
                    {
                        ch.IsUnlocked = true;
                        ch.IsCompleted = true;
                    }
                }
            }

            if (saveData.PuzzleEntries != null)
            {
                foreach (var entry in saveData.PuzzleEntries)
                {
                    var ch = GetChapterProgress(entry.ChapterId);
                    if (ch?.Puzzles == null) continue;

                    int idx = entry.PuzzleId - 1;
                    if (idx < 0 || idx >= ch.Puzzles.Length) continue;

                    var state = entry.State;
                    if (!Enum.IsDefined(typeof(PuzzleStateEnum), state))
                        state = PuzzleStateEnum.Locked;

                    ch.Puzzles[idx].State = state;
                }
            }
        }
    }
}
