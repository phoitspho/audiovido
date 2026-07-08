using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// AUDIOVIDO — Main Scene Builder
/// Menu: AUDIOVIDO → Build Main Scene
/// Creates the hub entry screen with AUDIOVIDO branding and lounge entry button.
/// Run while MainScene is open (create it first via File > New Scene > Basic URP).
/// </summary>
public static class MainSceneBuilder
{
    static readonly Color BG_PRIMARY  = Hex("0A0A0F");
    static readonly Color CYAN        = Hex("00D4FF");
    static readonly Color AMBER       = Hex("FFD700");
    static readonly Color TEXT_WHITE  = Hex("FFFFFF");
    static readonly Color TEXT_DIM    = Hex("A0A0B8");

    [MenuItem("AUDIOVIDO/Build Main Scene")]
    public static void BuildMainScene()
    {
        // ── Camera ────────────────────────────────────────────────────────────
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = BG_PRIMARY;
            mainCam.orthographic = false;
        }

        // ── Dim / kill default directional light ──────────────────────────────
        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight) dirLight.intensity = 0f;

        // ── Canvas ────────────────────────────────────────────────────────────
        GameObject canvasObj = new GameObject("MainCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(844, 390);
        scaler.matchWidthOrHeight = 1f;

        canvasObj.AddComponent<GraphicRaycaster>();
        canvasObj.AddComponent<SafeAreaPanel>(); // insets content for notch / home bar
        RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();

        // ── Background panel (full screen) ────────────────────────────────────
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasRT, false);
        RectTransform bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = BG_PRIMARY;

        // ── Subtle cyan gradient overlay (top glow) ───────────────────────────
        GameObject glow = new GameObject("TopGlow");
        glow.transform.SetParent(canvasRT, false);
        RectTransform glowRT = glow.AddComponent<RectTransform>();
        glowRT.anchorMin = new Vector2(0f, 0.6f);
        glowRT.anchorMax = new Vector2(1f, 1f);
        glowRT.offsetMin = Vector2.zero; glowRT.offsetMax = Vector2.zero;
        Image glowImg = glow.AddComponent<Image>();
        glowImg.color = new Color(0f, 0.83f, 1f, 0.04f); // very subtle

        // ── App title — AUDIOVIDO ─────────────────────────────────────────────
        MakeText("Txt_AppTitle", canvasRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -160f), new Vector2(0f, -80f),
            new Vector2(0.5f, 1f),
            "AUDIOVIDO", CYAN, 32f, FontStyles.Bold, TextAlignmentOptions.Center);

        // ── Tagline ───────────────────────────────────────────────────────────
        MakeText("Txt_Tagline", canvasRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -200f), new Vector2(0f, -168f),
            new Vector2(0.5f, 1f),
            "Music. Video. Together.", TEXT_DIM, 14f, FontStyles.Normal, TextAlignmentOptions.Center);

        // ── Spaces label ──────────────────────────────────────────────────────
        MakeText("Txt_SpacesLabel", canvasRT,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, 60f), new Vector2(0f, 84f),
            new Vector2(0.5f, 0.5f),
            "CHOOSE YOUR SPACE", new Color(0.62f, 0.62f, 0.72f, 1f), 11f,
            FontStyles.Bold, TextAlignmentOptions.Center);

        // ── Enter The Lounge button ───────────────────────────────────────────
        GameObject loungeBtn = MakeButton("Btn_EnterLounge", canvasRT,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(300f, 60f),
            "Enter The Lounge →", AMBER, 18f,
            new Color(0.4f, 0.35f, 0f, 0.15f));

        // ── Coming soon labels for other spaces ───────────────────────────────
        MakeText("Txt_Cinema", canvasRT,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -70f), new Vector2(220f, 44f),
            new Vector2(0.5f, 0.5f),
            "Cinema  —  coming soon", TEXT_DIM, 13f, FontStyles.Normal, TextAlignmentOptions.Center);

        MakeText("Txt_Concert", canvasRT,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -110f), new Vector2(220f, 44f),
            new Vector2(0.5f, 0.5f),
            "Concert Venue  —  coming soon", TEXT_DIM, 13f, FontStyles.Normal, TextAlignmentOptions.Center);

        // ── Version label ─────────────────────────────────────────────────────
        MakeText("Txt_Version", canvasRT,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 20f), new Vector2(200f, 24f),
            new Vector2(0.5f, 0f),
            "v0.1 — Preview Build", new Color(0.3f, 0.3f, 0.4f, 1f), 10f,
            FontStyles.Normal, TextAlignmentOptions.Center);

        // ── Persistent managers (DontDestroyOnLoad) ──────────────────────────
        GameObject loaderObj = new GameObject("SceneLoader");
        loaderObj.AddComponent<SceneLoader>();

        // NXT token earn manager — persists across all scenes
        GameObject nxtObj = new GameObject("NxtEarnManager");
        nxtObj.AddComponent<NxtEarnManager>();

        // ── Fade canvas for SceneLoader ───────────────────────────────────────
        GameObject fadeObj = new GameObject("FadeCanvas");
        fadeObj.transform.SetParent(canvasRT, false);
        RectTransform fadeRT = fadeObj.AddComponent<RectTransform>();
        fadeRT.anchorMin = Vector2.zero; fadeRT.anchorMax = Vector2.one;
        fadeRT.offsetMin = Vector2.zero; fadeRT.offsetMax = Vector2.zero;
        fadeObj.AddComponent<Image>().color = Color.black;
        CanvasGroup fadeCG = fadeObj.AddComponent<CanvasGroup>();
        fadeCG.alpha = 0f;
        fadeCG.blocksRaycasts = false;
        fadeObj.SetActive(false);

        // Wire SceneLoader → fadeCanvas
        SerializedObject loaderSO = new SerializedObject(loaderObj.GetComponent<SceneLoader>());
        loaderSO.FindProperty("fadeCanvas").objectReferenceValue = fadeCG;
        loaderSO.ApplyModifiedPropertiesWithoutUndo();

        // ── Wire button → SceneLoader ─────────────────────────────────────────
        var btn = loungeBtn.GetComponent<Button>();
        if (btn != null)
        {
            // Add MainMenuController to canvas for button wiring
            var ctrl = canvasObj.AddComponent<MainMenuController>();
            SerializedObject ctrlSO = new SerializedObject(ctrl);
            ctrlSO.FindProperty("enterLoungeButton").objectReferenceValue = btn;
            ctrlSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── EventSystem ───────────────────────────────────────────────────────
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[MainSceneBuilder] Main scene built.");
        EditorUtility.DisplayDialog("Done",
            "MainScene built!\n\nSave as Assets/Scenes/MainScene.unity\nthen add both scenes to Build Settings.", "OK");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static GameObject MakeText(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Vector2 pivot,
        string text, Color color, float fontSize,
        FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.color = color;
        tmp.fontSize = fontSize; tmp.fontStyle = style;
        tmp.alignment = alignment;
        return go;
    }

    static GameObject MakeButton(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta,
        string label, Color textColor, float fontSize, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        // Border highlight (cyan glow)
        // (purely cosmetic — the button label carries the amber color)
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(go.transform, false);
        RectTransform lrt = labelObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.color = textColor;
        tmp.fontSize = fontSize; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
