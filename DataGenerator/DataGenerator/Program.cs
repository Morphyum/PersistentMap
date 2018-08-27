using BattleTech;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataGenerator {
    class Program {
        public static string systemDataFilePath = $"../../../../PersistentMapServer/PersistentMapServer/bin/StarSystems";
        public static string factionDataFilePath = $"../../../../PersistentMapServer/PersistentMapServer/bin/Factions";
        public static string systemsFilePath = $"../Data/systems.json";
        public static string factionsFilePath = $"../Data/factions.json";

        static void Main(string[] args) {
            List<JSONSystem> systems = new List<JSONSystem>();
            List<JSONFaction> factions = new List<JSONFaction>();

            foreach (string filePaths in Directory.GetFiles(systemDataFilePath)) {
                string originalJson = File.ReadAllText(filePaths);
                JObject originalJObject = JObject.Parse(originalJson);
                JSONSystem system = new JSONSystem();
                system.name = (string)originalJObject["Description"]["Name"];
                system.x = (int)originalJObject["Position"]["x"];
                system.y = (int)originalJObject["Position"]["y"];
                systems.Add(system);
            }
            foreach(string filePaths in Directory.GetFiles(factionDataFilePath)) {
                string originalJson = File.ReadAllText(filePaths);
                JObject originalJObject = JObject.Parse(originalJson);
                JSONFaction faction = new JSONFaction();
                faction.name = (string)originalJObject["Name"];
                faction.id = (int)Enum.Parse(typeof(Faction), (string)originalJObject["Faction"]);
                factions.Add(faction);
            }

            (new FileInfo(systemsFilePath)).Directory.Create();
            using (StreamWriter writer = new StreamWriter(systemsFilePath, false)) {
                string json = JsonConvert.SerializeObject(systems);
                writer.Write(json);
            }
            (new FileInfo(factionsFilePath)).Directory.Create();
            using (StreamWriter writer = new StreamWriter(factionsFilePath, false)) {
                string json = JsonConvert.SerializeObject(factions);
                writer.Write(json);
            }
        }
    }
}
