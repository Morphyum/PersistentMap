using BattleTech;
using PersistentMapAPI;
using PersistentMapServer.Worker;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistentMapServer.Objects {
    // Point in time overview of the size of internal data elements
    public class ServiceDataSnapshot {

        public int num_connections_active = 0;
        public int num_connections_inactive = 0;
        public float percent_connections_active = 0.0f;
            
        public int num_results = 0;
        public int num_results_past_inactive_time = 0;

        public Dictionary<Faction, int> faction_inventory_size;
        public Dictionary<Faction, int> faction_shop_size;

        DateTime server_startup;
        DateTime server_last_backup;

        public ServiceDataSnapshot() {
            var settings = Helper.LoadSettings();
            DateTime activeOnOrAfter = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(settings.MinutesForActive));

            // user data
            int size_connectionStore = Holder.connectionStore.Count;
            num_connections_active = Holder.connectionStore
                .Where(x => x.Value.LastDataSend >= activeOnOrAfter)
                .Count();
            num_connections_inactive = size_connectionStore - num_connections_active;
            percent_connections_active = size_connectionStore == 0 ? 
                0.0f : (size_connectionStore - num_connections_inactive) / (size_connectionStore);

            // result data
            num_results = Holder.resultHistory.Count;
            num_results_past_inactive_time = Holder.resultHistory
                .Where(x => x.date < activeOnOrAfter)
                .Count();

            // inventory data
            Dictionary<Faction, int> factionInventorySizes = new Dictionary<Faction, int>();
            if (Holder.factionInventories != null && Holder.factionInventories.Keys.Count > 0) {
                foreach (Faction faction in Holder.factionInventories.Keys) {
                    factionInventorySizes[faction] = Holder.factionInventories[faction].Count;
                }
            }
            faction_inventory_size = factionInventorySizes;

            // shop data
            Dictionary<Faction, int> factionShopSizes = new Dictionary<Faction, int>();
            if (Holder.factionShops != null && Holder.factionShops.Count > 0) {
                foreach (FactionShop factionShop in Holder.factionShops) {
                    factionShopSizes[factionShop.shopOwner] = factionShop.currentSoldItems.Count();
                }
            }
            faction_shop_size = factionShopSizes;

            // server data
            server_startup = Holder.startupTime;
            server_last_backup = BackupWorker.lastBackupTime;
        }
    }
}
