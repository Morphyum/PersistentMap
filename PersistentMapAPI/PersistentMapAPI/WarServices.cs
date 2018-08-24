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
            Console.WriteLine("Map retreived");
            return Helper.LoadCurrentMap();
        }

        public System GetSystem(string name) {
            StarMap map = Helper.LoadCurrentMap();
            Console.WriteLine("System retreived");
            return map.FindSystemByName(name); ;
        }

        public string ResetStarMap() {
            Console.WriteLine("Init new Map");
            StarMap map = Helper.initializeNewMap();
            Holder.currentMap = map;
            Console.WriteLine("Save new Map");
            Helper.SaveCurrentMap(map);
            Console.WriteLine("Map reset Sucessfull");
            return "Reset Sucessfull";
        }

        public System PostMissionResult(string employer, string target, string systemName, string mresult) {
            try {
                Console.WriteLine("New Result Posted");
                Console.WriteLine("employer: " + employer);
                Console.WriteLine("target: " + target);
                Console.WriteLine("systemName: " + systemName);
                Console.WriteLine("mresult: " + mresult);
                StarMap map = Helper.LoadCurrentMap();
                System system = map.FindSystemByName(systemName);
                FactionControl employerControl = system.FindFactionControlByFaction((Faction)Enum.Parse(typeof(Faction), employer));
                FactionControl targetControl = system.FindFactionControlByFaction((Faction)Enum.Parse(typeof(Faction), target));

                if (mresult == "Victory") {
                    Console.WriteLine("Victory Result");
                    int realChange = Math.Min(Math.Abs(employerControl.percentage - 100), Helper.LoadSettings().percentageForWin);
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
                                    Console.WriteLine("Points deducted");
                                }
                            }
                            debugcounter--;
                        }
                    }
                }
                else {
                    Console.WriteLine("Loss Result");
                    int realChange = Math.Min(employerControl.percentage, Helper.LoadSettings().percentageForLoss);
                    employerControl.percentage -= realChange;
                    targetControl.percentage += realChange;
                    Console.WriteLine(realChange + " Points traded");
                }
                Helper.SaveCurrentMap(map);
                return system;
            }
            catch (Exception e) {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}
