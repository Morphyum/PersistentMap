using System.Collections.Generic;

namespace PersistentMapServer.Objects {

    //[JsonObject("StarMap")]
    public class PlayerDecoratedStarMap : PersistentMapAPI.StarMap {

        public List<PlayerDecoratedSystem> systems123 = new List<PlayerDecoratedSystem>();
    }
}
