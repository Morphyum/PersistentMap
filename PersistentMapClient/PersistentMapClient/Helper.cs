using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;
using Harmony;
using HBS;
using HBS.Collections;
using Newtonsoft.Json;
using PersistentMapAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace PersistentMapClient {

    public class SaveFields {

    }

    public class Helper {
        public static Settings LoadSettings() {
            try {
                using (StreamReader r = new StreamReader($"{ PersistentMapClient.ModDirectory}/settings.json")) {
                    string json = r.ReadToEnd();
                    return JsonConvert.DeserializeObject<Settings>(json);
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex);
                return null;
            }
        }

        public static StarMap GetStarMap() {
            try {
                string URL = Fields.settings.ServerURL + "warServices/StarMap";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "GET";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                StarMap map;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string mapstring = reader.ReadToEnd();
                    map = JsonConvert.DeserializeObject<StarMap>(mapstring);
                }
                return map;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public static void PostMissionResult(PersistentMapAPI.MissionResult mresult) {
            try {
                string URL = Fields.settings.ServerURL + "warServices/Mission/?employer=" + mresult.employer + "&target=" + mresult.target
                    + "&systemName=" + mresult.systemName + "&mresult=" + mresult.result.ToString();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "GET";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string mapstring = reader.ReadToEnd();
                    Logger.LogLine(mapstring);
                }

            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }

        public static string GetFactionTag(Faction faction) {
            try {
                return "planet_faction_" + faction.ToString().ToLower();
            }
            catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public static bool IsBorder(StarSystem system, SimGameState Sim) {
            try {
                bool result = false;
                if (Sim.Starmap != null) {
                    if (system.Owner != Faction.NoFaction) {
                        foreach (StarSystem neigbourSystem in Sim.Starmap.GetAvailableNeighborSystem(system)) {
                            if (system.Owner != neigbourSystem.Owner && neigbourSystem.Owner != Faction.NoFaction) {
                                result = true;
                                break;
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception ex) {
                Logger.LogError(ex);
                return false;
            }
        }

        public static StarSystem ChangeWarDescription(StarSystem system, SimGameState Sim, PersistentMapAPI.System warsystem) {
            try {
                if (IsBorder(system, Sim)) {
                    List<string> factionList = new List<string>();
                    factionList.Add("Current Control:");
                    foreach (FactionControl fc in warsystem.controlList) {
                        if (fc.percentage != 0) {
                            factionList.Add(GetFactionName(fc.faction, Sim.DataManager) + ": " + fc.percentage + "%");
                        }
                    }
                    if (!Fields.FluffDescriptions.ContainsKey(system.Name)) {
                        Fields.FluffDescriptions.Add(system.Name, system.Def.Description.Details);
                    }
                    AccessTools.Method(typeof(DescriptionDef), "set_Details").Invoke(system.Def.Description, new object[] { string.Join("\n", factionList.ToArray()) });
                }
                else if (Fields.FluffDescriptions.ContainsKey(system.Name)) {
                    AccessTools.Method(typeof(DescriptionDef), "set_Details").Invoke(system.Def.Description, new object[] { Fields.FluffDescriptions[system.Name] });
                    Fields.FluffDescriptions.Remove(system.Name);
                }
                return system;
            }
            catch (Exception ex) {
                Logger.LogError(ex);
                return null;
            }
        }

        public static string GetFactionName(Faction faction, DataManager manager) {
            try {
                return FactionDef.GetFactionDefByEnum(manager, faction).Name.Replace("the ", "").Replace("The ", "");
            }
            catch (Exception ex) {
                Logger.LogError(ex);
                return null;
            }
        }

        public static List<Faction> GetEmployees(StarSystem system, SimGameState Sim) {
            try {
                List<Faction> employees = new List<Faction>();
                if (Sim.Starmap != null) {
                    if (system.Owner != Faction.NoFaction) {
                        employees.Add(Faction.Locals);
                        employees.Add(system.Owner);
                    }
                    foreach (StarSystem neigbourSystem in Sim.Starmap.GetAvailableNeighborSystem(system)) {
                        if (system.Owner != neigbourSystem.Owner && !employees.Contains(neigbourSystem.Owner) && neigbourSystem.Owner != Faction.NoFaction) {
                            employees.Add(neigbourSystem.Owner);
                        }
                    }

                }
                else {
                    foreach (KeyValuePair<Faction, FactionDef> pair in Sim.FactionsDict) {
                        employees.Add(pair.Key);
                    }
                }
                return employees;
            }
            catch (Exception ex) {
                Logger.LogError(ex);
                return null;
            }
        }

        public static List<Faction> GetTargets(StarSystem system, SimGameState Sim) {
            try {
                List<Faction> targets = new List<Faction>();
                if (Sim.Starmap != null) {
                    targets.Add(Faction.AuriganPirates);
                    if (system.Owner != Faction.NoFaction) {
                        targets.Add(Faction.Locals);
                    }
                    foreach (StarSystem neigbourSystem in Sim.Starmap.GetAvailableNeighborSystem(system)) {
                        if (system.Owner != neigbourSystem.Owner && !targets.Contains(neigbourSystem.Owner)) {
                            targets.Add(neigbourSystem.Owner);
                        }
                    }

                }
                else {
                    foreach (KeyValuePair<Faction, FactionDef> pair in Sim.FactionsDict) {
                        targets.Add(pair.Key);
                    }
                }
                return targets;
            }
            catch (Exception ex) {
                Logger.LogError(ex);
                return null;
            }
        }

        public static Contract GetNewWarContract(SimGameState Sim, int Difficulty, Faction emp, Faction targ, StarSystem system) {
            if (Difficulty <= 1) {
                Difficulty = 2;
            }
            else if (Difficulty > 9) {
                Difficulty = 9;
            }

            ContractDifficulty minDiffClamped = (ContractDifficulty)AccessTools.Method(typeof(SimGameState), "GetDifficultyEnumFromValue").Invoke(Sim, new object[] { Difficulty });
            ContractDifficulty maxDiffClamped = (ContractDifficulty)AccessTools.Method(typeof(SimGameState), "GetDifficultyEnumFromValue").Invoke(Sim, new object[] { Difficulty });
            List<Contract> contractList = new List<Contract>();
            int maxContracts = 1;
            int debugCount = 0;
            while (contractList.Count < maxContracts && debugCount < 1000) {
                WeightedList<MapAndEncounters> contractMaps = new WeightedList<MapAndEncounters>(WeightedListType.SimpleRandom, null, null, 0);
                List<ContractType> contractTypes = new List<ContractType>();
                Dictionary<ContractType, List<ContractOverride>> potentialOverrides = new Dictionary<ContractType, List<ContractOverride>>();
                AccessTools.Field(typeof(SimGameState), "singlePlayerTypes");
                ContractType[] singlePlayerTypes = (ContractType[])AccessTools.Field(typeof(SimGameState), "singlePlayerTypes").GetValue(Sim);
                using (MetadataDatabase metadataDatabase = new MetadataDatabase()) {
                    foreach (Contract_MDD contract_MDD in metadataDatabase.GetContractsByDifficultyRange(Difficulty - 1, Difficulty + 1)) {
                        ContractType contractType = contract_MDD.ContractTypeEntry.ContractType;
                        if (singlePlayerTypes.Contains(contractType)) {
                            if (!contractTypes.Contains(contractType)) {
                                contractTypes.Add(contractType);
                            }
                            if (!potentialOverrides.ContainsKey(contractType)) {
                                potentialOverrides.Add(contractType, new List<ContractOverride>());
                            }
                            ContractOverride item = Sim.DataManager.ContractOverrides.Get(contract_MDD.ContractID);
                            potentialOverrides[contractType].Add(item);
                        }
                    }
                    foreach (MapAndEncounters element in metadataDatabase.GetReleasedMapsAndEncountersByContractTypeAndTags(singlePlayerTypes, system.Def.MapRequiredTags, system.Def.MapExcludedTags, system.Def.SupportedBiomes)) {
                        if (!contractMaps.Contains(element)) {
                            contractMaps.Add(element, 0);
                        }
                    }
                }
                if (contractMaps.Count == 0) {
                    Logger.LogLine("Maps0 break");
                    break;
                }
                if (potentialOverrides.Count == 0) {
                    Logger.LogLine("Overrides0 break");
                    break;
                }
                contractMaps.Reset(false);
                WeightedList<Faction> validEmployers = new WeightedList<Faction>(WeightedListType.SimpleRandom, null, null, 0);
                Dictionary<Faction, WeightedList<Faction>> validTargets = new Dictionary<Faction, WeightedList<Faction>>();

                int i = debugCount;
                debugCount = i + 1;
                WeightedList<MapAndEncounters> activeMaps = new WeightedList<MapAndEncounters>(WeightedListType.SimpleRandom, contractMaps.ToList(), null, 0);
                List<MapAndEncounters> discardedMaps = new List<MapAndEncounters>();


                List<string> mapDiscardPile = (List<string>)AccessTools.Field(typeof(SimGameState), "mapDiscardPile").GetValue(Sim);

                for (int j = activeMaps.Count - 1; j >= 0; j--) {
                    if (mapDiscardPile.Contains(activeMaps[j].Map.MapID)) {
                        discardedMaps.Add(activeMaps[j]);
                        activeMaps.RemoveAt(j);
                    }
                }
                if (activeMaps.Count == 0) {
                    mapDiscardPile.Clear();
                    foreach (MapAndEncounters element2 in discardedMaps) {
                        activeMaps.Add(element2, 0);
                    }
                }
                activeMaps.Reset(false);
                MapAndEncounters level = null;
                List<EncounterLayer_MDD> validEncounters = new List<EncounterLayer_MDD>();


                Dictionary<ContractType, WeightedList<PotentialContract>> validContracts = new Dictionary<ContractType, WeightedList<PotentialContract>>();
                WeightedList<PotentialContract> flatValidContracts = null;
                do {
                    level = activeMaps.GetNext(false);
                    if (level == null) {
                        break;
                    }
                    validEncounters.Clear();
                    validContracts.Clear();
                    flatValidContracts = new WeightedList<PotentialContract>(WeightedListType.WeightedRandom, null, null, 0);
                    foreach (EncounterLayer_MDD encounterLayer_MDD in level.Encounters) {
                        ContractType contractType2 = encounterLayer_MDD.ContractTypeEntry.ContractType;
                        if (contractTypes.Contains(contractType2)) {
                            if (validContracts.ContainsKey(contractType2)) {
                                validEncounters.Add(encounterLayer_MDD);
                            }
                            else {
                                foreach (ContractOverride contractOverride2 in potentialOverrides[contractType2]) {
                                    bool flag = true;
                                    ContractDifficulty difficultyEnumFromValue = (ContractDifficulty)AccessTools.Method(typeof(SimGameState), "GetDifficultyEnumFromValue").Invoke(Sim, new object[] { contractOverride2.difficulty });
                                    Faction employer2 = Faction.INVALID_UNSET;
                                    Faction target2 = Faction.INVALID_UNSET;
                                    if (difficultyEnumFromValue >= minDiffClamped && difficultyEnumFromValue <= maxDiffClamped) {
                                        employer2 = emp;
                                        target2 = targ;
                                        int difficulty = Sim.NetworkRandom.Int(Difficulty, Difficulty + 1);
                                        system.SetCurrentContractFactions(employer2, target2);
                                        int k = 0;
                                        while (k < contractOverride2.requirementList.Count) {
                                            RequirementDef requirementDef = new RequirementDef(contractOverride2.requirementList[k]);
                                            EventScope scope = requirementDef.Scope;
                                            TagSet curTags;
                                            StatCollection stats;
                                            switch (scope) {
                                                case EventScope.Company:
                                                    curTags = Sim.CompanyTags;
                                                    stats = Sim.CompanyStats;
                                                    break;
                                                case EventScope.MechWarrior:
                                                case EventScope.Mech:
                                                    goto IL_88B;
                                                case EventScope.Commander:
                                                    goto IL_8E9;
                                                case EventScope.StarSystem:
                                                    curTags = system.Tags;
                                                    stats = system.Stats;
                                                    break;
                                                default:
                                                    goto IL_88B;
                                            }
                                            IL_803:
                                            for (int l = requirementDef.RequirementComparisons.Count - 1; l >= 0; l--) {
                                                ComparisonDef item2 = requirementDef.RequirementComparisons[l];
                                                if (item2.obj.StartsWith("Target") || item2.obj.StartsWith("Employer")) {
                                                    requirementDef.RequirementComparisons.Remove(item2);
                                                }
                                            }
                                            if (!SimGameState.MeetsRequirements(requirementDef, curTags, stats, null)) {
                                                flag = false;
                                                break;
                                            }
                                            k++;
                                            continue;
                                            IL_88B:
                                            if (scope != EventScope.Map) {
                                                throw new Exception("Contracts cannot use the scope of: " + requirementDef.Scope);
                                            }
                                            using (MetadataDatabase metadataDatabase2 = new MetadataDatabase()) {
                                                curTags = metadataDatabase2.GetTagSetForTagSetEntry(level.Map.TagSetID);
                                                stats = new StatCollection();
                                                goto IL_803;
                                            }
                                            IL_8E9:
                                            curTags = Sim.CommanderTags;
                                            stats = Sim.CommanderStats;
                                            goto IL_803;
                                        }
                                        if (flag) {
                                            PotentialContract element3 = default(PotentialContract);
                                            element3.contractOverride = contractOverride2;
                                            element3.difficulty = difficulty;
                                            element3.employer = employer2;
                                            element3.target = target2;
                                            validEncounters.Add(encounterLayer_MDD);
                                            if (!validContracts.ContainsKey(contractType2)) {
                                                validContracts.Add(contractType2, new WeightedList<PotentialContract>(WeightedListType.WeightedRandom, null, null, 0));
                                            }
                                            validContracts[contractType2].Add(element3, contractOverride2.weight);
                                            flatValidContracts.Add(element3, contractOverride2.weight);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                while (validContracts.Count == 0 && level != null);
                system.SetCurrentContractFactions(Faction.INVALID_UNSET, Faction.INVALID_UNSET);
                if (validContracts.Count == 0) {
                    if (mapDiscardPile.Count > 0) {
                        mapDiscardPile.Clear();
                    }
                    else {
                        debugCount = 1000;
                        Logger.LogLine(string.Format("[CONTRACT] Unable to find any valid contracts for available map pool. Alert designers.", new object[0]));
                    }
                }
                else {
                    GameContext gameContext = new GameContext(Sim.Context);
                    gameContext.SetObject(GameContextObjectTagEnum.TargetStarSystem, system);
                    Dictionary<ContractType, List<EncounterLayer_MDD>> finalEncounters = new Dictionary<ContractType, List<EncounterLayer_MDD>>();
                    foreach (EncounterLayer_MDD encounterLayer_MDD2 in validEncounters) {
                        ContractType contractType3 = encounterLayer_MDD2.ContractTypeEntry.ContractType;
                        if (!finalEncounters.ContainsKey(contractType3)) {
                            finalEncounters.Add(contractType3, new List<EncounterLayer_MDD>());
                        }
                        finalEncounters[contractType3].Add(encounterLayer_MDD2);
                    }
                    List<PotentialContract> discardedContracts = new List<PotentialContract>();

                    List<string> contractDiscardPile = (List<string>)AccessTools.Field(typeof(SimGameState), "contractDiscardPile").GetValue(Sim);
                    for (int m = flatValidContracts.Count - 1; m >= 0; m--) {
                        if (contractDiscardPile.Contains(flatValidContracts[m].contractOverride.ID)) {
                            discardedContracts.Add(flatValidContracts[m]);
                            flatValidContracts.RemoveAt(m);
                        }
                    }
                    if ((float)discardedContracts.Count >= (float)flatValidContracts.Count * Sim.Constants.Story.DiscardPileToActiveRatio || flatValidContracts.Count == 0) {
                        contractDiscardPile.Clear();
                        foreach (PotentialContract element4 in discardedContracts) {
                            flatValidContracts.Add(element4, 0);
                        }
                    }
                    PotentialContract next = flatValidContracts.GetNext(true);
                    ContractType finalContractType = next.contractOverride.contractType;
                    finalEncounters[finalContractType].Shuffle<EncounterLayer_MDD>();
                    string encounterGuid = finalEncounters[finalContractType][0].EncounterLayerGUID;
                    ContractOverride contractOverride3 = next.contractOverride;
                    Faction employer3 = next.employer;
                    Faction target3 = next.target;
                    int targetDifficulty = next.difficulty;

                    Contract con = (Contract)AccessTools.Method(typeof(SimGameState), "CreateTravelContract").Invoke(Sim, new object[] { level.Map.MapName, level.Map.MapPath, encounterGuid, finalContractType, contractOverride3, gameContext, employer3, target3, employer3, false, targetDifficulty });
                    mapDiscardPile.Add(level.Map.MapID);
                    contractDiscardPile.Add(contractOverride3.ID);
                    Sim.PrepContract(con, employer3, target3, target3, level.Map.BiomeSkinEntry.BiomeSkin, con.Override.travelSeed, system);
                    contractList.Add(con);
                }
            }
            if (debugCount >= 1000) {
                Logger.LogLine("Unable to fill contract list. Please inform AJ Immediately");
            }
            return contractList[0];
        }
    }
}

