using System;
using System.Reflection;
using API.Assembly;
using API.Attributes;
using Config;
using HarmonyLib;
using Utility;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * Copyright (c) 2022 kasvoton
 * All rights reserved.
 *
 */

[Metadata.Product("Gather Manager", "kasvoton", "1.0.0.0")]
[Metadata.Description("Gather manager for modded servers")]

public sealed partial class GatherManagerModule : ICarbonModule
{
	private Harmony _harmony;
	private static readonly string UUID
		= $"{Properties.GetProduct()}.{Properties.GetVersion()}";

	public void Awake(EventArgs args)
	{
		_harmony = new Harmony(UUID);
		Settings.LoadConfig();
	}

	public void OnLoaded(EventArgs args)
	{
		Logger.Log(">> OnLoaded");
	}

	public void OnUnloaded(EventArgs args)
	{
		Logger.Log(">> OnUnloaded");
	}

	public void OnEnable(EventArgs args)
	{
		Logger.Warning(">> OnEnable");
		var assembly = Assembly.GetExecutingAssembly();
		_harmony.PatchAll(assembly);
	}

	public void OnDisable(EventArgs args)
	{
		Logger.Warning(">> OnDisable");
		_harmony.UnpatchAll(UUID);
	}
}