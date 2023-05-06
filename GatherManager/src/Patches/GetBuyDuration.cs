using System;
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

[HarmonyPatch(typeof(VendingMachine), "GetBuyDuration")]
public class VendingMachine_GetBuyDuration
{
	[HarmonyPrefix]
	private static bool Prefix(ref float __result)
	{
		__result = Settings.Config.VendingMachineBuyDuration;
		return false;
	}
}
