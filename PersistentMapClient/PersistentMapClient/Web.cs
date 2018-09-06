using Newtonsoft.Json;
using PersistentMapAPI;
using System;
using System.IO;
using System.Net;

namespace PersistentMapClient {
    public static class Web {

        public static StarMap GetStarMap() {
            try {
                string URL = Fields.settings.ServerURL + "warServices/StarMap";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "GET";
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

        public static bool PostMissionResult(MissionResult mresult) {
            try {
                string URL = Fields.settings.ServerURL + "warServices/Mission/V4/?employer=" + mresult.employer + "&target=" + mresult.target
                    + "&systemName=" + Uri.EscapeDataString(mresult.systemName) + "&mresult=" + mresult.result.ToString() + "&difficulty=" + mresult.difficulty+"&rep=" + mresult.awardedRep + "&planetSupport=" + mresult.planetSupport;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "GET";
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
    }
}
