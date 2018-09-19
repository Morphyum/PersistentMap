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

        public static List<ShopDefItem> GetShopForFaction(Faction faction) {
            try {
                string URL = Fields.settings.ServerURL + "warServices/Shop/" + faction;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "GET";
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
                byte[] testarray = Encoding.ASCII.GetBytes(testjson);
                string URL = Fields.settings.ServerURL + "warServices/Salvage/" + faction;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.ContentLength = testarray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(testarray, 0, testarray.Length);
                dataStream.Close();
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string mapstring = reader.ReadToEnd();
                }
            }
            return true;
        }

        public static bool PostSoldItem(ShopDefItem item, Faction faction) {
            List<ShopDefItem> items = new List<ShopDefItem>();
            item.DiscountModifier = 1f;
            item.Count = 1;
            items.Add(item);

            string testjson = JsonConvert.SerializeObject(items);
            byte[] testarray = Encoding.ASCII.GetBytes(testjson);
            string URL = Fields.settings.ServerURL + "warServices/Salvage/" + faction;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.ContentType = "application/json; charset=utf-8";
            request.Method = "POST";
            request.ContentLength = testarray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(testarray, 0, testarray.Length);
            dataStream.Close();
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (Stream responseStream = response.GetResponseStream()) {
                StreamReader reader = new StreamReader(responseStream);
                string mapstring = reader.ReadToEnd();
                return true;
            }
        }

        public static bool PostBuyItem(string id, Faction owner) {
            try {
                string URL = Fields.settings.ServerURL + "warServices/Buy/?Faction=" + owner + "&ID=" + id;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
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

        public static ParseMap GetStarMap() {
            try {
                string URL = Fields.settings.ServerURL + "warServices/StarMap";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "GET";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                ParseMap map;
                using (Stream responseStream = response.GetResponseStream()) {
                    StreamReader reader = new StreamReader(responseStream);
                    string mapstring = reader.ReadToEnd();
                    map = JsonConvert.DeserializeObject<ParseMap>(mapstring);
                }
                return map;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return null;
            }
        }

        public static bool PostMissionResult(PersistentMapAPI.MissionResult mresult) {
            try {
                string testjson = JsonConvert.SerializeObject(mresult);
                byte[] testarray = Encoding.ASCII.GetBytes(testjson);
                string URL = Fields.settings.ServerURL + "warServices/Mission/";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                request.ContentType = "application/json; charset=utf-8";
                request.Method = "POST";
                request.ContentLength = testarray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(testarray, 0, testarray.Length);
                dataStream.Close();
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
