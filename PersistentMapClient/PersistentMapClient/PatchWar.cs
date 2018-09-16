using BattleTech;
using BattleTech.Framework;
using BattleTech.Save;
using BattleTech.UI;
using Harmony;
using HBS;
using PersistentMapAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PersistentMapClient {

    [HarmonyBefore(new string[] { "de.morphyum.MercDeployments" })]
    [HarmonyPatch(typeof(SimGameState), "Rehydrate")]
    public static class SimGameState_Rehydrate_Patch {
        static void Postfix(SimGameState __instance, GameInstanceSave gameInstanceSave) {
            try {
                foreach (Contract contract in __instance.GlobalContracts) {
                    contract.Override.contractDisplayStyle = ContractDisplayStyle.BaseCampaignStory;
                    int maxPriority = Mathf.FloorToInt(7 / __instance.Constants.Salvage.PrioritySalvageModifier);
                    contract.Override.salvagePotential = Mathf.Min(maxPriority, Mathf.RoundToInt(contract.SalvagePotential * Fields.settings.priorityContactPayPercentage));
                    contract.Override.negotiatedSalvage = 1f;
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

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
        static void Postfix(Starmap __instance, SimGameState simGame) {
            try {
                ParseMap map = Web.GetStarMap();
                List<StarSystem> needUpdates = new List<StarSystem>();
                if (map == null) {
                    SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(simGame);
                    interruptQueue.QueueGenericPopup_NonImmediate("Connection Failure", "Map could not be downloaded", true);
                    return;
                }
                List<string> changes = new List<string>();
                foreach (ParseSystem system in map.systems) {
                    if (system.activePlayers > 0) {
                        GameObject starObject = GameObject.Find(system.name);
                        Transform argoMarker = starObject.transform.Find("ArgoMarker");
                        argoMarker.gameObject.SetActive(true);
                        argoMarker.localScale = new Vector3(4f, 4f, 4f);
                        argoMarker.GetComponent<MeshRenderer>().material.color = Color.grey;
                        GameObject playerNumber = new GameObject();
                        playerNumber.transform.parent = argoMarker;
                        playerNumber.name = "PlayerNumberText";
                        playerNumber.layer = 25;
                        TextMeshPro textComponent = playerNumber.AddComponent<TextMeshPro>();
                        textComponent.SetText(system.activePlayers.ToString());
                        textComponent.transform.localPosition = new Vector3(0, -0.35f, -0.05f);
                        textComponent.fontSize = 6;
                        textComponent.alignment = TextAlignmentOptions.Center;
                        textComponent.faceColor = Color.black;
                        textComponent.fontStyle = FontStyles.Bold;
                    }
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
                            foreach (StarSystem changedSystem in simGame.Starmap.GetAvailableNeighborSystem(system2)) {
                                if (!needUpdates.Contains(changedSystem)) {
                                    needUpdates.Add(changedSystem);
                                }
                            }
                        }
                    }
                }
                foreach (StarSystem changedSystem in needUpdates) {
                    AccessTools.Method(typeof(StarSystemDef), "set_ContractEmployers").Invoke(changedSystem.Def, new object[] {
                            Helper.GetEmployees(changedSystem, simGame) });
                    AccessTools.Method(typeof(StarSystemDef), "set_ContractTargets").Invoke(changedSystem.Def, new object[] {
                            Helper.GetTargets(changedSystem, simGame) });
                    ParseSystem system = map.systems.FirstOrDefault(x => x.name.Equals(changedSystem.Name));
                    if (system != null) {
                        AccessTools.Method(typeof(StarSystemDef), "set_Description").Invoke(changedSystem.Def, new object[] {
                            Helper.ChangeWarDescription(changedSystem, simGame, system).Def.Description});
                    }
                }
                if (changes.Count > 0 && !Fields.firstpass) {
                    SimGameInterruptManager interruptQueue2 = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(simGame);
                    interruptQueue2.QueueGenericPopup_NonImmediate("War Activities", string.Join("\n", changes.ToArray()), true);
                }
                else {
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
                GameInstance game = LazySingletonBehavior<UnityGameInstance>.Instance.Game;
                StarSystem system = game.Simulation.StarSystems.Find(x => x.ID == __instance.TargetSystem);
                int planetSupport = Helper.CalculatePlanetSupport(game.Simulation, system, __instance.Override.employerTeam.faction, __instance.Override.targetTeam.faction);
                PersistentMapAPI.MissionResult mresult = new PersistentMapAPI.MissionResult(__instance.Override.employerTeam.faction, __instance.Override.targetTeam.faction, result, system.Name, __instance.Difficulty, Mathf.RoundToInt(__instance.InitialContractReputation * __instance.PercentageContractReputation), planetSupport);
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
                    if (pair.Key != Faction.NoFaction && pair.Key != Faction.MercenaryReviewBoard) {
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
                            List<ParseSystem> targets = new List<ParseSystem>();
                            foreach (ParseSystem potentialTarget in Web.GetStarMap().systems) {
                                FactionControl control = potentialTarget.controlList.FirstOrDefault(x => x.faction == pair.Key);
                                if (control != null && control.percentage < 100 && control.percentage != 0) {
                                    targets.Add(potentialTarget);
                                }
                            }
                            if (targets.Count() > 0) {
                                targets = targets.OrderBy(x => Helper.GetDistanceInLY(__instance.Sim.CurSystem, x, __instance.Sim.StarSystems)).ToList();
                                numberOfContracts = Mathf.Min(numberOfContracts, targets.Count);
                                for (int i = 0; i < numberOfContracts; i++) {
                                    StarSystem realSystem = __instance.Sim.StarSystems.FirstOrDefault(x => x.Name.Equals(targets[i].name));
                                    if (realSystem != null) {
                                        Faction target = realSystem.Owner;
                                        if (pair.Key == target || target == Faction.NoFaction) {
                                            List<FactionControl> ownerlist = targets[i].controlList.OrderByDescending(x => x.percentage).ToList();
                                            if (ownerlist.Count > 1) {
                                                target = ownerlist[1].faction;
                                                if (target == Faction.NoFaction) {
                                                    target = Faction.AuriganPirates;
                                                }
                                            }
                                            else {
                                                target = Faction.AuriganPirates;
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
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
}