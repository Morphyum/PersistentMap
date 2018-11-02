using Castle.DynamicProxy;
using PersistentMapAPI;
using PersistentMapServer.Attribute;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;

namespace PersistentMapServer.Interceptor {

    /* Castle.DynamicProxy Interceptor that looks for methods decorated with the AdminKeyAttribute. Invocations of these methods are checked 
     *   to ensure a user has passed the appropriate admin key for access.
     */
    class AdminKeyRequiredInterceptor : IInterceptor {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public void Intercept(IInvocation invocation) {

            bool preventMethodInvocation = false;
            foreach (System.Attribute attribute in invocation.GetConcreteMethod().GetCustomAttributes(false)) {
                if (attribute.GetType() == typeof(AdminKeyRequiredAttribute)) {
                    // Method is decorated 
                    var settings = Helper.LoadSettings();
                    var properties = OperationContext.Current.IncomingMessageProperties;
                    var property = properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
                    WebHeaderCollection headers = property.Headers;
                    var headerValue = headers != null && headers.Get(AdminKeyRequiredAttribute.HeaderName) != null ?
                        headers.Get(AdminKeyRequiredAttribute.HeaderName) : "";
                    if (!headerValue.Equals(settings.AdminKey)) {
                        // Header value doesn't match, block access. Otherwise, let it through
                        preventMethodInvocation = true;
                    }              
                }
            }
            if (preventMethodInvocation) {
                // Prevent the method from executing
                WebOperationContext context = WebOperationContext.Current;
                context.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                context.OutgoingResponse.StatusDescription = $"Access denied.";
                invocation.ReturnValue = null;

                IncomingWebRequestContext requestContext = WebOperationContext.Current.IncomingRequest;
                string serviceMethod = requestContext.UriTemplateMatch != null ? requestContext.UriTemplateMatch.Data.ToString() : "UNMAPPED";
                string requestIP = Helper.mapRequestIP();
                string obfuscatedIP = Helper.HashAndTruncate(requestIP);
                PersistentMapAPI.Settings settings = Helper.LoadSettings();
                logger.Warn($"Prevented unauthorized access from ({( settings.Debug ? requestIP : obfuscatedIP )}) to method {serviceMethod}");
            } else {                
                // Allow the method to execute normally
                invocation.Proceed();
            }
        }


    }
}
