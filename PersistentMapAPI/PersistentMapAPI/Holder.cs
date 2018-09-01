using System;
using System.Collections.Generic;

namespace PersistentMapAPI {
    public static class Holder {
        public static StarMap currentMap;
        public static Dictionary<string, UserInfo> connectionStore = new Dictionary<string, UserInfo>();
        public static List<HistoryResult> resultHistory = new List<HistoryResult>();
    }
}
