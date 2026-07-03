using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// AUDIOVIDO — City Scene Builder (3D city hub navigation)
/// Menu: AUDIOVIDO → Build City Scene
///
/// Generates Scene_City: a neon night city where districts ARE the navigation
/// (reference: "Audiovido City" concept art). Six districts around a central
/// plaza, emissive URP materials, fog, bloom, tappable via CityManager.
/// Music Street hosts The Lounge (Scene_Lounge). Saves the scene and updates
/// Build Settings. Idempotent — regenerates the whole scene each run.
/// </summary>
public static class CitySceneBuilder
{
    static readonly Color BG_NIGHT   = Hex("05050A");
    static readonly Color GROUND     = Hex("0B0B12");
    static readonly Color BUILDING   = Hex("13131C");
    static readonly Color TEXT_DIM   = Hex("A0A0B8");
    static readonly Color GOLD       = Hex("FFD700");
    static readonly Color CYAN       = Hex("00D4FF");

    const string MAT_DIR = "Assets/Materials/City";

    // name, tagline, sceneToLoad, accent hex
    static readonly string[,] DISTRICTS =
    {
        { "Home District",     "Your room. Your world.",      "Scene_Home",   "00D4FF" },
        { "Music Street",      "New sounds, everywhere.",     "Scene_Lounge", "FF2E92" },
        { "Club & Dance Arena","The beat never stops.",       "Scene_Arena",  "B44CFF" },
        { "Cinema District",   "Now playing: everything.",    "Scene_Cinema", "FF4757" },
        { "Fan Plaza",         "Share. Belong. Inspire.",     "Scene_Plaza",  "4D8DFF" },
    };

    [MenuItem("AUDIOVIDO/Build City Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        if (!AssetDatabase.IsValidFolder(MAT_DIR))
            AssetDatabase.CreateFolder("Assets/Materials", "City");

        // ── Atmosphere ───────────────────────────────────────────────────────
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.06f);
        RenderSettings.fogDensity = 0.009f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.17f, 0.25f);

        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight != null)
        {
            dirLight.intensity = 0.25f;
            dirLight.color = new Color(0.55f, 0.6f, 1f);
            dirLight.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            dirLight.shadows = LightShadows.None;
        }

        // ── Camera ───────────────────────────────────────────────────────────
        Camera cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BG_NIGHT;
        cam.farClipPlane = 220f;
        cam.fieldOfView = 70f; // portrait phones need a wide view to frame the ring
        cam.allowHDR = true;
        var camData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
        CityCameraController camCtrl = cam.gameObject.AddComponent<CityCameraController>();

        // ── Materials ────────────────────────────────────────────────────────
        Material groundMat   = MakeMat("City_Ground",   GROUND,   null, 0f);
        Material buildingMat = MakeMat("City_Building", BUILDING, null, 0f);
        Material plazaMat    = MakeMat("City_Plaza",    Hex("0E0E16"), CYAN, 1.2f);
        Material skylineMat  = MakeMat("City_Skyline",  BUILDING, Hex("1E3A5F"), 0.8f);

        // ── World root (hidden while inside a 3D space) ──────────────────────
        GameObject worldRoot = new GameObject("CityWorld");

        // ── Ground ───────────────────────────────────────────────────────────
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(worldRoot.transform);
        ground.transform.localScale = new Vector3(24f, 1f, 24f); // 240×240 m
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

        // ── Central plaza ────────────────────────────────────────────────────
        GameObject plazaRoot = new GameObject("Plaza");
        plazaRoot.transform.SetParent(worldRoot.transform);
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "PlazaRing";
        ring.transform.SetParent(plazaRoot.transform);
        ring.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        ring.transform.localScale = new Vector3(9f, 0.06f, 9f);
        ring.GetComponent<MeshRenderer>().sharedMaterial = plazaMat;

        GameObject ringCover = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringCover.name = "PlazaCenter";
        ringCover.transform.SetParent(plazaRoot.transform);
        ringCover.transform.localPosition = new Vector3(0f, 0.09f, 0f);
        ringCover.transform.localScale = new Vector3(7f, 0.06f, 7f);
        ringCover.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = "PlazaBeacon";
        beacon.transform.SetParent(plazaRoot.transform);
        beacon.transform.localPosition = new Vector3(0f, 3f, 0f);
        beacon.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
        beacon.GetComponent<MeshRenderer>().sharedMaterial = plazaMat;

        // ── Districts ────────────────────────────────────────────────────────
        GameObject districtsRoot = new GameObject("Districts");
        districtsRoot.transform.SetParent(worldRoot.transform);
        float radius = 21f;
        float[] heights = { 9f, 12f, 8f, 11f, 10f };
        int districtCount = DISTRICTS.GetLength(0);
        float angleStep = 360f / districtCount;

        for (int i = 0; i < districtCount; i++)
        {
            string dName    = DISTRICTS[i, 0];
            string dTagline = DISTRICTS[i, 1];
            string dScene   = DISTRICTS[i, 2];
            Color accent    = Hex(DISTRICTS[i, 3]);
            float h         = heights[i];

            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);

            GameObject root = new GameObject("District_" + dName.Replace(" ", ""));
            root.transform.SetParent(districtsRoot.transform);
            root.transform.position = pos;
            // Face the plaza
            root.transform.rotation = Quaternion.LookRotation(-new Vector3(pos.x, 0f, pos.z));

            Material glowMat = MakeMat("City_Glow_" + i, Hex("101018"), accent, 2f);

            // Main tower
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "Tower";
            tower.transform.SetParent(root.transform);
            tower.transform.localPosition = new Vector3(0f, h / 2f, 0f);
            tower.transform.localScale = new Vector3(4.5f, h, 4.5f);
            tower.GetComponent<MeshRenderer>().sharedMaterial = buildingMat;

            // Side block
            GameObject side = GameObject.CreatePrimitive(PrimitiveType.Cube);
            side.name = "SideBlock";
            side.transform.SetParent(root.transform);
            side.transform.localPosition = new Vector3(3.6f, h * 0.22f, 0.8f);
            side.transform.localScale = new Vector3(2.6f, h * 0.44f, 2.6f);
            side.GetComponent<MeshRenderer>().sharedMaterial = buildingMat;

            // Neon band around tower
            GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
            band.name = "GlowBand";
            band.transform.SetParent(root.transform);
            band.transform.localPosition = new Vector3(0f, h * 0.78f, 0f);
            band.transform.localScale = new Vector3(4.8f, 0.35f, 4.8f);
            band.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

            // Vertical neon edge
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = "GlowEdge";
            edge.transform.SetParent(root.transform);
            edge.transform.localPosition = new Vector3(2.35f, h * 0.45f, 2.35f);
            edge.transform.localScale = new Vector3(0.35f, h * 0.9f, 0.35f);
            edge.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

            // Glowing ground pad
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "GlowPad";
            pad.transform.SetParent(root.transform);
            pad.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            pad.transform.localScale = new Vector3(7f, 0.04f, 7f);
            pad.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

            // World-space label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(root.transform);
            labelObj.transform.position = pos + Vector3.up * (h + 2.2f);
            TextMeshPro label = labelObj.AddComponent<TextMeshPro>();
            label.text = dName.ToUpperInvariant();
            label.fontSize = 13f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = accent;
            label.rectTransform.sizeDelta = new Vector2(22f, 3f);

            // Component + wiring
            CityDistrict district = root.AddComponent<CityDistrict>();
            SerializedObject dso = new SerializedObject(district);
            dso.FindProperty("districtName").stringValue = dName;
            dso.FindProperty("tagline").stringValue = dTagline;
            dso.FindProperty("sceneToLoad").stringValue = dScene;
            dso.FindProperty("accent").colorValue = accent;
            dso.FindProperty("label").objectReferenceValue = labelObj.transform;
            SerializedProperty glows = dso.FindProperty("glowRenderers");
            Renderer[] glowRs =
            {
                band.GetComponent<MeshRenderer>(),
                edge.GetComponent<MeshRenderer>(),
                pad.GetComponent<MeshRenderer>()
            };
            glows.arraySize = glowRs.Length;
            for (int g = 0; g < glowRs.Length; g++)
                glows.GetArrayElementAtIndex(g).objectReferenceValue = glowRs[g];
            dso.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Background skyline (non-interactive) ─────────────────────────────
        GameObject skylineRoot = new GameObject("Skyline");
        skylineRoot.transform.SetParent(worldRoot.transform);
        System.Random rng = new System.Random(7);
        for (int i = 0; i < 18; i++)
        {
            float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float r = 34f + (float)rng.NextDouble() * 14f;
            float h = 7f + (float)rng.NextDouble() * 13f;
            float w = 3f + (float)rng.NextDouble() * 3f;

            GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            t.name = "SkyTower_" + i;
            t.transform.SetParent(skylineRoot.transform);
            t.transform.position = new Vector3(Mathf.Sin(ang) * r, h / 2f, Mathf.Cos(ang) * r);
            t.transform.localScale = new Vector3(w, h, w);
            t.GetComponent<MeshRenderer>().sharedMaterial = buildingMat;
            Object.DestroyImmediate(t.GetComponent<Collider>()); // not tappable

            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glow.name = "Glow";
            glow.transform.SetParent(t.transform);
            glow.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            glow.transform.localScale = new Vector3(1.05f, 0.03f, 1.05f);
            glow.GetComponent<MeshRenderer>().sharedMaterial = skylineMat;
            Object.DestroyImmediate(glow.GetComponent<Collider>());
        }

        // ── Post-processing (bloom = the neon look) ──────────────────────────
        GameObject volObj = new GameObject("PostFX");
        Volume vol = volObj.AddComponent<Volume>();
        vol.isGlobal = true;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.DeleteAsset("Assets/Settings/CityPostFX.asset");
        AssetDatabase.CreateAsset(profile, "Assets/Settings/CityPostFX.asset");
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 1.7f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.85f;
        Vignette vig = profile.Add<Vignette>(true);
        vig.intensity.overrideState = true;
        vig.intensity.value = 0.3f;
        vol.sharedProfile = profile;
        EditorUtility.SetDirty(profile);

        // ── UI overlay ───────────────────────────────────────────────────────
        GameObject canvasObj = new GameObject("CityUICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390, 844);
        scaler.matchWidthOrHeight = 0f;
        canvasObj.AddComponent<GraphicRaycaster>();
        RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();

        GameObject uiRoot = new GameObject("UIRoot");
        uiRoot.transform.SetParent(canvasRT, false);
        RectTransform uiRootRT = uiRoot.AddComponent<RectTransform>();
        Stretch(uiRootRT);
        uiRoot.AddComponent<SafeAreaPanel>();

        // Header
        MakeText("Txt_Logo", uiRootRT,
            new Vector2(0f, 1f), new Vector2(0.6f, 1f),
            new Vector2(16f, -44f), new Vector2(0f, -12f),
            "AUDIOVIDO", CYAN, 16f, FontStyles.Bold, TextAlignmentOptions.Left);

        GameObject pill = MakePanel("NxtPill", uiRootRT,
            new Vector2(1f, 1f), new Vector2(1f, 1f), Hex("1A1A24"), true);
        RectTransform pillRT = pill.GetComponent<RectTransform>();
        pillRT.pivot = new Vector2(1f, 1f);
        pillRT.anchoredPosition = new Vector2(-16f, -14f);
        pillRT.sizeDelta = new Vector2(84f, 28f);
        GameObject nxtText = MakeText("Txt_Nxt", pillRT,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "0 NXT", GOLD, 12f, FontStyles.Bold, TextAlignmentOptions.Center);

        // Hint
        GameObject hint = MakeText("Txt_Hint", uiRootRT,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(16f, 24f), new Vector2(-16f, 52f),
            "Drag to explore  ·  Tap a district", TEXT_DIM, 13f,
            FontStyles.Normal, TextAlignmentOptions.Center);

        // District info card (hidden until focus)
        GameObject card = MakePanel("InfoCard", uiRootRT,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Color(0.055f, 0.055f, 0.08f, 0.96f), true);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.pivot = new Vector2(0.5f, 0f);
        cardRT.offsetMin = new Vector2(12f, 16f);
        cardRT.offsetMax = new Vector2(-12f, 156f);

        GameObject nameText = MakeText("Txt_DistrictName", cardRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -42f), new Vector2(-16f, -10f),
            "District", Color.white, 20f, FontStyles.Bold, TextAlignmentOptions.Left);

        GameObject tagText = MakeText("Txt_Tagline", cardRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -66f), new Vector2(-16f, -42f),
            "Tagline", TEXT_DIM, 13f, FontStyles.Normal, TextAlignmentOptions.Left);

        GameObject backBtn = MakeButton("Btn_Back", cardRT,
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(16f, 14f), new Vector2(110f, 44f),
            "←  Back", TEXT_DIM, 14f, new Color(1f, 1f, 1f, 0.05f));

        GameObject enterBtn = MakeButton("Btn_Enter", cardRT,
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-16f, 14f), new Vector2(160f, 44f),
            "Enter  →", Hex("0A0A0F"), 15f, CYAN);
        RectTransform enterRT = enterBtn.GetComponent<RectTransform>();
        enterRT.pivot = new Vector2(1f, 0f);

        RectTransform backRT = backBtn.GetComponent<RectTransform>();
        backRT.pivot = new Vector2(0f, 0f);

        card.SetActive(false);

        // ── Fade canvas (own root so it survives UI hide) ────────────────────
        GameObject fadeRoot = new GameObject("FadeCanvasRoot");
        Canvas fadeCanvas = fadeRoot.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 100;
        GameObject fadeObj = new GameObject("Fade");
        fadeObj.transform.SetParent(fadeRoot.transform, false);
        RectTransform fadeRT = fadeObj.AddComponent<RectTransform>();
        Stretch(fadeRT);
        fadeObj.AddComponent<Image>().color = Color.black;
        CanvasGroup fadeCG = fadeObj.AddComponent<CanvasGroup>();
        fadeCG.alpha = 0f;
        fadeCG.blocksRaycasts = false;
        fadeObj.SetActive(false);

        // ── Managers ─────────────────────────────────────────────────────────
        GameObject loaderObj = new GameObject("SceneLoader");
        SceneLoader loader = loaderObj.AddComponent<SceneLoader>();
        SerializedObject lso = new SerializedObject(loader);
        lso.FindProperty("fadeCanvas").objectReferenceValue = fadeCG;
        lso.ApplyModifiedPropertiesWithoutUndo();

        GameObject nxtObj = new GameObject("NxtEarnManager");
        nxtObj.AddComponent<NxtEarnManager>();

        // Persistent music player with the sample tracks (placeholder catalog
        // until the content API is live)
        GameObject audioObj = new GameObject("AudioManager");
        AudioManager audioMgr = audioObj.AddComponent<AudioManager>();
        string[] trackFiles =
        {
            "city_ambient", "lounge_lofi", "arena_club", "home_chill", "cinema_score"
        };
        SerializedObject aso = new SerializedObject(audioMgr);
        SerializedProperty clipsProp = aso.FindProperty("clips");
        clipsProp.arraySize = trackFiles.Length;
        for (int i = 0; i < trackFiles.Length; i++)
            clipsProp.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{trackFiles[i]}.wav");
        aso.ApplyModifiedPropertiesWithoutUndo();

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── CityManager wiring ───────────────────────────────────────────────
        GameObject mgrObj = new GameObject("CityManager");
        CityManager mgr = mgrObj.AddComponent<CityManager>();
        SerializedObject mso = new SerializedObject(mgr);
        mso.FindProperty("cameraController").objectReferenceValue = camCtrl;
        mso.FindProperty("mainCamera").objectReferenceValue = cam;
        mso.FindProperty("worldRoot").objectReferenceValue = worldRoot;
        mso.FindProperty("uiRoot").objectReferenceValue = canvasObj;
        mso.FindProperty("nxtBalanceText").objectReferenceValue = nxtText.GetComponent<TextMeshProUGUI>();
        mso.FindProperty("hintText").objectReferenceValue = hint.GetComponent<TextMeshProUGUI>();
        mso.FindProperty("infoCard").objectReferenceValue = card;
        mso.FindProperty("districtNameText").objectReferenceValue = nameText.GetComponent<TextMeshProUGUI>();
        mso.FindProperty("taglineText").objectReferenceValue = tagText.GetComponent<TextMeshProUGUI>();
        mso.FindProperty("enterButton").objectReferenceValue = enterBtn.GetComponent<Button>();
        mso.FindProperty("enterLabel").objectReferenceValue =
            enterBtn.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        mso.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
        mso.FindProperty("fadeCanvas").objectReferenceValue = fadeCG;
        mso.ApplyModifiedPropertiesWithoutUndo();

        // ── Save + Build Settings ────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Scene_City.unity");

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Scene_City.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Home.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Lounge.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Cinema.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Arena.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Plaza.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true)
        };

        Debug.Log("[CitySceneBuilder] Scene_City built and saved. Build Settings updated. Press Play.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static Material MakeMat(string name, Color baseColor, Color? emission, float intensity)
    {
        string path = $"{MAT_DIR}/{name}.mat";
        AssetDatabase.DeleteAsset(path);
        Material m;
        if (emission.HasValue)
        {
            // Unlit + HDR base color = reliable glow (the _EMISSION keyword on
            // Lit materials does not survive asset serialization here).
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", emission.Value * intensity);
        }
        else
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", baseColor);
            m.SetFloat("_Smoothness", 0.45f);
        }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static GameObject MakePanel(string name, RectTransform parent,
        Vector2 aMin, Vector2 aMax, Color color, bool rounded)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        Image img = go.AddComponent<Image>();
        if (rounded)
        {
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
        }
        img.color = color;
        return go;
    }

    static GameObject MakeButton(string name, RectTransform parent,
        Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size,
        string label, Color textColor, float fontSize, Color bgColor)
    {
        GameObject go = MakePanel(name, parent, aMin, aMax, bgColor, true);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        GameObject lbl = MakeText("Label", rt, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, label, textColor, fontSize,
            FontStyles.Bold, TextAlignmentOptions.Center);
        return go;
    }

    static GameObject MakeText(string name, RectTransform parent,
        Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax,
        string text, Color color, float fontSize,
        FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false; // labels never eat button clicks (Lounge lesson)
        return go;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
