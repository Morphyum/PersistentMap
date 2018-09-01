using System.Collections.Generic;
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
        System PostMissionResult(string employer, string target, string systemName, string mresult, string difficulty);

        [OperationContract]
        [WebGet(UriTemplate = Routing.PostMissionResultDepricated, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        System PostMissionResultDeprecated(string employer, string target, string systemName, string mresult);

        [OperationContract]
        [WebGet(UriTemplate = Routing.GetMissionResults, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        List<HistoryResult> GetMissionResults(string MinutesBack, string MaxResults);

        [OperationContract]
        [WebGet(UriTemplate = Routing.GetActivePlayers, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        int GetActivePlayers(string MinutesBack);
        
        /*[OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = Routing.ResetStarMap, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        string ResetStarMap();*/
    }
}
