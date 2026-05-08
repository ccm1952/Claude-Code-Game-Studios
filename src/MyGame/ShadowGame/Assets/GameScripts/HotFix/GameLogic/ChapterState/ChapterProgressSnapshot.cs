// 该文件由Cursor 自动生成
using System;

namespace GameLogic
{
    /// <summary>
    /// Immutable, read-only projection of <see cref="ChapterStateManager"/>'s
    /// internal progress state. The sole implementation of
    /// <see cref="IChapterProgress"/> that the Chapter State module hands to
    /// the rest of the game (ADR-008 §4 — interface-only boundary with the
    /// Save System).
    /// </summary>
    /// <remarks>
    /// Produced by <see cref="ChapterStateManager.GetProgressSnapshot"/> on
    /// demand (Story-005 AC-1). Save System consumes it via the registered
    /// <c>Func&lt;IChapterProgress&gt;</c> provider delegate and writes the
    /// three arrays into <see cref="SaveData.chapterProgress"/>. Neither side
    /// holds a compile-time reference to the other's concrete type.
    /// </remarks>
    public sealed class ChapterProgressSnapshot : IChapterProgress
    {
        public int[] UnlockedChapterIds { get; }
        public int[] CompletedChapterIds { get; }
        public PuzzleProgressEntry[] PuzzleEntries { get; }

        public ChapterProgressSnapshot(
            int[] unlockedChapterIds,
            int[] completedChapterIds,
            PuzzleProgressEntry[] puzzleEntries)
        {
            UnlockedChapterIds = unlockedChapterIds ?? Array.Empty<int>();
            CompletedChapterIds = completedChapterIds ?? Array.Empty<int>();
            PuzzleEntries = puzzleEntries ?? Array.Empty<PuzzleProgressEntry>();
        }

        /// <summary>
        /// Story-005 AC-8 empty snapshot — returned when a caller invokes
        /// <see cref="ChapterStateManager.GetProgressSnapshot"/> before
        /// <c>Init</c>. All three arrays are non-null and zero-length.
        /// </summary>
        public static ChapterProgressSnapshot Empty { get; } = new ChapterProgressSnapshot(
            Array.Empty<int>(),
            Array.Empty<int>(),
            Array.Empty<PuzzleProgressEntry>());
    }
}
