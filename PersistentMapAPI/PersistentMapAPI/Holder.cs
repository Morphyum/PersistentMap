using System;
using System.Collections.Generic;

namespace PersistentMapAPI {
    public static class Holder {
        public static StarMap currentMap;
        public static Dictionary<string, DateTime> connectionStore = new Dictionary<string, DateTime>();
    }
}
