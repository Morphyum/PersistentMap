using Harmony;
using System.Reflection;

namespace PersistentMapClient {
    public class PersistentMapClient {
        internal static string ModDirectory;
        public static void Init(string directory, string settingsJSON) {
            ModDirectory = directory;
            Fields.settings = Helper.LoadSettings(settingsJSON);
            var harmony = HarmonyInstance.Create("de.morphyum.PersistentMapClient");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
