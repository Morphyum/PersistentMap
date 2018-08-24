using System.ServiceModel;
using System.ServiceModel.Web;

namespace PersistentMapAPI {
    [ServiceContract(Name = "WarServices")]
    interface IWarServices {

        [OperationContract]
        [WebGet(UriTemplate = Routing.GetStarMap, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        StarMap GetStarmap();

        [OperationContract]
        [WebGet(UriTemplate = Routing.GetSystem, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        System GetSystem(string name);

        [OperationContract]
        [WebGet(UriTemplate = Routing.PostMissionResult, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        System PostMissionResult(string employer, string target, string systemName, string mresult);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = Routing.ResetStarMap, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        string ResetStarMap();
    }
}
