using BattleTech;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Activation;

namespace WarTechAPI {

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class WarServices : IWarServices {
        public StarMap GetStarmap(string id) {
            StarMap map = new StarMap();
            return map;
        }

        public StarMap PostStarmap(string id) {
            StarMap map = new StarMap();
            map.systems = new List<System>();
            map.systems.Add(new System());
            map.systems[0].controlList = new List<FactionControl>();
            map.systems[0].name = id;
            map.systems[0].controlList.Add(new FactionControl());
            map.systems[0].controlList[0].faction = Faction.NoFaction;
            map.systems[0].controlList[0].percentage = 50;
            map.systems[0].controlList.Add(new FactionControl());
            map.systems[0].controlList[1].faction = Faction.Locals;
            map.systems[0].controlList[1].percentage = 25;

            map.systems.Add(new System());
            map.systems[1].controlList = new List<FactionControl>();
            map.systems[1].name = id;
            map.systems[1].controlList.Add(new FactionControl());
            map.systems[1].controlList[0].faction = Faction.NoFaction;
            map.systems[1].controlList[0].percentage = 50;
            map.systems[1].controlList.Add(new FactionControl());
            map.systems[1].controlList[1].faction = Faction.Locals;
            map.systems[1].controlList[1].percentage = 25;

            return map;
        }
    }
}
