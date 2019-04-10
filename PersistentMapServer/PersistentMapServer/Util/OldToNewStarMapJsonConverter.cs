using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PersistentMapAPI;
using PersistentMapAPI.Objects;
using System;
using System.Collections.Generic;

namespace PersistentMapServer.Util {
    public class OldToNewStarMapJsonConverter : JsonConverter<StarMap> {
        public override bool CanRead => true;

        public override StarMap ReadJson(JsonReader reader, Type objectType, StarMap existingValue, bool hasExistingValue, JsonSerializer serializer) {
            StarMap starmap = new StarMap();
            starmap.systems = new List<PersistentMapAPI.System>();
            JObject jObject = JObject.Load(reader);
            JArray systems = (JArray)jObject["systems"];

            foreach (JToken jtoken in systems) {
                PersistentMapAPI.System system = new PersistentMapAPI.System();
                system.activePlayers = (int)jtoken["activePlayers"];
                system.companies = new List<Company>();
                /*List<string> companyNames = jtoken["companies"].ToObject<List<string>>();
                foreach(string name in companyNames) {
                    Company convertedCompany = new Company();
                    convertedCompany.Name = name;
                    convertedCompany.Faction = BattleTech.Faction.Steiner;
                    system.companies.Add(convertedCompany);
                }*/
                system.controlList = jtoken["controlList"].ToObject<List<FactionControl>>();
                system.name = (string)jtoken["name"];
                starmap.systems.Add(system);
            }

            return starmap;
        }

        public override void WriteJson(JsonWriter writer, StarMap value, JsonSerializer serializer) {
            throw new NotImplementedException();
        }
    }
}
