// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Sequential migration step. Implementations are registered in order and
    /// applied when saved version &lt; <see cref="SaveData.CURRENT_VERSION"/>.
    /// </summary>
    public interface ISaveMigration
    {
        /// <summary>The schema version this migration upgrades FROM.</summary>
        int FromVersion { get; }

        /// <summary>
        /// Upgrade <paramref name="data"/> from <see cref="FromVersion"/>
        /// to <c>FromVersion + 1</c>. Must set <c>data.version</c> to the new value.
        /// </summary>
        SaveData Migrate(SaveData data);
    }
}
