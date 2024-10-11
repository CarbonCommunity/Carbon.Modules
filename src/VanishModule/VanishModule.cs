using System;
using System.Collections.Generic;
using System.Linq;
using Carbon.Base;
using Carbon.Components;
using Facepunch;
using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Rust.Ai;
using UnityEngine;

namespace Carbon.Modules;

public partial class VanishModule : CarbonModule<VanishConfig, EmptyModuleData>
{
	public override string Name => "Vanish";
	public override Type Type => typeof(VanishModule);
	public override VersionNumber Version => new(1, 0, 0);
	public override bool ForceModded => false;
	public override bool EnabledByDefault => false;

	public readonly CUI.Handler Handler = new();

	internal Dictionary<ulong, Vector3> _vanishedPlayers = new(500);

	internal readonly GameObjectRef _drownEffect = new() { guid = "28ad47c8e6d313742a7a2740674a25b5" };
	internal readonly GameObjectRef _fallDamageEffect = new() { guid = "ca14ed027d5924003b1c5d9e523a5fce" };
	internal readonly GameObjectRef _emptyEffect = new();

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

		Permissions.RegisterPermission(ConfigInstance.VanishPermission, this);
		Permissions.RegisterPermission(ConfigInstance.VanishUnlockWhileVanishedPermission, this);

		Community.Runtime.Core.cmd.AddCovalenceCommand(ConfigInstance.VanishCommand, this, nameof(Vanish), permissions: new [] { ConfigInstance.VanishPermission });
	}
	public override void OnDisabled(bool initialized)
	{
		base.OnDisabled(initialized);

		foreach (var vanished in _vanishedPlayers)
		{
			DoVanish(BasePlayer.FindByID(vanished.Key), false);
		}

		_vanishedPlayers.Clear();
	}

	private object CanUseLockedEntity(BasePlayer player, BaseLock @lock)
	{
		if (_vanishedPlayers.ContainsKey(player.userID)
			&& Permissions.UserHasPermission(player.UserIDString, ConfigInstance.VanishUnlockWhileVanishedPermission))
		{
			return true;
		}

		return null;
	}
	private object OnPlayerAttack(BasePlayer player, HitInfo hit)
	{
		if (hit == null || hit.Initiator == null || hit.HitEntity == null) return null;

		if (hit.Initiator is BasePlayer attacker && hit.HitEntity.OwnerID != attacker.userID)
		{
			if (!ConfigInstance.CanDamageWhenVanished && _vanishedPlayers.ContainsKey(attacker.userID))
			{
				var owner = BasePlayer.FindByID(hit.HitEntity.OwnerID);
				player.ChatMessage($"You're vanished. You may not damage this entity owned by {owner?.displayName ?? hit.HitEntity.OwnerID.ToString()}.");
				return false;
			}
		}

		return null;
	}
	private void OnPlayerConnected(BasePlayer player)
	{
		if (!_vanishedPlayers.ContainsKey(player.userID)) return;

		DoVanish(player, true);
	}
	private object CanBradleyApcTarget(BradleyAPC apc, BasePlayer player)
	{
		if (_vanishedPlayers.ContainsKey(player.userID))
		{
			return false;
		}

		return null;
	}

	public static void SendEffectTo(string effect, BasePlayer player)
	{
		if (player == null) return;

		var effectInstance = new Effect();
		effectInstance.Init(Effect.Type.Generic, player, 0, Vector3.up, Vector3.zero);
		effectInstance.pooledstringid = StringPool.Get(effect);

		var netWrite = Net.sv.StartWrite();
		netWrite.PacketID(Message.Type.Effect);
		effectInstance.WriteToStream(netWrite);
		netWrite.Send(new SendInfo(player.net.connection));

		effectInstance.Clear();
	}

	public void DoVanish(BasePlayer player, bool wants, bool withUI = true, bool toggleNoclip = true)
	{
		if (wants)
		{
			_clearTriggers(player);

			player.PauseFlyHackDetection();
			AntiHack.ShouldIgnore(player);

			player.fallDamageEffect = _emptyEffect;
			player.drownEffect = _emptyEffect;

			player._limitedNetworking = true;
			player.DisablePlayerCollider();

			var temp = Pool.Get<List<Connection>>();
			temp.AddRange(Net.sv.connections.Where(connection => connection.connected && connection.isAuthenticated && connection.player is BasePlayer && connection.player != player));
			player.OnNetworkSubscribersLeave(temp);
			Pool.FreeUnmanaged(ref temp);

			SimpleAIMemory.AddIgnorePlayer(player);

			if (ConfigInstance.WhooshSoundOnVanish)
			{
				if (ConfigInstance.BroadcastVanishSounds)
				{
					Effect.server.Run(ConfigInstance.Effect.Vanishing, player.transform.position);
				}
				else
				{
					SendEffectTo(ConfigInstance.Effect.Vanishing, player);
				}
			}

			if (withUI) _drawUI(player);

			if (ConfigInstance.EnableLogs) Puts($"{player} just vanished at {player.transform.position}");

			if (ConfigInstance.ToggleNoclipOnVanish && toggleNoclip && player.net.connection.authLevel > 0 && !player.IsFlying)
			{
				player.SendConsoleCommand("noclip");
			}

			var vanishObject = new GameObject("Vanish Collider");
			vanishObject.transform.SetParent(player.transform);
			vanishObject.AddComponent<VanishedPlayer>().Init(player);
		}
		else
		{
			player.ResetAntiHack();
			player._limitedNetworking = false;

			player.EnablePlayerCollider();
			player.SendNetworkUpdate();

			player.GetHeldEntity()?.SendNetworkUpdate();
			SimpleAIMemory.RemoveIgnorePlayer(player);

			player.drownEffect = _drownEffect;
			player.fallDamageEffect = _fallDamageEffect;

			player.ForceUpdateTriggers(enter: true, exit: false, invoke: true);

			if (ConfigInstance.GutshotScreamOnUnvanish)
			{
				if (ConfigInstance.BroadcastVanishSounds)
				{
					Effect.server.Run(ConfigInstance.Effect.Unvanishing, player.transform.position);
				}
				else
				{
					SendEffectTo(ConfigInstance.Effect.Unvanishing, player);
				}
			}

			using var cui = new CUI(Handler);
			cui.Destroy("vanishui", player);

			if (ConfigInstance.EnableLogs) Puts($"{player} unvanished at {player.transform.position}");

			if (ConfigInstance.ToggleNoclipOnUnvanish && toggleNoclip && player.net.connection.authLevel > 0 && player.IsFlying)
			{
				player.SendConsoleCommand("noclip");
			}

			var vanishMono = player.GetComponentInChildren<VanishedPlayer>();

			if(vanishMono != null)
			{
				GameObject.Destroy(vanishMono.gameObject);
			}
		}
	}

	private void Vanish(BasePlayer player, string cmd, string[] args)
	{
		var wants = false;

		if (_vanishedPlayers.TryGetValue(player.userID, out var originalPosition))
		{
			_vanishedPlayers.Remove(player.userID);
			if (ConfigInstance.TeleportBackOnUnvanish) player.Teleport(originalPosition);
		}
		else
		{
			_vanishedPlayers.Add(player.userID, player.transform.position);
			wants = true;
		}

		DoVanish(player, wants);
	}

	internal void _clearTriggers(BasePlayer player)
	{
		if (player.triggers != null && player.triggers.Count > 0)
		{
			foreach (var trigger in player.triggers)
			{

				trigger.OnEntityLeave(player);
			}
		}

		using var helis = Entities.Get<PatrolHelicopter>();
		helis.Each(heli =>
		{
			if (heli.myAI == null || heli.myAI.strafe_target != player) return;
			Logger.Warn($"Patrol Helicopter at {heli.transform.position} ended player strafe for '{player.Connection}'");

			heli.myAI.State_OrbitStrafe_Leave();
			heli.myAI.State_Strafe_Leave();
		});
	}

	internal void _drawUI(BasePlayer player)
	{
		using var cui = new CUI(Handler);
		var container = cui.CreateContainer("vanishui", parent: CUI.ClientPanels.Hud);
		if (!string.IsNullOrEmpty(ConfigInstance.InvisibleText))
		{
			var textX = ConfigInstance.InvisibleTextAnchorX;
			var textY = ConfigInstance.InvisibleTextAnchorY;
			cui.CreateText(container, "vanishui", color: ConfigInstance.InvisibleTextColor, ConfigInstance.InvisibleText, ConfigInstance.InvisibleTextSize,
				xMin: textX[0], xMax: textX[1], yMin: textY[0], yMax: textY[1], align: ConfigInstance.InvisibleTextAnchor);
		}

		if (!string.IsNullOrEmpty(ConfigInstance.InvisibleIconUrl))
		{
			var iconX = ConfigInstance.InvisibleIconAnchorX;
			var iconY = ConfigInstance.InvisibleIconAnchorY;
			cui.CreateClientImage(container, "vanishui", ConfigInstance.InvisibleIconUrl, ConfigInstance.InvisibleIconColor,
				xMin: iconX[0], xMax: iconX[1], yMin: iconY[0], yMax: iconY[1]);
		}

		cui.Send(container, player);
	}

	public class VanishedPlayer : FacepunchBehaviour
	{
		public BasePlayer player;

		public void Init(BasePlayer player)
		{
			this.player = player;
			gameObject.layer = (int)Rust.Layer.Reserved1;
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.transform.localRotation = Quaternion.identity;

			var playerCollider = player.colliderValue.Get();
			var vanishCollider = gameObject.AddComponent<CapsuleCollider>();
			vanishCollider.center = playerCollider.center;
			vanishCollider.radius = playerCollider.radius;
			vanishCollider.height = playerCollider.height;
			vanishCollider.direction = playerCollider.direction;
			vanishCollider.isTrigger = true;

			var colliders = Pool.Get<List<Collider>>();
			Vis.Components(gameObject.transform.position, vanishCollider.radius, colliders);

			foreach(var collider in colliders)
			{
				OnTriggerEnter(collider);
			}

			Pool.FreeUnmanaged(ref colliders);
		}

		private void OnTriggerEnter(Collider collider)
		{
			var parent = collider.gameObject.GetComponent<TriggerParent>();

			if (parent == null)
			{
				return;
			}

			parent.OnEntityEnter(player);
		}

		private void OnTriggerExit(Collider collider)
		{
			var parent = collider.gameObject.GetComponent<TriggerParent>();

			if (parent == null)
			{
				return;
			}

			parent.OnEntityLeave(player);
		}
	}
}

public class VanishConfig
{
	[JsonProperty("[Anchor] Legend")]
	public string AnchorLegend => "(0=UpperLeft, 1=UpperCenter, 2=UpperRight, 3=MiddleLeft, 4=MiddleCenter, 5=MiddleRight, 6=LowerLeft, 7=LowerCenter, 8=LowerRight)";

	public string VanishPermission = "vanish.allow";
	public string VanishUnlockWhileVanishedPermission = "vanish.unlock";
	public string VanishCommand = "vanish";
	public bool ToggleNoclipOnVanish = true;
	public bool ToggleNoclipOnUnvanish = false;

	public string InvisibleText = "You are currently invisible.";
	public int InvisibleTextSize = 10;
	public string InvisibleTextColor = "#8bba49";
	[JsonProperty("InvisibleTextAnchor [Anchor]")]
	public TextAnchor InvisibleTextAnchor = TextAnchor.LowerCenter;
	public float[] InvisibleTextAnchorX = [0, 1];
	public float[] InvisibleTextAnchorY = [0, 0.025f];

	public string InvisibleIconUrl = "";
	public string InvisibleIconColor = "1 1 1 0.3";
	public float[] InvisibleIconAnchorX = [0.175f, 0.22f];
	public float[] InvisibleIconAnchorY = [0.017f, 0.08f];

	public EffectConfig Effect = new();

	public bool BroadcastVanishSounds = false;
	public bool WhooshSoundOnVanish = true;
	public bool GutshotScreamOnUnvanish = true;
	public bool EnableLogs = true;
	public bool TeleportBackOnUnvanish = false;
	public bool CanDamageWhenVanished = true;

	public class EffectConfig
	{
		public string Vanishing = "assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab";
		public string Unvanishing = "assets/bundled/prefabs/fx/player/gutshot_scream.prefab";
	}
}
