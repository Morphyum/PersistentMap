using System.ServiceModel;
using System.ServiceModel.Web;

namespace WarTechAPI
{
    [ServiceContract(Name = "WarServices")]
    public interface IWarServices {
        [OperationContract]
        [WebGet(UriTemplate = Routing.GetClientRoute, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        WarModel GetClientNameById(string Id);
    }
}
