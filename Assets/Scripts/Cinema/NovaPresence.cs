using UnityEngine;

/// <summary>
/// AUDIOVIDO — NOVA (Cinema host, spec §4.1)
/// Placeholder presence for the full character: gentle idle bob and a
/// pulsing gold glow. Replaced by a rigged model in the art pass.
/// </summary>
public class NovaPresence : MonoBehaviour
{
    [SerializeField] Renderer[] glowRenderers;
    [SerializeField] Color accent = new Color(1f, 0.84f, 0.4f); // NOVA gold
    [SerializeField] float bobAmount = 0.06f;
    [SerializeField] float bobSpeed = 1.2f;
    [SerializeField] float baseIntensity = 1.7f;
    [SerializeField] float pulseAmount = 0.45f;
    [SerializeField] float pulseSpeed = 2f;

    static readonly int GlowColor = Shader.PropertyToID("_BaseColor"); // glow parts use URP Unlit
    MaterialPropertyBlock _mpb;
    Vector3 _basePos;

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _basePos = transform.localPosition;
    }

    void Update()
    {
        transform.localPosition = _basePos +
            Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmount);

        if (glowRenderers == null) return;
        float i = baseIntensity + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        _mpb.SetColor(GlowColor, accent * i);
        foreach (Renderer r in glowRenderers)
            if (r != null) r.SetPropertyBlock(_mpb);
    }
}
