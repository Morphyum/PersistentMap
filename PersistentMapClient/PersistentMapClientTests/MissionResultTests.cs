using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersistentMapClient;

namespace PersistentMapClientTests {

    [TestClass]
    public class MissionResultTests {
        [TestMethod]
        //[Timeout(5000)]
        public void TestPostMissionResult() {
            PersistentMapClient.PersistentMapClient.Init(".", "test.settings.json");

            PersistentMapAPI.MissionResult mresult = new PersistentMapAPI.MissionResult();
            mresult.employer = BattleTech.Faction.Davion;
            mresult.target = BattleTech.Faction.Liao;
            mresult.result = BattleTech.MissionResult.Victory;
            mresult.systemName = "Acrux";
            mresult.difficulty = 2;
            mresult.awardedRep = 2;
            mresult.planetSupport = 0;

            const string companyName = "Test Company";
            bool success = Web.PostMissionResult(mresult, companyName);
            Assert.IsTrue(success);            
        }
    }
}