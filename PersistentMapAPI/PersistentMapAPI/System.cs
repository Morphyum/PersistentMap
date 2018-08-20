using BattleTech;
using System.Collections.Generic;

namespace PersistentMapAPI {
    public class System {
        Faction owner;
        public List<FactionControl> controlList;
        public string name;


        public FactionControl FindFactionControlByFaction(Faction faction) {
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
    }
}
