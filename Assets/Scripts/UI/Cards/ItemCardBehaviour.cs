using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// reward popup card — title, desc, amount; leaves anim ends the reward loop
public class ItemCardBehaviour : MonoBehaviour
{
	const string ItemCardSpawnState = "Item Card Spawn";
	const string CardLeavesState = "CardLeaves";

	[SerializeField] Animator animator;
	[SerializeField] Image itemIcon;
	[SerializeField] TextMeshProUGUI titleText;
	[SerializeField] TextMeshProUGUI descriptionText;
	[SerializeField] TextMeshProUGUI amountText;

	IntentoryItemBase _inventoryItem;
	Coroutine _leaveRoutine;

	void Awake() => ResolveTextReferencesIfNeeded();

	// bind inventory item before show
	public void Setup(IntentoryItemBase inventoryItem)
	{
		_inventoryItem = inventoryItem;
		ApplyCardContentFromItemData();
	}

	void ApplyCardContentFromItemData()
	{
		if (_inventoryItem?.itemData == null)
			return;

		ItemDataSO itemData = _inventoryItem.GetCardDisplayItemData();

			itemIcon.sprite = itemData.Icon;

			titleText.text = _inventoryItem.GetCardTitle();

			descriptionText.text = _inventoryItem.GetCardDescription();

			amountText.text = $"{_inventoryItem.GetAmount()} ";
	}

	// fallback if refs not wired in inspector
	void ResolveTextReferencesIfNeeded()
	{
		if (titleText == null)
			titleText = transform.Find("UI_ItemCards_Title")?.GetComponent<TextMeshProUGUI>();

		if (descriptionText == null)
			descriptionText = transform.Find("UI_ItemCards_Desc")?.GetComponent<TextMeshProUGUI>();

		if (amountText == null)
			amountText = transform.Find("UI_ItemCards_Amount_Value")?.GetComponent<TextMeshProUGUI>();
	}

	public void PlayItemCardSpawn()
	{
		if (_leaveRoutine != null)
		{
			StopCoroutine(_leaveRoutine);
			_leaveRoutine = null;
		}

		gameObject.SetActive(true);
		ApplyCardContentFromItemData();

		if (animator == null)
		{
			Debug.LogError("No Animator", this);
			return;
		}

		animator.Play(ItemCardSpawnState, 0, 0f);
	}

	public void PlayCardLeaves()
	{
		if (animator == null)
		{
			Debug.LogError("No Animator", this);
			return;
		}

		if (_leaveRoutine != null)
			StopCoroutine(_leaveRoutine);

		animator.Play(CardLeavesState, 0, 0f);
		_leaveRoutine = StartCoroutine(WaitForLeaveAnimationEnd());
	}

	// wait for leave clip then tell game loop we're done
	IEnumerator WaitForLeaveAnimationEnd()
	{
		yield return null;

		AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
		while (animator != null
			&& stateInfo.IsName(CardLeavesState)
			&& stateInfo.normalizedTime < 1f)
		{
			yield return null;
			stateInfo = animator.GetCurrentAnimatorStateInfo(0);
		}

		_leaveRoutine = null;

		if (_inventoryItem == null)
			yield break;

		EventRefrenceManager.Instance?.RaiseRewardingEnded(_inventoryItem.itemData, _inventoryItem.GetAmount());
	}

	// animation event on spawn clip
	public void onAnimationEnded()
	{
		EventRefrenceManager.Instance?.RaiseRewardCardIsVisible(_inventoryItem);
	}
}
