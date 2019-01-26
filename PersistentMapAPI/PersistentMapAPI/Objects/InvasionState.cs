using BattleTech;
using System;

namespace PersistentMapAPI {
    public class InvasionState {
        public Faction attacker = Faction.INVALID_UNSET;
        public Faction defender;
        public int stage = 0;
        public int percentage = 0;
        public bool currentlyInvaded = false;


        public void changePercentage(int change) {
            if (currentlyInvaded) {
                this.percentage += change;
                if (this.percentage >= 100) {
                    stage++;
                    this.percentage -= 100;
                }
                else if (this.percentage < 100) {
                    stage--;
                    this.percentage += 100;
                    this.percentage = 100 - this.percentage;
                }
                //Attacker took planet
                if(stage > 5) {
                    this.defender = this.attacker;
                    this.attacker = Faction.INVALID_UNSET;
                    this.stage = 0;
                    this.percentage = 0;
                    this.currentlyInvaded = false;
                }
                //Defender repelled invasion
                else if (stage < 0){
                    this.attacker = Faction.INVALID_UNSET;
                    this.stage = 0;
                    this.percentage = 0;
                    this.currentlyInvaded = false;
                }
            }
        }
    }
}
