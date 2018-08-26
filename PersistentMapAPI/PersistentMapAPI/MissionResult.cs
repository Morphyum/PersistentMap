using BattleTech;

namespace PersistentMapAPI {
    public class MissionResult {
        public Faction employer;
        public Faction target;
        public BattleTech.MissionResult result;
        public string systemName;
        public int difficulty;
    }
}
