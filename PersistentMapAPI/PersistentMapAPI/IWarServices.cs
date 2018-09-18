﻿using BattleTech;
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
        [WebInvoke(Method = "POST", UriTemplate = Routing.PostMissionResult, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        System PostMissionResult(MissionResult mresult);

        [OperationContract]
        [WebGet(UriTemplate = Routing.GetMissionResults, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        List<HistoryResult> GetMissionResults(string MinutesBack, string MaxResults);

        [OperationContract]
        [WebGet(UriTemplate = Routing.GetActivePlayers, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        int GetActivePlayers(string MinutesBack);

        [OperationContract]
        [WebGet(UriTemplate = Routing.GetStartupTime, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        string GetStartupTime();

        [OperationContract]
        [WebGet(UriTemplate = Routing.GetShopForFaction, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        List<ShopDefItem> GetShopForFaction(string Faction);

        //DEPRECATED
        [OperationContract]
        [WebGet(UriTemplate = Routing.PostMissionResultDepricated4, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        System PostMissionResultDepricated4(string employer, string target, string systemName, string mresult, string difficulty, string rep, string planetSupport);

        [OperationContract]
        [WebGet(UriTemplate = Routing.PostMissionResultDepricated3, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        System PostMissionResultDeprecated3(string employer, string target, string systemName, string mresult, string difficulty, string rep);


        [OperationContract]
        [WebGet(UriTemplate = Routing.PostMissionResultDepricated2, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        System PostMissionResultDeprecated2(string employer, string target, string systemName, string mresult, string difficulty);

        [OperationContract]
        [WebGet(UriTemplate = Routing.PostMissionResultDepricated, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        System PostMissionResultDeprecated(string employer, string target, string systemName, string mresult);

        /* [OperationContract]
         [WebInvoke(Method = "POST", UriTemplate = Routing.ResetStarMap, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
         string ResetStarMap();*/
    }
}
