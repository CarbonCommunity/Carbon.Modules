using System;
using System.Collections.Generic;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * Copyright (c) 2022 kasvoton
 * All rights reserved.
 *
 */

namespace Config;

public sealed class Default
{
	public float GlobalMultiplier = 1f;

	public HashSet<string> Blacklist = new()
	{
		"water",
		"water.salt"
	};

	public Dictionary<ItemCategory, float> Categories = new()
	{
		{ ItemCategory.Ammunition, 1 },
		{ ItemCategory.Attire, 1 },
		{ ItemCategory.Component, 1 },
		{ ItemCategory.Construction, 1 },
		{ ItemCategory.Electrical, 1 },
		{ ItemCategory.Food, 1 },
		{ ItemCategory.Fun, 1 },
		{ ItemCategory.Items, 1 },
		{ ItemCategory.Medical, 1 },
		{ ItemCategory.Misc, 1 },
		{ ItemCategory.Resources, 1 },
		{ ItemCategory.Tool, 1 },
		{ ItemCategory.Traps, 1 },
		{ ItemCategory.Weapon, 1 }
	};

	public Dictionary<string, float> Items = new()
	{
		{ "explosive.timed", 1 }
	};
}
