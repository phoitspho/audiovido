using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// AUDIOVIDO — Arena Scene Builder (SCR-18, spec §5.12)
/// Menu: AUDIOVIDO → Build Arena Scene
///
/// Generates Scene_Arena: the Club &amp; Dance Arena — stage with beat-driven
/// visualizer bars, PULSE bouncing on stage, a bobbing crowd, neon strips,
/// bloom post-FX, and the overlay UI (Exit / now playing / HYPE!).
/// Entered additively from the city; exits to Scene_City. Idempotent.
/// </summary>
public static class ArenaSceneBuilder
{
    static readonly Color BG_NIGHT = Hex("05040A");
    static readonly Color WALL     = new Color(0.05f, 0.04f, 0.08f);
    static readonly Color FLOOR    = new Color(0.045f, 0.04f, 0.07f);
    static readonly Color CROWD    = new Color(0.09f, 0.07f, 0.13f);
    static readonly Color PURPLE   = Hex("B44CFF");
    static readonly Color PINK     = Hex("FF2E92");
    static readonly Color YELLOW   = Hex("FFF340");
    static readonly Color TEXT_DIM = Hex("A0A0B8");

    const string MAT_DIR = "Assets/Materials/Arena";
    const int BAR_COUNT = 9;

    [MenuItem("AUDIOVIDO/Build Arena Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        if (!AssetDatabase.IsValidFolder(MAT_DIR))
            AssetDatabase.CreateFolder("Assets/Materials", "Arena");

        // ── Atmosphere ───────────────────────────────────────────────────────
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.02f, 0.012f, 0.03f);
        RenderSettings.fogDensity = 0.018f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.11f, 0.09f, 0.16f);

        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight != null)
        {
            dirLight.intensity = 0.15f;
            dirLight.color = new Color(0.7f, 0.55f, 1f);
            dirLight.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            dirLight.shadows = LightShadows.None;
        }

        GameObject stageLightObj = new GameObject("StageLight");
        Light stageLight = stageLightObj.AddComponent<Light>();
        stageLight.type = LightType.Point;
        stageLight.color = new Color(0.75f, 0.45f, 1f);
        stageLight.intensity = 1.6f;
        stageLight.range = 18f;
        stageLightObj.transform.position = new Vector3(0f, 6f, 6f);

        // ── Camera (crowd-rear view of the stage) ────────────────────────────
        Camera cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BG_NIGHT;
        cam.fieldOfView = 60f;
        cam.farClipPlane = 80f;
        cam.allowHDR = true;
        cam.transform.position = new Vector3(0f, 3.4f, -10.5f);
        cam.transform.rotation = Quaternion.Euler(-1.5f, 0f, 0f);
        var camData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        // ── Materials ────────────────────────────────────────────────────────
        Material floorMat  = MakeMat("Arena_Floor", FLOOR, null, 0f);
        Material wallMat   = MakeMat("Arena_Wall", WALL, null, 0f);
        Material crowdMat  = MakeMat("Arena_Crowd", CROWD, null, 0f);
        Material stageMat  = MakeMat("Arena_Stage", new Color(0.03f, 0.03f, 0.05f), null, 0f);
        Material barMat    = MakeMat("Arena_Bar", Color.black, PURPLE, 2.2f);
        Material pinkMat   = MakeMat("Arena_Pink", Color.black, PINK, 2f);
        Material yellowMat = MakeMat("Arena_Yellow", Color.black, YELLOW, 2f);
        Material bodyMat   = MakeMat("Arena_PulseBody", new Color(0.08f, 0.08f, 0.11f), null, 0f);

        // ── Room ─────────────────────────────────────────────────────────────
        GameObject room = new GameObject("Arena_Structure");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(room.transform);
        floor.transform.localScale = new Vector3(3.2f, 1f, 3.2f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

        MakeBox(room, "Wall_Back",  new Vector3(0f, 6f, 13f),  new Vector3(32f, 12f, 0.4f), wallMat);
        MakeBox(room, "Wall_Rear",  new Vector3(0f, 6f, -14f), new Vector3(32f, 12f, 0.4f), wallMat);
        MakeBox(room, "Wall_Left",  new Vector3(-13f, 6f, 0f), new Vector3(0.4f, 12f, 28f), wallMat);
        MakeBox(room, "Wall_Right", new Vector3(13f, 6f, 0f),  new Vector3(0.4f, 12f, 28f), wallMat);

        // ── Stage ────────────────────────────────────────────────────────────
        MakeBox(room, "Stage", new Vector3(0f, 0.5f, 8.5f), new Vector3(18f, 1f, 6f), stageMat);
        MakeBox(room, "StageEdgeStrip", new Vector3(0f, 1.02f, 5.55f), new Vector3(18f, 0.1f, 0.14f), pinkMat);
        MakeBox(room, "StageBackdrop", new Vector3(0f, 5f, 11.4f), new Vector3(18f, 8f, 0.3f), stageMat);

        // ── Visualizer bars (driven by ArenaManager) ─────────────────────────
        GameObject barsRoot = new GameObject("VisualizerBars");
        barsRoot.transform.SetParent(room.transform);
        Transform[] barTs = new Transform[BAR_COUNT];
        Renderer[] barRs = new Renderer[BAR_COUNT];
        for (int i = 0; i < BAR_COUNT; i++)
        {
            float x = -6f + i * 1.5f;
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Bar_" + i;
            bar.transform.SetParent(barsRoot.transform);
            bar.transform.localPosition = new Vector3(x, 1.5f, 11.1f);
            bar.transform.localScale = new Vector3(1.1f, 1f, 0.2f);
            bar.GetComponent<MeshRenderer>().sharedMaterial = barMat;
            Object.DestroyImmediate(bar.GetComponent<Collider>());
            barTs[i] = bar.transform;
            barRs[i] = bar.GetComponent<MeshRenderer>();
        }

        // ── PULSE (stage front) — humanoid proxy, arms raised (art pass) ─────
        CharacterProxyBuilder.Proxy pulseProxy = CharacterProxyBuilder.Build(
            "PULSE_Character", new Vector3(-4.5f, 1f, 6.8f), 1.0f,
            bodyMat, yellowMat, armsRaised: true);
        pulseProxy.root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // Signature LED mohawk on top of the proxy head
        GameObject mohawk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mohawk.name = "GlowMohawk";
        mohawk.transform.SetParent(pulseProxy.root.transform);
        mohawk.transform.localPosition = new Vector3(0f, 1.95f, 0f);
        mohawk.transform.localScale = new Vector3(0.08f, 0.28f, 0.4f);
        mohawk.GetComponent<MeshRenderer>().sharedMaterial = yellowMat;

        PulsePresence pulsePresence = pulseProxy.root.AddComponent<PulsePresence>();
        SerializedObject pso = new SerializedObject(pulsePresence);
        SerializedProperty pGlows = pso.FindProperty("glowRenderers");
        Renderer[] pulseGlowRs = new Renderer[pulseProxy.glowRenderers.Length + 1];
        pulseProxy.glowRenderers.CopyTo(pulseGlowRs, 0);
        pulseGlowRs[pulseGlowRs.Length - 1] = mohawk.GetComponent<MeshRenderer>();
        pGlows.arraySize = pulseGlowRs.Length;
        for (int g = 0; g < pulseGlowRs.Length; g++)
            pGlows.GetArrayElementAtIndex(g).objectReferenceValue = pulseGlowRs[g];
        pso.ApplyModifiedPropertiesWithoutUndo();

        // ── Crowd (bobbed by ArenaManager) ───────────────────────────────────
        GameObject crowdRoot = new GameObject("Crowd");
        crowdRoot.transform.SetParent(room.transform);
        System.Random rng = new System.Random(11);
        Transform[] crowdTs = new Transform[20];
        int c = 0;
        for (int row = 0; row < 4; row++)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = -6f + i * 3f + (float)(rng.NextDouble() - 0.5) * 1.4f;
                float z = 2.5f - row * 2.4f + (float)(rng.NextDouble() - 0.5) * 1f;
                GameObject member = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                member.name = $"Crowd_{row}_{i}";
                member.transform.SetParent(crowdRoot.transform);
                member.transform.localPosition = new Vector3(x, 1f, z);
                member.transform.localScale = new Vector3(0.55f, 0.95f, 0.55f);
                member.GetComponent<MeshRenderer>().sharedMaterial = crowdMat;
                Object.DestroyImmediate(member.GetComponent<Collider>());
                crowdTs[c++] = member.transform;
            }
        }

        // Side neon strips along the walls
        MakeBox(room, "Neon_L", new Vector3(-12.7f, 4.5f, 0f), new Vector3(0.12f, 0.12f, 26f), pinkMat);
        MakeBox(room, "Neon_R", new Vector3(12.7f, 4.5f, 0f), new Vector3(0.12f, 0.12f, 26f), pinkMat);

        // ── Post-FX ──────────────────────────────────────────────────────────
        GameObject volObj = new GameObject("PostFX");
        Volume vol = volObj.AddComponent<Volume>();
        vol.isGlobal = true;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.DeleteAsset("Assets/Settings/ArenaPostFX.asset");
        AssetDatabase.CreateAsset(profile, "Assets/Settings/ArenaPostFX.asset");
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 1.9f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.8f;
        Vignette vig = profile.Add<Vignette>(true);
        vig.intensity.overrideState = true;
        vig.intensity.value = 0.32f;
        vol.sharedProfile = profile;
        EditorUtility.SetDirty(profile);

        // ── UI overlay ───────────────────────────────────────────────────────
        GameObject canvasObj = new GameObject("ArenaUICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(844, 390);
        scaler.matchWidthOrHeight = 1f;
        canvasObj.AddComponent<GraphicRaycaster>();
        RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();

        GameObject uiRoot = new GameObject("UIRoot");
        uiRoot.transform.SetParent(canvasRT, false);
        RectTransform uiRootRT = uiRoot.AddComponent<RectTransform>();
        Stretch(uiRootRT);
        uiRoot.AddComponent<SafeAreaPanel>();

        // Top bar
        GameObject topBar = MakePanel("TopBar", uiRootRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Color(0.02f, 0.02f, 0.03f, 0.85f), false);
        RectTransform topRT = topBar.GetComponent<RectTransform>();
        topRT.offsetMin = new Vector2(0f, -52f);
        topRT.offsetMax = new Vector2(0f, 0f);

        GameObject exitBtn = MakeButton("Btn_Exit", topRT,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(10f, 0f), new Vector2(72f, 36f),
            "Exit", Color.white, 13f, new Color(1f, 1f, 1f, 0.07f));
        exitBtn.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

        GameObject titleObj = MakeText("Txt_Title", topRT,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "AUDIOVIDO ARENA", PURPLE, 14f, FontStyles.Bold, TextAlignmentOptions.Center);

        // Bottom bar
        GameObject bottomBar = MakePanel("BottomBar", uiRootRT,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Color(0.02f, 0.02f, 0.03f, 0.9f), false);
        RectTransform botRT = bottomBar.GetComponent<RectTransform>();
        botRT.offsetMin = new Vector2(0f, 0f);
        botRT.offsetMax = new Vector2(0f, 64f);

        GameObject nowPlaying = MakeText("Txt_NowPlaying", botRT,
            new Vector2(0f, 0f), new Vector2(0.62f, 1f),
            new Vector2(16f, 0f), new Vector2(0f, 0f),
            ">> Neon Nights — Live Set", PINK, 12f, FontStyles.Bold, TextAlignmentOptions.Left);

        GameObject hypeBtn = MakeButton("Btn_Hype", botRT,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-14f, 0f), new Vector2(110f, 40f),
            "HYPE!", Hex("0A0A0F"), 15f, PINK);
        hypeBtn.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);

        // PULSE bubble
        GameObject bubble = MakePanel("PulseBubble", uiRootRT,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Color(0.09f, 0.08f, 0.02f, 0.96f), true);
        RectTransform bubRT = bubble.GetComponent<RectTransform>();
        bubRT.pivot = new Vector2(0.5f, 0f);
        bubRT.anchoredPosition = new Vector2(0f, 76f);
        bubRT.sizeDelta = new Vector2(320f, 66f);
        MakeText("Txt_PulseName", bubRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(14f, -24f), new Vector2(-14f, -4f),
            "PULSE", YELLOW, 11f, FontStyles.Bold, TextAlignmentOptions.Left);
        GameObject pulseMsg = MakeText("Txt_PulseMessage", bubRT,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(14f, 6f), new Vector2(-14f, -26f),
            "3...", Color.white, 14f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        bubble.SetActive(false);

        // Fade canvas
        GameObject fadeObj = new GameObject("FadeCanvas");
        fadeObj.transform.SetParent(canvasRT, false);
        RectTransform fadeRT = fadeObj.AddComponent<RectTransform>();
        Stretch(fadeRT);
        fadeObj.AddComponent<Image>().color = Color.black;
        CanvasGroup fadeCG = fadeObj.AddComponent<CanvasGroup>();
        fadeCG.alpha = 0f;
        fadeCG.blocksRaycasts = false;
        fadeObj.SetActive(false);

        // ── Managers ─────────────────────────────────────────────────────────
        GameObject nxtObj = new GameObject("NxtEarnManager");
        nxtObj.AddComponent<NxtEarnManager>();

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // UI controller wiring
        ArenaUIController ui = canvasObj.AddComponent<ArenaUIController>();
        SerializedObject uso = new SerializedObject(ui);
        uso.FindProperty("exitButton").objectReferenceValue = exitBtn.GetComponent<Button>();
        uso.FindProperty("titleText").objectReferenceValue = titleObj.GetComponent<TextMeshProUGUI>();
        uso.FindProperty("nowPlayingText").objectReferenceValue = nowPlaying.GetComponent<TextMeshProUGUI>();
        uso.FindProperty("hypeButton").objectReferenceValue = hypeBtn.GetComponent<Button>();
        uso.FindProperty("pulseBubbleRoot").objectReferenceValue = bubble;
        uso.FindProperty("pulseMessageText").objectReferenceValue = pulseMsg.GetComponent<TextMeshProUGUI>();
        uso.ApplyModifiedPropertiesWithoutUndo();

        // Arena manager wiring
        GameObject mgrObj = new GameObject("ArenaManager");
        ArenaManager mgr = mgrObj.AddComponent<ArenaManager>();
        SerializedObject mso = new SerializedObject(mgr);
        mso.FindProperty("ui").objectReferenceValue = ui;
        mso.FindProperty("fadeCanvas").objectReferenceValue = fadeCG;

        SerializedProperty barsProp = mso.FindProperty("visualizerBars");
        barsProp.arraySize = BAR_COUNT;
        for (int i = 0; i < BAR_COUNT; i++)
            barsProp.GetArrayElementAtIndex(i).objectReferenceValue = barTs[i];

        SerializedProperty barRsProp = mso.FindProperty("visualizerRenderers");
        barRsProp.arraySize = BAR_COUNT;
        for (int i = 0; i < BAR_COUNT; i++)
            barRsProp.GetArrayElementAtIndex(i).objectReferenceValue = barRs[i];

        SerializedProperty crowdProp = mso.FindProperty("crowdMembers");
        crowdProp.arraySize = crowdTs.Length;
        for (int i = 0; i < crowdTs.Length; i++)
            crowdProp.GetArrayElementAtIndex(i).objectReferenceValue = crowdTs[i];

        mso.ApplyModifiedPropertiesWithoutUndo();

        // ── Save + Build Settings ────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Scene_Arena.unity");

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Scene_City.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Lounge.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Cinema.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Arena.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true)
        };

        Debug.Log("[ArenaSceneBuilder] Scene_Arena built and saved. Build Settings updated.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static GameObject MakeBox(GameObject parent, string name,
        Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static Material MakeMat(string name, Color baseColor, Color? emission, float intensity)
    {
        string path = $"{MAT_DIR}/{name}.mat";
        AssetDatabase.DeleteAsset(path);
        Material m;
        if (emission.HasValue)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", emission.Value * intensity);
        }
        else
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", baseColor);
            m.SetFloat("_Smoothness", 0.4f);
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
        MakeText("Label", rt, Vector2.zero, Vector2.one,
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
        tmp.raycastTarget = false;
        return go;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
