using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistentMapAPI {
    public class System {
        public List<FactionControl> controlList;
        public string name;
        public int activePlayers {
            get{
                return GetActivePlayers();
            }
            set { }
        }
        public List<string> companies {
            get {
                return GetCompanies();
            }
            set { }
        }

        private List<string> GetCompanies() {
            List<string> companies = new List<string>();
            Settings settings = Helper.LoadSettings();
            Dictionary<string, UserInfo> activeConnections = Holder.connectionStore.Where(x => x.Value.LastDataSend.AddMinutes(settings.MinutesForActive) > DateTime.UtcNow).ToDictionary(p => p.Key, p => p.Value);
            foreach (KeyValuePair<string, UserInfo> info in activeConnections) {
                if (info.Value.lastSystemFoughtAt.Equals(this.name)) {
                    companies.Add(info.Value.companyName);
                }
            }
            return companies;
        }

        private int GetActivePlayers() {
            int players = 0;
            Settings settings = Helper.LoadSettings();
            Dictionary<string, UserInfo> activeConnections = Holder.connectionStore.Where(x => x.Value.LastDataSend.AddMinutes(settings.MinutesForActive) > DateTime.UtcNow).ToDictionary(p => p.Key, p => p.Value);
            foreach (KeyValuePair<string, UserInfo> info in activeConnections) {
                if (info.Value.lastSystemFoughtAt.Equals(this.name)) {
                    players++;
                }
            }
            return players;
        }

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

        public FactionControl FindHighestControl() {
            if (controlList == null) {
                controlList = new List<FactionControl>();
            }
            FactionControl result = controlList.OrderByDescending(x => x.percentage).First();
            return result;
        }
    }
}
