using PersistentMapServer.MessageInspector;
using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace PersistentMapServer.Behavior {

    /*
     * Applies conditional GZip compression logic to requests. Incoming gzip'd requests aren't handled yet.
     * Shamelessly stolen from https://gist.github.com/Lakerfield/32276ccb27f29316ddae
     */
    public class GZipBehavior : IEndpointBehavior {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) {}

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) {}

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new GZipInspector());
        }

        public void Validate(ServiceEndpoint endpoint) {}
    }

    public class GzipBehaviorExtensionElement : BehaviorExtensionElement {
        public GzipBehaviorExtensionElement() { }

        public override Type BehaviorType {
            get { return typeof(GZipBehavior); }
        }

        protected override object CreateBehavior() {
            return new GZipBehavior();
        }
    }
}
