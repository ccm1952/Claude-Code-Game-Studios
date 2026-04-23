// 该文件由Cursor 自动生成
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Handles atomic save/load with CRC32 integrity and backup files (ADR-008).
    /// All I/O is async via UniTask. A <see cref="SemaphoreSlim"/> prevents
    /// concurrent writes from race-corrupting files.
    /// </summary>
    public class SaveManager
    {
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly List<ISaveMigration> _migrations = new List<ISaveMigration>();

        /// <summary>
        /// Register a migration step. Migrations are applied in order of
        /// <see cref="ISaveMigration.FromVersion"/> during load.
        /// </summary>
        public void RegisterMigration(ISaveMigration migration)
        {
            _migrations.Add(migration);
            _migrations.Sort((a, b) => a.FromVersion.CompareTo(b.FromVersion));
        }

        /// <summary>
        /// Atomically persist <paramref name="data"/> to disk.
        /// Sequence: serialize → CRC → write temp → rename → backup.
        /// </summary>
        public async UniTask SaveAsync(SaveData data)
        {
            await _writeLock.WaitAsync();
            try
            {
                data.lastSaveTimestampUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                string json = JsonUtility.ToJson(data, false);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                string crcHex = Crc32.ComputeHex(bytes);

                string tmpJson = SavePaths.Temp;
                string tmpCrc = tmpJson + ".crc";

                EnsureDirectory();

                await WriteFileAsync(tmpJson, json);
                await WriteFileAsync(tmpCrc, crcHex);

                ReplaceFile(tmpJson, SavePaths.Primary);
                ReplaceFile(tmpCrc, SavePaths.PrimaryCrc);

                CopyFile(SavePaths.Primary, SavePaths.Backup);
                CopyFile(SavePaths.PrimaryCrc, SavePaths.BackupCrc);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e.Message}");
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <summary>
        /// Async load with fallback chain: primary → backup → null (fresh start).
        /// Applies registered migrations if save version &lt; current.
        /// </summary>
        public async UniTask<SaveData> LoadAsync()
        {
            var data = await TryLoadFileAsync(SavePaths.Primary, SavePaths.PrimaryCrc);
            if (data != null)
                return ApplyMigrations(data);

            Debug.LogWarning("[SaveManager] Primary save corrupt; falling back to backup.");
            data = await TryLoadFileAsync(SavePaths.Backup, SavePaths.BackupCrc);
            if (data != null)
                return ApplyMigrations(data);

            Debug.LogWarning("[SaveManager] Both saves corrupt; starting fresh.");
            return null;
        }

        /// <summary>
        /// Synchronous load — same fallback chain, for boot-time use.
        /// </summary>
        public SaveData Load()
        {
            var data = TryLoadAndVerify(SavePaths.Primary, SavePaths.PrimaryCrc);
            if (data != null) return ApplyMigrations(data);

            Debug.LogWarning("[SaveManager] Primary save invalid, trying backup");
            data = TryLoadAndVerify(SavePaths.Backup, SavePaths.BackupCrc);
            if (data != null) return ApplyMigrations(data);

            Debug.LogWarning("[SaveManager] No valid save found, returning null (new game)");
            return null;
        }

        /// <summary>Delete save files but NOT PlayerPrefs settings (ADR-008).</summary>
        public void DeleteSave()
        {
            TryDelete(SavePaths.Primary);
            TryDelete(SavePaths.PrimaryCrc);
            TryDelete(SavePaths.Backup);
            TryDelete(SavePaths.BackupCrc);
            TryDelete(SavePaths.Temp);
            TryDelete(SavePaths.Temp + ".crc");
        }

        private SaveData ApplyMigrations(SaveData data)
        {
            while (data.version < SaveData.CURRENT_VERSION)
            {
                ISaveMigration migration = null;
                for (int i = 0; i < _migrations.Count; i++)
                {
                    if (_migrations[i].FromVersion == data.version)
                    {
                        migration = _migrations[i];
                        break;
                    }
                }

                if (migration == null)
                {
                    Debug.LogWarning(
                        $"[SaveManager] No migration from v{data.version} to v{data.version + 1}, skipping");
                    break;
                }

                Debug.Log($"[SaveManager] Migrating save v{data.version} → v{data.version + 1}");
                data = migration.Migrate(data);
            }
            return data;
        }

        private async UniTask<SaveData> TryLoadFileAsync(string jsonPath, string crcPath)
        {
            if (!File.Exists(jsonPath) || !File.Exists(crcPath))
                return null;

            try
            {
                byte[] jsonBytes;
                using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read,
                           FileShare.Read, 4096, useAsync: true))
                {
                    jsonBytes = new byte[stream.Length];
                    await stream.ReadAsync(jsonBytes, 0, jsonBytes.Length);
                }

                string json = Encoding.UTF8.GetString(jsonBytes);
                string storedCrc;
                using (var stream = new FileStream(crcPath, FileMode.Open, FileAccess.Read,
                           FileShare.Read, 4096, useAsync: true))
                {
                    var crcBytes = new byte[stream.Length];
                    await stream.ReadAsync(crcBytes, 0, crcBytes.Length);
                    storedCrc = Encoding.UTF8.GetString(crcBytes).Trim();
                }

                string computedCrc = Crc32.ComputeHex(jsonBytes);
                if (!string.Equals(storedCrc, computedCrc, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"[SaveManager] CRC mismatch: stored={storedCrc}, computed={computedCrc}");
                    return null;
                }

                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Async load error for {jsonPath}: {e.Message}");
                return null;
            }
        }

        private SaveData TryLoadAndVerify(string jsonPath, string crcPath)
        {
            if (!File.Exists(jsonPath) || !File.Exists(crcPath))
                return null;

            try
            {
                string json = File.ReadAllText(jsonPath, Encoding.UTF8);
                string storedCrc = File.ReadAllText(crcPath, Encoding.UTF8).Trim();

                byte[] bytes = Encoding.UTF8.GetBytes(json);
                string computedCrc = Crc32.ComputeHex(bytes);

                if (!string.Equals(storedCrc, computedCrc, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"[SaveManager] CRC mismatch: stored={storedCrc}, computed={computedCrc}");
                    return null;
                }

                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Failed to load {jsonPath}: {e.Message}");
                return null;
            }
        }

        private static void EnsureDirectory()
        {
            string dir = Path.GetDirectoryName(SavePaths.Primary);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static async UniTask WriteFileAsync(string path, string content)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write,
                FileShare.None, 4096, useAsync: true);
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        private static void ReplaceFile(string source, string destination)
        {
            if (File.Exists(destination))
                File.Delete(destination);
            File.Move(source, destination);
        }

        private static void CopyFile(string source, string destination)
        {
            File.Copy(source, destination, overwrite: true);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception e) { Debug.LogWarning($"[SaveManager] Delete failed: {path} — {e.Message}"); }
        }
    }
}
