using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BarangayVillaMTejeroSystem.Data;
using Microsoft.Data.Sqlite;

namespace BarangayVillaMTejeroSystem.Services
{
    /// <summary>
    /// One backup file on disk, listed in the Backup && Restore module.
    /// </summary>
    public class BackupFileInfo
    {
        public string FileName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public long SizeBytes { get; set; }

        public string SizeLabel => SizeBytes < 1024 * 1024
            ? $"{SizeBytes / 1024.0:0.0} KB"
            : $"{SizeBytes / (1024.0 * 1024.0):0.00} MB";

        public string CreatedLabel => CreatedAt.ToString("MMM d, yyyy  h:mm tt");
    }

    /// <summary>
    /// Manual backup/restore for the SQLite database used by the whole
    /// system. Backups live as plain .db files under Data\Backups so they
    /// can also be copied to a USB drive / cloud folder outside the app.
    ///
    /// Backups are taken with SQLite's own online-backup API
    /// (SqliteConnection.BackupDatabase) rather than a raw file copy, since
    /// that is the safe way to snapshot a SQLite file that the app itself
    /// still has open. Restores clear the connection pool first so the live
    /// file can be replaced cleanly, and always take an automatic
    /// "pre-restore" safety snapshot before overwriting anything.
    /// </summary>
    public static class BackupService
    {
        private static readonly string BackupsDirectory =
            Path.Combine(Path.GetDirectoryName(DatabaseHelper.DbPath)!, "Backups");

        /// <summary>Creates a timestamped backup of the live database and returns its path.</summary>
        public static string CreateBackup()
        {
            Directory.CreateDirectory(BackupsDirectory);
            string fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            string destPath = Path.Combine(BackupsDirectory, fileName);

            using (var source = DatabaseHelper.CreateOpenConnection())
            using (var destination = new SqliteConnection($"Data Source={destPath}"))
            {
                destination.Open();
                source.BackupDatabase(destination);
            }

            return destPath;
        }

        /// <summary>Lists all backup files, newest first.</summary>
        public static List<BackupFileInfo> GetBackups()
        {
            Directory.CreateDirectory(BackupsDirectory);
            return Directory.GetFiles(BackupsDirectory, "*.db")
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new BackupFileInfo
                {
                    FileName = f.Name,
                    FullPath = f.FullName,
                    CreatedAt = f.CreationTime,
                    SizeBytes = f.Length
                })
                .ToList();
        }

        /// <summary>Quick sanity check that a file is actually a readable SQLite database before trusting it.</summary>
        public static bool IsValidDatabaseFile(string path)
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT count(*) FROM sqlite_master;";
                cmd.ExecuteScalar();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Restores the live database from the given backup file. Takes an
        /// automatic safety snapshot of the current database first (returned
        /// as the pre-restore file name) so a wrong or bad restore can always
        /// be undone from the backup list.
        /// </summary>
        public static string RestoreFromFile(string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
                throw new FileNotFoundException("Backup file not found.", backupFilePath);

            if (!IsValidDatabaseFile(backupFilePath))
                throw new InvalidOperationException("That file doesn't look like a valid backup database.");

            Directory.CreateDirectory(BackupsDirectory);
            string preRestoreFileName = $"pre_restore_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            string preRestorePath = Path.Combine(BackupsDirectory, preRestoreFileName);

            using (var source = DatabaseHelper.CreateOpenConnection())
            using (var snapshot = new SqliteConnection($"Data Source={preRestorePath}"))
            {
                snapshot.Open();
                source.BackupDatabase(snapshot);
            }

            // Release every pooled connection to the live file so it isn't
            // locked when we overwrite it below.
            SqliteConnection.ClearAllPools();

            File.Copy(backupFilePath, DatabaseHelper.DbPath, overwrite: true);

            return preRestoreFileName;
        }

        /// <summary>Copies a backup file to an arbitrary destination (e.g. a USB drive) chosen by the user.</summary>
        public static void ExportTo(string sourcePath, string destinationPath) =>
            File.Copy(sourcePath, destinationPath, overwrite: true);

        public static void DeleteBackup(string path) => File.Delete(path);

        public static (string Path, long SizeBytes, DateTime LastModified) GetCurrentDatabaseInfo()
        {
            var fi = new FileInfo(DatabaseHelper.DbPath);
            return (DatabaseHelper.DbPath, fi.Exists ? fi.Length : 0, fi.Exists ? fi.LastWriteTime : DateTime.MinValue);
        }
    }
}
