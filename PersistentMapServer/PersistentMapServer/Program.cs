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
                WarServices warServices = new WarServices();
                WebServiceHost _serviceHost = new WebServiceHost(warServices, new Uri("http://localhost:8001/warServices"));
               ServiceThrottlingBehavior behaviour = new ServiceThrottlingBehavior();
                behaviour.MaxConcurrentSessions = 9999;
                behaviour.MaxConcurrentCalls = 9999;
                behaviour.MaxConcurrentInstances = 9999;
                _serviceHost.Description.Behaviors.Add(behaviour);
                _serviceHost.Open();
                Console.WriteLine("Open Press Key to close");
                Console.ReadKey();
                _serviceHost.Close();
                Console.WriteLine("Connection Closed");
            } catch (Exception e) {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }
    }
}
