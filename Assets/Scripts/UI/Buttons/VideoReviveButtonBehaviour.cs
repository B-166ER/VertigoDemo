using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// rewarded ad revive — not wired yet (placeholder)
public class VideoReviveButtonBehaviour : MonoBehaviour, IPointerClickHandler
{
	public void OnPointerClick(PointerEventData eventData)
	{
		// spend gold other wise don't react
        Debug.Log("aa");
	}
}
