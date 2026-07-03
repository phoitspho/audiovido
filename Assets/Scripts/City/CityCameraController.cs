using System.Collections;
using UnityEngine;

/// <summary>
/// AUDIOVIDO — City Camera (3D city hub navigation)
/// Orbit camera around the city with drag-rotate, pinch/scroll zoom,
/// and smooth fly-to transitions when focusing a district.
/// Spec §7.2 gestures: drag = look around, pinch = zoom, double tap = reset.
/// </summary>
public class CityCameraController : MonoBehaviour
{
    [Header("Overview framing")]
    [SerializeField] Vector3 overviewPivot = new Vector3(0f, 0f, 0f);
    [SerializeField] float overviewDistance = 55f;
    [SerializeField] float overviewPitch = 55f;
    [SerializeField] float overviewYaw = 0f;

    [Header("Orbit limits")]
    [SerializeField] float minDistance = 10f;
    [SerializeField] float maxDistance = 62f;
    [SerializeField] float minPitch = 15f;
    [SerializeField] float maxPitch = 70f;

    [Header("Feel")]
    [SerializeField] float orbitSpeed = 0.35f;  // degrees per pixel
    [SerializeField] float zoomSpeed = 3f;
    [SerializeField] float pinchZoomSpeed = 0.05f;

    Vector3 _pivot;
    float _distance, _yaw, _pitch;
    bool _flying, _dragging;
    Vector2 _lastPointer;

    /// <summary>When true, user orbit input is ignored (e.g. while a district is focused).</summary>
    public bool InputLocked { get; set; }

    void Start() => ResetImmediate();

    void LateUpdate()
    {
        if (_flying || InputLocked) return;
        HandleOrbitInput();
        Apply();
    }

    // ── Input ────────────────────────────────────────────────────────────────

    void HandleOrbitInput()
    {
        // Touch (mobile)
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved)
            {
                _yaw += t.deltaPosition.x * orbitSpeed * 0.5f;
                _pitch = Mathf.Clamp(_pitch - t.deltaPosition.y * orbitSpeed * 0.5f,
                    minPitch, maxPitch);
            }
            return;
        }
        if (Input.touchCount == 2)
        {
            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);
            float prev = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
            float curr = (a.position - b.position).magnitude;
            _distance = Mathf.Clamp(_distance - (curr - prev) * pinchZoomSpeed,
                minDistance, maxDistance);
            return;
        }

        // Mouse (editor / standalone)
        if (Input.GetMouseButtonDown(0)) { _dragging = true; _lastPointer = Input.mousePosition; }
        else if (Input.GetMouseButtonUp(0)) _dragging = false;

        if (_dragging && Input.GetMouseButton(0))
        {
            Vector2 pos = Input.mousePosition;
            Vector2 delta = pos - _lastPointer;
            _lastPointer = pos;
            _yaw += delta.x * orbitSpeed;
            _pitch = Mathf.Clamp(_pitch - delta.y * orbitSpeed, minPitch, maxPitch);
        }

        _distance = Mathf.Clamp(_distance - Input.mouseScrollDelta.y * zoomSpeed,
            minDistance, maxDistance);
    }

    // ── Camera math ──────────────────────────────────────────────────────────

    void Apply()
    {
        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = _pivot + rot * new Vector3(0f, 0f, -_distance);
        transform.rotation = rot;
    }

    public void ResetImmediate()
    {
        _pivot = overviewPivot;
        _distance = overviewDistance;
        _yaw = overviewYaw;
        _pitch = overviewPitch;
        Apply();
    }

    // ── Fly transitions ──────────────────────────────────────────────────────

    /// <summary>Smoothly fly the camera to orbit a new pivot.</summary>
    public void FlyTo(Vector3 pivot, float distance, float yaw, float pitch,
        float seconds, System.Action onDone = null)
    {
        StopAllCoroutines();
        StartCoroutine(FlyRoutine(pivot, distance, yaw, pitch, seconds, onDone));
    }

    /// <summary>Fly back to the city overview framing.</summary>
    public void FlyToOverview(float seconds, System.Action onDone = null) =>
        FlyTo(overviewPivot, overviewDistance, _yaw, overviewPitch, seconds, onDone);

    IEnumerator FlyRoutine(Vector3 pivot, float distance, float yaw, float pitch,
        float seconds, System.Action onDone)
    {
        _flying = true;
        Vector3 p0 = _pivot; float d0 = _distance, y0 = _yaw, x0 = _pitch;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / seconds));
            _pivot = Vector3.Lerp(p0, pivot, k);
            _distance = Mathf.Lerp(d0, distance, k);
            _yaw = Mathf.LerpAngle(y0, yaw, k);
            _pitch = Mathf.Lerp(x0, pitch, k);
            Apply();
            yield return null;
        }
        _pivot = pivot; _distance = distance; _yaw = yaw; _pitch = pitch;
        Apply();
        _flying = false;
        onDone?.Invoke();
    }
}
