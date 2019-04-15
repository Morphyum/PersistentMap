namespace PersistentMapAPI {
    public class Settings {

        public int HalfSkullPercentageForWin = 2;
        public int HalfSkullPercentageForLoss = 1;

        public int minMinutesBetweenPost = 10;

        public int MinutesForActive = 60;
        public int MinutesTillShopUpdate = 1;
        public int MaxItemsPerShop = 30;

        public float DiscountPerItem = 0.01f;
        public float DiscountFloor = 0.5f;
        public float DiscountCeiling = 1.5f;

        public int HoursPerBackup = 24;
        public int MinutesPerBackup = 30;

        public int MaxPlanetSupport = 20;

        public int MaxRep = 30;

        public bool Debug = false;

        public string AdminKey = null;

        public int LowerFortBorder = 100;
        public int UpperFortBorder = 300;
        public float FortPercentage = 0.1f;
    }
}
