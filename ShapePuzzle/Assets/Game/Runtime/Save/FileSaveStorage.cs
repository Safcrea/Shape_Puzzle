using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ToyPuzzle
{
    public sealed class FileSaveStorage : ISaveStorage
    {
        public const string DefaultFileName = "toy_puzzle_save.json";

        private readonly string primaryPath;
        private readonly string backupPath;
        private readonly string temporaryPath;

        public FileSaveStorage(string primaryPath)
        {
            if (string.IsNullOrWhiteSpace(primaryPath))
            {
                throw new ArgumentException("A save path is required.", nameof(primaryPath));
            }

            this.primaryPath = primaryPath;
            backupPath = primaryPath + ".bak";
            temporaryPath = primaryPath + ".tmp";
        }

        public string PrimaryPath => primaryPath;
        public string BackupPath => backupPath;

        public static FileSaveStorage CreateDefault()
        {
            return new FileSaveStorage(Path.Combine(Application.persistentDataPath, DefaultFileName));
        }

        public bool TryReadPrimary(out string json)
        {
            return TryRead(primaryPath, out json);
        }

        public bool TryReadBackup(out string json)
        {
            return TryRead(backupPath, out json);
        }

        public void WriteAtomically(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            string directory = Path.GetDirectoryName(primaryPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
            if (!File.Exists(primaryPath))
            {
                File.Move(temporaryPath, primaryPath);
                return;
            }

            File.Copy(primaryPath, backupPath, true);
            try
            {
                File.Replace(temporaryPath, primaryPath, null);
            }
            catch (PlatformNotSupportedException)
            {
                ReplaceByCopy();
            }
            catch (IOException)
            {
                ReplaceByCopy();
            }
        }

        public void PreserveCorruptPrimary()
        {
            if (!File.Exists(primaryPath))
            {
                return;
            }

            string directory = Path.GetDirectoryName(primaryPath) ?? string.Empty;
            string name = Path.GetFileNameWithoutExtension(primaryPath);
            string extension = Path.GetExtension(primaryPath);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fffffff");
            string corruptPath = Path.Combine(directory, $"{name}.corrupt.{stamp}{extension}");
            File.Move(primaryPath, corruptPath);
        }

        private static bool TryRead(string path, out string json)
        {
            if (!File.Exists(path))
            {
                json = null;
                return false;
            }

            try
            {
                json = File.ReadAllText(path, Encoding.UTF8);
                return true;
            }
            catch (IOException)
            {
                json = null;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                json = null;
                return false;
            }
        }

        private void ReplaceByCopy()
        {
            File.Copy(temporaryPath, primaryPath, true);
            File.Delete(temporaryPath);
        }
    }
}
