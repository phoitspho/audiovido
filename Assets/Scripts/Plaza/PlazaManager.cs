using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AUDIOVIDO — Fan Plaza &amp; Social Hub (concept art panel 6: "Connect. Share.
/// Belong. Inspire.") The community heart of the city: a giant hologram of
/// tonight's featured artist rotates over the pedestal while VIBE hypes the
/// crowd and community updates stream in. WAVE! sends love (first wave earns
/// an NXT bonus). Exits to Scene_City.
/// </summary>
public class PlazaManager : MonoBehaviour
{
    public static PlazaManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] PlazaUIController ui;
    [SerializeField] Transform hologramRoot;
    [SerializeField] Renderer[] hologramRenderers;
    [SerializeField] Renderer pedestalRing;
    [SerializeField] CanvasGroup fadeCanvas;

    [Header("Hologram")]
    [SerializeField] Color holoColor = new Color(0.3f, 0.62f, 1f);
    [SerializeField] float rotateSpeed = 22f;   // deg/s
    [SerializeField] float bobAmount = 0.15f;
    [SerializeField] float fadeDuration = 0.3f;

    static readonly int GlowColor = Shader.PropertyToID("_BaseColor"); // URP Unlit

    static readonly string[] COMMUNITY_LINES =
    {
        "Synth Tribe just hit Level 8!",
        "New challenge dropped in Cyber Collective!",
        "Luna Shadow is trending in Music Street!",
        "3 friends are watching in the Cinema right now.",
        "Bass Nation started a live stream!"
    };

    MaterialPropertyBlock _mpb;
    Vector3 _holoBasePos;
    float _wavePulse;           // extra hologram glow after a wave
    bool _firstWaveAwarded;
    bool _exiting;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _mpb = new MaterialPropertyBlock();
    }

    IEnumerator Start()
    {
        if (hologramRoot != null) _holoBasePos = hologramRoot.localPosition;

        NxtEarnManager.Instance?.StartEarning();
        yield return FadeIn();

        ui?.ShowVibeLine("Your tribe is here! Welcome to the Plaza!");

        // Community feed drip
        while (!_exiting)
        {
            yield return new WaitForSeconds(Random.Range(8f, 14f));
            if (!_exiting)
                ui?.SetFeedLine(COMMUNITY_LINES[Random.Range(0, COMMUNITY_LINES.Length)]);
        }
    }

    void Update()
    {
        _wavePulse = Mathf.MoveTowards(_wavePulse, 0f, Time.deltaTime * 1.2f);

        // Hologram: slow rotation, gentle bob, flickery glow
        if (hologramRoot != null)
        {
            hologramRoot.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.Self);
            hologramRoot.localPosition = _holoBasePos +
                Vector3.up * (Mathf.Sin(Time.time * 0.9f) * bobAmount);
        }

        float flicker = 1.6f
            + Mathf.PerlinNoise(Time.time * 2.2f, 4.1f) * 0.7f
            + _wavePulse * 1.6f;
        _mpb.SetColor(GlowColor, holoColor * flicker);
        if (hologramRenderers != null)
            foreach (Renderer r in hologramRenderers)
                if (r != null) r.SetPropertyBlock(_mpb);

        if (pedestalRing != null)
        {
            _mpb.SetColor(GlowColor, holoColor * (1.4f + _wavePulse * 1.2f
                + Mathf.Sin(Time.time * 2f) * 0.25f));
            pedestalRing.SetPropertyBlock(_mpb);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Send love to the featured artist — hologram surges, VIBE celebrates.</summary>
    public void OnWavePressed()
    {
        if (_exiting) return;
        _wavePulse = 1f;
        ui?.ShowVibeLine("YESSS! The hologram felt that!");
        if (!_firstWaveAwarded)
        {
            _firstWaveAwarded = true;
            NxtEarnManager.Instance?.AwardInteractionBonus();
        }
    }

    public void ExitPlaza(string returnScene = "Scene_City")
    {
        if (_exiting) return;
        _exiting = true;
        NxtEarnManager.Instance?.StopEarning();
        StartCoroutine(ExitRoutine(returnScene));
    }

    // ── Transitions ───────────────────────────────────────────────────────────

    IEnumerator ExitRoutine(string returnScene)
    {
        ui?.HideAll();
        yield return FadeOut();
        Debug.Log($"[Plaza] Loading {returnScene}");
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
