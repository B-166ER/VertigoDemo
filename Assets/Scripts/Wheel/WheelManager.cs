using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// wheel visuals + slot population — icons, multipliers, bronze/silver/gold blend
public class WheelManager : MonoBehaviour
{
	public static WheelManager Instance { get; private set; }

	const float WheelBlendLerpDurationMs = 200f;

	public List<WheelSlot> WheelSlots = new List<WheelSlot>();
	[HideInInspector]
	public List<(ItemDataSO Item, int Amount)> CurrentRewards;
	public int LastSpinSlotIndex { get; private set; }

	public float PreferedSlotIconWidth = 442f; // horizontal icon size on wheel

	[SerializeField]
	Image wheelImage;

	[SerializeField]
	Image wheelPinImage;

	[SerializeField]
	List<WheelColorSpriteEntry> wheelSpritesByColor = new List<WheelColorSpriteEntry>();

	static readonly int TargetTexId = Shader.PropertyToID("_TargetTex");
	static readonly int TargetTexStId = Shader.PropertyToID("_TargetTex_ST");
	static readonly int BlendId = Shader.PropertyToID("_Blend");
	static readonly Vector4 TargetTexDefaultSt = new Vector4(1f, 1f, 0f, 0f);

	Coroutine _blendRoutine;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Debug.LogError("Multiple manager instances");
			return;
		}

		Instance = this;
	}

	public void SetLastSpinSlotIndex(int slotIndex) => LastSpinSlotIndex = slotIndex;

	// revive flow — swap one slot without full reroll
	public void SwapRewardAtSlot(int slotIndex, ItemDataSO item, int amount)
	{
		var updated = new List<(ItemDataSO Item, int Amount)>(CurrentRewards);
		updated[slotIndex] = (item, amount);
		PopulateTheWheel(updated);
	}

	// push reward list into each WheelSlot UI
	public void PopulateTheWheel(List<(ItemDataSO Item, int Amount)> rewards)
	{
		CurrentRewards = rewards;

		for (int i = 0; i < WheelSlots.Count; i++)
		{
			if (i < rewards.Count)
			{
				ItemDataSO item = rewards[i].Item;
				WheelSlots[i].image.sprite = item.Icon;
				WheelSlots[i].MyRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, PreferedSlotIconWidth);
				if (item.SingleUse || item.TypeId == ItemTypes.death)
				{
					WheelSlots[i].text.text = string.Empty;
					WheelSlots[i].Multiplier = 1;
				}
				else
				{
					WheelSlots[i].text.text = $"X{rewards[i].Amount} ";
					WheelSlots[i].Multiplier = rewards[i].Amount;
				}
			}
			else
			{
				Debug.LogError($"Wheel slot are being filled with null entries (reward list is smaller than the number of slots)");
				WheelSlots[i].image.sprite = null;
				WheelSlots[i].text.text = "Empty";
				WheelSlots[i].Multiplier = 0;
			}
		}
	}

	// swap wheel + pin sprites, lerp shader blend bronze ↔ premium
	public void SetWheelColor(WheelColor color)
	{
		WheelColorSpriteEntry entry = GetEntryForWheelColor(color);
		ApplyTargetTexture(wheelImage.material, entry.Sprite);

		float targetBlend = color == WheelColor.Bronze ? 0f : 1f;
		if (_blendRoutine != null)
			StopCoroutine(_blendRoutine);
		_blendRoutine = StartCoroutine(LerpWheelBlendRoutine(wheelImage.material, targetBlend));

		wheelPinImage.sprite = entry.PinSprite;
	}

	void ApplyTargetTexture(Material mat, Sprite sprite)
	{
		mat.SetTexture(TargetTexId, sprite.texture);
		mat.SetVector(TargetTexStId, TargetTexDefaultSt);
	}

	IEnumerator LerpWheelBlendRoutine(Material mat, float targetBlend)
	{
		float startBlend = mat.GetFloat(BlendId);
		float elapsed = 0f;

		while (elapsed < WheelBlendLerpDurationMs)
		{
			elapsed += Time.unscaledDeltaTime * 1000f;
			float t = Mathf.Clamp01(elapsed / WheelBlendLerpDurationMs);
			mat.SetFloat(BlendId, Mathf.Lerp(startBlend, targetBlend, t));
			yield return null;
		}

		mat.SetFloat(BlendId, targetBlend);
		_blendRoutine = null;
	}

	WheelColorSpriteEntry GetEntryForWheelColor(WheelColor color)
	{
		for (int i = 0; i < wheelSpritesByColor.Count; i++)
		{
			if (wheelSpritesByColor[i].Color == color)
				return wheelSpritesByColor[i];
		}

		Debug.LogError($"WheelManager: No sprite entry for {color}");
		return wheelSpritesByColor[0];
	}

	[ContextMenu("Set Color Golden")]
	public void SetColorGold()
	{
		SetWheelColor(WheelColor.Golden);
	}

	[ContextMenu("Set Color Silver")]
	public void SetColorSilver()
	{
		SetWheelColor(WheelColor.Silver);
	}

	public enum WheelColor
	{
		Bronze,
		Silver,
		Golden
	}

	[Serializable]
	public struct WheelColorSpriteEntry
	{
		public WheelColor Color;
		public Sprite Sprite;
		public Sprite PinSprite;
	}
}
