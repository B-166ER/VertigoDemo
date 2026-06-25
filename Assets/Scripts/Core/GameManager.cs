using System.Collections.Generic;
using UnityEngine;

// top-level game loop — zones, wheel refresh, save/load, bomb revive
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

	const string CurrentZoneIdSaveKey = "CurrentZoneId";

    public ItemManager ItemManager;
    public RewardManager RewardManager;
    public WheelManager WheelManager;

    public int CurrentZoneId = 0;
	public int silverRound = 5;   // every Nth zone → silver wheel
	public int goldRound = 10;    // every Nth zone → gold wheel
	public ItemTypes goldCoinType = ItemTypes.gold; // spent to revive from bomb

	int _bombWheelSlotIndex = -1; // which slot had the bomb — swap back on revive

	public enum ZoneRoundTier
	{
		Bronze,
		Silver,
		Gold
	}

    // singleton + survive scene loads
    private void Awake()
    {
        if (Instance != null && Instance != this)
		{
			Debug.LogError("Multiple manager instances");
			return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
		if (EventRefrenceManager.Instance != null)
			EventRefrenceManager.Instance.OnRewardingEnded += HandleRewardingEnded;

		if (!TryLoadSavedGame())
			StartNewGameLoop();
    }

	void OnDestroy()
	{
		if (EventRefrenceManager.Instance != null)
			EventRefrenceManager.Instance.OnRewardingEnded -= HandleRewardingEnded;

		if (Instance == this)
			Instance = null;
	}

	// player took reward off card → next spin round
	void HandleRewardingEnded(ItemDataSO item, int amount)
	{
		StartNewGameLoop();
		SaveGameState();
	}

    // bump zone, refill wheel, broadcast restart
    public void StartNewGameLoop()
    {
        // prepare game for a new spin
        CurrentZoneId++;
        EventRefrenceManager.Instance.RaiseZoneIdChanged(CurrentZoneId);
		// fill the wheel slots with random rewards
		UpdateTheWheel();
        EventRefrenceManager.Instance.RaiseGameLoopRestarted();
    }

    [ContextMenu("Update The Wheel")]
	public void UpdateTheWheel()
    {
        SetWheelColor();
		// get new reward list
		List<(ItemDataSO Item, int Amount)> rewards = RewardManager.BuildRewardList();
        // reach to WheelManager and populate the slots
        WheelManager.PopulateTheWheel(rewards);
		EventRefrenceManager.Instance.RaiseWheelRefreshed();
	}

	// bronze default; silver/gold on interval — zoneId is the round number
	public ZoneRoundTier GetZoneRoundTier(int zoneId)
	{
		if (zoneId > 0 && goldRound > 0 && zoneId % goldRound == 0)
			return ZoneRoundTier.Gold;
		if (zoneId > 0 && silverRound > 0 && zoneId % silverRound == 0)
			return ZoneRoundTier.Silver;
		return ZoneRoundTier.Bronze;
	}

	// bomb only on bronze rounds
	public bool ShouldExcludeBombFromWheel()
	{
		return GetZoneRoundTier(CurrentZoneId) != ZoneRoundTier.Bronze;
	}

	// wheel skin from current tier
	public void SetWheelColor()
	{
		WheelManager.WheelColor wheelColor = GetZoneRoundTier(CurrentZoneId) switch
		{
			ZoneRoundTier.Gold => WheelManager.WheelColor.Golden,
			ZoneRoundTier.Silver => WheelManager.WheelColor.Silver,
			_ => WheelManager.WheelColor.Bronze
		};

		WheelManager.SetWheelColor(wheelColor);
	}

	// inventory calls this when bomb lands — remember slot for revive swap
	public void CacheBombWheelSlot(int slotIndex) => _bombWheelSlotIndex = slotIndex;

	// spend gold, hide death card, replace bomb slot with gold roll
	public void ReviveFromBomb()
	{
		InventoryManager.Instance.RemoveItemsByType(goldCoinType);
		RewardManager.Instance.RewardListUI.RemoveListElementsByType(goldCoinType);
		RewardManager.Instance.HideDeathCard();

		ItemDataSO goldItem = ItemManager.GetItemData(goldCoinType);
		int amount = RewardManager.Instance.RollRewardAmount(goldItem);
		WheelManager.SwapRewardAtSlot(_bombWheelSlotIndex, goldItem, amount);
		_bombWheelSlotIndex = -1;

		SaveGameState();
	}

	// give up / fresh start — zone 1, clear inv + saves
	public void ResetToInitialState()
	{
		CurrentZoneId = 1;
		EventRefrenceManager.Instance?.RaiseZoneSetImmediate(CurrentZoneId);

		if (InventoryManager.Instance != null)
			InventoryManager.Instance.ClearAllInventory();

		ClearSaveData();

		RewardManager.Instance?.HideDeathCard();
		RewardManager.Instance?.RewardListUI?.ClearAllListElements();

		UpdateTheWheel();
	}

	public bool HasSavedGame()
	{
		return PlayerPrefs.HasKey(CurrentZoneIdSaveKey) && PlayerPrefs.GetInt(CurrentZoneIdSaveKey) > 0;
	}

	public void SaveGameState()
	{
		PlayerPrefs.SetInt(CurrentZoneIdSaveKey, CurrentZoneId);
		InventoryManager.Instance?.SaveInventory();
		PlayerPrefs.Save();
	}

	public void ClearSaveData()
	{
		PlayerPrefs.DeleteKey(CurrentZoneIdSaveKey);
		InventoryManager.Instance?.ClearSavedInventory();
		PlayerPrefs.Save();
	}

	// restore zone + inventory from disk, or false → new game
	bool TryLoadSavedGame()
	{
		if (!HasSavedGame())
			return false;

		CurrentZoneId = PlayerPrefs.GetInt(CurrentZoneIdSaveKey);
		EventRefrenceManager.Instance?.RaiseZoneSetImmediate(CurrentZoneId);
		InventoryManager.Instance?.LoadInventory();
		UpdateTheWheel();
		return true;
	}

}
