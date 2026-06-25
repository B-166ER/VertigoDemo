using System.Collections.Generic;
using UnityEngine;

// editor / dev helper — fill all wheel slots with one item type
public class DebugManager : MonoBehaviour
{
	public static DebugManager Instance { get; private set; }

	[SerializeField] ItemTypes _wheelFillItemId = ItemTypes.cash;
	[SerializeField] int _wheelSlotAmount = 1;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Debug.LogError("Multiple DebugManager instances");
			return;
		}

		Instance = this;
	}

	void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}

	[ContextMenu("Fill Wheel With Selected Item")]
	public void FillWheelWithSelectedItem()
	{
		if (ItemManager.Instance == null)
		{
			Debug.LogError("DebugManager: ItemManager instance not found.");
			return;
		}

		ItemDataSO itemData = ItemManager.Instance.GetItemData(_wheelFillItemId);
		if (itemData == null)
		{
			Debug.LogError($"DebugManager: No ItemDataSO found for {_wheelFillItemId}.");
			return;
		}

		if (WheelManager.Instance == null)
		{
			Debug.LogError("DebugManager: WheelManager instance not found.");
			return;
		}

		int slotCount = WheelManager.Instance.WheelSlots.Count;
		var rewards = new List<(ItemDataSO Item, int Amount)>(slotCount);

		for (int i = 0; i < slotCount; i++)
			rewards.Add((itemData, _wheelSlotAmount));

		WheelManager.Instance.PopulateTheWheel(rewards);
		EventRefrenceManager.Instance?.RaiseWheelRefreshed();
	}
}
