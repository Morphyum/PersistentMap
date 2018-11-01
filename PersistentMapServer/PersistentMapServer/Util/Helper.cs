using BattleTech;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using SysSecurity = System.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;
using SysText = System.Text;
using System.Threading;
using PersistentMapServer.Objects;

namespace PersistentMapAPI {
    public static class Helper {

        public static string currentMapFilePath = $"../Map/current.json";
        public static string backupMapFilePath = $"../Map/";
        public static string currentShopFilePath = $"../Shop/current.json";
        public static string settingsFilePath = $"../Settings/settings.json";
        public static string systemDataFilePath = $"../StarSystems/";

        public static readonly string DateFormat = "yyyy-dd-M--HH-mm-ss";

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Settings that have been previously read
        private static Settings cachedSettings;

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
                logger.Warn(e, "Failed to load current inventories.");
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
                                logger.Debug("Reading settings from disk");
                            }
                            wasAbleToRead = true;
                        } catch (IOException) {
                            // Handle race conditions when the file has been modified (in an editor) but the lock isn't released yet.
                            logger.Trace("Failed to open settings.json due to lock, waiting 5ms");
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
                        logger.Warn("Writing default settings");
                    }
                }
                cachedSettings = settings;
            }
            return settings;
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
                logger.Debug($"Added {item.ID} count {item.Count}");
            }

            Holder.factionInventories[realFaction].RemoveAll(x => x.Count <= 0);
            logger.Info($"New shop generated for faction ({realFaction})");
            return newShop;

        }

        // Stolen from https://stackoverflow.com/questions/33166679/get-client-ip-address-using-wcf-4-5-remoteendpointmessageproperty-in-load-balanc
        // Note: Old method didn't account for load-balancer
        public static string mapRequestIP() {
            OperationContext context = OperationContext.Current;
            MessageProperties properties = context.IncomingMessageProperties;
            RemoteEndpointMessageProperty endpoint = properties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            string address = string.Empty;
            if (properties.Keys.Contains(HttpRequestMessageProperty.Name)) {
                HttpRequestMessageProperty endpointLoadBalancer = properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
                if (endpointLoadBalancer != null && endpointLoadBalancer.Headers["X-Forwarded-For"] != null)
                    address = endpointLoadBalancer.Headers["X-Forwarded-For"];
            }
            if (string.IsNullOrEmpty(address)) {
                address = endpoint.Address;
            }
            return address;
        }


        // Stolen from https://stackoverflow.com/questions/3984138/hash-string-in-c-sharp
        internal static string HashAndTruncate(string text) {
            if (String.IsNullOrEmpty(text))
                return String.Empty;

            using (var sha = new SysSecurity.Cryptography.SHA256Managed()) {
                byte[] textData = SysText.Encoding.UTF8.GetBytes(text);
                byte[] hash = sha.ComputeHash(textData);
                String convertedHash = BitConverter.ToString(hash).Replace("-", String.Empty);
                String truncatedHash = convertedHash.Length > 12 ? convertedHash.Substring(0, 11) : convertedHash.Substring(0, convertedHash.Length);
                return truncatedHash + "...";
            }
        }

        // TODO: Generates fake user activity for local testing
        public static List<UserInfo> GenerateFakeActivity() {

            List<UserInfo> randos = new List<UserInfo>(20);
            var random = new Random();
            int count = random.Next(5, 20); // No more than twenty
            logger.Info($"Generating {count} companies");

            // Ensure that the map has been loaded
            StarMapBuilder.Build();

            int systemCount = Holder.currentMap.systems.Count;
            int numSystems = random.Next(1, count);
            List<string> systems = new List<string>(count);
            for (int i = 0; i < numSystems; i++) {
                int systemId = random.Next(0, systemCount - 1);
                logger.Info($"Using systemID {systemId}");
                systems.Add(Holder.currentMap.systems.ElementAt(systemId).name);
            }
            logger.Info($"Generated {numSystems} systems");

            for (int i = 0; i < count; i++) {
                UserInfo randomUser = new UserInfo();
                randomUser.companyName = GenerateFakeCompanyName(random);
                randomUser.LastDataSend = DateTime.UtcNow;

                int systemId = random.Next(0, systems.Count - 1);
                randomUser.lastSystemFoughtAt = systems[systemId];
                randos.Add(randomUser);
                logger.Info($"Adding randomUser - Company {randomUser.companyName} at system {randomUser.lastSystemFoughtAt}");
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
