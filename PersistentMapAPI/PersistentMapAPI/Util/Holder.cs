using BattleTech;
using System;
using System.Collections.Generic;

namespace PersistentMapAPI {
    public static class Holder {
        // The current state of the map
        public static StarMap currentMap;

        // List of all connections currently tracked
        public static Dictionary<string, UserInfo> connectionStore = new Dictionary<string, UserInfo>();

        // All results that have been posted
        public static List<HistoryResult> resultHistory = new List<HistoryResult>();
        
        // When the web-service started
        public static DateTime startupTime = DateTime.UtcNow;

        // All of the player contributed items available to a faction
        public static Dictionary<Faction, List<ShopDefItem>> factionInventories;

        // All of the items a faction has for sale
        public static List<FactionShop> factionShops;

        // When the last backup occurred
        public static DateTime lastBackup = DateTime.MinValue;
    }
}
