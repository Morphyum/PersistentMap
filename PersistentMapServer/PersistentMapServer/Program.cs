using PersistentMapAPI;
using PersistentMapServer.Behavior;
using PersistentMapServer.Interceptor;
using PersistentMapServer.Worker;
using System;
using System.ComponentModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace PersistentMapServer {

    class Program {

        public static string ServiceUrl = "http://localhost:8001/warServices";

        /*
         *Application that uses Windows Communications Foundation (WCF) to provide a RESTful API that allows persistence of Morphyum's WarTech.
         * If you're unfamiliar with WCF, checkout the following:
         * 
         * http://dotnetmentors.com/wcf/overview-on-wcf-service-architecture.aspx
         * https://docs.microsoft.com/en-us/dotnet/framework/wcf/extending/extending-dispatchers
         * 
         * The client is the PersistentMapClient, in this repository.
         * This PersistentMapServer is the server.
         * 
         * Note that WCF is no longer the preferred solution for REST endpoints, which has become ASP.NET5 w/ MVC6. 
         *  See https://blog.tonysneed.com/2016/01/06/wcf-is-dead-long-live-mvc-6/.
        */
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

                BackgroundWorker backupWorker = new BackgroundWorker();
                backupWorker.WorkerSupportsCancellation = true;
                backupWorker.DoWork += new DoWorkEventHandler(BackupWorker.DoWork);
                backupWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackupWorker.RunWorkerCompleted);
                backupWorker.RunWorkerAsync();

                WarServices warServices = new WarServices();
                // Create an AOP proxy object that we can hang Castle.DynamicProxies upon. These are useful for operations across the whole
                //   of the service, or for when we need to fail a message in a reasonable way. 
                var proxy = new Castle.DynamicProxy.ProxyGenerator()
                    .CreateClassProxyWithTarget<WarServices>(warServices, new Castle.DynamicProxy.IInterceptor[] {
                        new UserQuotaInterceptor(), new AdminKeyRequiredInterceptor()
                    });

                WebServiceHost _serviceHost = new WebServiceHost(proxy, new Uri(ServiceUrl));
                addBehaviors(_serviceHost);
                _serviceHost.Open();

                Console.WriteLine("Open Press Key to close");
                Console.ReadKey();

                _serviceHost.Close();
                Console.WriteLine("Connection Closed");

                // Cleanup any outstanding processes
                monitor.disable();
                heartbeatWorker.CancelAsync();
                backupWorker.CancelAsync();
                BackupWorker.BackupOnExit();

            } catch (Exception e) {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }

        private static void addBehaviors(WebServiceHost _serviceHost) {
            ServiceThrottlingBehavior throttlingBehavior = new ServiceThrottlingBehavior();
                throttlingBehavior.MaxConcurrentCalls = 64; // Recommendation is 16 * Processors so 4*16=64
                throttlingBehavior.MaxConcurrentInstances = 9999; // Using a singleton instance, so this doesn't matter
                throttlingBehavior.MaxConcurrentSessions = 9999; // Not using HTTP sessions, so this doesn't matter
            _serviceHost.Description.Behaviors.Add(throttlingBehavior);

            RequestLoggingBehavior loggingBehavior = new RequestLoggingBehavior();
            _serviceHost.Description.Behaviors.Add(loggingBehavior);

            CorsWildcardForAllResponsesBehavior corsBehavior = new CorsWildcardForAllResponsesBehavior();
            _serviceHost.Description.Behaviors.Add(corsBehavior);
        }
    }
}
