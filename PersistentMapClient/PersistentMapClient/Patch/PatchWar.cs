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
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
                PersistentMapClient.Logger.LogError(e);
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
                PersistentMapClient.Logger.LogError(e);
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
                PersistentMapClient.Logger.LogError(e);
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
                PersistentMapClient.Logger.LogError(e);
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
                PersistentMapClient.Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SGSystemViewPopulator), "UpdateRoutedSystem")]
     public static class SGSystemViewPopulator_UpdateRoutedSystem_Patch {
         static void Postfix(SGSystemViewPopulator __instance, StarSystem ___starSystem) {
             try {
                if (GameObject.Find("COMPANYNAMES") == null) {
                    GameObject old = GameObject.Find("uixPrfPanl_NAV_systemStats-Element-MANAGED");
                    if (old != null) {
                        GameObject newwidget = GameObject.Instantiate(old);
                        newwidget.transform.SetParent(old.transform.parent, false);
                        newwidget.name = "COMPANYNAMES";
                        old.transform.position = new Vector3(old.transform.position.x, 311, old.transform.position.z);
                        old.transform.FindRecursive("dotgrid").gameObject.active = false;
                        old.transform.FindRecursive("crossLL").gameObject.active = false;
                        newwidget.transform.position = new Vector3(old.transform.position.x, 106, old.transform.position.z);
                        newwidget.transform.FindRecursive("stats_factionsAndClimate").gameObject.active = false;
                        newwidget.transform.FindRecursive("owner_icon").gameObject.active = false;
                        newwidget.transform.FindRecursive("uixPrfIndc_SIM_Reputation-MANAGED").gameObject.active = false;
                        newwidget.transform.FindRecursive("crossUL").gameObject.active = false;
                        GameObject ownerPanel = newwidget.transform.FindRecursive("owner_detailsPanel").gameObject;
                        ownerPanel.transform.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
                        RectTransform ownerRect = ownerPanel.GetComponent<RectTransform>();
                        ownerRect.sizeDelta = new Vector2(ownerRect.sizeDelta.x, 145);
                        TextMeshProUGUI title = newwidget.transform.FindRecursive("ownerTitle_text").GetComponent<TextMeshProUGUI>();
                        title.SetText("COMPANIES");
                        TextMeshProUGUI text = newwidget.transform.FindRecursive("txt-owner").GetComponent<TextMeshProUGUI>();
                        text.alignment = TextAlignmentOptions.TopLeft;
                        text.enableWordWrapping = false;
                    }
                }
                GameObject companyObject = GameObject.Find("COMPANYNAMES");
                if (companyObject != null) {
                    TextMeshProUGUI companietext = companyObject.transform.FindRecursive("txt-owner").GetComponent<TextMeshProUGUI>();
                    PersistentMapAPI.System system = Fields.currentMap.systems.FirstOrDefault(x => x.name.Equals(___starSystem.Name));
                    if (system != null && companietext != null) {
                        companietext.SetText(string.Join(Environment.NewLine, system.companies.ToArray()));
                    }
                    else {
                        companietext.SetText("");
                    }
                }
             }
             catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
             }
         }
     }


    [HarmonyPatch(typeof(Starmap), "PopulateMap", new Type[] { typeof(SimGameState) })]
    public static class Starmap_PopulateMap_Patch {

        private static MethodInfo methodSetOwner = AccessTools.Method(typeof(StarSystemDef), "set_Owner");
        private static MethodInfo methodSetContractEmployers = AccessTools.Method(typeof(StarSystemDef), "set_ContractEmployers");
        private static MethodInfo methodSetContractTargets = AccessTools.Method(typeof(StarSystemDef), "set_ContractTargets");
        private static MethodInfo methodSetDescription = AccessTools.Method(typeof(StarSystemDef), "set_Description");
        private static FieldInfo fieldSimGameInterruptManager = AccessTools.Field(typeof(SimGameState), "interruptQueue");

        static void Postfix(Starmap __instance, SimGameState simGame) {
            try {
                Fields.currentMap = Web.GetStarMap();
                if (Fields.currentMap == null) {
                    SimGameInterruptManager interruptQueue = (SimGameInterruptManager)fieldSimGameInterruptManager.GetValue(simGame);
                    interruptQueue.QueueGenericPopup_NonImmediate("Connection Failure", "Map could not be downloaded", true);
                    return;
                }

                List<string> changeNotifications = new List<string>();
                List<StarSystem> transitiveContractUpdateTargets = new List<StarSystem>();
                foreach (PersistentMapAPI.System system in Fields.currentMap.systems) {
                    if (system.activePlayers > 0) {
                        AddActivePlayersBadgeToSystem(system);
                    }

                    StarSystem system2 = simGame.StarSystems.Find(x => x.Name.Equals(system.name));
                    if (system2 != null) {
                        Faction newOwner = system.controlList.OrderByDescending(x => x.percentage).First().faction;
                        Faction oldOwner = system2.Owner;
                        // Update control to the new faction
                        methodSetOwner.Invoke(system2.Def, new object[] { newOwner });
                        system2.Tags.Remove(Helper.GetFactionTag(oldOwner));
                        system2.Tags.Add(Helper.GetFactionTag(newOwner));
                        system2 = Helper.ChangeWarDescription(system2, simGame, system);

                        // Update the contracts on the system
                        methodSetContractEmployers.Invoke(system2.Def, new object[] { Helper.GetEmployees(system2, simGame) });
                        methodSetContractTargets.Invoke(system2.Def, new object[] { Helper.GetTargets(system2, simGame) });

                        // If the system is next to enemy factions, update the map to show the border
                        if (Helper.IsBorder(system2, simGame) && simGame.Starmap != null) {
                            system2.Tags.Add("planet_other_battlefield");
                        } else {
                            system2.Tags.Remove("planet_other_battlefield");
                        }

                        // If the owner changes, add a notice to the player and mark neighbors for contract updates
                        if (newOwner != oldOwner) {
                            string newOwnerName = Helper.GetFactionShortName(newOwner, simGame.DataManager);
                            string oldOwnerName = Helper.GetFactionShortName(oldOwner, simGame.DataManager);
                            changeNotifications.Add($"{newOwnerName} took {system2.Name} from {oldOwnerName}");
                            foreach (StarSystem changedSystem in simGame.Starmap.GetAvailableNeighborSystem(system2)) {
                                if (!transitiveContractUpdateTargets.Contains(changedSystem)) {
                                    transitiveContractUpdateTargets.Add(changedSystem);
                                }
                            }
                        }
                    }
                }

                // For each system neighboring a system whose ownership changed, update their contracts as well
                foreach (StarSystem changedSystem in transitiveContractUpdateTargets) {
                    methodSetContractEmployers.Invoke(changedSystem.Def, new object[] { Helper.GetEmployees(changedSystem, simGame) });
                    methodSetContractTargets.Invoke(changedSystem.Def, new object[] { Helper.GetTargets(changedSystem, simGame) });

                    // Update the description on these systems to show the new contract options
                    PersistentMapAPI.System system = Fields.currentMap.systems.FirstOrDefault(x => x.name.Equals(changedSystem.Name));
                    if (system != null) {
                        methodSetDescription.Invoke(changedSystem.Def, 
                            new object[] { Helper.ChangeWarDescription(changedSystem, simGame, system).Def.Description} );
                    }
                }

                if (changeNotifications.Count > 0 && !Fields.firstpass) {
                    SimGameInterruptManager interruptQueue2 = (SimGameInterruptManager)fieldSimGameInterruptManager.GetValue(simGame);
                    interruptQueue2.QueueGenericPopup_NonImmediate("War Activities", string.Join("\n", changeNotifications.ToArray()), true);
                } else {
                    Fields.firstpass = false;
                }
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
            }
        }

        // Creates the argo marker for player activity
        private static void AddActivePlayersBadgeToSystem(PersistentMapAPI.System system) {
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
    }

    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract_Patch {
        static void Postfix(Contract __instance, BattleTech.MissionResult result) {
            try {
                if (!__instance.IsFlashpointContract) {
                    GameInstance game = LazySingletonBehavior<UnityGameInstance>.Instance.Game;
                    StarSystem system = game.Simulation.StarSystems.Find(x => x.ID == __instance.TargetSystem);
                    int planetSupport = Helper.CalculatePlanetSupport(game.Simulation, system, __instance.Override.employerTeam.faction, __instance.Override.targetTeam.faction);
                    PersistentMapAPI.MissionResult mresult = new PersistentMapAPI.MissionResult(__instance.Override.employerTeam.faction, __instance.Override.targetTeam.faction, result, system.Name, __instance.Difficulty, Mathf.RoundToInt(__instance.GetNegotiableReputationBaseValue(game.Simulation.Constants) * __instance.PercentageContractReputation), planetSupport);
                    bool postSuccessfull = Web.PostMissionResult(mresult, game.Simulation.Player1sMercUnitHeraldryDef.Description.Name);
                    if (!postSuccessfull) {
                        SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(game.Simulation);
                        interruptQueue.QueueGenericPopup_NonImmediate("Connection Failure", "Result could not be transfered", true);
                    }
                }
                return;
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
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
                    if (!Fields.excludedFactions.Contains(pair.Key)) {
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
                            foreach (PersistentMapAPI.System potentialTarget in Fields.currentMap.systems) {
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
                                        if (pair.Key == target || Fields.excludedFactions.Contains(target)) {
                                            List<FactionControl> ownerlist = targets[i].controlList.OrderByDescending(x => x.percentage).ToList();
                                            if (ownerlist.Count > 1) {
                                                target = ownerlist[1].faction;
                                                if (Fields.excludedFactions.Contains(target)) {
                                                    target = Faction.AuriganPirates;
                                                }
                                            }
                                            else {
                                                target = Faction.AuriganPirates;
                                            }
                                        }
                                        Contract contract = Helper.GetNewWarContract(__instance.Sim, realSystem.Def.GetDifficulty(__instance.Sim.SimGameMode), pair.Key, target, realSystem);
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
                PersistentMapClient.Logger.LogError(e);
            }
        }
    }
}