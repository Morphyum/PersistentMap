using PersistentMapAPI;
using PersistentMapServer.Behavior;
using PersistentMapServer.Worker;
using System;
using System.ComponentModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace PersistentMapServer {

    class Program {

        public static string ServiceUrl = "http://localhost:8000/warServices";

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
                BackgroundWorker heartbeatWorker = new BackgroundWorker {
                    WorkerSupportsCancellation = true
                };
                heartbeatWorker.DoWork += new DoWorkEventHandler(HeartBeatMonitor.DoWork);
                heartbeatWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(HeartBeatMonitor.RunWorkerCompleted);
                heartbeatWorker.RunWorkerAsync();

                SettingsFileMonitor monitor = new SettingsFileMonitor();
                monitor.enable();

                BackgroundWorker backupWorker = new BackgroundWorker {
                    WorkerSupportsCancellation = true
                };
                backupWorker.DoWork += new DoWorkEventHandler(BackupWorker.DoWork);
                backupWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackupWorker.RunWorkerCompleted);
                backupWorker.RunWorkerAsync();

                // Mark events to end on process death
                AppDomain.CurrentDomain.ProcessExit += (s, e) => {
                    monitor.disable();
                    heartbeatWorker.CancelAsync();
                    backupWorker.CancelAsync();
                };
                AppDomain.CurrentDomain.ProcessExit += BackupWorker.ProcessExitHandler;

                // Create a RESTful service host. The service instance is automatically, through 
                //   the WarServiceInstanceProviderBehaviorAttribute. We create the singleton this way to give
                //   us the chance to customize the binding 
                WebServiceHost _serviceHost = new WebServiceHost(typeof(WarServices), new Uri(ServiceUrl));
                AddServiceBehaviors(_serviceHost);

                // Create a binding that wraps the default WebMessageEncodingBindingElement with a BindingElement
                //   that can GZip compress responses when a client requests it.
                WebMessageEncodingBindingElement innerEncoding = new WebMessageEncodingBindingElement {
                    ContentTypeMapper = new ForceJsonWebContentMapper()
                };
                GZipMessageEncodingBindingElement encodingWrapper = new GZipMessageEncodingBindingElement(innerEncoding);

                var transport = new HttpTransportBindingElement {
                    ManualAddressing = true,
                    KeepAliveEnabled = false,
                    AllowCookies = false
                };

                var customBinding = new CustomBinding(encodingWrapper, transport);

                // Create a default endpoint with the JSON/XML behaviors and the behavior to check the incoming headers for GZIP requests
                var endpoint = _serviceHost.AddServiceEndpoint(typeof(IWarServices), customBinding, "");
                endpoint.Behaviors.Add(new WebHttpBehavior());
                endpoint.Behaviors.Add(new GZipBehavior());                

                _serviceHost.Open();

                Console.WriteLine("Open Press Key to close");
                Console.ReadKey();

                _serviceHost.Close();
                Console.WriteLine("Connection Closed");

                // TODO: Move to backup worker
                Helper.SaveCurrentInventories(Helper.LoadCurrentInventories());
                Console.WriteLine("Shops Saved");

                Console.ReadKey();
            } catch (Exception e) {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }

        private static void AddServiceBehaviors(WebServiceHost _serviceHost) {
            ServiceThrottlingBehavior throttlingBehavior = new ServiceThrottlingBehavior {
                MaxConcurrentCalls = 64, // Recommendation is 16 * Processors so 4*16=64
                MaxConcurrentInstances = 9999, // Using a singleton instance, so this doesn't matter
                MaxConcurrentSessions = 9999 // Not using HTTP sessions, so this doesn't matter
            };
            _serviceHost.Description.Behaviors.Add(throttlingBehavior);

            RequestLoggingBehavior loggingBehavior = new RequestLoggingBehavior();
            _serviceHost.Description.Behaviors.Add(loggingBehavior);

            CorsWildcardForAllResponsesBehavior corsBehavior = new CorsWildcardForAllResponsesBehavior();
            _serviceHost.Description.Behaviors.Add(corsBehavior);

            InstanceProviderServiceBehavior instanceProviderBehavior = new InstanceProviderServiceBehavior();
            _serviceHost.Description.Behaviors.Add(instanceProviderBehavior);

        }

        class ForceJsonWebContentMapper : WebContentTypeMapper {
            public override WebContentFormat GetMessageFormatForContentType(string contentType) {
                return WebContentFormat.Json;
            }
        }
    }
}
