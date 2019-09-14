using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using HBS.Collections;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PersistentMapClient {

    [HarmonyPatch(typeof(SG_Shop_Screen), "ChangeToStoreTypeState")]
    public static class SG_Shop_Screen_ChangeToStoreTypeState_Patch {
        static void Prefix(SG_Shop_Screen __instance, SG_Shop_Screen.StoreType newType, Shop.ShopType ___shopType, StarSystem ___theSystem, SimGameState ___simState) {
            try {
                if (newType == SG_Shop_Screen.StoreType.FactionStore && ___simState.IsFactionAlly(___theSystem.Owner, null)) {
                    ___theSystem.FactionShop.RefreshShop();
                }
            }
            catch (Exception e) {
               PersistentMapClient.Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Shop), "Initialize")]
    public static class Shop_Initialize_Patch {
        static bool Prefix(Shop __instance, Shop.ShopType shopType) {
            try {
                if (shopType == Shop.ShopType.Faction) {
                    Traverse.Create(__instance).Property("ThisShopType").SetValue(shopType);
                    return false;
                } else {
                    return true;
                }
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(StarSystem), "RefreshShops")]
    public static class StarSystem_RefreshShops_Patch {
        static bool Prefix(StarSystem __instance) {
            try {
                __instance.RefreshShop(__instance.SystemShop);
                __instance.RefreshShop(__instance.BlackMarketShop);
                return false;
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
                return true;
            }
        }
    }

    
    [HarmonyPatch(typeof(Contract), "FinalizeSalvage")]
    public static class Contract_FinalizeSalvage_Patch {
        static void Postfix(Contract __instance, List<SalvageDef> ___finalPotentialSalvage) {
            try {
                SimGameState simulation = __instance.BattleTechGame.Simulation;
                if (simulation.IsFactionAlly(__instance.Override.employerTeam.faction, null)) {
                    Web.PostUnusedSalvage(___finalPotentialSalvage, __instance.Override.employerTeam.faction);
                }
            }
            catch (Exception e) {
               PersistentMapClient.Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Shop), "RefreshShop")]
    public static class Shop_RefreshShop_Patch {
        static bool Prefix(Shop __instance, StarSystem ___system, SimGameState ___Sim) {
            try {
                if (__instance.ThisShopType == Shop.ShopType.Faction && ___Sim.IsFactionAlly(___system.Owner, null)) {
                    __instance.Clear();
                    if (!Fields.LastUpdate.ContainsKey(___system.Owner)) {
                        Fields.LastUpdate.Add(___system.Owner, DateTime.MinValue);
                    }
                    if (!Fields.currentShops.ContainsKey(___system.Owner)) {
                        Fields.currentShops.Add(___system.Owner, new List<ShopDefItem>());
                    }
                    if (Fields.LastUpdate[___system.Owner].AddMinutes(Fields.UpdateTimer) < DateTime.UtcNow) {
                        Fields.currentShops[___system.Owner] = Web.GetShopForFaction(___system.Owner);
                    }
                    foreach (ShopDefItem item in Fields.currentShops[___system.Owner]) {
                        DataManager dataManager = ___Sim.DataManager;
                        if (item.Count > 0) {
                            switch (item.Type) {
                                case ShopItemType.Weapon: {
                                        if (dataManager.WeaponDefs.Exists(item.ID)) {
                                            __instance.ActiveInventory.Add(item);
                                        }
                                        break;
                                    }
                                case ShopItemType.AmmunitionBox: {
                                        if (dataManager.AmmoBoxDefs.Exists(item.ID)) {
                                            __instance.ActiveInventory.Add(item);
                                        }
                                        break;
                                    }
                                case ShopItemType.HeatSink: {

                                        if (dataManager.HeatSinkDefs.Exists(item.ID)) {
                                            __instance.ActiveInventory.Add(item);
                                        }
                                        break;
                                    }
                                case ShopItemType.JumpJet: {
                                        if (dataManager.JumpJetDefs.Exists(item.ID)) {
                                            __instance.ActiveInventory.Add(item);
                                        }
                                        break;

                                    }
                                case ShopItemType.MechPart: {
                                        if (dataManager.MechDefs.Exists(item.ID)) {
                                            __instance.ActiveInventory.Add(item);
                                        }
                                        break;
                                    }
                                case ShopItemType.Upgrade: {
                                        if (dataManager.UpgradeDefs.Exists(item.ID)) {
                                            __instance.ActiveInventory.Add(item);
                                        }
                                        break;
                                    }
                                case ShopItemType.Mech: {
                                        if (dataManager.MechDefs.Exists(item.ID)) {
                                            __instance.ActiveInventory.Add(item);
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                    Fields.LastUpdate[___system.Owner] = DateTime.UtcNow;
                    return false;
                }
                return true;
            }
            catch (Exception e) {
               PersistentMapClient.Logger.LogError(e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Shop), "SellInventoryItem", new Type[] { typeof(ShopDefItem) })]
    public static class Shop_SellInventoryItem_Patch {
        static void Postfix(Shop __instance, ShopDefItem item, bool __result, StarSystem ___system, SimGameState ___Sim) {
            try {
                if (__result && __instance.ThisShopType == Shop.ShopType.Faction && ___Sim.IsFactionAlly(___system.Owner, null)) {
                    if (Fields.currentShopSold.Key == Faction.INVALID_UNSET) {
                        Fields.currentShopSold = new KeyValuePair<Faction, List<ShopDefItem>>(___system.Owner, new List<ShopDefItem>());
                    }
                    Fields.currentShopSold.Value.Add(item);
                }
            }
            catch (Exception e) {
               PersistentMapClient.Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SG_Shop_Screen), "RefreshStoreTypeButtons")]
    public static class StarSystem_RefreshStoreTypeButtons_Patch {
        static void Postfix(SG_Shop_Screen __instance, StarSystem ___theSystem, SimGameState ___simState, HBSDOTweenStoreTypeToggle ___FactionStoreButton, GameObject ___LowRepFactionOverlay, GameObject ___SystemStoreButtonHoldingObject, GameObject ___FactionStoreButtonHoldingObject) {
            try {
                if (___simState.IsFactionAlly(___theSystem.Owner, null)) {
                    ___FactionStoreButton.FillInByFaction(___simState, ___theSystem.Owner);
                    ___SystemStoreButtonHoldingObject.SetActive(true);
                    ___FactionStoreButtonHoldingObject.SetActive(true);
                    ___FactionStoreButton.SetState(ButtonState.Enabled, false);
                    ___LowRepFactionOverlay.SetActive(false);
                }
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SG_Shop_Screen), "FillInFactionData")]
    public static class StarSystem_FillInFactionData_Patch {
        static bool Prefix(SG_Shop_Screen __instance, SG_Stores_StoreImagePanel ___StoreImagePanel, StarSystem ___theSystem) {
            try {
                ___StoreImagePanel.FillInData(SG_Shop_Screen.StoreType.FactionStore, ___theSystem.Owner);
                return false;
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
                return true;
            }
        }
    }
    

    [HarmonyPatch(typeof(SG_Shop_Screen), "OnCompleted")]
    public static class SG_Shop_Screen_OnCompleted_Patch {
        static void Postfix() {
            try {
                if (Fields.currentShopSold.Key != Faction.INVALID_UNSET) {
                    Web.PostSoldItems(Fields.currentShopSold.Value, Fields.currentShopSold.Key);
                    Fields.currentShopSold = new KeyValuePair<Faction, List<ShopDefItem>>(Faction.INVALID_UNSET, new List<ShopDefItem>());
                }
                if (Fields.currentShopBought.Key != Faction.INVALID_UNSET) {
                    Web.PostBuyItems(Fields.currentShopBought.Value, Fields.currentShopBought.Key);
                    foreach (string id in Fields.currentShopBought.Value) {
                        ShopDefItem match = Fields.currentShops[Fields.currentShopBought.Key].FirstOrDefault(x => x.ID.Equals(id));
                        if (match != null) {
                            if (match.Count == 0) {
                                Fields.currentShops[Fields.currentShopBought.Key].Remove(match);
                            }
                        }
                    }
                    Fields.currentShopBought = new KeyValuePair<Faction, List<string>>(Faction.INVALID_UNSET, new List<string>());               
                }
            }
            catch (Exception e) {
               PersistentMapClient.Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Shop), "Purchase")]
    public static class Shop_Purchase_Patch {
        static void Postfix(Shop __instance, string id, bool __result, StarSystem ___system, SimGameState ___Sim) {
            try {
                    if (__result && __instance.ThisShopType == Shop.ShopType.Faction && ___Sim.IsFactionAlly(___system.Owner, null)) {
                    if (Fields.currentShopBought.Key == Faction.INVALID_UNSET) {
                        Fields.currentShopBought = new KeyValuePair<Faction, List<string>>(___system.Owner, new List<string>());
                    }
                    Fields.currentShopBought.Value.Add(id);
                }
            }
            catch (Exception e) {
               PersistentMapClient.Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Shop), "GetPrice")]
    public static class Shop_GetPrice_Patch {
        static void Postfix(Shop __instance, ShopDefItem item, Shop.ShopType shopType, ref int __result) {
            try {
                if (shopType == Shop.ShopType.Faction) {
                    DescriptionDef itemDescription = __instance.GetItemDescription(item);
                    __result = Mathf.CeilToInt(itemDescription.Cost * item.DiscountModifier);
                }
            }
            catch (Exception e) {
                PersistentMapClient.Logger.LogError(e);
            }
        }
    }
}