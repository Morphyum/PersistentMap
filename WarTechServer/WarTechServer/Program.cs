using System;
using System.ServiceModel.Web;
using WarTechAPI;

namespace WarTechServer {
    class Program {
        static void Main(string[] args) {
            try {
                WarServices warServices = new WarServices();
                WebServiceHost _serviceHost = new WebServiceHost(warServices, new Uri("http://localhost:8000/warServices"));
                _serviceHost.Open();
                Console.WriteLine("Open");
                Console.ReadKey();
                _serviceHost.Close();
            } catch (Exception e) {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }
    }
}
