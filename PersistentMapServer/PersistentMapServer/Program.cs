using System;
using System.ServiceModel.Web;
using PersistentMapAPI;
using System.ServiceModel.Description;
using System.ComponentModel;

namespace PersistentMapServer {
    class Program {

        public static string ServiceUrl = "http://localhost:8001/warServices";

        static void Main(string[] args) {
            try {
                // Start a heart-beat monitor to check the server status
                BackgroundWorker heartbeatWorker = new BackgroundWorker();
                heartbeatWorker.WorkerSupportsCancellation = true;
                heartbeatWorker.DoWork += new DoWorkEventHandler(HeartBeatMonitor.DoWork);
                heartbeatWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(HeartBeatMonitor.RunWorkerCompleted);
                heartbeatWorker.RunWorkerAsync();

                SettingsFileMonitor monitor = new SettingsFileMonitor();
                monitor.enable();

                ServiceThrottlingBehavior behaviour = new ServiceThrottlingBehavior();
                behaviour.MaxConcurrentSessions = 9999;
                behaviour.MaxConcurrentCalls = 9999;
                behaviour.MaxConcurrentInstances = 9999;

                WarServices warServices = new WarServices();

                WebServiceHost _serviceHost = new WebServiceHost(warServices, new Uri(ServiceUrl));
                _serviceHost.Description.Behaviors.Add(behaviour);
                _serviceHost.Open();

                Console.WriteLine("Open Press Key to close");
                Console.ReadKey();

                _serviceHost.Close();
                Console.WriteLine("Connection Closed");

                Helper.SaveCurrentMap(Helper.LoadCurrentMap());
                Console.WriteLine("Map Saved");

                Helper.SaveCurrentInventories(Helper.LoadCurrentInventories());
                Console.WriteLine("Shops Saved");

                monitor.disable();
                Console.WriteLine("Monitor disabled");
                heartbeatWorker.CancelAsync();

                Console.WriteLine("HeartBeatMonitor cancelled");
                Console.ReadKey();
            } catch (Exception e) {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }
    }
}
