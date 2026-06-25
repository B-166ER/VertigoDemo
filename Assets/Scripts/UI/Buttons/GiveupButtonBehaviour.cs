using UnityEngine;
using UnityEngine.EventSystems;

// reset run — zone 1, wipe inventory and saves
public class GiveupButtonBehaviour : MonoBehaviour, IPointerClickHandler
{
	public void OnPointerClick(PointerEventData eventData)
	{
		GameManager.Instance?.ResetToInitialState();
	}
}
