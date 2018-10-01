using BattleTech;
using System;
using System.Collections.Generic;

namespace PersistentMapAPI {
    public static class Holder {
        public static StarMap currentMap;
        public static Dictionary<string, UserInfo> connectionStore = new Dictionary<string, UserInfo>();
        public static List<HistoryResult> resultHistory = new List<HistoryResult>();
        public static DateTime startupTime = DateTime.UtcNow;
        public static Dictionary<Faction, List<ShopDefItem>> factionInventories;
        public static List<FactionShop> factionShops;
        public static DateTime lastBackup = DateTime.MinValue;
    }
}
