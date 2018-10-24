using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistentMapAPI {
    public class StarMap : ICloneable {

        public List<System> systems = new List<System>();

        public System FindSystemByName(string name) {
            System result = null;
            if (systems != null && systems.Count > 0) {
                result = systems.FirstOrDefault(x => x.name.Equals(name));
            }
            return result;
        }

        // Do a deep clone of all members
        public object Clone() {
            return MemberwiseClone();
        }

    }
}
