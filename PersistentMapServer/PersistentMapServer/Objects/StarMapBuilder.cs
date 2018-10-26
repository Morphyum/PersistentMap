using BattleTech;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PersistentMapAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PersistentMapServer.Objects {

    class StarMapBuilder {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly object _deserializationLock = new Object();
        private static readonly object _updateLock = new Object();

        public static StarMap Build() {
            // If the holder is empty, we're on startup. Deserialize the most recent backup, or initialize a new map
            lock(_deserializationLock) {
                if (Holder.currentMap == null) {
                    readOrInitialize();
                }
            }

            // Update the map with the current connections
            lock (_updateLock) {                
                Dictionary<string, List<UserInfo>> activePlayers = GetSystemsWithActivePlayers(Holder.currentMap.systems.Count);
                foreach (PersistentMapAPI.System system in Holder.currentMap.systems) {
                    if (activePlayers.ContainsKey(system.name)) {
                        List<UserInfo> systemUsers = activePlayers[system.name];
                        system.activePlayers = systemUsers.Count;
                        system.companies = systemUsers.Select(p => p.companyName).ToList();
                        logger.Trace($"Mapping {systemUsers.Count} activePlayers to {system} system.");
                    }
                }
            }
            return Holder.currentMap;
        }

        // Find all of the players that have been active within N minutes. Return their UserInfos keyed by system name
        private static Dictionary<string, List<UserInfo>> GetSystemsWithActivePlayers(int numSystems) {
            PersistentMapAPI.Settings settings = Helper.LoadSettings();
            DateTime now = DateTime.UtcNow;
            Dictionary<string, List<UserInfo>> playersBySystem = Holder.connectionStore
                .AsParallel()
                .Where(x => x.Value.LastDataSend.AddMinutes(settings.MinutesForActive) > now)
                .AsSequential()
                .GroupBy(p => p.Value.lastSystemFoughtAt)
                .ToDictionary(p => p.Key, p => p.Select(g => g.Value).ToList());
            logger.Trace($"Mapped players to { playersBySystem.Keys.Count} systems.");

            return playersBySystem;
        }

        // Read the system data from disk, or create a new copy
        private static void readOrInitialize() {
            if (File.Exists(Helper.currentMapFilePath)) {
                using (StreamReader r = new StreamReader(Helper.currentMapFilePath)) {
                    string json = r.ReadToEnd();
                    StarMap mapFromDisk = JsonConvert.DeserializeObject<StarMap>(json);
                    Holder.currentMap = mapFromDisk;
                }
            } else {
                Holder.currentMap = initializeNewMap();
            }
        }

        // Create a new StarMap from InnerSphereMap system data
        private static StarMap initializeNewMap() {
            logger.Info("Initializing map from InnerSphereMap system data.");
            StarMap map = new StarMap();
            map.systems = new List<PersistentMapAPI.System>();

            foreach (string filePaths in Directory.GetFiles(Helper.systemDataFilePath)) {
                string originalJson = File.ReadAllText(filePaths);
                JObject originalJObject = JObject.Parse(originalJson);
                Faction owner = (Faction)Enum.Parse(typeof(Faction), (string)originalJObject["Owner"]);

                FactionControl ownerControl = new FactionControl();
                ownerControl.faction = owner;
                if (owner != Faction.NoFaction) {
                    ownerControl.percentage = 100;
                } else {
                    ownerControl.percentage = 0;
                }

                PersistentMapAPI.System system = new PersistentMapAPI.System();
                system.controlList = new List<FactionControl>();
                system.name = (string)originalJObject["Description"]["Name"];
                system.controlList.Add(ownerControl);

                map.systems.Add(system);
            }
            return map;
        }

    }
}
