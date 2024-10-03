using System.Collections.Generic;
using API.Hooks;
using Network.Visibility;

namespace Carbon.Modules;

public partial class CircularNetworkingModule
{
	[HookAttribute.Patch("CircularNetworkDistance", "CircularNetworkDistance [patch]", typeof(NetworkVisibilityGrid), "GetVisibleFrom")]
	[HookAttribute.Identifier("ae0577348a5140ea9aa861cd71c31e7c")]
	[HookAttribute.Options(HookFlags.None)]
	public class NetworkVisibilityGrid_GetVisibleFrom_ae0577348a5140ea9aa861cd71c31e7c : API.Hooks.Patch
	{
		public static bool Prefix(NetworkVisibilityGrid __instance, Group group, List<Group> groups, int radius)
			=> GetVisibleFromCircle(__instance, group, groups, radius);
	}
}
