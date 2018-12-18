using Harmony;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace PersistentMapClient {

    public class PersistentMapClient {
        internal static string ModDirectory;
        public static void Init(string directory, string settingsJSON) {
            ModDirectory = directory;
            try {
                Fields.settings = JsonConvert.DeserializeObject<Settings>(settingsJSON);
            } catch (Exception e) {
                Fields.settings = new Settings();
                Logger.LogLine("Failed to read settingsJSON from modtek, loaded defaults.");
                Logger.LogError(e);
            }

            /* Read the ClientID from a location that is persistent across installs. 
               Everything under /mods is wiped out during RT installs. Instead we write 
               to Battletech/ModSaves/PersistentMapClient to allow it to persist across installs */
            if (Fields.settings.ClientID == null || Fields.settings.ClientID.Equals("")) {
                Helper.FetchClientID(directory);
            } else {
                // We were passed an ID by the test harness. Do nothing.
            }
            
            var harmony = HarmonyInstance.Create("de.morphyum.PersistentMapClient");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
