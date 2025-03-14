using System.Collections.Generic;
using API.Hooks;
using HarmonyLib;
using Network.Visibility;
using Oxide.Core.Plugins;

namespace Carbon.Modules;

public partial class CircularNetworkingModule
{
	[AutoPatch, HarmonyPatch(typeof(NetworkVisibilityGrid), "GetVisibleFrom")]
	public class GetVisibleFrom : API.Hooks.Patch
	{
		public static bool Prefix(NetworkVisibilityGrid __instance, Group group, List<Group> groups, int radius) => GetVisibleFromCircle(__instance, group, groups, radius);
	}
}
