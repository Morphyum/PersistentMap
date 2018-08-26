using BattleTech;
using Newtonsoft.Json;
using System;
using System.ServiceModel;
using System.ServiceModel.Activation;

namespace PersistentMapAPI {

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class WarServices : IWarServices {

        public StarMap GetStarmap() {
            Logger.LogLine("Map retreived");
            return Helper.LoadCurrentMap();
        }

        public System GetSystem(string name) {
            StarMap map = Helper.LoadCurrentMap();
            Logger.LogLine("System retreived");
            return map.FindSystemByName(name); ;
        }

        public string ResetStarMap() {
            Logger.LogLine("Init new Map");
            StarMap map = Helper.initializeNewMap();
            Holder.currentMap = map;
            Logger.LogLine("Save new Map");
            Helper.SaveCurrentMap(map);
            Logger.LogLine("Map reset Sucessfull");
            return "Reset Sucessfull";
        }

        public System PostMissionResult(string employer, string target, string systemName, string mresult) {
            try {
                Logger.LogLine("New Result Posted");
                Logger.LogLine("employer: " + employer);
                Logger.LogLine("target: " + target);
                Logger.LogLine("systemName: " + systemName);
                Logger.LogLine("mresult: " + mresult);
                StarMap map = Helper.LoadCurrentMap();
                System system = map.FindSystemByName(systemName);
                FactionControl employerControl = system.FindFactionControlByFaction((Faction)Enum.Parse(typeof(Faction), employer));
                FactionControl targetControl = system.FindFactionControlByFaction((Faction)Enum.Parse(typeof(Faction), target));

                if (mresult == "Victory") {
                    Logger.LogLine("Victory Result");
                    int realChange = Math.Min(Math.Abs(employerControl.percentage - 100), Helper.LoadSettings().percentageForWin);
                    employerControl.percentage += realChange;
                    targetControl.percentage -= realChange;
                    Logger.LogLine(realChange + " Points traded");
                    if (targetControl.percentage < 0) {
                        int leftoverChange = Math.Abs(targetControl.percentage);
                        Logger.LogLine(leftoverChange + " Leftover Points");
                        targetControl.percentage = 0;
                        int debugcounter = leftoverChange;
                        while (leftoverChange > 0 && debugcounter != 0) {
                            foreach (FactionControl leftOverFaction in system.controlList) {
                                if (leftOverFaction.faction != (Faction)Enum.Parse(typeof(Faction), employer) &&
                                    leftOverFaction.faction != (Faction)Enum.Parse(typeof(Faction), target) && leftOverFaction.percentage > 0
                                    && leftoverChange > 0) {
                                    leftOverFaction.percentage--;
                                    leftoverChange--;
                                    Logger.LogLine("Points deducted");
                                }
                            }
                            debugcounter--;
                        }
                    }
                }
                else {
                    Logger.LogLine("Loss Result");
                    int realChange = Math.Min(employerControl.percentage, Helper.LoadSettings().percentageForLoss);
                    employerControl.percentage -= realChange;
                    targetControl.percentage += realChange;
                    Logger.LogLine(realChange + " Points traded");
                }
                Helper.SaveCurrentMap(map);
                return system;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }
    }
}
