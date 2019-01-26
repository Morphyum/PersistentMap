using BattleTech;
using System.Collections.Generic;
using System.Linq;

namespace PersistentMapAPI {
    public class System {
        public InvasionState invasionsState;
        //public List<FactionControl> controlList;
        public string name;
        public int activePlayers;
        public List<string> companies;

       /* public FactionControl FindFactionControlByFaction(Faction faction) {
            if(controlList == null) {
                controlList = new List<FactionControl>();
            }
            FactionControl result = controlList.Find(x => x.faction == faction);
            if(result == null) {
                result = new FactionControl();
                result.faction = faction;
                result.percentage = 0;
                controlList.Add(result);
            }
            return result;
        }

        public FactionControl FindHighestControl() {
            if (controlList == null) {
                controlList = new List<FactionControl>();
            }
            FactionControl result = controlList.OrderByDescending(x => x.percentage).First();
            return result;
        }*/
    }
}
