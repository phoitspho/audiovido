using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AUDIOVIDO — Bar / Lounge 3D Space (SCR-19)
/// Spec §5.13 — manages lounge state, entry/exit transitions,
/// ambient night-mode timing, DRIFT proximity triggers, ambient audio,
/// NXT passive earn, and DRIFT deep mode.
/// </summary>
public class LoungeManager : MonoBehaviour
{
    public static LoungeManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] DriftController drift;
    [SerializeField] LoungeUIController ui;

    [Header("Transition")]
    [SerializeField] CanvasGroup fadeCanvas;
    [SerializeField] float fadeDuration = 0.3f;        // spec §9.1: 300ms

    [Header("Night Mode")]
    [SerializeField] float nightModeStartHour = 21f;
    [SerializeField] Light[] ambientLights;
    [SerializeField] float nightIntensityMultiplier = 0.5f;

    [Header("Ambient Audio")]
    [SerializeField] AudioSource ambientAudio;
    [SerializeField] float normalVolume  = 0.55f;
    [SerializeField] float nightVolume   = 0.35f;
    [SerializeField] float audioFadeTime = 1.5f;

    [Header("Deep Mode")]
    [SerializeField] float deepModeDelay      = 120f;  // 2 min after first mood pick
    [SerializeField] float deepModeIntervalMin = 180f; // 3 min between lines
    [SerializeField] float deepModeIntervalMax = 300f; // 5 min between lines

    // ── State ────────────────────────────────────────────────────────────────
    public enum LoungeState { Entering, Ambient, DriftConversation, Exiting }
    public LoungeState State { get; private set; } = LoungeState.Entering;

    bool _deepModeStarted;
    bool _firstMoodPicked;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()  => NxtEarnManager.OnNxtChanged += OnNxtChanged;
    void OnDisable() => NxtEarnManager.OnNxtChanged -= OnNxtChanged;

    IEnumerator Start()
    {
        StartAmbientAudio();

        // Start passive NXT earn
        NxtEarnManager.Instance?.StartEarning();

        yield return FadeIn();
        State = LoungeState.Ambient;
        ApplyNightMode();
        drift?.PlayIdleGreeting();
        ui?.ShowEntryBubble();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void ExitLounge(string returnScene = "Scene_City")
    {
        if (State == LoungeState.Exiting) return;
        NxtEarnManager.Instance?.StopEarning();
        StartCoroutine(ExitRoutine(returnScene));
    }

    public void OnPlayerApproachBar()
    {
        if (State != LoungeState.Ambient) return;
        State = LoungeState.DriftConversation;
        drift?.OnPlayerApproached();
        string greeting = drift != null ? drift.GetGreetingLine() : "Long day?";
        ui?.ShowDriftBubble(greeting);
        ShowMoodSelection();
    }

    public void OnConversationClosed()
    {
        if (State != LoungeState.DriftConversation) return;
        State = LoungeState.Ambient;
        drift?.ReturnToIdle();
        ui?.HideDriftBubble();
        ui?.HideMoodPanel();
    }

    public void ShowMoodSelection()
    {
        if (State != LoungeState.DriftConversation) return;
        StartCoroutine(DelayedMoodPanel(1.8f));
    }

    public void OnMoodSelected(MoodType mood)
    {
        if (State != LoungeState.DriftConversation) return;
        ui?.HideMoodPanel();

        var (line, track) = drift != null
            ? drift.GetMoodResponse(mood)
            : ("Here you go.", "Lo-fi chill mix");

        drift?.ReactPositive();
        ui?.ShowDriftBubble(line);
        ui?.SetNowPlaying(track);

        // NXT: first mood interaction bonus
        if (!_firstMoodPicked)
        {
            _firstMoodPicked = true;
            NxtEarnManager.Instance?.AwardInteractionBonus();
        }

        // Return to ambient after DRIFT responds
        StartCoroutine(ReturnToAmbientAfter(6f));

        // Start deep mode timer on first mood pick
        if (!_deepModeStarted)
        {
            _deepModeStarted = true;
            StartCoroutine(DeepModeTimer());
        }
    }

    // ── NXT callback ──────────────────────────────────────────────────────────

    void OnNxtChanged(int total, int delta)
    {
        ui?.SetNxtBalance(total);
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    IEnumerator ReturnToAmbientAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (State == LoungeState.DriftConversation)
        {
            State = LoungeState.Ambient;
            drift?.ReturnToIdle();
        }
    }

    IEnumerator DelayedMoodPanel(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (State == LoungeState.DriftConversation)
            ui?.ShowMoodPanel();
    }

    IEnumerator DeepModeTimer()
    {
        yield return new WaitForSeconds(deepModeDelay);
        StartCoroutine(DeepModeRoutine());
    }

    IEnumerator DeepModeRoutine()
    {
        while (State != LoungeState.Exiting)
        {
            float wait = Random.Range(deepModeIntervalMin, deepModeIntervalMax);
            yield return new WaitForSeconds(wait);

            // Only interrupt when player is idle (not mid-conversation)
            if (State == LoungeState.Ambient)
            {
                string line = drift != null ? drift.GetDeepLine()
                                            : "Music is just time made beautiful.";
                ui?.ShowDriftBubble(line);
                NxtEarnManager.Instance?.AwardDeepModeBonus();
                Debug.Log($"[Lounge] DRIFT deep mode: \"{line}\"");
            }
        }
    }

    IEnumerator ExitRoutine(string returnScene)
    {
        Debug.Log("[Lounge] ExitRoutine started");
        State = LoungeState.Exiting;
        ui?.HideAll();
        yield return FadeOut();
        Debug.Log($"[Lounge] Loading {returnScene}");
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

    void ApplyNightMode()
    {
        float hour = System.DateTime.Now.Hour;
        bool isNight = hour >= nightModeStartHour || hour < 5f;

        if (isNight)
            foreach (var l in ambientLights)
                if (l) l.intensity *= nightIntensityMultiplier;

        StartCoroutine(FadeAudioVolume(isNight ? nightVolume : normalVolume));
    }

    // ── Audio ─────────────────────────────────────────────────────────────────

    void StartAmbientAudio()
    {
        if (ambientAudio == null) return;
        ambientAudio.loop = true;
        ambientAudio.volume = 0f;
        ambientAudio.Play();
        StartCoroutine(FadeAudioVolume(normalVolume));
    }

    IEnumerator FadeAudioVolume(float target)
    {
        if (ambientAudio == null) yield break;
        float start = ambientAudio.volume;
        float t = 0f;
        while (t < audioFadeTime)
        {
            t += Time.deltaTime;
            ambientAudio.volume = Mathf.Lerp(start, target, t / audioFadeTime);
            yield return null;
        }
        ambientAudio.volume = target;
    }
}
