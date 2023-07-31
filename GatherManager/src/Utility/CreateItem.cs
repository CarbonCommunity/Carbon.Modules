using System;
using System.Collections.Generic;
using Config;
using Utility;

public sealed class CreateItem
{
	public static Item ByID(int itemID, int Amount, ulong Skin, int Kind)
	{
		ItemDefinition itemDefinition = ItemManager.FindItemDefinition(itemID);
		return ByDefinition(itemDefinition, Amount, Skin, Kind);
	}

	public static Item ByDefinition(ItemDefinition itemDefinition, int Amount, ulong Skin, int Kind)
	{
		var List = Kind switch
		{
			0 => Settings.Config.Pickup,
			1 => Settings.Config.Gather,
			2 => Settings.Config.Quarry,
			3 => Settings.Config.Excavator,
			4 => Settings.Config.Trap,

			_ => throw new Exception("Invalid CreateItemEx kind"),
		};

		string itemName = itemDefinition.shortname;
		if (!List.TryGetValue(itemName, out float Multiplier))
		{
			if (!List.TryGetValue("*", out Multiplier))
			{
				Multiplier = 1f;
			}
		}

		int newAmount = UnityEngine.Mathf.CeilToInt(Amount * Multiplier);
		//Logger.Warning($"CreateItemEx::{Kind} item:{itemName} mul:{Multiplier} out:{newAmount}");
		return ItemManager.Create(itemDefinition, newAmount, Skin);
	}

}