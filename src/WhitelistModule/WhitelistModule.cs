﻿using System;
using System.Collections.Generic;
using System.Linq;
using Carbon.Base;
using Oxide.Core;
using Net = Network.Net;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * Copyright (c) 2022 Patrette
 * All rights reserved.
 *
 */

namespace Carbon.Modules;

public partial class WhitelistModule : CarbonModule<WhitelistConfig, EmptyModuleData>
{
	internal static WhitelistModule Singleton { get; set; }

	public override string Name => "Whitelist";
	public override VersionNumber Version => new(1, 0, 0);
	public override Type Type => typeof(WhitelistModule);
	public override bool ForceModded => false;

	public WhitelistModule()
	{
		Singleton = this;
	}

	public override void OnEnabled(bool initialized)
	{
		base.OnEnabled(initialized);

		Subscribe("CanUserLogin");

		Permissions.UnregisterPermissions(this);
		Permissions.RegisterPermission(ConfigInstance.BypassPermission, this);
	}
	public override void OnDisabled(bool initialized)
	{
		base.OnDisabled(initialized);

		Unsubscribe("CanUserLogin");
	}

	public override Dictionary<string, Dictionary<string, string>> GetDefaultPhrases()
	{
		return new Dictionary<string, Dictionary<string, string>>
		{
			["en"] = new()
			{
				["denied"] = "Not whitelisted"
			}
		};
	}

	#region Hooks

	private object CanUserLogin(string name, string id, string ipAddress)
	{
		var connection = Net.sv.connections.FirstOrDefault(x => x.userid.ToString() == id);

		if (connection.authLevel >= 2 || CanBypass(id))
		{
			return null;
		}

		ConsoleNetwork.SendClientCommand(connection, $"echo {GetPhrase("denied", id)}");
		Community.Runtime.Core.NextTick(() => ConnectionAuth.Reject(connection, GetPhrase("denied", id), null));

		return null;
	}

	#endregion

	public bool CanBypass(string playerId)
	{
		return Permissions.UserHasPermission(playerId.ToString(), ConfigInstance.BypassPermission)
			|| (!string.IsNullOrEmpty(ConfigInstance.BypassGroup) && Permissions.UserHasGroup(playerId, ConfigInstance.BypassGroup));
	}
}

public class WhitelistConfig
{
	public string BypassPermission = "whitelist.bypass";
	public string BypassGroup = "whitelisted";
}
