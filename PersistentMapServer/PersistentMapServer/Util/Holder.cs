using BattleTech;
using PersistentMapServer.Objects;
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

        // History of results organized by player
        public static HashSet<PlayerHistory> playerHistory = 
            new HashSet<PlayerHistory>(new PlayerHistoryComparer());

        // When the web-service started
        public static DateTime startupTime = DateTime.UtcNow;

        // All of the player contributed items available to a faction
        public static Dictionary<Faction, List<ShopDefItem>> factionInventories;

        // All of the items a faction has for sale
        public static List<FactionShop> factionShops;

    }

    // Comparator that only looks at ID values
    class PlayerHistoryComparer : IEqualityComparer<PlayerHistory> {

        public bool Equals(PlayerHistory x, PlayerHistory y) {
            if (x == null && y == null)
                return true;
            else if (x == null || y == null)
                return false;
            else if (x.Id == y.Id)
                return true;
            else
                return false;
        }

        public int GetHashCode(PlayerHistory obj) {
            return obj.Id.GetHashCode();
        }
    }
}
