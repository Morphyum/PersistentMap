using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;

namespace PersistentMapServer.MessageInspector {
    /* Inspector that records the total time spend processing a request. This only includes the time between when WCF received the request, 
     * dispatched it to the service for processing, and the service returned. It does NOT cover the transfer time.
     */
    class RequestDurationLoggingInspector : IDispatchMessageInspector {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Requests that take longer than this many millseconds will always be reported. All other requests logged at trace.
        private const long ReportingThreshold = 50;

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext) {
            return Stopwatch.StartNew();
        }

        public void BeforeSendReply(ref Message reply, object correlationState) {
            IncomingWebRequestContext requestContext = WebOperationContext.Current.IncomingRequest;
            string serviceMethod = requestContext.UriTemplateMatch != null ? requestContext.UriTemplateMatch.Data.ToString() : "UNMAPPED";
            int requestId = OperationContext.Current.GetHashCode();

            Stopwatch stopWatch = (Stopwatch)correlationState;
            stopWatch.Stop();

            long deltaAsMillis = stopWatch.ElapsedTicks / TimeSpan.TicksPerMillisecond;
            if (deltaAsMillis > ReportingThreshold) {
                logger.Info($"RequestID ({requestId}) mapped to method ({serviceMethod}) returned in {deltaAsMillis}ms");
            } else {
                logger.Trace($"RequestID ({requestId}) mapped to method ({serviceMethod}) returned in {deltaAsMillis}ms");
            }
        }

    }
}
