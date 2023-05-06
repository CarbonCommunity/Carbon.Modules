using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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

[HarmonyPatch(typeof(MiningQuarry), "ProcessResources")]
public class MiningQuarry_ProcessResources
{
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> Instructions)
	{
		MethodInfo Target = AccessTools.Method(typeof(ItemManager), nameof(ItemManager.Create));

		foreach (CodeInstruction Instruction in Instructions)
		{
			MethodInfo methodInfo = Instruction.operand as MethodInfo;

			if (methodInfo == Target)
			{
				yield return new CodeInstruction(OpCodes.Ldc_I4_2);
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.Method(typeof(CreateItem), nameof(CreateItem.ByDefinition)));
			}
			else
			{
				yield return Instruction;
			}
		}
	}
}
