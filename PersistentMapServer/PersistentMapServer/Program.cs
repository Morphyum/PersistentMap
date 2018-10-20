using System;
using System.ServiceModel.Web;
using PersistentMapAPI;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using PersistentMapServer.Behavior;

namespace PersistentMapServer {
    class Program {
        static void Main(string[] args) {
            try {
                SettingsFileMonitor monitor = new SettingsFileMonitor();
                monitor.enable();


                WarServices warServices = new WarServices();

                WebServiceHost _serviceHost = new WebServiceHost(warServices, new Uri("http://localhost:8001/warServices"));

                ServiceThrottlingBehavior throttlingBehavior = new ServiceThrottlingBehavior();
                throttlingBehavior.MaxConcurrentSessions = 9999;
                throttlingBehavior.MaxConcurrentCalls = 9999;
                throttlingBehavior.MaxConcurrentInstances = 9999;
                _serviceHost.Description.Behaviors.Add(throttlingBehavior);

                RequestLoggingBehavior loggingBehavior = new RequestLoggingBehavior();
                _serviceHost.Description.Behaviors.Add(loggingBehavior);

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
                Console.WriteLine("Monitor Disabled");
                Console.ReadKey();
            } catch (Exception e) {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }
    }
}
