using BattleTech;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;

namespace PersistentMapAPI {

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class WarServices : IWarServices {

        public StarMap GetStarmap() {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            return Helper.LoadCurrentMap();
        }

        public System GetSystem(string name) {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
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
                return PostMissionResult(employer, target, systemName, mresult, "5");
            }
            catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public System PostMissionResult(string employer, string target, string systemName, string mresult, string difficulty) {
            try {
                HistoryResult hresult = new HistoryResult();
                hresult.date = DateTime.UtcNow;
                int realDifficulty = Math.Min(10,int.Parse(difficulty));
                string ip = Helper.GetIP();
                if (Helper.CheckUserInfo(ip, systemName)) {
                    Logger.LogLine("One ip trys to send Missions to fast");
                    return null;
                }
                Logger.LogLine("New Result Posted");
                Console.WriteLine("employer: " + employer);
                Console.WriteLine("target: " + target);
                Console.WriteLine("systemName: " + systemName);
                Console.WriteLine("mresult: " + mresult);
                StarMap map = Helper.LoadCurrentMap();
                System system = map.FindSystemByName(systemName);
                FactionControl oldOwnerControl = system.FindHighestControl();
                Faction oldOwner = Faction.INVALID_UNSET;
                if(oldOwnerControl != null) {
                    oldOwner = oldOwnerControl.faction;
                }
                FactionControl employerControl = system.FindFactionControlByFaction((Faction)Enum.Parse(typeof(Faction), employer));
                FactionControl targetControl = system.FindFactionControlByFaction((Faction)Enum.Parse(typeof(Faction), target));

                if (mresult == "Victory") {
                    Console.WriteLine("Victory Result");
                    int realChange = Math.Min(Math.Abs(employerControl.percentage - 100), Helper.LoadSettings().HalfSkullPercentageForWin * realDifficulty);
                    hresult.winner = employerControl.faction;
                    hresult.loser = targetControl.faction;
                    hresult.pointsTraded = realChange;
                    employerControl.percentage += realChange;
                    targetControl.percentage -= realChange;
                    Console.WriteLine(realChange + " Points traded");
                    if (targetControl.percentage < 0) {
                        int leftoverChange = Math.Abs(targetControl.percentage);
                        Console.WriteLine(leftoverChange + " Leftover Points");
                        targetControl.percentage = 0;
                        int debugcounter = leftoverChange;
                        while (leftoverChange > 0 && debugcounter != 0) {
                            foreach (FactionControl leftOverFaction in system.controlList) {
                                if (leftOverFaction.faction != (Faction)Enum.Parse(typeof(Faction), employer) &&
                                    leftOverFaction.faction != (Faction)Enum.Parse(typeof(Faction), target) && leftOverFaction.percentage > 0
                                    && leftoverChange > 0) {
                                    leftOverFaction.percentage--;
                                    leftoverChange--;
                                    Console.WriteLine(leftOverFaction.faction.ToString() + " Points deducted");
                                }
                            }
                            debugcounter--;
                        }
                    }
                }
                else {
                    Console.WriteLine("Loss Result");
                    int realChange = Math.Min(employerControl.percentage, Helper.LoadSettings().HalfSkullPercentageForLoss * realDifficulty);
                    hresult.winner = targetControl.faction;
                    hresult.loser = employerControl.faction;
                    hresult.pointsTraded = realChange;
                    employerControl.percentage -= realChange;
                    targetControl.percentage += realChange;
                    Console.WriteLine(realChange + " Points traded");
                }
                Helper.SaveCurrentMap(map);
                FactionControl afterBattleOwnerControl = system.FindHighestControl();
                Faction newOwner = afterBattleOwnerControl.faction;
                if(oldOwner != newOwner) {
                    hresult.planetSwitched = true;
                } else {
                    hresult.planetSwitched = false;
                }
                hresult.system = systemName;
                Holder.resultHistory.Add(hresult);
                return system;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public List<HistoryResult> GetMissionResults(string MinutesBack, string MaxResults) {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            List<HistoryResult> resultList = Holder.resultHistory.Where(i => i.date.Value.AddMinutes(int.Parse(MinutesBack)) > DateTime.UtcNow).OrderByDescending(x => x.date).Take(int.Parse(MaxResults)).ToList();
            return resultList;
        }

        public int GetActivePlayers(string MinutesBack) {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            return Holder.connectionStore.Where(x => x.Value.LastDataSend.AddMinutes(int.Parse(MinutesBack)) > DateTime.UtcNow).Count();
        }
    }
}
