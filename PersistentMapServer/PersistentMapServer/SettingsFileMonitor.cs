using PersistentMapAPI;
using System.IO;

namespace PersistentMapServer {

    class SettingsFileMonitor {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private FileSystemWatcher fileSystemWatcher;

        public SettingsFileMonitor() {
            this.fileSystemWatcher = new FileSystemWatcher();
            string settingsFilePath = Helper.settingsFilePath.Substring(0, Helper.settingsFilePath.IndexOf("settings.json"));
            this.fileSystemWatcher.Path = settingsFilePath;
            this.fileSystemWatcher.Changed += this.SettingsFileChange;
            this.fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
            this.fileSystemWatcher.Filter = "settings.json";
        }

        public void enable() {
            logger.Trace("Settings monitoring enabled.");
            this.fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void disable() {
            logger.Trace("Settings monitoring disabled.");
            this.fileSystemWatcher.EnableRaisingEvents = false;
        }

        // For some reason, this fires twice. Can't track down why, but it does.
        private void SettingsFileChange(object sender, FileSystemEventArgs e) {
            logger.Debug("Settings file changed, refreshing settings from disk.");
            Helper.LoadSettings(true);
            return;
        }
    }
}
