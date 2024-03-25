﻿using System;
using System.Collections.Generic;
using System.Linq;
using Carbon.Base;
using Facepunch.Rust;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using static BaseEntity;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * Copyright (c) 2023 kasvoton
 * All rights reserved.
 *
 */

namespace Carbon.Modules;
#pragma warning disable IDE0051

public partial class GatherManagerModule : CarbonModule<GatherManagerConfig, EmptyModuleData>
{
	public static GatherManagerModule Singleton { get; internal set; }

	public override string Name => "GatherManager";
	public override VersionNumber Version => new(1, 0, 0);
	public override bool ForceModded => true;
	public override Type Type => typeof(GatherManagerModule);

	public override bool EnabledByDefault => false;

	internal Item _processedItem;

	public override void Init()
	{
		base.Init();

		Singleton = this;
	}

	#region Hooks

	private object OnCollectiblePickup(CollectibleEntity entity, BasePlayer reciever, bool eat)
	{
		foreach (var itemAmount in entity.itemList)
		{
			var item = ByDefinition(itemAmount.itemDef, (int)itemAmount.amount, 0, 0);
			if (item == null)
			{
				continue;
			}

			if (eat && item.info.category == ItemCategory.Food && reciever != null)
			{
				var component = item.info.GetComponent<ItemModConsume>();
				if (component != null)
				{
					component.DoAction(item, reciever);
					continue;
				}
			}

			if ((bool)reciever)
			{
				Analytics.Azure.OnGatherItem(item.info.shortname, item.amount, entity, reciever);
				reciever.GiveItem(item, GiveItemReason.ResourceHarvested);
			}
			else
			{
				item.Drop(entity.transform.position + Vector3.up * 0.5f, Vector3.up);
			}
		}

		if (entity.pickupEffect.isValid)
		{
			Effect.server.Run(entity.pickupEffect.resourcePath, entity.transform.position, entity.transform.up);
		}

		var randomItemDispenser = PrefabAttribute.server.Find<RandomItemDispenser>(entity.prefabID);
		if (randomItemDispenser != null)
		{
			randomItemDispenser.DistributeItems(reciever, entity.transform.position);
		}

		NextFrame(() =>
		{
			if (entity == null || entity.IsDestroyed) return;

			entity.Kill();
		});

		return false;
	}
	private void OnExcavatorGather(ExcavatorArm arm, Item item)
	{
		item.amount = GetAmount(item.info, item.amount, 3);
	}
	private void OnQuarryGather(MiningQuarry quarry, Item item)
	{
		item.amount = GetAmount(item.info, item.amount, 3);
	}
	private void OnGrowableGathered(GrowableEntity entity, Item item, BasePlayer player)
	{
		item.amount = GetAmount(item.info, item.amount, 1);
	}
	private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
	{
		item.amount = GetAmount(item.info, item.amount, 1);
	}
	private void OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
	{
		if (_processedItem == item) return;
		_processedItem = item;

		item.amount = GetAmount(item.info, item.amount, 1);
	}
	private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
	{
		if (_processedItem == item) return;
		_processedItem = item;

		item.amount = GetAmount(item.info, item.amount, 1);
	}

	public object IOvenSmeltSpeedOverride(BaseOven oven)
	{
		if (Enumerable.Contains(Singleton.ConfigInstance.OvenSpeedOverrideBlacklist, oven.ShortPrefabName) ||
			Enumerable.Contains(Singleton.ConfigInstance.OvenSpeedOverrideBlacklist, oven.GetType().Name))
		{
			return Singleton.ConfigInstance.OvenSpeedBlacklistedOverride;
		}

		return Singleton.ConfigInstance.OvenSpeedOverride;
	}

	#endregion

	#region Helpers

	internal Item ByID(int itemID, int amount, ulong skin, int kind)
	{
		return ByDefinition(ItemManager.FindItemDefinition(itemID), amount, skin, kind);
	}
	internal Item ByDefinition(ItemDefinition itemDefinition, int amount, ulong skin, int kind)
	{
		return ItemManager.Create(itemDefinition, GetAmount(itemDefinition, amount, kind), skin);
	}
	internal int GetAmount(ItemDefinition itemDefinition, int amount, int kind)
	{
		var dictionary = kind switch
		{
			0 => ConfigInstance.Pickup,
			1 => ConfigInstance.Gather,
			2 => ConfigInstance.Quarry,
			3 => ConfigInstance.Excavator,
			_ => throw new Exception("Invalid GetAmount kind"),
		};

		if (!dictionary.TryGetValue(itemDefinition.shortname, out var multiply) && !dictionary.TryGetValue("*", out multiply))
		{
			multiply = 1f;
		}

		return Mathf.CeilToInt(amount * multiply);
	}

	#endregion
}

public class GatherManagerConfig
{
	public float OvenSpeedOverride = 0.5f;
	public float OvenSpeedBlacklistedOverride = 0.5f;

	[JsonProperty("OvenSpeedOverrideBlacklist (prefab shortname, type)")]
	public string[] OvenSpeedOverrideBlacklist = new string[]
	{
		"lantern.deployed",
		"tunalight.deployed",
		"chineselantern.deployed"
	};

	public Dictionary<string, float> Quarry = new()
	{
		["*"] = 1f
	};
	public Dictionary<string, float> Excavator = new()
	{
		["*"] = 1f
	};
	public Dictionary<string, float> Pickup = new()
	{
		["*"] = 1f,
		["seed.black.berry"] = 1f,
		["seed.blue.berry"] = 1f,
		["seed.corn"] = 1f,
		["seed.green.berry"] = 1f,
		["seed.hemp"] = 1f,
		["seed.potato"] = 1f,
		["seed.pumpkin"] = 1f,
		["seed.red.berry"] = 1f,
		["seed.white.berry"] = 1f,
		["seed.yellow.berry"] = 1f
	};
	public Dictionary<string, float> Gather = new()
	{
		["*"] = 1f,
		["skull.wolf"] = 1f,
		["skull.human"] = 1f
	};
}
