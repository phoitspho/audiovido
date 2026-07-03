using UnityEngine;

/// <summary>
/// AUDIOVIDO — City District
/// One tappable district in the 3D city hub (Home District, Music Street, ...).
/// Holds metadata, pulses its neon glow, and billboards its label to the camera.
/// </summary>
public class CityDistrict : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] string districtName = "District";
    [SerializeField] string tagline = "";
    [Tooltip("Scene to load when entering. Empty = coming soon.")]
    [SerializeField] string sceneToLoad = "";
    [SerializeField] Color accent = Color.cyan;

    [Header("Visuals")]
    [SerializeField] Renderer[] glowRenderers;
    [SerializeField] Transform label;
    [SerializeField] float pulseSpeed = 1.4f;
    [SerializeField] float baseIntensity = 1.7f;
    [SerializeField] float pulseAmount = 0.55f;

    public string DistrictName => districtName;
    public string Tagline => tagline;
    public string SceneToLoad => sceneToLoad;
    public Color Accent => accent;

    static readonly int GlowColor = Shader.PropertyToID("_BaseColor"); // glow parts use URP Unlit
    MaterialPropertyBlock _mpb;
    float _phase;

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _phase = (GetInstanceID() % 628) * 0.01f; // desync pulses between districts
    }

    void Update()
    {
        // Neon pulse
        if (glowRenderers != null && glowRenderers.Length > 0)
        {
            float i = baseIntensity + Mathf.Sin(Time.time * pulseSpeed + _phase) * pulseAmount;
            _mpb.SetColor(GlowColor, accent * i);
            foreach (Renderer r in glowRenderers)
                if (r != null) r.SetPropertyBlock(_mpb);
        }

        // Label faces the camera
        if (label != null && Camera.main != null)
            label.rotation = Quaternion.LookRotation(
                label.position - Camera.main.transform.position);
    }
}
