using BattleTech;
using PersistentMapServer.Attribute;
using PersistentMapServer.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;

namespace PersistentMapAPI {

    // Implementation of the current service methods used    
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class WarServices : API.AdminWarServices {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Locks to prevent concurrent modification
        private readonly object _missionResultLock = new Object();
        private readonly object _salvageLock = new Object();
        private readonly object _purchaseLock = new Object();

        // Thread-safe; returns copy of starmap. We clone to prevent modification during serialization (due to heavy nesting).
        public override StarMap GetStarmap() {
            StarMap builtMap = StarMapStateManager.Build();
            return builtMap;
        }

        // Thread-safe; returns copy of starmap. We clone to prevent modification during serialization (due to heavy nesting).
        public override System GetSystem(string name) {
            StarMap builtMap = StarMapStateManager.Build();            
            return builtMap.FindSystemByName(name);
        }

        [UserQuota(enforcement : UserQuotaAttribute.EnforcementEnum.Block)]
        public override System PostMissionResult(MissionResult mresult, string companyName) {
            lock (_missionResultLock) {
                try {
                    // TODO: Update connection data in a cleaner fashion
                    string ip = Helper.MapRequestIP();
                    string hashedIP = String.Format("{0:X}", ip.GetHashCode());
                    DateTime reportTime = DateTime.UtcNow;
                    // TODO: For now, use the IP as the playerId. In the future, GUID
                    Helper.RecordPlayerActivity(mresult, ip, companyName, reportTime);

                    // Check to see if the post is suspicious
                    int realDifficulty = Math.Min(10, mresult.difficulty);
                    int realPlanets = Math.Min(Helper.LoadSettings().MaxPlanetSupport, mresult.planetSupport);
                    int realRep = Math.Min(Helper.LoadSettings().MaxRep, mresult.awardedRep);
                    if ((Helper.LoadSettings().HalfSkullPercentageForWin * realDifficulty) + realRep + realPlanets > 50) {
                        logger.Info("Suspicious result reported. See console.log for details.");
                        logger.Debug($"Suspicous result for IP:({ip})" +
                            $" normalized difficulty:({realDifficulty}) planetSupport:({realPlanets}) reptuation:({realRep})" +
                            $" for employer:({mresult.employer}) vs target:({mresult.target}) on system: ({mresult.systemName})" +
                            $" with result:({mresult.result})"
                            );
                    }
                    HistoryResult hresult = new HistoryResult {
                        date = reportTime
                    };

                    logger.Info($"New Mission Result for ({companyName}) on ({mresult.systemName})");
                    logger.Debug($"New MissionResult - ({companyName}) fought for ({mresult.employer}) against ({mresult.target})" +
                        $" on ({mresult.systemName}) and achieved ({mresult.result})");
                  
                    StarMap builtMap = StarMapStateManager.Build();
                    System system = builtMap.FindSystemByName(mresult.systemName);

                    Faction oldOwner = system.invasionsState.defender;

                    if (mresult.result == BattleTech.MissionResult.Victory) {
                        int realChange = Math.Max(1, (Helper.LoadSettings().HalfSkullPercentageForWin * realDifficulty) + realRep + realPlanets);
                        hresult.winner = mresult.employer;
                        hresult.loser = mresult.target;
                        hresult.pointsTraded = realChange;
                        system.invasionsState.changePercentage(realChange);
                        logger.Debug($"Victory for ({hresult.winner}) over ({hresult.loser}) - {realChange} points were traded.");                        
                    } else {
                        int realChange = Math.Max(1, (Helper.LoadSettings().HalfSkullPercentageForLoss * realDifficulty) + realRep / 2 + realPlanets / 2);
                        hresult.winner = mresult.target;
                        hresult.loser = mresult.employer;
                        hresult.pointsTraded = realChange;
                        system.invasionsState.changePercentage(0 - realChange);
                        logger.Debug($"Loss for ({hresult.loser}) against ({hresult.winner}) - {realChange} points were traded.");
                    }
                    Faction newOwner = system.invasionsState.defender;
                    if (oldOwner != newOwner) {
                        hresult.planetSwitched = true;
                    } else {
                        hresult.planetSwitched = false;
                    }
                    hresult.system = mresult.systemName;
                    Holder.resultHistory.Add(hresult);
                    return system;
                } catch (Exception e) {
                    logger.Warn(e, "Failed to process mission result!");
                    return null;
                }
            }
        }

        // TODO: Test with large # of results
        public override List<HistoryResult> GetMissionResults(string MinutesBack, string MaxResults) {
            List<HistoryResult> resultList = Holder.resultHistory
                .Where(i => i.date.Value.AddMinutes(int.Parse(MinutesBack)) > DateTime.UtcNow)
                .OrderByDescending(x => x.date)
                .Take(int.Parse(MaxResults))
                .ToList();
            return resultList;
        }

        // TODO: Test with large # of 
        public override int GetActivePlayers(string MinutesBack) {
            return Holder.connectionStore
                .Where(x => x.Value.LastDataSend.AddMinutes(int.Parse(MinutesBack)) > DateTime.UtcNow)
                .Count();
        }

        // Returns all the companies that are known. Values are the playerIds that have used that company name.
        public override Dictionary<string, List<string>> GetPlayerCompanies() {

            var playerCompanies = new Dictionary<string, HashSet<string>>();
            foreach (PlayerHistory history in Holder.playerHistory) {
                foreach (string companyName in history.CompanyNames()) {
                    if (playerCompanies.ContainsKey(companyName)) {
                        playerCompanies[companyName].Add(history.Id);
                    } else {
                        playerCompanies[companyName] = new HashSet<string> { history.Id };
                    }
                }
            }

            return playerCompanies.ToDictionary(item => item.Key, item => item.Value.ToList());
        }

        public override List<CompanyActivity> GetPlayerActivity(string PlayerId) {
            var playerHistory = Holder.playerHistory.SingleOrDefault(x => x.Id.Equals(PlayerId));
            return playerHistory != null ? 
                playerHistory.activities.ToList() 
                : new List<CompanyActivity>();
        }

        // Thread-safe; returns simple value.
        public override string GetStartupTime() {
            return Holder.startupTime.ToString("o", CultureInfo.InvariantCulture);
        }

        public override List<ShopDefItem> GetShopForFaction(string Faction) {
            try {
                Faction realFaction = (Faction)Enum.Parse(typeof(Faction), Faction);
                if (Holder.factionShops == null) {
                    Holder.factionShops = new List<FactionShop>();
                    logger.Debug("Faction shops were null - initialized to empty shops");
                }
                if (Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction) == null) {
                    Holder.factionShops.Add(new FactionShop(realFaction, new List<ShopDefItem>(), DateTime.MinValue));
                    logger.Info($"Shop for faction ({Faction}) was not found!");
                }
                if (Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction).lastUpdate.AddMinutes(Helper.LoadSettings().MinutesTillShopUpdate) < DateTime.UtcNow) {
                    logger.Info($"Shop for faction ({Faction}) was refreshed.");                    
                    List<ShopDefItem> newShop = Helper.GenerateNewShop(realFaction);
                    Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction).currentSoldItems.Clear();
                    Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction).currentSoldItems = newShop;
                    Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction).lastUpdate = DateTime.UtcNow;
                }
                return Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction).currentSoldItems;
            }
            catch (Exception e) {
                logger.Warn(e, "Failed to get shop for faction!");
                return null;
            }
        }

        public override string PostSalvageForFaction(List<ShopDefItem> salvage, string Faction) {
            lock(_salvageLock) {
                Faction realFaction = (Faction)Enum.Parse(typeof(Faction), Faction);
                if (Holder.factionInventories == null) {
                    Holder.factionInventories = FactionInventoryStateManager.Build();
                }
                if (!Holder.factionInventories.ContainsKey(realFaction)) {
                    Holder.factionInventories.Add(realFaction, new List<ShopDefItem>());
                }
                foreach (ShopDefItem item in salvage) {
                    if (Holder.factionInventories[realFaction].FirstOrDefault(x => x.ID.Equals(item.ID)) == null) {
                        Holder.factionInventories[realFaction].Add(item);
                    } else {
                        int index = Holder.factionInventories[realFaction].FindIndex(x => x.ID.Equals(item.ID));
                        Holder.factionInventories[realFaction][index].Count++;
                        Holder.factionInventories[realFaction][index].DiscountModifier = Math.Max(Holder.factionInventories[realFaction][index].DiscountModifier - Helper.LoadSettings().DiscountPerItem, Helper.LoadSettings().DiscountFloor);
                    }
                }
                logger.Info($"INV: Added {salvage.Count} items into inventory for faction ({Faction})");
                return salvage.Count + " items inserted into inventory for " + Faction;
            }
        }

        public override string PostPurchaseForFaction(List<string> ids, string Faction) {
            lock(_purchaseLock) {
                try {
                    Faction realFaction = (Faction)Enum.Parse(typeof(Faction), Faction);
                    if (Holder.factionShops != null) {
                        FactionShop shop = Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction);
                        if (shop != null) {
                            foreach (string ID in ids) {
                                ShopDefItem item = shop.currentSoldItems.FirstOrDefault(x => x.ID.Equals(ID));
                                if (item != null) {
                                    item.Count--;
                                }
                            }
                            shop.currentSoldItems.RemoveAll(x => x.Count <= 0);
                        }
                    }
                    logger.Info($"INV: {ids.Count} items were removed from the shop for faction ({Faction})");
                    return ids.Count + " items removed from shop for " + Faction;
                } catch (Exception e) {
                    logger.Warn(e, "Failed to process purchase!");
                    return "Error";
                }
            }
        }

    }
}
