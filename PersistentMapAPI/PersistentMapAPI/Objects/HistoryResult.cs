using BattleTech;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace PersistentMapAPI {
    public class HistoryResult {
        public DateTime? date;
        public string DateString;
        public Faction winner;
        public Faction loser;
        public int pointsTraded;
        public bool planetSwitched;
        public string system;

        [OnSerializing]
        void OnSerializing(StreamingContext context) {
            if (this.date == null)
                this.DateString = "";
            else
                this.DateString = this.date.Value.ToString("o", CultureInfo.InvariantCulture);
        }

        [OnDeserialized]
        void OnDeserializing(StreamingContext context) {
            if (this.DateString == null)
                this.date = null;
            else
                this.date = DateTime.ParseExact(this.DateString, "o", CultureInfo.InvariantCulture);
        }
    }

    
}
