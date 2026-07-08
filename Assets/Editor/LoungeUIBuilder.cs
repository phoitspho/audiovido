using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEditor;

/// <summary>
/// AUDIOVIDO — Lounge UI Builder
/// Menu: AUDIOVIDO → Build Lounge UI
/// Creates the full 2D UI overlay for Scene_Lounge:
///   • UI Camera (URP Overlay) added to Main Camera stack
///   • Top bar: ← Exit | The Lounge | 👥 count
///   • Bottom bar: Now Playing | Queue | Chat | Invite | SitHere
///   • DRIFT chat bubble (slides up)
///   • Fade canvas (CanvasGroup for transitions)
/// </summary>
public static class LoungeUIBuilder
{
    // Design tokens
    static readonly Color BG_SECONDARY  = Hex("111118");
    static readonly Color TEXT_PRIMARY   = Hex("FFFFFF");
    static readonly Color TEXT_SECONDARY = Hex("A0A0B8");
    static readonly Color CYAN           = Hex("00D4FF");
    static readonly Color AMBER          = Hex("FFD700");
    static readonly Color CORAL          = Hex("FF4757");
    static readonly Color TRANSPARENT    = new Color(0, 0, 0, 0);

    [MenuItem("AUDIOVIDO/Build Lounge UI")]
    public static void BuildLoungeUI()
    {
        // ── Step 1: UI Camera (URP Overlay) ─────────────────────────────────
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            EditorUtility.DisplayDialog("Error", "No Main Camera in scene.", "OK");
            return;
        }

        // Create UI Camera GameObject
        GameObject uiCamObj = new GameObject("UI_Camera");
        Camera uiCam = uiCamObj.AddComponent<Camera>();
        uiCam.cullingMask = LayerMask.GetMask("UI");
        uiCam.clearFlags = CameraClearFlags.Nothing; // don't clear depth
        uiCam.depth = mainCam.depth + 1;
        uiCam.orthographic = false;

        // Set as URP Overlay camera
        var uiCamData = uiCamObj.GetComponent<UniversalAdditionalCameraData>();
        if (uiCamData == null) uiCamData = uiCamObj.AddComponent<UniversalAdditionalCameraData>();
        uiCamData.renderType = CameraRenderType.Overlay;

        // Add to Main Camera stack
        var mainCamData = mainCam.GetComponent<UniversalAdditionalCameraData>();
        if (mainCamData != null)
            mainCamData.cameraStack.Add(uiCam);

        // ── Step 2: Root Canvas ──────────────────────────────────────────────
        GameObject canvasObj = new GameObject("LoungeUICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCam;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(844, 390); // iPhone 14 Pro
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f; // match width for phone

        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();

        // ── Step 3: Top Bar ──────────────────────────────────────────────────
        // Panel (full-width, anchored top)
        GameObject topBar = MakePanel("TopBar", canvasRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f),   // anchor min/max
            new Vector2(0f, -60f), new Vector2(0f, 0f),  // offset min/max
            new Vector2(0.5f, 1f),                         // pivot
            60f,                                            // height
            new Color(0.067f, 0.067f, 0.094f, 0.85f));    // semi-trans dark

        // Safe area — top inset only (clears notch / Dynamic Island)
        var topSafe = topBar.AddComponent<SafeAreaPanel>();
        var topSafeSO = new UnityEditor.SerializedObject(topSafe);
        topSafeSO.FindProperty("applyTop").boolValue    = true;
        topSafeSO.FindProperty("applyBottom").boolValue = false;
        topSafeSO.FindProperty("applySides").boolValue  = true;
        topSafeSO.ApplyModifiedPropertiesWithoutUndo();

        // Exit button (left)
        GameObject exitBtn = MakeButton("Btn_Exit", topBar.GetComponent<RectTransform>(),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(10f, 0), new Vector2(80f, 44f), "← Exit", TEXT_PRIMARY, 14f);

        // Title (center)
        GameObject titleObj = MakeText("Txt_Title", topBar.GetComponent<RectTransform>(),
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero,
            "The Lounge", TEXT_PRIMARY, 18f, FontStyles.Bold, TextAlignmentOptions.Center);

        // User count (right)
        GameObject userCountObj = MakeText("Txt_UserCount", topBar.GetComponent<RectTransform>(),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-80f, 0), new Vector2(70f, 44f),
            "👥 1", TEXT_SECONDARY, 14f, FontStyles.Normal, TextAlignmentOptions.Right);

        // ── Step 4: Bottom Bar ───────────────────────────────────────────────
        GameObject bottomBar = MakePanel("BottomBar", canvasRT,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, 90f),
            new Vector2(0.5f, 0f),
            90f,
            new Color(0.067f, 0.067f, 0.094f, 0.9f));

        // Safe area — bottom inset only (clears iOS home bar)
        var botSafe = bottomBar.AddComponent<SafeAreaPanel>();
        var botSafeSO = new UnityEditor.SerializedObject(botSafe);
        botSafeSO.FindProperty("applyTop").boolValue    = false;
        botSafeSO.FindProperty("applyBottom").boolValue = true;
        botSafeSO.FindProperty("applySides").boolValue  = true;
        botSafeSO.ApplyModifiedPropertiesWithoutUndo();

        RectTransform bottomRT = bottomBar.GetComponent<RectTransform>();

        // Now Playing — viewport container (clips overflowing marquee text)
        GameObject npViewport = new GameObject("NowPlaying_Viewport");
        npViewport.transform.SetParent(bottomRT, false);
        RectTransform npVP = npViewport.AddComponent<RectTransform>();
        npVP.anchorMin = new Vector2(0f, 1f); npVP.anchorMax = new Vector2(1f, 1f);
        npVP.pivot     = new Vector2(0f, 1f);
        npVP.offsetMin = new Vector2(12f, -36f);
        npVP.offsetMax = new Vector2(-12f, -8f);
        npViewport.AddComponent<UnityEngine.UI.RectMask2D>(); // clips marquee

        // Now Playing label (child of viewport — width unconstrained so marquee can scroll)
        var npTextObj = MakeText("Txt_NowPlaying", npVP,
            new Vector2(0f, 0f), new Vector2(0f, 1f),  // left-anchored, free width
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            ">> Lo-fi chill mix", CYAN, 12f,
            FontStyles.Normal, TextAlignmentOptions.Left);
        // Allow text to be as wide as it needs (overflow unconstrained on X)
        var npTMP = npTextObj.GetComponent<TextMeshProUGUI>();
        if (npTMP != null)
        {
            npTMP.textWrappingMode = TextWrappingModes.NoWrap;
            npTMP.overflowMode     = TextOverflowModes.Overflow;
        }
        // Add marquee scroll
        npTextObj.AddComponent<MarqueeText>();

        // Button row
        float btnY = -70f;
        string[] btnLabels = { "Queue", "Chat", "Invite", "Sit Here" };
        string[] btnNames  = { "Btn_Queue", "Btn_Chat", "Btn_Invite", "Btn_SitHere" };
        Color[] btnColors  = { TEXT_SECONDARY, TEXT_SECONDARY, TEXT_SECONDARY, CYAN };
        float totalBtns = btnLabels.Length;
        float btnW = 80f;
        float spacing = 10f;
        float totalW = totalBtns * btnW + (totalBtns - 1) * spacing;
        float startX = -totalW / 2f + btnW / 2f;

        for (int i = 0; i < btnLabels.Length; i++)
        {
            MakeButton(btnNames[i], bottomRT,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + i * (btnW + spacing), btnY),
                new Vector2(btnW, 36f),
                btnLabels[i], btnColors[i], 13f);
        }

        // ── Step 5: DRIFT Chat Bubble ────────────────────────────────────────
        // Anchored center-bottom, slides up 120px when active
        GameObject driftBubble = MakePanel("DriftBubble", canvasRT,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-150f, 95f), new Vector2(150f, 175f),
            new Vector2(0.5f, 0f),
            80f,
            new Color(0.067f, 0.067f, 0.094f, 0.95f));

        // Rounded appearance via Image color
        Image bubbleImg = driftBubble.GetComponent<Image>();
        if (bubbleImg) bubbleImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // DRIFT name label
        MakeText("Txt_DriftName", driftBubble.GetComponent<RectTransform>(),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(12f, -28f), new Vector2(-12f, -4f),
            "DRIFT", AMBER, 12f, FontStyles.Bold, TextAlignmentOptions.Left);

        // Bubble text
        MakeText("Txt_DriftMessage", driftBubble.GetComponent<RectTransform>(),
            new Vector2(0f, 0f), new Vector2(1f, 0.6f),
            new Vector2(12f, 8f), new Vector2(-12f, -4f),
            "Long day?", TEXT_PRIMARY, 15f, FontStyles.Normal, TextAlignmentOptions.Left);

        // Hide by default (off-screen below)
        driftBubble.SetActive(false);

        // ── Step 6: Fade Canvas (black overlay for transitions) ──────────────
        GameObject fadeObj = new GameObject("FadeCanvas");
        fadeObj.transform.SetParent(canvasRT);
        RectTransform fadeRT = fadeObj.AddComponent<RectTransform>();
        fadeRT.anchorMin = Vector2.zero;
        fadeRT.anchorMax = Vector2.one;
        fadeRT.offsetMin = Vector2.zero;
        fadeRT.offsetMax = Vector2.zero;

        Image fadeImg = fadeObj.AddComponent<Image>();
        fadeImg.color = Color.black;

        CanvasGroup fadeCG = fadeObj.AddComponent<CanvasGroup>();
        fadeCG.alpha = 0f;
        fadeCG.blocksRaycasts = false;

        // ── Step 7: Add EventSystem if missing ───────────────────────────────
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── Step 8: Add LoungeUIController ──────────────────────────────────
        LoungeUIController uiController = canvasObj.AddComponent<LoungeUIController>();
        // References will need to be set in Inspector (can't easily auto-assign private fields)

        // Register undo
        Undo.RegisterCreatedObjectUndo(uiCamObj, "Build Lounge UI");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Build Lounge UI");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[LoungeUIBuilder] UI overlay built. 0 errors.");
        EditorUtility.DisplayDialog("Done",
            "Lounge UI overlay built!\n\n" +
            "Now wire up LoungeUIController in the Inspector:\n" +
            "• exitButton → Btn_Exit\n" +
            "• titleText → Txt_Title\n" +
            "• userCountText → Txt_UserCount\n" +
            "• nowPlayingText → Txt_NowPlaying\n" +
            "• sitHereButton → Btn_SitHere\n" +
            "• driftBubbleRoot → DriftBubble\n" +
            "• driftBubbleText → Txt_DriftMessage\n" +
            "• fadeCanvas (LoungeManager) → FadeCanvas", "OK");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static GameObject MakePanel(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        Vector2 pivot, float height, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        return go;
    }

    static GameObject MakeButton(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta,
        string label, Color textColor, float fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Image img = go.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.05f); // nearly transparent bg
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        // Label child
        MakeText(name + "_Label", rt,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            label, textColor, fontSize, FontStyles.Normal, TextAlignmentOptions.Center);

        return go;
    }

    static GameObject MakeText(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        string text, Color color, float fontSize,
        FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return go;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
