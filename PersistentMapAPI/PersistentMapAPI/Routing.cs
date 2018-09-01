namespace PersistentMapAPI {
    public static class Routing {
        public const string GetStarMap = "/StarMap/";
        public const string GetSystem = "/StarMap/System/{name}";
        public const string ResetStarMap = "/StarMap/Reset";
        public const string PostMissionResultDepricated = "/Mission/?employer={employer}&target={target}&systemName={systemName}&mresult={mresult}";
        public const string PostMissionResult = "/Mission/V2/?employer={employer}&target={target}&systemName={systemName}&mresult={mresult}&difficulty={difficulty}";
        public const string GetMissionResults = "/Mission/Results/?MinutesBack={MinutesBack}&MaxResults={MaxResults}";
        public const string GetActivePlayers = "Users/Active/?MinutesBack={MinutesBack}";
    }
}
