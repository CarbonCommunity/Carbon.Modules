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
	// test PR
	public override string Name => "AutoWipe";
	public override VersionNumber Version => new(1, 0, 1);
	public override Type Type => typeof(AutoWipeModule);
	public override bool EnabledByDefault => false;

	private readonly float wipeCooldown = 60 * 60;
	private Timer wipeTimer;

	public bool InCooldown() => (DateTime.UtcNow - new DateTime(DataInstance.LastWipeTick)).TotalSeconds <= wipeCooldown;

	public override void Load()
	{
		base.Load();

		if (!IsEnabled())
		{
			return;
		}

		if (ConfigInstance.Wipes.Count == 0)
		{
			ConfigInstance.Wipes.Add(new());
			Save();
		}

		ConfigInstance.MapPool ??= new();

		var currentWipe = DataInstance.CurrentWipe;
		var wipe = DataInstance.CurrentWipe.IsValid || InCooldown() ? DataInstance.CurrentWipe : ConfigInstance.GetWipe(DataInstance);
		var justWiped = !currentWipe.Equals(wipe);
		var config = ConfigInstance.GetWipeConfig(wipe);

		if (!InCooldown() && wipe.IsDue())
		{
			DataInstance.LastWipeTick = DateTime.UtcNow.Ticks;
			wipe = ConfigInstance.GetWipe(DataInstance);
			config = ConfigInstance.GetWipeConfig(wipe);
			justWiped = true;
		}

		if (justWiped)
		{
			ConVar.Server.autoUploadMap = false;

			if (wipe.Temporary)
			{
				ConfigInstance.Wipes.Remove(wipe);
				PutsWarn($"Removed map from list");
			}

			wipe.InitWorld(ConfigInstance.MapPool);
			DataInstance.CurrentWipe = wipe;

			using var table = new StringTable("name", "seed", "size", "url");
			table.AddRow(wipe.MapBrowserName, wipe.ServerSeed, wipe.MapSize, wipe.MapUrl);
			PutsWarn($"Selected map:\n{table.ToStringMinimal()}");

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
						PutsWarn($"Deleting file '{delete}'");
						continue;
					}

					if (OsEx.Folder.Exists(delete))
					{
						OsEx.Folder.Delete(delete);
						PutsWarn($"Deleting directory '{delete}'");
					}
				}
			}

			Save();
		}
		else
		{
			DataInstance.CurrentWipe.InitWorld(ConfigInstance.MapPool);
		}
	}

	public override void OnServerInit(bool initial)
	{
		base.OnServerInit(initial);

		wipeTimer = Community.Runtime.Core.timer.Every(ConfigInstance.Tick, WipeTickImpl);
	}

	private void WipeTickImpl()
	{
		if (!IsEnabled() || InCooldown())
		{
			return;
		}

		if (!DataInstance.CurrentWipe.IsDue())
		{
			return;
		}

		if (DataInstance.CurrentWipe.WipeCommands != null)
		{
			foreach (var command in DataInstance.CurrentWipe.WipeCommands)
			{
				if (string.IsNullOrEmpty(command))
					continue;
				ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), command);
			}
		}

		wipeTimer.Destroy();
	}

	[ConsoleCommand("autowipe.wipes", "Prints all available wipes present in the Wipes config property.")]
	[AuthLevel(2)]
	private void print_wipes(ConsoleSystem.Arg arg)
	{
		using var table = new StringTable("", "wipename", "mapurl", "mapsize", "serverseed", "type", "temp", "nextwipe",
			"wipecommands");
		for (int i = 0; i < ConfigInstance.Wipes.Count; i++)
		{
			var wipe = ConfigInstance.Wipes[i];
			table.AddRow(i + 1, wipe.WipeName, wipe.MapUrl, wipe.MapSize,
				wipe.ServerSeed == 0 ? "random" : wipe.ServerSeed,
				wipe.Type, wipe.Temporary ? "yes" : "no", wipe.NextWipeCron, wipe.WipeCommands?.ToString("->"));
		}

		arg.ReplyWith(table.ToStringMinimal());
	}

	[ConsoleCommand("autowipe.delete", "Deletes an existent wipe present in the Wipes config property.")]
	[AuthLevel(2)]
	private void delete_wipe(ConsoleSystem.Arg arg)
	{
		if (arg.HasArgs())
		{
			arg.ReplyWith("Provide an index from 'autowipe.wipes'");
			return;
		}

		var i = arg.GetInt(0);
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
		if (!arg.HasArgs(7))
		{
			arg.ReplyWith("You've got missing arguments. Please make sure to follow the following syntax:\n" +
			              "eg. autowipe.add \"<WipeName>\" \"<MapUrl>\" \"<MapSize>\" \"<ServerSeed>\" \"<Type|0=fullwipe 1=mapwipe>\" \"<Temporary|True/False>\" \"<NextWipeCron>\" \"<WipeCommands>\"");
			return;
		}

		ConfigInstance.Wipes.Add(new()
		{
			MapBrowserName = arg.GetString(0),
			MapUrl = arg.GetString(1),
			MapSize = arg.GetInt(2),
			ServerSeed = arg.GetInt(3),
			Type = (WipeTypes)arg.GetInt(4),
			Temporary = arg.GetBool(5),
			NextWipeCron = arg.GetString(6),
			WipeCommands = arg.GetString(7).Split('|')
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
		if (arg.HasArgs())
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

	[CommandVar("autowipe.tick")]
	[AuthLevel(2)]
	private float tick
	{
		get => ConfigInstance.Tick;
		set => ConfigInstance.Tick = value;
	}

	public struct Wipe
	{
		public string WipeName;
		public string[] WipeCommands;
		public string MapBrowserName;
		public string MapUrl;
		public int MapSize;
		public int ServerSeed;

		[JsonProperty("Type (0=fullwipe 1=mapwipe)")]
		public WipeTypes Type;

		public bool Temporary;
		public string NextWipeCron;

		[JsonIgnore]
		public bool IsValid => !string.IsNullOrEmpty(WipeName) ||
		                       !string.IsNullOrEmpty(MapBrowserName) ||
		                       !string.IsNullOrEmpty(MapUrl) || MapSize > 0 || ServerSeed > 0;

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
				return GetHashCode() == otherVal.GetHashCode();

			return false;
		}

		public override int GetHashCode()
		{
			return (WipeName, MapBrowserName, MapUrl, MapSize, ServerSeed, Type, Temporary, NextWipeCron).GetHashCode();
		}

		public bool IsDue()
		{
			if (string.IsNullOrEmpty(NextWipeCron))
			{
				return false;
			}

			var split = NextWipeCron.Split(' ');
			if (split.Length < 1)
			{
				return false;
			}

			split[0] = "*";
			var time = DateTime.UtcNow;
			var cron = CronExpression.Parse(split.ToString(" "));
			var occurence = cron.GetNextOccurrence(time);

			if (!occurence.HasValue)
			{
				return false;
			}

			var matchTime = occurence.Value;
			return matchTime.Hour == time.Hour &&
			       matchTime.Day == time.Day &&
			       matchTime.Month == time.Month &&
			       matchTime.Year == time.Year;
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

	public enum PickOrders
	{
		Next,
		Previous,
		Random
	}
}

public class AutoWipeConfig
{
	public float Tick = 60f;
	public AutoWipeModule.WipeConfig FullWipe;
	public AutoWipeModule.WipeConfig MapWipe;
	public List<string> MapPool = new();
	public List<AutoWipeModule.Wipe> Wipes = new();
	[JsonProperty("PickOrder (0=next 1=prev 2=random)")]
	public AutoWipeModule.PickOrders PickOrder = AutoWipeModule.PickOrders.Next;

	public AutoWipeModule.Wipe GetWipe(AutoWipeData data)
	{
		if (Wipes.Count == 0)
			return default;

		switch (PickOrder)
		{
			case AutoWipeModule.PickOrders.Next:
				data.NextPickIndex++;
				if (data.NextPickIndex >= Wipes.Count)
					data.NextPickIndex = 0;
				return Wipes[data.NextPickIndex];

			case AutoWipeModule.PickOrders.Previous:
				data.NextPickIndex--;
				if (data.NextPickIndex < 0)
					data.NextPickIndex = Wipes.Count - 1;
				return Wipes[data.NextPickIndex];
			case AutoWipeModule.PickOrders.Random:
				return Wipes[data.NextPickIndex = Random.Range(0, Wipes.Count)];
		}

		return default;
	}
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
	public AutoWipeModule.Wipe CurrentWipe;
	public int NextPickIndex = -1;
	public long LastWipeTick;
}
