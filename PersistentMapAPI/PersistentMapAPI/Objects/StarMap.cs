using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistentMapAPI {

    public class StarMap : ICloneable {

        public List<System> systems = new List<System>();

        [NonSerialized]
        private bool fixated = false;

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
            this.fixated = true;
            return this;
        }

        public object Clone() {
            return this.MemberwiseClone();
        }

        public bool hasBeenFixated() {
            return this.fixated;
        }

    }
}
