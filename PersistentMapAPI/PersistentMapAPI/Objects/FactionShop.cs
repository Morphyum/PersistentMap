using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentMapAPI {
    public class FactionShop {
        public Faction shopOwner;
        public List<ShopDefItem> currentSoldItems;
        public DateTime lastUpdate;

        public FactionShop(Faction shopOwner, List<ShopDefItem> currentSoldItems, DateTime lastUpdate){
            this.currentSoldItems = currentSoldItems;
            this.lastUpdate = lastUpdate;
            this.shopOwner = shopOwner;
        }
    }
}
