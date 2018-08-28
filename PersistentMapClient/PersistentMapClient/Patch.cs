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

    [HarmonyPatch(typeof(SimGameState), "DeductQuarterlyFunds")]
    public static class SimGameState_DeductQuarterlyFunds_Patch {
        static bool Prefix(SimGameState __instance, int quarterPassed) {
            try {
                int expenditures = __instance.GetExpenditures(false);
                if (Fields.warmission) {
                    expenditures /= 2;
                }
                __instance.AddFunds(-expenditures * quarterPassed, "SimGame_Monthly", false);
                if (!__instance.IsGameOverCondition(false)) {
                    SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(__instance);
                    interruptQueue.QueueFinancialReport();
                }
                __instance.RoomManager.RefreshDisplay();
                AccessTools.Method(typeof(SimGameState), "OnNewQuarterBegin").Invoke(__instance, new object[] { });
                return false;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(StarSystem), "ResetContracts")]
    public static class StarSystem_ResetContracts_Patch {
        static void Postfix(StarSystem __instance) {
            try {
                AccessTools.Field(typeof(SimGameState), "globalContracts").SetValue(__instance.Sim, new List<Contract>());
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "PrepareBreadcrumb")]
    public static class SimGameState_PrepareBreadcrumb_Patch {
        static void Postfix(SimGameState __instance, ref Contract contract) {
            try {
                if (contract.IsPriorityContract) {
                    Fields.warmission = true;
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "AddPredefinedContract")]
    public static class SimGameState_AddPredefinedContract_Patch {
        static void Postfix(SimGameState __instance, Contract __result) {
            try {
                if (Fields.warmission) {
                    __result.SetInitialReward(Mathf.RoundToInt(__result.InitialContractValue * Fields.settings.priorityContactPayPercentage));
                    int maxPriority = Mathf.FloorToInt(7 / __instance.Constants.Salvage.PrioritySalvageModifier);
                    __result.Override.salvagePotential = Mathf.Min(maxPriority, Mathf.RoundToInt(__result.Override.salvagePotential * Fields.settings.priorityContactPayPercentage));
                    AccessTools.Method(typeof(Contract), "set_SalvagePotential").Invoke(__result, new object[] { __result.Override.salvagePotential });
                    Fields.warmission = false;
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Starmap), "PopulateMap", new Type[] { typeof(SimGameState) })]
    public static class Starmap_PopulateMap_Patch {
        static void Postfix(SimGameState simGame) {
            try {
                StarMap map = Web.GetStarMap();
                if (map == null) {
                    SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(simGame);
                    interruptQueue.QueueGenericPopup_NonImmediate("Connection Failure", "Map could not be downloaded", true);
                    return;
                }
                List<string> changes = new List<string>();
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
                        if (newOwner != oldOwner) {
                            changes.Add(Helper.GetFactionShortName(newOwner, simGame.DataManager) + " took " + system2.Name + " from " + Helper.GetFactionShortName(oldOwner, simGame.DataManager));
                        }
                    }
                }
                if (changes.Count > 0 && !Fields.firstpass) {
                    SimGameInterruptManager interruptQueue2 = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(simGame);
                    interruptQueue2.QueueGenericPopup_NonImmediate("War Activities", string.Join("\n", changes.ToArray()), true);
                } else {
                    Fields.firstpass = false;
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
                mresult.difficulty = __instance.Difficulty;
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
                            foreach (PersistentMapAPI.System potentialTarget in Web.GetStarMap().systems) {
                                FactionControl control = potentialTarget.controlList.FirstOrDefault(x => x.faction == pair.Key);
                                if (control != null && control.percentage < 100 && control.percentage != 0) {
                                    targets.Add(potentialTarget);
                                }
                            }
                            targets =  targets.OrderBy(x => Helper.GetDistanceInLY(__instance.Sim.CurSystem,x, __instance.Sim.StarSystems)).ToList();
                            numberOfContracts = Mathf.Min(numberOfContracts, targets.Count);
                            for (int i = 0; i < numberOfContracts; i++) {
                                StarSystem realSystem = __instance.Sim.StarSystems.FirstOrDefault(x => x.Name.Equals(targets[i].name));
                                if (realSystem != null) {
                                    Faction target = realSystem.Owner;
                                    if (pair.Key == target) {
                                        List<FactionControl> ownerlist = targets[i].controlList.OrderByDescending(x => x.percentage).ToList();
                                        if(ownerlist.Count > 1) {
                                            target = ownerlist[1].faction;
                                        } else {
                                            target = Faction.Locals;
                                        }
                                    }
                                    Contract contract = Helper.GetNewWarContract(__instance.Sim, realSystem.Def.Difficulty, pair.Key, target, realSystem);
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