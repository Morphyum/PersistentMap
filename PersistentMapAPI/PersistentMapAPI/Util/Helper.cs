using BattleTech;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace PersistentMapAPI {
    public static class Helper {
        public static string currentMapFilePath = $"../Map/current.json";
        public static string currentShopFilePath = $"../Shop/current.json";
        public static string settingsFilePath = $"../Settings/settings.json";
        public static string systemDataFilePath = $"../StarSystems/";


        public static StarMap initializeNewMap() {
            Console.WriteLine("Map Init Started");
            StarMap map = new StarMap();
            map.systems = new List<System>();
            foreach (string filePaths in Directory.GetFiles(systemDataFilePath)) {
                string originalJson = File.ReadAllText(filePaths);
                JObject originalJObject = JObject.Parse(originalJson);
                Faction owner = (Faction)Enum.Parse(typeof(Faction), (string)originalJObject["Owner"]);

                FactionControl ownerControl = new FactionControl();
                ownerControl.faction = owner;
                if (owner != Faction.NoFaction) {
                    ownerControl.percentage = 100;
                }
                else {
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

        public static StarMap LoadCurrentMap() {
            if (Holder.currentMap == null) {
                if (File.Exists(currentMapFilePath)) {
                    using (StreamReader r = new StreamReader(currentMapFilePath)) {
                        string json = r.ReadToEnd();
                        Holder.currentMap = JsonConvert.DeserializeObject<StarMap>(json);
                    }
                }
                else {
                    Holder.currentMap = initializeNewMap();
                }
            }
            StarMap result = Holder.currentMap;
            return result;
        }

        public static void SaveCurrentMap(StarMap map) {
            (new FileInfo(currentMapFilePath)).Directory.Create();
            using (StreamWriter writer = new StreamWriter(currentMapFilePath, false)) {
                string json = JsonConvert.SerializeObject(map);
                writer.Write(json);
            }
            Console.WriteLine("Map Saved");
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

        public static Settings LoadSettings() {
            Settings settings;
            if (File.Exists(settingsFilePath)) {
                using (StreamReader r = new StreamReader(settingsFilePath)) {
                    string json = r.ReadToEnd();
                    settings = JsonConvert.DeserializeObject<Settings>(json);
                }
            }
            else {
                settings = new Settings();
                (new FileInfo(settingsFilePath)).Directory.Create();
                using (StreamWriter writer = new StreamWriter(settingsFilePath, false)) {
                    string json = JsonConvert.SerializeObject(settings);
                    writer.Write(json);
                }
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

        public static bool CheckUserInfo(string ip, string systemname) {
            if (Holder.connectionStore.ContainsKey(ip)) {
                if (Holder.connectionStore[ip].LastDataSend.AddMinutes(LoadSettings().minMinutesBetweenPost) > DateTime.Now) {
                    return true;
                }
                else {
                    Holder.connectionStore[ip].LastDataSend = DateTime.UtcNow;
                    Holder.connectionStore[ip].lastSystemFoughtAt = systemname;
                }
            }
            else {
                UserInfo info = new UserInfo();
                info.LastDataSend = DateTime.UtcNow;
                info.lastSystemFoughtAt = systemname;
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
                        item.DiscountModifier += Helper.LoadSettings().DiscountPerItem;
                    }
                }
            }
            foreach(ShopDefItem item in newShop) {
                Logger.LogLine("Added " + item.ID + " Count" + item.Count);
            }
            Holder.factionInventories[realFaction].RemoveAll(x => x.Count <= 0);
            Helper.SaveCurrentInventories(Holder.factionInventories);
            return newShop;

        }
    }
}
