using System.ServiceModel;
using System.ServiceModel.Web;

namespace PersistentMapAPI {
    [ServiceContract(Name = "WarServices")]
    interface IWarServices {

        [OperationContract]
        [WebGet(UriTemplate = Routing.GetStarMap, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        StarMap GetStarmap();

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = Routing.PostMissionResult, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        System PostMissionResult(MissionResult postedResult);
    }
}
