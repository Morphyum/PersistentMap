using System;

namespace PersistentMapServer.Attribute {
    // Attribute that indicates calls against this service method should be quota controlled. The incoming user will be inspected
    //   against recent activity and either reported or throttled as necessary.
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class UserQuotaAttribute : System.Attribute {

        private EnforcementEnum _enforcement = EnforcementEnum.Report;
        public EnforcementEnum enforcementPolicy {
            get { return _enforcement;  }
        }

        public enum EnforcementEnum {
            Block,
            Report
        }

        public UserQuotaAttribute(EnforcementEnum enforcement) {
            this._enforcement = enforcement;
        }

    }
}
