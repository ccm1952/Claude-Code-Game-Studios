// 该文件由Cursor 自动生成
using System.IO;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Canonical file paths for save data (ADR-008).
    /// All paths live under <see cref="Application.persistentDataPath"/>.
    /// </summary>
    public static class SavePaths
    {
        private static string Base => Application.persistentDataPath;

        public static string Primary    => Path.Combine(Base, "save.json");
        public static string PrimaryCrc => Path.Combine(Base, "save.crc");
        public static string Backup     => Path.Combine(Base, "save.backup.json");
        public static string BackupCrc  => Path.Combine(Base, "save.backup.crc");
        public static string Temp       => Path.Combine(Base, "save.tmp");
    }
}
