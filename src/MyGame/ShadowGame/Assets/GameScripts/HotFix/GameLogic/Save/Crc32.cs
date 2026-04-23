// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Lookup-table CRC32 (IEEE 802.3 polynomial 0xEDB88320).
    /// Used for save-file integrity checks (ADR-008).
    /// </summary>
    public static class Crc32
    {
        private static readonly uint[] s_table = GenerateTable();

        private static uint[] GenerateTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
                table[i] = crc;
            }
            return table;
        }

        /// <summary>
        /// Compute CRC32 over <paramref name="data"/> and return the result
        /// as an 8-character uppercase hex string (e.g. "A3F2C1D4").
        /// </summary>
        public static string ComputeHex(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                    crc = (crc >> 8) ^ s_table[(crc ^ data[i]) & 0xFF];
            }
            return (~crc).ToString("X8");
        }

        /// <summary>Compute CRC32 and return the raw uint value.</summary>
        public static uint Compute(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                    crc = (crc >> 8) ^ s_table[(crc ^ data[i]) & 0xFF];
            }
            return ~crc;
        }
    }
}
