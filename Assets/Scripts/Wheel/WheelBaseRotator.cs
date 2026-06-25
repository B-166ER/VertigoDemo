using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// pointer drag on wheel — tracks angular velocity, triggers auto-spin past threshold
public class WheelBaseRotator : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    const int AngleHistoryFrameCount = 20;

    public WheelBaseBehaviour WheelBaseBehaviour;

	[SerializeField, Min(1f)]
    [Tooltip("Snap Val")]
    float followResponsiveness = 18f; // how tight wheel follows finger

    [SerializeField, Min(0f)]
    [Tooltip("Threshold that triggers Wheel Rotation")]
    float angularSpeedThresholdDegPerSec = 360f;

	[Tooltip("Record angular change in last 20 frames. use it as drag speed")]
	public float AngleChangeLast20Frames;

	[Tooltip("Maximum angular speed before wheel starts rotating on its own")]
	public float MaxAngularSpeed;

	RectTransform _rect;
    bool _dragging;
    public bool IsAutoRotating { get; private set; }
    float _pointerAngleStart;
    float _imageZStart;

    readonly float[] _angleDeltaHistory = new float[AngleHistoryFrameCount];
    readonly float[] _dtHistory = new float[AngleHistoryFrameCount];
    int _angleHistoryIndex;
    float _rotationZLastFrame;

    float _sumDt20;
    bool _recordingMaxAngularSpeed;

    float _lastRotationSign = 1f; 

    void Awake()
    {
        _rect = GetComponent<RectTransform>(); 

        float nominalDt = 1f / 60f;
        for (int i = 0; i < AngleHistoryFrameCount; i++)
        {
            _angleDeltaHistory[i] = 0f;
            _dtHistory[i] = nominalDt;
            _sumDt20 += nominalDt;
        }
    }

	public void EndAutoRotate() => IsAutoRotating = false;

	// spin button path — skip drag detection, go straight to auto rotate
    public void TriggerAutoRotate(float speedDegPerSec)
    {
        if (WheelBaseBehaviour == null)
            return;

        AutoRot(speedDegPerSec);
    }

	// rolling window of angular speed while dragging
    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        float zNow = _rect.localEulerAngles.z;
        float zPrev = _rotationZLastFrame;
        float signedFrameDelta = Mathf.DeltaAngle(zPrev, zNow);
        float normalizedAngleChange = Quaternion.Angle(
            Quaternion.Euler(0f, 0f, zPrev),
            Quaternion.Euler(0f, 0f, zNow));
        _rotationZLastFrame = zNow;

        if (_dragging && Mathf.Abs(signedFrameDelta) > 1e-3f)
            _lastRotationSign = Mathf.Sign(signedFrameDelta);

        float oldEntry = _angleDeltaHistory[_angleHistoryIndex];
        float oldDt = _dtHistory[_angleHistoryIndex];

        AngleChangeLast20Frames -= oldEntry;
        _sumDt20 -= oldDt;

        _angleDeltaHistory[_angleHistoryIndex] = normalizedAngleChange;
        _dtHistory[_angleHistoryIndex] = dt;
        AngleChangeLast20Frames += normalizedAngleChange;
        _sumDt20 += dt;
        _angleHistoryIndex = (_angleHistoryIndex + 1) % AngleHistoryFrameCount;

        if (IsAutoRotating || !_dragging)
            return;

        float avgSpeedLast20 = AngleChangeLast20Frames / Mathf.Max(1e-5f, _sumDt20);
        if (!_recordingMaxAngularSpeed && avgSpeedLast20 >= angularSpeedThresholdDegPerSec)
            _recordingMaxAngularSpeed = true;

        if (_recordingMaxAngularSpeed)
        {
            float instSpeed = normalizedAngleChange / Mathf.Max(1e-5f, dt);
            if (instSpeed > MaxAngularSpeed)
            {
                MaxAngularSpeed = instSpeed;
                float directionSign = Mathf.Abs(signedFrameDelta) > 1e-3f
                    ? Mathf.Sign(signedFrameDelta)
                    : _lastRotationSign;
                if (Mathf.Abs(directionSign) < 1e-3f)
                    directionSign = 1f;
                AutoRot(instSpeed * directionSign);
            }
        }
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (IsAutoRotating)
            return;

        MaxAngularSpeed = 0f;
        _recordingMaxAngularSpeed = false;

        _dragging = PointerAngleDeg(e, out _pointerAngleStart);
        if (_dragging)
            _imageZStart = _rect.localEulerAngles.z;
    }

    public void OnDrag(PointerEventData e)
    {
        if (IsAutoRotating || !_dragging || !PointerAngleDeg(e, out float pointerAngleNow))
            return;

        float targetZ = _imageZStart + Mathf.DeltaAngle(_pointerAngleStart, pointerAngleNow);
        float t = 1f - Mathf.Exp(-followResponsiveness * Time.unscaledDeltaTime);
        float z = Mathf.LerpAngle(_rect.localEulerAngles.z, targetZ, t);

        Vector3 euler = _rect.localEulerAngles;
        euler.z = z;
        _rect.localEulerAngles = euler;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (IsAutoRotating)
            return;

        _dragging = false;
        MaxAngularSpeed = 0f;
        _recordingMaxAngularSpeed = false;
    }

    private void AutoRot(float speed)
    {
        IsAutoRotating = true;
        _dragging = false;
        _recordingMaxAngularSpeed = false;
        MaxAngularSpeed = 0f;

        WheelBaseBehaviour.RotateAuto(speed);
	}

	// angle from wheel center to touch — ignore dead zone in middle
    bool PointerAngleDeg(PointerEventData e, out float degrees)
    {
        degrees = 0f;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rect, e.position, e.pressEventCamera, out Vector2 local))
            return false;

        Vector2 fromCenter = local - _rect.rect.center;
        if (fromCenter.sqrMagnitude < 25f)
            return false;

        degrees = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg;
        return true;
    }
}
