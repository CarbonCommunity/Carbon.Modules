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
	public float CraftingSpeedMultiplier = 1f;
	public float RecycleTick = 5f;
	public float ResearchDuration = 10f;
	public float VendingMachineBuyDuration = 2.5f;

	public Dictionary<string, float> Excavator = new()
	{
		{ "*", 1.0f }
	};

	public Dictionary<string, float> Gather = new()
	{
		{ "*",           1.00f },
		{ "skull.wolf",  1.00f },
		{ "skull.human", 1.00f },
	};

	public Dictionary<string, float> Pickup = new()
	{
		{ "*",                 1.00f },
		{ "seed.black.berry",  1.00f },
		{ "seed.blue.berry",   1.00f },
		{ "seed.corn",         1.00f },
		{ "seed.green.berry",  1.00f },
		{ "seed.hemp",         1.00f },
		{ "seed.potato",       1.00f },
		{ "seed.pumpkin",      1.00f },
		{ "seed.red.berry",    1.00f },
		{ "seed.white.berry",  1.00f },
		{ "seed.yellow.berry", 1.00f },
	};

	public Dictionary<string, float> Quarry = new()
	{
		{ "*", 1.0f }
	};

	public Dictionary<string, float> Trap = new()
	{
		{ "*", 1.00f },
	};
}
