using BattleTech;
using Harmony;
using HBS;
using PersistentMapAPI;
using System;
using System.Linq;

namespace PersistentMapClient {

    [HarmonyPatch(typeof(Starmap), "PopulateMap", new Type[] { typeof(SimGameState) })]
    public static class Starmap_PopulateMap_Patch {
        static void Postfix(SimGameState simGame) {
            try {
                StarMap map = Helper.GetStarMap();
                foreach (PersistentMapAPI.System system in map.systems) {
                    StarSystem system2 = simGame.StarSystems.Find(x => x.Name.Equals(system.name));
                    if (system2 != null) {
                        AccessTools.Method(typeof(StarSystemDef), "set_Owner").Invoke(system2.Def, new object[] {
                            system.controlList.OrderByDescending(x => x.percentage).First().faction });
                    }
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract_Patch {
        static void Postfix(Contract __instance, BattleTech.MissionResult result) {
            try {
                PersistentMapAPI.MissionResult mresult = new PersistentMapAPI.MissionResult();
                mresult.employer = __instance.Override.employerTeam.faction;
                mresult.target = __instance.Override.targetTeam.faction;
                mresult.result = result;
                mresult.systemName = __instance.TargetSystem;
                Helper.PostMissionResult(mresult);
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
}