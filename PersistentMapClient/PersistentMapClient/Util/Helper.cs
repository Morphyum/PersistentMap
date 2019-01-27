using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;
using Harmony;
using HBS.Collections;
using Newtonsoft.Json;
using PersistentMapAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PersistentMapClient {

    public class SaveFields {

    }

    public class Helper {
        public const string GeneratedSettingsFile = "generatedSettings.json";

        public static Settings LoadSettings() {
            string _settingsPath = $"{ PersistentMapClient.ModDirectory}/settings.json";
            try {
                // Load the settings file
                Settings settings = null;
                using (StreamReader r = new StreamReader(_settingsPath)) {
                    string json = r.ReadToEnd();
                    settings = JsonConvert.DeserializeObject<Settings>(json);
                }
                return settings;
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return null;
            }
        }

        public static double GetDistanceInLY(float x1, float y1, float x2, float y2) {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        public static Faction getfaction(string faction) {
            return (Faction)Enum.Parse(typeof(Faction), faction, true);
        }

        public static bool MeetsNewReqs(StarSystem instance, TagSet reqTags, TagSet exTags, TagSet curTags) {
            try {
                if (!curTags.ContainsAny(exTags, false)) {
                    //Check exclution for time and rep
                    foreach (string item in exTags) {
                        if (item.StartsWith("time")) {
                            string[] times = item.Split('_');
                            if ((instance.Sim.DaysPassed >= int.Parse(times[1]))) {
                                return false;
                            }
                        }
                        else if (item.StartsWith("rep")) {
                            string[] reps = item.Split('_');
                            int test = instance.Sim.GetRawReputation(Helper.getfaction(reps[1]));
                            if ((test >= int.Parse(reps[2]))) {
                                return false;
                            }
                        }
                    }

                    //Check requirements for time and rep
                    foreach (string item in reqTags) {
                        if (!curTags.Contains(item)) {
                            if (item.StartsWith("time")) {
                                string[] times = item.Split('_');
                                if (!(instance.Sim.DaysPassed >= int.Parse(times[1]))) {
                                    return false;
                                }
                            }
                            else if (item.StartsWith("rep")) {
                                string[] reps = item.Split('_');
                                int test = instance.Sim.GetRawReputation(Helper.getfaction(reps[1]));
                                if (!(test >= int.Parse(reps[2]))) {
                                    return false;
                                }
                            }
                            else {
                                return false;
                            }
                        }
                    }
                    return true;
                }
                return false;
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
                return false;
            }
        }

        public static string GetFactionShortName(Faction faction, DataManager manager) {
            try {
                return FactionDef.GetFactionDefByEnum(manager, faction).ShortName.Replace("the ", "").Replace("The ", "");
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return null;
            }
        }

        // Read the clientID (a GUID) from a place that should persist across installs.
        public static void FetchClientID(string modDirectoryPath) {
            // Starting path should be battletech\mods\PersistMapClient
            string[] directories = modDirectoryPath.Split(Path.DirectorySeparatorChar);
            DirectoryInfo modsDir = Directory.GetParent(modDirectoryPath);
            DirectoryInfo battletechDir = modsDir.Parent;

            // We want to write to Battletech/ModSaves/PersistentMapClient directory
            DirectoryInfo modSavesDir = battletechDir.CreateSubdirectory("ModSaves");
            DirectoryInfo clientDir = modSavesDir.CreateSubdirectory("PersistentMapClient");
            
            // Finally see if the file exists
            FileInfo GeneratedSettingsFile = new FileInfo(Path.Combine(clientDir.FullName, Helper.GeneratedSettingsFile));
            if (GeneratedSettingsFile.Exists) {
                // Attempt to read the file                
                try {
                    GeneratedSettings generatedSettings = null;
                    using (StreamReader r = new StreamReader(GeneratedSettingsFile.FullName)) {
                        string json = r.ReadToEnd();
                        generatedSettings = JsonConvert.DeserializeObject<GeneratedSettings>(json);
                    }                    
                    Fields.settings.ClientID = generatedSettings.ClientID;
                    PersistentMapClient.Logger.Log($"Fetched clientID:({Fields.settings.ClientID}).");
                } catch (Exception e) {
                    PersistentMapClient.Logger.Log($"Failed to read clientID from {GeneratedSettingsFile}, will overwrite!");
                    PersistentMapClient.Logger.LogError(e);
                }
            } else {
                PersistentMapClient.Logger.Log($"GeneratedSettings file at path:{GeneratedSettingsFile.FullName} does not exist, will be created.");
            }

            // If the clientID hasn't been written at this point, something went wrong. Generate a new one.
            if (Fields.settings.ClientID == null || Fields.settings.ClientID.Equals("")) {
                Guid clientID = Guid.NewGuid();                
                try {
                    GeneratedSettings newSettings = new GeneratedSettings {
                        ClientID = clientID.ToString()
                    };
                    using (StreamWriter writer = new StreamWriter(GeneratedSettingsFile.FullName, false)) {
                        string json = JsonConvert.SerializeObject(newSettings);
                        writer.Write(json);
                    }
                    Fields.settings.ClientID = clientID.ToString();
                    PersistentMapClient.Logger.Log($"Wrote new clientID ({Fields.settings.ClientID}) to generatedSettings at:{GeneratedSettingsFile.FullName}.");
                } catch (Exception e) {
                    PersistentMapClient.Logger.Log("FATAL ERROR: Failed to write clientID, cannot continue!");
                    PersistentMapClient.Logger.LogError(e);
                    // TODO: Figure out a failure strategy...
                }
            }
        }

        public static string GetFactionTag(Faction faction) {
            try {
                return "planet_faction_" + faction.ToString().ToLower();
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
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
                PersistentMapClient.Logger.LogError(ex);
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
                PersistentMapClient.Logger.LogError(ex);
                return null;
            }
        }

        public static string GetFactionName(Faction faction, DataManager manager) {
            try {
                return FactionDef.GetFactionDefByEnum(manager, faction).Name.Replace("the ", "").Replace("The ", "");
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return null;
            }
        }

        public static double GetDistanceInLY(StarSystem currPosition, PersistentMapAPI.System target, List<StarSystem> allSystems) {
            try {
                StarSystem targetSystem = allSystems.FirstOrDefault(x => x.Name.Equals(target.name));
                return Math.Sqrt(Math.Pow(targetSystem.Position.x - currPosition.Position.x, 2) + Math.Pow(targetSystem.Position.y - currPosition.Position.y, 2));
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return 0;
            }
        }

        public static double GetDistanceInLY(StarSystem currPosition, StarSystem targetSystem) {
            try {
                return Math.Sqrt(Math.Pow(targetSystem.Position.x - currPosition.Position.x, 2) + Math.Pow(targetSystem.Position.y - currPosition.Position.y, 2));
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return 0;
            }
        }

        public static List<Faction> GetEmployees(StarSystem system, SimGameState Sim) {
            try {
                List<Faction> employees = new List<Faction>();
                if (Sim.Starmap != null) {
                    // If a faction owns the planet, add the owning faction and local government
                    if (system.Owner != Faction.NoFaction) {
                        employees.Add(Faction.Locals);
                        employees.Add(system.Owner);
                    }

                    // Look across neighboring systems, and add employees of factions that border this system
                    List<Faction> distinctNeighbors = Sim.Starmap.GetAvailableNeighborSystem(system)
                        .Select(s => s.Owner)
                        .Where(f => f != Faction.NoFaction && f != system.Owner && f != Faction.Locals)
                        .Distinct()
                        .ToList();
                    employees.AddRange(distinctNeighbors);

                    // If a capital is occupied, add the faction that originally owned the capital to the employer list
                    if (Helper.capitalsBySystemName.Contains(system.Name)) {
                        Faction originalCapitalFaction = Helper.capitalsBySystemName[system.Name].First();
                        if (!employees.Contains(originalCapitalFaction)) {
                            employees.Add(originalCapitalFaction);
                        }
                    }
                }
                return employees;
            }
            catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
                return null;
            }
        }

        public static List<Faction> GetTargets(StarSystem system, SimGameState Sim) {
            try {
                List<Faction> targets = new List<Faction>();
                if (Sim.Starmap != null) {
                    targets.Add(Faction.AuriganPirates);
                    if (system.Owner != Faction.NoFaction) {
                        targets.Add(system.Owner);
                        targets.Add(Faction.Locals);
                    }
                    foreach (StarSystem neigbourSystem in Sim.Starmap.GetAvailableNeighborSystem(system)) {
                        if (system.Owner != neigbourSystem.Owner && !targets.Contains(neigbourSystem.Owner) && neigbourSystem.Owner != Faction.NoFaction) {
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
                PersistentMapClient.Logger.LogError(ex);
                return null;
            }
        }

        // Capitals by faction
        private static Dictionary<Faction, string> capitalsByFaction = new Dictionary<Faction, string> {
            { Faction.Kurita, "Luthien" },
            { Faction.Davion, "New Avalon" },
            { Faction.Liao, "Sian" },
            { Faction.Marik, "Atreus (FWL)" },
            { Faction.Rasalhague, "Rasalhague" },
            { Faction.Ives, "St. Ives" },
            { Faction.Oberon, "Oberon" },
            { Faction.TaurianConcordat, "Taurus" },
            { Faction.MagistracyOfCanopus, "Canopus" },
            { Faction.Outworld, "Alpheratz" },
            { Faction.Circinus, "Circinus" },
            { Faction.Marian, "Alphard (MH)" },
            { Faction.Lothian, "Lothario" },
            { Faction.AuriganRestoration, "Coromodir" },
            { Faction.Steiner, "Tharkad" },
            { Faction.ComStar, "Terra" },
            { Faction.Castile, "Asturias" },
            { Faction.Chainelane, "Far Reach" },
            { Faction.ClanBurrock, "Albion (Clan)" },
            { Faction.ClanCloudCobra, " Zara(Homer 2850 +)" },
            { Faction.ClanCoyote, "Tamaron" },
            { Faction.ClanDiamondShark, "Strato Domingo" },
            { Faction.ClanFireMandrill, "Shadow" },
            { Faction.ClanGhostBear, "Arcadia (Clan)" },
            { Faction.ClanGoliathScorpion, "Dagda (Clan)" },
            { Faction.ClanHellsHorses, "Kirin" },
            { Faction.ClanIceHellion, "Hector" },
            { Faction.ClanJadeFalcon, "Ironhold" },
            { Faction.ClanNovaCat, "Barcella" },
            { Faction.ClansGeneric, "Strana Mechty" },
            { Faction.ClanSmokeJaguar, "Huntress" },
            { Faction.ClanSnowRaven, "Lum" },
            { Faction.ClanStarAdder, "Sheridan (Clan)" },
            { Faction.ClanSteelViper, "New Kent" },
            { Faction.ClanWolf, "Tiber (Clan)" },
            { Faction.Delphi, "New Delphi" },
            { Faction.Elysia, "Blackbone (Nyserta 3025+)" },
            { Faction.Hanse, "Bremen (HL)" },
            { Faction.JarnFolk, "Trondheim (JF)" },
            { Faction.Tortuga, "Tortuga Prime" },
            { Faction.Valkyrate, "Gotterdammerung" },
            { Faction.Axumite, "Thala" },
        };
        private static ILookup<string, Faction> capitalsBySystemName = capitalsByFaction.ToLookup(pair => pair.Value, pair => pair.Key);
        public static bool IsCapital(StarSystem system, Faction faction) {
            bool isCapital = false;
            try {                
                if (capitalsBySystemName.Contains(system.Name)) {
                    Faction systemFaction = capitalsBySystemName[system.Name].First();
                    isCapital = (systemFaction == faction);
                }                
            } catch (Exception ex) {
                PersistentMapClient.Logger.LogError(ex);
            }
            return isCapital;
        }

        public static int CalculatePlanetSupport(SimGameState Sim, StarSystem attackSystem, Faction attacker, Faction defender) {
            int support = 0;
            PersistentMapClient.Logger.Log("Calculating planet support");
            List<StarSystem> neighbours = new List<StarSystem>();
            foreach (StarSystem possibleSystem in Sim.StarSystems) {
                if (GetDistanceInLY(attackSystem, possibleSystem) <= Sim.Constants.Travel.MaxJumpDistance && !possibleSystem.Name.Equals(attackSystem.Name)) {
                    neighbours.Add(possibleSystem);
                }
            }
            if (attackSystem.Owner == attacker) {
                if (IsCapital(attackSystem, attacker)) {
                    support += 10;
                }
                else {
                    support++;
                }
            }
            else if (attackSystem.Owner == defender) {
                if (IsCapital(attackSystem, defender)) {
                    support -= 10;
                }
                else {
                    support--;
                }
            }

            foreach (StarSystem neigbourSystem in neighbours) {
                if (neigbourSystem.Owner == attacker) {
                    if (IsCapital(neigbourSystem, attacker)) {
                        support += 10;
                    }
                    else {
                        support++;
                    }
                }
                else if (neigbourSystem.Owner == defender) {
                    if (IsCapital(neigbourSystem, defender)) {
                        support -= 10;
                    }
                    else {
                        support--;
                    }
                }
            }
            return support;
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
                    foreach (Contract_MDD contract_MDD in metadataDatabase.GetContractsByDifficultyRangeAndScopeAndOwnership(Difficulty - 1, Difficulty + 1, Sim.ContractScope, true)) {
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
                    PersistentMapClient.Logger.Log("Maps0 break");
                    break;
                }
                if (potentialOverrides.Count == 0) {
                    PersistentMapClient.Logger.Log("Overrides0 break");
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
                        PersistentMapClient.Logger.Log(string.Format("[CONTRACT] Unable to find any valid contracts for available map pool. Alert designers.", new object[0]));
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
                PersistentMapClient.Logger.Log("Unable to fill contract list. Please inform AJ Immediately");
            }
            return contractList[0];
        }
    }
}

