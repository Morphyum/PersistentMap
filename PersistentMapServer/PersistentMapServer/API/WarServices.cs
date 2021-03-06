﻿using BattleTech;
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

        [UserQuota(enforcement: UserQuotaAttribute.EnforcementEnum.Block)]
        public override System PostMissionResult(MissionResult mresult, string companyName) {
            lock (_missionResultLock) {
                try {
                    // TODO: Update connection data in a cleaner fashion
                    string ip = Helper.MapRequestIP();
                    string hashedIP = String.Format("{0:X}", ip.GetHashCode());
                    DateTime reportTime = DateTime.UtcNow;
                    // TODO: For now, use the IP as the playerId. In the future, GUID
                    Helper.RecordPlayerActivity(mresult, ip, companyName, reportTime);
                    int realDifficulty = 0;
                    // Check to see if the post is suspicious
                    if (Helper.LoadSettings().HardCodedDifficulty != 0) {
                        realDifficulty = Helper.LoadSettings().HardCodedDifficulty;
                    }
                    else {
                        realDifficulty = Math.Min(10, mresult.difficulty);
                    }
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

                    FactionControl oldOwnerControl = system.FindHighestControl();
                    Faction oldOwner = Faction.INVALID_UNSET;
                    if (oldOwnerControl != null) {
                        oldOwner = oldOwnerControl.faction;
                    }
                    FactionControl employerControl = system.FindFactionControlByFaction(mresult.employer);
                    FactionControl targetControl = system.FindFactionControlByFaction(mresult.target);
                    logger.Debug($"Real rep - ({realRep}) and real planets ({realPlanets})");

                    if (mresult.result == BattleTech.MissionResult.Victory) {

                        int gain = (Helper.LoadSettings().HalfSkullPercentageForWin * realDifficulty) + realRep + realPlanets;
                        int realChange = 0;
                        if (employerControl.percentage >= Helper.LoadSettings().LowerFortBorder &&
                            employerControl.percentage <= Helper.LoadSettings().UpperFortBorder) {
                            logger.Debug("Fort Rules");
                            realChange = Math.Min(
                            Math.Abs(employerControl.percentage - Helper.LoadSettings().UpperFortBorder),
                            Math.Max(1, (int)Math.Round(gain * Helper.LoadSettings().FortPercentage))
                            );
                        }
                        else {
                            realChange = Math.Min(
                            Math.Abs(employerControl.percentage - Helper.LoadSettings().LowerFortBorder),
                            Math.Max(1, gain)
                            );
                        }
                        hresult.winner = employerControl.faction;
                        hresult.loser = targetControl.faction;
                        hresult.pointsTraded = realChange;
                        logger.Debug($"Victory for ({hresult.winner}) over ({hresult.loser})");
                        int fortLoss = 0;
                        while (targetControl.percentage > Helper.LoadSettings().LowerFortBorder && realChange > 0) {
                            targetControl.percentage--;
                            realChange--;
                            fortLoss++;
                        }
                        if (fortLoss > 0) {
                            logger.Debug($"Fortification of ({hresult.loser}) lost {fortLoss} points.");
                        }
                        if (realChange > 0) {
                            targetControl.percentage -= realChange;
                            if (targetControl.percentage < 0) {
                                int leftover = Math.Abs(targetControl.percentage);
                                logger.Debug($"{leftover} points could not be removed from ({hresult.loser}) because its below 0.");
                                targetControl.percentage = 0;

                                int totalEnemyControl = 0;
                                foreach (FactionControl control in system.controlList) {
                                    if (control.faction != employerControl.faction) {
                                        totalEnemyControl += control.percentage;
                                    }
                                }
                                if (totalEnemyControl != 0) {
                                    realChange -= leftover;
                                }
                                else {
                                    logger.Debug($"No other Factions on Planet.");
                                }
                            }
                            employerControl.percentage += realChange;
                            logger.Debug($"{realChange} points were traded.");
                        }
                    }
                    FactionControl afterBattleOwnerControl = system.FindHighestControl();
                    Faction newOwner = afterBattleOwnerControl.faction;
                    if (oldOwner != newOwner) {
                        hresult.planetSwitched = true;
                    }
                    else {
                        hresult.planetSwitched = false;
                    }
                    hresult.system = mresult.systemName;
                    Holder.resultHistory.Add(hresult);
                    return system;
                }
                catch (Exception e) {
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

        public override Dictionary<string, int> GetActiveFactions(string MinutesBack) {
            var connections = Holder.connectionStore
                 .Where(x => x.Value.LastDataSend.AddMinutes(int.Parse(MinutesBack)) > DateTime.UtcNow);
            var FactionList = new Dictionary<string, int>();
            foreach (KeyValuePair<string, UserInfo> pair in connections) {
                var lastFaction = pair.Value.lastFactionFoughtForInWar.ToString();
                if (!FactionList.ContainsKey(lastFaction)) {
                    FactionList.Add(lastFaction, 0);
                }
                FactionList[lastFaction]++;
            }
            return FactionList;
        }

        public override List<string> GetActiveCompaniesPerFaction(string Faction, string MinutesBack) {
            var connections = Holder.connectionStore
                 .Where(x => x.Value.LastDataSend.AddMinutes(int.Parse(MinutesBack)) > DateTime.UtcNow);
            var CompanyList = new List<string>();
            foreach (KeyValuePair<string, UserInfo> pair in connections) {
                if (pair.Value.lastFactionFoughtForInWar.ToString().ToLower().Equals(Faction.ToLower())) {
                    CompanyList.Add(pair.Value.companyName);
                        }
            }
            return CompanyList;
        }

        // Returns all the companies that are known. Values are the playerIds that have used that company name.
        public override Dictionary<string, List<string>> GetPlayerCompanies() {

            var playerCompanies = new Dictionary<string, HashSet<string>>();
            foreach (PlayerHistory history in Holder.playerHistory) {
                foreach (string companyName in history.CompanyNames()) {
                    if (playerCompanies.ContainsKey(companyName)) {
                        playerCompanies[companyName].Add(history.Id);
                    }
                    else {
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
            lock (_salvageLock) {
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
                    }
                    else {
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
            lock (_purchaseLock) {
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
                }
                catch (Exception e) {
                    logger.Warn(e, "Failed to process purchase!");
                    return "Error";
                }
            }
        }

    }
}
