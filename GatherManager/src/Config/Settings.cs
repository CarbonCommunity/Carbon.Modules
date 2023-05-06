using System.IO;
using Newtonsoft.Json;
using Utility;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * Copyright (c) 2022 kasvoton
 * All rights reserved.
 *
 */

namespace Config;

public sealed class Settings
{
	private static bool Loaded;
	public static Default Config;

	private static readonly string Home
		= Path.Combine("carbon", "modules");

	private static readonly string Location
		= Path.Combine(Home, Properties.GetProduct() + ".json");

	public static void LoadConfig()
	{
		if (Loaded)
		{
			return;
		}

		try
		{
			Config = JsonConvert.DeserializeObject<Default>(File.ReadAllText(Location));
			Logger.Log($"Configuration file was successfully loaded.");
		}
		catch
		{
			Config = new Default();
			Logger.Warning($"A new default configuration file was created at: {Location}");
		}
		finally
		{
			Loaded = true;
			SaveConfig();
		}
	}

	public static void SaveConfig()
	{
		try
		{
			File.WriteAllText(Location, JsonConvert.SerializeObject(Config, Formatting.Indented));
		}
		catch
		{
			Logger.Error($"Unknown error while saving the configuration file.");
		}
	}
}

