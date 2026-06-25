using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// wheel spin phases — drag handoff → coast → snap to slot → fire spin complete
public class WheelBaseBehaviour : MonoBehaviour
{
	private const int SlotCount = 8;

	public int CurrentSlot;
	public float slotAngleOffset; // align pin with wedge centers
	public WheelBaseRotator _wheelBaseRotator;

	[Tooltip("Which direction is the wheel being autorotated. Dont use when player is rotating the wheel")]
	public bool RotationDirection;

	[Space(10)]
	[Tooltip("Rotate this long before slowing down")]
	public float RotateSelfDurationSeconds = 3f;
	[Tooltip("Rotate this long before slowing")]
	public float RotateBeforeSlowingDuration = 3f;
	[Tooltip("Max time to coast at slow_speed onto the next slot (safety cap)")]
	public float RotateBeforeStoppingDuration = 3f; 
	[Tooltip("Wheel is considered slow at this point, it will rotate to next slot and stop")]
	public float WheelStopThresholdDegPerSec;


	/*
	 * To fix clicking transparent pixels being detected.
	 * Unstable !!! probably because compressed atlas. More research needed
    [Range(0f, 1f)]
	public float alphaHitTestMinimumThreshold = 0.01f;
	*/

	public Image _image;

	RectTransform _rect;
	Coroutine _rotateSelfRoutine;

	private void Awake()
	{
		_rect = GetComponent<RectTransform>(); 

		//_image = GetComponent<Image>();
		//_image.alphaHitTestMinimumThreshold = alphaHitTestMinimumThreshold;
	}

	//public float DebugMethod;
	private void Update()
	{
		//DebugMethod = RemainingAngleToNextSlot(GetComponent<RectTransform>().localEulerAngles.z, false);

	}

	private void LateUpdate()
	{
		UpdateCurrentSlotFromWheelAngle();
	}

	// track which slot is under the pin — runs every frame (todo: only while spinning)
	private void UpdateCurrentSlotFromWheelAngle()
	{
		if (_wheelBaseRotator == null)
			return;

		if (!_wheelBaseRotator.TryGetComponent<RectTransform>(out RectTransform rect))
			return;

		float segment = 360f / SlotCount;
		float a = Mathf.Repeat(rect.localEulerAngles.z + slotAngleOffset, 360f);
		int slot = Mathf.FloorToInt(a / segment);
		if (slot < 0)
			slot = 0;
		else if (slot >= SlotCount)
			slot = SlotCount - 1;

		CurrentSlot = slot;
	}

	// degrees left until next slot stop — direction = spin clockwise or not
	public float RemainingAngleToNextSlot(float angle, bool direction)
	{
		float segment = 360f / SlotCount;
		float a = Mathf.Repeat(angle + slotAngleOffset, 360f);
		float posInSegment = Mathf.Repeat(a, segment);

		if (direction)
			return Mathf.Repeat(segment - posInSegment, segment);

		return -posInSegment;
	}

	// called when drag speed high enough — kicks off auto spin coroutine
	// After WheelBaseRotator decides wheel is roated fast enough. Start rotating on its own, then slow down, then stop on a slot.
	public void RotateAuto(float speed)
	{
		if (_rotateSelfRoutine != null)
			StopCoroutine(_rotateSelfRoutine);

		RotationDirection = Mathf.Sign(speed) == 1;
		_rotateSelfRoutine = StartCoroutine(RotateSelf(speed));
	}

	// spin: full speed → ease down → creep to slot alignment
	// this operation should be done under rotator script. However these parts are more related to the wheel behaviour rather then rotation
	// maybe rename WheelBaseRotator.cs to WheelBasePointerDrag.cs
	private IEnumerator RotateSelf(float speedDegPerSec)
	{
		EventRefrenceManager.Instance.RaiseWheelSpinStarted();

		float elapsed = 0f;
		float deltaZ = 0f;
		Vector3 euler = Vector3.zero;

	// auto rotate for designated duration
		while (elapsed < RotateSelfDurationSeconds)
		{  
			float remaining = RotateSelfDurationSeconds - elapsed; 
			deltaZ = speedDegPerSec * Time.unscaledDeltaTime;
			euler = _rect.localEulerAngles;
			euler.z += deltaZ;
			_rect.localEulerAngles = euler;

			elapsed += Time.unscaledDeltaTime;
			yield return null;
		}

		// auto rotation is ended. start slowing down
		elapsed = 0f;
		float slow_speed = speedDegPerSec;
		while (elapsed < RotateBeforeSlowingDuration)
		{
			euler = _rect.localEulerAngles;
			float t = elapsed / RotateBeforeSlowingDuration;
			slow_speed = Mathf.Lerp(speedDegPerSec , 0 , t); 
			deltaZ = slow_speed * Time.unscaledDeltaTime;
			euler.z += deltaZ;
			_rect.localEulerAngles = euler;

			elapsed += Time.unscaledDeltaTime;

			if (Mathf.Abs(slow_speed) <= Mathf.Abs(WheelStopThresholdDegPerSec))
				break;

			yield return null;
		}

		// Coast at slow_speed until the wheel sits on a slot,use threshold as fallback when speed is 0.
		float coastMag = Mathf.Abs(slow_speed);
		if (coastMag < 0.0001f)
			coastMag = Mathf.Abs(WheelStopThresholdDegPerSec); 
		elapsed = 0f;
		while (elapsed < RotateBeforeStoppingDuration)
		{
			euler = _rect.localEulerAngles;
			float remaining = RemainingAngleToNextSlot(euler.z, RotationDirection);
			if (Mathf.Abs(remaining) < 0.05f)
			{
				euler.z += remaining;
				_rect.localEulerAngles = euler;
				break;
			}

			float step = Mathf.Sign(remaining) * Mathf.Min(coastMag * Time.unscaledDeltaTime, Mathf.Abs(remaining));
			euler.z += step;
			_rect.localEulerAngles = euler;

			elapsed += Time.unscaledDeltaTime;
			yield return null;
		}

		_rotateSelfRoutine = null;

		// wheel rotation ended. trigger reward mechanisms
		_wheelBaseRotator?.EndAutoRotate();
		WheelManager.Instance.SetLastSpinSlotIndex(CurrentSlot);
		ItemDataSO item_d = WheelManager.Instance.CurrentRewards[CurrentSlot].Item;
		int amount = WheelManager.Instance.CurrentRewards[CurrentSlot].Amount;
		EventRefrenceManager.Instance.RaiseWheelSpinCompleted(item_d, amount); 
		
		//EventRefrenceManager.Instance.RaiseRewardingStarted(inventoryItem); 
		//instead inventory manager should listen wheelspin and raise rewarding started there
	} 

}
