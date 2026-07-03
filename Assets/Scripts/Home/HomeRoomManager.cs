using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AUDIOVIDO — Home District: Your Room (concept mockup "Your Room")
/// The user's personal sanctuary: spinning record player, pulsing lava lamp,
/// themeable neon accents, city view out the window. Theme cycling is the
/// first slice of the mockup's room customization. Exits to Scene_City.
/// </summary>
public class HomeRoomManager : MonoBehaviour
{
    public static HomeRoomManager Instance { get; private set; }

    [System.Serializable]
    public struct RoomTheme
    {
        public string name;
        public Color accent;
    }

    [Header("References")]
    [SerializeField] HomeRoomUIController ui;
    [SerializeField] Transform recordDisc;
    [SerializeField] Renderer[] themedRenderers;   // neon sign, corner strips, floor strip
    [SerializeField] Renderer lavaRenderer;
    [SerializeField] CanvasGroup fadeCanvas;

    [Header("Vibe")]
    [SerializeField] string trackTitle = "Night Cruise — MobiLack";
    [SerializeField] float discSpinSpeed = 150f; // deg/s
    [SerializeField] float fadeDuration = 0.3f;

    static readonly int GlowColor = Shader.PropertyToID("_BaseColor"); // URP Unlit glow parts
    static readonly Color LAVA_WARM = new Color(1f, 0.45f, 0.15f);

    readonly RoomTheme[] _themes =
    {
        new RoomTheme { name = "Cyan Drift",   accent = new Color(0f, 0.83f, 1f) },
        new RoomTheme { name = "Pink Haze",    accent = new Color(1f, 0.18f, 0.57f) },
        new RoomTheme { name = "Violet Hour",  accent = new Color(0.71f, 0.3f, 1f) },
        new RoomTheme { name = "Gold Dusk",    accent = new Color(1f, 0.84f, 0.3f) },
    };

    MaterialPropertyBlock _mpb;
    int _themeIndex;
    bool _musicPlaying = true;
    bool _exiting;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _mpb = new MaterialPropertyBlock();
    }

    IEnumerator Start()
    {
        NxtEarnManager.Instance?.StartEarning();
        AudioManager.Instance?.Play("home_chill", 0.55f);
        yield return FadeIn();
        ui?.SetNowPlaying(trackTitle, _musicPlaying);
        ui?.SetThemeName(_themes[_themeIndex].name);
    }

    void Update()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock(); // survives domain reload
        // Record spins while music plays
        if (recordDisc != null && _musicPlaying)
            recordDisc.Rotate(0f, discSpinSpeed * Time.deltaTime, 0f, Space.Self);

        // Themed neon breathes with the current accent
        Color accent = _themes[_themeIndex].accent;
        float glow = 1.7f + Mathf.Sin(Time.time * 1.4f) * 0.3f;
        if (themedRenderers != null)
        {
            _mpb.SetColor(GlowColor, accent * glow);
            foreach (Renderer r in themedRenderers)
                if (r != null) r.SetPropertyBlock(_mpb);
        }

        // Lava lamp slow warm pulse (independent of theme)
        if (lavaRenderer != null)
        {
            float lava = 1.4f + Mathf.PerlinNoise(Time.time * 0.4f, 3.7f) * 1.2f;
            _mpb.SetColor(GlowColor, LAVA_WARM * lava);
            lavaRenderer.SetPropertyBlock(_mpb);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ToggleMusic()
    {
        _musicPlaying = !_musicPlaying;
        if (_musicPlaying) AudioManager.Instance?.ResumeMusic();
        else AudioManager.Instance?.PauseMusic();
        ui?.SetNowPlaying(trackTitle, _musicPlaying);
    }

    /// <summary>Cycle to the next room theme (first slice of room customization).</summary>
    public void CycleTheme()
    {
        _themeIndex = (_themeIndex + 1) % _themes.Length;
        ui?.SetThemeName(_themes[_themeIndex].name);
    }

    public void ExitRoom(string returnScene = "Scene_City")
    {
        if (_exiting) return;
        _exiting = true;
        NxtEarnManager.Instance?.StopEarning();
        StartCoroutine(ExitRoutine(returnScene));
    }

    // ── Transitions ───────────────────────────────────────────────────────────

    IEnumerator ExitRoutine(string returnScene)
    {
        yield return FadeOut();
        Debug.Log($"[HomeRoom] Loading {returnScene}");
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
