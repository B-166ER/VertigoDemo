using System.Collections.Generic;
using UnityEngine;

// builds 8-slot wheel loot table, rolls amounts, drives item/death cards
public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }
    public RewardListBehaviour RewardListUI;
    public Transform lastTakenRewardListItemIconTransform; // flying icons target

    [SerializeField] DeathCardBehaviour deathCardBehaviour;
	[SerializeField] ItemCardBehaviour itemCardBehaviour;

	private const int RewardCount = 8;

	[SerializeField] ItemTypes[] excludedTypes;      // never on wheel
	[SerializeField] ItemTypes[] alwaysIncludeTypes; // forced into every roll
	[SerializeField] ItemTypes[] goldRoundItems;     // extra picks on gold zones

    private void Awake()
    {
        if (Instance != null && Instance != this)
		{
			Debug.LogError("Multiple manager instances");
			return;
        }

        Instance = this;
    }

	private void Start()
	{
		if (EventRefrenceManager.Instance != null)
			EventRefrenceManager.Instance.OnRewardingStarted += OnRewardingStarted;
	}

	void OnDestroy()
    {
        if (EventRefrenceManager.Instance != null) 
            EventRefrenceManager.Instance.OnRewardingStarted -= OnRewardingStarted;
    }

	// inventory added item → show card + list row
    void OnRewardingStarted(IntentoryItemBase inventoryItem)
    {
        inventoryItem.ProcessItem();
        RewardListUI.AddListElementInstance(inventoryItem.itemData, inventoryItem.GetAmount());
    }

    public void ShowItemCard(IntentoryItemBase inventoryItem)
    {
        itemCardBehaviour.Setup(inventoryItem);
        itemCardBehaviour.PlayItemCardSpawn(); 
    }

    public void PlayItemCardLeaves()
    {
        itemCardBehaviour?.PlayCardLeaves();
    }

    public void ShowDeathCard()
    {
        deathCardBehaviour.PlayDeathCardSpawn();
    }

	public void RefreshCoinReviveButton()
	{
		deathCardBehaviour.ReviveButtonBeh.RefreshState();
	}

    public void HideDeathCard()
    {
        deathCardBehaviour?.HideDeathCard();
    }

	// random in [MinAmount, MaxAmount] + zone pile bonus
	public int RollRewardAmount(ItemDataSO item)
	{
		int amount = Random.Range(item.MinAmount, item.MaxAmount + 1);
		return ApplyPileZoneBonus(item, amount);
	}

	// non-single-use piles scale up every ~5 zones
	public int ApplyPileZoneBonus(ItemDataSO item, int baseAmount)
	{
		if (item == null || item.SingleUse)
			return baseAmount;

		GameManager gameManager = GameManager.Instance;
		if (gameManager == null || item.MultiplierAmount <= 0)
			return baseAmount;

		int zoneMultiplierTier = Mathf.CeilToInt(gameManager.CurrentZoneId / 5f);
		return baseAmount + zoneMultiplierTier * item.MultiplierAmount;
	}

	public void ReviveFromBomb()
	{
		GameManager.Instance.ReviveFromBomb();
	}

	// shuffle pool → 8 wheel slots respecting gold round / bomb rules
    public List<(ItemDataSO Item, int Amount)> BuildRewardList()
    {
		ItemManager itemManager = ItemManager.Instance;
		bool excludeBomb = GameManager.Instance.ShouldExcludeBombFromWheel();
		bool isGoldRound = GameManager.Instance.GetZoneRoundTier(GameManager.Instance.CurrentZoneId) == GameManager.ZoneRoundTier.Gold;
		var rewards = new List<(ItemDataSO Item, int Amount)>(RewardCount);
		var includedTypeIds = new HashSet<ItemTypes>();

		if (isGoldRound && goldRoundItems != null)
		{
			for (int i = 0; i < goldRoundItems.Length && rewards.Count < RewardCount; i++)
			{
				ItemDataSO item = itemManager.GetItemData(goldRoundItems[i]);
				if (item == null)
					continue;

				rewards.Add((item, RollRewardAmount(item)));
				includedTypeIds.Add(item.TypeId);
			}
		}

		if (alwaysIncludeTypes != null)
		{
			for (int i = 0; i < alwaysIncludeTypes.Length && rewards.Count < RewardCount; i++)
			{
				ItemDataSO item = itemManager.GetItemData(alwaysIncludeTypes[i]);
				if (item == null || IsExcludedFromWheel(item, excludeBomb, isGoldRound) || !includedTypeIds.Add(item.TypeId))
					continue;

				rewards.Add((item, RollRewardAmount(item)));
			}
		}

		List<ItemDataSO> pool = new List<ItemDataSO>();
        foreach (ItemDataSO item in itemManager.ItemTypes)
        {
            if (item == null)
            {
                Debug.LogError("Corrupted Item List in Item manager");
                continue;
            }
			if (IsExcludedFromWheel(item, excludeBomb, isGoldRound))
				continue;
			if (includedTypeIds.Contains(item.TypeId))
				continue;
			pool.Add(item);
        }

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int remainingSlots = RewardCount - rewards.Count;
        int pickCount = Mathf.Min(remainingSlots, pool.Count);
        for (int i = 0; i < pickCount; i++)
            rewards.Add((pool[i], RollRewardAmount(pool[i])));

		for (int i = rewards.Count - 1; i > 0; i--)
		{
			int j = Random.Range(0, i + 1);
			(rewards[i], rewards[j]) = (rewards[j], rewards[i]);
		}

        return rewards;
    }

	bool IsExcludedFromWheel(ItemDataSO item, bool excludeBomb, bool isGoldRound)
	{
		if (excludedTypes != null && System.Array.IndexOf(excludedTypes, item.TypeId) >= 0)
			return true;
		if (!isGoldRound && goldRoundItems != null && System.Array.IndexOf(goldRoundItems, item.TypeId) >= 0)
			return true;
		return excludeBomb && item.TypeId == ItemTypes.death;
	}
}
