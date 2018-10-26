using Castle.DynamicProxy;
using PersistentMapAPI;
using PersistentMapServer.Attribute;
using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;

namespace PersistentMapServer.Interceptor {

    /* Castle.DynamicProxy Interceptor that looks for methods decorated with the UserQuotaAttribute. Invocations of these methods are checked 
     *   to ensure a user isn't trying to send too many of them at once. 
     *   
     *   At this point in time, all requests are constrained by Settings.minMinutesBetweenPost. This value may need to be flexible to support
     *     POST methods other than MissionResults. The user's likely to send multiple shop purchase orders during that time, for instance, 
     *     and if we want quotas there this needs to be refined.
     */
    class UserQuotaInterceptor : IInterceptor {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public void Intercept(IInvocation invocation) {

            bool returnNull = false;
            foreach (System.Attribute attribute in invocation.GetConcreteMethod().GetCustomAttributes(false)) {
                if (attribute.GetType() == typeof(UserQuotaAttribute)) {

                    string requestIP = mapRequestIP();
                    string obfuscatedIP = HashAndTruncate(requestIP);
                    if (Holder.connectionStore.ContainsKey(requestIP) && Holder.connectionStore[requestIP].LastDataSend != null) {
                        UserInfo info = Holder.connectionStore[requestIP];

                        PersistentMapAPI.Settings settings = Helper.LoadSettings();
                        DateTime now = DateTime.UtcNow;
                        DateTime blockedUntil = info.LastDataSend.AddMinutes(settings.minMinutesBetweenPost);
                        TimeSpan delta = now.Subtract(info.LastDataSend);
                        if (now >= blockedUntil) {
                            // The user hasn't sent a message within the time limit, so just mark it when we're tracing
                            logger.Trace($"IP:{(settings.Debug ? requestIP : obfuscatedIP)} last send a request {delta.ToString()} ago.");
                        } else {
                            // User is flooding. We should send back a 429 (Too Many Requests) but WCF isn't there yet. Send back a 403 for now.
                            // TODO: Verify this works for the client.
                            if (((UserQuotaAttribute)attribute).enforcementPolicy == UserQuotaAttribute.EnforcementEnum.Block) {
                                WebOperationContext context = WebOperationContext.Current;
                                context.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                                context.OutgoingResponse.StatusDescription = $"Too many requests - try again later.";
                                logger.Info($"Flooding from IP:({(settings.Debug ? requestIP : obfuscatedIP)}) - last request was {info.LastDataSend.ToString()} which was {delta.Seconds}s ago.");
                                returnNull = true;
                            } else {
                                // Update the last sent marker
                                info.LastDataSend = DateTime.UtcNow;
                                logger.Debug($"Potential flooding from IP:({(settings.Debug ? requestIP : obfuscatedIP)}) - last request was {info.LastDataSend.ToString()} which was {delta.Seconds}s ago.");
                            }
                        }
                    } else {
                        // Add a new record of access
                        UserInfo info = new UserInfo();
                        info.LastDataSend = DateTime.UtcNow;
                        info.companyName = "";
                        info.lastSystemFoughtAt = "";
                        Holder.connectionStore.Add(requestIP, info);
                    }                    
                }
            }
            if (returnNull) {
                // Prevent the method from executing by not invoking proceed
                invocation.ReturnValue = null;
            } else {
                // Allow the method to execute normally
                invocation.Proceed();
            }
        }

        // Stolen from https://stackoverflow.com/questions/3984138/hash-string-in-c-sharp
        internal static string HashAndTruncate(string text) {
            if (String.IsNullOrEmpty(text))
                return String.Empty;

            using (var sha = new System.Security.Cryptography.SHA256Managed()) {
                byte[] textData = System.Text.Encoding.UTF8.GetBytes(text);
                byte[] hash = sha.ComputeHash(textData);
                String convertedHash = BitConverter.ToString(hash).Replace("-", String.Empty);
                String truncatedHash = convertedHash.Length > 12 ? convertedHash.Substring(0, 11) : convertedHash.Substring(0, convertedHash.Length);
                return truncatedHash + "...";
            }
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
