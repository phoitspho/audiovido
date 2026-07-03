using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Sits on the Main Camera. Detects touch (mobile) and mouse click (editor),
/// raycasts into the scene, and notifies any TappableObject it hits.
/// </summary>
public class RoomInputManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float maxRayDistance = 30f;
    [SerializeField] LayerMask tappableLayer = ~0; // all layers by default

    Camera _cam;
    TappableObject _currentHighlighted;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
    }

    void Update()
    {
        // --- Editor / Desktop: mouse ---
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            ProcessHit(Input.mousePosition);
#endif

        // --- Mobile: single touch ---
#if UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began && !IsPointerOverUI())
                ProcessHit(t.position);
        }
#endif

        // Fallback for editor testing on all platforms
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            ProcessHit(Input.mousePosition);

        // Press T to fire a test ray at the center of the screen
        if (Input.GetKeyDown(KeyCode.T))
            ProcessHit(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
#endif
    }

    void ProcessHit(Vector2 screenPos)
    {
        Ray ray = _cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, tappableLayer))
        {
            TappableObject tappable = hit.collider.GetComponentInParent<TappableObject>();
            if (tappable != null && tappable.IsInteractable)
                tappable.OnTapped(hit);
        }
    }

    static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
