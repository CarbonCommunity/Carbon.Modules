using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Carbon.Base;
using HarmonyLib;
using JetBrains.Annotations;
using Network;
using Oxide.Core;

/*
 *
 * Copyright (c) 2023-2024 Patrette, under the GNU v3 license rights
 * Copyright (c) 2023-2024 Carbon Community, under the GNU v3 license rights
 *
 */

namespace Carbon.Modules;

public partial class SelectiveEACModule : CarbonModule<SelectiveEACConfig, EmptyModuleData>
{
	internal static SelectiveEACModule Singleton { get; set; }

	public override string Name => "SelectiveEAC";
	public override VersionNumber Version => new(1, 0, 0);
	public override Type Type => typeof(SelectiveEACModule);
	public override bool ForceModded => false;
	public override bool AutoPatch => true;

	public SelectiveEACModule()
	{
		Singleton = this;
	}

	public override void OnServerInit(bool initial)
	{
		base.OnServerInit(initial);

		if (!initial)
		{
			return;
		}

		OnEnabled(true);
	}
	public override void OnEnabled(bool initialized)
	{
		base.OnEnabled(initialized);

		if (!initialized)
		{
			return;
		}

		Permissions.UnregisterPermissions(this);
		Permissions.RegisterPermission(ConfigInstance.UsePermission, this);

		if (!Permissions.GroupExists(ConfigInstance.UseGroup))
		{
			Permissions.CreateGroup(ConfigInstance.UseGroup, "Selective EAC", 0);
		}
	}

	private static bool CanBypass(Connection connection)
	{
		var id = connection.userid.ToString();
		var permissions = Community.Runtime.Core.permission;

		if (!permissions.UserExists(id))
		{
			permissions.GetUserData(id, true);

			if (!string.IsNullOrEmpty(Community.Runtime.Config.Permissions.PlayerDefaultGroup))
			{
				permissions.AddUserGroup(id, Community.Runtime.Config.Permissions.PlayerDefaultGroup);
			}
		}

		return connection.os == "editor" &&
			(Community.Runtime.Core.permission.UserHasPermission(id, Singleton.ConfigInstance.UsePermission) || Community.Runtime.Core.permission.UserHasGroup(id, Singleton.ConfigInstance.UseGroup));
	}

	// Returns the encryption level for a user
	// 2: EAC Black magic
	// 1: XOR using the network protocol uint and a constant 256 byte salt (also bad idea)
	// 0: Nothing (bad idea)
	private static int UserEncryptionOverride(Connection connection)
	{
		try
		{
			if (CanBypass(connection))
			{
				return Singleton.ConfigInstance.ServerEncryptionOverride;
			}
		}
		catch (Exception e)
		{
			Logger.Error($"Failed getting UserEncryptionOverride", e);
		}

		return ConVar.Server.encryption;
	}

	#region Patches

	[HarmonyPatch(typeof(EACServer), nameof(EACServer.OnJoinGame))]
	[UsedImplicitly]
	private class EACServer_OnJoinGame
	{
		[HarmonyPrefix]
		[UsedImplicitly]
		private static bool Prefix(Connection connection)
		{
			try
			{
				if (CanBypass(connection))
				{
					EACServer.OnAuthenticatedLocal(connection);
					EACServer.OnAuthenticatedRemote(connection);
					return false;
				}
			}
			catch (Exception e)
			{
				Logger.Error($"EACServer.OnJoinGame CanBypass failure", e);
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(ServerMgr), nameof(ServerMgr.JoinGame))]
	[UsedImplicitly]
	private class ServerMgr_JoinGame
	{
		[HarmonyTranspiler]
		[UsedImplicitly]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> op)
		{
			List<CodeInstruction> il = new(op);

			for (int i = 0; i < il.Count; i++)
			{
				var cil = il[i];

				if (cil.opcode == OpCodes.Ldsfld && cil.operand is FieldInfo
					{
						Name: "encryption",
						DeclaringType.Name: "Server",
						DeclaringType.Namespace: "ConVar"
					})
				{
					cil.opcode = OpCodes.Ldarg_1;
					cil.operand = null;
					il.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SelectiveEACModule), nameof(UserEncryptionOverride))));
					i++;
				}
			}

			return il;
		}
	}

	#endregion
}

public class SelectiveEACConfig
{
	public int ServerEncryptionOverride = 1;
	public string UsePermission = "selectiveeac.use";
	public string UseGroup = "selectiveeac";
}
