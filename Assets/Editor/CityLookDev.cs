using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// AUDIOVIDO — Cinematic Look-Dev: "Silver Moonlit Night"
/// Menu: AUDIOVIDO → Look → Apply Silver Moonlit Night (City)
///
/// The City is a neon model floating in space:
///   • Night sky — star field, galaxy band, low SILVER MOON (no sun). Neon is
///     the only real light source, so lit windows finally make sense.
///   • Cool cinematic grade — bloom, ACES, blue/silver split-toning, vignette,
///     grain, chromatic aberration.
///   • Cool low moonlight aligned to the moon; deep-blue ambient + fog.
///   • The "Ground" surface becomes dark glossy obsidian that reflects the stars,
///     moon and neon (realtime reflection probe) — the interactive tabletop in space.
///   • Comets streak across the sky at runtime.
///
/// Idempotent + re-runnable. Assets written to Assets/Settings/.
/// </summary>
public static class CityLookDev
{
    const string ProfilePath = "Assets/Settings/CityLookDev_Profile.asset";
    const string SkyMatPath  = "Assets/Settings/NightSky.mat";
    const string CityScene   = "Assets/Scenes/Scene_City.unity";

    // Shared moon direction (front + low), so sky, moonlight and horizon-glow agree.
    static readonly Vector3 MoonDir = new Vector3(0.15f, 0.14f, 1f);

    [MenuItem("AUDIOVIDO/Look/Apply Silver Moonlit Night (City)")]
    public static void ApplyCity()
    {
        if (SceneManager.GetActiveScene().path != CityScene)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(CityScene, OpenSceneMode.Single);
        }

        Directory.CreateDirectory("Assets/Settings");
        CleanupDusk();
        BuildSky();
        BuildEnvironmentAndFog();
        BuildMoonlight();
        BuildVolume();
        ConfigureCameraAndPipeline();
        BuildReflectionProbe();
        BuildComets();
        DressMaterials();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[LookDev] Silver Moonlit Night applied to City — night sky, silver moon, moonlight, obsidian surface, reflections, comets, cool grade.");
    }

    // Remove the earlier dusk experiment so nothing stale lingers.
    static void CleanupDusk()
    {
        foreach (string p in new[] { "Assets/Shaders/AudiovidoDuskSky.shader", "Assets/Settings/DuskSky.mat" })
            if (File.Exists(p)) AssetDatabase.DeleteAsset(p);
    }

    // ── Night sky ───────────────────────────────────────────────────────────
    static void BuildSky()
    {
        Shader sh = Shader.Find("AUDIOVIDO/NightSky");
        if (sh == null) { Debug.LogError("[LookDev] Shader AUDIOVIDO/NightSky not found (still compiling?)."); return; }

        Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMatPath);
        if (sky == null) { sky = new Material(sh); AssetDatabase.CreateAsset(sky, SkyMatPath); }
        sky.shader = sh;
        sky.SetColor("_ZenithColor",  new Color(0.02f, 0.02f, 0.06f));
        sky.SetColor("_HorizonColor", new Color(0.02f, 0.03f, 0.10f));
        sky.SetColor("_GroundColor",  new Color(0.004f, 0.004f, 0.010f));
        sky.SetColor("_GalaxyColor",  new Color(0.16f, 0.12f, 0.30f));
        sky.SetColor("_MoonColor",    new Color(0.86f, 0.90f, 1.0f));
        sky.SetVector("_MoonDir",     new Vector4(MoonDir.x, MoonDir.y, MoonDir.z, 0f));
        sky.SetFloat("_MoonSize", 0.030f);
        sky.SetFloat("_MoonGlow", 90f);
        sky.SetFloat("_StarDensity", 55f);
        sky.SetFloat("_StarSparsity", 0.965f);
        sky.SetFloat("_StarRadius", 0.00006f);
        sky.SetFloat("_StarBright", 0.55f);
        sky.SetFloat("_Twinkle", 3f);
        sky.SetFloat("_Exposure", 1f);
        EditorUtility.SetDirty(sky);

        RenderSettings.skybox = sky;
        DynamicGI.UpdateEnvironment();
    }

    // ── Ambient + fog (cool night) ──────────────────────────────────────────
    static void BuildEnvironmentAndFog()
    {
        RenderSettings.ambientMode         = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.03f, 0.04f, 0.09f);
        RenderSettings.ambientEquatorColor = new Color(0.02f, 0.02f, 0.05f);
        RenderSettings.ambientGroundColor  = new Color(0.004f, 0.004f, 0.010f);
        RenderSettings.ambientIntensity    = 0.8f;
        RenderSettings.reflectionIntensity = 1.0f;

        RenderSettings.fog        = true;
        RenderSettings.fogMode    = FogMode.ExponentialSquared;
        RenderSettings.fogColor   = new Color(0.015f, 0.02f, 0.05f);
        RenderSettings.fogDensity = 0.012f;
    }

    // ── Cool silver moonlight (low key; neon dominates) ─────────────────────
    static void BuildMoonlight()
    {
        Light moon = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { moon = l; break; }
        if (moon == null) return;

        moon.color             = new Color(0.55f, 0.66f, 1.0f);
        moon.intensity         = 0.35f;
        moon.transform.rotation = Quaternion.LookRotation(-MoonDir.normalized);
        moon.shadows           = LightShadows.Soft;
        moon.shadowStrength    = 0.5f;
        EditorUtility.SetDirty(moon);
    }

    // ── Cinematic post-processing (cool night grade) ────────────────────────
    static void BuildVolume()
    {
        if (File.Exists(ProfilePath)) AssetDatabase.DeleteAsset(ProfilePath);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);

        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(1.3f);
        bloom.threshold.Override(0.9f);
        bloom.scatter.Override(0.75f);
        bloom.tint.Override(new Color(0.9f, 0.95f, 1.0f));
        bloom.highQualityFiltering.Override(true);

        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);

        var split = profile.Add<SplitToning>(true); // cool: blue shadows, silver highlights
        split.shadows.Override(new Color(0.05f, 0.14f, 0.40f));
        split.highlights.Override(new Color(0.75f, 0.85f, 1.0f));
        split.balance.Override(0f);

        var ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(-0.05f);
        ca.contrast.Override(12f);
        ca.saturation.Override(-4f);
        ca.colorFilter.Override(new Color(0.92f, 0.96f, 1.05f));

        var wb = profile.Add<WhiteBalance>(true);
        wb.temperature.Override(-12f);
        wb.tint.Override(6f);

        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.42f);
        vig.smoothness.Override(0.5f);
        vig.color.Override(new Color(0.0f, 0.01f, 0.03f));

        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.3f);
        grain.response.Override(0.7f);

        var chroma = profile.Add<ChromaticAberration>(true);
        chroma.intensity.Override(0.12f);

        EditorUtility.SetDirty(profile);

        var go = GameObject.Find("LookDev Volume") ?? new GameObject("LookDev Volume");
        var vol = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f;
        vol.sharedProfile = profile;
        EditorUtility.SetDirty(go);

        // Silence any earlier global volume (e.g. builder's "PostFX") so it can't stack.
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
            if (v != vol && v.isGlobal) { v.enabled = false; EditorUtility.SetDirty(v); }
    }

    // ── Camera + URP pipeline flags ─────────────────────────────────────────
    static void ConfigureCameraAndPipeline()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.allowHDR = true;
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            EditorUtility.SetDirty(cam);
        }

        var rp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (rp != null)
        {
            try
            {
                var so = new SerializedObject(rp);
                var hdr = so.FindProperty("m_SupportsHDR");
                if (hdr != null) { hdr.boolValue = true; so.ApplyModifiedProperties(); EditorUtility.SetDirty(rp); }
            }
            catch { /* non-fatal */ }
        }
    }

    // ── Realtime reflection probe (neon + stars on the obsidian surface) ─────
    static void BuildReflectionProbe()
    {
        var go = GameObject.Find("CityReflectionProbe");
        if (go == null) go = new GameObject("CityReflectionProbe");
        go.transform.position = new Vector3(0f, 6f, 0f);
        var probe = go.GetComponent<ReflectionProbe>();
        if (probe == null) probe = go.AddComponent<ReflectionProbe>();
        probe.mode            = ReflectionProbeMode.Realtime;
        probe.refreshMode     = ReflectionProbeRefreshMode.EveryFrame;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.clearFlags      = ReflectionProbeClearFlags.Skybox;
        probe.boxProjection   = true;
        probe.size            = new Vector3(150f, 80f, 150f);
        probe.center          = new Vector3(0f, 8f, 0f);
        probe.resolution      = 128;
        probe.hdr             = true;
        probe.cullingMask     = ~0;
        probe.intensity       = 1f;
        EditorUtility.SetDirty(go);
    }

    // ── Comets ──────────────────────────────────────────────────────────────
    static void BuildComets()
    {
        var go = GameObject.Find("Comets") ?? new GameObject("Comets");
        if (go.GetComponent<CometSpawner>() == null) go.AddComponent<CometSpawner>();
        EditorUtility.SetDirty(go);
    }

    // ── Obsidian surface + normalized neon emission ─────────────────────────
    static void DressMaterials()
    {
        var seen = new System.Collections.Generic.HashSet<Material>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            string n = r.gameObject.name.ToLowerInvariant();
            bool isGround = n == "ground";
            bool glossy   = isGround || n.Contains("lane") || n.Contains("road") ||
                            n.Contains("plazaring") || n.Contains("plazacenter");

            foreach (var m in r.sharedMaterials)
            {
                if (m == null || !seen.Add(m)) continue;

                if (isGround)
                {
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.015f, 0.016f, 0.022f, 1f));
                    if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic", 0.6f);
                    if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.92f);
                }
                else if (glossy)
                {
                    if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic", 0.3f);
                    if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.85f);
                }

                // Normalize emission to a fixed intensity → consistent bloom, idempotent on re-run.
                if (m.HasProperty("_EmissionColor"))
                {
                    Color e = m.GetColor("_EmissionColor");
                    float mx = e.maxColorComponent;
                    if (mx > 0.05f)
                    {
                        m.EnableKeyword("_EMISSION");
                        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                        m.SetColor("_EmissionColor", (e / mx) * 2.2f);
                    }
                }
                EditorUtility.SetDirty(m);
            }
        }
    }
}
