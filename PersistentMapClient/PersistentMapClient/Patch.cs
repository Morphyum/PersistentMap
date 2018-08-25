using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using Harmony;
using HBS;
using PersistentMapAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PersistentMapClient {

    [HarmonyPatch(typeof(Starmap), "PopulateMap", new Type[] { typeof(SimGameState) })]
    public static class Starmap_PopulateMap_Patch {
        static void Postfix(SimGameState simGame) {
            try {
                StarMap map = Web.GetStarMap();
                if(map == null) {
                    SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(simGame);
                    interruptQueue.QueueGenericPopup_NonImmediate("Connection Failure", "Map could not be downloaded", true);
                    return;
                }
                foreach (PersistentMapAPI.System system in map.systems) {
                    StarSystem system2 = simGame.StarSystems.Find(x => x.Name.Equals(system.name));
                    if (system2 != null) {
                        Faction newOwner = system.controlList.OrderByDescending(x => x.percentage).First().faction;
                        Faction oldOwner = system2.Owner;
                        AccessTools.Method(typeof(StarSystemDef), "set_Owner").Invoke(system2.Def, new object[] {
                            newOwner });
                        AccessTools.Method(typeof(StarSystemDef), "set_ContractEmployers").Invoke(system2.Def, new object[] {
                            Helper.GetEmployees(system2, simGame) });
                        AccessTools.Method(typeof(StarSystemDef), "set_ContractTargets").Invoke(system2.Def, new object[] {
                            Helper.GetTargets(system2, simGame) });
                        system2.Tags.Remove(Helper.GetFactionTag(oldOwner));
                        system2.Tags.Add(Helper.GetFactionTag(newOwner));
                        if (Helper.IsBorder(system2, simGame) && simGame.Starmap != null) {
                            system2.Tags.Add("planet_other_battlefield");
                        }
                        else {
                            system2.Tags.Remove("planet_other_battlefield");
                        }
                        system2 = Helper.ChangeWarDescription(system2, simGame, system);
                        
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
                GameInstance game = LazySingletonBehavior<UnityGameInstance>.Instance.Game;
                StarSystem system = game.Simulation.StarSystems.Find(x => x.ID == __instance.TargetSystem);
                mresult.systemName = system.Name;
                bool postSuccessfull = Web.PostMissionResult(mresult);
                if (!postSuccessfull) {
                    SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(game.Simulation);
                    interruptQueue.QueueGenericPopup_NonImmediate("Connection Failure", "Result could not be transfered", true);
                }
                return;
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(StarSystem), "GenerateInitialContracts")]
    public static class StarSystem_GenerateInitialContracts_Patch {
        static void Postfix(StarSystem __instance) {
            try {
                if (Fields.settings.debug) {
                    AccessTools.Method(typeof(SimGameState), "SetReputation").Invoke(__instance.Sim, new object[] {
                        Faction.Steiner, 100, StatCollection.StatOperation.Set, null });
                }
                __instance.Sim.GlobalContracts.Clear();
                foreach (KeyValuePair<Faction, FactionDef> pair in __instance.Sim.FactionsDict) {
                    if (pair.Key != Faction.NoFaction) {
                        SimGameReputation rep = __instance.Sim.GetReputation(pair.Key);
                        int numberOfContracts;
                        switch (rep) {
                            case SimGameReputation.LIKED: {
                                    numberOfContracts = 1;
                                    break;
                                }
                            case SimGameReputation.FRIENDLY: {
                                    numberOfContracts = 2;
                                    break;
                                }
                            case SimGameReputation.ALLIED: {
                                    numberOfContracts = 3;
                                    break;
                                }
                            default: {
                                    numberOfContracts = 0;
                                    break;
                                }
                        }
                        if (numberOfContracts > 0) {
                            List<PersistentMapAPI.System> targets = new List<PersistentMapAPI.System>();
                            foreach(PersistentMapAPI.System potentialTarget in Web.GetStarMap().systems) {
                                FactionControl control = potentialTarget.controlList.FirstOrDefault(x => x.faction == pair.Key);
                                if (control != null && control.percentage < 100) {
                                    targets.Add(potentialTarget);
                                }
                            }
                            targets.Shuffle();
                            numberOfContracts = Mathf.Min(numberOfContracts, targets.Count);
                            for (int i = 0; i < numberOfContracts; i++) {
                                StarSystem realSystem = __instance.Sim.StarSystems.FirstOrDefault(x => x.Name.Equals(targets[i].name));
                                if (realSystem != null) {
                                    Contract contract = Helper.GetNewWarContract(__instance.Sim, realSystem.Def.Difficulty, pair.Key, realSystem.Owner, realSystem);
                                    contract.Override.contractDisplayStyle = ContractDisplayStyle.BaseCampaignStory;
                                    contract.SetInitialReward(Mathf.RoundToInt(contract.InitialContractValue * Fields.settings.priorityContactPayPercentage));
                                    int maxPriority = Mathf.FloorToInt(7 / __instance.Sim.Constants.Salvage.PrioritySalvageModifier);
                                    contract.Override.salvagePotential = Mathf.Min(maxPriority, Mathf.RoundToInt(contract.Override.salvagePotential * Fields.settings.priorityContactPayPercentage));
                                    contract.Override.negotiatedSalvage = 1f;
                                    __instance.Sim.GlobalContracts.Add(contract);
                                }
                            }
                        }

                       
                        
                    }
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
}