using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistentMapAPI {
    public class System {

        public List<FactionControl> controlList;

        public string name;

        // Settings used when the object was fixated
        private Settings settingsForFixation = null;

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

        // Call to fixate the object graph, activePlayers and companies will no longer be dynamically calculated after this call is performed
        public void fixate(Settings settings) {
            this.settingsForFixation = settings;
        }

        private List<string> GetCompanies() {
            Settings settings = this.settingsForFixation != null ? this.settingsForFixation : Helper.LoadSettings();

            List<string> companies = new List<string>();
            Dictionary<string, UserInfo> activeConnections = Holder.connectionStore.Where(x => x.Value.LastDataSend.AddMinutes(settings.MinutesForActive) > DateTime.UtcNow).ToDictionary(p => p.Key, p => p.Value);
            foreach (KeyValuePair<string, UserInfo> info in activeConnections) {
                if (info.Value.lastSystemFoughtAt.Equals(this.name)) {
                    companies.Add(info.Value.companyName);
                }
            }
            return companies;
        }

        private int GetActivePlayers() {
            Settings settings = this.settingsForFixation != null ? this.settingsForFixation : Helper.LoadSettings();

            int players = 0;
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
