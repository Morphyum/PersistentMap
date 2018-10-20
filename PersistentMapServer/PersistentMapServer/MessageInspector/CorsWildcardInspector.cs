using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;

namespace PersistentMapServer.MessageInspector {
    /* Inspector that adds a global Cross-Origin-Resource-Sharing wildcard to all GET requests. Browsers will require this header to allow
     *   fetches from in support of other domains. We generally don't care who consumes the information, so this is reasonable. 
     */
    class CorsWildcardInspector : IDispatchMessageInspector {

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext) {
            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState) {
            string method = WebOperationContext.Current.IncomingRequest.Method;
            if (method.ToLower().Equals("get")) {
                WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin:*");
            } 
        }
    }
}
