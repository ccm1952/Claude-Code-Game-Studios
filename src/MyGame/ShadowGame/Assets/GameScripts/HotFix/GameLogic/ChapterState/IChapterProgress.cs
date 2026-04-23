// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Read-only contract between the Save System and <see cref="ChapterStateManager"/>.
    /// The Save System produces this on load; ChapterStateManager consumes it during Init.
    /// </summary>
    public interface IChapterProgress
    {
        int[] UnlockedChapterIds { get; }
        int[] CompletedChapterIds { get; }
        PuzzleProgressEntry[] PuzzleEntries { get; }
    }

    /// <summary>
    /// Serializable puzzle state entry used in save files and the
    /// <see cref="IChapterProgress"/> contract. Uses int ordinal for migration safety.
    /// </summary>
    [System.Serializable]
    public struct PuzzleProgressEntry
    {
        public int ChapterId;
        public int PuzzleId;
        public int StateOrdinal;

        public PuzzleStateEnum State
        {
            get => (PuzzleStateEnum)StateOrdinal;
            set => StateOrdinal = (int)value;
        }
    }
}
