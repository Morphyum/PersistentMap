using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersistentMapClient;

namespace PersistentMapClientTests {

    [TestClass]
    public class StarMapTests {
        [TestMethod]
        [Timeout(2000)]
        public void TestGetStarmap() {
            PersistentMapClient.PersistentMapClient.Init(".", "test.settings.json");
            ParseMap parsedMap = Web.GetStarMap();
            Assert.IsNotNull(parsedMap);
            Assert.AreEqual(parsedMap.systems.Count, 3359);
        }
    }
}