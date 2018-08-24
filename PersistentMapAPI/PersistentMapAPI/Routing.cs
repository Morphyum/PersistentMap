namespace PersistentMapAPI {
    public static class Routing {
        public const string GetStarMap = "/StarMap/";
        public const string GetSystem = "/StarMap/System/{name}";
        public const string ResetStarMap = "/StarMap/Reset";
        public const string PostMissionResult = "/Mission/?employer={employer}&target={target}&systemName={systemName}&mresult={mresult}";
    }
}
