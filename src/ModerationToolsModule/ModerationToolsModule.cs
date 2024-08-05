using System;
using System.Linq;
using Carbon.Base;
using Carbon.Core;
using Carbon.Extensions;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Carbon.Modules;

public partial class ModerationToolsModule : CarbonModule<ModerationToolsConfig, EmptyModuleData>
{
	public static ModerationToolsModule Singleton { get; internal set; }

	public override string Name => "ModerationTools";
	public override Type Type => typeof(ModerationToolsModule);
	public override VersionNumber Version => new(1, 0, 0);
	public override bool ForceModded => false;

	public override bool EnabledByDefault => false;

	public override void Init()
	{
		base.Init();

		Singleton = this;
	}

	public override void OnServerInit(bool initial)
	{
		base.OnServerInit(initial);

		if (!initial) return;

		OnEnabled(true);
	}

	public override void OnEnabled(bool initialized)
	{
		base.OnEnabled(initialized);

		if (!initialized) return;

		Permissions.RegisterPermission(ConfigInstance.Moderation.Cmod1Permission, this);
		Permissions.RegisterPermission(ConfigInstance.Moderation.Cmod2Permission, this);

		var cmod1Permissions = new string[] { ConfigInstance.Moderation.Cmod1Permission };
		var cmod2Permissions = new string[] { ConfigInstance.Moderation.Cmod2Permission };
		Community.Runtime.Core.cmd.AddCovalenceCommand(ConfigInstance.Moderation.CmodCommand, this, nameof(ToggleCadmin), permissions: cmod1Permissions, cooldown: ConfigInstance.Moderation.CmodCommandCooldown);
		Community.Runtime.Core.cmd.AddConsoleCommand("cmod.mute", this, nameof(Mute), permissions: cmod2Permissions, cooldown: ConfigInstance.Moderation.CmodCommandCooldown, silent: true);
		Community.Runtime.Core.cmd.AddConsoleCommand("cmod.unmute", this, nameof(Unmute), permissions: cmod2Permissions, cooldown: ConfigInstance.Moderation.CmodCommandCooldown, silent: true);
		Community.Runtime.Core.cmd.AddConsoleCommand("cmod.mutelist", this, nameof(MuteList), permissions: cmod2Permissions, cooldown: ConfigInstance.Moderation.CmodCommandCooldown, silent: true);
		Community.Runtime.Core.cmd.AddConsoleCommand("cmod.kick", this, nameof(Kick), permissions: cmod2Permissions, cooldown: ConfigInstance.Moderation.CmodCommandCooldown, silent: true);
		Community.Runtime.Core.cmd.AddConsoleCommand("cmod.ban", this, nameof(Ban), permissions: cmod2Permissions, cooldown: ConfigInstance.Moderation.CmodCommandCooldown, silent: true);
	}

	private object INoteAdminHack(BasePlayer player)
	{
		if (Permissions.UserHasPermission(player.UserIDString, ConfigInstance.Moderation.Cmod1Permission))
		{
			return false;
		}

		return null;
	}
	private object IServerEventToasts(GameTip.Styles style)
	{
		if (ConfigInstance.ShowServerEventToasts) return null;

		return false;
	}
	private object CanUnlockTechTreeNode()
	{
		if (ConfigInstance.NoTechTreeUnlock) return false;

		return null;
	}

	public void Mute(ConsoleSystem.Arg arg)
	{
		var player = arg.Player();
		if (player == null) return;

		var playerName = arg.GetString(0);
		var reason = arg.Args.Skip(1).ToString(" ");

		var targetPlayer = BasePlayer.FindAwakeOrSleeping(playerName);
		if (targetPlayer == null)
		{
			player.ConsoleMessage($"Couldn't find that player.");
			return;
		}

		targetPlayer.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, true);
		targetPlayer.ChatMessage($"{player.displayName} has muted you: {reason}");

		var log = $"{targetPlayer.displayName} has been muted by {player}: {reason}";
		player.ConsoleMessage(log);
		Puts(log);
	}
	public void Unmute(ConsoleSystem.Arg arg)
	{
		var player = arg.Player();
		if (player == null) return;

		var playerName = arg.GetString(0);

		var targetPlayer = BasePlayer.FindAwakeOrSleeping(playerName);
		if (targetPlayer == null)
		{
			player.ConsoleMessage($"Couldn't find that player.");
			return;
		}

		targetPlayer.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, false);
		targetPlayer.ChatMessage($"{player.displayName} has unmuted you.");

		var log = $"{targetPlayer.displayName} has been unmuted by {player}";
		player.ConsoleMessage(log);
		Puts(log);
	}
	public void MuteList(ConsoleSystem.Arg arg)
	{
		var player = arg.Player();
		if (player == null) return;

		var obj = from x in global::BasePlayer.allPlayerList
				  where x.HasPlayerFlag(global::BasePlayer.PlayerFlags.ChatMute)
				  select new
				  {
					  SteamId = x.UserIDString,
					  Name = x.displayName
				  };

		var log = $"{JsonConvert.SerializeObject(obj, Formatting.Indented)}";
		player.ConsoleMessage(log);
		Puts(log);
	}
	public void Kick(ConsoleSystem.Arg arg)
	{
		var player = arg.Player();
		if (player == null) return;

		var targetPlayer = BasePlayer.FindAwakeOrSleeping(arg.GetString(0));
		if (targetPlayer == null)
		{
			player.ConsoleMessage($"Couldn't find that player.");
			return;
		}

		var reason = arg.Args.Skip(1).ToString(" ") ?? "no reason given";

		Puts($"{player} kicked {targetPlayer}: {reason}");
		Chat.Broadcast($"Kicking {targetPlayer.displayName} ({reason})", "SERVER", "#eee", 0UL);
		targetPlayer.Kick("Kicked: " + reason);
	}
	public void Ban(ConsoleSystem.Arg arg)
	{
		var player = arg.Player();
		if (player == null) return;

		var steamId = arg.GetString(0);
		var validSteamId = steamId.IsSteamId();
		var targetPlayer = BasePlayer.FindAwakeOrSleeping(steamId);
		if (targetPlayer == null)
		{
			player.ConsoleMessage($"Couldn't find that player on the server. {(validSteamId ? "Banning SteamID." : "Invalid steam ID.")}");

			if (!validSteamId)
			{
				return;
			}
		}

		var user = ServerUsers.Get((targetPlayer?.userID) ?? steamId.ToUlong());
		if (user != null && user.group == ServerUsers.UserGroup.Banned)
		{
			arg.ReplyWith($"User {targetPlayer.userID} is already banned.");
			return;
		}

		var reason = arg.GetString(1) ?? "no reason given";
		var expiry = arg.GetInt(2, -1);
		ServerUsers.Set(player.userID, global::ServerUsers.UserGroup.Banned, player.displayName, reason, expiry);
		ServerUsers.Save();

		arg.ReplyWith($"Kickbanned User: {player.userID} - {player.displayName}: {reason}");
		Chat.Broadcast($"Kickbanning {player.displayName} ({reason})", "SERVER", "#eee", 0UL);
		Network.Net.sv.Kick(player.net.connection, $"Banned: {reason}", false);
	}

	private object OnServerMessage(string message, string name)
	{
#if MINIMAL
		if (!ConfigInstance.NoGiveNotices || !(name == "SERVER" && message.Contains("gave"))) return null;
#else
		var core = Community.Runtime.Core.To<CorePlugin>();
		var defaultName = core.DefaultServerChatName != "-1" ? core.DefaultServerChatName : "SERVER";

		if (!ConfigInstance.NoGiveNotices || !(name == defaultName && message.Contains("gave"))) return null;
#endif

		return true;
	}

	private void ToggleCadmin(BasePlayer player, string cmd, string[] args)
	{
		if (player == null)
		{
			Logger.Warn($"This command can only be called by a player (console/chat).");
			return;
		}

		var value = player.HasPlayerFlag(BasePlayer.PlayerFlags.IsDeveloper);
		player.SetPlayerFlag(BasePlayer.PlayerFlags.IsDeveloper, !value);
		player.ChatMessage($"You've {(!value ? "enabled" : "disabled")} <color=orange>cadmin</color> mode.");
	}
}

public class ModerationToolsConfig
{
	[JsonProperty("No give notices")]
	public bool NoGiveNotices = true;

	[JsonProperty("No TechTree unlock")]
	public bool NoTechTreeUnlock = false;

	[JsonProperty("Show server event toasts")]
	public bool ShowServerEventToasts = true;

	public ModerationSettings Moderation = new();

	public class ModerationSettings
	{
		[JsonProperty("/cadmin command")]
		public string CmodCommand = "cadmin";

		[JsonProperty("/cadmin command cooldown (ms)")]
		public int CmodCommandCooldown = 5000;

		[JsonProperty("/cadmin permission (developer)")]
		public string Cmod1Permission = "carbon.cadmin";

		[JsonProperty("/cmod permission (mute, kick, ban)")]
		public string Cmod2Permission = "carbon.cmod";
	}
}
