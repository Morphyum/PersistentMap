namespace PersistentMapAPI {
    public static class Routing {
        public const string GetStarMap = "/StarMap/";
        public const string GetSystem = "/StarMap/System/{name}";

        public const string GetMissionResults = "/Mission/Results/?MinutesBack={MinutesBack}&MaxResults={MaxResults}";
        public const string GetActivePlayers = "/Users/Active/?MinutesBack={MinutesBack}";
        public const string GetShopForFaction = "/Shop/{Faction}";

        public const string PostMissionResult = "/Mission/?CompanyName={CompanyName}";
        public const string PostSalvageForFaction = "/Salvage/{Faction}";
        public const string PostPurchaseForFaction = "/Buy/{Faction}";

        public const string GetStartupTime = "/Info/StartupTime";

        public const string GetPlayerCompanies = "/Player/";
        public const string GetPlayerHistory = "/Player/{PlayerId}/History/";

        // Admin functions
        public const string GetServiceDataSnapshot = "/Admin/ServiceDataSnapshot";
        public const string GetConnections = "/Admin/Connections";
        public const string LoadTestData = "/Admin/loadTestData";
        public const string ResetStarMap = "/Admin/StarMap/Reset";

        //DEPRECATED
        public const string PostPurchaseForFactionDepricated = "/Buy/?Faction={Faction}&ID={ID}";
        public const string PostMissionResultDepricated = "/Mission/?employer={employer}&target={target}&systemName={systemName}&mresult={mresult}";
        public const string PostMissionResultDepricated2 = "/Mission/V2/?employer={employer}&target={target}&systemName={systemName}&mresult={mresult}&difficulty={difficulty}";
        public const string PostMissionResultDepricated3 = "/Mission/V3/?employer={employer}&target={target}&systemName={systemName}&mresult={mresult}&difficulty={difficulty}&rep={rep}";
        public const string PostMissionResultDepricated4 = "/Mission/V4/?employer={employer}&target={target}&systemName={systemName}&mresult={mresult}&difficulty={difficulty}&rep={rep}&planetSupport={planetSupport}";
        public const string PostMissionResultDepricated5 = "/Mission/";
    }
}
