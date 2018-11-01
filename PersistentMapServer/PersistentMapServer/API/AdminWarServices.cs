using PersistentMapAPI.Objects;
using PersistentMapServer.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistentMapAPI.API {

    // Container to hold deprecated methods and keep them out of the main service class.
    public abstract class AdminWarServices : DeprecatedWarServices {

        // Helper method to return data on current data sizes. Intended to help determine if some objects are growing out of bounds.
        [AdminKeyRequired]
        public override ServiceDataSnapshot GetServiceDataSnapshot() {
            ServiceDataSnapshot snapshot = new ServiceDataSnapshot();
            return snapshot;
        }

        [AdminKeyRequired]
        public override Dictionary<string, UserInfo> GetConnections() {
            Settings settings = Helper.LoadSettings();
            Dictionary<string, UserInfo> clone = new Dictionary<string, UserInfo>(Holder.connectionStore);
            DateTime isActiveOnOrAfter = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(settings.MinutesForActive));
            Dictionary<string, UserInfo> filtered = clone.Where(x => x.Value.LastDataSend >= isActiveOnOrAfter).ToDictionary(p => p.Key, p => p.Value);
            return filtered;                        
        }

        // Admin helper that loads some necessary test data
        [AdminKeyRequired]
        public override void LoadTestData() {

            // TODO: Remove after testing
            List<UserInfo> randos = Helper.GenerateFakeActivity();
            foreach (UserInfo rando in randos) {
                Holder.connectionStore.Add(rando.companyName, rando);
            }
        }

        

    }
}
