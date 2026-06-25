using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// spend gold coin from inventory to revive after bomb
public class CoinReviveButtonBehaviour : MonoBehaviour, IPointerClickHandler
{
	[SerializeField] Image buttonImage;
	[SerializeField] TextMeshProUGUI label;
	[SerializeField] Color EnabledColor;
	[SerializeField] Color DisabledColor;
	[SerializeField] string RevivePossibleText;
	[SerializeField] string NoGoldText;

	bool _canRevive;

	void Awake()
	{
		if (buttonImage == null)
			buttonImage = GetComponent<Image>();
		if (label == null)
			label = GetComponentInChildren<TextMeshProUGUI>();
	}

	// call when death card opens — grey out if no gold
	public void RefreshState()
	{
		ItemTypes goldCoinType = GameManager.Instance.goldCoinType;
		_canRevive = InventoryManager.Instance.HasItemOfType(goldCoinType);

		if (_canRevive)
		{
			label.text = RevivePossibleText;
			buttonImage.color = EnabledColor;
		}
		else
		{
			label.text = NoGoldText;
			buttonImage.color = DisabledColor;
		}

		buttonImage.raycastTarget = _canRevive;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (!_canRevive)
			return;

		GameManager.Instance.ReviveFromBomb();
	}
}
