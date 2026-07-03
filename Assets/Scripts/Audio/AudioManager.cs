using System.Collections;
using UnityEngine;

/// <summary>
/// AUDIOVIDO — Audio Manager (spec §12.7)
/// Persistent music player: one track at a time with crossfade between
/// scenes (city hum → lounge lo-fi → arena set → ...), pause/resume,
/// and real-time FFT band energies for music-reactive visuals.
///
/// Sample tracks are procedurally generated placeholders — the real
/// catalog arrives via the content API (backend: Pedram). Swapping a
/// track = replacing the .wav in Assets/Audio with the same name.
/// Created + wired by CitySceneBuilder; survives every scene load.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] AudioClip[] clips;
    [SerializeField] float crossfadeSeconds = 1.2f;

    AudioSource _a, _b, _active;
    readonly float[] _spectrum = new float[512];
    float _spectrumTime = -1f;
    string _currentName;

    public bool IsPlaying => _active != null && _active.isPlaying;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _a = gameObject.AddComponent<AudioSource>();
        _b = gameObject.AddComponent<AudioSource>();
        foreach (AudioSource s in new[] { _a, _b })
        {
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
        }
        _active = _a;
    }

    // ── Playback ─────────────────────────────────────────────────────────────

    /// <summary>Play a track by clip name, crossfading from whatever is on.</summary>
    public void Play(string clipName, float volume = 0.6f)
    {
        if (_currentName == clipName && _active.clip != null)
        {
            _active.volume = volume;
            if (!_active.isPlaying) _active.UnPause();
            return;
        }

        AudioClip clip = FindClip(clipName);
        if (clip == null)
        {
            Debug.LogWarning($"[Audio] Clip '{clipName}' not found.");
            return;
        }

        _currentName = clipName;
        AudioSource next = _active == _a ? _b : _a;
        next.clip = clip;
        next.volume = 0f;
        next.Play();

        StopAllCoroutines();
        StartCoroutine(CrossfadeRoutine(_active, next, volume));
        _active = next;
    }

    public void PauseMusic() { if (_active != null) _active.Pause(); }
    public void ResumeMusic() { if (_active != null) _active.UnPause(); }

    // ── Spectrum (music-reactive visuals) ────────────────────────────────────

    /// <summary>
    /// Energy of log-spaced frequency band <paramref name="band"/> of
    /// <paramref name="bandCount"/>, roughly 0..1. Band 0 = bass.
    /// </summary>
    public float GetBandEnergy(int band, int bandCount)
    {
        if (_active == null || !_active.isPlaying) return 0f;

        // Refresh FFT once per frame regardless of how many callers ask
        if (!Mathf.Approximately(Time.unscaledTime, _spectrumTime))
        {
            _active.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);
            _spectrumTime = Time.unscaledTime;
        }

        float lo = Mathf.Pow(256f, (float)band / bandCount);
        float hi = Mathf.Pow(256f, (float)(band + 1) / bandCount);
        int i0 = Mathf.Clamp((int)lo, 1, 255);
        int i1 = Mathf.Clamp((int)hi, i0 + 1, 256);

        float sum = 0f;
        for (int i = i0; i < i1; i++) sum += _spectrum[i];
        float avg = sum / (i1 - i0);

        // Rough normalization; boost higher bands (naturally quieter)
        return Mathf.Clamp01(avg * 60f * Mathf.Sqrt(band + 1f));
    }

    // ── Internals ────────────────────────────────────────────────────────────

    IEnumerator CrossfadeRoutine(AudioSource from, AudioSource to, float targetVolume)
    {
        float fromStart = from != null ? from.volume : 0f;
        float t = 0f;
        while (t < crossfadeSeconds)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / crossfadeSeconds);
            if (from != null) from.volume = fromStart * (1f - k);
            to.volume = targetVolume * k;
            yield return null;
        }
        if (from != null) { from.Stop(); from.volume = 0f; }
        to.volume = targetVolume;
    }

    AudioClip FindClip(string clipName)
    {
        if (clips == null) return null;
        foreach (AudioClip c in clips)
            if (c != null && c.name == clipName) return c;
        return null;
    }
}
