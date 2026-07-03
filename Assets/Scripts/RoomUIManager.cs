using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages UI panels that slide in/out when room objects are tapped.
/// Place this on a persistent Canvas GameObject in the scene.
///
/// Usage:
///   RoomUIManager.Instance.ShowPanel("mirror");
///   RoomUIManager.Instance.HideAll();
/// </summary>
public class RoomUIManager : MonoBehaviour
{
    public static RoomUIManager Instance { get; private set; }

    [System.Serializable]
    public class RoomPanel
    {
        public string panelId;          // matches TappableObject.objectId
        public GameObject panelRoot;    // the UI panel GameObject
        public float animDuration = 0.25f;
    }

    [SerializeField] List<RoomPanel> panels = new();
    [SerializeField] GameObject dimOverlay;   // optional dark overlay behind panels

    Dictionary<string, RoomPanel> _panelMap = new();
    RoomPanel _activePanel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var p in panels)
        {
            _panelMap[p.panelId] = p;
            p.panelRoot.SetActive(false);
        }

        if (dimOverlay) dimOverlay.SetActive(false);
        Debug.Log($"[RoomUIManager] Awake — panels registered: {_panelMap.Count}");
    }

    // Test helper — right-click the component header in Play mode → "Test Show North Panel"
    [ContextMenu("Test Show North Panel")]
    void TestShowNorth() => ShowPanel("north");

    // Show a panel by its id (matches objectId on TappableObject)
    public void ShowPanel(string panelId)
    {
        if (_activePanel != null) HidePanel(_activePanel, immediate: true);

        if (!_panelMap.TryGetValue(panelId, out RoomPanel panel))
        {
            Debug.LogWarning($"[RoomUIManager] Panel '{panelId}' not found.");
            return;
        }

        _activePanel = panel;
        panel.panelRoot.SetActive(true);
        if (dimOverlay) dimOverlay.SetActive(true);
        StartCoroutine(AnimateIn(panel));
    }

    // Hide whatever is currently open
    public void HideAll()
    {
        if (_activePanel != null)
            StartCoroutine(AnimateOut(_activePanel));
    }

    void HidePanel(RoomPanel panel, bool immediate = false)
    {
        if (immediate)
        {
            panel.panelRoot.SetActive(false);
            if (dimOverlay) dimOverlay.SetActive(false);
        }
        else
        {
            StartCoroutine(AnimateOut(panel));
        }
    }

    IEnumerator AnimateIn(RoomPanel panel)
    {
        CanvasGroup cg = panel.panelRoot.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.panelRoot.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        float t = 0f;
        while (t < panel.animDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / panel.animDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    IEnumerator AnimateOut(RoomPanel panel)
    {
        CanvasGroup cg = panel.panelRoot.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.panelRoot.AddComponent<CanvasGroup>();

        float t = panel.animDuration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / panel.animDuration);
            yield return null;
        }

        panel.panelRoot.SetActive(false);
        if (dimOverlay) dimOverlay.SetActive(false);
        _activePanel = null;
    }
}
