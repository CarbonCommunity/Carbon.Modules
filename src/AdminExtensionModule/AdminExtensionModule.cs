using System;
using Carbon.Base;
using Oxide.Core;

namespace Carbon.Modules;

public partial class AdminExtensionModule : CarbonModule<AdminExtensionConfig, EmptyModuleData>
{
    public override string Name => "AdminExtension";
    public override VersionNumber Version => new(1, 0, 0);
    public override Type Type => typeof(AdminExtensionModule);
    public override bool ForceModded => false;

    public override void OnEnabled(bool initialized)
    {
	    base.OnEnabled(initialized);

	    if (!initialized) return;

	    Permissions.RegisterPermission(ConfigInstance.Spectate.Permission, this);
	    Permissions.RegisterPermission(ConfigInstance.Blind.Permission, this);
	    Permissions.RegisterPermission(ConfigInstance.Empower.Permission, this);
	    Permissions.RegisterPermission(ConfigInstance.PrivateMessage.Permission, this);
	    Permissions.RegisterPermission(ConfigInstance.Lock.Permission, this);

	    Community.Runtime.Core.cmd.AddChatCommand(ConfigInstance.Spectate.Command, this, nameof(CmdSpectate));
	    Community.Runtime.Core.cmd.AddChatCommand(ConfigInstance.Blind.Command, this, nameof(CmdBlind));
	    Community.Runtime.Core.cmd.AddChatCommand(ConfigInstance.Empower.Command, this, nameof(CmdEmpower));
	    Community.Runtime.Core.cmd.AddChatCommand(ConfigInstance.PrivateMessage.Command, this, nameof(CmdPrivateMessage));
	    Community.Runtime.Core.cmd.AddChatCommand(ConfigInstance.Lock.Command, this, nameof(CmdLockPlayerInventory));
    }

    private void CmdSpectate(BasePlayer player, string command, string[] args)
	{
		if (!Permissions.UserHasPermission(player.UserIDString, ConfigInstance.Spectate.Permission)) return;

		if(player.IsSpectating() && args.Length == 0)
		{
			player.StopSpectating();
			return;
		}

		var targetPlayer = BasePlayer.Find(args[0]);
		if (targetPlayer == null)
		{
			player.ChatMessage($"Player '{args[0]}' not found.");
			return;
		}

		AdminModule.StartSpectating(player, targetPlayer);
	}

	private void CmdBlind(BasePlayer player, string command, string[] args)
	{
		if (!Permissions.UserHasPermission(player.UserIDString, ConfigInstance.Blind.Permission)) return;

		var targetPlayer = BasePlayer.Find(args[0]);
		if (targetPlayer == null)
		{
			player.ChatMessage($"Player '{args[0]}' not found.");
			return;
		}

		if (AdminModule.PlayersTab.BlindedPlayers.Contains(targetPlayer))
		{
			AdminModule.UnblindPlayer(targetPlayer);
			player.ChatMessage($"Unblinded {targetPlayer.displayName}.");
			return;
		}
		AdminModule.BlindPlayer(targetPlayer);
		player.ChatMessage($"Blinded {targetPlayer.displayName}.");
	}

	private void CmdEmpower(BasePlayer player, string command, string[] args)
	{
		if (!Permissions.UserHasPermission(player.UserIDString, ConfigInstance.Blind.Permission)) return;

		var targetPlayer = BasePlayer.Find(args[0]);
		if (targetPlayer == null)
		{
			player.ChatMessage($"Player '{args[0]}' not found.");
			return;
		}

		AdminModule.EmpowerPlayerStats(targetPlayer);
		player.ChatMessage($"Empowered {targetPlayer.displayName}.");
	}

	private void CmdPrivateMessage(BasePlayer player, string command, string[] args)
	{
		var targetPlayer = BasePlayer.Find(args[0]);
		if (targetPlayer == null)
		{
			player.ChatMessage($"Player '{args[0]}' not found.");
			return;
		}

		string message = string.Join(" ", args, 1, args.Length - 1);
		AdminModule.PrivateMessagePlayer(player,targetPlayer, message);
	}

	private void CmdLockPlayerInventory(BasePlayer player, string command, string[] args)
	{
		if (!Permissions.UserHasPermission(player.UserIDString, ConfigInstance.Blind.Permission)) return; // fuck you

		var targetPlayer = BasePlayer.Find(args[0]);
		if (targetPlayer == null)
		{
			player.ChatMessage($"Player '{args[0]}' not found.");
			return;
		}

		bool wants = bool.Parse(args[2]);

		switch (args[1])
		{
			case "main":
				AdminModule.LockPlayerContainer(targetPlayer, targetPlayer.inventory.containerMain, wants);
				break;
			case "wear":
				AdminModule.LockPlayerContainer(targetPlayer, targetPlayer.inventory.containerWear, wants);
				break;
			case "belt":
				AdminModule.LockPlayerContainer(targetPlayer, targetPlayer.inventory.containerBelt, wants);
				break;
			case "all":
				AdminModule.LockPlayerContainer(targetPlayer, targetPlayer.inventory.containerBelt, wants);
				AdminModule.LockPlayerContainer(targetPlayer, targetPlayer.inventory.containerWear, wants);
				AdminModule.LockPlayerContainer(targetPlayer, targetPlayer.inventory.containerMain, wants);
				break;
			default:
				player.ChatMessage($"Container '{args[0]}' not found.");
				return;
		}
	}

}

public class AdminExtensionConfig
{
	public class CommandSettings
	{
		public string Command;
		public string Permission;
	}

	public CommandSettings Spectate = new()
	{
		Command = "spectate",
		Permission = "adminextension.spectate"
	};

	public CommandSettings Blind = new()
	{
		Command = "blind",
		Permission = "adminextension.blind"
	};

	public CommandSettings Empower = new()
	{
		Command = "empower",
		Permission = "adminextension.empower"
	};

	public CommandSettings PrivateMessage = new()
	{
		Command = "cpm",
		Permission = "adminextension.pm"
	};

	public CommandSettings Lock = new()
	{
		Command = "lock",
		Permission = "adminextension.lock"
	};
}
