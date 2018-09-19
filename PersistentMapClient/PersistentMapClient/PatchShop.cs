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

    [HarmonyPatch(typeof(SimGameState), "SetSimRoomState")]
    public static class SimGameState_SetSimRoomState_Patch {
        static void Prefix(SimGameState __instance, DropshipLocation state) {
            try {
                if(state == DropshipLocation.SHOP) {
                    if (__instance.CurSystem.Shop == null) {
                        __instance.CurSystem.InitializeShop();
                    }
                    else {
                        __instance.CurSystem.Shop.UpdateShop(true);
                    }
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "FinalizeSalvage")]
    public static class Contract_FinalizeSalvage_Patch {
        static void Postfix(Contract __instance, List<SalvageDef> ___finalPotentialSalvage) {
            try {
                Web.PostUnusedSalvage(___finalPotentialSalvage, __instance.Override.employerTeam.faction);
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Shop), "UpdateShop")]
    public static class Shop_UpdateShop_Patch {
        static void Postfix(Shop __instance, StarSystem ___system) {
            try {
                foreach (ShopDefItem item in Web.GetShopForFaction(___system.Owner)) {
                    __instance.ActiveSpecials.Add(item);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
    [HarmonyPatch(typeof(StarSystem), "OnSystemChange")]
    public static class StarSystem_OnSystemChange_Patch {
        static bool Prefix() {
            try {
                return false;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(StarSystem), "SetNewStarSystemDef")]
    public static class StarSystem_SetNewStarSystemDef_Patch {
        static void Prefix(ref bool resetShops) {
            try {
                resetShops = false;
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

    [HarmonyPatch(typeof(Shop), "SellInventoryItem", new Type[] { typeof(ShopDefItem) })]
    public static class Shop_SellInventoryItem_Patch {
        static void Postfix(ShopDefItem item, bool __result, StarSystem ___system) {
            try {
                if (__result) {
                    Web.PostSoldItem(item, ___system.Owner);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Shop), "Purchase")]
    public static class Shop_Purchase_Patch {
        static void Postfix(string id, bool __result, StarSystem ___system) {
            try {
                if (__result) {
                    Web.PostBuyItem(id, ___system.Owner);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(StarSystem), "RefreshSystem")]
    public static class StarSystem_RefreshSystem_Patch {
        static bool Prefix(StarSystem __instance) {
            try {
                __instance.GeneratePilots(__instance.Sim.Constants.Story.DefaultPilotsPerSystem);
                __instance.GenerateTechs(__instance.Sim.Constants.Story.DefaultMechTechsPerSystem, true);
                __instance.GenerateTechs(__instance.Sim.Constants.Story.DefaultMedTechsPerSystem, false);
                __instance.RefreshBreadcrumbs();
                return false;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return true;
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
        static bool Prefix(SGLocationWidget __instance, SimGameState ___simState,HBSDOTweenButton ___storeButton, StarSystem currSystem, GameObject ___NothingToBuyStoreOverlay, GameObject ___LowRepStoreOverlay) {
            try {
                Faction owner = currSystem.Owner;
                SimGameReputation reputation = ___simState.GetReputation(owner);
                if (reputation <= SimGameReputation.LOATHED) {
                    ___LowRepStoreOverlay.SetActive(true);
                    ___NothingToBuyStoreOverlay.SetActive(false);
                    ___storeButton.SetState(ButtonState.Disabled, false);
                }
                else {
                    ___LowRepStoreOverlay.SetActive(false);
                    ___NothingToBuyStoreOverlay.SetActive(false);
                    ___storeButton.SetState(ButtonState.Enabled, false);
                }
                return false;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return true;
            }
        }
    }

}