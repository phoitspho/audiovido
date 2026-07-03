using UnityEngine;

/// <summary>
/// AUDIOVIDO — Lounge Input Manager (SCR-19)
/// Raycast tap detection for the 3D lounge space.
/// Spec §7.2: Tap on character → initiate conversation
///            Tap on object → interact (sit, approach bar)
/// Attach to Main Camera.
/// </summary>
[RequireComponent(typeof(Camera))]
public class LoungeInputManager : MonoBehaviour
{
    [SerializeField] float maxRayDistance = 20f;
    [SerializeField] LayerMask interactableMask = ~0;

    Camera _cam;

    void Awake() => _cam = GetComponent<Camera>();

    void Update()
    {
        if (!TryGetTapPosition(out Vector2 screenPos)) return;

        Ray ray = _cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableMask)) return;

        // Check for LoungeInteractable first
        var interactable = hit.collider.GetComponentInParent<LoungeInteractable>();
        if (interactable != null) { interactable.OnTapped(); return; }

        // Check for DRIFT trigger
        var drift = hit.collider.GetComponentInParent<DriftController>();
        if (drift != null) { LoungeManager.Instance?.OnPlayerApproachBar(); return; }
    }

    bool TryGetTapPosition(out Vector2 pos)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0)) { pos = Input.mousePosition; return true; }
#endif
#if !UNITY_EDITOR
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        { pos = Input.GetTouch(0).position; return true; }
#endif
        pos = default;
        return false;
    }
}
