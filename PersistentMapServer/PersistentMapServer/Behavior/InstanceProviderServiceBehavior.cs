using PersistentMapAPI;
using PersistentMapServer.Interceptor;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace PersistentMapServer {

    /* Provides a proxy-wrapped version of WarServices when the ServiceHost loads. This indirection is necessary
     * to allow customization of the ServiceEndpoint to allow GZIp behaviors. 
     */
    public class InstanceProviderServiceBehavior : IServiceBehavior {
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) {}

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) {
            var cd = serviceHostBase.ChannelDispatchers[0] as ChannelDispatcher;
            var dispatchRuntime = cd.Endpoints[0].DispatchRuntime;
            dispatchRuntime.SingletonInstanceContext = new InstanceContext(serviceHostBase);
            dispatchRuntime.InstanceProvider = new WarServiceInstanceProvider();
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
                .CreateClassProxyWithTarget<WarServices>(warServices, new Castle.DynamicProxy.IInterceptor[] {
                        new UserQuotaInterceptor(), new AdminKeyRequiredInterceptor()
                });
            return proxy;
        }

        public void ReleaseInstance(InstanceContext instanceContext, object instance) {
            
        }
    }
}
