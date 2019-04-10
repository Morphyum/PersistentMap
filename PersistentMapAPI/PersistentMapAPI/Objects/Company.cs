using BattleTech;

namespace PersistentMapAPI.Objects {
    public class Company {
        string name;
        Faction faction;

        public string Name {
            get {
                return name;
            }

            set {
                name = value;
            }
        }

        public Faction Faction {
            get {
                return faction;
            }

            set {
                faction = value;
            }
        }
    }
}
