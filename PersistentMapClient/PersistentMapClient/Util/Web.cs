using BattleTech;
using Newtonsoft.Json;
using PersistentMapAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace PersistentMapClient {
    public static class Web {

        private enum WarService {
            GetFactionShop,
            GetStarMap,
            PostBuyItems,
            PostMissionResult,
            PostSalvage
        }        

        // Pulls the inventory for the specified faction
        public static List<ShopDefItem> GetShopForFaction(Faction faction) {
            try {

                HttpWebRequest request = new RequestBuilder(WarService.GetFactionShop).Faction(faction).Build();
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                List<ShopDefItem> items;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string itemsstring = reader.ReadToEnd();
                    items = JsonConvert.DeserializeObject<List<ShopDefItem>>(itemsstring);
                }
                return items;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        // Send any salvage the user didn't want to the faction inventory
        public static bool PostUnusedSalvage(List<SalvageDef> ___finalPotentialSalvage, Faction faction) {
            List<ShopDefItem> items = new List<ShopDefItem>();
            foreach (SalvageDef salvage in ___finalPotentialSalvage) {
                ShopDefItem item = new ShopDefItem();
                item.ID = salvage.Description.Id;
                switch (salvage.ComponentType) {
                    case ComponentType.AmmunitionBox: {
                            item.Type = ShopItemType.AmmunitionBox;
                            break;
                        }
                    case ComponentType.HeatSink: {
                            item.Type = ShopItemType.HeatSink;
                            break;
                        }
                    case ComponentType.JumpJet: {
                            item.Type = ShopItemType.JumpJet;
                            break;
                        }
                    case ComponentType.MechPart: {
                            item.Type = ShopItemType.MechPart;
                            break;
                        }
                    case ComponentType.Upgrade: {
                            item.Type = ShopItemType.Upgrade;
                            break;
                        }
                    case ComponentType.Weapon: {
                            item.Type = ShopItemType.Weapon;
                            break;
                        }
                }
                item.DiscountModifier = 1f;
                item.Count = 1;
                items.Add(item);
            }
            if (items.Count > 0) {
                string testjson = JsonConvert.SerializeObject(items);
                HttpWebRequest request = new RequestBuilder(WarService.PostSalvage).Faction(faction).PostData(testjson).Build();
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string mapstring = reader.ReadToEnd();
                }
            }
            return true;
        }

        // Anything the user sells goes into faction inventory as well.
        public static bool PostSoldItems(List<ShopDefItem> items, Faction faction) {
            foreach (ShopDefItem item in items) {
                item.DiscountModifier = 1f;
                item.Count = 1;
            }
            string testjson = JsonConvert.SerializeObject(items);
            HttpWebRequest request = new RequestBuilder(WarService.PostSalvage).Faction(faction).PostData(testjson).Build();
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (Stream responseStream = response.GetResponseStream()) {
                StreamReader reader = new StreamReader(responseStream);
                string mapstring = reader.ReadToEnd();
                return true;
            }
        }

        // Send a list of items to purchase from the faction store
        public static bool PostBuyItems(List<string> ids, Faction owner) {
            try {
                string testjson = JsonConvert.SerializeObject(ids);
                HttpWebRequest request = new RequestBuilder(WarService.PostBuyItems).Faction(owner).PostData(testjson).Build();
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string mapstring = reader.ReadToEnd();
                    return true;
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
                return false;
            }
        }

        // Fetch the current state of the starmap
        public static PersistentMapAPI.StarMap GetStarMap() {
            try {
                HttpWebRequest request = new RequestBuilder(WarService.GetStarMap).Build();
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                StarMap map;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string mapstring = reader.ReadToEnd();
                    map = JsonConvert.DeserializeObject<StarMap>(mapstring);
                }
                return map;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        // Send the results of a mission to the server
        public static bool PostMissionResult(PersistentMapAPI.MissionResult mresult, string companyName) {
            try {
                string testjson = JsonConvert.SerializeObject(mresult);           
                HttpWebRequest request = new RequestBuilder(WarService.PostMissionResult).CompanyName(companyName).PostData(testjson).Build();
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string mapstring = reader.ReadToEnd();
                }
                return true;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return false;
            }
        }

        // Builder class that encapsulates all the common functions of making a request to the REST API
        private class RequestBuilder {

            private string _requestUrl;
            private string _requestMethod;
            private string _faction;
            private string _companyName;
            private string _postJSON;
            private WarService _service;

            public RequestBuilder(WarService service) {
                this._service = service;
            }

            public RequestBuilder Faction(Faction faction) {
                _faction = faction.ToString();
                return this;
            }

            public RequestBuilder CompanyName(string companyName) {
                _companyName = companyName;
                return this;
            }

            public RequestBuilder PostData(string postJSON) {
                _postJSON = postJSON;
                return this;
            }

            public HttpWebRequest Build() {
                switch (_service) {
                    case WarService.PostBuyItems:
                        _requestUrl = $"{Fields.settings.ServerURL}warServices/Buy/{_faction}";
                        _requestMethod = "POST";
                        break;
                    case WarService.PostMissionResult:
                        _requestUrl = $"{Fields.settings.ServerURL}warServices/Mission/?CompanyName={_companyName}";
                        _requestMethod = "POST";
                        break;
                    case WarService.PostSalvage:
                        _requestUrl = $"{Fields.settings.ServerURL}warServices/Salvage/{_faction}";
                        _requestMethod = "POST";
                        break;
                    case WarService.GetFactionShop:
                        _requestUrl = $"{Fields.settings.ServerURL}warServices/Shop/{_faction}";
                        _requestMethod = "GET";
                        break;
                    case WarService.GetStarMap:
                    default:
                        _requestUrl = $"{Fields.settings.ServerURL}warServices/StarMap/";
                        _requestMethod = "GET";
                        break;
                }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_requestUrl);
                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.AllowAutoRedirect = true;
                request.Method = _requestMethod;
                request.ContentType = "application/json; charset=utf-8";
                request.Timeout = 30000; // 30s connection timeout

                if (_postJSON != null) {
                    // TODO: Why are we ASCII encoding instead of UTF-8?
                    byte[] testarray = Encoding.ASCII.GetBytes(_postJSON);
                    request.ContentLength = testarray.Length;

                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(testarray, 0, testarray.Length);
                    dataStream.Close();
                }

                if (Fields.settings.ClientID != null) {
                    request.Headers.Add("X-RT-MAP-CLIENT_ID", Fields.settings.ClientID);
                }

                return request;
            }
        }
    }
}
