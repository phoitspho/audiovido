using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AUDIOVIDO — App Shell Controller (spec §3.1)
/// Owns the 2D backbone of the app in MainScene:
///   • Bottom navigation (Home / Search / Hub / Fan Club / Profile)
///   • Screen switching with active-tab accent colors
///   • Header NXT balance pill (live via NxtEarnManager)
///   • Hiding the shell + main camera when a 3D space loads additively
/// References are wired by AppShellBuilder (Editor menu).
/// </summary>
public class AppShellController : MonoBehaviour
{
    public static AppShellController Instance { get; private set; }

    [Header("Screens (Home, Search, Hub, FanClub, Profile)")]
    [SerializeField] GameObject[] screens;

    [Header("Bottom Nav")]
    [SerializeField] Button[] navButtons;
    [SerializeField] TMP_Text[] navLabels;
    [SerializeField] Image[] navDots;
    [SerializeField] Color[] tabAccents;   // per-tab active color

    [Header("Header")]
    [SerializeField] TMP_Text nxtBalanceText;

    [Header("Shell")]
    [SerializeField] GameObject shellRoot;       // whole 2D UI canvas
    [SerializeField] Camera mainSceneCamera;     // disabled while inside 3D space
    [SerializeField] CanvasGroup fadeCanvas;     // standalone fade overlay

    static readonly Color INACTIVE = new Color(0.353f, 0.353f, 0.47f, 1f); // --text-tertiary

    int _currentTab = -1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < navButtons.Length; i++)
        {
            int index = i; // capture
            if (navButtons[i] != null)
                navButtons[i].onClick.AddListener(() => SwitchTab(index));
        }

        SwitchTab(0);

        // NXT pill — initial value + live updates
        UpdateNxt(NxtEarnManager.Instance != null ? NxtEarnManager.Instance.Balance : 0, 0);
        NxtEarnManager.OnNxtChanged += UpdateNxt;

        // The SceneLoader singleton survives scene reloads but its fade canvas
        // dies with the old scene — re-wire it to ours every time we build.
        if (SceneLoader.Instance != null && fadeCanvas != null)
            SceneLoader.Instance.SetFadeCanvas(fadeCanvas);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        NxtEarnManager.OnNxtChanged -= UpdateNxt;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    /// <summary>Switch bottom-nav tab. 0=Home 1=Search 2=Hub 3=FanClub 4=Profile.</summary>
    public void SwitchTab(int index)
    {
        if (index == _currentTab || index < 0 || index >= screens.Length) return;
        _currentTab = index;

        for (int i = 0; i < screens.Length; i++)
        {
            if (screens[i] != null)
                screens[i].SetActive(i == index);

            bool active = i == index;
            Color accent = (tabAccents != null && i < tabAccents.Length)
                ? tabAccents[i] : Color.white;

            if (navLabels != null && i < navLabels.Length && navLabels[i] != null)
            {
                navLabels[i].color = active ? accent : INACTIVE;
                navLabels[i].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
            }
            if (navDots != null && i < navDots.Length && navDots[i] != null)
            {
                navDots[i].enabled = active;
                navDots[i].color = accent;
            }
        }
    }

    /// <summary>Enter a 3D space additively (spec §3.3). Shell hides once loaded.</summary>
    public void EnterSpace(string sceneName)
    {
        SceneLoader.Instance?.LoadAdditive(sceneName);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Additive) return;
        // A 3D space took over — hide the 2D shell and our camera.
        if (shellRoot != null) shellRoot.SetActive(false);
        if (mainSceneCamera != null) mainSceneCamera.gameObject.SetActive(false);

        // Unity requires exactly one EventSystem. Keep OURS (proven to dispatch
        // clicks in this project) and disable the one shipped inside the
        // additive scene — EventSystems are global, so ours serves the lounge
        // UI's GraphicRaycaster too. (On exit, MainScene reloads fresh.)
        var systems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(
            FindObjectsSortMode.None);
        if (systems.Length > 1)
        {
            UnityEngine.EventSystems.EventSystem keep = null;
            foreach (var es in systems)
                if (es.gameObject.scene != scene) { keep = es; break; }
            if (keep == null) keep = systems[0];
            foreach (var es in systems)
                if (es != keep) es.gameObject.SetActive(false);
        }
    }

    void UpdateNxt(int total, int delta)
    {
        if (nxtBalanceText != null)
            nxtBalanceText.text = $"{total} NXT";
    }
}
