using System;
using System.ServiceModel;
using System.ServiceModel.Activation;

namespace WarTechAPI {

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class WarServices : IWarServices {
        public StarMap GetStarmap() {
            Console.WriteLine("Map retreived");
            return Helper.LoadCurrentMap();
        }

        public System PostMissionResult(MissionResult postedResult) {
            try {
                Console.WriteLine("New Result Posted");
                StarMap map = Helper.LoadCurrentMap();
                System system = map.FindSystemByName(postedResult.systemName);
                FactionControl employerControl = system.FindFactionControlByFaction(postedResult.employer);
                FactionControl targetControl = system.FindFactionControlByFaction(postedResult.target);

                if (postedResult.result == BattleTech.MissionResult.Victory) {
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
                                if (leftOverFaction.faction != postedResult.employer && leftOverFaction.faction != postedResult.target && leftOverFaction.percentage > 0
                                    && leftoverChange > 0) {
                                    leftOverFaction.percentage--;
                                    leftoverChange--;
                                    Console.WriteLine("Points deducted");
                                }
                            }
                            debugcounter--;
                        }
                    }
                } else {
                    Console.WriteLine("Loss Result");
                    int realChange = Math.Min(employerControl.percentage, Helper.LoadSettings().percentageForLoss);
                    employerControl.percentage -= realChange;
                    targetControl.percentage += realChange;
                    Console.WriteLine(realChange + " Points traded");
                }
                Helper.SaveCurrentMap(map);
                return system;
            } catch(Exception e) {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}
