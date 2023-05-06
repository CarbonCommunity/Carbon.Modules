using System;
using System.Collections.Generic;
using System.Linq;
using API.Assembly;
using Config;
using Utility;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * Copyright (c) 2022 kasvoton
 * All rights reserved.
 *
 */

public sealed class StackManager : ICarbonModule
{
	private readonly Dictionary<string, int> _cache = new();

	public void Awake(EventArgs args)
	{
		Settings.LoadConfig();
	}

	public void OnLoaded(EventArgs args)
	{
		Logger.Log(">> OnLoaded");
	}

	public void OnUnloaded(EventArgs args)
	{
		Logger.Log(">> OnUnloaded");
	}

	public void OnEnable(EventArgs args)
	{
		foreach (KeyValuePair<ItemCategory, float> category in Settings.Config.Categories)
		{
			foreach (ItemDefinition item in ItemManager.itemList.Where(x => x.category == category.Key &&
				!Settings.Config.Blacklist.Contains(x.shortname) && !Settings.Config.Items.ContainsKey(x.shortname)))
			{
				_cache.Add(item.shortname, item.stackable);

				item.stackable = UnityEngine.Mathf.Clamp(
					UnityEngine.Mathf.CeilToInt(item.stackable * category.Value * Settings.Config.GlobalMultiplier), 1, int.MaxValue);
			}
		}

		foreach (ItemDefinition item in ItemManager.itemList.Where(
			x => Settings.Config.Items.ContainsKey(x.shortname)))
		{
			_cache.Add(item.shortname, item.stackable);

			item.stackable = UnityEngine.Mathf.Clamp(
				UnityEngine.Mathf.CeilToInt(item.stackable * Settings.Config.Items[item.shortname]), 1, int.MaxValue);
		}
	}

	public void OnDisable(EventArgs args)
	{
		foreach (ItemDefinition item in ItemManager.itemList.Where(x => _cache.ContainsKey(x.shortname)))
		{
			item.stackable = _cache[item.shortname];
			_cache.Remove(item.shortname);
		}
	}
}