using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// AUDIOVIDO — Plaza Scene Builder (Fan Plaza &amp; Social Hub, concept panel 6)
/// Menu: AUDIOVIDO → Build Plaza Scene
///
/// Generates Scene_Plaza: open night plaza with a giant rotating hologram
/// over a glowing pedestal, CONNECT signage on surrounding buildings,
/// ambient fans, VIBE with a rainbow-cycling glow, bloom post-FX, and the
/// overlay UI (Exit / community feed / WAVE!). Exits to Scene_City.
/// </summary>
public static class PlazaSceneBuilder
{
    static readonly Color BG_NIGHT = Hex("04050B");
    static readonly Color GROUND   = new Color(0.05f, 0.055f, 0.08f);
    static readonly Color BUILDING = new Color(0.07f, 0.07f, 0.11f);
    static readonly Color CROWD    = new Color(0.09f, 0.08f, 0.13f);
    static readonly Color HOLO     = new Color(0.3f, 0.62f, 1f);
    static readonly Color BLUE     = Hex("4D8DFF");
    static readonly Color PINK     = Hex("FF2E92");
    static readonly Color TEXT_DIM = Hex("A0A0B8");

    const string MAT_DIR = "Assets/Materials/Plaza";

    [MenuItem("AUDIOVIDO/Build Plaza Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        if (!AssetDatabase.IsValidFolder(MAT_DIR))
            AssetDatabase.CreateFolder("Assets/Materials", "Plaza");

        // ── Atmosphere ───────────────────────────────────────────────────────
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.015f, 0.02f, 0.04f);
        RenderSettings.fogDensity = 0.014f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.12f, 0.13f, 0.2f);

        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight != null)
        {
            dirLight.intensity = 0.2f;
            dirLight.color = new Color(0.6f, 0.7f, 1f);
            dirLight.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            dirLight.shadows = LightShadows.None;
        }

        GameObject holoLightObj = new GameObject("HoloLight");
        Light holoLight = holoLightObj.AddComponent<Light>();
        holoLight.type = LightType.Point;
        holoLight.color = HOLO;
        holoLight.intensity = 1.8f;
        holoLight.range = 16f;
        holoLightObj.transform.position = new Vector3(0f, 5f, 5f);

        // ── Camera ───────────────────────────────────────────────────────────
        Camera cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BG_NIGHT;
        cam.fieldOfView = 60f;
        cam.farClipPlane = 90f;
        cam.allowHDR = true;
        cam.transform.position = new Vector3(0f, 3.2f, -9f);
        cam.transform.rotation = Quaternion.Euler(2f, 0f, 0f);
        var camData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        // ── Materials ────────────────────────────────────────────────────────
        Material groundMat   = MakeMat("Plaza_Ground", GROUND, null, 0f);
        Material buildingMat = MakeMat("Plaza_Building", BUILDING, null, 0f);
        Material crowdMat    = MakeMat("Plaza_Crowd", CROWD, null, 0f);
        Material holoMat     = MakeMat("Plaza_Holo", Color.black, HOLO, 1.8f);
        Material ringMat     = MakeMat("Plaza_Ring", Color.black, HOLO, 1.5f);
        Material blueMat     = MakeMat("Plaza_Blue", Color.black, BLUE, 1.8f);
        Material pinkMat     = MakeMat("Plaza_Pink", Color.black, PINK, 1.8f);
        Material vibeBodyMat = MakeMat("Plaza_VibeBody", new Color(0.09f, 0.08f, 0.12f), null, 0f);

        // ── Ground + pedestal ────────────────────────────────────────────────
        GameObject plaza = new GameObject("Plaza_Structure");

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(plaza.transform);
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = "Pedestal";
        pedestal.transform.SetParent(plaza.transform);
        pedestal.transform.position = new Vector3(0f, 0.35f, 5f);
        pedestal.transform.localScale = new Vector3(4.4f, 0.35f, 4.4f);
        pedestal.GetComponent<MeshRenderer>().sharedMaterial = buildingMat;

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "PedestalRing";
        ring.transform.SetParent(plaza.transform);
        ring.transform.position = new Vector3(0f, 0.72f, 5f);
        ring.transform.localScale = new Vector3(4.6f, 0.05f, 4.6f);
        ring.GetComponent<MeshRenderer>().sharedMaterial = ringMat;

        // ── Hologram figure — giant humanoid proxy, arms raised (art pass) ───
        GameObject holoRoot = new GameObject("HologramRoot");
        holoRoot.transform.SetParent(plaza.transform);
        holoRoot.transform.position = new Vector3(0f, 0.75f, 5f); // stands on pedestal

        CharacterProxyBuilder.Proxy holoProxy = CharacterProxyBuilder.Build(
            "HoloFigure", holoRoot.transform.position, 2.4f, holoMat, holoMat,
            armsRaised: true);
        holoProxy.root.transform.SetParent(holoRoot.transform, true);
        foreach (Collider c in holoProxy.root.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);

        // ── Surrounding buildings with CONNECT signage ───────────────────────
        MakeBox(plaza, "Bldg_L", new Vector3(-9f, 4f, 8f), new Vector3(5f, 8f, 5f), buildingMat);
        MakeBox(plaza, "Bldg_R", new Vector3(9f, 5f, 8f), new Vector3(5f, 10f, 5f), buildingMat);
        MakeBox(plaza, "Bldg_Back", new Vector3(0f, 4.5f, 14f), new Vector3(8f, 9f, 4f), buildingMat);

        MakeBox(plaza, "Sign_L", new Vector3(-9f, 6.5f, 5.4f), new Vector3(3.6f, 0.35f, 0.1f), pinkMat);
        MakeBox(plaza, "Sign_R", new Vector3(9f, 8f, 5.4f), new Vector3(3.6f, 0.35f, 0.1f), blueMat);
        MakeBox(plaza, "Sign_Back", new Vector3(0f, 7.6f, 11.9f), new Vector3(5f, 0.4f, 0.1f), blueMat);

        // World-space CONNECT label above the back building
        GameObject connectObj = new GameObject("Txt_Connect");
        connectObj.transform.SetParent(plaza.transform);
        connectObj.transform.position = new Vector3(0f, 9.6f, 13.8f);
        TextMeshPro connect = connectObj.AddComponent<TextMeshPro>();
        connect.text = "CONNECT";
        connect.fontSize = 20f;
        connect.fontStyle = FontStyles.Bold;
        connect.alignment = TextAlignmentOptions.Center;
        connect.color = BLUE;
        connect.rectTransform.sizeDelta = new Vector2(20f, 3f);

        // ── Ambient fans around the pedestal ─────────────────────────────────
        System.Random rng = new System.Random(23);
        GameObject fans = new GameObject("Fans");
        fans.transform.SetParent(plaza.transform);
        for (int i = 0; i < 10; i++)
        {
            float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float r = 4.2f + (float)rng.NextDouble() * 3.5f;
            GameObject fan = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fan.name = "Fan_" + i;
            fan.transform.SetParent(fans.transform);
            fan.transform.position = new Vector3(
                Mathf.Sin(ang) * r, 0.85f, 5f + Mathf.Cos(ang) * r * 0.7f);
            fan.transform.localScale = new Vector3(0.5f, 0.85f, 0.5f);
            fan.GetComponent<MeshRenderer>().sharedMaterial = crowdMat;
            Object.DestroyImmediate(fan.GetComponent<Collider>());
        }

        // ── VIBE (front-right, hyping) ───────────────────────────────────────
        GameObject vibe = new GameObject("VIBE_Character");
        vibe.transform.position = new Vector3(3.2f, 0f, 1.2f);

        GameObject vBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        vBody.name = "Body";
        vBody.transform.SetParent(vibe.transform);
        vBody.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        vBody.transform.localScale = new Vector3(0.8f, 1.05f, 0.8f);
        vBody.GetComponent<MeshRenderer>().sharedMaterial = vibeBodyMat;

        GameObject vSash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vSash.name = "GlowSash";
        vSash.transform.SetParent(vibe.transform);
        vSash.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        vSash.transform.localScale = new Vector3(0.9f, 0.12f, 0.9f);
        vSash.GetComponent<MeshRenderer>().sharedMaterial = pinkMat;

        GameObject vHair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vHair.name = "GlowHair";
        vHair.transform.SetParent(vibe.transform);
        vHair.transform.localPosition = new Vector3(0f, 2.25f, 0f);
        vHair.transform.localScale = new Vector3(0.5f, 0.35f, 0.5f);
        vHair.GetComponent<MeshRenderer>().sharedMaterial = pinkMat;

        VibePresence vibePresence = vibe.AddComponent<VibePresence>();
        SerializedObject vso = new SerializedObject(vibePresence);
        SerializedProperty vGlows = vso.FindProperty("glowRenderers");
        Renderer[] vibeGlowRs = { vSash.GetComponent<MeshRenderer>(), vHair.GetComponent<MeshRenderer>() };
        vGlows.arraySize = vibeGlowRs.Length;
        for (int g = 0; g < vibeGlowRs.Length; g++)
            vGlows.GetArrayElementAtIndex(g).objectReferenceValue = vibeGlowRs[g];
        vso.ApplyModifiedPropertiesWithoutUndo();

        // ── Post-FX ──────────────────────────────────────────────────────────
        GameObject volObj = new GameObject("PostFX");
        Volume vol = volObj.AddComponent<Volume>();
        vol.isGlobal = true;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.DeleteAsset("Assets/Settings/PlazaPostFX.asset");
        AssetDatabase.CreateAsset(profile, "Assets/Settings/PlazaPostFX.asset");
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
        GameObject canvasObj = new GameObject("PlazaUICanvas");
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
            "FAN PLAZA", BLUE, 14f, FontStyles.Bold, TextAlignmentOptions.Center);

        // Bottom bar
        GameObject bottomBar = MakePanel("BottomBar", uiRootRT,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Color(0.02f, 0.02f, 0.03f, 0.9f), false);
        RectTransform botRT = bottomBar.GetComponent<RectTransform>();
        botRT.offsetMin = new Vector2(0f, 0f);
        botRT.offsetMax = new Vector2(0f, 64f);

        GameObject feed = MakeText("Txt_Feed", botRT,
            new Vector2(0f, 0f), new Vector2(0.68f, 1f),
            new Vector2(16f, 0f), new Vector2(0f, 0f),
            "Share. Belong. Inspire.", TEXT_DIM, 11f, FontStyles.Normal, TextAlignmentOptions.Left);

        GameObject waveBtn = MakeButton("Btn_Wave", botRT,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-14f, 0f), new Vector2(100f, 40f),
            "WAVE!", Hex("0A0A0F"), 15f, BLUE);
        waveBtn.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);

        // VIBE bubble
        GameObject bubble = MakePanel("VibeBubble", uiRootRT,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Color(0.07f, 0.05f, 0.09f, 0.96f), true);
        RectTransform bubRT = bubble.GetComponent<RectTransform>();
        bubRT.pivot = new Vector2(0.5f, 0f);
        bubRT.anchoredPosition = new Vector2(0f, 76f);
        bubRT.sizeDelta = new Vector2(320f, 66f);
        MakeText("Txt_VibeName", bubRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(14f, -24f), new Vector2(-14f, -4f),
            "VIBE", PINK, 11f, FontStyles.Bold, TextAlignmentOptions.Left);
        GameObject vibeMsg = MakeText("Txt_VibeMessage", bubRT,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(14f, 6f), new Vector2(-14f, -26f),
            "Your tribe is here!", Color.white, 13f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
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
        PlazaUIController ui = canvasObj.AddComponent<PlazaUIController>();
        SerializedObject uso = new SerializedObject(ui);
        uso.FindProperty("exitButton").objectReferenceValue = exitBtn.GetComponent<Button>();
        uso.FindProperty("titleText").objectReferenceValue = titleObj.GetComponent<TextMeshProUGUI>();
        uso.FindProperty("feedText").objectReferenceValue = feed.GetComponent<TextMeshProUGUI>();
        uso.FindProperty("waveButton").objectReferenceValue = waveBtn.GetComponent<Button>();
        uso.FindProperty("vibeBubbleRoot").objectReferenceValue = bubble;
        uso.FindProperty("vibeMessageText").objectReferenceValue = vibeMsg.GetComponent<TextMeshProUGUI>();
        uso.ApplyModifiedPropertiesWithoutUndo();

        // Plaza manager wiring
        GameObject mgrObj = new GameObject("PlazaManager");
        PlazaManager mgr = mgrObj.AddComponent<PlazaManager>();
        SerializedObject mso = new SerializedObject(mgr);
        mso.FindProperty("ui").objectReferenceValue = ui;
        mso.FindProperty("hologramRoot").objectReferenceValue = holoRoot.transform;
        mso.FindProperty("pedestalRing").objectReferenceValue = ring.GetComponent<MeshRenderer>();
        mso.FindProperty("fadeCanvas").objectReferenceValue = fadeCG;

        SerializedProperty holos = mso.FindProperty("hologramRenderers");
        Renderer[] holoRs = holoProxy.root.GetComponentsInChildren<Renderer>();
        holos.arraySize = holoRs.Length;
        for (int i = 0; i < holoRs.Length; i++)
            holos.GetArrayElementAtIndex(i).objectReferenceValue = holoRs[i];
        mso.ApplyModifiedPropertiesWithoutUndo();

        // ── Save + Build Settings ────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Scene_Plaza.unity");

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

        Debug.Log("[PlazaSceneBuilder] Scene_Plaza built and saved. Build Settings updated.");
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
