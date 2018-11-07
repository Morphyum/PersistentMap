using Newtonsoft.Json;
using PersistentMapAPI;
using PersistentMapServer.Worker;
using System;
using System.Collections.Generic;
using System.IO;

namespace PersistentMapServer.Objects {

    class PlayerStateManager {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static string StoragePath = $"../Players/";

        private static readonly object _deserializationLock = new Object();
        private static readonly object _updateLock = new Object();

        public static HashSet<PlayerHistory> Build() {
            // If the holder is empty, we're on startup. Deserialize the most recent backup, or initialize a collection
            lock(_deserializationLock) {
                if (Holder.playerHistory == null) {
                    ReadOrInitialize();
                }
            }

            return Holder.playerHistory;
        }

        // Replace the map with a new map from files
        public static void Reset() {
            lock(_deserializationLock) {
                lock (_updateLock) {
                    logger.Info("Backing up existing player history");
                    string objectToSave = JsonConvert.SerializeObject(Holder.playerHistory);
                    BackupWorker.WriteBoth(PlayerStateManager.StoragePath, objectToSave);
                    Holder.playerHistory = null;
                    Holder.playerHistory = new HashSet<PlayerHistory>();                    
                }
            }
        }

        // Read the system data from disk, or create a new copy
        private static void ReadOrInitialize() {
            List<PlayerHistory> historyFromDisk;
            string filePath = Path.Combine(PlayerStateManager.StoragePath, "current.json");
            if (File.Exists(filePath)) {
                using (StreamReader r = new StreamReader(filePath)) {
                    string json = r.ReadToEnd();
                    historyFromDisk = JsonConvert.DeserializeObject<List<PlayerHistory>>(json);                    
                }
            } else {
                logger.Debug("No player history found, initializing as empty.");
                historyFromDisk = new List<PlayerHistory>();
            }
            Holder.playerHistory = new HashSet<PlayerHistory>(historyFromDisk);
        }

    }
}
