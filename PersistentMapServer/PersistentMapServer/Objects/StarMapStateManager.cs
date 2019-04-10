using BattleTech;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PersistentMapAPI;
using PersistentMapAPI.Objects;
using PersistentMapServer.Util;
using PersistentMapServer.Worker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PersistentMapServer.Objects {

    class StarMapStateManager {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static string MapFileDirectory = $"../Map/";
        public static string systemDataFilePath = $"../StarSystems/";

        private static readonly object _deserializationLock = new Object();
        private static readonly object _updateLock = new Object();

        public static StarMap Build() {
            // If the holder is empty, we're on startup. Deserialize the most recent backup, or initialize a new map
            lock(_deserializationLock) {
                if (Holder.currentMap == null) {
                    ReadOrInitialize();
                }
            }

            // Update the map with the current connections
            lock (_updateLock) {                
                Dictionary<string, List<UserInfo>> activePlayers = GetSystemsWithActivePlayers(Holder.currentMap.systems.Count);
                foreach (PersistentMapAPI.System system in Holder.currentMap.systems) {
                    if (activePlayers.ContainsKey(system.name)) {
                        List<UserInfo> systemUsers = activePlayers[system.name];
                        system.activePlayers = systemUsers.Count;
                        system.companies = new List<Company>();
                        foreach (UserInfo user in systemUsers) {
                            Company company = new Company();
                            company.Name = user.companyName;
                            company.Faction = user.lastFactionFoughtForInWar;
                            system.companies.Add(company);
                        }
                        logger.Trace($"Mapping {systemUsers.Count} activePlayers to {system} system.");
                    } else {
                        // Remove any expired records
                        system.activePlayers = 0;
                        system.companies = new List<Company>();
                    }
                }
            }
            return Holder.currentMap;
        }

        // Replace the map with a new map from files
        public static void Reset() {
            lock(_deserializationLock) {
                lock (_updateLock) {
                    logger.Info("Backing up existing starmap");
                    string mapToSave = JsonConvert.SerializeObject(Holder.currentMap);
                    BackupWorker.WriteBoth(StarMapStateManager.MapFileDirectory, mapToSave);
                    Holder.currentMap = null;
                    Holder.currentMap = InitializeNewMap();
                }
            }
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
        private static void ReadOrInitialize() {
            StarMap mapFromDisk;
            string mapFilePath = Path.Combine(StarMapStateManager.MapFileDirectory, "current.json");
            if (File.Exists(mapFilePath)) {
                using (StreamReader r = new StreamReader(mapFilePath)) {
                    string json = r.ReadToEnd();
                    mapFromDisk = JsonConvert.DeserializeObject<StarMap>(json);
                    Holder.currentMap = mapFromDisk;
                }
            } else {
                mapFromDisk = InitializeNewMap();
            }
            Holder.currentMap = mapFromDisk;
        }

        // Create a new StarMap from InnerSphereMap system data
        private static StarMap InitializeNewMap() {
            logger.Warn("Initializing map from InnerSphereMap system data.");
            StarMap map = new StarMap();
            map.systems = new List<PersistentMapAPI.System>();

            foreach (string filePaths in Directory.GetFiles(StarMapStateManager.systemDataFilePath)) {
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
