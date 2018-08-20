using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;
using Harmony;
using HBS.Collections;
using Newtonsoft.Json;
using System;
using System.IO;

namespace PersistentMapClient {

    public class SaveFields {
       
    }

    public class Helper {
        public static Settings LoadSettings() {
            try {
                using (StreamReader r = new StreamReader($"{ PersistentMapClient.ModDirectory}/settings.json")) {
                    string json = r.ReadToEnd();
                    return JsonConvert.DeserializeObject<Settings>(json);
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex);
                return null;
            }
        }
    }
}
