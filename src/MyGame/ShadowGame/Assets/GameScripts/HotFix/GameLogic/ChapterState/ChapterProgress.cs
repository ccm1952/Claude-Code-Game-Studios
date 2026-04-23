// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Runtime snapshot of a single chapter's progress. Owned exclusively by
    /// <see cref="ChapterStateManager"/>.
    /// </summary>
    public class ChapterProgress
    {
        public int ChapterId;
        public bool IsUnlocked;
        public bool IsCompleted;
        public PuzzleProgress[] Puzzles;
    }

    /// <summary>
    /// Runtime state for a single puzzle within a chapter.
    /// </summary>
    public class PuzzleProgress
    {
        public int PuzzleId;
        public PuzzleStateEnum State;
        public int AttemptCount;
        public float BestMatchScore;
    }
}
