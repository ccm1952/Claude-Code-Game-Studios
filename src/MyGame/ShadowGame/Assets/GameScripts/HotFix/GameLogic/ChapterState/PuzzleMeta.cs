// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Static metadata for a single puzzle, sourced from Luban <c>TbPuzzle</c>.
    /// Used by <see cref="ChapterStateManager.Init(IChapterProgress, PuzzleMeta[][])"/>
    /// to drive puzzle ordering within a chapter.
    /// <para>
    /// In production <c>PuzzleOrder</c> is read from <c>TbPuzzle.PuzzleOrder</c>
    /// (1-based per chapter). In tests and in the legacy <c>int[] puzzlesPerChapter</c>
    /// overload, <c>PuzzleOrder</c> defaults to <c>PuzzleId</c>.
    /// </para>
    /// </summary>
    public readonly struct PuzzleMeta
    {
        public readonly int ChapterId;
        public readonly int PuzzleId;
        public readonly int PuzzleOrder;

        public PuzzleMeta(int chapterId, int puzzleId, int puzzleOrder)
        {
            ChapterId = chapterId;
            PuzzleId = puzzleId;
            PuzzleOrder = puzzleOrder;
        }
    }
}
