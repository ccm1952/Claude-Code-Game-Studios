// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Lifecycle states for a single puzzle, ordered by progression.
    /// Ordinal values are serialized in save files — do NOT reorder existing entries.
    /// Append new entries at the end with an explicit ordinal.
    /// </summary>
    public enum PuzzleStateEnum : byte
    {
        Locked = 0,
        Idle = 1,
        Active = 2,
        NearMatch = 3,
        PerfectMatch = 4,
        AbsenceAccepted = 5,
        Complete = 6
    }
}
