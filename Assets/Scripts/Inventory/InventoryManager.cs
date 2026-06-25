using System;
using System.Collections.Generic;
using UnityEngine;

// owns runtime inventory + item class polymorphism
// listens to spin complete, picks pile / consumable / bomb / converted path
// also syncs PlayerPrefs save and hands off to reward UI via events
public class InventoryManager : MonoBehaviour
{
	public static InventoryManager Instance { get; private set; }

	[SerializeReference]
	HashSet<IntentoryItemBase> _inventory = new HashSet<IntentoryItemBase>(); 

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Debug.LogError("Multiple InventoryManager instances");
			return;
		}

		Instance = this;
	}

	void Start()
	{
		if (EventRefrenceManager.Instance != null)
			EventRefrenceManager.Instance.OnWheelSpinCompleted += OnWheelRotationEnded;
	}

	void OnDestroy()
	{
		if (EventRefrenceManager.Instance != null)
			EventRefrenceManager.Instance.OnWheelSpinCompleted -= OnWheelRotationEnded;

		if (Instance == this)
			Instance = null;
	}

	void OnWheelRotationEnded(ItemDataSO itemData, int amount)
	{
		AddItem(itemData, amount);
	}

	// route item to correct inventory subclass
	public void AddItem(ItemDataSO itemData, int amount)
	{
		if (itemData.TypeId == ItemTypes.death)
		{
			GameManager.Instance.CacheBombWheelSlot(WheelManager.Instance.LastSpinSlotIndex);
			InventoryItemBomb bomb = new InventoryItemBomb(itemData, 0);
			bomb.ProcessItem();
		}
		else if (itemData.SingleUse)
		{
			if (HasItemOfType(itemData.TypeId))
			{
				if (itemData.ConvertIntoIfAlreadyHaveIt != null && itemData.ConvertAmount > 0)
				{
					InventoryItemConvertedReward item = new InventoryItemConvertedReward(
						itemData,
						itemData.ConvertIntoIfAlreadyHaveIt,
						itemData.ConvertAmount);
					AddAndNotify(item);
				}
				else
					Debug.LogWarning($"Already own {itemData.Name} but no conversion is configured.", this);
			}
			else
			{
				InventoryItemConsumable item = new InventoryItemConsumable(itemData, amount);
				AddAndNotify(item);
			}
		}
		else
		{
			IntentoryItemPile item = new IntentoryItemPile(itemData, amount);
			AddAndNotify(item);
		}
	}

	// last item that triggered reward flow — cards read this
	public static IntentoryItemBase RewardedItem;

	void AddAndNotify(IntentoryItemBase item)
	{
		_inventory.Add(item);
		RewardedItem = item;
		EventRefrenceManager.Instance?.RaiseRewardingStarted(item);
	}
	 
	public void RemoveItem(ItemDataSO itemData)
	{  
		_inventory.RemoveWhere(entry =>
		{ 
			if (entry.itemData.TypeId != itemData.TypeId)
				return false;
			 
			return true;
		});
	}

	public bool HasItemOfType(ItemTypes type)
	{
		foreach (IntentoryItemBase entry in _inventory)
		{
			if (entry.itemData.TypeId == type && entry.GetAmount() > 0)
				return true;
		}

		return false;
	}

	public void RemoveItemsByType(ItemTypes type)
	{
		_inventory.RemoveWhere(entry => entry.itemData.TypeId == type);

		string key = type.ToString();
		if (PlayerPrefs.HasKey(key))
			PlayerPrefs.DeleteKey(key);

		PlayerPrefs.Save();
	}

	public void ClearAllInventory()
	{
		_inventory.Clear();
		RewardedItem = null;
	}

	public void ClearSavedInventory()
	{
		foreach (ItemTypes type in Enum.GetValues(typeof(ItemTypes)))
		{
			string key = type.ToString();
			if (PlayerPrefs.HasKey(key))
				PlayerPrefs.DeleteKey(key);
		}

		PlayerPrefs.Save();
	}

	public void SaveInventory()
	{
		ClearSavedInventory();

		Dictionary<string, int> savedAmounts = new Dictionary<string, int>();

		foreach (IntentoryItemBase entry in _inventory)
		{
			string key = entry.itemData.TypeId.ToString();
			int amount = entry.GetAmount();

			if (savedAmounts.TryGetValue(key, out int existing))
				savedAmounts[key] = existing + amount;
			else
				savedAmounts[key] = amount;
		}

		foreach (KeyValuePair<string, int> pair in savedAmounts)
			PlayerPrefs.SetInt(pair.Key, pair.Value);
	}

	// rebuild inventory + reward list rows from PlayerPrefs
	public void LoadInventory()
	{
		_inventory.Clear();
		RewardedItem = null;

		ItemManager itemManager = ItemManager.Instance;
		if (itemManager == null)
			return;

		foreach (ItemTypes type in Enum.GetValues(typeof(ItemTypes)))
		{
			if (type == ItemTypes.death || type == ItemTypes.debug_only)
				continue;

			string key = type.ToString();
			if (!PlayerPrefs.HasKey(key))
				continue;

			int amount = PlayerPrefs.GetInt(key);
			if (amount <= 0)
				continue;

			ItemDataSO itemData = itemManager.GetItemData(type);
			if (itemData == null)
				continue;

			IntentoryItemBase item = itemData.SingleUse
				? new InventoryItemConsumable(itemData, 1)
				: new IntentoryItemPile(itemData, amount);
			_inventory.Add(item);

			RewardManager.Instance?.RewardListUI?.AddListElementInstance(itemData, amount);
		}
	}

}

// base for inventory entries — card text + ProcessItem hook
[System.Serializable]
public abstract class IntentoryItemBase
{
	public ItemDataSO itemData;
	int _rewardAmount;

	protected IntentoryItemBase(ItemDataSO itemData, int rewardAmount = 1)
	{
		this.itemData = itemData;
		_rewardAmount = Mathf.Max(0, rewardAmount);
	}

	// show card / death UI etc
	public abstract void ProcessItem();

	// flying icons to list — after card anim
	public virtual void TriggerItemTakeOperations() { }

	public virtual int GetAmount() => _rewardAmount;

	public virtual ItemDataSO GetCardDisplayItemData() => itemData;

	public virtual string GetCardTitle()
	{
		ItemDataSO displayItem = GetCardDisplayItemData();
		return displayItem != null ? displayItem.CardDisplayTextTitle : string.Empty;
	}

	public virtual string GetCardDescription()
	{
		ItemDataSO displayItem = GetCardDisplayItemData();
		return displayItem != null ? displayItem.CardDisplayTextDescription : string.Empty;
	}

	protected void SetRewardAmount(int amount) => _rewardAmount = Mathf.Max(0, amount);
}

// stackable currency-style rewards
[System.Serializable]
public class IntentoryItemPile : IntentoryItemBase
{
	public int Amount; 

	public IntentoryItemPile(ItemDataSO itemData, int amount) : base(itemData, amount)
	{
		Amount = Mathf.Max(0, amount);
	}

	public override void ProcessItem()
	{
		RewardManager.Instance.ShowItemCard(this);
	}

	public override void TriggerItemTakeOperations()
	{
		RewardManager.Instance?.RewardListUI?.PlayFlyingRewards(this);
	}

	public override int GetAmount() => Amount;

	public void Stack(int addAmount)
	{
		Amount += addAmount;
	}

}

// one-time unlock — helmet, weapon skin, etc
[System.Serializable]
public class InventoryItemConsumable : IntentoryItemBase
{
	public InventoryItemConsumable(ItemDataSO itemData, int amount) : base(itemData, amount) { }

	public override void ProcessItem()
	{
		if (itemData.SingleUse)
			SetRewardAmount(1);
		RewardManager.Instance.ShowItemCard(this);
	}

	public override void TriggerItemTakeOperations()
	{
		RewardManager.Instance?.RewardListUI?.PlayFlyingRewards(this);
	}
}

// already owned single-use → show original icon, grant converted pile
[System.Serializable]
public class InventoryItemConvertedReward : IntentoryItemPile
{
	public const string ConvertedCardDescription = "You already Own This, It Is Being Converted";

	readonly ItemDataSO _originalItemData;

	public InventoryItemConvertedReward(ItemDataSO originalItemData, ItemDataSO convertedItemData, int convertAmount)
		: base(convertedItemData, convertAmount)
	{
		_originalItemData = originalItemData;
	}

	public override ItemDataSO GetCardDisplayItemData() => _originalItemData ?? itemData;

	public override string GetCardDescription() => ConvertedCardDescription;
}

// bomb slot — death card, maybe wipe save if no gold revive
[System.Serializable]
public class InventoryItemBomb : IntentoryItemBase
{ 
	public InventoryItemBomb(ItemDataSO itemData, int amount) : base(itemData, amount) { }

	public override void ProcessItem()
	{
		RewardManager.Instance.ShowDeathCard();
		RewardManager.Instance.RefreshCoinReviveButton();

		if (!InventoryManager.Instance.HasItemOfType(GameManager.Instance.goldCoinType))
			GameManager.Instance.ClearSaveData();
	}
}

// HashSet.Remove(predicate) helper — legacy, prefer LINQ elsewhere
static class InventoryHashSetExtensions
{
	public static int Remove<T>(this HashSet<T> set, Predicate<T> match)
	{
		List<T> toRemove = new List<T>();

		foreach (T item in set)
		{
			if (match(item))
				toRemove.Add(item);
		}

		int count = 0;

		for (int i = 0; i < toRemove.Count; i++)
		{
			if (set.Remove(toRemove[i]))
				count++;
		}

		return count;
	}
}
