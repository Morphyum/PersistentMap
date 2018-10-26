using BattleTech;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace PersistentMapAPI {
    public static class Helper {
        public static string currentMapFilePath = $"../Map/current.json";
        public static string backupMapFilePath = $"../Map/";
        public static string currentShopFilePath = $"../Shop/current.json";
        public static string settingsFilePath = $"../Settings/settings.json";
        public static string systemDataFilePath = $"../StarSystems/";

        public static readonly string DateFormat = "yyyy-dd-M--HH-mm-ss";

        public static StarMap LoadCurrentMap() {
            if (Holder.currentMap == null) {
                if (File.Exists(currentMapFilePath)) {
                    using (StreamReader r = new StreamReader(currentMapFilePath)) {
                        string json = r.ReadToEnd();
                        Holder.currentMap = JsonConvert.DeserializeObject<StarMap>(json);
                    }
                } else {
                    Holder.currentMap = initializeNewMap();
                }
            }
            StarMap result = Holder.currentMap;
            return result;
        }

        // TODO: remove!
        public static StarMap initializeNewMap() {
            Logger.LogLine("Map Init Started");
            StarMap map = new StarMap();
            map.systems = new List<System>();

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

                System system = new System();
                system.controlList = new List<FactionControl>();
                system.name = (string)originalJObject["Description"]["Name"];
                system.controlList.Add(ownerControl);

                map.systems.Add(system);
            }
            return map;
        }

        public static void SaveCurrentMap(StarMap map) {
            (new FileInfo(currentMapFilePath)).Directory.Create();
            using (StreamWriter writer = new StreamWriter(currentMapFilePath, false)) {
                string json = JsonConvert.SerializeObject(map);
                writer.Write(json);
            }
            if(Holder.lastBackup.AddHours(Helper.LoadSettings().HoursPerBackup) < DateTime.UtcNow) {
                using (StreamWriter writer = new StreamWriter(backupMapFilePath + DateTime.UtcNow.ToString(DateFormat) +".json", false)) {
                    string json = JsonConvert.SerializeObject(map);
                    writer.Write(json);
                }
                Holder.lastBackup = DateTime.UtcNow;
            }
        }

        public static Dictionary<Faction, List<ShopDefItem>> LoadCurrentInventories() {
            try {
                if (Holder.factionInventories == null) {
                    if (File.Exists(currentShopFilePath)) {
                        using (StreamReader r = new StreamReader(currentShopFilePath)) {
                            string json = r.ReadToEnd();
                            Holder.factionInventories = JsonConvert.DeserializeObject<Dictionary<Faction, List<ShopDefItem>>>(json);
                        }
                    }
                    else {
                        Holder.factionInventories = new Dictionary<Faction, List<ShopDefItem>>();
                    }
                }
                Dictionary<Faction, List<ShopDefItem>> result = Holder.factionInventories;
                return result;
            }catch(Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public static void SaveCurrentInventories(Dictionary<Faction, List<ShopDefItem>> shop) {
            (new FileInfo(currentShopFilePath)).Directory.Create();
            using (StreamWriter writer = new StreamWriter(currentShopFilePath, false)) {
                string json = JsonConvert.SerializeObject(shop);
                writer.Write(json);
            }
        }

        // Settings that were previously read
        private static Settings cachedSettings;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Settings LoadSettings(bool forceRefresh=false) {
            Settings settings = null;
            if (! forceRefresh && cachedSettings != null) {
                // Cached settings can be returned
                settings = cachedSettings;
            } else {
                if (File.Exists(settingsFilePath)) {
                    // Load the files from disk
                    bool wasAbleToRead = false;
                    while (!wasAbleToRead) {
                        try {
                            using (StreamReader r = new StreamReader(settingsFilePath)) {
                                string json = r.ReadToEnd();
                                settings = JsonConvert.DeserializeObject<Settings>(json);
                                Logger.LogLine("Reading settings from disk");
                            }
                            wasAbleToRead = true;
                        } catch (IOException) {
                            // Handle race conditions when the file has been modified (in an editor) but the lock isn't released yet.
                            Logger.LogLine("Failed to open settings.json due to lock, waiting 5ms");
                            Thread.Sleep(5);
                        }
                    }
                } else {
                    // Create default settings
                    settings = new Settings();
                    (new FileInfo(settingsFilePath)).Directory.Create();
                    using (StreamWriter writer = new StreamWriter(settingsFilePath, false)) {
                        string json = JsonConvert.SerializeObject(settings);
                        writer.Write(json);
                        //Logger.LogLine("Writing new default settings");
                    }
                }
                cachedSettings = settings;
            }
            return settings;
        }

        public static string GetIP() {
            OperationContext context = OperationContext.Current;
            MessageProperties prop = context.IncomingMessageProperties;
            RemoteEndpointMessageProperty endpoint =
               prop[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            string ip = endpoint.Address;

            return ip;

        }

        public static bool CheckUserInfo(string ip, string systemname, string companyName) {
            if (Holder.connectionStore.ContainsKey(ip)) {
                if (Holder.connectionStore[ip].LastDataSend.AddMinutes(LoadSettings().minMinutesBetweenPost) > DateTime.UtcNow) {
                    return true;
                }
                else {
                    Holder.connectionStore[ip].LastDataSend = DateTime.UtcNow;
                    Holder.connectionStore[ip].lastSystemFoughtAt = systemname;
                    Holder.connectionStore[ip].companyName = companyName;
                }
            }
            else {
                UserInfo info = new UserInfo();
                info.LastDataSend = DateTime.UtcNow;
                info.lastSystemFoughtAt = systemname;
                info.companyName = companyName;
                Holder.connectionStore.Add(ip, info);
            }
            return false;

        }

        public static List<ShopDefItem> GenerateNewShop(Faction realFaction) {
            List<ShopDefItem> newShop = new List<ShopDefItem>();
            Random rand = new Random();
            if (Holder.factionInventories == null) {
                Helper.LoadCurrentInventories();
            }
            if (!Holder.factionInventories.ContainsKey(realFaction)) {
                Holder.factionInventories.Add(realFaction, new List<ShopDefItem>());
            }
            if(Holder.factionInventories[realFaction].Count <= 0) {
                return newShop;
            }
            int maxCount = Holder.factionInventories[realFaction].Max(x => x.Count);
            foreach (ShopDefItem item in Holder.factionInventories[realFaction].OrderByDescending(x => x.Count)) {
                if(newShop.Count >= Helper.LoadSettings().MaxItemsPerShop) {
                    break;
                }
                int rolledNumber = rand.Next(0, maxCount + 1);
                if (rolledNumber <= item.Count) {
                    while (rolledNumber < item.Count) {
                        if (newShop.FirstOrDefault(x => x.ID.Equals(item.ID)) == null) {
                            ShopDefItem newItem = new ShopDefItem(item);
                            newItem.Count = 1;
                            newShop.Add(newItem);
                            
                        }
                        else {
                            newShop.FirstOrDefault(x => x.ID.Equals(item.ID)).Count++;
                        }
                        item.Count--;
                        item.DiscountModifier = Math.Min(item.DiscountModifier + Helper.LoadSettings().DiscountPerItem, Helper.LoadSettings().DiscountCeiling);
                    }
                }
            }
            foreach(ShopDefItem item in newShop) {
                Logger.Debug("Added " + item.ID + " Count" + item.Count);
            }
            Holder.factionInventories[realFaction].RemoveAll(x => x.Count <= 0);
            Logger.LogLine("New Shop generated for " + realFaction);
            return newShop;

        }

        // TODO: Generates fake user activity for local testing
        public static List<UserInfo> GenerateFakeActivity() {

            List<UserInfo> randos = new List<UserInfo>(20);
            var random = new Random();
            int count = random.Next(5, 20); // No more than twenty
            Console.WriteLine($"Generating {count} companies");

            int systemCount = Holder.currentMap.systems.Count;
            int numSystems = random.Next(1, count);
            List<string> systems = new List<string>(count);
            for (int i = 0; i < numSystems; i++) {
                int systemId = random.Next(0, systemCount - 1);
                Console.WriteLine($"Using systemID {systemId}");
                systems.Add(Holder.currentMap.systems.ElementAt(systemId).name);
            }
            Console.WriteLine($"Generated {numSystems} systems");

            for (int i = 0; i < count; i++) {
                UserInfo randomUser = new UserInfo();
                randomUser.companyName = GenerateFakeCompanyName(random);
                randomUser.LastDataSend = DateTime.UtcNow;

                int systemId = random.Next(0, systems.Count - 1);
                randomUser.lastSystemFoughtAt = systems[systemId];
                randos.Add(randomUser);
                Console.WriteLine($"Adding randomUser - Company {randomUser.companyName} at system {randomUser.lastSystemFoughtAt}");
            }
            return randos;
        }

        // TODO: Generates fake company names
        private static String GenerateFakeCompanyName(Random random) {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[12];

            for (int i = 0; i < stringChars.Length; i++) {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new String(stringChars);
            return "Random_Company_" + finalString;
        }

    }
}
