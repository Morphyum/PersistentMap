using Newtonsoft.Json;
using PersistentMapAPI;
using PersistentMapServer.Objects;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace PersistentMapServer.Worker {

    /* BackgroundWorker responsible for backing up in-memory copies of system data and writing it to 
     *  disk periodically. Currently manages backups for:
     *    * StarMap (and derived objects)
     */ 
    public class BackupWorker {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static TimeSpan backupTimeSpan {
            get {
                double hours = Helper.LoadSettings().HoursPerBackup;
                return TimeSpan.FromHours(hours);
            }
        } 

        // When the last backup occurred
        private static DateTime lastBackupTime = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));

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
        public static void ProcessExitHandler(object sender, EventArgs e) {
            logger.Info("Performing backups before process exit");

            logger.Info("Saving current StarMap");
            PeriodicBackup();

            lastBackupTime = DateTime.UtcNow;
            Thread.Sleep(10 * 1000);
        }

        private static void PeriodicBackup() {
            // Create the backup path if it doesn't exist
            (new FileInfo(Helper.currentMapFilePath)).Directory.Create();
            StarMap mapToSave = StarMapBuilder.Build();

            SaveAsCurrent(mapToSave);
            SaveAsBackup(mapToSave);
        }

        public static void SaveAsCurrent(StarMap mapToWrite) {
            // Create the backup path if it doesn't exist
            (new FileInfo(Helper.currentMapFilePath)).Directory.Create();

            // Write the map as current.json
            string json = JsonConvert.SerializeObject(mapToWrite);
            logger.Debug("Writing current.json");
            using (StreamWriter writer = new StreamWriter(Helper.currentMapFilePath, false)) {
                writer.Write(json);
            }
        }

        public static void SaveAsBackup(StarMap mapToWrite) {
            // Create the backup path if it doesn't exist
            (new FileInfo(Helper.currentMapFilePath)).Directory.Create();

            // Writes the map as yyyy-dd-M--HH-mm-ss.json
            string backupPath = Helper.backupMapFilePath + DateTime.UtcNow.ToString(Helper.DateFormat) + ".json";
            string json = JsonConvert.SerializeObject(mapToWrite);
            logger.Debug($"Writing backup file {backupPath}");
            using (StreamWriter writer = new StreamWriter(backupPath, false)) {
                writer.Write(json);
            }
        }

    }

}
