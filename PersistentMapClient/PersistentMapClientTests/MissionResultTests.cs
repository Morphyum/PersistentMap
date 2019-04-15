using NUnit.Framework;
using PersistentMapClient;

namespace PersistentMapClientTests {

    [TestFixture, NonParallelizable]
    public class MissionResultTests {

        [Test]
        public void TestPostMissionResult() {
            PersistentMapClient.PersistentMapClient.Init(TestContext.CurrentContext.TestDirectory, "test.settings.json");

            PersistentMapAPI.MissionResult mresult = new PersistentMapAPI.MissionResult {
                employer = BattleTech.Faction.Davion,
                target = BattleTech.Faction.Liao,
                result = BattleTech.MissionResult.Victory,
                systemName = "Acrux",
                difficulty = 2,
                awardedRep = 2,
                planetSupport = 0
            };

            const string companyName = "Test Company";
            bool success = Web.PostMissionResult(mresult, companyName);
            Assert.IsTrue(success);
        }

        [TearDown]
        public void Cleanup() {
            PersistentMapClient.PersistentMapClient.Dispose();
        }
    }

}