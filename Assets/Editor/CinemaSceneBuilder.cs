using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// AUDIOVIDO — Cinema Scene Builder (SCR-17, spec §5.11)
/// Menu: AUDIOVIDO → Build Cinema Scene
///
/// Generates Scene_Cinema: dark theater with a glowing projection screen,
/// seat rows, aisle lighting, NOVA (gold host presence), bloom post-FX,
/// and the 2D overlay (Exit / now showing / play-pause / NOVA bubble).
/// Entered additively from the city's Cinema District; exits to Scene_City.
/// Idempotent — regenerates the scene each run.
/// </summary>
public static class CinemaSceneBuilder
{
    static readonly Color BG_NIGHT  = Hex("040407");
    static readonly Color WALL      = new Color(0.055f, 0.03f, 0.045f);
    static readonly Color FLOOR     = new Color(0.05f, 0.045f, 0.06f);
    static readonly Color SEAT      = new Color(0.16f, 0.05f, 0.08f);
    static readonly Color GOLD      = Hex("FFD766");
    static readonly Color CORAL     = Hex("FF4757");
    static readonly Color TEXT_DIM  = Hex("A0A0B8");

    const string MAT_DIR = "Assets/Materials/Cinema";

    [MenuItem("AUDIOVIDO/Build Cinema Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        if (!AssetDatabase.IsValidFolder(MAT_DIR))
            AssetDatabase.CreateFolder("Assets/Materials", "Cinema");

        // ── Atmosphere ───────────────────────────────────────────────────────
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.015f, 0.012f, 0.02f);
        RenderSettings.fogDensity = 0.02f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.10f, 0.09f, 0.13f);

        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight != null)
        {
            dirLight.intensity = 0.12f;
            dirLight.color = new Color(0.6f, 0.6f, 0.9f);
            dirLight.transform.rotation = Quaternion.Euler(60f, -20f, 0f);
            dirLight.shadows = LightShadows.None;
        }

        // Warm screen spill light onto the seats
        GameObject spill = new GameObject("ScreenSpillLight");
        Light spillLight = spill.AddComponent<Light>();
        spillLight.type = LightType.Point;
        spillLight.color = new Color(0.85f, 0.85f, 1f);
        spillLight.intensity = 1.4f;
        spillLight.range = 16f;
        spill.transform.position = new Vector3(0f, 4f, 6.5f);

        // ── Camera (fixed seated view) ───────────────────────────────────────
        Camera cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BG_NIGHT;
        cam.fieldOfView = 58f;
        cam.farClipPlane = 80f;
        cam.allowHDR = true;
        cam.transform.position = new Vector3(0f, 3.1f, -7f);
        cam.transform.rotation = Quaternion.Euler(-2.5f, 0f, 0f);
        var camData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        // ── Materials ────────────────────────────────────────────────────────
        Material floorMat  = MakeMat("Cinema_Floor", FLOOR, null, 0f);
        Material wallMat   = MakeMat("Cinema_Wall", WALL, null, 0f);
        Material seatMat   = MakeMat("Cinema_Seat", SEAT, null, 0f);
        Material screenMat = MakeMat("Cinema_Screen", Color.black, new Color(0.7f, 0.78f, 1f), 1.8f);
        Material frameMat  = MakeMat("Cinema_Frame", new Color(0.03f, 0.03f, 0.04f), null, 0f);
        Material goldMat   = MakeMat("Cinema_Gold", new Color(0.12f, 0.09f, 0.03f), GOLD, 1.8f);
        Material bodyMat   = MakeMat("Cinema_NovaBody", new Color(0.09f, 0.07f, 0.1f), null, 0f);

        // ── Room ─────────────────────────────────────────────────────────────
        GameObject room = new GameObject("Cinema_Structure");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(room.transform);
        floor.transform.localScale = new Vector3(3f, 1f, 3f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

        MakeBox(room, "Wall_Back",  new Vector3(0f, 5f, -12f), new Vector3(30f, 10f, 0.4f), wallMat);
        MakeBox(room, "Wall_Front", new Vector3(0f, 5f, 12f),  new Vector3(30f, 10f, 0.4f), wallMat);
        MakeBox(room, "Wall_Left",  new Vector3(-11f, 5f, 0f), new Vector3(0.4f, 10f, 24f), wallMat);
        MakeBox(room, "Wall_Right", new Vector3(11f, 5f, 0f),  new Vector3(0.4f, 10f, 24f), wallMat);
        MakeBox(room, "Ceiling",    new Vector3(0f, 10f, 0f),  new Vector3(30f, 0.4f, 24f), wallMat);

        // ── Screen + stage ───────────────────────────────────────────────────
        GameObject frame = MakeBox(room, "ScreenFrame",
            new Vector3(0f, 4.6f, 11.5f), new Vector3(14.6f, 6.6f, 0.25f), frameMat);
        GameObject screen = MakeBox(room, "Screen",
            new Vector3(0f, 4.6f, 11.3f), new Vector3(13.6f, 5.7f, 0.15f), screenMat);
        MakeBox(room, "Stage", new Vector3(0f, 0.25f, 9.5f), new Vector3(16f, 0.5f, 3.5f), frameMat);
        MakeBox(room, "StageStrip", new Vector3(0f, 0.52f, 7.8f), new Vector3(16f, 0.08f, 0.12f), goldMat);

        // ── Seats (3 rows × 5) ───────────────────────────────────────────────
        GameObject seats = new GameObject("Seats");
        seats.transform.SetParent(room.transform);
        for (int row = 0; row < 3; row++)
        {
            float z = 1.5f - row * 2.6f;
            float y = 0.25f;
            for (int i = 0; i < 5; i++)
            {
                float x = -6f + i * 3f;
                MakeBox(seats, $"Seat_{row}_{i}",
                    new Vector3(x, y, z), new Vector3(1.6f, 0.5f, 1.3f), seatMat);
                MakeBox(seats, $"SeatBack_{row}_{i}",
                    new Vector3(x, y + 0.65f, z - 0.6f), new Vector3(1.6f, 1.0f, 0.25f), seatMat);
            }
        }

        // Aisle glow strips
        MakeBox(room, "Aisle_L", new Vector3(-8.2f, 0.03f, -1f), new Vector3(0.15f, 0.05f, 14f), goldMat);
        MakeBox(room, "Aisle_R", new Vector3(8.2f, 0.03f, -1f), new Vector3(0.15f, 0.05f, 14f), goldMat);

        // ── NOVA (host presence, stage left) — humanoid proxy (art pass) ─────
        CharacterProxyBuilder.Proxy novaProxy = CharacterProxyBuilder.Build(
            "NOVA_Character", new Vector3(-6f, 0f, 8.6f), 1.05f, bodyMat, goldMat);
        // Face the audience
        novaProxy.root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        NovaPresence novaPresence = novaProxy.root.AddComponent<NovaPresence>();
        SerializedObject nso = new SerializedObject(novaPresence);
        SerializedProperty glows = nso.FindProperty("glowRenderers");
        glows.arraySize = novaProxy.glowRenderers.Length;
        for (int g = 0; g < novaProxy.glowRenderers.Length; g++)
            glows.GetArrayElementAtIndex(g).objectReferenceValue = novaProxy.glowRenderers[g];
        nso.ApplyModifiedPropertiesWithoutUndo();

        // ── Post-FX ──────────────────────────────────────────────────────────
        GameObject volObj = new GameObject("PostFX");
        Volume vol = volObj.AddComponent<Volume>();
        vol.isGlobal = true;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.DeleteAsset("Assets/Settings/CinemaPostFX.asset");
        AssetDatabase.CreateAsset(profile, "Assets/Settings/CinemaPostFX.asset");
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 1.5f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.9f;
        Vignette vig = profile.Add<Vignette>(true);
        vig.intensity.overrideState = true;
        vig.intensity.value = 0.35f;
        vol.sharedProfile = profile;
        EditorUtility.SetDirty(profile);

        // ── UI overlay ───────────────────────────────────────────────────────
        GameObject canvasObj = new GameObject("CinemaUICanvas");
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
            "AUDIOVIDO CINEMA", GOLD, 14f, FontStyles.Bold, TextAlignmentOptions.Center);

        // Bottom bar
        GameObject bottomBar = MakePanel("BottomBar", uiRootRT,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Color(0.02f, 0.02f, 0.03f, 0.9f), false);
        RectTransform botRT = bottomBar.GetComponent<RectTransform>();
        botRT.offsetMin = new Vector2(0f, 0f);
        botRT.offsetMax = new Vector2(0f, 64f);

        GameObject nowShowing = MakeText("Txt_NowShowing", botRT,
            new Vector2(0f, 0f), new Vector2(0.65f, 1f),
            new Vector2(16f, 0f), new Vector2(0f, 0f),
            ">> Beyond The Horizon", CORAL, 13f, FontStyles.Bold, TextAlignmentOptions.Left);

        GameObject playBtn = MakeButton("Btn_PlayPause", botRT,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-14f, 0f), new Vector2(96f, 40f),
            "Pause", Hex("0A0A0F"), 14f, GOLD);
        playBtn.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);

        // NOVA bubble
        GameObject bubble = MakePanel("NovaBubble", uiRootRT,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Color(0.08f, 0.07f, 0.05f, 0.96f), true);
        RectTransform bubRT = bubble.GetComponent<RectTransform>();
        bubRT.pivot = new Vector2(0.5f, 0f);
        bubRT.anchoredPosition = new Vector2(0f, 76f);
        bubRT.sizeDelta = new Vector2(320f, 66f);
        MakeText("Txt_NovaName", bubRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(14f, -24f), new Vector2(-14f, -4f),
            "NOVA", GOLD, 11f, FontStyles.Bold, TextAlignmentOptions.Left);
        GameObject novaMsg = MakeText("Txt_NovaMessage", bubRT,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(14f, 6f), new Vector2(-14f, -26f),
            "Welcome to your cinema...", Color.white, 13f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        bubble.SetActive(false);

        // Fade canvas (inside overlay canvas, above everything)
        GameObject fadeObj = new GameObject("FadeCanvas");
        fadeObj.transform.SetParent(canvasRT, false);
        RectTransform fadeRT = fadeObj.AddComponent<RectTransform>();
        Stretch(fadeRT);
        Image fadeImg = fadeObj.AddComponent<Image>();
        fadeImg.color = Color.black;
        CanvasGroup fadeCG = fadeObj.AddComponent<CanvasGroup>();
        fadeCG.alpha = 0f;
        fadeCG.blocksRaycasts = false;
        fadeObj.SetActive(false);

        // ── Managers ─────────────────────────────────────────────────────────
        GameObject nxtObj = new GameObject("NxtEarnManager");
        nxtObj.AddComponent<NxtEarnManager>(); // self-deduplicates when additive

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // UI controller + wiring
        CinemaUIController ui = canvasObj.AddComponent<CinemaUIController>();
        SerializedObject uso = new SerializedObject(ui);
        uso.FindProperty("exitButton").objectReferenceValue = exitBtn.GetComponent<Button>();
        uso.FindProperty("titleText").objectReferenceValue = titleObj.GetComponent<TextMeshProUGUI>();
        uso.FindProperty("nowShowingText").objectReferenceValue = nowShowing.GetComponent<TextMeshProUGUI>();
        uso.FindProperty("playPauseButton").objectReferenceValue = playBtn.GetComponent<Button>();
        uso.FindProperty("playPauseLabel").objectReferenceValue =
            playBtn.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        uso.FindProperty("novaBubbleRoot").objectReferenceValue = bubble;
        uso.FindProperty("novaMessageText").objectReferenceValue = novaMsg.GetComponent<TextMeshProUGUI>();
        uso.ApplyModifiedPropertiesWithoutUndo();

        // Cinema manager + wiring
        GameObject mgrObj = new GameObject("CinemaManager");
        CinemaManager mgr = mgrObj.AddComponent<CinemaManager>();
        SerializedObject mso = new SerializedObject(mgr);
        mso.FindProperty("ui").objectReferenceValue = ui;
        mso.FindProperty("screenRenderer").objectReferenceValue = screen.GetComponent<MeshRenderer>();
        mso.FindProperty("fadeCanvas").objectReferenceValue = fadeCG;
        mso.ApplyModifiedPropertiesWithoutUndo();

        // ── Save + Build Settings ────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Scene_Cinema.unity");

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Scene_City.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Lounge.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Cinema.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true)
        };

        Debug.Log("[CinemaSceneBuilder] Scene_Cinema built and saved. Build Settings updated.");
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
            // Unlit + HDR base color = reliable glow (the _EMISSION keyword on
            // Lit materials does not survive asset serialization here).
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
