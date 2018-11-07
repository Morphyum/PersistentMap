using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistentMapAPI.Objects {

    public class PlayerHistory {
        public string Id;
        public DateTime lastActive;
        public HashSet<CompanyActivity> activities = new HashSet<CompanyActivity>();

        public List<string> CompanyNames() {
            return activities.Select(a => a.companyName).Distinct().ToList();
        }
    }

    public class CompanyActivity {
        public string companyName;
        public Faction employer;
        public Faction target;
        public BattleTech.MissionResult result;
        public string systemId;
        public DateTime resultTime;
    }

}
