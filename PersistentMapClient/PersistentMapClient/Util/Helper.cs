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
                }
                catch (Exception e) {
                    PersistentMapClient.Logger.Log($"Failed to read clientID from {GeneratedSettingsFile}, will overwrite!");
                    PersistentMapClient.Logger.LogError(e);
                }
            }
            else {
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
                }
                catch (Exception e) {
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
                if (!result && capitalsBySystemName.Contains(system.Name) && !IsCapital(system, system.Owner)) {
                    result = true;
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
                        if (system.Owner != Faction.Locals) {
                            employees.Add(system.Owner);
                        }
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
                        if (system.Owner != Faction.Locals) {
                            targets.Add(system.Owner);
                        }
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
            { Faction.ClanCloudCobra, "Zara (Homer 2850+)" },
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
            { Faction.Axumite, "Thala" }
            //,{ Faction.WordOfBlake, "Hope (Randis 2988+)" }
        };

        private static ILookup<string, Faction> capitalsBySystemName = capitalsByFaction.ToLookup(pair => pair.Value, pair => pair.Key);
        public static bool IsCapital(StarSystem system, Faction faction) {
            bool isCapital = false;
            try {
                if (capitalsBySystemName.Contains(system.Name)) {
                    Faction systemFaction = capitalsBySystemName[system.Name].First();
                    isCapital = (systemFaction == faction);
                }
            }
            catch (Exception ex) {
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

        private static readonly ContractType[] prioTypes = new ContractType[]
        {
            ContractType.AmbushConvoy,
            ContractType.Assassinate,
            ContractType.CaptureBase,
            ContractType.CaptureEscort,
            ContractType.DefendBase,
            ContractType.DestroyBase,
            ContractType.Rescue,
            ContractType.SimpleBattle,
            ContractType.FireMission,
            ContractType.AttackDefend,
            ContractType.ThreeWayBattle
        };

        public static Contract GetNewWarContract(SimGameState Sim, int Difficulty, Faction emp, Faction targ, Faction third, StarSystem system) {
            Fields.prioGen = true;
            Fields.prioEmployer = emp;
            Fields.prioTarget = targ;
            Fields.prioThird = third;

            if (Difficulty <= 1) {
                Difficulty = 2;
            }
            else if (Difficulty > 9) {
                Difficulty = 9;
            }

            var difficultyRange = AccessTools.Method(typeof(SimGameState), "GetContractRangeDifficultyRange").Invoke(Sim, new object[] { system, Sim.SimGameMode, Sim.GlobalDifficulty });
            Dictionary<int, List<ContractOverride>> potentialContracts = (Dictionary<int, List<ContractOverride>>)AccessTools.Method(typeof(SimGameState), "GetContractOverrides").Invoke(Sim, new object[] { difficultyRange, prioTypes });
            WeightedList<MapAndEncounters> playableMaps =
                //MetadataDatabase.Instance.GetReleasedMapsAndEncountersByContractTypeAndTagsAndOwnership(potentialContracts.Keys.ToArray<ContractType>(), 
                //      system.Def.MapRequiredTags, system.Def.MapExcludedTags, system.Def.SupportedBiomes).ToWeightedList(WeightedListType.SimpleRandom);
                // TODO: MORPH - please review!
                MetadataDatabase.Instance.GetReleasedMapsAndEncountersBySinglePlayerProceduralContractTypeAndTags(
                    system.Def.MapRequiredTags, system.Def.MapExcludedTags, system.Def.SupportedBiomes, true)
                    .ToWeightedList(WeightedListType.SimpleRandom);
            var validParticipants = AccessTools.Method(typeof(SimGameState), "GetValidParticipants").Invoke(Sim, new object[] { system });
            if (!(bool)AccessTools.Method(typeof(SimGameState), "HasValidMaps").Invoke(Sim, new object[] { system, playableMaps })
                || !(bool)AccessTools.Method(typeof(SimGameState), "HasValidContracts").Invoke(Sim, new object[] { difficultyRange, potentialContracts })
                || !(bool)AccessTools.Method(typeof(SimGameState), "HasValidParticipants").Invoke(Sim, new object[] { system, validParticipants })) {
                return null;
            }
            AccessTools.Method(typeof(SimGameState), "ClearUsedBiomeFromDiscardPile").Invoke(Sim, new object[] { playableMaps });
            IEnumerable<int> mapWeights = from map in playableMaps
                                          select map.Map.Weight;
            WeightedList<MapAndEncounters> activeMaps = new WeightedList<MapAndEncounters>(WeightedListType.WeightedRandom, playableMaps.ToList(), mapWeights.ToList<int>(), 0);
            AccessTools.Method(typeof(SimGameState), "FilterActiveMaps").Invoke(Sim, new object[] { activeMaps, Sim.GlobalContracts });
            activeMaps.Reset(false);
            MapAndEncounters level = activeMaps.GetNext(false);
            var MapEncounterContractData = AccessTools.Method(typeof(SimGameState), "FillMapEncounterContractData").Invoke(Sim, new object[] { system, difficultyRange, potentialContracts, validParticipants, level });
            bool HasContracts = Traverse.Create(MapEncounterContractData).Property("HasContracts").GetValue<bool>();
            while (!HasContracts && activeMaps.ActiveListCount > 0) {
                level = activeMaps.GetNext(false);
                MapEncounterContractData = AccessTools.Method(typeof(SimGameState), "FillMapEncounterContractData").Invoke(Sim, new object[] { system, difficultyRange, potentialContracts, validParticipants, level });
            }
            system.SetCurrentContractFactions(Faction.INVALID_UNSET, Faction.INVALID_UNSET);
            HashSet<int> Contracts = Traverse.Create(MapEncounterContractData).Field("Contracts").GetValue<HashSet<int>>();

            if (MapEncounterContractData == null || Contracts.Count == 0) {
                List<string> mapDiscardPile = Traverse.Create(Sim).Field("mapDiscardPile").GetValue<List<string>>();
                if (mapDiscardPile.Count > 0) {
                    mapDiscardPile.Clear();
                }
                else {
                    PersistentMapClient.Logger.Log(string.Format("[CONTRACT] Unable to find any valid contracts for available map pool. Alert designers.", new object[0]));
                }
            }
            GameContext gameContext = new GameContext(Sim.Context);
            gameContext.SetObject(GameContextObjectTagEnum.TargetStarSystem, system);


            Contract contract = (Contract)AccessTools.Method(typeof(SimGameState), "CreateProceduralContract").Invoke(Sim, new object[] { system, true, level, MapEncounterContractData, gameContext });
            Fields.prioGen = false;
            return contract;
        }
    }
}

