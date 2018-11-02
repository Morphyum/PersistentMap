using BattleTech;
using PersistentMapAPI.Objects;
using PersistentMapServer;
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
            StarMap builtMap = StarMapBuilder.Build();
            return builtMap;
        }

        // Thread-safe; returns copy of starmap. We clone to prevent modification during serialization (due to heavy nesting).
        public override System GetSystem(string name) {
            StarMap builtMap = StarMapBuilder.Build();            
            return builtMap.FindSystemByName(name);
        }

        [UserQuota(enforcement : UserQuotaAttribute.EnforcementEnum.Block)]
        public override System PostMissionResult(MissionResult mresult, string companyName) {
            lock (_missionResultLock) {
                try {
                    int realDifficulty = Math.Min(10, mresult.difficulty);
                    string ip = Helper.mapRequestIP();
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
                    // TODO: Add unique id (guid?) for each result, to allow easy correlation in the logs?
                    HistoryResult hresult = new HistoryResult();
                    hresult.date = DateTime.UtcNow;

                    // TODO: Update connection data in a cleaner fashion
                    Holder.connectionStore[ip].companyName = companyName;
                    Holder.connectionStore[ip].lastSystemFoughtAt = mresult.systemName;

                    logger.Info($"New Mission Result for ({companyName}) on ({mresult.systemName})");
                    logger.Debug($"New MissionResult - ({companyName}) fought for ({mresult.employer}) against ({mresult.target})" +
                        $" on ({mresult.systemName}) and achieved ({mresult.result})");
                  
                    StarMap builtMap = StarMapBuilder.Build();
                    System system = builtMap.FindSystemByName(mresult.systemName);
                    
                    FactionControl oldOwnerControl = system.FindHighestControl();
                    Faction oldOwner = Faction.INVALID_UNSET;
                    if (oldOwnerControl != null) {
                        oldOwner = oldOwnerControl.faction;
                    }
                    FactionControl employerControl = system.FindFactionControlByFaction(mresult.employer);
                    FactionControl targetControl = system.FindFactionControlByFaction(mresult.target);

                    if (mresult.result == BattleTech.MissionResult.Victory) {
                        int realChange = Math.Min(
                            Math.Abs(employerControl.percentage - 100), 
                            Math.Max(1, (Helper.LoadSettings().HalfSkullPercentageForWin * realDifficulty) + realRep + realPlanets)
                            );
                        hresult.winner = employerControl.faction;
                        hresult.loser = targetControl.faction;
                        hresult.pointsTraded = realChange;
                        employerControl.percentage += realChange;
                        targetControl.percentage -= realChange;
                        logger.Debug($"Victory for ({hresult.winner}) over ({hresult.loser}) - {realChange} points were traded.");                        
                        if (targetControl.percentage < 0) {
                            int leftoverChange = Math.Abs(targetControl.percentage);
                            logger.Debug($"{leftoverChange} points could not be removed from ({hresult.loser}) - distributing them to other factions.");                            
                            targetControl.percentage = 0;
                            int debugcounter = leftoverChange;
                            while (leftoverChange > 0 && debugcounter != 0) {
                                foreach (FactionControl leftOverFaction in system.controlList) {
                                    if (leftOverFaction.faction != mresult.employer && leftOverFaction.faction != mresult.target && 
                                        leftOverFaction.percentage > 0 && leftoverChange > 0) {
                                        leftOverFaction.percentage--;
                                        leftoverChange--;
                                        logger.Debug($"Removed {leftoverChange} points {leftOverFaction.faction.ToString()}.");
                                    }
                                }
                                debugcounter--;
                            }
                        }
                    } else {
                        int realChange = Math.Min(employerControl.percentage, Math.Max(1, (Helper.LoadSettings().HalfSkullPercentageForLoss * realDifficulty) + realRep / 2 + realPlanets / 2));
                        hresult.winner = targetControl.faction;
                        hresult.loser = employerControl.faction;
                        hresult.pointsTraded = realChange;
                        employerControl.percentage -= realChange;
                        targetControl.percentage += realChange;
                        logger.Debug($"Loss for ({hresult.loser}) against ({hresult.winner}) - {realChange} points were traded.");
                    }
                    FactionControl afterBattleOwnerControl = system.FindHighestControl();
                    Faction newOwner = afterBattleOwnerControl.faction;
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

        // Thread-safe; returns copy of list
        // TODO: Test with large # of results
        public override List<HistoryResult> GetMissionResults(string MinutesBack, string MaxResults) {
            List<HistoryResult> resultList = Holder.resultHistory
                .Where(i => i.date.Value.AddMinutes(int.Parse(MinutesBack)) > DateTime.UtcNow)
                .OrderByDescending(x => x.date)
                .Take(int.Parse(MaxResults))
                .ToList();
            return resultList;
        }

        // Thread-safe; returns count only
        // TODO: Test with large # of 
        public override int GetActivePlayers(string MinutesBack) {
            return Holder.connectionStore
                .Where(x => x.Value.LastDataSend.AddMinutes(int.Parse(MinutesBack)) > DateTime.UtcNow)
                .Count();
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
                    Helper.LoadCurrentInventories();
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
