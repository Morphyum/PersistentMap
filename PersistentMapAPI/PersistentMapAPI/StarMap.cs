using System.Collections.Generic;

namespace PersistentMapAPI {
    public class StarMap {
        public List<System> systems = new List<System>();


        public System FindSystemByName(string name) {
            if(systems == null) {
                systems = new List<System>();
            }
            System result = systems.Find(x => x.name.Equals(name));
            if(result == null) {
                result = new System();
                result.name = name;
                result.controlList = new List<FactionControl>();
                systems.Add(result);
            }
            return result;
        }
    }
}
