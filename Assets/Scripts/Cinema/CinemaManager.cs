using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AUDIOVIDO — Cinema 3D Space (SCR-17, spec §5.11)
/// Manages cinema state, entry/exit fades, the "projected" screen glow,
/// play/pause, NOVA's host lines, and passive NXT earning.
/// Entered additively from the city (Cinema District); exits back to Scene_City.
/// </summary>
public class CinemaManager : MonoBehaviour
{
    public static CinemaManager Instance { get; private set; }

    public enum CinemaState { Entering, Watching, Paused, Exiting }
    public CinemaState State { get; private set; } = CinemaState.Entering;

    [Header("References")]
    [SerializeField] CinemaUIController ui;
    [SerializeField] Renderer screenRenderer;
    [SerializeField] CanvasGroup fadeCanvas;

    [Header("Transition")]
    [SerializeField] float fadeDuration = 0.3f; // spec §9.1

    [Header("Feature")]
    [SerializeField] string featureTitle = "Beyond The Horizon";

    static readonly int GlowColor = Shader.PropertyToID("_BaseColor"); // screen uses URP Unlit
    static readonly Color TINT_COOL = new Color(0.65f, 0.75f, 1f);
    static readonly Color TINT_WARM = new Color(1f, 0.8f, 0.7f);
    static readonly Color SCREEN_IDLE = new Color(0.10f, 0.11f, 0.18f);

    MaterialPropertyBlock _mpb;
    float _flickerSeed;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _mpb = new MaterialPropertyBlock();
        _flickerSeed = Random.Range(0f, 100f);
    }

    IEnumerator Start()
    {
        NxtEarnManager.Instance?.StartEarning();
        yield return FadeIn();
        State = CinemaState.Watching;
        ui?.SetNowShowing(featureTitle, true);
        ui?.ShowNovaLine($"Welcome to your cinema. Tonight: {featureTitle}.");
    }

    void Update()
    {
        if (screenRenderer == null) return;

        // Fake film projection — drifting warm/cool tint with luminance flicker.
        Color emission;
        if (State == CinemaState.Watching)
        {
            float t = Time.time;
            float luminance = 0.8f + Mathf.PerlinNoise(t * 1.7f, _flickerSeed) * 0.7f;
            Color tint = Color.Lerp(TINT_COOL, TINT_WARM, Mathf.PerlinNoise(t * 0.23f, 7.31f));
            emission = tint * luminance * 2.1f;
        }
        else
        {
            emission = SCREEN_IDLE; // paused / dim
        }
        _mpb.SetColor(GlowColor, emission);
        screenRenderer.SetPropertyBlock(_mpb);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void TogglePlayback()
    {
        if (State == CinemaState.Watching)
        {
            State = CinemaState.Paused;
            ui?.SetNowShowing(featureTitle, false);
            ui?.ShowNovaLine("Taking a quick break?"); // spec §5.11.2 NOVA on pause
        }
        else if (State == CinemaState.Paused)
        {
            State = CinemaState.Watching;
            ui?.SetNowShowing(featureTitle, true);
            ui?.ShowNovaLine("And... we're rolling again.");
        }
    }

    public void ExitCinema(string returnScene = "Scene_City")
    {
        if (State == CinemaState.Exiting) return;
        NxtEarnManager.Instance?.StopEarning();
        StartCoroutine(ExitRoutine(returnScene));
    }

    // ── Transitions ───────────────────────────────────────────────────────────

    IEnumerator ExitRoutine(string returnScene)
    {
        State = CinemaState.Exiting;
        ui?.HideAll();
        yield return FadeOut();
        Debug.Log($"[Cinema] Loading {returnScene}");
        SceneManager.LoadScene(returnScene);
    }

    IEnumerator FadeIn()
    {
        if (fadeCanvas == null) yield break;
        fadeCanvas.alpha = 1f;
        fadeCanvas.gameObject.SetActive(true);
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvas.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        fadeCanvas.alpha = 0f;
        fadeCanvas.gameObject.SetActive(false);
    }

    IEnumerator FadeOut()
    {
        if (fadeCanvas == null) yield break;
        fadeCanvas.gameObject.SetActive(true);
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        fadeCanvas.alpha = 1f;
    }
}
