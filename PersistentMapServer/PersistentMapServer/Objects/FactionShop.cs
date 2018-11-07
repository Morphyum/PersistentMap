using BattleTech;
using System;
using System.Collections.Generic;

namespace PersistentMapAPI.Objects {
    // Contains all of the items currently available for sale unique to a specific faction.
    public class FactionShop {

        public Faction shopOwner;
        // Items currently for sale
        public List<ShopDefItem> currentSoldItems;
        public DateTime lastUpdate;

        public FactionShop(Faction shopOwner, List<ShopDefItem> currentSoldItems, DateTime lastUpdate){
            this.currentSoldItems = currentSoldItems;
            this.lastUpdate = lastUpdate;
            this.shopOwner = shopOwner;
        }
    }
}
