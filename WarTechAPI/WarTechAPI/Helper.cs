using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarTechAPI {
    public static class Helper {

        public static StarMap LoadCurrentMap() {
            if (Holder.currentMap == null) {
                //TODO: Load from existing data
                Holder.currentMap = new StarMap();
            }

            StarMap result = Holder.currentMap;
            Console.WriteLine("Map Loaded");
            return result;
        }

        public static void SaveCurrentMap() {
            //TODO: Save new data
        }

        public static Settings LoadSettings() {
            //TODO: Load from existing data
            Console.WriteLine("Settings Loaded");
            return new Settings();
        }
    }
}
