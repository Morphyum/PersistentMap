using System.Collections.Generic;
using System.Linq;

namespace PersistentMapAPI {
    public class StarMap {
        public List<System> systems = new List<System>();


        public System FindSystemByName(string name) {
            System result = null;
            if (systems != null && systems.Count > 0) {
                result = systems.FirstOrDefault(x => x.name.Equals(name));
            }
            return result;
        }
    }
}
