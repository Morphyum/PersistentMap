﻿using BattleTech;
using System;

namespace PersistentMapAPI {
    public class UserInfo {
        public DateTime LastDataSend;
        public string lastSystemFoughtAt;
        public string companyName;
        public Faction lastFactionFoughtForInWar = Faction.INVALID_UNSET;
    }
}
