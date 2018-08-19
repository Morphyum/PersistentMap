using System.ServiceModel;
using System.ServiceModel.Web;

namespace WarTechAPI {
    [ServiceContract(Name = "WarServices")]
    interface IWarServices {

        [OperationContract]
        [WebGet(UriTemplate = Routing.GetClientRoute, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        StarMap GetStarmap();
    }
}
