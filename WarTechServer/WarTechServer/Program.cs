using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
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
            }
        }
    }
}
