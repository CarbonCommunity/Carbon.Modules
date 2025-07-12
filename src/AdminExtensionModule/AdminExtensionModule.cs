using System;
using System.Collections.Generic;
using Carbon.Base;
using Carbon.Extensions;
using Oxide.Core;
using ProtoBuf;
using UnityEngine;

namespace Carbon.Modules;

public partial class AdminExtensionModule : CarbonModule<AdminExtensionConfig, EmptyModuleData>
{
    public override string Name => "AdminExtension";
    public override VersionNumber Version => new(1, 0, 0);
    public override Type Type => typeof(AdminExtensionModule);
    public override bool ForceModded => false;

    private readonly HashSet<ulong> _tpmUsers = [];

#if !MINIMAL
    public override void OnEnabled(bool initialized)
    {
	    base.OnEnabled(initialized);

	    if (!initialized) return;

	    Permissions.RegisterPermission(ConfigInstance.Spectate.Permission, this);
	    Permissions.RegisterPermission(ConfigInstance.Blind.Permission, this);
	    Permissions.RegisterPermission(ConfigInstance.Empower.Permission, this);
	    Permissions.RegisterPermission(ConfigInstance.PrivateMessage.Permission, this);
	    Permissions.RegisterPermission(ConfigInstance.Lock.Permission, this);
	    Permissions.RegisterPermission(ConfigInstance.TeleportMarker.Permission, this);

	    Community.Runtime.Core.cmd.AddChatCommand(ConfigInstance.Spectate.Command, this, nameof(CmdSpectate));
	    Community.Runtime.Core.cmd.AddChatCommand(ConfigInstance.Blind.Command, this, nameof(CmdBlind));
	    Community.Runtime.Core.cmd.AddChatCommand(ConfigInstance.Empower.Command, this, nameof(CmdEmpower));
	    Community.Runtime.Core.cmd.AddChatCommand(ConfigInstance.PrivateMessage.Command, this, nameof(CmdPrivateMessage));
	    Community.Runtime.Core.cmd.AddChatCommand(ConfigInstance.Lock.Command, this, nameof(CmdLockPlayerInventory));
	    Community.Runtime.Core.cmd.AddChatCommand(ConfigInstance.TeleportMarker.Command, this, nameof(CmdTeleportMarker));

	    Unsubscribe(nameof(OnMapMarkerAdded));
    }
    public override void OnDisabled(bool initialized)
    {
	    base.OnDisabled(initialized);

	    _tpmUsers.Clear();
    }

    public void TeleportPlayer(BasePlayer player, Vector3 pos)
    {
	    if (!player.IsAlive() || player.IsSpectating())
	    {
		    return;
	    }
	    try
	    {
		    player.PauseFlyHackDetection(5f);
		    player.PauseSpeedHackDetection(5f);
		    player.UpdateActiveItem(default);
		    player.EnsureDismounted();
		    player.Server_CancelGesture();
		    player.SetParent(null, true, true);
		    player.SetServerFall(true);
		    pos.y += 0.1f;
		    player.MovePosition(pos);
		    player.ClientRPC(RpcTarget.Player("ForcePositionTo", player), pos);
		    player.StartSleeping();
		    player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
		    player.ClientRPC(RpcTarget.Player("StartLoading", player));
		    player.SendEntityUpdate();
		    player.UpdateNetworkGroup();
		    player.SendNetworkUpdateImmediate();
	    }
	    finally
	    {
		    player.SetServerFall(false);
		    ServerMgr.Instance.Invoke(player.EndSleeping, 0.5f);
	    }
    }

    private void OnMapMarkerAdded(BasePlayer player, MapNote marker)
    {
	    if (_tpmUsers.Contains(player.userID))
	    {
		    var position = marker.worldPosition + new Vector3(0, TerrainMeta.HeightMap.GetHeight(marker.worldPosition), 0);
		    TeleportPlayer(player, position);
		    Community.Runtime.Core.persistence.Invoke(() =>
		    {
			    player.State.pointsOfInterest.Remove(marker);
			    marker.Dispose();
			    player.DirtyPlayerState();
			    player.SendMarkersToClient();
		    }, 0.5f);
	    }
    }

    [Conditional("!MINIMAL")]
    private void CmdSpectate(BasePlayer player, string _, string[] args)
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

	[Conditional("!MINIMAL")]
	private void CmdBlind(BasePlayer player, string _, string[] args)
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

	[Conditional("!MINIMAL")]
	private void CmdEmpower(BasePlayer player, string _, string[] args)
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

	[Conditional("!MINIMAL")]
	private void CmdPrivateMessage(BasePlayer player, string _, string[] args)
	{
		if (!Permissions.UserHasPermission(player.UserIDString, ConfigInstance.PrivateMessage.Permission)) return;

		var targetPlayer = BasePlayer.Find(args[0]);
		if (targetPlayer == null)
		{
			player.ChatMessage($"Player '{args[0]}' not found.");
			return;
		}

		var message = string.Join(" ", args, 1, args.Length - 1);
		AdminModule.PrivateMessagePlayer(player,targetPlayer, message);
	}

	[Conditional("!MINIMAL")]
	private void CmdLockPlayerInventory(BasePlayer player, string _, string[] args)
	{
		if (!Permissions.UserHasPermission(player.UserIDString, ConfigInstance.Lock.Permission)) return;

		if (args == null || args.Length == 0)
		{
			player.ChatMessage($"No args provided.");
			return;
		}

		var targetPlayer = BasePlayer.Find(args[0]);
		if (targetPlayer == null)
		{
			player.ChatMessage($"Player '{args[0]}' not found.");
			return;
		}

		var shouldToggle = args.Length <= 2;
		var wants = args.Length > 2 && args[2].ToBool();

		switch (args[1])
		{
			case "main":
				AdminModule.LockPlayerContainer(targetPlayer, targetPlayer.inventory.containerMain, shouldToggle ? !targetPlayer.inventory.containerMain.IsLocked() : wants);
				break;
			case "wear":
				AdminModule.LockPlayerContainer(targetPlayer, targetPlayer.inventory.containerWear, shouldToggle ? !targetPlayer.inventory.containerWear.IsLocked() : wants);
				break;
			case "belt":
				AdminModule.LockPlayerContainer(targetPlayer, targetPlayer.inventory.containerBelt, shouldToggle ? !targetPlayer.inventory.containerBelt.IsLocked() : wants);
				break;
			case "all":
				AdminModule.LockPlayerContainer(targetPlayer, targetPlayer.inventory.containerBelt, shouldToggle ? !targetPlayer.inventory.containerBelt.IsLocked() : wants);
				AdminModule.LockPlayerContainer(targetPlayer, targetPlayer.inventory.containerWear, shouldToggle ? !targetPlayer.inventory.containerWear.IsLocked() : wants);
				AdminModule.LockPlayerContainer(targetPlayer, targetPlayer.inventory.containerMain, shouldToggle ? !targetPlayer.inventory.containerMain.IsLocked() : wants);
				break;
			default:
				player.ChatMessage($"Container '{args[0]}' not found.");
				return;
		}
	}

	[Conditional("!MINIMAL")]
	private void CmdTeleportMarker(BasePlayer player, string _, string[] args)
	{
		if (!Permissions.UserHasPermission(player.UserIDString, ConfigInstance.Lock.Permission)) return;

		if (_tpmUsers.Contains(player.userID))
		{
			player.ChatMessage("Teleport Marker disabled.");
			_tpmUsers.Remove(player.userID);

			if (_tpmUsers.Count == 0)
			{
				Unsubscribe(nameof(OnMapMarkerAdded));
			}
			return;
		}

		if (_tpmUsers.Count == 0)
		{
			Subscribe(nameof(OnMapMarkerAdded));
		}

		player.ChatMessage("Teleport Marker enabled.");
		_tpmUsers.Add(player.userID);
	}
#endif
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

	public CommandSettings TeleportMarker = new()
	{
		Command = "tpm",
		Permission = "adminextension.tpm"
	};
}
