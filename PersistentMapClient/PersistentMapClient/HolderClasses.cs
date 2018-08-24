using BattleTech;
using BattleTech.Framework;
using System.Collections.Generic;

namespace PersistentMapClient {
    public class Settings {
        public string ServerURL = "http://localhost:8000/";
        public bool debug = false;
        public float priorityContactPayPercentage = 2f;
    }

    public static class Fields {
        public static Settings settings;
        public static Dictionary<string, string> FluffDescriptions = new Dictionary<string, string>();
    }

    public struct PotentialContract {
        public ContractOverride contractOverride;
        public Faction employer;
        public Faction target;
        public int difficulty;
    }
}