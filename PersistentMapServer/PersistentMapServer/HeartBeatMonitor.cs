using PersistentMapAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace PersistentMapServer {
    class HeartBeatMonitor {

        private const string CategoryName_ServiceModelService = "ServiceModelService 4.0.0.0";

        private static string InstanceName_WarServices = "WarServices@" + Program.ServiceUrl.Replace("/", "|");

        private static TimeSpan reportingTimeSpan = TimeSpan.FromSeconds(5);
 
        private static DateTime lastReportedTime = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));

        private static PerformanceCounter pc_calls = null;
        private static PerformanceCounter pc_callsOutstanding = null;
        private static PerformanceCounter pc_callsFaulted = null;
        private static PerformanceCounter pc_callsFailed = null;

        public static void DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker bw = sender as BackgroundWorker;

            initializePerformanceCounters();

            while (!bw.CancellationPending) {

                // If more than reportTimeSpan was passed since we last logged values, log them
                DateTime now = DateTime.UtcNow;
                if (now.Subtract(reportingTimeSpan) > lastReportedTime) {
                    Logger.LogLine("Reporting values!");
                    Logger.LogLine($"Calls - Total: ({pc_calls.NextValue()}) Outstanding: ({pc_callsOutstanding.NextValue()}) Faulted: ({pc_callsFaulted.NextValue()}) Failed: ({pc_callsFailed.NextValue()})");
                    lastReportedTime = now;
                }

                // Sleep 50ms then check to see if we are cancelled
                Thread.Sleep(50);
            }                        
        }

        private static void initializePerformanceCounters() {
            
            HeartBeatMonitor.pc_calls =
                new PerformanceCounter(CategoryName_ServiceModelService, "calls", InstanceName_WarServices);
            HeartBeatMonitor.pc_callsOutstanding =
                new PerformanceCounter(CategoryName_ServiceModelService, "calls outstanding", InstanceName_WarServices);
            HeartBeatMonitor.pc_callsFaulted =
                new PerformanceCounter(CategoryName_ServiceModelService, "calls faulted", InstanceName_WarServices);
            HeartBeatMonitor.pc_callsFailed =
                new PerformanceCounter(CategoryName_ServiceModelService, "calls failed", InstanceName_WarServices);
        }

        private void printCategoryNames() {
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
