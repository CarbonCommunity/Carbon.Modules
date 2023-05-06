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

[HarmonyPatch(typeof(ResearchTable), "DoResearch")]
public class ResearchTable_DoResearch
{
	[HarmonyPrefix]
	private static bool Prefix(ref ResearchTable __instance)
	{
		__instance.researchDuration = Settings.Config.ResearchDuration;
		return true;
	}
}
