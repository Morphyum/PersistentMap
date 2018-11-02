using System;

namespace PersistentMapServer.Attribute {
    // Attribute that indicates calls against this service method are controlled by an access token, expressed as a HTTP eader
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class AdminKeyRequiredAttribute : System.Attribute {

        public const string HeaderName = "X-RT-PMS-ADMIN-KEY";

        public AdminKeyRequiredAttribute() {}

    }
}
