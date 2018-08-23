using BattleTech;
using Harmony;
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
}