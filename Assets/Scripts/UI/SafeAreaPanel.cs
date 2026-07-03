using UnityEngine;

/// <summary>
/// AUDIOVIDO — Safe Area Panel
/// Insets a RectTransform to fit inside Screen.safeArea so UI
/// clears the iPhone notch, Dynamic Island, and Android cutouts.
///
/// Usage:
///   Attach to ANY RectTransform that should respect the safe area.
///   Typically applied to the root content panel of each Canvas
///   (or separately to TopBar and BottomBar for fine control).
///
/// The inset is applied once on Awake and again when the screen
/// orientation or resolution changes (detected via LateUpdate).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaPanel : MonoBehaviour
{
    [Tooltip("Apply top inset (clears notch / Dynamic Island)")]
    [SerializeField] bool applyTop    = true;
    [Tooltip("Apply bottom inset (clears iOS home bar)")]
    [SerializeField] bool applyBottom = true;
    [Tooltip("Apply left/right insets (clears side cutouts)")]
    [SerializeField] bool applySides  = true;

    RectTransform _rt;
    Rect _lastSafeArea;
    Vector2 _lastResolution;
    ScreenOrientation _lastOrientation;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        Apply();
    }

    void LateUpdate()
    {
        // Only recalculate when something has actually changed
        if (Screen.safeArea == _lastSafeArea &&
            new Vector2(Screen.width, Screen.height) == _lastResolution &&
            Screen.orientation == _lastOrientation)
            return;

        Apply();
    }

    void Apply()
    {
        _lastSafeArea    = Screen.safeArea;
        _lastResolution  = new Vector2(Screen.width, Screen.height);
        _lastOrientation = Screen.orientation;

        Rect safe = Screen.safeArea;
        float sw = Screen.width;
        float sh = Screen.height;

        // Convert safe area to anchor-space [0..1]
        Vector2 anchorMin = new Vector2(safe.x / sw, safe.y / sh);
        Vector2 anchorMax = new Vector2((safe.x + safe.width) / sw,
                                         (safe.y + safe.height) / sh);

        if (!applySides)
        {
            anchorMin.x = 0f;
            anchorMax.x = 1f;
        }
        if (!applyTop)    anchorMax.y = 1f;
        if (!applyBottom) anchorMin.y = 0f;

        _rt.anchorMin = anchorMin;
        _rt.anchorMax = anchorMax;
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }

#if UNITY_EDITOR
    /// <summary>Preview safe area in the editor at design time.</summary>
    [ContextMenu("Preview Safe Area")]
    void PreviewSafeArea()
    {
        _rt = GetComponent<RectTransform>();
        Apply();
        Debug.Log($"[SafeArea] safeArea={Screen.safeArea}  screen={Screen.width}x{Screen.height}");
    }
#endif
}
