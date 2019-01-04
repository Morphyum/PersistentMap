using NUnit.Framework;
using PersistentMapClient;

namespace PersistentMapClientTests {

    [TestFixture, NonParallelizable]
    public class StarMapTests {

        [Test, Timeout(2000)]        
        public void TestGetStarmap() {
            PersistentMapClient.PersistentMapClient.Init(TestContext.CurrentContext.TestDirectory, 
                "test.settings.json");
            PersistentMapAPI.StarMap parsedMap = Web.GetStarMap();
            Assert.IsNotNull(parsedMap);
            Assert.AreEqual(3337, parsedMap.systems.Count);
            
        }

        [TearDown]
        public void Cleanup() {
            PersistentMapClient.PersistentMapClient.Dispose();
        }
    }
}