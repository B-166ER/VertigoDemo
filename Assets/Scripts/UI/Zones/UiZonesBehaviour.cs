using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static GameManager;

// horizontal zone progress bar — slides left each round, colors silver/gold tiers
public class UiZonesBehaviour : MonoBehaviour
{
	public List<ZoneUnitInstance> ZoneUnits;

	[SerializeField] RectTransform zonesContent;
	[SerializeField] RectTransform zonesViewport;
	[SerializeField] float zoneStepWidth = 50f;
	[SerializeField] float slideDurationMs = 300f;
	[SerializeField] Color defaultZoneTextColor = new Color(0.9245283f, 0.9245283f, 0.9245283f, 1f);
	[SerializeField] Color silverZoneTextColor = new Color(0.72f, 0.78f, 0.86f, 1f);
	[SerializeField] Color goldZoneTextColor = new Color(1f, 0.84f, 0.35f, 1f);

	int currentZoneId;
	Coroutine slideRoutine;

	void Start()
	{
		if (GameManager.Instance != null)
		{
			currentZoneId = GameManager.Instance.CurrentZoneId;
			ResetBar(currentZoneId, false);
		}

		if (EventRefrenceManager.Instance != null)
		{
			EventRefrenceManager.Instance.OnZoneIdChanged += HandleZoneIdChanged;
			EventRefrenceManager.Instance.OnZoneSetImmediate += HandleZoneSetImmediate;
		}
	}

	void OnDestroy()
	{
		if (EventRefrenceManager.Instance != null)
		{
			EventRefrenceManager.Instance.OnZoneIdChanged -= HandleZoneIdChanged;
			EventRefrenceManager.Instance.OnZoneSetImmediate -= HandleZoneSetImmediate;
		}
	}

	void HandleZoneIdChanged(int zoneId)
	{
		currentZoneId = zoneId;
		IncreaseCurrentZone();
	}

	// load game — snap without slide
	void HandleZoneSetImmediate(int zoneId)
	{
		if (slideRoutine != null)
		{
			StopCoroutine(slideRoutine);
			slideRoutine = null;
		}

		currentZoneId = zoneId;
		ResetBar(currentZoneId, false);
	}

	[ContextMenu("Increase Current Zone")]
	public void IncreaseCurrentZone()
	{
		if (slideRoutine != null)
			StopCoroutine(slideRoutine);
		slideRoutine = StartCoroutine(SlideContentLeftRoutine());
	}

	IEnumerator SlideContentLeftRoutine()
	{
		Vector2 start = zonesContent.anchoredPosition;
		Vector2 target = start - new Vector2(zoneStepWidth, 0);
		float timer = 0f;

		while (timer < slideDurationMs)
		{
			timer += Time.unscaledDeltaTime * 1000f;
			float t = Mathf.Clamp01(timer / slideDurationMs);
			zonesContent.anchoredPosition = Vector2.Lerp(start, target, t);
			yield return null;
		}

		zonesContent.anchoredPosition = target;
		slideRoutine = null;
		ResetBar(currentZoneId);
	}

	// recycle first slot to end, repaint zone numbers around center
	void ResetBar(int zoneId, bool rotateSlots = true)
	{
		if (rotateSlots)
		{
			ZoneUnitInstance first = ZoneUnits[0];
			ZoneUnits.RemoveAt(0);
			ZoneUnits.Add(first);
			first.transform.SetAsLastSibling();
		}

		int middleIndex = ZoneUnits.Count / 2;
		GameManager gameManager = GameManager.Instance;
		for (int i = 0; i < ZoneUnits.Count; i++)
		{
			int value = zoneId + (i - middleIndex);
			ZoneUnits[i].ZoneText.text = value <= 0 ? "" : value.ToString();
			ZoneUnits[i].ZoneText.color = GetZoneTextColor(value, gameManager);
		}

		float y = zonesContent.anchoredPosition.y;
		zonesContent.anchoredPosition = new Vector2(GetCenteredContentX(middleIndex), y);
	}

	Color GetZoneTextColor(int zoneValue, GameManager gameManager)
	{
		if (zoneValue <= 0)
			return defaultZoneTextColor;

		return gameManager.GetZoneRoundTier(zoneValue) switch
		{
			ZoneRoundTier.Gold => goldZoneTextColor,
			ZoneRoundTier.Silver => silverZoneTextColor,
			_ => defaultZoneTextColor
		};
	}

	float GetCenteredContentX(int middleIndex)
	{
		RectTransform viewport = zonesViewport != null ? zonesViewport : zonesContent.parent as RectTransform;
		LayoutRebuilder.ForceRebuildLayoutImmediate(zonesContent);
		RectTransform middleUnit = ZoneUnits[middleIndex].GetComponent<RectTransform>();
		float middleCenterX = middleUnit.anchoredPosition.x + middleUnit.rect.width * (0.5f - middleUnit.pivot.x);
		return viewport.rect.width * 0.5f - middleCenterX;
	}
}
