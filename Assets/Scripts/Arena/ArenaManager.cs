using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AUDIOVIDO — Concert Venue / Club &amp; Dance Arena (SCR-18, spec §5.12)
/// Runs the live set: PULSE's countdown intro, the beat clock driving the
/// visualizer bars + stage lights + crowd bob, hype reactions, NXT earning,
/// and entry/exit transitions. Exits back to Scene_City.
/// </summary>
public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    public enum ArenaState { Entering, Live, Exiting }
    public ArenaState State { get; private set; } = ArenaState.Entering;

    [Header("References")]
    [SerializeField] ArenaUIController ui;
    [SerializeField] Transform[] visualizerBars;
    [SerializeField] Renderer[] visualizerRenderers;
    [SerializeField] Transform[] crowdMembers;
    [SerializeField] CanvasGroup fadeCanvas;

    [Header("Show")]
    [SerializeField] string trackTitle = "Neon Nights — Live Set";
    [SerializeField] float bpm = 124f;
    [SerializeField] float fadeDuration = 0.3f;

    [Header("Visualizer")]
    [SerializeField] float barBaseHeight = 0.6f;
    [SerializeField] float barMaxExtra = 3.4f;
    [SerializeField] Color barColorA = new Color(0.7f, 0.3f, 1f);   // purple
    [SerializeField] Color barColorB = new Color(1f, 0.18f, 0.57f); // pink

    [Header("Crowd")]
    [SerializeField] float crowdBobHeight = 0.14f;

    static readonly int GlowColor = Shader.PropertyToID("_BaseColor"); // bars use URP Unlit
    static readonly string[] HYPE_LINES =
    {
        "LOUDER! I can't hear you!",
        "The crowd is ON FIRE tonight!",
        "Drop incoming... hold on!",
        "This is what we live for!"
    };

    MaterialPropertyBlock _mpb;
    float[] _barPhases;
    float[] _crowdPhases;
    Vector3[] _crowdBasePos;
    float _hypeBoost = 1f;          // >1 briefly after a hype reaction
    bool _firstHypeAwarded;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _mpb = new MaterialPropertyBlock();
    }

    IEnumerator Start()
    {
        // Cache phases / base positions
        _barPhases = new float[visualizerBars != null ? visualizerBars.Length : 0];
        for (int i = 0; i < _barPhases.Length; i++)
            _barPhases[i] = i * 0.9f;

        int crowdCount = crowdMembers != null ? crowdMembers.Length : 0;
        _crowdPhases = new float[crowdCount];
        _crowdBasePos = new Vector3[crowdCount];
        for (int i = 0; i < crowdCount; i++)
        {
            _crowdPhases[i] = (i * 37) % 10 * 0.63f;
            _crowdBasePos[i] = crowdMembers[i] != null
                ? crowdMembers[i].localPosition : Vector3.zero;
        }

        NxtEarnManager.Instance?.StartEarning();
        AudioManager.Instance?.Play("arena_club", 0.6f);
        yield return FadeIn();

        // PULSE countdown (spec §5.12: "3... 2... 1... LET'S GO!")
        ui?.SetNowPlaying(trackTitle);
        ui?.ShowPulseLine("3...", 0.7f);
        yield return new WaitForSeconds(0.8f);
        ui?.ShowPulseLine("2...", 0.7f);
        yield return new WaitForSeconds(0.8f);
        ui?.ShowPulseLine("1...", 0.7f);
        yield return new WaitForSeconds(0.8f);
        ui?.ShowPulseLine("LET'S GOOO!", 2.5f);
        State = ArenaState.Live;

        // Periodic hype lines from PULSE
        while (State == ArenaState.Live)
        {
            yield return new WaitForSeconds(Random.Range(20f, 35f));
            if (State == ArenaState.Live)
                ui?.ShowPulseLine(HYPE_LINES[Random.Range(0, HYPE_LINES.Length)]);
        }
    }

    void Update()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock(); // survives domain reload
        float beat = Time.time * (bpm / 60f);
        _hypeBoost = Mathf.MoveTowards(_hypeBoost, 1f, Time.deltaTime * 0.8f);
        bool live = State == ArenaState.Live;

        // Visualizer bars — REAL spectrum when audio plays (FFT bands),
        // beat-locked pseudo-spectrum as fallback.
        bool haveAudio = AudioManager.Instance != null && AudioManager.Instance.IsPlaying;
        if (visualizerBars != null)
        {
            for (int i = 0; i < visualizerBars.Length; i++)
            {
                Transform bar = visualizerBars[i];
                if (bar == null) continue;
                float energy;
                if (haveAudio)
                {
                    energy = AudioManager.Instance.GetBandEnergy(i, visualizerBars.Length);
                }
                else
                {
                    float pulse = Mathf.Abs(Mathf.Sin(beat * Mathf.PI + _barPhases[i]));
                    energy = pulse * Mathf.PerlinNoise(Time.time * 1.3f, i * 3.7f);
                }
                float h = barBaseHeight +
                    (live ? energy * barMaxExtra * _hypeBoost : 0.15f);
                Vector3 s = bar.localScale;
                bar.localScale = new Vector3(s.x, h, s.z);
                Vector3 p = bar.localPosition;
                bar.localPosition = new Vector3(p.x, h * 0.5f, p.z);
            }
        }

        // Bar colors sweep between purple and pink
        if (visualizerRenderers != null)
        {
            for (int i = 0; i < visualizerRenderers.Length; i++)
            {
                Renderer r = visualizerRenderers[i];
                if (r == null) continue;
                float k = Mathf.PingPong(beat * 0.25f + i * 0.15f, 1f);
                Color c = Color.Lerp(barColorA, barColorB, k) * (live ? 2.2f : 0.8f);
                _mpb.SetColor(GlowColor, c);
                r.SetPropertyBlock(_mpb);
            }
        }

        // Crowd bob — bass energy drives amplitude when audio plays
        if (crowdMembers != null)
        {
            float bassBoost = haveAudio
                ? 0.6f + AudioManager.Instance.GetBandEnergy(0, 9) * 1.2f
                : 1f;
            for (int i = 0; i < crowdMembers.Length; i++)
            {
                Transform member = crowdMembers[i];
                if (member == null) continue;
                float bob = live
                    ? Mathf.Abs(Mathf.Sin(beat * Mathf.PI * 0.5f + _crowdPhases[i]))
                        * crowdBobHeight * _hypeBoost * bassBoost
                    : 0f;
                member.localPosition = _crowdBasePos[i] + Vector3.up * bob;
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Hype reaction (spec §5.12 emoji reactions → crowd surge).</summary>
    public void OnHypePressed()
    {
        if (State != ArenaState.Live) return;
        _hypeBoost = 2.4f;
        ui?.ShowPulseLine("YEAH! Feel that energy!");
        if (!_firstHypeAwarded)
        {
            _firstHypeAwarded = true;
            NxtEarnManager.Instance?.AwardInteractionBonus();
        }
    }

    public void ExitArena(string returnScene = "Scene_City")
    {
        if (State == ArenaState.Exiting) return;
        NxtEarnManager.Instance?.StopEarning();
        StartCoroutine(ExitRoutine(returnScene));
    }

    // ── Transitions ───────────────────────────────────────────────────────────

    IEnumerator ExitRoutine(string returnScene)
    {
        State = ArenaState.Exiting;
        ui?.HideAll();
        yield return FadeOut();
        Debug.Log($"[Arena] Loading {returnScene}");
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
