using BattleTech;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;

namespace PersistentMapAPI {

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class WarServices : IWarServices {

        public StarMap GetStarmap() {
            return Helper.LoadCurrentMap();
        }

        public System GetSystem(string name) {
            StarMap map = Helper.LoadCurrentMap();
            return map.FindSystemByName(name); ;
        }

        public string ResetStarMap() {
            Logger.LogLine("Init new Map");
            StarMap map = Helper.initializeNewMap();
            Holder.currentMap = map;
            Console.WriteLine("Save new Map");
            Helper.SaveCurrentMap(map);
            Console.WriteLine("Map reset Sucessfull");
            return "Reset Sucessfull";
        }

        public System PostMissionResultDeprecated(string employer, string target, string systemName, string mresult) {
            try {
                return PostMissionResult(new MissionResult((Faction)Enum.Parse(typeof(Faction), employer), (Faction)Enum.Parse(typeof(Faction), target), (BattleTech.MissionResult)Enum.Parse(typeof(BattleTech.MissionResult), mresult), systemName, 5, 0, 0), "UNKNOWN");

            }
            catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public System PostMissionResultDeprecated2(string employer, string target, string systemName, string mresult, string difficulty) {
            try {
                return PostMissionResult(new MissionResult((Faction)Enum.Parse(typeof(Faction), employer), (Faction)Enum.Parse(typeof(Faction), target), (BattleTech.MissionResult)Enum.Parse(typeof(BattleTech.MissionResult), mresult), systemName, int.Parse(difficulty), 0, 0), "UNKNOWN");

            }
            catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public System PostMissionResultDeprecated3(string employer, string target, string systemName, string mresult, string difficulty, string rep) {
            try {
                return PostMissionResult(new MissionResult((Faction)Enum.Parse(typeof(Faction), employer), (Faction)Enum.Parse(typeof(Faction), target), (BattleTech.MissionResult)Enum.Parse(typeof(BattleTech.MissionResult), mresult), systemName, int.Parse(difficulty), int.Parse(rep), 0), "UNKNOWN");

            }
            catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public System PostMissionResultDepricated4(string employer, string target, string systemName, string mresult, string difficulty, string rep, string planetSupport) {
            try {
                return PostMissionResult(new MissionResult((Faction)Enum.Parse(typeof(Faction), employer), (Faction)Enum.Parse(typeof(Faction), target), (BattleTech.MissionResult)Enum.Parse(typeof(BattleTech.MissionResult), mresult), systemName, int.Parse(difficulty), int.Parse(rep), int.Parse(planetSupport)), "UNKNOWN");
            }
            catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public System PostMissionResultDepricated5(MissionResult mresult) {
            return PostMissionResult(mresult, "UNKNOWN");
        }

        public System PostMissionResult(MissionResult mresult, string companyName) {
            try {
                int realDifficulty = Math.Min(10, mresult.difficulty);
                string ip = Helper.GetIP();
                int realPlanets = Math.Min(Helper.LoadSettings().MaxPlanetSupport, mresult.planetSupport);
                int realRep = Math.Min(Helper.LoadSettings().MaxRep, mresult.awardedRep);
                if ((Helper.LoadSettings().HalfSkullPercentageForWin * realDifficulty) + realRep + realPlanets > 50){
                    Logger.LogToFile("Weird Result - Difficulty: " + realDifficulty + " - planet: " + realPlanets + " - rep: " + realRep + " - employer: " + mresult.employer + " - target: " + mresult.target + " - systemName: " + mresult.systemName + " - mresult: " + mresult.result + " - IP: "+ip);
                }
                HistoryResult hresult = new HistoryResult();
                hresult.date = DateTime.UtcNow;
                
                if (Helper.CheckUserInfo(ip, mresult.systemName, companyName)) {
                    Logger.LogToFile("One ip trys to send Missions to fast. IP:" + ip);
                    return null;
                }
                Logger.LogLine("New Result Posted");
                Logger.Debug("employer: " + mresult.employer);
                Logger.Debug("target: " + mresult.target);
                Logger.Debug("systemName: " + mresult.systemName);
                Logger.Debug("mresult: " + mresult.result);
                StarMap map = Helper.LoadCurrentMap();
                System system = map.FindSystemByName(mresult.systemName);
                FactionControl oldOwnerControl = system.FindHighestControl();
                Faction oldOwner = Faction.INVALID_UNSET;
                if (oldOwnerControl != null) {
                    oldOwner = oldOwnerControl.faction;
                }
                FactionControl employerControl = system.FindFactionControlByFaction(mresult.employer);
                FactionControl targetControl = system.FindFactionControlByFaction(mresult.target);

                if (mresult.result == BattleTech.MissionResult.Victory) {
                    Logger.Debug("Victory Result");
                    int realChange = Math.Min(Math.Abs(employerControl.percentage - 100), Math.Max(1, (Helper.LoadSettings().HalfSkullPercentageForWin * realDifficulty) + realRep + realPlanets));
                    hresult.winner = employerControl.faction;
                    hresult.loser = targetControl.faction;
                    hresult.pointsTraded = realChange;
                    employerControl.percentage += realChange;
                    targetControl.percentage -= realChange;
                    Logger.Debug(realChange + " Points traded");
                    if (targetControl.percentage < 0) {
                        int leftoverChange = Math.Abs(targetControl.percentage);
                        Logger.Debug(leftoverChange + " Leftover Points");
                        targetControl.percentage = 0;
                        int debugcounter = leftoverChange;
                        while (leftoverChange > 0 && debugcounter != 0) {
                            foreach (FactionControl leftOverFaction in system.controlList) {
                                if (leftOverFaction.faction != mresult.employer &&
                                    leftOverFaction.faction != mresult.target && leftOverFaction.percentage > 0
                                    && leftoverChange > 0) {
                                    leftOverFaction.percentage--;
                                    leftoverChange--;
                                    Logger.Debug(leftOverFaction.faction.ToString() + " Points deducted");
                                }
                            }
                            debugcounter--;
                        }
                    }
                }
                else {
                    Logger.Debug("Loss Result");
                    int realChange = Math.Min(employerControl.percentage, Math.Max(1, (Helper.LoadSettings().HalfSkullPercentageForLoss * realDifficulty) + realRep / 2 + realPlanets / 2));
                    hresult.winner = targetControl.faction;
                    hresult.loser = employerControl.faction;
                    hresult.pointsTraded = realChange;
                    employerControl.percentage -= realChange;
                    targetControl.percentage += realChange;
                    Logger.Debug(realChange + " Points traded");
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
                Logger.LogError(e);
                return null;
            }
        }

        public List<HistoryResult> GetMissionResults(string MinutesBack, string MaxResults) {
            List<HistoryResult> resultList = Holder.resultHistory.Where(i => i.date.Value.AddMinutes(int.Parse(MinutesBack)) > DateTime.UtcNow).OrderByDescending(x => x.date).Take(int.Parse(MaxResults)).ToList();
            return resultList;
        }

        public int GetActivePlayers(string MinutesBack) {
            return Holder.connectionStore.Where(x => x.Value.LastDataSend.AddMinutes(int.Parse(MinutesBack)) > DateTime.UtcNow).Count();
        }

        public string GetStartupTime() {
            return Holder.startupTime.ToString("o", CultureInfo.InvariantCulture);
        }

        public List<ShopDefItem> GetShopForFaction(string Faction) {
            try {
                Faction realFaction = (Faction)Enum.Parse(typeof(Faction), Faction);
                if (Holder.factionShops == null) {
                    Holder.factionShops = new List<FactionShop>();
                    Logger.LogLine("Shops initilaized");
                }
                if (Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction) == null) {
                    Holder.factionShops.Add(new FactionShop(realFaction, new List<ShopDefItem>(), DateTime.MinValue));
                    Logger.LogLine(Faction + ": Shop not found");
                }
                if (Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction).lastUpdate.AddMinutes(Helper.LoadSettings().MinutesTillShopUpdate) < DateTime.UtcNow) {
                    Logger.LogLine(Faction + ": Shop refresh");
                    List<ShopDefItem> newShop = Helper.GenerateNewShop(realFaction);
                    Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction).currentSoldItems.Clear();
                    Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction).currentSoldItems = newShop;
                    Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction).lastUpdate = DateTime.UtcNow;
                }
                return Holder.factionShops.FirstOrDefault(x => x.shopOwner == realFaction).currentSoldItems;
            }
            catch (Exception e) {
                Console.WriteLine(e);
                return null;
            }
        }

        public string PostSalvageForFaction(List<ShopDefItem> salvage, string Faction) {
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
                }
                else {
                    int index = Holder.factionInventories[realFaction].FindIndex(x => x.ID.Equals(item.ID));
                    Holder.factionInventories[realFaction][index].Count++;
                    Holder.factionInventories[realFaction][index].DiscountModifier = Math.Max(Holder.factionInventories[realFaction][index].DiscountModifier - Helper.LoadSettings().DiscountPerItem, Helper.LoadSettings().DiscountFloor);                  
                }
            }
            Logger.LogLine(salvage.Count + " items inserted into inventory for " + Faction);
            return salvage.Count + " items inserted into inventory for " + Faction;
        }

        public string PostPurchaseForFactionDepricated(string Faction, string ID) {
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

        public string PostPurchaseForFaction(List<string> ids, string Faction) {
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
                Logger.LogLine(ids.Count + " items removed from shop for " + Faction);
                return ids.Count + " items removed from shop for " + Faction;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return "Error";
            }
        }
    }
}
