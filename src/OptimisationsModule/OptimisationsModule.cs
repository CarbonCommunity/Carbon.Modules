using System;
using System.Collections.Generic;
using Carbon.Base;
using Network;
using Network.Visibility;
using Oxide.Core;

/*
 *
 * Copyright (c) 2022 Vice <https://codefling.com/vice>, under the GNU v3 license rights
 * Copyright (c) 2024 Cobalt Studios, under the GNU v3 license rights
 * Copyright (c) 2022-2024 Carbon Community, under the GNU v3 license rights
 *
 */

namespace Carbon.Modules;
#pragma warning disable IDE0051

public partial class OptimisationsModule : CarbonModule<EmptyModuleConfig, EmptyModuleData>
{
	public override string Name => "Optimisations";
	public override VersionNumber Version => new(1, 0, 0);
	public override Type Type => typeof(OptimisationsModule);
	public override bool EnabledByDefault => false;

	internal int visibilityRadiusFarOverrideOriginal;
	internal int visibilityRadiusNearOverrideOriginal;

	private void CircularNetworkDistance() { }

	public override void OnServerInit(bool initial)
	{
		base.OnServerInit(initial);

		if (!initial) return;

		OnEnabled(true);
	}
	public override void OnEnabled(bool initialized)
	{
		if (initialized)
		{
			visibilityRadiusFarOverrideOriginal = ConVar.Net.visibilityRadiusFarOverride;
			visibilityRadiusNearOverrideOriginal = ConVar.Net.visibilityRadiusNearOverride;
		}

		if (ConVar.Net.visibilityRadiusFarOverride == -1)
			ConVar.Net.visibilityRadiusFarOverride = 6;

		if (ConVar.Net.visibilityRadiusNearOverride == -1)
			ConVar.Net.visibilityRadiusNearOverride = 4;

		base.OnEnabled(initialized);
	}
	public override void OnDisabled(bool initialized)
	{
		ConVar.Net.visibilityRadiusFarOverride = visibilityRadiusFarOverrideOriginal;
		ConVar.Net.visibilityRadiusNearOverride = visibilityRadiusNearOverrideOriginal;

		base.OnDisabled(initialized);
	}

	private static readonly List<int> EightCircle = [2, 4, 6, 6, 7, 7, 8, 8, 8, 8, 8, 7, 7, 6, 6, 4, 2];
	private static readonly List<int> SevenCircle = [2, 4, 5, 6, 6, 7, 7, 7, 7, 7, 6, 6, 5, 4, 2];
	private static readonly List<int> SixCircle = [2, 4, 5, 5, 6, 6, 6, 6, 6, 5, 5, 4, 2];
	private static readonly List<int> FiveCircle = [2, 3, 4, 5, 5, 5, 5, 5, 4, 3, 2];
	private static readonly List<int> FourCircle = [2, 3, 4, 4, 4, 4, 4, 3, 2];
	private static readonly List<int> ThreeCircle = [1, 2, 3, 3, 3, 2, 1];
	private static readonly List<int> TwoCircle = [1, 2, 2, 2, 1];

	static List<int> GetCircleSizeLookup(int radius)
	{
		return radius switch
		{
			8 => EightCircle,
			7 => SevenCircle,
			6 => SixCircle,
			5 => FiveCircle,
			4 => FourCircle,
			3 => ThreeCircle,
			2 => TwoCircle,
			_ => null,
		};
	}

	static bool GetVisibleFromCircle(NetworkVisibilityGrid grid, Group group, List<Group> groups, int radius)
	{
		var lookup = GetCircleSizeLookup(radius);

		if (lookup == null)
			return true;

		// Global netgroup
		groups.Add(Net.sv.visibility.Get(0U));
		if (group.restricted)
		{
			groups.Add(group);
			return false;
		}
		int id = (int)group.ID;
		if (id < grid.startID)
			return false;

		ValueTuple<int, int, int> valueTuple = DeconstructGroupId(grid, id);
		int item1 = valueTuple.Item1;
		int item2 = valueTuple.Item2;
		int item3 = valueTuple.Item3;

		for (int deltaY = -radius; deltaY <= radius; deltaY++)
		{
			int bounds = lookup[deltaY + radius];
			for (int deltaX = -bounds; deltaX <= bounds; deltaX++)
			{
				AddLayers(grid, groups, item1 + deltaX, item2 + deltaY, item3);
			}
		}
		return false;
	}

	private static ValueTuple<int, int, int> DeconstructGroupId(NetworkVisibilityGrid grid, int groupId)
	{
		groupId -= grid.startID;
		var num2 = Math.DivRem(groupId, grid.cellCount * grid.cellCount, out var num);
		return new ValueTuple<int, int, int>(Math.DivRem(num, grid.cellCount, out var num1), num1, num2);
	}

	static void AddLayers(NetworkVisibilityGrid grid, List<Group> groups, int groupX, int groupY, int groupLayer)
	{
		Add(grid, groups, groupX, groupY, groupLayer);

		if (groupLayer == 0)
		{
			Add(grid, groups, groupX, groupY, 1);
		}
		else if (groupLayer == 1)
		{
			Add(grid, groups, groupX, groupY, 2);
			Add(grid, groups, groupX, groupY, 0);
		}
		else if (groupLayer == 2)
		{
			Add(grid, groups, groupX, groupY, 1);
		}
	}

	static void Add(NetworkVisibilityGrid grid, List<Group> groups, int groupX, int groupY, int groupLayer) => groups.Add(Net.sv.visibility.Get(CoordToID(grid, groupX, groupY, groupLayer)));

	static uint CoordToID(NetworkVisibilityGrid grid, int x, int y, int layer) => (uint)(layer * (grid.cellCount * grid.cellCount) + x * grid.cellCount + y + grid.startID);
}
