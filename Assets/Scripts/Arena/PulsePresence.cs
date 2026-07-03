using UnityEngine;

/// <summary>
/// AUDIOVIDO — PULSE (Stage Manager, spec §4.1)
/// Placeholder presence: high-energy bounce and an electric yellow glow that
/// snaps to the beat. Replaced by a rigged model in the art pass.
/// </summary>
public class PulsePresence : MonoBehaviour
{
    [SerializeField] Renderer[] glowRenderers;
    [SerializeField] Color accent = new Color(1f, 0.95f, 0.25f); // electric yellow
    [SerializeField] float bounceHeight = 0.18f;
    [SerializeField] float bpm = 124f;
    [SerializeField] float baseIntensity = 1.9f;
    [SerializeField] float pulseAmount = 0.7f;

    static readonly int GlowColor = Shader.PropertyToID("_BaseColor"); // URP Unlit
    MaterialPropertyBlock _mpb;
    Vector3 _basePos;

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _basePos = transform.localPosition;
    }

    void Update()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock(); // survives domain reload
        float beat = Time.time * (bpm / 60f);
        // Sharp bounce locked to the beat — PULSE is "always moving" (spec)
        float bounce = Mathf.Abs(Mathf.Sin(beat * Mathf.PI));
        transform.localPosition = _basePos + Vector3.up * (bounce * bounceHeight);

        if (glowRenderers == null) return;
        float i = baseIntensity + bounce * pulseAmount;
        _mpb.SetColor(GlowColor, accent * i);
        foreach (Renderer r in glowRenderers)
            if (r != null) r.SetPropertyBlock(_mpb);
    }
}
