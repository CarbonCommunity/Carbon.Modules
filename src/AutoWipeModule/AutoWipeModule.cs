using System;
using System.Collections.Generic;
using System.Linq;
using Carbon.Base;
using Carbon.Components;
using Carbon.Extensions;
using Newtonsoft.Json;
using Oxide.Core;
using Rust;
using Cronos;
using Oxide.Plugins;
using Random = UnityEngine.Random;

namespace Carbon.Modules;

public partial class AutoWipeModule : CarbonModule<AutoWipeConfig, AutoWipeData>
{
	public override string Name => "AutoWipe";
	public override VersionNumber Version => new(2, 0, 0);
	public override Type Type => typeof(AutoWipeModule);
	public override bool EnabledByDefault => false;

	private readonly char[] splitter = new[] { '|' };
	private readonly float wipeCooldown = 60 * 60;
	private readonly float wipeTick = 30;
	private Timer wipeTimer;

	public bool InCooldown() => (DateTime.UtcNow - new DateTime(DataInstance.LastWipeTime)).TotalSeconds <= wipeCooldown;

	public override void Load()
	{
		base.Load();

		if (!IsEnabled())
		{
			return;
		}

		if (InCooldown())
		{
			DataInstance.Wipe?.InitWorld(ConfigInstance.MapPool);
			return;
		}

		if (DataInstance.NextWipe == null)
		{
			DataInstance.NextWipe = GetUpcomingAvailableWipeImpl();
		}

		var currentWipe = DataInstance.Wipe;
		var wipe = DataInstance.NextWipe ?? currentWipe;
		var justWiped = wipe != null && !wipe.Equals(currentWipe);

		if (justWiped)
		{
			var config = ConfigInstance.GetWipeConfig(wipe);
			DataInstance.LastWipeTime = DateTime.UtcNow.Ticks;
			DataInstance.NextWipe = null;
			ConVar.Server.autoUploadMap = false;

			if (wipe.Temp)
			{
				ConfigInstance.Wipes.Remove(wipe);
				PutsWarn($"Removed map from list");
			}

			DataInstance.Wipe ??= new();
			wipe.CloneTo(DataInstance.Wipe);
			DataInstance.Wipe?.InitWorld(ConfigInstance.MapPool);

			using var table = new StringTable("wipename", "seed", "size", "url");
			table.AddRow(wipe.WipeName, wipe.ServerSeed, wipe.MapSize, wipe.MapUrl);
			PutsWarn($"New wipe detected!\n{table.ToStringMinimal()}");

			if (config.PostWipeCommands != null)
			{
				foreach (var command in config.PostWipeCommands)
				{
					if (string.IsNullOrEmpty(command))
						continue;
					ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), command);
				}
			}

			if (config.PostWipeDeletes != null)
			{
				foreach (var delete in config.PostWipeDeletes)
				{
					if (string.IsNullOrEmpty(delete))
						continue;

					if (OsEx.File.Exists(delete))
					{
						OsEx.File.Delete(delete);
						PutsWarn($"AutoWipe deleting scheduled file '{delete}'");
						continue;
					}

					if (OsEx.Folder.Exists(delete))
					{
						OsEx.Folder.Delete(delete);
						PutsWarn($"AutoWipe deleting scheduled directory '{delete}'");
					}
				}
			}

			Save();
		}
		else
		{
			DataInstance.Wipe?.InitWorld(ConfigInstance.MapPool);
		}
	}

	public override void OnServerInit(bool initial)
	{
		base.OnServerInit(initial);

		wipeTimer = Community.Runtime.Core.timer.Every(wipeTick, WipeTickImpl);
	}

	public override bool PreLoadShouldSave(bool newConfig, bool newData)
	{
		var invalidConfigCorrected = false;

		if (ConfigInstance.MapPool == null)
		{
			ConfigInstance.MapPool = new();
			invalidConfigCorrected = true;
		}

		return invalidConfigCorrected;
	}

	private void WipeTickImpl()
	{
		if (!IsEnabled() || InCooldown())
		{
			return;
		}

		DataInstance.NextWipe = GetUpcomingAvailableWipeImpl();

		if (DataInstance.NextWipe == null)
		{
			return;
		}

		if (DataInstance.NextWipe.Commands != null)
		{
			foreach (var command in DataInstance.NextWipe.Commands)
			{
				if (string.IsNullOrEmpty(command))
					continue;
				ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), command);
			}
		}

		wipeTimer.Destroy();
		Save();
	}

	private Wipe GetUpcomingAvailableWipeImpl()
	{
		for (int i = 0; i < ConfigInstance.Wipes.Count; i++)
		{
			var wipe = ConfigInstance.Wipes[i];
			if (wipe.ShouldWipe())
			{
				return wipe;
			}
		}
		return null;
	}

	[ConsoleCommand("autowipe.wipes", "Prints all available wipes present in the Wipes config property.")]
	[AuthLevel(2)]
	private void print_wipes(ConsoleSystem.Arg arg)
	{
		using var table = new StringTable("#", "wipename", "mapurl", "mapsize", "serverseed", "type", "temp", "nextwipe", "wipecommands");
		for (int i = 0; i < ConfigInstance.Wipes.Count; i++)
		{
			var wipe = ConfigInstance.Wipes[i];
			table.AddRow(i + 1, wipe.WipeName, wipe.MapUrl, wipe.MapSize,
				wipe.ServerSeed == 0 ? "random" : wipe.ServerSeed,
				wipe.Type, wipe.Temp ? "yes" : "no", wipe.Cron, wipe.Commands?.ToString("->"));
		}

		arg.ReplyWith(table.ToStringMinimal());
	}

	[ConsoleCommand("autowipe.delete", "Deletes an existent wipe present in the Wipes config property.")]
	[AuthLevel(2)]
	private void delete_wipe(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs())
		{
			arg.ReplyWith("Provide an index from 'autowipe.wipes'");
			return;
		}

		var i = arg.GetInt(0) - 1;
		if (i < 0 || i >= ConfigInstance.Wipes.Count)
		{
			arg.ReplyWith("Went above or below indexes available. Use numbers from 'autowipe.wipes`'");
			return;
		}

		ConfigInstance.Wipes.RemoveAt(i);
		Save();
		arg.ReplyWith("Removed wipe");
	}

	[ConsoleCommand("autowipe.add", "Adds a new wipe to the list.")]
	[AuthLevel(2)]
	private void add_wipe(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs(8))
		{
			arg.ReplyWith("You've got missing arguments. Please make sure to follow the following syntax:\n" +
			              "eg. autowipe.add \"<WipeName>\" \"<MapBrowserName>\" \"<MapUrl>\" \"<MapSize>\" \"<ServerSeed|0=random>\" \"<Type|0=fullwipe 1=mapwipe>\" \"<Temp|True/False>\" \"<Cron>\" \"<Commands>\"");
			return;
		}

		ConfigInstance.Wipes.Add(new()
		{
			WipeName = arg.GetString(0),
			MapBrowserName = arg.GetString(1),
			MapUrl = arg.GetString(2),
			MapSize = arg.GetInt(3),
			ServerSeed = arg.GetInt(4),
			Type = (WipeTypes)arg.GetInt(5),
			Temp = arg.GetBool(6),
			Cron = arg.GetString(7),
			Commands = arg.GetString(8).Split(splitter, StringSplitOptions.RemoveEmptyEntries)
		});
		Save();
		arg.ReplyWith("Added wipe");
	}

	[ConsoleCommand("autowipe.maps", "Prints all available map urls present in the MapPool config property.")]
	[AuthLevel(2)]
	private void print_maps(ConsoleSystem.Arg arg)
	{
		using var table = new StringTable("", "mapurl");
		for (int i = 0; i < ConfigInstance.MapPool.Count; i++)
		{
			var wipe = ConfigInstance.MapPool[i];
			table.AddRow(i + 1, wipe);
		}

		arg.ReplyWith(table.ToStringMinimal());
	}

	[ConsoleCommand("autowipe.deletemap", "Deletes an existent map url present in the MapPool config property.")]
	[AuthLevel(2)]
	private void delete_map(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs())
		{
			arg.ReplyWith("Provide an index from 'autowipe.maps'");
			return;
		}

		var i = arg.GetInt(0);
		if (i < 0 || i >= ConfigInstance.Wipes.Count)
		{
			arg.ReplyWith("Went above or below indexes available. Use numbers from 'autowipe.maps`'");
			return;
		}

		ConfigInstance.Wipes.RemoveAt(i);
		Save();
		arg.ReplyWith("Removed map URL");
	}

	[ConsoleCommand("autowipe.addmap", "Adds a new map URLs to the list.")]
	[AuthLevel(2)]
	private void add_map(ConsoleSystem.Arg arg)
	{
		if (!arg.HasArgs())
		{
			arg.ReplyWith("You've got missing arguments. Please make sure to follow the following syntax:\n" +
			              "eg. autowipe.addmap \"<MapUrl>\"");
			return;
		}

		for(int i = 0; i < arg.Args.Length; i++)
		{
			var map = arg.Args[i];
			if (ConfigInstance.MapPool.Contains(map))
			{
				arg.ReplyWith($"Map url '{map}' already exists in the pool");
				continue;
			}

			ConfigInstance.MapPool.Add(map);
		}
		Save();
		arg.ReplyWith("Added map url");
	}

	public class Wipe
	{
		public string WipeName;
		public string[] Commands;
		public string MapBrowserName;
		public string MapUrl;
		public int MapSize;
		public int ServerSeed;
		public string Cron;
		public bool Temp;

		[JsonProperty("Type (0=fullwipe 1=mapwipe)")]
		public WipeTypes Type;

		public void CloneTo(Wipe other)
		{
			other.WipeName = WipeName;
			other.Commands = Commands.ToArray();
			other.MapBrowserName = MapBrowserName;
			other.MapUrl = MapUrl;
			other.MapSize = MapSize;
			other.ServerSeed = ServerSeed;
			other.Cron = Cron;
			other.Temp = Temp;
			other.Type = Type;
		}

		public void InitWorld(List<string> mapPool)
		{
#if !MINIMAL
			Community.Runtime.Core.CustomMapName = string.IsNullOrEmpty(MapBrowserName) ? "-1" : MapBrowserName;
#endif
			World.Url = ConVar.Server.levelurl = MapUrl == "POOL" ? mapPool[Random.Range(0, mapPool.Count)] : MapUrl;
			if (MapSize != 0)
				World.InitSize(ConVar.Server.worldsize = MapSize);
			if (ServerSeed == 0)
				ServerSeed = Random.Range(1, int.MaxValue);
			World.InitSeed(ConVar.Server.seed = ServerSeed);
		}

		public override bool Equals(object other)
		{
			if (other is Wipe otherVal)
			{
				return GetHashCode() == otherVal.GetHashCode();
			}

			return false;
		}

		public override int GetHashCode()
		{
			return (WipeName, MapBrowserName, MapUrl, MapSize, ServerSeed, Type, Temp, Cron, UpcomingWipeCommands: Commands).GetHashCode();
		}

		public bool ShouldWipe()
		{
			if (string.IsNullOrEmpty(Cron))
			{
				return false;
			}

			var now = DateTime.UtcNow;
			var cron = CronExpression.Parse(Cron);
			var nextOccurrence = cron.GetNextOccurrence(now.AddMinutes(-1), TimeZoneInfo.Utc);
			if (!nextOccurrence.HasValue)
			{
				return false;
			}

			static DateTime RoundDownTo10Minutes(DateTime dt)
			{
				int roundedMinutes = dt.Minute - (dt.Minute % 10);
				return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, roundedMinutes, 0, dt.Kind);
			}

			var nowRounded = RoundDownTo10Minutes(now);
			var occurrenceRounded = RoundDownTo10Minutes(nextOccurrence.Value);

			return nowRounded == occurrenceRounded;
		}
	}

	public struct WipeConfig
	{
		public string[] PostWipeCommands;
		public string[] PostWipeDeletes;
	}

	public enum WipeTypes
	{
		FullWipe,
		MapWipe
	}
}

public class AutoWipeConfig
{
	public AutoWipeModule.WipeConfig FullWipe;
	public AutoWipeModule.WipeConfig MapWipe;
	public List<string> MapPool = new();
	public List<AutoWipeModule.Wipe> Wipes = new();

	public AutoWipeModule.WipeConfig GetWipeConfig(AutoWipeModule.Wipe wipe)
	{
		return wipe.Type switch
		{
			AutoWipeModule.WipeTypes.FullWipe => FullWipe,
			AutoWipeModule.WipeTypes.MapWipe => MapWipe,
			_ => default
		};
	}
}

public class AutoWipeData
{
	public long LastWipeTime;
	public AutoWipeModule.Wipe Wipe;
	public AutoWipeModule.Wipe NextWipe;
}
