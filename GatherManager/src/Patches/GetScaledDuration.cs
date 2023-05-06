using Config;
using HarmonyLib;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * Copyright (c) 2022 kasvoton
 * All rights reserved.
 *
 */

namespace Patches;
#pragma warning disable IDE0051

[HarmonyPatch(typeof(ItemCrafter), "GetScaledDuration")]
public class ItemCrafter_GetScaledDuration
{
	[HarmonyPostfix]
	private static void Postfix(ref float __result)
	{
		__result /= Settings.Config.CraftingSpeedMultiplier;
	}
}
