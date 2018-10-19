using PersistentMapAPI;
using System.IO;

namespace PersistentMapServer {

    class SettingsFileMonitor {

        private FileSystemWatcher fileSystemWatcher;

        public SettingsFileMonitor() {
            this.fileSystemWatcher = new FileSystemWatcher();
            string settingsFilePath = Helper.settingsFilePath.Substring(0, Helper.settingsFilePath.IndexOf("settings.json"));
            //Logger.LogLine("Attempting to monitor: " + System.IO.Directory.GetCurrentDirectory() + settingsFilePath);
            this.fileSystemWatcher.Path = settingsFilePath;
            this.fileSystemWatcher.Changed += this.SettingsFileChange;
            this.fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
            this.fileSystemWatcher.Filter = "settings.json";
        }

        public void enable() {
            this.fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void disable() {
            this.fileSystemWatcher.EnableRaisingEvents = false;
        }

        // For some reason, this fires twice. Can't track down why, but it does.
        private void SettingsFileChange(object sender, FileSystemEventArgs e) {
            Logger.LogLine("Settings file changed, refreshing.");
            Helper.LoadSettings(true);
            return;
        }
    }
}
