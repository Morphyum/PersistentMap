using PersistentMapServer.MessageInspector;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace PersistentMapServer.Behavior {
    // Behavior that injects RequestDurationLoggingInspector, which records the time spend dispatching a request
    class RequestLoggingBehavior : IServiceBehavior {
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, 
            Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) { }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) {
            foreach (ChannelDispatcher channelDispatcher in serviceHostBase.ChannelDispatchers) {
                foreach (EndpointDispatcher endpointDispatcher in channelDispatcher.Endpoints) {
                    endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new RequestDurationLoggingInspector());
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }
    }
}
