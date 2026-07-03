using UnityEngine;

/// <summary>
/// AUDIOVIDO — VIBE (Fan Club Hype Master, spec §4.1)
/// Placeholder presence: energetic bounce and a rainbow-cycling glow —
/// spec: "colorful streaked hair (changes color)... energy is electric."
/// Replaced by a rigged model in the art pass.
/// </summary>
public class VibePresence : MonoBehaviour
{
    [SerializeField] Renderer[] glowRenderers;
    [SerializeField] float hueCycleSpeed = 0.15f;  // full rainbow every ~6.7s
    [SerializeField] float bounceHeight = 0.12f;
    [SerializeField] float bounceSpeed = 2.6f;
    [SerializeField] float glowIntensity = 2f;

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
        float bounce = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed));
        transform.localPosition = _basePos + Vector3.up * (bounce * bounceHeight);

        if (glowRenderers == null) return;
        Color rainbow = Color.HSVToRGB(Mathf.Repeat(Time.time * hueCycleSpeed, 1f), 0.85f, 1f);
        _mpb.SetColor(GlowColor, rainbow * (glowIntensity + bounce * 0.5f));
        foreach (Renderer r in glowRenderers)
            if (r != null) r.SetPropertyBlock(_mpb);
    }
}
