using System;
using System.ServiceModel.Web;
using PersistentMapAPI;
using System.Net;

namespace PersistentMapServer {
    class Program {
        static void Main(string[] args) {
            try {
                WarServices warServices = new WarServices();
                WebServiceHost _serviceHost = new WebServiceHost(warServices, new Uri("http://localhost:8000/warServices"));
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
