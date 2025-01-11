using System.Collections.Generic;
using Carbon.Base;
using Carbon.Components;
using Carbon.Extensions;
using Newtonsoft.Json;
using Oxide.Core;
using Rust;

namespace Carbon.Modules;

public partial class AutoWipeModule : CarbonModule<AutoWipeConfig, EmptyModuleData>
{
	public override string Name => "AutoWipe";
	public override VersionNumber Version => new(1, 0, 0);
	public override System.Type Type => typeof(AutoWipeModule);
	public override bool EnabledByDefault => false;

	public bool justWiped;

	public override void OnPostServerInit(bool initial)
	{
		base.OnPostServerInit(initial);

		if (!justWiped)
		{
			return;
		}

		foreach (var command in ConfigInstance.PostWipeCommands)
		{
			if (string.IsNullOrEmpty(command))
				continue;
			ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), command);
		}
	}

	public override void Load()
	{
		base.Load();

		if (!IsEnabled())
		{
			return;
		}

		if (ConfigInstance.Maps.Count == 0)
		{
			ConfigInstance.Maps.Add(new());
			Save();
		}

		if (ConfigInstance.LastProtocol != Protocol.save)
		{
			ConfigInstance.LastProtocol = Protocol.save;
			var map = ConfigInstance.LastMap = ConfigInstance.GetMap();

			ConVar.Server.autoUploadMap = false;

			justWiped = true;
			using var table = new StringTable("name", "seed", "size", "url");
			table.AddRow(map.Name, map.Seed, map.Size, map.Url);
			PutsWarn($"Selected map:\n{table.ToStringMinimal()}");
			if (map.RemoveOnPicked)
			{
				ConfigInstance.Maps.Remove(map);
				PutsWarn($"Removed map from list");
			}

			map.InitWorld();

			foreach (var delete in ConfigInstance.PostWipeDeletes)
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

			Save();
		}
		else
		{
			ConfigInstance.LastMap.InitWorld();
			PutsWarn($"Save file valid at protocol {Protocol.printable}. No auto-wipe necessary.");
		}
	}
}

public class AutoWipeConfig
{
	public string[] PostWipeCommands = new[] { "" };
	public string[] PostWipeDeletes = new[] { "" };
	public List<Map> Maps = new();
	[JsonProperty("MapPickOrder (0=next 1=prev 2=random)")]
	public MapPickOrders MapPickOrder = MapPickOrders.Next;
	public int NextMapPick = -1;
	public int LastProtocol;
	public Map LastMap;

	public Map GetMap()
	{
		if (Maps.Count == 0)
		{
			return default;
		}

		switch (MapPickOrder)
		{
			case MapPickOrders.Next:
				NextMapPick++;
				if (NextMapPick >= Maps.Count)
					NextMapPick = 0;
				return Maps[NextMapPick];

			case MapPickOrders.Previous:
				NextMapPick--;
				if (NextMapPick < 0)
					NextMapPick = Maps.Count - 1;
				return Maps[NextMapPick];
			case MapPickOrders.Random:
				return Maps[NextMapPick = Random.Range(0, Maps.Count)];
		}

		return default;
	}

	public struct Map
	{
		public string Name;
		public string Url;
		public int Size;
		public int Seed;
		public bool RemoveOnPicked;

		public void InitWorld()
		{
#if !MINIMAL
			Community.Runtime.Core.CustomMapName = string.IsNullOrEmpty(Name) ? "-1" : Name;
#endif
			World.Url = ConVar.Server.levelurl = Url;
			if (Size != 0)
				World.InitSize(ConVar.Server.worldsize = Size);
			if (Seed != 0)
				World.InitSeed(ConVar.Server.seed = Seed);
		}
	}

	public enum MapPickOrders
	{
		Next,
		Previous,
		Random
	}
}
