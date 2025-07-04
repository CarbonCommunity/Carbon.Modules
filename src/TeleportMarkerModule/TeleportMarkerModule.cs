using System;
using System.Collections.Generic;
using Carbon.Base;
using Oxide.Core;
using ProtoBuf;
using UnityEngine;

namespace Carbon.Modules;

public partial class TeleportMarkerModule : CarbonModule<TeleportMarkerModule, EmptyModuleData>
{
    internal static WhitelistModule Singleton { get; set; }

    public override string Name => "TeleportMarker";
    public override VersionNumber Version => new(1, 0, 0);
    public override Type Type => typeof(TeleportMarkerModule);
    public override bool ForceModded => false;

    private readonly HashSet<string> _tpmUsers = [];

    private const string PermTpm = "teleportmarker.use";

    private void Init()
    {
        Community.Runtime.Core.permission.RegisterPermission(PermTpm, this);
        Community.Runtime.Core.cmd.AddChatCommand("tpm", this, nameof(CmdTpm));
        Unsubscribe(nameof(OnMapMarkerAdded));
    }

    private void OnMapMarkerAdded(BasePlayer player, MapNote marker)
    {
        if (_tpmUsers.Contains(player.UserIDString))
        {
            TeleportToMarker(player, marker);
        }
    }

    private void TeleportToMarker(BasePlayer player, MapNote marker)
    {
        var position = marker.worldPosition + new Vector3(0, TerrainMeta.HeightMap.GetHeight(marker.worldPosition), 0);
        TeleportToPos(player, position);
        ServerMgr.Instance.Invoke(() => RemoveMarker(player, marker), 0.5f);
    }

    private static void RemoveMarker(BasePlayer player, MapNote marker)
    {
        player.State.pointsOfInterest.Remove(marker);
        marker.Dispose();
        player.DirtyPlayerState();
        player.SendMarkersToClient();
    }

    private void TeleportToPos(BasePlayer player, Vector3 destination)
    {
        if (!player.IsAlive() || player.IsSpectating()) return;
        try
        {
            player.PauseFlyHackDetection(5f);
            player.PauseSpeedHackDetection(5f);
            player.UpdateActiveItem(default);
            player.EnsureDismounted();
            player.Server_CancelGesture();
            player.SetParent(null, true, true);
            player.SetServerFall(true);
            destination.y += 0.1f;
            player.MovePosition(destination);
            player.ClientRPC(RpcTarget.Player("ForcePositionTo", player), destination);
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

    private bool CheckPermission(BasePlayer player, string perm)
    {
        return Community.Runtime.Core.permission.UserHasPermission(player.UserIDString, perm);
    }

    private void CmdTpm(BasePlayer player, string command, string[] args)
    {
	    if (!CheckPermission(player, PermTpm)) return;

        if (_tpmUsers.Contains(player.UserIDString))
        {
            player.ChatMessage("Teleport Marker disabled.");
            _tpmUsers.Remove(player.UserIDString);

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
        _tpmUsers.Add(player.UserIDString);
    }
}
