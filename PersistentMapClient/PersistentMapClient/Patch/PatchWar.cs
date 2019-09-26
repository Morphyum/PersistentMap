using BattleTech;
using BattleTech.Framework;
using BattleTech.Save;
using BattleTech.UI;
using Harmony;
using HBS;
using Localize;
using PersistentMapAPI;
using PersistentMapAPI.Objects;
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
                if (__instance.HasTravelContract && Fields.warmission) {
                    __instance.GlobalContracts.Add(__instance.ActiveTravelContract);
                }
                foreach (Contract contract in __instance.GlobalContracts) {
                    contract.Override.contractDisplayStyle = ContractDisplayStyle.BaseCampaignStory;
                    int maxPriority = Mathf.FloorToInt(7 / __instance.Constants.Salvage.PrioritySalvageModifier);
                    contract.Override.salvagePotential = Mathf.Min(maxPriority, Mathf.RoundToInt(contract.SalvagePotential * Fields.settings.priorityContactPayPercentage));
                    contract.Override.negotiatedSalvage = 1f;
                }
                //tags:
                /* FundsAddedAction = 'BtSaveEdit.FundsAdded'
    InventoryAddedAction = 'BtSaveEdit.InventoryAdded'
    InventoryDeletedAction = 'BtSaveEdit.InventoryDeleted'
    PilotChangedAction = 'BtSaveEdit.PilotChanged'
    ReputationChangedAction = 'BtSaveEdit.ReputationChanged'
    SaveCleanedAction = 'BtSaveEdit.SaveCleaned'
    StarSystemsDeletedAction = 'BtSaveEdit.StarSystemsDeleted'
    ContractsDeletedAction = 'BtSaveEdit.ContractsDeleted'
    MechsRemovedAction = 'BtSaveEdit.MechsRemoved'
    MechsAddedAction = 'BtSaveEdit.MechsAdded'
    BlackMarketChangedAction = 'BtSaveEdit.BlackMarketAccessChanged'
    CompanyTagsChangedAction = 'BtSaveEdit.CompanyTagsChanged'
    StarSystemWarpAction = 'BtSaveEdit.ChangedCurrentStarSystem'*/
                List<string> saveedits = new List<string>() { "BtSaveEdit.FundsAdded", "BtSaveEdit.InventoryAdded", "BtSaveEdit.ReputationChanged",
                    "BtSaveEdit.MechsAdded" };
                foreach (string cheat in saveedits) {
                    if (__instance.CompanyStats.ContainsStatistic(cheat)) {
                        Fields.cheater = true;
                        SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(__instance);
                        interruptQueue.QueueGenericPopup_NonImmediate("Save Edited!", "You have edited your save file in a way that disqualifies you from the war game, your missions wont be influenceing the war. All other fucntions work as normally.", true);
                        break;
                    }
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
                AccessTools.Method(typeof(SimGameState), "OnNewQuarterBegin")
                    .Invoke(__instance, new object[] { });
                return false;
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(SGDebugEventWidget), "Submit")]
    public static class SGDebugEventWidget_Submit_Patch {
        static void Postfix(SGDebugEventWidget __instance, SGDebugEventWidget.DebugType ___curType, SimGameState ___Sim) {
            try {
                /*public enum DebugType
		{
			All = -1,
			Insert_Event,
			Update_Tags,
			Update_Stats,
			Add_Mech,
			Add_Funds,
			Add_Pilot_Exp
		}*/
                if (___curType == SGDebugEventWidget.DebugType.Add_Funds) {
                    ___Sim.CompanyStats.AddStatistic<int>("BtSaveEdit.FundsAdded", 1);
                    Fields.cheater = true;
                    SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(__instance);
                    interruptQueue.QueueGenericPopup_NonImmediate("Save Edited!", "You have edited your save file in a way that disqualifies you from the war game, your missions wont be influenceing the war. All other fucntions work as normally.", true);

                }
                if (___curType == SGDebugEventWidget.DebugType.Add_Mech) {
                    ___Sim.CompanyStats.AddStatistic<int>("BtSaveEdit.MechsAdded", 1);
                    Fields.cheater = true;
                    SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(__instance);
                    interruptQueue.QueueGenericPopup_NonImmediate("Save Edited!", "You have edited your save file in a way that disqualifies you from the war game, your missions wont be influenceing the war. All other fucntions work as normally.", true);

                }
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(CombatDebugHUD), "DEBUG_CompleteAllContractObjectives")]
    public static class CombatDebugHUD_DEBUG_CompleteAllContractObjectives_Patch {
        static void Postfix() {
            try {
                Fields.skipmission = true;
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(StarSystem), "ResetContracts")]
    public static class StarSystem_ResetContracts_Patch {
        static void Postfix(StarSystem __instance) {
            try {
                AccessTools.Field(typeof(SimGameState), "globalContracts")
                    .SetValue(__instance.Sim, new List<Contract>());
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

    [HarmonyPatch(typeof(SimGameState), "AddPredefinedContract2")]
    public static class SimGameState_AddPredefinedContract_Patch {
        static void Postfix(SimGameState __instance, Contract __result) {
            try {
                if (Fields.warmission) {
                    if (__result == null) {
                        PersistentMapClient.Logger.Log("No Contract");
                    }
                    if (__result.Override == null) {
                        PersistentMapClient.Logger.Log(__result.Name + " Does not have an ovveride");
                    }
                    if (__result.InitialContractValue == 0) {
                        PersistentMapClient.Logger.Log(__result.Name + " Does not have an InitialContractValue");
                    }
                    if (__instance.Constants == null) {
                        PersistentMapClient.Logger.Log("No Constants");
                    }
                    if (__instance.Constants.Salvage == null) {
                        PersistentMapClient.Logger.Log("No Salvage Constants");
                    }
                    if (__instance.Constants.Salvage.PrioritySalvageModifier == 0f) {
                        PersistentMapClient.Logger.Log("No PrioritySalvageModifier");
                    }
                    if (Fields.settings == null) {
                        PersistentMapClient.Logger.Log("No Settings");
                    }
                    if (Fields.settings.priorityContactPayPercentage == 0f) {
                        PersistentMapClient.Logger.Log("No priorityContactPayPercentage");
                    }
                    __result.SetInitialReward(Mathf.RoundToInt(__result.InitialContractValue * Fields.settings.priorityContactPayPercentage));
                    int maxPriority = Mathf.FloorToInt(7 / __instance.Constants.Salvage.PrioritySalvageModifier);
                    __result.Override.salvagePotential = Mathf.Min(maxPriority, Mathf.RoundToInt(__result.Override.salvagePotential * Fields.settings.priorityContactPayPercentage));
                    AccessTools.Method(typeof(Contract), "set_SalvagePotential")
                        .Invoke(__result, new object[] { __result.Override.salvagePotential });
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
        static void Postfix(SGSystemViewPopulator __instance, StarSystem ___starSystem, SimGameState ___simState) {
            try {
                if (GameObject.Find("COMPANYNAMES") == null) {
                    GameObject old = GameObject.Find("uixPrfPanl_NAV_systemStats-Element-MANAGED");
                    if (old != null) {
                        GameObject newwidget = GameObject.Instantiate(old);
                        newwidget.transform.SetParent(old.transform.parent, false);
                        newwidget.name = "COMPANYNAMES";
                        old.transform.position = new Vector3(old.transform.position.x, 311, old.transform.position.z);
                        old.transform.FindRecursive("dotgrid").gameObject.SetActive(false);
                        old.transform.FindRecursive("crossLL").gameObject.SetActive(false);
                        newwidget.transform.position = new Vector3(old.transform.position.x, 106, old.transform.position.z);
                        newwidget.transform.FindRecursive("stats_factionsAndClimate").gameObject.SetActive(false);
                        newwidget.transform.FindRecursive("owner_icon").gameObject.SetActive(false);
                        newwidget.transform.FindRecursive("uixPrfIndc_SIM_Reputation-MANAGED").gameObject.SetActive(false);
                        newwidget.transform.FindRecursive("crossUL").gameObject.SetActive(false);
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
                if (companyObject != null && Fields.currentMap != null) {
                    TextMeshProUGUI companietext = companyObject.transform.FindRecursive("txt-owner").GetComponent<TextMeshProUGUI>();
                    PersistentMapAPI.System system = Fields.currentMap.systems.FirstOrDefault(x => x.name.Equals(___starSystem.Name));
                    if (system != null && companietext != null) {
                        List<string> companyNames = new List<string>();
                        foreach (Company company in system.companies) {
                            companyNames.Add("(" + Helper.GetFactionShortName(company.Faction, ___simState.DataManager) + ") " + company.Name);
                        }
                        companietext.SetText(string.Join(Environment.NewLine, companyNames.ToArray()));
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

    [HarmonyBefore(new string[] { "de.morphyum.GlobalDifficulty" })]
    [HarmonyPatch(typeof(Starmap), "PopulateMap", new Type[] { typeof(SimGameState) })]
    public static class Starmap_PopulateMap_Patch {
       
        private static MethodInfo methodSetOwner = AccessTools.Method(typeof(StarSystemDef), "set_Owner");
        private static MethodInfo methodSetContractEmployers = AccessTools.Method(typeof(StarSystemDef), "set_ContractEmployers");
        private static MethodInfo methodSetContractTargets = AccessTools.Method(typeof(StarSystemDef), "set_ContractTargets");
        private static MethodInfo methodSetDescription = AccessTools.Method(typeof(StarSystemDef), "set_Description");
        private static FieldInfo fieldSimGameInterruptManager = AccessTools.Field(typeof(SimGameState), "interruptQueue");

        static void Postfix(Starmap __instance, SimGameState simGame) {
            try {
                PersistentMapClient.Logger.LogIfDebug($"methodSetOwner is:({methodSetOwner})");
                PersistentMapClient.Logger.LogIfDebug($"methodSetContractEmployers is:({methodSetContractEmployers})");
                PersistentMapClient.Logger.LogIfDebug($"methodSetContractTargets is:({methodSetContractTargets})");
                PersistentMapClient.Logger.LogIfDebug($"methodSetDescription is:({methodSetDescription})");
                PersistentMapClient.Logger.LogIfDebug($"fieldSimGameInterruptManager is:({fieldSimGameInterruptManager})");
                Fields.currentMap = Web.GetStarMap();
                if (Fields.currentMap == null) {
                    PersistentMapClient.Logger.LogIfDebug("Map not found");
                    SimGameInterruptManager interruptQueue = (SimGameInterruptManager)fieldSimGameInterruptManager.GetValue(simGame);
                    interruptQueue.QueueGenericPopup_NonImmediate("Connection Failure", "Map could not be downloaded", true);
                    return;
                }

                List<string> changeNotifications = new List<string>();
                List<StarSystem> transitiveContractUpdateTargets = new List<StarSystem>();

                foreach (PersistentMapAPI.System system in Fields.currentMap.systems) {
                    if (system == null) {
                        PersistentMapClient.Logger.Log("System in map null");
                    }
                    if (system.activePlayers > 0) {
                        //DISABLED BECAUSE MARKER BROKE
                       // AddActivePlayersBadgeToSystem(system);
                    }

                    StarSystem system2 = simGame.StarSystems.Find(x => x.Name.Equals(system.name));
                    if (system2 != null) {
                        if (system2.Tags == null) {
                            PersistentMapClient.Logger.Log(system2.Name + ": Has no Tags");
                        }
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
                        }
                        else {
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
                            new object[] { Helper.ChangeWarDescription(changedSystem, simGame, system).Def.Description });
                    }
                }

                if (changeNotifications.Count > 0 && !Fields.firstpass) {
                    SimGameInterruptManager interruptQueue2 = (SimGameInterruptManager)fieldSimGameInterruptManager.GetValue(simGame);
                    interruptQueue2.QueueGenericPopup_NonImmediate("War Activities", string.Join("\n", changeNotifications.ToArray()), true);
                }
                else {
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
            Transform playerMarker = starObject.transform.Find("StarInner");
            Transform playerMarkerUnvisited = starObject.transform.Find("StarInnerUnvisited");
            // Only one of these will actually be active for a star system at any given time
            playerMarker.localScale = new Vector3(Fields.settings.activePlayerMarkerSize, Fields.settings.activePlayerMarkerSize, Fields.settings.activePlayerMarkerSize);
            playerMarkerUnvisited.localScale = new Vector3(Fields.settings.activePlayerMarkerSize, Fields.settings.activePlayerMarkerSize, Fields.settings.activePlayerMarkerSize);
        }
    }

    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract_Patch {
        static void Postfix(Contract __instance, BattleTech.MissionResult result) {
            try {
                if (!__instance.IsFlashpointContract) {
                    GameInstance game = LazySingletonBehavior<UnityGameInstance>.Instance.Game;
                    if (game.Simulation.IsFactionAlly(__instance.Override.employerTeam.faction)) {
                        if (Fields.cheater) {
                            PersistentMapClient.Logger.Log("cheated save, skipping war upload");
                            return;
                        }
                        if (Fields.skipmission) {
                            Fields.skipmission = false;
                            SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(game.Simulation);
                            interruptQueue.QueueGenericPopup_NonImmediate("Invalid Mission!", "Something went wrong with your mission, result not uploaded.", true);
                            return;
                        }
                        bool updated = false;
                        StarSystem system = game.Simulation.StarSystems.Find(x => x.ID == __instance.TargetSystem);
                        foreach (StarSystem potential in game.Simulation.StarSystems) {
                            if (Helper.IsCapital(system, __instance.Override.employerTeam.faction) || (!potential.Name.Equals(system.Name) &&
                                potential.Owner == __instance.Override.employerTeam.faction &&
                                Helper.GetDistanceInLY(potential.Position.x, potential.Position.y, system.Position.x, system.Position.y) <= game.Simulation.Constants.Travel.MaxJumpDistance)) {
                                int planetSupport = Helper.CalculatePlanetSupport(game.Simulation, system, __instance.Override.employerTeam.faction, __instance.Override.targetTeam.faction);
                                float num8 = (float)__instance.GetNegotiableReputationBaseValue(game.Simulation.Constants) * __instance.PercentageContractReputation;
                                float num9 = Convert.ToSingle(__instance.GameContext.GetObject(GameContextObjectTagEnum.ContractBonusEmployerReputation));
                                float num10 = (float)__instance.GetBaseReputationValue(game.Simulation.Constants);
                                float num11 = num8 + num9 + num10;
                                int repchange = Mathf.RoundToInt(num11);
                                PersistentMapAPI.MissionResult mresult = new PersistentMapAPI.MissionResult(__instance.Override.employerTeam.faction, __instance.Override.targetTeam.faction, result, system.Name, __instance.Difficulty, repchange, planetSupport);
                                bool postSuccessfull = Web.PostMissionResult(mresult, game.Simulation.Player1sMercUnitHeraldryDef.Description.Name);
                                if (!postSuccessfull) {
                                    SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(game.Simulation);
                                    interruptQueue.QueueGenericPopup_NonImmediate("Connection Failure", "Result could not be transfered", true);
                                }
                                updated = true;
                                break;
                            }
                        }
                        if (!updated) {
                            SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(game.Simulation);
                            interruptQueue.QueueGenericPopup_NonImmediate("You are surrounded!", "There is no more neighbor system in your factions control, so you didnt earn any influence here.", true);
                        }
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
                __instance.Sim.GlobalContracts.Clear();
                if (__instance.Sim.HasTravelContract && Fields.warmission) {
                    __instance.Sim.GlobalContracts.Add(__instance.Sim.ActiveTravelContract);
                }

                foreach (KeyValuePair<Faction, FactionDef> pair in __instance.Sim.FactionsDict) {
                    if (!Fields.excludedFactions.Contains(pair.Key)) {
                        int numberOfContracts = 0;
                        if (__instance.Sim.IsFactionAlly(pair.Key, null)) {
                            numberOfContracts = Fields.settings.priorityContractsPerAlly;
                        }
                        if (numberOfContracts > 0) {
                            List<PersistentMapAPI.System> targets = new List<PersistentMapAPI.System>();
                            if (Fields.currentMap != null) {
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
                                            Faction possibleThird = Faction.AuriganPirates;
                                            foreach (FactionControl control in targets[i].controlList.OrderByDescending(x => x.percentage)) {
                                                if (control.faction != pair.Key && control.faction != target) {
                                                    possibleThird = control.faction;
                                                    break;
                                                }
                                            }
                                            Contract contract = Helper.GetNewWarContract(__instance.Sim, realSystem.Def.GetDifficulty(__instance.Sim.SimGameMode), pair.Key, target, possibleThird, realSystem);
                                            if (contract != null) {
                                                contract.Override.contractDisplayStyle = ContractDisplayStyle.BaseCampaignStory;
                                                contract.SetInitialReward(Mathf.RoundToInt(contract.InitialContractValue * Fields.settings.priorityContactPayPercentage));
                                                int maxPriority = Mathf.FloorToInt(7 / __instance.Sim.Constants.Salvage.PrioritySalvageModifier);
                                                contract.Override.salvagePotential = Mathf.Min(maxPriority, Mathf.RoundToInt(contract.Override.salvagePotential * Fields.settings.priorityContactPayPercentage));
                                                contract.Override.negotiatedSalvage = 1f;
                                                __instance.Sim.GlobalContracts.Add(contract);
                                            }
                                            else {
                                                PersistentMapClient.Logger.Log("Prio contract is null");
                                            }
                                        }
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

    [HarmonyPatch(typeof(SimGameState), "CreateTravelContract")]
    public static class SimGameState_CreateTravelContract_Patch {
        static void Prefix(ref Faction employer, ref Faction target, ref Faction targetsAlly, ref Faction employersAlly, ref Faction neutralToAll, ref Faction hostileToAll) {
            try {
                if (Fields.prioGen) {
                    employer = Fields.prioEmployer;
                    employersAlly = Fields.prioEmployer;
                    target = Fields.prioTarget;
                    targetsAlly = Fields.prioTarget;
                    if(hostileToAll != Faction.INVALID_UNSET) {
                        hostileToAll = Fields.prioThird;
                    }
                }
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "PrepContract")]
    public static class SimGameState_PrepContract_Patch {
        static void Prefix(ref Faction employer, ref Faction employersAlly, ref Faction target, ref Faction targetsAlly, ref Faction NeutralToAll, ref Faction HostileToAll) {
            try {
                if (Fields.prioGen) {
                    employer = Fields.prioEmployer;
                    employersAlly = Fields.prioEmployer;
                    target = Fields.prioTarget;
                    targetsAlly = Fields.prioTarget;
                    if (HostileToAll != Faction.INVALID_UNSET) {
                        HostileToAll = Fields.prioThird;
                    }
                }
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "CreateBreakContractWarning")]
    public static class SimGameState_CreateBreakContractWarning_Patch {
        static bool Prefix(SimGameState __instance, Action continueAction, Action cancelAction) {
            try {
                if (__instance.ActiveTravelContract == null) {
                    return false;
                }
                string primaryButtonText = Strings.T("Confirm");
                string message = Strings.T("Commander, we're locked into our existing contract already. We can't take another one without seeing this one through first. We've got enough problems with people shooting us already, let's not add lawyers to the mix.");
                PauseNotification.Show("CONTRACT VIOLATION", message, __instance.GetCrewPortrait(SimGameCrew.Crew_Darius), string.Empty, true, cancelAction, primaryButtonText, null, null);
                return false;
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
                return false;
            }
        }
    }
}
