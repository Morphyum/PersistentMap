using BattleTech;
using BattleTech.Framework;
using BattleTech.Save;
using BattleTech.UI;
using Harmony;
using HBS;
using PersistentMapAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PersistentMapClient {
    
   /* [HarmonyPatch(typeof(Shop), "UpdateShop")]
    public static class Shop_UpdateShop_Patch {
        static void Postfix(Shop __instance) {
            try {
                ShopDefItem temp = new ShopDefItem();
                temp.ID = "Ammo_AmmunitionBox_Generic_AC2";
                temp.Type = ShopItemType.AmmunitionBox;
                temp.DiscountModifier = 0.25f;
                temp.Count = 1;
                __instance.ActiveInventory.Clear();
                __instance.ActiveInventory.Add(temp);
                temp = new ShopDefItem();
                temp.ID = "Weapon_Laser_MediumLaser_0-STOCK";
                temp.Type = ShopItemType.Weapon;
                temp.DiscountModifier = 0.0001f;
                temp.Count = 2;
                __instance.ActiveSpecials.Clear();
                __instance.ActiveSpecials.Add(temp);
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Shop), "GetPrice")]
    public static class Shop_GetPrice_Patch {
        static void Prefix(ref Shop.PurchaseType purchaseType) {
            try {
                purchaseType = Shop.PurchaseType.Special;
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }*/
}