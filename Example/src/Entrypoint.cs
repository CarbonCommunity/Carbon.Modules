using System;
using API.Assembly;
using Utility;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

public sealed class ExampleModule : ICarbonModule
{
	public void Awake(EventArgs args)
	{
		Logger.Log(">> Awake");
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
	}

	public void OnDisable(EventArgs args)
	{
		Logger.Warning(">> OnDisable");
	}
}