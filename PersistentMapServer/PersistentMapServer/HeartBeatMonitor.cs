using PersistentMapAPI;
using PersistentMapAPI.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace PersistentMapServer {
    /* Reads various performanceCounters that indicate server health, and writes them to the Console */
    class HeartBeatMonitor {

        private const string CategoryName_ServiceModelService = "ServiceModelService 4.0.0.0";

        private static string InstanceName_WarServices = "WarServices@" + Program.ServiceUrl.Replace("/", "|");

        private static TimeSpan reportingTimeSpan = TimeSpan.FromSeconds(5);

        private static DateTime lastReportedTime = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));

        private static PerformanceCounter pc_sms_calls = null;
        private static PerformanceCounter pc_sms_callsOutstanding = null;
        private static PerformanceCounter pc_sms_callsFaulted = null;
        private static PerformanceCounter pc_sms_callsFailed = null;
        private static PerformanceCounter pc_sms_pctMaxSessions = null;
        private static PerformanceCounter pc_sms_pctMaxInstances = null;
        private static PerformanceCounter pc_sms_pctMaxCalls = null;

        public static void DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker bw = sender as BackgroundWorker;

            initializePerformanceCounters();

            while (!bw.CancellationPending) {

                // If more than reportTimeSpan was passed since we last logged values, log them
                DateTime now = DateTime.UtcNow;
                if (now.Subtract(reportingTimeSpan) > lastReportedTime) {
                    Logger.Debug("  --- HeartBeat ---");
                    Logger.Debug($"  Calls - Total:({pc_sms_calls.NextValue()}) Outstanding:({pc_sms_callsOutstanding.NextValue()}) " + 
                        $"Faulted:({pc_sms_callsFaulted.NextValue()}) Failed:({pc_sms_callsFailed.NextValue()}) " +
                        $" Max% ({pc_sms_pctMaxCalls.NextValue()})%"
                        );
                    ServiceDataSnapshot snapshot = new ServiceDataSnapshot();
                    Logger.Debug($"{snapshot.ToString()}");
                    
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
    
        private static void printCategoryNames() {
            PerformanceCounterCategory[] perfCategories = PerformanceCounterCategory.GetCategories();
            List<PerformanceCounterCategory> categories = new List<PerformanceCounterCategory>(perfCategories);
            var categoryNames = categories.ConvertAll(new Converter<PerformanceCounterCategory, String>(CategoryToNameConverter));
            categoryNames.Sort();
            foreach (String cname in categoryNames) {
                Logger.LogLine($"Found category {cname}");
            }
        }

        public static string CategoryToNameConverter(PerformanceCounterCategory category) {
            return category.CategoryName;
        }

        public static void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            Logger.LogLine("Work completed...");
        }
    }
}
