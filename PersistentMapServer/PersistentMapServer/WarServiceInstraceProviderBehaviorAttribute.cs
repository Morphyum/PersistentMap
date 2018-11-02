using PersistentMapAPI;
using PersistentMapServer.Interceptor;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace PersistentMapServer {

    public class InstanceProviderServiceBehaviorAttribute : System.Attribute, IServiceBehavior {
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) {}

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) {
            var cd = serviceHostBase.ChannelDispatchers[0] as ChannelDispatcher;
            var dispatchRuntime = cd.Endpoints[0].DispatchRuntime;
            dispatchRuntime.SingletonInstanceContext = new InstanceContext(serviceHostBase);
            dispatchRuntime.InstanceProvider = new WarServiceInstanceProvider();

            //foreach (ChannelDispatcher cd in serviceHostBase.ChannelDispatchers) {
            //    foreach (EndpointDispatcher ed in cd.Endpoints) {
            //        if (!ed.IsSystemEndpoint) {
            //            ed.DispatchRuntime.InstanceProvider = new WarServiceInstanceProvider();
            //        }
            //    }
            //}
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) {}
    }

    public class WarServiceInstanceProvider : IInstanceProvider {

        public object GetInstance(InstanceContext instanceContext) {
            return this.GetInstance(instanceContext, null);    
        }

        public object GetInstance(InstanceContext instanceContext, Message message) {
            WarServices warServices = new WarServices();
            // Create an AOP proxy object that we can hang Castle.DynamicProxies upon. These are useful for operations across the whole
            //   of the service, or for when we need to fail a message in a reasonable way. 
            var proxy = new Castle.DynamicProxy.ProxyGenerator()
                .CreateClassProxyWithTarget<WarServices>(warServices, new UserQuotaInterceptor());
            return proxy;
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance) {
            
        }
    }
}
