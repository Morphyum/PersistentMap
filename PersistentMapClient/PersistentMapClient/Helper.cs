using Newtonsoft.Json;
using PersistentMapAPI;
using System;
using System.IO;
using System.Net;

namespace PersistentMapClient {

    public class SaveFields {

    }

    public class Helper {
        public static Settings LoadSettings() {
            try {
                using (StreamReader r = new StreamReader($"{ PersistentMapClient.ModDirectory}/settings.json")) {
                    string json = r.ReadToEnd();
                    return JsonConvert.DeserializeObject<Settings>(json);
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex);
                return null;
            }
        }

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

        public static void PostMissionResult(MissionResult mresult) {
            try {

                string URL = Fields.settings.ServerURL + "warServices/Mission/?employer="+mresult.employer+ "&target=" + mresult.target 
                    + "&systemName=" + mresult.systemName + "&mresult=" + mresult.result.ToString();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "GET";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                StarMap map;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string mapstring = reader.ReadToEnd();
                    Logger.LogLine(mapstring);
                }
                   
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
}
