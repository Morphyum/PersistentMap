using BattleTech;
using Newtonsoft.Json;
using PersistentMapAPI;
using PersistentMapServer.Worker;
using System;
using System.Collections.Generic;
using System.IO;

namespace PersistentMapServer.Objects {

    class FactionInventoryBuilder {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static string ShopFileDirectory = $"../Shop/";

        private static readonly object _deserializationLock = new Object();
        private static readonly object _updateLock = new Object();

        public static Dictionary<Faction, List<ShopDefItem>> Build() {
            // If the holder is empty, we're on startup. Deserialize the most recent backup, or initialize a new map
            lock(_deserializationLock) {
                if (Holder.factionInventories == null) {
                    ReadOrInitialize();
                }
            }

            return Holder.factionInventories;
        }

        // Replace the map with a new map from files
        public static void Reset() {
            lock(_deserializationLock) {
                lock (_updateLock) {
                    logger.Info("Backing up existing inventory data");
                    string inventoryToSave = JsonConvert.SerializeObject(Holder.factionInventories);
                    BackupWorker.WriteBoth(FactionInventoryBuilder.ShopFileDirectory, inventoryToSave);
                    Holder.factionInventories = new Dictionary<Faction, List<ShopDefItem>>();
                }
            }
        }

        // Read the system data from disk, or create a new copy
        private static void ReadOrInitialize() {
            Dictionary<Faction, List<ShopDefItem>> inventoryFromDisk = new Dictionary<Faction, List<ShopDefItem>>();
            string filePath = Path.Combine(FactionInventoryBuilder.ShopFileDirectory, "current.json");
            if (File.Exists(filePath)) {
                using (StreamReader r = new StreamReader(filePath)) {
                    string json = r.ReadToEnd();
                    inventoryFromDisk = JsonConvert.DeserializeObject<Dictionary<Faction, List<ShopDefItem>>>(json);
                }
            }
            Holder.factionInventories = inventoryFromDisk;
        }

    }
}
