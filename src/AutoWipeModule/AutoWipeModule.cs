using System;
using System.Collections.Generic;
using Carbon.Base;
using Carbon.Components;
using Carbon.Extensions;
using Newtonsoft.Json;
using Oxide.Core;
using Rust;
using Cronos;
using Random = UnityEngine.Random;

namespace Carbon.Modules;

public partial class AutoWipeModule : CarbonModule<AutoWipeConfig, EmptyModuleData>
{
	public override string Name => "AutoWipe";
	public override VersionNumber Version => new(1, 0, 0);
	public override System.Type Type => typeof(AutoWipeModule);
	public override bool EnabledByDefault => false;

	private CronExpression cronCache;

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

		var currentWipe = ConfigInstance.CurrentWipe;
		var wipe = ConfigInstance.CurrentWipe.IsValid ? ConfigInstance.CurrentWipe : ConfigInstance.GetWipe();
		var justWiped = !currentWipe.Equals(wipe);
		var config = ConfigInstance.GetWipeConfig(wipe);

		if (wipe.IsDue(ConfigInstance.UseUtc))
		{
			wipe = ConfigInstance.GetWipe();
			config = ConfigInstance.GetWipeConfig(wipe);
			justWiped = true;
		}

		if (justWiped)
		{
			ConfigInstance.CurrentWipe = wipe;
			ConVar.Server.autoUploadMap = false;

			using var table = new StringTable("name", "seed", "size", "url");
			table.AddRow(wipe.MapName, wipe.ServerSeed, wipe.MapSize, wipe.MapUrl);
			PutsWarn($"Selected map:\n{table.ToStringMinimal()}");
			if (wipe.Temporary)
			{
				ConfigInstance.Wipes.Remove(wipe);
				PutsWarn($"Removed map from list");
			}

			wipe.InitWorld();

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
			ConfigInstance.CurrentWipe.InitWorld();
			PutsWarn($"Save file valid at protocol {Protocol.printable}. No auto-wipe necessary.");
		}
	}
}

public class AutoWipeConfig
{
	public bool UseUtc = true;

	public WipeConfig FullWipe;
	public WipeConfig MapWipe;

	public List<Wipe> Wipes = new();
	[JsonProperty("PickOrder (0=next 1=prev 2=random)")]
	public PickOrders PickOrder = PickOrders.Next;
	public Wipe CurrentWipe;
	public int NextPickIndex = -1;

	public Wipe GetWipe()
	{
		if (Wipes.Count == 0)
			return default;

		switch (PickOrder)
		{
			case PickOrders.Next:
				NextPickIndex++;
				if (NextPickIndex >= Wipes.Count)
					NextPickIndex = 0;
				return Wipes[NextPickIndex];

			case PickOrders.Previous:
				NextPickIndex--;
				if (NextPickIndex < 0)
					NextPickIndex = Wipes.Count - 1;
				return Wipes[NextPickIndex];
			case PickOrders.Random:
				return Wipes[NextPickIndex = Random.Range(0, Wipes.Count)];
		}

		return default;
	}
	public WipeConfig GetWipeConfig(Wipe wipe)
	{
		return wipe.Type switch
		{
			WipeTypes.FullWipe => FullWipe,
			WipeTypes.MapWipe => MapWipe,
			_ => default
		};
	}

	public struct Wipe
	{
		public string MapName;
		public string MapUrl;
		public int MapSize;
		public int ServerSeed;
		[JsonProperty("Type (0=fullwipe 1=mapwipe)")]
		public WipeTypes Type;
		public bool Temporary;
		public string NextWipeCron;

		[JsonIgnore]
		public bool IsValid => !string.IsNullOrEmpty(MapName) || !string.IsNullOrEmpty(MapUrl) || MapSize > 0 || ServerSeed > 0;

		public void InitWorld()
		{
#if !MINIMAL
			Community.Runtime.Core.CustomMapName = string.IsNullOrEmpty(MapName) ? "-1" : MapName;
#endif
			World.Url = ConVar.Server.levelurl = MapUrl;
			if (MapSize != 0)
				World.InitSize(ConVar.Server.worldsize = MapSize);
			if (ServerSeed != 0)
				World.InitSeed(ConVar.Server.seed = ServerSeed);
		}

		public override bool Equals(object other)
		{
			if (other is Wipe otherVal)
				return otherVal.MapName == MapName && otherVal.MapUrl == MapUrl && otherVal.MapSize == MapSize && otherVal.ServerSeed == ServerSeed && otherVal.Type == Type;

			return false;
		}

		public bool IsDue(bool useUtc)
		{
			var time = useUtc ? DateTime.UtcNow : DateTime.Now;
			var cron = CronExpression.Parse(NextWipeCron);
			var occurence = cron.GetNextOccurrence(time);

			if (!occurence.HasValue)
			{
				return false;
			}

			var matchTime = occurence.Value;
			Logger.Log($"{time}: {matchTime}");
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
