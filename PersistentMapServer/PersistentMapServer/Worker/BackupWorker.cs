using Newtonsoft.Json;
using PersistentMapAPI;
using PersistentMapServer.Objects;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace PersistentMapServer.Worker {

    /* BackgroundWorker responsible for backing up in-memory copies of system data and writing it to 
     *  disk periodically. Currently manages backups for:
     *    * StarMap (and derived objects)
     */
    public class BackupWorker {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Modified ISO 8601 format, replacing colons with - to make it windows friendlier
        public static readonly string DateFormat = "yyyy-MM-ddTHH-mm-ssZ";

        private static TimeSpan backupTimeSpan {
            get {
                var settings = Helper.LoadSettings();
                double minutes = settings.MinutesPerBackup != 0 ? settings.MinutesPerBackup : settings.HoursPerBackup * 60;
                return TimeSpan.FromMinutes(minutes);
            }
        } 

        // When the last backup occurred
        public static DateTime lastBackupTime = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1));

        public static void DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker bw = sender as BackgroundWorker;

            while (!bw.CancellationPending) {

                // If more than the lastBackupTime has passed, run the backup
                DateTime now = DateTime.UtcNow;
                if (now.Subtract(backupTimeSpan) > lastBackupTime) {
                    logger.Debug("Performing scheduled backup");
                    PeriodicBackup();
                    lastBackupTime = now;
                }

                // Sleep a short period to see if we are cancelled.
                Thread.Sleep(50);
            }
        }

        public static void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            logger.Debug("Shutting down BackupWorker");
        }

        // Invoked when the system is being shutdown
        public static void BackupOnExit() {
            logger.Info("Performing backups before process exit...");

            PeriodicBackup();

            lastBackupTime = DateTime.UtcNow;
            Thread.Sleep(2 * 1000);
            logger.Info("Complete!");
        }

        private static void PeriodicBackup() {
            // Create the backup path if it doesn't exist
            (new FileInfo(StarMapBuilder.MapFileDirectory)).Directory.Create();

            // Save the map
            var mapToSave = StarMapBuilder.Build();
            string mapAsJson = JsonConvert.SerializeObject(mapToSave);
            logger.Info("Saving StarMap");
            WriteBoth(StarMapBuilder.MapFileDirectory, mapAsJson);

            // Save faction inventories
            var inventoriesToSave = FactionInventoryBuilder.Build();
            string inventoryAsJson = JsonConvert.SerializeObject(inventoriesToSave);
            logger.Info("Saving Faction Inventories");
            WriteBoth(FactionInventoryBuilder.ShopFileDirectory, inventoryAsJson);

            lastBackupTime = DateTime.UtcNow;
        }

        public static void WriteBoth(string directory, string objectAsJson) {
            // Create the backup path if it doesn't exist
            (new FileInfo(directory)).Directory.Create();

            // Write as current.json
            string currentFilePath = Path.Combine(directory, $"current.json");
            logger.Trace($"Writing current file as {currentFilePath}");
            using (StreamWriter writer = new StreamWriter(currentFilePath, false)) {
                writer.Write(objectAsJson);
            }

            // Write compressed backup as backup.{dateStamp}.json.gz
            string dateFragment = DateTime.UtcNow.ToString(BackupWorker.DateFormat);
            string backupFilePath = Path.Combine(directory, $"backup.{dateFragment}.json.gz");
            logger.Trace($"Writing backup file as {backupFilePath}");
            using (FileStream outFile = File.Create(backupFilePath))
            using (GZipStream compress = new GZipStream(outFile, CompressionMode.Compress))
            using (StreamWriter writer = new StreamWriter(compress)) {
                writer.Write(objectAsJson);
            }
        }     

    }

}
