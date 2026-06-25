using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// single row in reward list — icon + count, count-up anim on land
public class RewardListElementInstanceBehaviour : MonoBehaviour
{
	[SerializeField] Image iconImage;
	[SerializeField] TextMeshProUGUI amountText;

	const float ShowAnimDurationMs = 600f;
	const float IconScalePulseDurationMs = 100f;
	const float IconScaleStart = 1f;
	const float IconScalePeak = 1.4f;
	const string SingleUseListAmountLabel = "Unlocked";

	ItemDataSO item;
	int amount;
	int currentTemporaryDisplayedAmount;
	Coroutine amountAnimationRoutine;
	Coroutine iconScaleAnimationRoutine;

	public ItemDataSO Item
	{
		get => item;
		set => Debug.LogError("Item is read-only; after initialization, you cannot change it.");
	}

	public RectTransform IconRectTransform => iconImage != null ? iconImage.rectTransform : null;

	// itemData = SO ref, initialAmount = stack size or 1 for unlocks
	public void Initilize(ItemDataSO itemData, int initialAmount)
	{ 
		item = itemData;
		currentTemporaryDisplayedAmount = 0;
		amount = initialAmount; 
		iconImage.sprite = itemData.Icon;
		 
		RewardManager.Instance.lastTakenRewardListItemIconTransform = iconImage.rectTransform; 
		RefreshAmountDisplay(playAnimation: false);
	}

	public void SetAmount(int newAmount, bool playAnimation = true)
	{
		amount = newAmount;
		RefreshAmountDisplay(playAnimation);
	}

	public void AddAmount(int delta, bool playAnimation = false)
	{
		if (RewardManager.Instance != null && iconImage != null)
			RewardManager.Instance.lastTakenRewardListItemIconTransform = iconImage.rectTransform;

		SetAmount(amount + delta, playAnimation);
	}

	bool ShowsUnlockedLabel => item != null && item.SingleUse;

	void RefreshAmountDisplay(bool playAnimation)
	{
		if (ShowsUnlockedLabel)
		{
			ApplyAmountText();
			return;
		}

		if (!playAnimation)
			currentTemporaryDisplayedAmount = amount;

		ApplyAmountText();

		if (playAnimation)
			PlayShowAnimation();
	}

	void ApplyAmountText()
	{
		if (amountText == null)
			return;

		amountText.text = ShowsUnlockedLabel
			? SingleUseListAmountLabel
			: $"{currentTemporaryDisplayedAmount} ";
	}

	// count ticks up to final amount
	public void PlayShowAnimation()
	{
		if (ShowsUnlockedLabel)
		{
			ApplyAmountText();
			return;
		}

		StopAmountAnimation();
		amountAnimationRoutine = StartCoroutine(AnimateDisplayedAmountRoutine());
	}

	// little punch when each flying icon arrives
	public void PlayScaleUpAndResetAnimation()
	{
		if (iconImage == null)
			return;

		if (iconScaleAnimationRoutine != null)
			StopCoroutine(iconScaleAnimationRoutine);

		iconScaleAnimationRoutine = StartCoroutine(ScaleUpAndResetAnimationRoutine());
	}

	void StopAmountAnimation()
	{
		if (amountAnimationRoutine != null)
		{
			StopCoroutine(amountAnimationRoutine);
			amountAnimationRoutine = null;
		}
	}

	void StopShowAnimations()
	{
		StopAmountAnimation();

		if (iconScaleAnimationRoutine != null)
		{
			StopCoroutine(iconScaleAnimationRoutine);
			iconScaleAnimationRoutine = null;
		}

		if (iconImage != null)
			iconImage.rectTransform.localScale = Vector3.one * IconScaleStart;
	}

	IEnumerator AnimateDisplayedAmountRoutine()
	{
		int start = currentTemporaryDisplayedAmount;
		float anim_timer = 0f;

		while (anim_timer < ShowAnimDurationMs)
		{
			anim_timer += Time.unscaledDeltaTime * 1000f;

			float t = Mathf.Clamp01(anim_timer / ShowAnimDurationMs);
			float frameAmount = Mathf.Lerp(start, amount, t);
			currentTemporaryDisplayedAmount = Mathf.CeilToInt(frameAmount);

			ApplyAmountText();

			yield return null;
		}

		currentTemporaryDisplayedAmount = amount;
		ApplyAmountText();

		amountAnimationRoutine = null;
	}

	IEnumerator ScaleUpAndResetAnimationRoutine()
	{
		if (iconImage == null)
			yield break;

		RectTransform iconRect = iconImage.rectTransform;
		iconRect.localScale = Vector3.one * IconScaleStart;

		float anim_timer = 0f;
		while (anim_timer < IconScalePulseDurationMs)
		{
			anim_timer += Time.unscaledDeltaTime * 1000f;
			float t = Mathf.Clamp01(anim_timer / IconScalePulseDurationMs);
			float scale = Mathf.Lerp(IconScaleStart, IconScalePeak, t);
			iconRect.localScale = Vector3.one * scale;
			yield return null;
		}

		iconRect.localScale = Vector3.one * IconScaleStart;
		iconScaleAnimationRoutine = null;
	}

	void OnDisable() => StopShowAnimations();
}
