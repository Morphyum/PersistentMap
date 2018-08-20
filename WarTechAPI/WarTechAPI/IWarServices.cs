using System.ServiceModel;
using System.ServiceModel.Web;

namespace WarTechAPI {
    [ServiceContract(Name = "WarServices")]
    interface IWarServices {

        [OperationContract]
        [WebGet(UriTemplate = Routing.GetClientRoute, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        StarMap GetStarmap(string id);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = Routing.GetClientRoute, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        StarMap PostStarmap(string id);
    }
}
