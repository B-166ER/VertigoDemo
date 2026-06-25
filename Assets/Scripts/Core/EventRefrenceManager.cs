using System;
using UnityEngine;

// global event bus — decouple wheel / inventory / UI without direct refs everywhere
public class EventRefrenceManager : MonoBehaviour
{
	public static EventRefrenceManager Instance { get; private set; }

	public Action OnGameLoopRestarted;
	public Action OnWheelRefreshed;
	public Action<int> OnZoneIdChanged;       // animated zone tick
	public Action<int> OnZoneSetImmediate;    // load save — no slide anim
	public Action OnWheelSpinStarted;
	public Action<ItemDataSO, int> OnWheelSpinCompleted; // landed slot item + amount
	public Action<IntentoryItemBase> OnRewardingStarted;
	public Action<IntentoryItemBase> OnRewardCardIsVisible; // anim done — fly icons etc
	public Action<ItemDataSO, int> OnRewardingEnded;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Debug.LogError("Multiple EventRefrenceManager instances");
			return;
		}

		Instance = this; 
	}

	void OnDestroy()
	{
		OnGameLoopRestarted = null;
		OnWheelRefreshed = null;
		OnZoneIdChanged = null;
		OnZoneSetImmediate = null;
		OnWheelSpinStarted = null;
		OnWheelSpinCompleted = null;
		OnRewardingStarted = null;
		OnRewardCardIsVisible = null;
		OnRewardingEnded = null;

		if (Instance == this)
			Instance = null;
	}

	public void RaiseGameLoopRestarted() => OnGameLoopRestarted?.Invoke();

	public void RaiseWheelRefreshed() => OnWheelRefreshed?.Invoke();

	public void RaiseZoneIdChanged(int zoneId) => OnZoneIdChanged?.Invoke(zoneId);

	public void RaiseZoneSetImmediate(int zoneId) => OnZoneSetImmediate?.Invoke(zoneId);

	public void RaiseWheelSpinStarted() => OnWheelSpinStarted?.Invoke();

	public void RaiseWheelSpinCompleted(ItemDataSO item, int amount) => OnWheelSpinCompleted?.Invoke(item, amount);

	public void RaiseRewardingStarted(IntentoryItemBase inventoryItem) => OnRewardingStarted?.Invoke(inventoryItem);

	public void RaiseRewardCardIsVisible(IntentoryItemBase inventoryItem) => OnRewardCardIsVisible?.Invoke(inventoryItem);

	public void RaiseRewardingEnded(ItemDataSO item, int amount) => OnRewardingEnded?.Invoke(item, amount);
}
