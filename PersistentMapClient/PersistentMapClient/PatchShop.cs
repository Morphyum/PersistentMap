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
    /*
    [HarmonyPatch(typeof(SGTravelManager), "DisplayEnteredOrbitPopup")]
    public static class SGTravelManager_DisplayEnteredOrbitPopup_Patch {
        static bool Prefix(SGTravelManager __instance, SimGameState ___simState) {
            try {
                bool flag = true;
                Faction owner = ___simState.CurSystem.Owner;
                SimGameReputation reputation = ___simState.GetReputation(owner);
                if (reputation <= SimGameReputation.LOATHED) {
                    flag = false;
                }
                if (flag) {
                    Action actionArrive = (Action)Delegate.CreateDelegate(typeof(Action), __instance, "OnArrivedAtPlanet");
                    Action actionSave = (Action)Delegate.CreateDelegate(typeof(Action), __instance, "SaveNow");
                    ___simState.GetInterruptQueue().QueueTravelPauseNotification("Arrived", Strings.T("We've arrived at {0}.", new object[]
                    {
                    ___simState.Starmap.CurPlanet.System.Def.Description.Name
                    }), ___simState.GetCrewPortrait(SimGameCrew.Crew_Sumire), "notification_travelcomplete", actionArrive, "Visit Store", actionSave, "Continue");
                }
                else {
                    Action actionSave = (Action)Delegate.CreateDelegate(typeof(Action), __instance, "SaveNow");
                    ___simState.GetInterruptQueue().QueueTravelPauseNotification("Arrived", Strings.T("We've arrived at {0}.", new object[]
                    {
                    ___simState.Starmap.CurPlanet.System.Def.Description.Name
                    }), ___simState.GetCrewPortrait(SimGameCrew.Crew_Sumire), "notification_travelcomplete", actionSave, "Continue", null, null);
                }
                if (!___simState.TimeMoving) {
                    ___simState.GetInterruptQueue().DisplayIfAvailable();
                }
                return false;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return true;
            }
        }

    }

    [HarmonyPatch(typeof(SimGameState), "SetSimRoomState")]
    public static class SimGameState_SetSimRoomState_Patch {
        static void Prefix(SimGameState __instance, DropshipLocation state) {
            try {
                if (state == DropshipLocation.SHOP) {
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
        static void Postfix(Shop __instance, StarSystem ___system, SimGameState ___Sim) {
            try {
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
                    switch (item.Type) {
                        case ShopItemType.Weapon: {
                                if (dataManager.WeaponDefs.Exists(item.ID)) {
                                    __instance.ActiveSpecials.Add(item);
                                }
                                break;
                            }
                        case ShopItemType.AmmunitionBox: {
                                if (dataManager.AmmoBoxDefs.Exists(item.ID)) {
                                    __instance.ActiveSpecials.Add(item);
                                }
                                break;
                            }
                        case ShopItemType.HeatSink: {

                                if (dataManager.HeatSinkDefs.Exists(item.ID)) {
                                    __instance.ActiveSpecials.Add(item);
                                }
                                break;
                            }
                        case ShopItemType.JumpJet: {
                                if (dataManager.JumpJetDefs.Exists(item.ID)) {
                                    __instance.ActiveSpecials.Add(item);
                                }
                                break;

                            }
                        case ShopItemType.MechPart: {
                                if (dataManager.MechDefs.Exists(item.ID)) {
                                    __instance.ActiveSpecials.Add(item);
                                }
                                break;
                            }
                        case ShopItemType.Upgrade: {
                                if (dataManager.UpgradeDefs.Exists(item.ID)) {
                                    __instance.ActiveSpecials.Add(item);
                                }
                                break;
                            }
                        case ShopItemType.Mech: {
                                if (dataManager.MechDefs.Exists(item.ID)) {
                                    __instance.ActiveSpecials.Add(item);
                                }
                                break;
                            }
                    }
                }
                Fields.LastUpdate[___system.Owner] = DateTime.UtcNow;
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
                        TagSet filteredRequirements = new TagSet(shopDef.RequirementTags);
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
                    if (Fields.currentShopSold.Key == Faction.INVALID_UNSET) {
                        Fields.currentShopSold = new KeyValuePair<Faction, List<ShopDefItem>>(___system.Owner, new List<ShopDefItem>());
                    }
                    Fields.currentShopSold.Value.Add(item);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
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
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(Shop), "Purchase")]
    public static class Shop_Purchase_Patch {
        static void Postfix(string id, bool __result, StarSystem ___system) {
            try {
                if (__result) {
                    if (Fields.currentShopBought.Key == Faction.INVALID_UNSET) {
                        Fields.currentShopBought = new KeyValuePair<Faction, List<string>>(___system.Owner, new List<string>());
                    }
                    Fields.currentShopBought.Value.Add(id);
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
        static bool Prefix(SGLocationWidget __instance, SimGameState ___simState, HBSDOTweenButton ___storeButton, StarSystem currSystem, GameObject ___NothingToBuyStoreOverlay, GameObject ___LowRepStoreOverlay) {
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

    [HarmonyPatch(typeof(SGNavigationButton), "ManageShopFlyout")]
    public static class SGNavigationButton_ManageShopFlyout_Patch {
        static bool Prefix(SGNavigationButton __instance, SimGameState ___simState) {
            try {
                Faction owner = ___simState.CurSystem.Owner;
                SimGameReputation reputation = ___simState.GetReputation(owner);
                bool flag = true;
                if (reputation <= SimGameReputation.LOATHED) {
                    flag = false;
                }
                if (flag) {
                    __instance.AddFlyoutButton("Store", DropshipMenuType.Shop);
                }
                return false;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return true;
            }
        }
    }
    */
}