using Harmony;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace PersistentMapClient {

    public class PersistentMapClient {

        internal static Logger Logger;
        internal static string ModDirectory;

        public static void Init(string directory, string settingsJSON) {
            ModDirectory = directory;

            Exception settingsE = null;
            try {
                Fields.settings = JsonConvert.DeserializeObject<Settings>(settingsJSON);
            } catch (Exception e) {
                settingsE = e;
                Fields.settings = new Settings();
            }
            Logger = new Logger(directory, "persistent_map_client", Fields.settings.debug);

            if (settingsE != null) {
                Logger.Log($"Using default settings due to exception reading modTek settings: {settingsE.Message}");
            }

            
            Logger.LogIfDebug($"Mod settings are:({settingsJSON})");

            /* Read the ClientID from a location that is persistent across installs. 
               Everything under /mods is wiped out during RT installs. Instead we write 
               to Battletech/ModSaves/PersistentMapClient to allow it to persist across installs */
            if (Fields.settings.ClientID == null || Fields.settings.ClientID.Equals("")) {
                Helper.FetchClientID(directory);
            } else {
                // We were passed an ID by the test harness. Do nothing.
                Logger.Log("Test harness passed a clientID, skipping.");
            }
            
            var harmony = HarmonyInstance.Create("de.morphyum.PersistentMapClient");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
