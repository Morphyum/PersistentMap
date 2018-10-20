using PersistentMapAPI;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;

namespace PersistentMapServer.MessageInspector {
    /* Inspector that records the total time spend processing a request. This only includes the time between when WCF received the request, 
     * dispatched it to the service for processing, and the service returned. It does NOT cover the transfer time.
     */
    class RequestDurationLoggingInspector : IDispatchMessageInspector {

        private Dictionary<int, DateTime> requests = new Dictionary<int, DateTime>();

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext) {
            int requestId = OperationContext.Current.GetHashCode();
            requests[requestId] = DateTime.UtcNow;          
            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState) {
            int requestId = OperationContext.Current.GetHashCode();
            DateTime start = requests[requestId];
            DateTime now = DateTime.UtcNow;
            TimeSpan span = now - start;
            int duration = (int)span.TotalMilliseconds;

            string requestIP = mapRequestIP();
            string requestUrl = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri.ToString();
            string serviceMethod = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.Data.ToString();

            Logger.LogLine($"Request from IP ({requestIP}) for url ({requestUrl}) mapped to method ({serviceMethod}) returned in {duration}ms");
        }

        // Stolen from https://stackoverflow.com/questions/33166679/get-client-ip-address-using-wcf-4-5-remoteendpointmessageproperty-in-load-balanc
        protected string mapRequestIP() {
            OperationContext context = OperationContext.Current;
            MessageProperties properties = context.IncomingMessageProperties;
            RemoteEndpointMessageProperty endpoint = properties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            string address = string.Empty;
            if (properties.Keys.Contains(HttpRequestMessageProperty.Name)) {
                HttpRequestMessageProperty endpointLoadBalancer = properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
                if (endpointLoadBalancer != null && endpointLoadBalancer.Headers["X-Forwarded-For"] != null)
                    address = endpointLoadBalancer.Headers["X-Forwarded-For"];
            }
            if (string.IsNullOrEmpty(address)) {
                address = endpoint.Address;
            }
            return address;
        }
    }
}
