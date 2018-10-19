using System;
using System.ServiceModel.Web;
using PersistentMapAPI;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace PersistentMapServer {
    class Program {
        static void Main(string[] args) {
            try {
                SettingsFileMonitor monitor = new SettingsFileMonitor();
                monitor.enable();

                ServiceThrottlingBehavior behaviour = new ServiceThrottlingBehavior();
                behaviour.MaxConcurrentSessions = 9999;
                behaviour.MaxConcurrentCalls = 9999;
                behaviour.MaxConcurrentInstances = 9999;

                WarServices warServices = new WarServices();

                WebServiceHost _serviceHost = new WebServiceHost(warServices, new Uri("http://localhost:8001/warServices"));
                _serviceHost.Description.Behaviors.Add(behaviour);
                _serviceHost.Open();

                Console.WriteLine("Open Press Key to close");
                Console.ReadKey();

                _serviceHost.Close();
                Console.WriteLine("Connection Closed");

                monitor.disable();
            } catch (Exception e) {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }
    }
}
