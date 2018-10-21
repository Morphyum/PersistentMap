using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;

namespace PersistentMapAPI.API {

    // Container to hold deprecated methods and keep them out of the main service class.
    public abstract class DeprecatedWarServices : IWarServices {

        public abstract int GetActivePlayers(string MinutesBack);
        public abstract List<HistoryResult> GetMissionResults(string MinutesBack, string MaxResults);
        public abstract List<ShopDefItem> GetShopForFaction(string Faction);
        public abstract StarMap GetStarmap();
        public abstract string GetStartupTime();
        public abstract System GetSystem(string name);
        public abstract System PostMissionResult(MissionResult mresult, string CompanyName);
        public abstract string PostPurchaseForFaction(List<string> ids, string Faction);
        public abstract string PostSalvageForFaction(List<ShopDefItem> salvage, string Faction);

        public System PostMissionResultDeprecated(string employer, string target, string systemName, string mresult) {
            Logger.Debug("WARNING: Deprecated method invoked: PostMissionResultDeprecated");
            try {
                return PostMissionResult(new MissionResult((Faction)Enum.Parse(typeof(Faction), employer), (Faction)Enum.Parse(typeof(Faction), target), (BattleTech.MissionResult)Enum.Parse(typeof(BattleTech.MissionResult), mresult), systemName, 5, 0, 0), "UNKNOWN");

            } catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public System PostMissionResultDeprecated2(string employer, string target, string systemName, string mresult, string difficulty) {
            Logger.Debug("WARNING: Deprecated method invoked: PostMissionResultDeprecated2");
            try {
                return PostMissionResult(new MissionResult((Faction)Enum.Parse(typeof(Faction), employer), (Faction)Enum.Parse(typeof(Faction), target), (BattleTech.MissionResult)Enum.Parse(typeof(BattleTech.MissionResult), mresult), systemName, int.Parse(difficulty), 0, 0), "UNKNOWN");

            } catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public System PostMissionResultDeprecated3(string employer, string target, string systemName, string mresult, string difficulty, string rep) {
            Logger.Debug("WARNING: Deprecated method invoked: PostMissionResultDeprecated3");
            try {
                return PostMissionResult(new MissionResult((Faction)Enum.Parse(typeof(Faction), employer), (Faction)Enum.Parse(typeof(Faction), target), (BattleTech.MissionResult)Enum.Parse(typeof(BattleTech.MissionResult), mresult), systemName, int.Parse(difficulty), int.Parse(rep), 0), "UNKNOWN");

            } catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public System PostMissionResultDepricated4(string employer, string target, string systemName, string mresult, string difficulty, string rep, string planetSupport) {
            Logger.Debug("WARNING: Deprecated method invoked: PostMissionResultDepricated4");
            try {
                return PostMissionResult(new MissionResult((Faction)Enum.Parse(typeof(Faction), employer), (Faction)Enum.Parse(typeof(Faction), target), (BattleTech.MissionResult)Enum.Parse(typeof(BattleTech.MissionResult), mresult), systemName, int.Parse(difficulty), int.Parse(rep), int.Parse(planetSupport)), "UNKNOWN");
            } catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public System PostMissionResultDepricated5(MissionResult mresult) {
            Logger.Debug("WARNING: Deprecated method invoked: PostMissionResultDepricated5");
            return PostMissionResult(mresult, "UNKNOWN");
        }

        public string PostPurchaseForFactionDepricated(string Faction, string ID) {
            Logger.Debug("WARNING: Deprecated method invoked: PostPurchaseForFactionDepricated");
            Faction realFaction = (Faction)Enum.Parse(typeof(Faction), Faction);
            if (Holder.factionShops != null) {
                FactionShop shop = Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction);
                if (shop != null) {
                    ShopDefItem item = shop.currentSoldItems.FirstOrDefault(x => x.ID.Equals(ID));
                    if (item != null) {
                        item.Count--;
                    }
                    shop.currentSoldItems.RemoveAll(x => x.Count <= 0);
                }
            }
            Logger.Debug(ID + " 1 removed from shop for " + Faction);
            return ID + " 1 removed from shop for " + Faction;
        }

    }
}
