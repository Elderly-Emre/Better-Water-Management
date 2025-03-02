﻿using Il2Cpp;
using HarmonyLib;
using UnityEngine;
using Il2CppTLD.Gear;

namespace BetterWaterManagement;

//This patch appends fill labels to the names of water containers
[HarmonyPatch(typeof(GearItemData), nameof(GearItemData.GetDisplayNameWithCondition))]
internal class ConditionTableManager_GetDisplayNameWithCondition
{
	internal static void Postfix(GearItemData __instance, ref string __result)
	{
		LiquidItem liquidItem = __instance.PrefabReference.LoadAssetAsync<GameObject>().WaitForCompletion().GetComponent<GearItem>().m_LiquidItem;
		if (!liquidItem || liquidItem.m_LiquidType != GearLiquidTypeEnum.Water)
		{
			return;
		}

		if (Water.IsEmpty(liquidItem))
		{
			__result += " - " + Localization.Get("GAMEPLAY_BWM_Empty");
		}
		else if (liquidItem.m_LiquidQuality == LiquidQuality.Potable)
		{
			__result += " - " + Localization.Get("GAMEPLAY_WaterPotable");
		}
		else
		{
			__result += " - " + Localization.Get("GAMEPLAY_WaterUnsafe");
		}
	}
}

//This and the next patch makes the hidden inventory watersupply weightless without interfering with other watersupplies.
[HarmonyPatch(typeof(GearItem), nameof(GearItem.GetSingleItemWeightKG))]
internal class GearItem_GetSingleItemWeightKG
{
	internal static void Postfix(GearItem __instance, ref float __result)
	{
		var potableWaterSupply = GameManager.GetInventoryComponent().m_WaterSupplyPotable;
		var nonPotableWaterSupply = GameManager.GetInventoryComponent().m_WaterSupplyNotPotable;
		if (__instance == potableWaterSupply || __instance == nonPotableWaterSupply)
		{
			__result = 0;
		}
	}
}

//This and the previous patch makes the hidden inventory watersupply weightless without interfering with other watersupplies.
[HarmonyPatch(typeof(GearItem), nameof(GearItem.GetItemWeightKG), new Type[] { typeof(bool) })]
[HarmonyPatch(typeof(GearItem), nameof(GearItem.GetItemWeightKG), new Type[] { typeof(float),typeof(bool) })]
internal class GearItem_GetItemWeightKG
{
	internal static void Postfix(GearItem __instance, ref float __result)
	{
		var potableWaterSupply = GameManager.GetInventoryComponent().m_WaterSupplyPotable;
		var nonPotableWaterSupply = GameManager.GetInventoryComponent().m_WaterSupplyNotPotable;
		if (__instance == potableWaterSupply || __instance == nonPotableWaterSupply)
		{
			__result = 0;
		}
	}
}

//Updates the sound and texture of a water bottle
//The sound and texture depend on the emptiness and quality of water inside.
//[HarmonyPatch(typeof(GearItem), nameof(GearItem.Awake))]
//internal class GearItem_Awake
//{
//	internal static void Postfix(GearItem __instance)
//	{
//		LiquidItem liquidItem = __instance.m_LiquidItem;
//		if (liquidItem && liquidItem.m_LiquidType == GearLiquidTypeEnum.Water)
//		{
//			MelonLoader.MelonLogger.Warning("GearItem_Awake if | "+ __instance.name);
//			WaterUtils.UpdateWaterBottle(__instance);
//		}
//	}
//}

//Disables the water adjustments while loading
[HarmonyPatch(typeof(Inventory), nameof(Inventory.Deserialize))]
internal class Inventory_Deserialize
{
	internal static void Prefix()
	{
		Water.IgnoreChanges = true;
	}

	internal static void Postfix()
	{
		Water.IgnoreChanges = false;
		Water.AdjustWaterSupplyToWater();
	}
}

//Prevents Water Containers from being converted into one giant pile of water
[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddGear))]
internal class Inventory_AddGear
{
	private const GearLiquidTypeEnum ModWater = (GearLiquidTypeEnum)1000;

	internal static void Postfix(GearItem gi)
	{
		LiquidItem liquidItem = gi.gameObject.GetComponent<LiquidItem>();
		if (liquidItem && liquidItem.m_LiquidType == ModWater)
		{

			liquidItem.m_LiquidType = GearLiquidTypeEnum.Water;
			Water.AdjustWaterSupplyToWater();
		}
	}

	internal static void Prefix(Inventory __instance, GearItem gi)
	{
		LiquidItem liquidItem = gi.gameObject.GetComponent<LiquidItem>();
		if (liquidItem && liquidItem.m_LiquidType == GearLiquidTypeEnum.Water)
		{
			liquidItem.m_LiquidType = ModWater;
		}
	}
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddToPotableWaterSupply))]
internal class Inventory_AddToPotableWaterSupply
{
	internal static void Postfix(float volumeLiters)
	{
		Water.AdjustWaterToWaterSupply();
	}
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddToWaterSupply))]
internal class Inventory_AddToWaterSupply
{
	internal static void Postfix(float numLiters, LiquidQuality quality)
	{
		Water.AdjustWaterToWaterSupply();
	}
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveGear), new Type[] { typeof(GameObject) })]
internal class Inventory_RemoveGear
{
	internal static void Postfix(GameObject go)
	{
		if (go == null)
		{
			return;
		}
		LiquidItem liquidItem = go.GetComponent<LiquidItem>();
		if (liquidItem && liquidItem.m_LiquidType == GearLiquidTypeEnum.Water)
		{
			Water.AdjustWaterSupplyToWater();
		}
	}
}

//repeats the previous patch because the method is overloaded
[HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveGear), new Type[] { typeof(GameObject), typeof(bool) })]
internal class Inventory_RemoveGear2
{
	internal static void Postfix(GameObject go)
	{
		if (go == null)
		{
			return;
		}
		LiquidItem liquidItem = go.GetComponent<LiquidItem>();
		if (liquidItem && liquidItem.m_LiquidType == GearLiquidTypeEnum.Water)
		{
			Water.AdjustWaterSupplyToWater();
		}
	}
}

[HarmonyPatch(typeof(Panel_Container), nameof(Panel_Container.IgnoreWaterSupplyItem))]//runs constantly while the container menu is open
internal class Panel_Container_IgnoreWaterSupplyItem
{
	internal static bool Prefix(WaterSupply ws, ref bool __result)
	{
		//Implementation.Log("Panel_Container -- IgnoreWaterSupplyItem");
		__result = ws != null && ws.GetComponent<LiquidItem>() == null; //the water supply exists but has no liquid component
		return false;
	}
}

[HarmonyPatch(typeof(Panel_Inventory), nameof(Panel_Inventory.IgnoreWaterSupplyItem))]//runs constantly while the inventory is open
internal class Panel_Inventory_IgnoreWaterSupplyItem
{
	internal static bool Prefix(WaterSupply ws, ref bool __result)
	{
		__result = ws != null && ws.GetComponent<LiquidItem>() == null; //the water supply exists but has no liquid component
		return false;
	}
}

[HarmonyPatch(typeof(Panel_PickWater), nameof(Panel_PickWater.Enable))]
internal class Panel_PickWater_Enable
{
	internal static void Postfix(Panel_PickWater __instance)
	{
		PickWater.UpdateDrinking(__instance);
	}
}

[HarmonyPatch(typeof(Panel_PickWater), nameof(Panel_PickWater.Refresh))]
internal class Panel_PickWater_Refresh
{
	internal static void Prefix(Panel_PickWater __instance)
	{
		PickWater.ClampAmount(__instance);
	}

	internal static void Postfix(Panel_PickWater __instance)
	{
		PickWater.UpdateButtons(__instance);
	}
}

[HarmonyPatch(typeof(Panel_PickWater), nameof(Panel_PickWater.OnTakeWaterComplete))]//runs after taking water from a toilet
internal class Panel_PickWater_OnTakeWaterComplete
{
	internal static void Postfix()
	{
		//Implementation.Log("Panel_PickWater -- OnTakeWaterComplete");
		Water.AdjustWaterToWaterSupply();
	}
}

[HarmonyPatch(typeof(Panel_PickWater), nameof(Panel_PickWater.SetWaterSourceForTaking))]
internal class Panel_PickWater_SetWaterSourceForTaking
{
	internal static void Postfix(Panel_PickWater __instance)
	{
		PickWater.UpdateCapacityInfo(__instance);
	}
}

[HarmonyPatch(typeof(Panel_PickWater), nameof(Panel_PickWater.Initialize))]
internal class Panel_PickWater_Initialize
{
	internal static void Postfix(Panel_PickWater __instance)
	{
		PickWater.Prepare(__instance);
	}
}
//* Drinking from toilets.
[HarmonyPatch(typeof(Panel_PickWater), nameof(Panel_PickWater.Update))]
internal class Panel_PickWater_Update
{
	internal static void Postfix(Panel_PickWater __instance)
	{
		if (InputManager.GetEquipPressed(__instance))
		{
			var waterSource = __instance.m_WaterSource;
			if (!waterSource)
			{
				Implementation.LogError("UpdateCapacityInfo: Could not find WaterSource");
				return;
			}
			if (Water.IsNone(waterSource.m_CurrentLiters))
			{
				HUDMessage.AddMessage(Localization.Get("GAMEPLAY_BWM_Empty"));
				GameAudioManager.PlayGUIError();
				return;
			}
			float waterVolumeToDrink = GameManager.GetPlayerManagerComponent().CalculateWaterVolumeToDrink(waterSource.m_CurrentLiters);
			if (Water.IsNone(waterVolumeToDrink))
			{
				GameAudioManager.PlayGUIError();
				HUDMessage.AddMessage(Localization.Get("GAMEPLAY_Youarenotthirsty"));
				return;
			}
			GameAudioManager.PlayGuiConfirm();
			WaterSupply waterSupply;
			if (waterSource.GetQuality() == LiquidQuality.Potable)
			{
				waterSupply = __instance.InstantiateWaterSupply(__instance.m_WaterSupply_Potable).m_WaterSupply;
			}
			else
			{
				waterSupply = __instance.InstantiateWaterSupply(__instance.m_WaterSupply_NonPotable).m_WaterSupply;
			}
			waterSupply.m_VolumeInLiters = waterSource.m_CurrentLiters;
			waterSource.RemoveLiters(waterVolumeToDrink);
			GameManager.GetPlayerManagerComponent().DrinkFromWaterSupply(waterSupply, waterSupply.m_VolumeInLiters);
			UnityEngine.Object.Destroy(waterSupply.gameObject);
			__instance.ExitInterface();
		}
	}
}

[HarmonyPatch(typeof(Utils), nameof(Utils.GetInventoryIconTexture), new Type[] { typeof(GearItem) })]
[HarmonyPatch(typeof(Utils), nameof(Utils.GetInventoryIconTexture), new Type[] { typeof(GearItem), typeof(float) })]
internal class Utils_GetInventoryIconTexture
{
	internal static bool Prefix(GearItem gi, ref Texture2D __result)
	{
		LiquidItem liquidItem = gi.m_LiquidItem;
		if (!liquidItem || liquidItem.m_LiquidType != GearLiquidTypeEnum.Water)
		{
			return true;
		}

		string textureName = gi.name.Replace("GEAR_", "ico_GearItem__") + WaterUtils.GetWaterSuffix(liquidItem);
		__result = Utils.GetInventoryGridIconTexture(textureName);

		return __result == null;
	}
}

internal class WaterSourceSupply : WaterSupply
{
	internal WaterSource waterSource;

	internal void SetWaterSource(WaterSource waterSource)
	{
		this.waterSource = waterSource;
		m_VolumeInLiters = waterSource.GetVolumeLiters();
		m_WaterQuality = waterSource.GetQuality();
		m_DrinkingAudio = "Play_Slurping1";
		m_TimeToDrinkSeconds = 4;
	}

	internal void UpdateWaterSource()
	{
		waterSource.RemoveLiters(waterSource.GetVolumeLiters() - m_VolumeInLiters);
	}
}