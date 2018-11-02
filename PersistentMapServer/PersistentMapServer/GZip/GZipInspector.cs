using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace PersistentMapServer.MessageInspector {
    public class GZipInspector : IDispatchMessageInspector {
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, System.ServiceModel.InstanceContext instanceContext) {
            try {
                var prop = request.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
                var accept = prop.Headers[HttpRequestHeader.AcceptEncoding];

                if (!string.IsNullOrEmpty(accept) && accept.Contains("gzip"))
                    OperationContext.Current.Extensions.Add(new CompressOutputExtension());
            } catch { }

            return null;
        }

        public void BeforeSendReply(ref System.ServiceModel.Channels.Message reply, object correlationState) {
            if (OperationContext.Current.Extensions.OfType<CompressOutputExtension>().Any()) {
                HttpResponseMessageProperty httpResponseProperty = new HttpResponseMessageProperty();
                httpResponseProperty.Headers.Add(HttpResponseHeader.ContentEncoding, "gzip");
                reply.Properties[HttpResponseMessageProperty.Name] = httpResponseProperty;
            }
        }
    }
}
