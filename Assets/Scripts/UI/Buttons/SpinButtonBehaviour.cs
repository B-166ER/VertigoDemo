using UnityEngine;
using UnityEngine.EventSystems;

// tap to spin — fixed speed, no drag needed
public class SpınButtonBehaviour : MonoBehaviour, IPointerClickHandler
{
	[SerializeField]
	WheelBaseRotator wheelRotator;

	[SerializeField, Min(1f)]
	float spinSpeedDegPerSec = 720f;

	[SerializeField]
	bool clockwise = true;

	public void OnPointerClick(PointerEventData eventData)
	{
		if (wheelRotator == null)
			return;

		float speed = clockwise ? spinSpeedDegPerSec : -spinSpeedDegPerSec;
		wheelRotator.TriggerAutoRotate(speed);
	}
}
