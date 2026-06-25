using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
 
// bottom reward strip — rows, flying icon VFX from card to list
public class RewardListBehaviour : MonoBehaviour
{
	const int FlyingRewardsPoolSize = 20;

	[SerializeField] GameObject RewardListUIElementPrefab;
	[SerializeField] GameObject RewardListElementsParent;
	[SerializeField] List<RewardListElementInstanceBehaviour> RewardListElements;

	[Tooltip("If unset, show/hide uses this GameObject.")]
	[SerializeField] GameObject rewardListPanelRoot;

	[SerializeField] float listAnimationStaggerSeconds = 0.05f;

	[SerializeField] Transform cardIconTransform;       // fly start — item card icon
	[SerializeField] GameObject flyingRewardIconPrefab;
	[SerializeField] Transform flyingRewardsPoolParent;
	[SerializeField] float flyingRewardDurationSeconds = 0.9f;
	[SerializeField] float flyingRewardStaggerSeconds = 0.06f;
	[SerializeField] float flyingRewardDurationVariance = 0.15f;
	[SerializeField] float flyingRewardArcHeightMin = 50f;
	[SerializeField] float flyingRewardArcHeightMax = 180f;
	[SerializeField] float flyingRewardSpawnPopDurationSeconds = 0.14f;
	[SerializeField] float flyingRewardSpawnPopStartScale = 0.15f;
	[SerializeField] float flyingRewardSpawnPopOvershoot = 1.25f;
	[SerializeField] float delayBeforeCardLeavesSeconds = 1f;

	readonly List<Image> flyingRewardsPool = new List<Image>(FlyingRewardsPoolSize);

	GameObject PanelRoot => rewardListPanelRoot != null ? rewardListPanelRoot : gameObject;

	void Awake()
	{
		InitializeFlyingRewardsPool();
	}

	void Start()
	{
		if (EventRefrenceManager.Instance != null)
			EventRefrenceManager.Instance.OnRewardCardIsVisible += OnRewardCardIsVisible;
	}

	void OnDestroy()
	{
		if (EventRefrenceManager.Instance != null)
			EventRefrenceManager.Instance.OnRewardCardIsVisible -= OnRewardCardIsVisible;
	}

	void InitializeFlyingRewardsPool()
	{ 
		Transform parent = flyingRewardsPoolParent != null ? flyingRewardsPoolParent : transform;

		for (int i = flyingRewardsPool.Count; i < FlyingRewardsPoolSize; i++)
		{
			GameObject instance = Instantiate(flyingRewardIconPrefab, parent);
			instance.SetActive(false);

			Image image = instance.GetComponent<Image>();
			if (image == null)
			{
				Debug.LogError("Flying reward icon prefab must include an Image component.", instance);
				Destroy(instance);
				continue;
			}

			flyingRewardsPool.Add(image);
		}
	}

	// card spawn anim finished — start fly VFX
	void OnRewardCardIsVisible(IntentoryItemBase inventoryItem)
	{
		inventoryItem?.TriggerItemTakeOperations();
	}

	public void HideThePanel()
	{
		PanelRoot.SetActive(false);
	}

	public void ShowThePanel()
	{
		PanelRoot.SetActive(true);
	}

	// new row or merge stack if same ItemTypes
	public RewardListElementInstanceBehaviour AddListElementInstance(ItemDataSO item, int amount, bool mergeIfSameItem = true)
	{
		if (item == null || RewardListUIElementPrefab == null)
		{
			Debug.Log("Corrupted Method call");
			return null;
		}

		if (mergeIfSameItem)
		{
			for (int i = 0; i < RewardListElements.Count; i++)
			{
				RewardListElementInstanceBehaviour existing = RewardListElements[i];
				if (existing == null)
					continue;
				if (existing.Item != null && existing.Item.TypeId == item.TypeId)
				{
					existing.AddAmount(amount);
					return existing;
				}
			}
		}

		GameObject go = Instantiate(RewardListUIElementPrefab, RewardListElementsParent.transform);
		RewardListElementInstanceBehaviour behaviour = go.GetComponent<RewardListElementInstanceBehaviour>();
 
		behaviour.Initilize(item, amount);
		RewardListElements.Add(behaviour);
		UpdatePanelVisibilityForCurrentList();
		return behaviour;
	}

	public void RemoveListElementsByType(ItemTypes type)
	{
		for (int i = RewardListElements.Count - 1; i >= 0; i--)
		{
			RewardListElementInstanceBehaviour element = RewardListElements[i];
			if (element.Item.TypeId == type)
				RemoveListElementInstance(element);
		}
	}

	public void RemoveListElementInstance(RewardListElementInstanceBehaviour element)
	{
		if (element == null)
			return;

		if (RewardListElements.Remove(element))
			Destroy(element.gameObject);

		UpdatePanelVisibilityForCurrentList();
	}

	public void SetRewardAmount(RewardListElementInstanceBehaviour element, int amount)
	{
		element.SetAmount(amount);
	}

	public void AddToRewardAmount(RewardListElementInstanceBehaviour element, int delta)
	{
		element.AddAmount(delta);
	}
    
	public void ClearAllListElements()
	{
		for (int i = 0; i < RewardListElements.Count; i++)
		{
			if (RewardListElements[i] != null)
				Destroy(RewardListElements[i].gameObject);
		}

		RewardListElements.Clear();
		UpdatePanelVisibilityForCurrentList();
	}
	 
	void UpdatePanelVisibilityForCurrentList()
	{
		int count = 0;
		for (int i = 0; i < RewardListElements.Count; i++)
		{
			if (RewardListElements[i] != null)
				count++;
		}

		if (count == 0)
			HideThePanel();
		else
			ShowThePanel();
	}

	public void PlayFlyingRewards(IntentoryItemBase inventoryItem)
	{ 
		StartCoroutine(FlyRewardsRoutine(inventoryItem));
	}

	RewardListElementInstanceBehaviour GetRewardListElement(ItemDataSO itemData)
	{
		if (itemData == null)
			return null;

		for (int i = 0; i < RewardListElements.Count; i++)
		{
			RewardListElementInstanceBehaviour element = RewardListElements[i];
			if (element == null || element.Item == null)
				continue;
			if (element.Item.TypeId != itemData.TypeId)
				continue;

			return element;
		}

		return null;
	}

	Transform GetRewardListIconTransform(ItemDataSO itemData)
	{
		RewardListElementInstanceBehaviour element = GetRewardListElement(itemData);
		return element != null ? element.IconRectTransform : null;
	}

	IEnumerator FlyRewardsRoutine(IntentoryItemBase inventoryItem)
	{
		ItemDataSO itemData = inventoryItem.itemData; 

		RewardListElementInstanceBehaviour listElement = GetRewardListElement(itemData);
		Transform targetTransform = listElement != null ? listElement.IconRectTransform : null;
		if (targetTransform == null)
			yield break;

		int iconCount = Mathf.Min(inventoryItem.GetAmount(), flyingRewardsPool.Count); 

		Vector3 startPosition = cardIconTransform.position;
		Vector3 endPosition = targetTransform.position;
		Sprite rewardSprite = itemData.Icon;
		int iconsInFlight = 0;
		bool amountAnimationStarted = false;

		void OnFlyingIconComplete()
		{
			iconsInFlight--;

			listElement?.PlayScaleUpAndResetAnimation();

			if (amountAnimationStarted)
				return;

			amountAnimationStarted = true;
			listElement?.PlayShowAnimation();
		}

		if (iconCount == 0)
			listElement?.PlayShowAnimation();

		for (int i = 0; i < iconCount; i++)
		{
			Image flyingIcon = flyingRewardsPool[i];
			if (flyingIcon == null)
				continue;

			flyingIcon.sprite = rewardSprite;
			flyingIcon.rectTransform.position = startPosition;
			flyingIcon.gameObject.SetActive(true);

			float durationMultiplier = 1f + Random.Range(-flyingRewardDurationVariance, flyingRewardDurationVariance);
			float arcMagnitude = Random.Range(flyingRewardArcHeightMin, flyingRewardArcHeightMax);
			float arcYOffset = Random.value < 0.5f ? -arcMagnitude : arcMagnitude;
			float iconDuration = flyingRewardDurationSeconds * durationMultiplier;

			iconsInFlight++;
			StartCoroutine(FlySingleRewardIconRoutine(
				flyingIcon,
				startPosition,
				endPosition,
				iconDuration,
				arcYOffset,
				OnFlyingIconComplete));

			if (flyingRewardStaggerSeconds > 0f && i < iconCount - 1)
				yield return new WaitForSecondsRealtime(flyingRewardStaggerSeconds);
		}

		while (iconsInFlight > 0)
			yield return null;

		if (delayBeforeCardLeavesSeconds > 0f)
			yield return new WaitForSecondsRealtime(delayBeforeCardLeavesSeconds);

		RewardManager.Instance?.PlayItemCardLeaves();
	}

	static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
	{
		float inverse = 1f - t;
		return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
	}

	static float EvaluateSpawnPopScale(float normalizedTime, float startScale, float overshootScale)
	{
		const float overshootPortion = 0.55f;

		if (normalizedTime < overshootPortion)
		{
			float riseT = normalizedTime / overshootPortion;
			return Mathf.Lerp(startScale, overshootScale, Mathf.SmoothStep(0f, 1f, riseT));
		}

		float settleT = (normalizedTime - overshootPortion) / (1f - overshootPortion);
		return Mathf.Lerp(overshootScale, 1f, Mathf.SmoothStep(0f, 1f, settleT));
	}

	IEnumerator FlySingleRewardIconRoutine(
		Image flyingIcon,
		Vector3 startPosition,
		Vector3 endPosition,
		float duration,
		float arcYOffset,
		System.Action onComplete)
	{
		try
		{
			RectTransform iconRect = flyingIcon.rectTransform;
			Vector3 controlPosition = Vector3.Lerp(startPosition, endPosition, 0.5f);
			controlPosition.y += arcYOffset;

			iconRect.localScale = Vector3.one * flyingRewardSpawnPopStartScale;

			float flyElapsed = 0f;
			float popElapsed = 0f;
			float popDuration = flyingRewardSpawnPopDurationSeconds;

			while (flyElapsed < duration)
			{
				float delta = Time.unscaledDeltaTime;
				flyElapsed += delta;
				popElapsed += delta;

				if (popElapsed < popDuration)
				{
					float popT = popDuration > 0f ? Mathf.Clamp01(popElapsed / popDuration) : 1f;
					float popScale = EvaluateSpawnPopScale(
						popT,
						flyingRewardSpawnPopStartScale,
						flyingRewardSpawnPopOvershoot);
					iconRect.localScale = Vector3.one * popScale;
				}
				else
				{
					iconRect.localScale = Vector3.one;
				}

				float flyT = duration > 0f ? Mathf.Clamp01(flyElapsed / duration) : 1f;
				float easedFlyT = Mathf.SmoothStep(0f, 1f, flyT);
				iconRect.position = QuadraticBezier(startPosition, controlPosition, endPosition, easedFlyT);
				yield return null;
			}

			iconRect.position = endPosition;
			iconRect.localScale = Vector3.one;
			flyingIcon.gameObject.SetActive(false);
		}
		finally
		{
			onComplete?.Invoke();
		}
	}
}
