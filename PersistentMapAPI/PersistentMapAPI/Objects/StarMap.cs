using System.Collections.Generic;
using System.Linq;

namespace PersistentMapAPI {
    public class StarMap {

        public List<System> systems = new List<System>();

        public bool hasBeenFixated = false;

        public System FindSystemByName(string name) {
            System result = null;
            if (systems != null && systems.Count > 0) {
                result = systems.FirstOrDefault(x => x.name.Equals(name));
            }
            return result;
        }

        // Call to fixate the object graph, activePlayers and companies will no longer be dynamically calculated after this call is performed
        public StarMap fixate () {
            Settings settingsForFixation = Helper.LoadSettings();
            systems.ForEach(x => { x.fixate(settingsForFixation); });
            this.hasBeenFixated = true;
            return this;
        }
    }
}
