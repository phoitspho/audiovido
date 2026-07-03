using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AUDIOVIDO — City Manager (3D city hub navigation)
/// The city IS the app's main navigation: tap a district → camera flies in →
/// info card → enter its 3D space (spec §3.3 additive load).
/// References wired by CitySceneBuilder.
/// </summary>
public class CityManager : MonoBehaviour
{
    public static CityManager Instance { get; private set; }

    [Header("Core")]
    [SerializeField] CityCameraController cameraController;
    [SerializeField] Camera mainCamera;
    [SerializeField] GameObject worldRoot; // all city geometry — hidden inside 3D spaces

    [Header("UI")]
    [SerializeField] GameObject uiRoot;
    [SerializeField] TMP_Text nxtBalanceText;
    [SerializeField] TMP_Text hintText;
    [SerializeField] GameObject infoCard;
    [SerializeField] TMP_Text districtNameText;
    [SerializeField] TMP_Text taglineText;
    [SerializeField] Button enterButton;
    [SerializeField] TMP_Text enterLabel;
    [SerializeField] Button backButton;
    [SerializeField] CanvasGroup fadeCanvas;

    CityDistrict _focused;
    Vector2 _downPos;
    bool _pressTracked;

    void Awake() => Instance = this;

    void Start()
    {
        if (infoCard != null) infoCard.SetActive(false);
        enterButton?.onClick.AddListener(OnEnterPressed);
        backButton?.onClick.AddListener(OnBackPressed);

        AudioManager.Instance?.Play("city_ambient", 0.4f);

        UpdateNxt(NxtEarnManager.Instance != null ? NxtEarnManager.Instance.Balance : 0, 0);
        NxtEarnManager.OnNxtChanged += UpdateNxt;

        // SceneLoader singleton outlives scenes; re-point its fade overlay at ours.
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

    void Update()
    {
        if (TryGetTap(out Vector2 screenPos))
            HandleTap(screenPos);
    }

    // ── Tap detection (tap = press + release without drag) ───────────────────

    bool TryGetTap(out Vector2 pos)
    {
        pos = default;

        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { _downPos = t.position; _pressTracked = true; }
            else if (t.phase == TouchPhase.Ended && _pressTracked)
            {
                _pressTracked = false;
                if ((t.position - _downPos).sqrMagnitude < 400f) { pos = t.position; return true; }
            }
            return false;
        }

        // NOTE: no else-if — press and release can land on the SAME frame
        // (fast/scripted clicks), and both must be processed.
        if (Input.GetMouseButtonDown(0)) { _downPos = Input.mousePosition; _pressTracked = true; }
        if (Input.GetMouseButtonUp(0) && _pressTracked)
        {
            _pressTracked = false;
            if (((Vector2)Input.mousePosition - _downPos).sqrMagnitude < 400f)
            { pos = Input.mousePosition; return true; }
        }
        return false;
    }

    void HandleTap(Vector2 screenPos)
    {
        // Ignore taps on UI
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;
        if (mainCamera == null || cameraController == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 300f)) return;

        CityDistrict district = hit.collider.GetComponentInParent<CityDistrict>();
        if (district != null && district != _focused)
            FocusDistrict(district);
    }

    // ── Focus flow ────────────────────────────────────────────────────────────

    void FocusDistrict(CityDistrict d)
    {
        _focused = d;
        if (infoCard != null) infoCard.SetActive(false);
        if (hintText != null) hintText.gameObject.SetActive(false);

        Vector3 pos = d.transform.position;
        float yaw = Mathf.Atan2(pos.x, pos.z) * Mathf.Rad2Deg; // approach from city center
        cameraController.InputLocked = true;
        cameraController.FlyTo(pos + Vector3.up * 4f, 16f, yaw, 30f, 0.8f,
            () => ShowInfoCard(d));
    }

    void ShowInfoCard(CityDistrict d)
    {
        if (infoCard == null || d != _focused) return;
        if (districtNameText != null) districtNameText.text = d.DistrictName;
        if (taglineText != null) taglineText.text = d.Tagline;

        bool open = !string.IsNullOrEmpty(d.SceneToLoad);
        if (enterLabel != null) enterLabel.text = open ? "Enter  →" : "Coming soon";
        if (enterButton != null) enterButton.interactable = open;
        infoCard.SetActive(true);
    }

    void OnEnterPressed()
    {
        if (_focused == null || string.IsNullOrEmpty(_focused.SceneToLoad)) return;
        SceneLoader.Instance?.LoadAdditive(_focused.SceneToLoad);
    }

    void OnBackPressed()
    {
        _focused = null;
        if (infoCard != null) infoCard.SetActive(false);
        cameraController.FlyToOverview(0.8f, () =>
        {
            cameraController.InputLocked = false;
            if (hintText != null) hintText.gameObject.SetActive(true);
        });
    }

    // ── 3D space handoff ─────────────────────────────────────────────────────

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Additive) return;

        // A space took over: hide city UI, camera, AND world geometry —
        // both scenes share the origin, so city towers would poke through
        // the space's interior otherwise.
        if (uiRoot != null) uiRoot.SetActive(false);
        if (mainCamera != null) mainCamera.gameObject.SetActive(false);
        if (worldRoot != null) worldRoot.SetActive(false);

        // Exactly one EventSystem: keep the city's (proven module), disable the
        // additive scene's copy. On exit, the space single-loads Scene_City fresh.
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
        if (nxtBalanceText != null) nxtBalanceText.text = $"{total} NXT";
    }
}
