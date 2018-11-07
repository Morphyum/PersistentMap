using PersistentMapAPI;
using PersistentMapServer.Objects;
using System;
using System.ComponentModel;
using System.Threading;

namespace PersistentMapServer.Worker {

    /* BackgroundWorker responsible for pruning player history to keep the memory and storage constraints light.
     */
    public class PlayerHistoryPruner {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Modified ISO 8601 format, replacing colons with - to make it windows friendlier
        public static readonly string DateFormat = "yyyy-MM-ddTHH-mm-ssZ";

        // The amount of time after which the files will be pruned
        private static TimeSpan pruneAfterSpan = TimeSpan.FromHours(12);

        // The number of days to retain history for
        private static int MaxRetentionInDays = 30;

        // When the last pruning occured occurred
        public static DateTime lastPrune = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1));

        public static void DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker bw = sender as BackgroundWorker;

            while (!bw.CancellationPending) {

                // If more than the lastBackupTime has passed, run the backup
                DateTime now = DateTime.UtcNow;
                if (now.Subtract(pruneAfterSpan) > lastPrune) {
                    logger.Debug("Performing scheduled player history pruning");
                    var prunedRecords = PrunePlayerHistory();
                    logger.Debug($"Pruned {prunedRecords} from player history.");
                    lastPrune = now;
                }

                // Sleep a short period to see if we are cancelled.
                Thread.Sleep(50);
            }
        }

        public static void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            logger.Debug("Shutting down PlayerHistoryPruner");
        }

        // Invoked when the system is being shutdown
        public static void PruneOnExit() {
            logger.Info("Pruning history process exit...");
            var prunedRecords = PrunePlayerHistory();
            logger.Info($"Pruned {prunedRecords} from player history");

        }

        private static int PrunePlayerHistory() {
            int prunedRecords = 0;
            DateTime pruneOnOrBefore = DateTime.UtcNow.Subtract(TimeSpan.FromDays(MaxRetentionInDays));
            foreach (PlayerHistory playerHistory in Holder.playerHistory) {
                prunedRecords += playerHistory.activities.RemoveWhere(x => x.resultTime <= pruneOnOrBefore);
            }
            return prunedRecords;
        }

    }

}
