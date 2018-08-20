using Newtonsoft.Json;
using System;
using System.IO;

namespace PersistentMapAPI {
    public static class Helper {
        public static string currentMapFilePath = $"../Map/current.json";
        public static string settingsFilePath = $"../Settings/settings.json";

        public static StarMap initializeNewMap() {
            //TODO: Load all Data from Preset
            Console.WriteLine("Map Initialized");
            return new StarMap();
        }

        public static StarMap LoadCurrentMap() {
            if (Holder.currentMap == null) {
                if (File.Exists(currentMapFilePath)) {
                    using (StreamReader r = new StreamReader(currentMapFilePath)) {
                        string json = r.ReadToEnd();
                        Holder.currentMap = JsonConvert.DeserializeObject<StarMap>(json);
                    }
                }
                else {
                    Holder.currentMap = initializeNewMap();
                }
            }
            StarMap result = Holder.currentMap;
            Console.WriteLine("Map Loaded");
            return result;
        }

        public static void SaveCurrentMap(StarMap map) {
            (new FileInfo(currentMapFilePath)).Directory.Create();
            using (StreamWriter writer = new StreamWriter(currentMapFilePath, false)) {
                string json = JsonConvert.SerializeObject(map);
                writer.Write(json);
            }
            Console.WriteLine("Map Saved");
        }

        public static Settings LoadSettings() {
            Settings settings;
            if (File.Exists(settingsFilePath)) {
                using (StreamReader r = new StreamReader(settingsFilePath)) {
                    string json = r.ReadToEnd();
                    settings = JsonConvert.DeserializeObject<Settings>(json);
                }
            }
            else {
                settings = new Settings();
                (new FileInfo(settingsFilePath)).Directory.Create();
                using (StreamWriter writer = new StreamWriter(settingsFilePath, false)) {
                    string json = JsonConvert.SerializeObject(settings);
                    writer.Write(json);
                }
            }
            Console.WriteLine("Settings Loaded");
            return settings;
        }
    }
}
