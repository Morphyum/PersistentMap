using BattleTech;
using HBS.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using static BattleTech.StarSystemDef;

namespace PersistentMapClientTests {


    [TestClass]
    public class HelperTests {



        [TestMethod]
        public void TestIsCapital() {
            Dictionary<Faction, string> testCapitals = new Dictionary<Faction, string> {
            { Faction.Kurita, "Luthien" },
            { Faction.Davion, "New Avalon" },
            { Faction.Liao, "Sian" },
            { Faction.Marik, "Atreus (FWL)" },
            { Faction.Rasalhague, "Rasalhague" },
            { Faction.Ives, "St. Ives" },
            { Faction.Oberon, "Oberon" },
            { Faction.TaurianConcordat, "Taurus" },
            { Faction.MagistracyOfCanopus, "Canopus" },
            { Faction.Outworld, "Alpheratz" },
            { Faction.Circinus, "Circinus" },
            { Faction.Marian, "Alphard (MH)" },
            { Faction.Lothian, "Lothario" },
            { Faction.AuriganRestoration, "Coromodir" },
            { Faction.Steiner, "Tharkad" },
            { Faction.ComStar, "Terra" },
            { Faction.Castile, "Asturias" },
            { Faction.Chainelane, "Far Reach" },
            { Faction.ClanBurrock, "Albion (Clan)" },
            { Faction.ClanCloudCobra, " Zara(Homer 2850 +)" },
            { Faction.ClanCoyote, "Tamaron" },
            { Faction.ClanDiamondShark, "Strato Domingo" },
            { Faction.ClanFireMandrill, "Shadow" },
            { Faction.ClanGhostBear, "Arcadia (Clan)" },
            { Faction.ClanGoliathScorpion, "Dagda (Clan)" },
            { Faction.ClanHellsHorses, "Kirin" },
            { Faction.ClanIceHellion, "Hector" },
            { Faction.ClanJadeFalcon, "Ironhold" },
            { Faction.ClanNovaCat, "Barcella" },
            { Faction.ClansGeneric, "Strana Mechty" },
            { Faction.ClanSmokeJaguar, "Huntress" },
            { Faction.ClanSnowRaven, "Lum" },
            { Faction.ClanStarAdder, "Sheridan (Clan)" },
            { Faction.ClanSteelViper, "New Kent" },
            { Faction.ClanWolf, "Tiber (Clan)" },
            { Faction.Delphi, "New Delphi" },
            { Faction.Elysia, "Blackbone (Nyserta 3025+)" },
            { Faction.Hanse, "Bremen (HL)" },
            { Faction.JarnFolk, "Trondheim (JF)" },
            { Faction.Tortuga, "Tortuga Prime" },
            { Faction.Valkyrate, "Gotterdammerung" },
            { Faction.Axumite, "Thala" },
            };

            foreach (KeyValuePair<Faction, string> entry in testCapitals) {
                bool isCapital = HelperTests.isCapital(entry.Key, entry.Value);
                Assert.IsTrue(isCapital);
            }
            Assert.AreEqual(testCapitals.Count, 42);
        }

        // Bass-ackwards way of testing this, but StarSystem is too @#$&*( hard to instantiate for a test
        private static bool isCapital(Faction faction, string systemName) {
            switch (systemName) {
                case "Luthien": {
                        if (faction == Faction.Kurita)
                            return true;
                        else
                            return false;
                    }
                case "New Avalon": {
                        if (faction == Faction.Davion)
                            return true;
                        else
                            return false;
                    }
                case "Sian": {
                        if (faction == Faction.Liao)
                            return true;
                        else
                            return false;
                    }
                case "Atreus (FWL)": {
                        if (faction == Faction.Marik)
                            return true;
                        else
                            return false;
                    }
                case "Rasalhague": {
                        if (faction == Faction.Rasalhague)
                            return true;
                        else
                            return false;
                    }
                case "St. Ives": {
                        if (faction == Faction.Ives)
                            return true;
                        else
                            return false;
                    }
                case "Oberon": {
                        if (faction == Faction.Oberon)
                            return true;
                        else
                            return false;
                    }
                case "Taurus": {
                        if (faction == Faction.TaurianConcordat)
                            return true;
                        else
                            return false;
                    }
                case "Canopus": {
                        if (faction == Faction.MagistracyOfCanopus)
                            return true;
                        else
                            return false;
                    }
                case "Alpheratz": {
                        if (faction == Faction.Outworld)
                            return true;
                        else
                            return false;
                    }
                case "Circinus": {
                        if (faction == Faction.Circinus)
                            return true;
                        else
                            return false;
                    }
                case "Alphard (MH)": {
                        if (faction == Faction.Marian)
                            return true;
                        else
                            return false;
                    }
                case "Lothario": {
                        if (faction == Faction.Lothian)
                            return true;
                        else
                            return false;
                    }
                case "Coromodir": {
                        if (faction == Faction.AuriganRestoration)
                            return true;
                        else
                            return false;
                    }
                case "Tharkad": {
                        if (faction == Faction.Steiner)
                            return true;
                        else
                            return false;
                    }
                case "Terra": {
                        if (faction == Faction.ComStar)
                            return true;
                        else
                            return false;
                    }
                case "Asturias": {
                        if (faction == Faction.Castile)
                            return true;
                        else
                            return false;
                    }
                case "Far Reach": {
                        if (faction == Faction.Chainelane)
                            return true;
                        else
                            return false;
                    }
                case "Albion (Clan)": {
                        if (faction == Faction.ClanBurrock)
                            return true;
                        else
                            return false;
                    }
                case " Zara(Homer 2850 +)": {
                        if (faction == Faction.ClanCloudCobra)
                            return true;
                        else
                            return false;
                    }
                case "Tamaron": {
                        if (faction == Faction.ClanCoyote)
                            return true;
                        else
                            return false;
                    }
                case "Strato Domingo": {
                        if (faction == Faction.ClanDiamondShark)
                            return true;
                        else
                            return false;
                    }
                case "Shadow": {
                        if (faction == Faction.ClanFireMandrill)
                            return true;
                        else
                            return false;
                    }
                case "Arcadia (Clan)": {
                        if (faction == Faction.ClanGhostBear)
                            return true;
                        else
                            return false;
                    }
                case "Dagda (Clan)": {
                        if (faction == Faction.ClanGoliathScorpion)
                            return true;
                        else
                            return false;
                    }
                case "Kirin": {
                        if (faction == Faction.ClanHellsHorses)
                            return true;
                        else
                            return false;
                    }
                case "Hector": {
                        if (faction == Faction.ClanIceHellion)
                            return true;
                        else
                            return false;
                    }
                case "Ironhold": {
                        if (faction == Faction.ClanJadeFalcon)
                            return true;
                        else
                            return false;
                    }
                case "Barcella": {
                        if (faction == Faction.ClanNovaCat)
                            return true;
                        else
                            return false;
                    }
                case "Strana Mechty": {
                        if (faction == Faction.ClansGeneric)
                            return true;
                        else
                            return false;
                    }
                case "Huntress": {
                        if (faction == Faction.ClanSmokeJaguar)
                            return true;
                        else
                            return false;
                    }
                case "Lum": {
                        if (faction == Faction.ClanSnowRaven)
                            return true;
                        else
                            return false;
                    }
                case "Sheridan (Clan)": {
                        if (faction == Faction.ClanStarAdder)
                            return true;
                        else
                            return false;
                    }
                case "New Kent": {
                        if (faction == Faction.ClanSteelViper)
                            return true;
                        else
                            return false;
                    }
                case "Tiber (Clan)": {
                        if (faction == Faction.ClanWolf)
                            return true;
                        else
                            return false;
                    }
                case "New Delphi": {
                        if (faction == Faction.Delphi)
                            return true;
                        else
                            return false;
                    }
                case "Blackbone (Nyserta 3025+)": {
                        if (faction == Faction.Elysia)
                            return true;
                        else
                            return false;
                    }
                case "Bremen (HL)": {
                        if (faction == Faction.Hanse)
                            return true;
                        else
                            return false;
                    }
                case "Trondheim (JF)": {
                        if (faction == Faction.JarnFolk)
                            return true;
                        else
                            return false;
                    }
                case "Tortuga Prime": {
                        if (faction == Faction.Tortuga)
                            return true;
                        else
                            return false;
                    }
                case "Gotterdammerung": {
                        if (faction == Faction.Valkyrate)
                            return true;
                        else
                            return false;
                    }
                case "Thala": {
                        if (faction == Faction.Axumite)
                            return true;
                        else
                            return false;
                    }
                default: {
                        return false;
                    }
            }
        }
    }
}