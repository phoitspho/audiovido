using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// AUDIOVIDO — Home Scene Builder ("Your Room" concept mockup)
/// Menu: AUDIOVIDO → Build Home Scene
///
/// Generates Scene_Home: the user's personal room — city-view window with
/// glowing skyline, couch, table with spinning record player, lava lamp,
/// themeable neon accents, warm lighting, bloom. UI: Exit / level / now
/// playing / Music / Theme. Entered from Home District; exits to Scene_City.
/// </summary>
public static class HomeSceneBuilder
{
    static readonly Color BG_NIGHT  = Hex("06060C");
    static readonly Color WALL      = new Color(0.08f, 0.065f, 0.075f);
    static readonly Color FLOOR     = new Color(0.10f, 0.07f, 0.05f);  // warm wood
    static readonly Color COUCH     = new Color(0.13f, 0.10f, 0.20f);
    static readonly Color FURNITURE = new Color(0.07f, 0.06f, 0.08f);
    static readonly Color CYAN      = Hex("00D4FF");
    static readonly Color PINK      = Hex("FF2E92");
    static readonly Color GOLD      = Hex("FFD766");
    static readonly Color TEXT_DIM  = Hex("A0A0B8");

    const string MAT_DIR = "Assets/Materials/Home";

    [MenuItem("AUDIOVIDO/Build Home Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        if (!AssetDatabase.IsValidFolder(MAT_DIR))
            AssetDatabase.CreateFolder("Assets/Materials", "Home");

        // ── Atmosphere (cozy, warm) ──────────────────────────────────────────
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.13f, 0.15f);

        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight != null)
        {
            dirLight.intensity = 0.2f;
            dirLight.color = new Color(0.7f, 0.65f, 0.9f);
            dirLight.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
            dirLight.shadows = LightShadows.None;
        }

        GameObject warmObj = new GameObject("WarmLight");
        Light warm = warmObj.AddComponent<Light>();
        warm.type = LightType.Point;
        warm.color = new Color(1f, 0.75f, 0.5f);
        warm.intensity = 1.5f;
        warm.range = 12f;
        warmObj.transform.position = new Vector3(0f, 3.4f, 0f);

        // ── Camera (cozy corner view toward the window) ──────────────────────
        Camera cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BG_NIGHT;
        cam.fieldOfView = 62f;
        cam.farClipPlane = 60f;
        cam.allowHDR = true;
        cam.transform.position = new Vector3(0f, 2.5f, -4.8f);
        cam.transform.rotation = Quaternion.Euler(4f, 0f, 0f);
        var camData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        // ── Materials ────────────────────────────────────────────────────────
        Material floorMat   = MakeMat("Home_Floor", FLOOR, null, 0f);
        Material wallMat    = MakeMat("Home_Wall", WALL, null, 0f);
        Material couchMat   = MakeMat("Home_Couch", COUCH, null, 0f);
        Material darkMat    = MakeMat("Home_Furniture", FURNITURE, null, 0f);
        Material skyMat     = MakeMat("Home_SkyBackdrop", Color.black, new Color(0.05f, 0.07f, 0.16f), 1f);
        Material towerAMat  = MakeMat("Home_TowerA", Color.black, CYAN, 1.3f);
        Material towerBMat  = MakeMat("Home_TowerB", Color.black, PINK, 1.2f);
        Material neonMat    = MakeMat("Home_Neon", Color.black, CYAN, 1.8f);     // themed at runtime
        Material lavaMat    = MakeMat("Home_Lava", Color.black, new Color(1f, 0.45f, 0.15f), 1.6f);
        Material discMat    = MakeMat("Home_Disc", new Color(0.03f, 0.03f, 0.035f), null, 0f);
        Material plantMat   = MakeMat("Home_Plant", new Color(0.06f, 0.14f, 0.07f), null, 0f);

        // ── Room shell ───────────────────────────────────────────────────────
        GameObject room = new GameObject("Room_Structure");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(room.transform);
        floor.transform.localScale = new Vector3(1.4f, 1f, 1.4f); // 14×14
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

        MakeBox(room, "Ceiling", new Vector3(0f, 5f, 0f), new Vector3(14f, 0.3f, 14f), wallMat);
        MakeBox(room, "Wall_Left", new Vector3(-6f, 2.5f, 0f), new Vector3(0.3f, 5f, 14f), wallMat);
        MakeBox(room, "Wall_Right", new Vector3(6f, 2.5f, 0f), new Vector3(0.3f, 5f, 14f), wallMat);
        MakeBox(room, "Wall_Rear", new Vector3(0f, 2.5f, -6f), new Vector3(14f, 5f, 0.3f), wallMat);

        // Back wall with window opening (window: 7 wide × 2.6 tall, sill 1.2)
        MakeBox(room, "WallB_Left",   new Vector3(-4.75f, 2.5f, 6f), new Vector3(2.5f, 5f, 0.3f), wallMat);
        MakeBox(room, "WallB_Right",  new Vector3(4.75f, 2.5f, 6f),  new Vector3(2.5f, 5f, 0.3f), wallMat);
        MakeBox(room, "WallB_Top",    new Vector3(0f, 4.4f, 6f),     new Vector3(7f, 1.2f, 0.3f), wallMat);
        MakeBox(room, "WallB_Bottom", new Vector3(0f, 0.6f, 6f),     new Vector3(7f, 1.2f, 0.3f), wallMat);

        // City view outside the window
        MakeBox(room, "CityBackdrop", new Vector3(0f, 3f, 9.5f), new Vector3(16f, 7f, 0.2f), skyMat);
        MakeBox(room, "SkyTower_1", new Vector3(-2.6f, 2.2f, 8.6f), new Vector3(0.5f, 3.4f, 0.5f), towerAMat);
        MakeBox(room, "SkyTower_2", new Vector3(-0.9f, 1.8f, 9.0f), new Vector3(0.4f, 2.6f, 0.4f), towerBMat);
        MakeBox(room, "SkyTower_3", new Vector3(1.1f, 2.5f, 8.8f), new Vector3(0.55f, 3.9f, 0.55f), towerAMat);
        MakeBox(room, "SkyTower_4", new Vector3(2.9f, 1.9f, 8.5f), new Vector3(0.4f, 2.8f, 0.4f), towerBMat);

        // ── Furniture ────────────────────────────────────────────────────────
        // Couch (left side, facing window)
        MakeBox(room, "Couch_Seat", new Vector3(-2.6f, 0.35f, 1.6f), new Vector3(3f, 0.7f, 1.3f), couchMat);
        MakeBox(room, "Couch_Back", new Vector3(-2.6f, 0.95f, 0.85f), new Vector3(3f, 0.9f, 0.3f), couchMat);
        MakeBox(room, "Couch_ArmL", new Vector3(-4.25f, 0.75f, 1.6f), new Vector3(0.3f, 0.8f, 1.3f), couchMat);
        MakeBox(room, "Couch_ArmR", new Vector3(-0.95f, 0.75f, 1.6f), new Vector3(0.3f, 0.8f, 1.3f), couchMat);

        // Low table (center)
        MakeBox(room, "Table", new Vector3(0.6f, 0.35f, 3.2f), new Vector3(1.6f, 0.12f, 1f), darkMat);
        MakeBox(room, "TableLeg1", new Vector3(0f, 0.15f, 2.85f), new Vector3(0.1f, 0.3f, 0.1f), darkMat);
        MakeBox(room, "TableLeg2", new Vector3(1.2f, 0.15f, 2.85f), new Vector3(0.1f, 0.3f, 0.1f), darkMat);
        MakeBox(room, "TableLeg3", new Vector3(0f, 0.15f, 3.55f), new Vector3(0.1f, 0.3f, 0.1f), darkMat);
        MakeBox(room, "TableLeg4", new Vector3(1.2f, 0.15f, 3.55f), new Vector3(0.1f, 0.3f, 0.1f), darkMat);

        // Record player on the table
        GameObject playerBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        playerBase.name = "RecordPlayer_Base";
        playerBase.transform.SetParent(room.transform);
        playerBase.transform.position = new Vector3(0.6f, 0.46f, 3.2f);
        playerBase.transform.localScale = new Vector3(0.7f, 0.05f, 0.7f);
        playerBase.GetComponent<MeshRenderer>().sharedMaterial = darkMat;

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "RecordDisc";
        disc.transform.SetParent(room.transform);
        disc.transform.position = new Vector3(0.6f, 0.53f, 3.2f);
        disc.transform.localScale = new Vector3(0.55f, 0.015f, 0.55f);
        disc.GetComponent<MeshRenderer>().sharedMaterial = discMat;

        GameObject discDot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        discDot.name = "DiscLabel";
        discDot.transform.SetParent(disc.transform);
        discDot.transform.localPosition = new Vector3(0.28f, 0.6f, 0f); // off-center so spin reads
        discDot.transform.localScale = new Vector3(0.14f, 0.6f, 0.14f);
        discDot.GetComponent<MeshRenderer>().sharedMaterial = MakeMat("Home_DiscDot", Color.black, GOLD, 1.2f);

        // Shelf + lava lamp (right side)
        MakeBox(room, "Shelf", new Vector3(4.2f, 1.0f, 3.6f), new Vector3(1.4f, 0.1f, 0.9f), darkMat);
        MakeBox(room, "ShelfLeg", new Vector3(4.2f, 0.5f, 3.6f), new Vector3(0.12f, 1f, 0.12f), darkMat);
        GameObject lava = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        lava.name = "LavaLamp";
        lava.transform.SetParent(room.transform);
        lava.transform.position = new Vector3(4.2f, 1.35f, 3.6f);
        lava.transform.localScale = new Vector3(0.22f, 0.3f, 0.22f);
        lava.GetComponent<MeshRenderer>().sharedMaterial = lavaMat;

        // Plant (front-left corner)
        GameObject pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pot.name = "PlantPot";
        pot.transform.SetParent(room.transform);
        pot.transform.position = new Vector3(-4.9f, 0.25f, 4.6f);
        pot.transform.localScale = new Vector3(0.5f, 0.25f, 0.5f);
        pot.GetComponent<MeshRenderer>().sharedMaterial = darkMat;
        GameObject plant = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        plant.name = "Plant";
        plant.transform.SetParent(room.transform);
        plant.transform.position = new Vector3(-4.9f, 0.95f, 4.6f);
        plant.transform.localScale = new Vector3(0.45f, 0.5f, 0.45f);
        plant.GetComponent<MeshRenderer>().sharedMaterial = plantMat;

        // ── Themed neon accents (recolored at runtime) ───────────────────────
        GameObject sign = MakeBox(room, "NeonSign", new Vector3(0f, 4.55f, 5.8f), new Vector3(5f, 0.14f, 0.1f), neonMat);
        GameObject stripL = MakeBox(room, "NeonCorner_L", new Vector3(-5.85f, 2.5f, 5.8f), new Vector3(0.1f, 5f, 0.1f), neonMat);
        GameObject stripR = MakeBox(room, "NeonCorner_R", new Vector3(5.85f, 2.5f, 5.8f), new Vector3(0.1f, 5f, 0.1f), neonMat);
        GameObject floorStrip = MakeBox(room, "NeonFloorStrip", new Vector3(0f, 0.03f, 5.6f), new Vector3(11.6f, 0.05f, 0.1f), neonMat);

        // ── Post-FX ──────────────────────────────────────────────────────────
        GameObject volObj = new GameObject("PostFX");
        Volume vol = volObj.AddComponent<Volume>();
        vol.isGlobal = true;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.DeleteAsset("Assets/Settings/HomePostFX.asset");
        AssetDatabase.CreateAsset(profile, "Assets/Settings/HomePostFX.asset");
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 1.3f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.9f;
        Vignette vig = profile.Add<Vignette>(true);
        vig.intensity.overrideState = true;
        vig.intensity.value = 0.3f;
        vol.sharedProfile = profile;
        EditorUtility.SetDirty(profile);

        // ── UI overlay ───────────────────────────────────────────────────────
        GameObject canvasObj = new GameObject("HomeUICanvas");
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
            "YOUR ROOM", CYAN, 14f, FontStyles.Bold, TextAlignmentOptions.Center);

        GameObject levelObj = MakeText("Txt_Level", topRT,
            new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(-110f, 0f), new Vector2(-14f, 0f),
            "LEVEL 37", GOLD, 11f, FontStyles.Bold, TextAlignmentOptions.Right);

        // Bottom bar
        GameObject bottomBar = MakePanel("BottomBar", uiRootRT,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Color(0.02f, 0.02f, 0.03f, 0.9f), false);
        RectTransform botRT = bottomBar.GetComponent<RectTransform>();
        botRT.offsetMin = new Vector2(0f, 0f);
        botRT.offsetMax = new Vector2(0f, 64f);

        GameObject nowPlaying = MakeText("Txt_NowPlaying", botRT,
            new Vector2(0f, 0.5f), new Vector2(0.55f, 1f),
            new Vector2(16f, -4f), new Vector2(0f, -6f),
            ">> Night Cruise — MobiLack", CYAN, 11f, FontStyles.Bold, TextAlignmentOptions.Left);

        GameObject themeName = MakeText("Txt_ThemeName", botRT,
            new Vector2(0f, 0f), new Vector2(0.55f, 0.5f),
            new Vector2(16f, 6f), new Vector2(0f, 2f),
            "Cyan Drift", TEXT_DIM, 10f, FontStyles.Normal, TextAlignmentOptions.Left);

        GameObject themeBtn = MakeButton("Btn_Theme", botRT,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-14f, 0f), new Vector2(82f, 40f),
            "Theme", Hex("0A0A0F"), 13f, PINK);
        themeBtn.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);

        GameObject musicBtn = MakeButton("Btn_Music", botRT,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-104f, 0f), new Vector2(82f, 40f),
            "Pause", Hex("0A0A0F"), 13f, CYAN);
        musicBtn.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);

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
        HomeRoomUIController ui = canvasObj.AddComponent<HomeRoomUIController>();
        SerializedObject uso = new SerializedObject(ui);
        uso.FindProperty("exitButton").objectReferenceValue = exitBtn.GetComponent<Button>();
        uso.FindProperty("titleText").objectReferenceValue = titleObj.GetComponent<TextMeshProUGUI>();
        uso.FindProperty("levelText").objectReferenceValue = levelObj.GetComponent<TextMeshProUGUI>();
        uso.FindProperty("nowPlayingText").objectReferenceValue = nowPlaying.GetComponent<TextMeshProUGUI>();
        uso.FindProperty("musicButton").objectReferenceValue = musicBtn.GetComponent<Button>();
        uso.FindProperty("musicLabel").objectReferenceValue =
            musicBtn.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        uso.FindProperty("themeButton").objectReferenceValue = themeBtn.GetComponent<Button>();
        uso.FindProperty("themeNameText").objectReferenceValue = themeName.GetComponent<TextMeshProUGUI>();
        uso.ApplyModifiedPropertiesWithoutUndo();

        // Room manager wiring
        GameObject mgrObj = new GameObject("HomeRoomManager");
        HomeRoomManager mgr = mgrObj.AddComponent<HomeRoomManager>();
        SerializedObject mso = new SerializedObject(mgr);
        mso.FindProperty("ui").objectReferenceValue = ui;
        mso.FindProperty("recordDisc").objectReferenceValue = disc.transform;
        mso.FindProperty("lavaRenderer").objectReferenceValue = lava.GetComponent<MeshRenderer>();
        mso.FindProperty("fadeCanvas").objectReferenceValue = fadeCG;

        SerializedProperty themed = mso.FindProperty("themedRenderers");
        Renderer[] themedRs =
        {
            sign.GetComponent<MeshRenderer>(),
            stripL.GetComponent<MeshRenderer>(),
            stripR.GetComponent<MeshRenderer>(),
            floorStrip.GetComponent<MeshRenderer>()
        };
        themed.arraySize = themedRs.Length;
        for (int i = 0; i < themedRs.Length; i++)
            themed.GetArrayElementAtIndex(i).objectReferenceValue = themedRs[i];
        mso.ApplyModifiedPropertiesWithoutUndo();

        // ── Save + Build Settings ────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Scene_Home.unity");

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Scene_City.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Home.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Lounge.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Cinema.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Arena.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true)
        };

        Debug.Log("[HomeSceneBuilder] Scene_Home built and saved. Build Settings updated.");
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
            m.SetFloat("_Smoothness", 0.35f);
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
