using PersistentMapAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistentMapServer.Objects {

    //[JsonObject("System")]
    public class PlayerDecoratedSystem : PersistentMapAPI.System {

        public int activePlayers {
            get {
                return GetActivePlayers();
            }
            set { }
        }

        private int GetActivePlayers() {
            int players = 0;
            PersistentMapAPI.Settings settings = Helper.LoadSettings();
            Dictionary<string, UserInfo> activeConnections = Holder.connectionStore
                .Where(x => x.Value.LastDataSend.AddMinutes(settings.MinutesForActive) > DateTime.UtcNow)
                .ToDictionary(p => p.Key, p => p.Value);

            foreach (KeyValuePair<string, UserInfo> info in activeConnections) {
                if (info.Value.lastSystemFoughtAt.Equals(this.name)) {
                    players++;
                }
            }
            return players;
        }

        public List<string> companies {
            get {
                return GetCompanies();
            }
            set { }
        }

        private List<string> GetCompanies() {
            List<string> companies = new List<string>();
            PersistentMapAPI.Settings settings = Helper.LoadSettings();
            Dictionary<string, UserInfo> activeConnections = Holder.connectionStore
                .Where(x => x.Value.LastDataSend.AddMinutes(settings.MinutesForActive) > DateTime.UtcNow)
                .ToDictionary(p => p.Key, p => p.Value);

            foreach (KeyValuePair<string, UserInfo> info in activeConnections) {
                if (info.Value.lastSystemFoughtAt.Equals(this.name)) {
                    companies.Add(info.Value.companyName);
                }
            }
            return companies;
        }


    }
}
