using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentMapAPI.Objects {
    // Point in time overview of the size of internal data elements
    public class ServiceDataSnapshot {

        public int num_connections_active = 0;
        public int num_connections_inactive = 0;
        public float percent_connections_active = 0.0f;
            
        public int size_resultHistory = 0;
        public int size_factionInventory = 0;
        public int size_factionShopsInventory = 0;

        public ServiceDataSnapshot() {
            Settings settings = Helper.LoadSettings();

            DateTime activeOnOrAfter = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(settings.MinutesForActive));
            int size_connectionStore = Holder.connectionStore.Count;
            num_connections_active = Holder.connectionStore
                .Where(x => x.Value.LastDataSend >= activeOnOrAfter)
                .Count();
            num_connections_inactive = size_connectionStore - num_connections_active;
            percent_connections_active = size_connectionStore == 0 ? 
                0.0f : (size_connectionStore - num_connections_inactive) / (size_connectionStore);

            size_resultHistory = Holder.resultHistory.Count;

        }
    }

    /*
     *         // The current state of the map
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
        */
}
