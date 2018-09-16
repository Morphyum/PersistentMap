using BattleTech;
using BattleTech.Framework;
using BattleTech.Save;
using BattleTech.UI;
using Harmony;
using HBS;
using HBS.Collections;
using PersistentMapAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PersistentMapClient {
    
   /*[HarmonyPatch(typeof(Shop), "UpdateShop")]
    public static class Shop_UpdateShop_Patch {
        static void Postfix(Shop __instance) {
            try {
                ShopDefItem temp = new ShopDefItem();
                temp.ID = "Weapon_Laser_MediumLaser_0-STOCK";
                temp.Type = ShopItemType.Weapon;
                temp.DiscountModifier = 0.0001f;
                temp.Count = 2;
                //__instance.ActiveSpecials.Clear();
                __instance.ActiveSpecials.Add(temp);
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(StarSystem), "InitializeShop")]
    public static class StarSystem_InitializeShop {
        static bool Prefix(StarSystem __instance) {
            try {
                List<ShopDef> list = new List<ShopDef>();
                foreach (string id in __instance.Sim.DataManager.Shops.Keys) {
                    ShopDef shopDef = __instance.Sim.DataManager.Shops.Get(id);
                    if (shopDef.RequirementTags.Contains(Fields.ShopFileTag)) {
                        TagSet filteredRequirements = shopDef.RequirementTags;
                        filteredRequirements.Remove(Fields.ShopFileTag);
                        if (SimGameState.MeetsTagRequirements(filteredRequirements, shopDef.ExclusionTags, __instance.Tags, null)) {
                            list.Add(shopDef);
                        }
                        else if (Helper.meetsNewReqs(__instance, filteredRequirements, shopDef.ExclusionTags, __instance.Tags)) {
                            list.Add(shopDef);
                        }
                    }
                }
                AccessTools.Method(typeof(StarSystem), "set_Shop").Invoke(__instance, new object[] { new Shop(__instance, list) });
                return false;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return false;
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
    }

    [HarmonyPatch(typeof(SGLocationWidget), "ManageShopButtonState")]
    public static class SGLocationWidget_ManageShopButtonState_Patch {
        static void Postfix(SGLocationWidget __instance, HBSDOTweenButton ___storeButton, StarSystem currSystem, GameObject ___NothingToBuyStoreOverlay, GameObject ___LowRepStoreOverlay) {
            try {
                if(___storeButton.State == ButtonState.Disabled) {
                    if (currSystem.Shop != null && currSystem.Shop.ActiveSpecials.Count >= 0) {
                        ___LowRepStoreOverlay.SetActive(false);
                        ___NothingToBuyStoreOverlay.SetActive(false);
                        ___storeButton.SetState(ButtonState.Enabled, false);
                    }
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }*/

}