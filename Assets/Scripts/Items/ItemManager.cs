using System.Collections.Generic;
using UnityEngine;

// registry of all ItemDataSO assets — lookup by ItemTypes enum
public class ItemManager : MonoBehaviour
{
	public static ItemManager Instance { get; private set; }

	public List<ItemDataSO> ItemTypes;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Debug.LogError("Multiple manager instances");
			return;
		}

		Instance = this; 
	}

	// first SO matching type id
	public ItemDataSO GetItemData(ItemTypes itemType)
	{
		return ItemTypes.Find(item => item.TypeId == itemType);
	}
}
