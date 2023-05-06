using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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

[HarmonyPatch(typeof(Recycler), "StartRecycling")]
public class Recycler_StartRecycling
{
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> Instructions)
	{
		foreach (CodeInstruction Instruction in Instructions)
		{
			if (Instruction.opcode == OpCodes.Ldc_R4)
			{
				yield return new CodeInstruction(OpCodes.Ldc_R4, Settings.Config.RecycleTick);
			}
			else
			{
				yield return Instruction;
			}
		}
	}
}

