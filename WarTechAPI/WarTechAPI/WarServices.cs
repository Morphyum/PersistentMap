using System.ServiceModel;
using System.ServiceModel.Activation;

namespace WarTechAPI {

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class WarServices : IWarServices {
        public StarMap GetStarmap() {
            StarMap map = new StarMap();
            return map;
        }
    }
}
