using Newtonsoft.Json;
using System;
using System.ServiceModel;
using System.ServiceModel.Activation;

namespace WarTechAPI {

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,ConcurrencyMode = ConcurrencyMode.Single, IncludeExceptionDetailInFaults = true)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class WarServices : IWarServices {
        public WarModel GetClientNameById(string Id) {
            WarModel model = new WarModel();
            model.abc = Id;
            return model;
        }
    }
}
