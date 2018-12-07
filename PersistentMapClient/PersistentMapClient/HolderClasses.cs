using BattleTech;
using BattleTech.Framework;
using System;
using System.Collections.Generic;

namespace PersistentMapClient {
    public class Settings {
        public string ServerURL = "http://localhost:8000/";
        public bool debug = false;
        public float priorityContactPayPercentage = 2f;
    }

    public static class Fields {
        public static Settings settings;
        public static Dictionary<string, string> FluffDescriptions = new Dictionary<string, string>();
        public static bool firstpass = true;
        public static bool warmission = false;
        public static string ShopFileTag = "rt_economy";
        public static List<Faction> excludedFactions = new List<Faction>() { Faction.AuriganMercenaries, Faction.Betrayers, Faction.MagistracyCentrella,
            Faction.MajestyMetals, Faction.MercenaryReviewBoard, Faction.Nautilus, Faction.NoFaction, Faction.FlakJackals, Faction.LocalsBrockwayRefugees,
            Faction.SelfEmployed, Faction.MasonsMarauders, Faction.SteelBeast, Faction.KellHounds, Faction.RazorbackMercs, Faction.HostileMercenaries };
        public static ParseMap currentMap;
        public static Dictionary<Faction, List<ShopDefItem>> currentShops = new Dictionary<Faction, List<ShopDefItem>>();
        public static KeyValuePair<Faction, List<ShopDefItem>> currentShopSold = new KeyValuePair<Faction, List<ShopDefItem>>(Faction.INVALID_UNSET,new List<ShopDefItem>());
        public static KeyValuePair<Faction, List<string>> currentShopBought= new KeyValuePair<Faction, List<string>>(Faction.INVALID_UNSET, new List<string>());
        public static Dictionary<Faction, DateTime> LastUpdate = new Dictionary<Faction, DateTime>();
        public static int UpdateTimer = 15;
    }

    public struct PotentialContract {
        public ContractOverride contractOverride;
        public Faction employer;
        public Faction target;
        public int difficulty;
    }
}