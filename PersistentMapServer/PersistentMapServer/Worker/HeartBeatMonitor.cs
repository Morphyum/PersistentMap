using PersistentMapAPI.Objects;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace PersistentMapServer.Worker {
    /* Reads various performanceCounters that indicate server health, and writes them to the Console */
    class HeartBeatMonitor {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private const string CategoryName_ServiceModelService = "ServiceModelService 4.0.0.0";

        // TODO: Pull 'WarServicesProxy' from the instance, instead of hard-coding it like this.
        private static string InstanceName_WarServices = "WarServicesProxy@" + Program.ServiceUrl.Replace("/", "|");

        private static TimeSpan reportingTimeSpan = TimeSpan.FromSeconds(15);

        private static DateTime lastReportedTime = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));

        private static PerformanceCounter pc_sms_calls = null;
        private static PerformanceCounter pc_sms_callsOutstanding = null;
        private static PerformanceCounter pc_sms_callsFaulted = null;
        private static PerformanceCounter pc_sms_callsFailed = null;
        private static PerformanceCounter pc_sms_pctMaxCalls = null;

        public static void DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker bw = sender as BackgroundWorker;

            initializePerformanceCounters();

            while (!bw.CancellationPending) {

                // If more than reportTimeSpan was passed since we last logged values, log them
                DateTime now = DateTime.UtcNow;
                if (now.Subtract(reportingTimeSpan) > lastReportedTime) {
                    // Report WCF connection status
                    logger.Debug($"Connections-> Total:({pc_sms_calls.NextValue()}) Outstanding:({pc_sms_callsOutstanding.NextValue()}) " +
                        $"Faulted:({pc_sms_callsFaulted.NextValue()}) Failed:({pc_sms_callsFailed.NextValue()}) " +
                        $" Max% ({pc_sms_pctMaxCalls.NextValue()})%"
                        );

                    ServiceDataSnapshot snapshot = new ServiceDataSnapshot();
                    // Report internal data sizes
                    logger.Debug($"Users-> active({snapshot.num_connections_active}) inactive:({snapshot.num_connections_inactive}) percent active:({snapshot.percent_connections_active})");
                    logger.Debug($"ResultsHistory-> total objects:({snapshot.num_results}) before_inactivity:({snapshot.num_results_past_inactive_time})");
                    string json_inventory_size = fastJSON.JSON.ToJSON(snapshot.faction_inventory_size);
                    string json_faction_shops = fastJSON.JSON.ToJSON(snapshot.faction_shop_size);
                    logger.Debug($"Faction inventory: {json_inventory_size}");
                    logger.Debug($"Faction shop size: {json_faction_shops}");

                    lastReportedTime = now;
                }

                // Sleep a short period to see if we are cancelled.
                Thread.Sleep(50);
            }                        
        }

        private static void initializePerformanceCounters() {
            HeartBeatMonitor.pc_sms_calls =
                new PerformanceCounter(CategoryName_ServiceModelService, "calls", InstanceName_WarServices);
            HeartBeatMonitor.pc_sms_callsOutstanding =
                new PerformanceCounter(CategoryName_ServiceModelService, "calls outstanding", InstanceName_WarServices);
            HeartBeatMonitor.pc_sms_callsFaulted =
                new PerformanceCounter(CategoryName_ServiceModelService, "calls faulted", InstanceName_WarServices);
            HeartBeatMonitor.pc_sms_callsFailed =
                new PerformanceCounter(CategoryName_ServiceModelService, "calls failed", InstanceName_WarServices);
            HeartBeatMonitor.pc_sms_pctMaxCalls =
                new PerformanceCounter(CategoryName_ServiceModelService, "percent of max concurrent sessions", InstanceName_WarServices);
        }
    
        public static void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            logger.Debug("Shutting down HeartBeatMonitor");
        }
    }
}
