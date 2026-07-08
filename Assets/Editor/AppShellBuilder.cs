using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// AUDIOVIDO — App Shell Builder
/// Menu: AUDIOVIDO → Build App Shell (MainScene)
///
/// Rebuilds MainScene as the full 2D app backbone (spec §3):
///   • Header: AUDIOVIDO + NXT pill
///   • 5 screens: Home (Luna), Search (Petros), Hub Music/Video (Echo/Reel),
///     Fan Club (Vibe), Profile (Mirror) — mock content per spec §5
///   • Mini player bar + bottom navigation (5 tabs)
///   • Standalone fade canvas + persistent managers
/// Idempotent — safe to run repeatedly. Run with MainScene open, then save.
/// </summary>
public static class AppShellBuilder
{
    // ── Design tokens (spec §1.2) ────────────────────────────────────────────
    static readonly Color BG_PRIMARY    = Hex("0A0A0F");
    static readonly Color BG_SECONDARY  = Hex("111118");
    static readonly Color BG_TERTIARY   = Hex("1A1A24");
    static readonly Color CYAN          = Hex("00D4FF");
    static readonly Color CORAL         = Hex("FF4757");
    static readonly Color GOLD          = Hex("FFD700");
    static readonly Color SILVER        = Hex("C0C0D0");
    static readonly Color TEXT_PRIMARY  = Hex("FFFFFF");
    static readonly Color TEXT_SECOND   = Hex("A0A0B8");
    static readonly Color TEXT_TERTIARY = Hex("5A5A78");
    static readonly Color CLEAR_HIT     = new Color(0f, 0f, 0f, 0.001f); // invisible but raycastable
    static readonly Color CYAN_SURFACE  = new Color(0f, 0.831f, 1f, 0.08f);
    static readonly Color CORAL_SURFACE = new Color(1f, 0.278f, 0.341f, 0.08f);

    const float NAV_H = 64f;
    const float MINI_H = 56f;
    const float HEADER_H = 56f;

    static Sprite Knob    => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    static Sprite Rounded => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    [MenuItem("AUDIOVIDO/Build App Shell (MainScene)")]
    public static void Build()
    {
        // ── Ensure MainScene is open ─────────────────────────────────────────
        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (active.name != "MainScene")
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        }

        // ── Cleanup (idempotent) ─────────────────────────────────────────────
        string[] stale = { "AppShellCanvas", "MainCanvas", "FadeCanvasRoot",
                           "SceneLoader", "NxtEarnManager", "EventSystem" };
        foreach (string n in stale)
        {
            GameObject old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }

        // ── Camera / light ───────────────────────────────────────────────────
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = BG_PRIMARY;
        }
        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight != null) dirLight.intensity = 0f;

        // ── Root canvas ──────────────────────────────────────────────────────
        GameObject canvasObj = new GameObject("AppShellCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(844, 390);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        canvasObj.AddComponent<GraphicRaycaster>();
        RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();

        // Full-bleed background
        MakePanel("BG", canvasRT, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, BG_PRIMARY, false);

        // Safe-area root — everything lives inside
        GameObject rootObj = new GameObject("ShellRoot");
        rootObj.transform.SetParent(canvasRT, false);
        RectTransform rootRT = rootObj.AddComponent<RectTransform>();
        Stretch(rootRT);
        rootObj.AddComponent<SafeAreaPanel>();

        // ── Header ───────────────────────────────────────────────────────────
        GameObject header = MakePanel("Header", rootRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -HEADER_H), new Vector2(0f, 0f),
            new Color(0.039f, 0.039f, 0.059f, 0.95f), false);
        RectTransform headerRT = header.GetComponent<RectTransform>();

        MakeText("Txt_Logo", headerRT,
            new Vector2(0f, 0f), new Vector2(0.6f, 1f),
            new Vector2(16f, 0f), new Vector2(0f, 0f),
            "AUDIOVIDO", CYAN, 16f, FontStyles.Bold, TextAlignmentOptions.Left);

        GameObject nxtPill = MakePanel("NxtPill", headerRT,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            Vector2.zero, Vector2.zero, BG_TERTIARY, true);
        RectTransform pillRT = nxtPill.GetComponent<RectTransform>();
        pillRT.pivot = new Vector2(1f, 0.5f);
        pillRT.anchoredPosition = new Vector2(-16f, 0f);
        pillRT.sizeDelta = new Vector2(84f, 28f);
        GameObject nxtTextObj = MakeText("Txt_NxtBalance", pillRT,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "0 NXT", GOLD, 12f, FontStyles.Bold, TextAlignmentOptions.Center);

        // ── Screen container ─────────────────────────────────────────────────
        GameObject container = new GameObject("ScreenContainer");
        container.transform.SetParent(rootRT, false);
        RectTransform containerRT = container.AddComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = new Vector2(0f, NAV_H + MINI_H);
        containerRT.offsetMax = new Vector2(0f, -HEADER_H);

        // ── Screens ──────────────────────────────────────────────────────────
        GameObject homeScreen    = BuildHomeScreen(containerRT);
        GameObject searchScreen  = BuildSearchScreen(containerRT);
        GameObject hubScreen     = BuildHubScreen(containerRT);
        GameObject fanClubScreen = BuildFanClubScreen(containerRT);
        GameObject profileScreen = BuildProfileScreen(containerRT);

        // Only Home visible initially
        searchScreen.SetActive(false);
        hubScreen.SetActive(false);
        fanClubScreen.SetActive(false);
        profileScreen.SetActive(false);

        // ── Mini player (spec §5.9.1 — static preview for now) ───────────────
        GameObject mini = MakePanel("MiniPlayer", rootRT,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(8f, NAV_H + 4f), new Vector2(-8f, NAV_H + MINI_H - 4f),
            BG_SECONDARY, true);
        RectTransform miniRT = mini.GetComponent<RectTransform>();
        GameObject art = MakePanel("Art", miniRT,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero, CYAN_SURFACE, true);
        RectTransform artRT = art.GetComponent<RectTransform>();
        artRT.anchoredPosition = new Vector2(28f, 0f);
        artRT.sizeDelta = new Vector2(40f, 40f);
        MakeText("Txt_MiniTitle", miniRT,
            new Vector2(0f, 0.5f), new Vector2(1f, 1f),
            new Vector2(56f, -12f), new Vector2(-56f, -4f),
            "Night Cruise", TEXT_PRIMARY, 13f, FontStyles.Bold, TextAlignmentOptions.Left);
        MakeText("Txt_MiniArtist", miniRT,
            new Vector2(0f, 0f), new Vector2(1f, 0.5f),
            new Vector2(56f, 6f), new Vector2(-56f, 10f),
            "MobiLack", TEXT_SECOND, 11f, FontStyles.Normal, TextAlignmentOptions.Left);
        MakeText("Txt_MiniPlay", miniRT,
            new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(-48f, 0f), new Vector2(-12f, 0f),
            ">", CYAN, 20f, FontStyles.Bold, TextAlignmentOptions.Center);

        // ── Bottom nav (spec §3.1) ───────────────────────────────────────────
        GameObject nav = MakePanel("BottomNav", rootRT,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, NAV_H),
            new Color(0.039f, 0.039f, 0.059f, 0.97f), false);
        RectTransform navRT = nav.GetComponent<RectTransform>();

        string[] tabNames = { "Home", "Search", "Hub", "Club", "You" };
        Button[] navButtons = new Button[5];
        TMP_Text[] navLabels = new TMP_Text[5];
        Image[] navDots = new Image[5];

        for (int i = 0; i < 5; i++)
        {
            GameObject tab = new GameObject("Btn_Nav_" + tabNames[i]);
            tab.transform.SetParent(navRT, false);
            RectTransform tabRT = tab.AddComponent<RectTransform>();
            tabRT.anchorMin = new Vector2(i / 5f, 0f);
            tabRT.anchorMax = new Vector2((i + 1) / 5f, 1f);
            tabRT.offsetMin = Vector2.zero;
            tabRT.offsetMax = Vector2.zero;
            Image tabImg = tab.AddComponent<Image>();
            tabImg.color = CLEAR_HIT;
            Button tabBtn = tab.AddComponent<Button>();
            tabBtn.targetGraphic = tabImg;
            navButtons[i] = tabBtn;

            // Hub tab gets an elevated pill background (spec §3.1)
            if (i == 2)
            {
                GameObject pill = MakePanel("HubPill", tabRT,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero, BG_TERTIARY, true);
                RectTransform hpRT = pill.GetComponent<RectTransform>();
                hpRT.sizeDelta = new Vector2(56f, 44f);
                pill.GetComponent<Image>().raycastTarget = false;
            }

            // Active-tab dot indicator
            GameObject dot = new GameObject("Dot");
            dot.transform.SetParent(tabRT, false);
            RectTransform dotRT = dot.AddComponent<RectTransform>();
            dotRT.anchorMin = new Vector2(0.5f, 1f);
            dotRT.anchorMax = new Vector2(0.5f, 1f);
            dotRT.anchoredPosition = new Vector2(0f, -10f);
            dotRT.sizeDelta = new Vector2(6f, 6f);
            Image dotImg = dot.AddComponent<Image>();
            dotImg.sprite = Knob;
            dotImg.color = CYAN;
            dotImg.raycastTarget = false;
            navDots[i] = dotImg;

            GameObject lbl = MakeText("Label", tabRT,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                tabNames[i], TEXT_TERTIARY, i == 2 ? 14f : 12f,
                FontStyles.Normal, TextAlignmentOptions.Center);
            lbl.GetComponent<TextMeshProUGUI>().raycastTarget = false;
            navLabels[i] = lbl.GetComponent<TextMeshProUGUI>();
        }

        // ── Standalone fade canvas (survives shell hide) ─────────────────────
        GameObject fadeRoot = new GameObject("FadeCanvasRoot");
        Canvas fadeCanvasCmp = fadeRoot.AddComponent<Canvas>();
        fadeCanvasCmp.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvasCmp.sortingOrder = 100;
        GameObject fadeObj = new GameObject("Fade");
        fadeObj.transform.SetParent(fadeRoot.transform, false);
        RectTransform fadeRT = fadeObj.AddComponent<RectTransform>();
        Stretch(fadeRT);
        fadeObj.AddComponent<Image>().color = Color.black;
        CanvasGroup fadeCG = fadeObj.AddComponent<CanvasGroup>();
        fadeCG.alpha = 0f;
        fadeCG.blocksRaycasts = false;
        fadeObj.SetActive(false);

        // ── Persistent managers ──────────────────────────────────────────────
        GameObject loaderObj = new GameObject("SceneLoader");
        SceneLoader loader = loaderObj.AddComponent<SceneLoader>();
        SerializedObject loaderSO = new SerializedObject(loader);
        loaderSO.FindProperty("fadeCanvas").objectReferenceValue = fadeCG;
        loaderSO.ApplyModifiedPropertiesWithoutUndo();

        GameObject nxtObj = new GameObject("NxtEarnManager");
        nxtObj.AddComponent<NxtEarnManager>();

        // ── EventSystem ──────────────────────────────────────────────────────
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── Wire AppShellController ──────────────────────────────────────────
        AppShellController shell = canvasObj.AddComponent<AppShellController>();
        SerializedObject so = new SerializedObject(shell);

        GameObject[] screens = { homeScreen, searchScreen, hubScreen, fanClubScreen, profileScreen };
        SerializedProperty screensProp = so.FindProperty("screens");
        screensProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
            screensProp.GetArrayElementAtIndex(i).objectReferenceValue = screens[i];

        SerializedProperty btnProp = so.FindProperty("navButtons");
        btnProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
            btnProp.GetArrayElementAtIndex(i).objectReferenceValue = navButtons[i];

        SerializedProperty lblProp = so.FindProperty("navLabels");
        lblProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
            lblProp.GetArrayElementAtIndex(i).objectReferenceValue = navLabels[i];

        SerializedProperty dotProp = so.FindProperty("navDots");
        dotProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
            dotProp.GetArrayElementAtIndex(i).objectReferenceValue = navDots[i];

        Color[] accents = { CYAN, CYAN, CYAN, CORAL, SILVER };
        SerializedProperty accProp = so.FindProperty("tabAccents");
        accProp.arraySize = 5;
        for (int i = 0; i < 5; i++)
            accProp.GetArrayElementAtIndex(i).colorValue = accents[i];

        so.FindProperty("nxtBalanceText").objectReferenceValue =
            nxtTextObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("shellRoot").objectReferenceValue = canvasObj;
        so.FindProperty("mainSceneCamera").objectReferenceValue = mainCam;
        so.FindProperty("fadeCanvas").objectReferenceValue = fadeCG;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ── Save scene + register Build Settings ─────────────────────────────
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Scene_Lounge.unity", true)
        };

        Debug.Log("[AppShellBuilder] App shell built, MainScene saved, Build Settings updated. 0 errors expected — press Play.");
    }

    // ═════════════════════════════ SCREENS ═════════════════════════════════

    // ── HOME (spec §5.3) ─────────────────────────────────────────────────────
    static GameObject BuildHomeScreen(RectTransform container)
    {
        GameObject screen = MakeScreenRoot("Screen_Home", container);
        RectTransform content = MakeVScroll(screen.GetComponent<RectTransform>(),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject greet = MakeLayoutText(content, "Txt_Greeting",
            "Good evening, PHO", TEXT_PRIMARY, 24f, FontStyles.Bold, 34f);
        MakeLayoutText(content, "Txt_GreetingSub",
            "Here's what's calling you.", TEXT_SECOND, 14f, FontStyles.Normal, 22f);

        // CONTINUE LISTENING
        MakeSectionLabel(content, "CONTINUE LISTENING");
        RectTransform cl = MakeLayoutPanel(content, "Card_Continue", 72f, BG_SECONDARY);
        GameObject clArt = MakePanel("Art", cl, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero, CYAN_SURFACE, true);
        RectTransform clArtRT = clArt.GetComponent<RectTransform>();
        clArtRT.anchoredPosition = new Vector2(36f, 0f);
        clArtRT.sizeDelta = new Vector2(56f, 56f);
        MakeText("Txt_Track", cl, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
            new Vector2(72f, -16f), new Vector2(-16f, -6f),
            "Night Cruise · MobiLack", TEXT_PRIMARY, 14f, FontStyles.Bold, TextAlignmentOptions.Left);
        MakeText("Txt_Time", cl, new Vector2(0f, 0f), new Vector2(1f, 0.5f),
            new Vector2(72f, 8f), new Vector2(-16f, -2f),
            "3:24 / 5:10", TEXT_SECOND, 11f, FontStyles.Normal, TextAlignmentOptions.Left);
        GameObject bar = MakePanel("Progress", cl, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(72f, 4f), new Vector2(-16f, 8f), new Color(1f, 1f, 1f, 0.08f), false);
        MakePanel("Fill", bar.GetComponent<RectTransform>(),
            Vector2.zero, new Vector2(0.65f, 1f), Vector2.zero, Vector2.zero, CYAN, false);

        // TRENDING NOW
        MakeSectionLabel(content, "TRENDING NOW");
        RectTransform trending = MakeHRow(content, "Row_Trending", 150f);
        string[] tTitles = { "Cyber Heart", "Beyond The Grid", "Neon Nights", "Synth Wave", "Deep House" };
        string[] tArtists = { "MobiLack", "Luna Shadow", "Neon Rider", "Radio 24/7", "Bass Nation" };
        Color[] tColors = { CYAN_SURFACE, CORAL_SURFACE, new Color(0.5f, 0.3f, 1f, 0.15f),
                            CYAN_SURFACE, new Color(1f, 0.7f, 0f, 0.12f) };
        for (int i = 0; i < 5; i++)
            MakeMediaCard(trending, "Card_T" + i, 120f, 120f, tColors[i], tTitles[i], tArtists[i]);

        // MOVIES FOR YOU
        MakeSectionLabel(content, "MOVIES FOR YOU");
        RectTransform movies = MakeHRow(content, "Row_Movies", 130f);
        string[] mTitles = { "Beyond The Horizon", "Blade Runner 2049", "Inception" };
        string[] mYears = { "2024 · Sci-Fi", "2017 · Sci-Fi", "2010 · Thriller" };
        for (int i = 0; i < 3; i++)
            MakeMediaCard(movies, "Card_M" + i, 160f, 100f, CORAL_SURFACE, mTitles[i], mYears[i]);

        // YOUR FAN CLUBS
        MakeSectionLabel(content, "YOUR FAN CLUBS");
        RectTransform clubs = MakeHRow(content, "Row_Clubs", 100f);
        string[] cNames = { "Synth Tribe", "Cyber Collective", "+ Join" };
        for (int i = 0; i < 3; i++)
        {
            GameObject circle = new GameObject("Club_" + i);
            circle.transform.SetParent(clubs, false);
            RectTransform cRT = circle.AddComponent<RectTransform>();
            cRT.sizeDelta = new Vector2(76f, 96f);
            GameObject avatar = MakePanel("Avatar", cRT,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero,
                i == 2 ? BG_TERTIARY : CORAL_SURFACE, false);
            RectTransform aRT = avatar.GetComponent<RectTransform>();
            aRT.pivot = new Vector2(0.5f, 1f);
            aRT.anchoredPosition = new Vector2(0f, -2f);
            aRT.sizeDelta = new Vector2(64f, 64f);
            Image aImg = avatar.GetComponent<Image>();
            aImg.sprite = Knob;
            MakeText("Name", cRT, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(-10f, 0f), new Vector2(10f, 24f),
                cNames[i], TEXT_SECOND, 10f, FontStyles.Normal, TextAlignmentOptions.Center);
        }

        // LIVE NOW
        MakeSectionLabel(content, "LIVE NOW");
        RectTransform live = MakeLayoutPanel(content, "Card_Live", 80f, BG_SECONDARY);
        MakeText("Txt_LiveBadge", live, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero, "● LIVE", CORAL, 12f, FontStyles.Bold, TextAlignmentOptions.Left);
        RectTransform badgeRT = live.Find("Txt_LiveBadge").GetComponent<RectTransform>();
        badgeRT.anchoredPosition = new Vector2(46f, 14f);
        badgeRT.sizeDelta = new Vector2(60f, 20f);
        MakeText("Txt_LiveTitle", live, new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(16f, 8f), new Vector2(-16f, -34f),
            "Neon Nights — Live Set  ·  1.2K watching", TEXT_PRIMARY, 13f,
            FontStyles.Normal, TextAlignmentOptions.Left);

        MakeLayoutSpacer(content, 24f);

        // Luna (top-right, spec §5.3.2)
        CharacterBubble luna = MakeCharacter(screen, "Luna", "LUNA",
            new Color(0.55f, 0.35f, 1f, 1f),
            new Vector2(1f, 1f), new Vector2(-40f, -40f),
            new Vector2(1f, 1f), new Vector2(-16f, -84f),
            "I found something for you →");

        // Controller
        HomeScreenController ctrl = screen.AddComponent<HomeScreenController>();
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("greetingText").objectReferenceValue = greet.GetComponent<TextMeshProUGUI>();
        so.FindProperty("luna").objectReferenceValue = luna;
        so.FindProperty("userName").stringValue = "PHO";
        so.ApplyModifiedPropertiesWithoutUndo();

        return screen;
    }

    // ── SEARCH (spec §5.4) ───────────────────────────────────────────────────
    static GameObject BuildSearchScreen(RectTransform container)
    {
        GameObject screen = MakeScreenRoot("Screen_Search", container);
        RectTransform screenRT = screen.GetComponent<RectTransform>();

        MakeText("Txt_Title", screenRT, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -44f), new Vector2(-16f, -8f),
            "Search", TEXT_PRIMARY, 24f, FontStyles.Bold, TextAlignmentOptions.Left);

        // Search input (TMP_InputField)
        GameObject inputObj = MakePanel("SearchInput", screenRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -96f), new Vector2(-16f, -52f), BG_TERTIARY, true);
        RectTransform inputRT = inputObj.GetComponent<RectTransform>();
        TMP_InputField input = inputObj.AddComponent<TMP_InputField>();
        input.targetGraphic = inputObj.GetComponent<Image>();

        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(inputRT, false);
        RectTransform taRT = textArea.AddComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(14f, 6f); taRT.offsetMax = new Vector2(-14f, -6f);
        textArea.AddComponent<RectMask2D>();

        GameObject placeholder = MakeText("Placeholder", taRT,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "What are you looking for?", TEXT_TERTIARY, 14f,
            FontStyles.Italic, TextAlignmentOptions.Left);
        GameObject inputText = MakeText("Text", taRT,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "", TEXT_PRIMARY, 14f, FontStyles.Normal, TextAlignmentOptions.Left);

        input.textViewport = taRT;
        input.textComponent = inputText.GetComponent<TextMeshProUGUI>();
        input.placeholder = placeholder.GetComponent<TextMeshProUGUI>();

        // Browse panel (scrollable) below input
        GameObject browse = new GameObject("BrowsePanel");
        browse.transform.SetParent(screenRT, false);
        RectTransform browseRT = browse.AddComponent<RectTransform>();
        browseRT.anchorMin = Vector2.zero; browseRT.anchorMax = Vector2.one;
        browseRT.offsetMin = Vector2.zero; browseRT.offsetMax = new Vector2(0f, -104f);
        RectTransform content = MakeVScroll(browseRT,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        MakeSectionLabel(content, "BROWSE CATEGORIES");
        GameObject grid = new GameObject("Grid");
        grid.transform.SetParent(content, false);
        grid.AddComponent<RectTransform>();
        GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(170f, 72f);
        glg.spacing = new Vector2(12f, 12f);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        LayoutElement gridLE = grid.AddComponent<LayoutElement>();
        gridLE.preferredHeight = 3 * 72f + 2 * 12f;
        string[] cats = { "Music", "Movies", "Artists", "Shows", "Fan Clubs", "Live" };
        Color[] catText = { CYAN, CORAL, TEXT_PRIMARY, TEXT_PRIMARY, TEXT_PRIMARY, CORAL };
        Color[] catBg = { CYAN_SURFACE, CORAL_SURFACE, BG_TERTIARY, BG_TERTIARY, BG_TERTIARY, BG_TERTIARY };
        for (int i = 0; i < 6; i++)
        {
            GameObject cat = new GameObject("Cat_" + cats[i]);
            cat.transform.SetParent(grid.transform, false);
            cat.AddComponent<RectTransform>();
            Image catImg = cat.AddComponent<Image>();
            catImg.sprite = Rounded; catImg.type = Image.Type.Sliced;
            catImg.color = catBg[i];
            MakeText("Label", cat.GetComponent<RectTransform>(),
                Vector2.zero, Vector2.one, new Vector2(12f, 0f), Vector2.zero,
                cats[i], catText[i], 15f, FontStyles.Bold, TextAlignmentOptions.Left);
        }

        MakeSectionLabel(content, "RECENT SEARCHES");
        string[] recents = { "Daft Punk", "Inception", "Electronic" };
        foreach (string r in recents)
            MakeLayoutText(content, "Recent_" + r, "·  " + r, TEXT_SECOND, 14f,
                FontStyles.Normal, 28f);

        MakeSectionLabel(content, "TRENDING SEARCHES");
        string[] trends = { "Luna Shadow", "Beyond The Horizon", "Synth Collective" };
        foreach (string t in trends)
            MakeLayoutText(content, "Trend_" + t, "^  " + t, TEXT_SECOND, 14f,
                FontStyles.Normal, 28f);

        MakeLayoutSpacer(content, 24f);

        // Suggestions panel (hidden until typing)
        GameObject suggestions = new GameObject("SuggestionsPanel");
        suggestions.transform.SetParent(screenRT, false);
        RectTransform sugRT = suggestions.AddComponent<RectTransform>();
        sugRT.anchorMin = Vector2.zero; sugRT.anchorMax = Vector2.one;
        sugRT.offsetMin = Vector2.zero; sugRT.offsetMax = new Vector2(0f, -104f);
        RectTransform sugContent = MakeVScroll(sugRT,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        MakeSectionLabel(sugContent, "SUGGESTIONS");
        string[] sugs = { "Daft Punk — Artist", "Daft Punk · Random Access Memories",
                          "Daft Punk · Discovery", "Daft Punk at Coachella — Video" };
        foreach (string s in sugs)
        {
            RectTransform row = MakeLayoutPanel(sugContent, "Sug", 48f, BG_SECONDARY);
            MakeText("Label", row, Vector2.zero, Vector2.one,
                new Vector2(16f, 0f), new Vector2(-16f, 0f),
                s, TEXT_PRIMARY, 13f, FontStyles.Normal, TextAlignmentOptions.Left);
        }
        suggestions.SetActive(false);

        // Petros (bottom-left, spec §5.4)
        CharacterBubble petros = MakeCharacter(screen, "Petros", "PETROS",
            CYAN,
            new Vector2(0f, 0f), new Vector2(40f, 44f),
            new Vector2(0f, 0f), new Vector2(16f, 88f),
            "Need help finding something specific?");

        SearchScreenController ctrl = screen.AddComponent<SearchScreenController>();
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("searchInput").objectReferenceValue = input;
        so.FindProperty("petros").objectReferenceValue = petros;
        so.FindProperty("suggestionsPanel").objectReferenceValue = suggestions;
        so.FindProperty("browsePanel").objectReferenceValue = browse;
        so.ApplyModifiedPropertiesWithoutUndo();

        return screen;
    }

    // ── HUB (spec §5.5 / §5.6) ───────────────────────────────────────────────
    static GameObject BuildHubScreen(RectTransform container)
    {
        GameObject screen = MakeScreenRoot("Screen_Hub", container);
        RectTransform screenRT = screen.GetComponent<RectTransform>();

        MakeText("Txt_Title", screenRT, new Vector2(0f, 1f), new Vector2(0.4f, 1f),
            new Vector2(16f, -44f), new Vector2(0f, -8f),
            "Hub", TEXT_PRIMARY, 24f, FontStyles.Bold, TextAlignmentOptions.Left);

        // Sub-tab switcher
        GameObject musicTab = MakeSimpleButton("Btn_TabMusic", screenRT,
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-150f, -26f), new Vector2(70f, 32f), "Music", CYAN, 14f);
        GameObject videoTab = MakeSimpleButton("Btn_TabVideo", screenRT,
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-70f, -26f), new Vector2(70f, 32f), "Video", TEXT_TERTIARY, 14f);

        // ── Music content ────────────────────────────────────────────────────
        GameObject musicPanel = new GameObject("MusicContent");
        musicPanel.transform.SetParent(screenRT, false);
        RectTransform musicRT = musicPanel.AddComponent<RectTransform>();
        musicRT.anchorMin = Vector2.zero; musicRT.anchorMax = Vector2.one;
        musicRT.offsetMin = Vector2.zero; musicRT.offsetMax = new Vector2(0f, -52f);
        RectTransform mc = MakeVScroll(musicRT,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        MakeSectionLabel(mc, "AI PLAYLIST CHAT");
        RectTransform echoCard = MakeLayoutPanel(mc, "Card_EchoChat", 92f, BG_SECONDARY);
        MakeText("Txt_EchoLine", echoCard, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
            new Vector2(16f, -14f), new Vector2(-16f, -8f),
            "Echo: \"What's the vibe tonight?\"", CYAN, 14f,
            FontStyles.Normal, TextAlignmentOptions.Left);
        string[] chips = { "Chill", "Hype", "Focus" };
        for (int i = 0; i < 3; i++)
        {
            GameObject chip = MakePanel("Chip_" + chips[i], echoCard,
                new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, Vector2.zero,
                CYAN_SURFACE, true);
            RectTransform chipRT = chip.GetComponent<RectTransform>();
            chipRT.pivot = new Vector2(0f, 0f);
            chipRT.anchoredPosition = new Vector2(16f + i * 78f, 12f);
            chipRT.sizeDelta = new Vector2(70f, 28f);
            MakeText("Label", chipRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                chips[i], CYAN, 12f, FontStyles.Normal, TextAlignmentOptions.Center);
        }

        MakeSectionLabel(mc, "YOUR PLAYLISTS");
        MakeListRow(mc, "Liked Songs", "423 songs");
        MakeListRow(mc, "Late Night Vibes", "67 songs");
        MakeListRow(mc, "+ Create Playlist", "");

        MakeSectionLabel(mc, "DJ MIXER");
        RectTransform dj = MakeLayoutPanel(mc, "Card_DJ", 64f, BG_SECONDARY);
        MakeText("Label", dj, Vector2.zero, Vector2.one,
            new Vector2(16f, 0f), new Vector2(-16f, 0f),
            "Open DJ Mode  —  coming soon", TEXT_TERTIARY, 14f,
            FontStyles.Normal, TextAlignmentOptions.Left);

        MakeSectionLabel(mc, "3D SPACES");
        RectTransform loungeCard = MakeLayoutPanel(mc, "Card_Lounge", 64f, new Color(0.25f, 0.2f, 0.02f, 0.5f));
        Image loungeImg = loungeCard.GetComponent<Image>();
        loungeImg.color = new Color(1f, 0.84f, 0f, 0.10f);
        Button loungeBtn = loungeCard.gameObject.AddComponent<Button>();
        loungeBtn.targetGraphic = loungeImg;
        MakeText("Label", loungeCard, Vector2.zero, Vector2.one,
            new Vector2(16f, 0f), new Vector2(-16f, 0f),
            "Enter The Lounge  →", GOLD, 16f, FontStyles.Bold, TextAlignmentOptions.Left);

        MakeSectionLabel(mc, "RECENTLY PLAYED");
        RectTransform recent = MakeHRow(mc, "Row_Recent", 150f);
        string[] rTitles = { "Cyber Heart", "Night Cruise", "Beyond The Grid" };
        string[] rArtists = { "MobiLack", "MobiLack", "Luna Shadow" };
        for (int i = 0; i < 3; i++)
            MakeMediaCard(recent, "Card_R" + i, 120f, 120f, CYAN_SURFACE, rTitles[i], rArtists[i]);

        MakeLayoutSpacer(mc, 24f);

        // ── Video content ────────────────────────────────────────────────────
        GameObject videoPanel = new GameObject("VideoContent");
        videoPanel.transform.SetParent(screenRT, false);
        RectTransform videoRT = videoPanel.AddComponent<RectTransform>();
        videoRT.anchorMin = Vector2.zero; videoRT.anchorMax = Vector2.one;
        videoRT.offsetMin = Vector2.zero; videoRT.offsetMax = new Vector2(0f, -52f);
        RectTransform vc = MakeVScroll(videoRT,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        MakeSectionLabel(vc, "CINEMA SESSION");
        RectTransform cin = MakeLayoutPanel(vc, "Card_Cinema", 84f, CORAL_SURFACE);
        MakeText("Txt_CinTitle", cin, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
            new Vector2(16f, -14f), new Vector2(-16f, -8f),
            "Start Cinema Session", CORAL, 15f, FontStyles.Bold, TextAlignmentOptions.Left);
        MakeText("Txt_CinSub", cin, new Vector2(0f, 0f), new Vector2(1f, 0.5f),
            new Vector2(16f, 10f), new Vector2(-16f, 0f),
            "Watch together, live  —  coming soon", TEXT_TERTIARY, 12f,
            FontStyles.Normal, TextAlignmentOptions.Left);

        MakeSectionLabel(vc, "ACTIVE SESSIONS");
        RectTransform act = MakeLayoutPanel(vc, "Card_Active", 64f, BG_SECONDARY);
        MakeText("Label", act, Vector2.zero, Vector2.one,
            new Vector2(16f, 0f), new Vector2(-16f, 0f),
            "● LIVE   Beyond The Horizon — Alex's Cinema · 5 watching", TEXT_SECOND, 12f,
            FontStyles.Normal, TextAlignmentOptions.Left);

        MakeSectionLabel(vc, "YOUR VIDEO PLAYLISTS");
        MakeListRow(vc, "Sci-Fi Collection", "12 movies");
        MakeListRow(vc, "Watch Later", "8 items");
        MakeListRow(vc, "+ Create Video Playlist", "");

        MakeSectionLabel(vc, "REEL RECOMMENDS");
        RectTransform rec = MakeHRow(vc, "Row_ReelRec", 130f);
        string[] vTitles = { "Beyond The Horizon", "Duna 1984", "Inception" };
        string[] vYears = { "2024 · 2h 12m", "1984 · Sci-Fi", "2010 · Thriller" };
        for (int i = 0; i < 3; i++)
            MakeMediaCard(rec, "Card_V" + i, 160f, 100f, CORAL_SURFACE, vTitles[i], vYears[i]);

        MakeLayoutSpacer(vc, 24f);
        videoPanel.SetActive(false);

        // Characters
        CharacterBubble echo = MakeCharacter(screen, "Echo", "ECHO", CYAN,
            new Vector2(0f, 0f), new Vector2(40f, 44f),
            new Vector2(0f, 0f), new Vector2(16f, 88f),
            "What's the vibe tonight?");
        CharacterBubble reel = MakeCharacter(screen, "Reel", "REEL", CORAL,
            new Vector2(1f, 0f), new Vector2(-40f, 44f),
            new Vector2(1f, 0f), new Vector2(-16f, 88f),
            "Every frame tells a story.");

        HubScreenController ctrl = screen.AddComponent<HubScreenController>();
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("musicTabButton").objectReferenceValue = musicTab.GetComponent<Button>();
        so.FindProperty("videoTabButton").objectReferenceValue = videoTab.GetComponent<Button>();
        so.FindProperty("musicTabLabel").objectReferenceValue =
            musicTab.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        so.FindProperty("videoTabLabel").objectReferenceValue =
            videoTab.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        so.FindProperty("musicContent").objectReferenceValue = musicPanel;
        so.FindProperty("videoContent").objectReferenceValue = videoPanel;
        so.FindProperty("echo").objectReferenceValue = echo;
        so.FindProperty("reel").objectReferenceValue = reel;
        so.FindProperty("enterLoungeButton").objectReferenceValue = loungeBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        return screen;
    }

    // ── FAN CLUB (spec §5.7) ─────────────────────────────────────────────────
    static GameObject BuildFanClubScreen(RectTransform container)
    {
        GameObject screen = MakeScreenRoot("Screen_FanClub", container);
        RectTransform screenRT = screen.GetComponent<RectTransform>();

        MakeText("Txt_Title", screenRT, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -44f), new Vector2(-16f, -8f),
            "Fan Club", TEXT_PRIMARY, 24f, FontStyles.Bold, TextAlignmentOptions.Left);

        GameObject body = new GameObject("Body");
        body.transform.SetParent(screenRT, false);
        RectTransform bodyRT = body.AddComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero; bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = Vector2.zero; bodyRT.offsetMax = new Vector2(0f, -52f);
        RectTransform content = MakeVScroll(bodyRT,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        MakeSectionLabel(content, "YOUR CLUBS");
        RectTransform club1 = MakeLayoutPanel(content, "Card_Club1", 84f, BG_SECONDARY);
        MakeText("T1", club1, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
            new Vector2(16f, -14f), new Vector2(-16f, -8f),
            "Synth Tribe", TEXT_PRIMARY, 15f, FontStyles.Bold, TextAlignmentOptions.Left);
        MakeText("T2", club1, new Vector2(0f, 0f), new Vector2(1f, 0.5f),
            new Vector2(16f, 10f), new Vector2(-16f, 0f),
            "● LIVE Challenge   ·   Level 7 · 2,340 pts", CORAL, 12f,
            FontStyles.Normal, TextAlignmentOptions.Left);

        RectTransform club2 = MakeLayoutPanel(content, "Card_Club2", 84f, BG_SECONDARY);
        MakeText("T1", club2, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
            new Vector2(16f, -14f), new Vector2(-16f, -8f),
            "Cyber Collective", TEXT_PRIMARY, 15f, FontStyles.Bold, TextAlignmentOptions.Left);
        MakeText("T2", club2, new Vector2(0f, 0f), new Vector2(1f, 0.5f),
            new Vector2(16f, 10f), new Vector2(-16f, 0f),
            "New badge earned!   ·   Level 4 · 890 pts", GOLD, 12f,
            FontStyles.Normal, TextAlignmentOptions.Left);

        MakeSectionLabel(content, "DISCOVER CLUBS");
        RectTransform disc = MakeHRow(content, "Row_Discover", 100f);
        string[] dNames = { "Beat Bar", "Neon Riders", "Bass Nation", "Deep House" };
        for (int i = 0; i < 4; i++)
        {
            GameObject circle = new GameObject("Disc_" + i);
            circle.transform.SetParent(disc, false);
            RectTransform cRT = circle.AddComponent<RectTransform>();
            cRT.sizeDelta = new Vector2(76f, 96f);
            GameObject avatar = MakePanel("Avatar", cRT,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero,
                CYAN_SURFACE, false);
            RectTransform aRT = avatar.GetComponent<RectTransform>();
            aRT.pivot = new Vector2(0.5f, 1f);
            aRT.anchoredPosition = new Vector2(0f, -2f);
            aRT.sizeDelta = new Vector2(64f, 64f);
            avatar.GetComponent<Image>().sprite = Knob;
            MakeText("Name", cRT, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(-10f, 0f), new Vector2(10f, 24f),
                dNames[i], TEXT_SECOND, 10f, FontStyles.Normal, TextAlignmentOptions.Center);
        }

        RectTransform create = MakeLayoutPanel(content, "Card_Create", 52f, CORAL_SURFACE);
        MakeText("Label", create, Vector2.zero, Vector2.one,
            new Vector2(16f, 0f), new Vector2(-16f, 0f),
            "+ Create Your Club", CORAL, 14f, FontStyles.Bold, TextAlignmentOptions.Center);

        MakeLayoutSpacer(content, 24f);

        // Vibe (top-right)
        CharacterBubble vibe = MakeCharacter(screen, "Vibe", "VIBE",
            new Color(1f, 0.4f, 0.8f, 1f),
            new Vector2(1f, 1f), new Vector2(-40f, -26f),
            new Vector2(1f, 1f), new Vector2(-16f, -70f),
            "Your tribe is here!");

        FanClubScreenController ctrl = screen.AddComponent<FanClubScreenController>();
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("vibe").objectReferenceValue = vibe;
        so.ApplyModifiedPropertiesWithoutUndo();

        return screen;
    }

    // ── PROFILE (spec §5.8) ──────────────────────────────────────────────────
    static GameObject BuildProfileScreen(RectTransform container)
    {
        GameObject screen = MakeScreenRoot("Screen_Profile", container);
        RectTransform content = MakeVScroll(screen.GetComponent<RectTransform>(),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Banner + identity
        RectTransform banner = MakeLayoutPanel(content, "Banner", 110f, new Color(0.1f, 0.05f, 0.2f, 1f));
        GameObject avatar = MakePanel("Avatar", banner,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero,
            SILVER, false);
        RectTransform avRT = avatar.GetComponent<RectTransform>();
        avRT.anchoredPosition = new Vector2(56f, 0f);
        avRT.sizeDelta = new Vector2(72f, 72f);
        avatar.GetComponent<Image>().sprite = Knob;
        MakeText("Txt_Name", banner, new Vector2(0f, 0.5f), new Vector2(1f, 1f),
            new Vector2(104f, -30f), new Vector2(-16f, -22f),
            "PHO", TEXT_PRIMARY, 20f, FontStyles.Bold, TextAlignmentOptions.Left);
        MakeText("Txt_Handle", banner, new Vector2(0f, 0f), new Vector2(1f, 0.5f),
            new Vector2(104f, 18f), new Vector2(-16f, 4f),
            "@pho  ·  Building the future of sound", TEXT_SECOND, 12f,
            FontStyles.Normal, TextAlignmentOptions.Left);

        // Stats trio
        GameObject trio = new GameObject("StatsTrio");
        trio.transform.SetParent(content, false);
        trio.AddComponent<RectTransform>();
        HorizontalLayoutGroup hlg = trio.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        LayoutElement trioLE = trio.AddComponent<LayoutElement>();
        trioLE.preferredHeight = 64f;
        string[] nums = { "423", "89", "12" };
        string[] labels = { "Songs", "Films", "Clubs" };
        for (int i = 0; i < 3; i++)
        {
            GameObject box = new GameObject("Stat_" + labels[i]);
            box.transform.SetParent(trio.transform, false);
            box.AddComponent<RectTransform>();
            Image boxImg = box.AddComponent<Image>();
            boxImg.sprite = Rounded; boxImg.type = Image.Type.Sliced;
            boxImg.color = BG_SECONDARY;
            MakeText("Num", box.GetComponent<RectTransform>(),
                new Vector2(0f, 0.45f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -6f),
                nums[i], TEXT_PRIMARY, 18f, FontStyles.Bold, TextAlignmentOptions.Center);
            MakeText("Lbl", box.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(1f, 0.45f), new Vector2(0f, 6f), Vector2.zero,
                labels[i], TEXT_TERTIARY, 11f, FontStyles.Normal, TextAlignmentOptions.Center);
        }

        // Stats
        MakeSectionLabel(content, "STATS");
        MakeLayoutText(content, "Stat1", "Top Genre:  Electronic", TEXT_SECOND, 14f, FontStyles.Normal, 26f);
        MakeLayoutText(content, "Stat2", "Top Category:  Sci-Fi", TEXT_SECOND, 14f, FontStyles.Normal, 26f);
        MakeLayoutText(content, "Stat3", "Streak:  14 days", TEXT_SECOND, 14f, FontStyles.Normal, 26f);
        GameObject nxtStat = MakeLayoutText(content, "Txt_NxtEarned",
            "NXT Earned: 0", GOLD, 14f, FontStyles.Bold, 26f);

        // Badges
        MakeSectionLabel(content, "BADGES");
        RectTransform badges = MakeHRow(content, "Row_Badges", 56f);
        Color[] bColors = { GOLD, CYAN, CORAL, SILVER };
        for (int i = 0; i < 4; i++)
        {
            GameObject b = new GameObject("Badge_" + i);
            b.transform.SetParent(badges, false);
            RectTransform bRT = b.AddComponent<RectTransform>();
            bRT.sizeDelta = new Vector2(44f, 44f);
            Image bImg = b.AddComponent<Image>();
            bImg.sprite = Knob;
            bImg.color = new Color(bColors[i].r, bColors[i].g, bColors[i].b, 0.35f);
        }

        // Library
        MakeSectionLabel(content, "MY LIBRARY");
        MakeListRow(content, "Playlists", "");
        MakeListRow(content, "Downloads", "");
        MakeListRow(content, "History", "");
        MakeListRow(content, "Liked", "");

        RectTransform nxtRow = MakeLayoutPanel(content, "Card_NxtDash", 52f, new Color(1f, 0.84f, 0f, 0.08f));
        MakeText("Label", nxtRow, Vector2.zero, Vector2.one,
            new Vector2(16f, 0f), new Vector2(-16f, 0f),
            "NXT Token Dashboard  →", GOLD, 14f, FontStyles.Bold, TextAlignmentOptions.Left);

        MakeLayoutSpacer(content, 24f);

        // Mirror (right side)
        CharacterBubble mirror = MakeCharacter(screen, "Mirror", "MIRROR", SILVER,
            new Vector2(1f, 1f), new Vector2(-40f, -140f),
            new Vector2(1f, 1f), new Vector2(-16f, -184f),
            "Every session tells me a little more about you.");

        ProfileScreenController ctrl = screen.AddComponent<ProfileScreenController>();
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("mirror").objectReferenceValue = mirror;
        so.FindProperty("nxtEarnedText").objectReferenceValue =
            nxtStat.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedPropertiesWithoutUndo();

        return screen;
    }

    // ═════════════════════════════ HELPERS ═════════════════════════════════

    static GameObject MakeScreenRoot(string name, RectTransform container)
    {
        GameObject screen = new GameObject(name);
        screen.transform.SetParent(container, false);
        RectTransform rt = screen.AddComponent<RectTransform>();
        Stretch(rt);
        return screen;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Vertical scroll area; returns the layout content RT.</summary>
    static RectTransform MakeVScroll(RectTransform parent,
        Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        GameObject scrollObj = new GameObject("Scroll");
        scrollObj.transform.SetParent(parent, false);
        RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
        scrollRT.anchorMin = aMin; scrollRT.anchorMax = aMax;
        scrollRT.offsetMin = oMin; scrollRT.offsetMax = oMax;
        Image hit = scrollObj.AddComponent<Image>();
        hit.color = CLEAR_HIT;
        scrollObj.AddComponent<RectMask2D>();
        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollRT, false);
        RectTransform content = contentObj.AddComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 12, 12);
        vlg.spacing = 10f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = scrollRT;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 20f;

        return content;
    }

    /// <summary>Horizontal card row inside a vertical layout; returns row content RT.</summary>
    static RectTransform MakeHRow(RectTransform verticalContent, string name, float height)
    {
        GameObject rowObj = new GameObject(name);
        rowObj.transform.SetParent(verticalContent, false);
        RectTransform rowRT = rowObj.AddComponent<RectTransform>();
        LayoutElement le = rowObj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        Image hit = rowObj.AddComponent<Image>();
        hit.color = CLEAR_HIT;
        rowObj.AddComponent<RectMask2D>();
        ScrollRect scroll = rowObj.AddComponent<ScrollRect>();

        GameObject contentObj = new GameObject("RowContent");
        contentObj.transform.SetParent(rowRT, false);
        RectTransform content = contentObj.AddComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 0.5f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        HorizontalLayoutGroup hlg = contentObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = rowRT;
        scroll.content = content;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 20f;

        return content;
    }

    /// <summary>Media card (art bg + title + subtitle) for horizontal rows.</summary>
    static GameObject MakeMediaCard(RectTransform rowContent, string name,
        float w, float h, Color bg, string title, string subtitle)
    {
        GameObject card = new GameObject(name);
        card.transform.SetParent(rowContent, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        Image img = card.AddComponent<Image>();
        img.sprite = Rounded; img.type = Image.Type.Sliced;
        img.color = bg;

        MakeText("Title", rt, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(8f, 20f), new Vector2(-8f, 38f),
            title, TEXT_PRIMARY, 12f, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
        MakeText("Sub", rt, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(8f, 4f), new Vector2(-8f, 20f),
            subtitle, TEXT_SECOND, 10f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        return card;
    }

    /// <summary>Full-width list row card (title left, meta right).</summary>
    static RectTransform MakeListRow(RectTransform verticalContent, string title, string meta)
    {
        RectTransform row = MakeLayoutPanel(verticalContent, "Row_" + title, 52f, BG_SECONDARY);
        MakeText("Title", row, Vector2.zero, Vector2.one,
            new Vector2(16f, 0f), new Vector2(-90f, 0f),
            title, TEXT_PRIMARY, 14f, FontStyles.Normal, TextAlignmentOptions.Left);
        if (!string.IsNullOrEmpty(meta))
            MakeText("Meta", row, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-90f, 0f), new Vector2(-16f, 0f),
                meta, TEXT_TERTIARY, 12f, FontStyles.Normal, TextAlignmentOptions.Right);
        return row;
    }

    /// <summary>Rounded panel as a vertical-layout child; returns its RT.</summary>
    static RectTransform MakeLayoutPanel(RectTransform verticalContent, string name,
        float height, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(verticalContent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        Image img = go.AddComponent<Image>();
        img.sprite = Rounded; img.type = Image.Type.Sliced;
        img.color = color;
        return rt;
    }

    /// <summary>Section header label (spec §5.3.2 style).</summary>
    static void MakeSectionLabel(RectTransform verticalContent, string label)
    {
        GameObject go = MakeLayoutText(verticalContent, "Sec_" + label, label,
            TEXT_TERTIARY, 11f, FontStyles.Bold, 26f);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.characterSpacing = 8f;
        tmp.alignment = TextAlignmentOptions.BottomLeft;
    }

    /// <summary>TMP text as a vertical-layout child with fixed preferred height.</summary>
    static GameObject MakeLayoutText(RectTransform verticalContent, string name,
        string text, Color color, float size, FontStyles style, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(verticalContent, false);
        go.AddComponent<RectTransform>();
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return go;
    }

    static void MakeLayoutSpacer(RectTransform verticalContent, float height)
    {
        GameObject go = new GameObject("Spacer");
        go.transform.SetParent(verticalContent, false);
        go.AddComponent<RectTransform>();
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    /// <summary>Floating character avatar + hidden speech bubble; wired CharacterBubble.</summary>
    static CharacterBubble MakeCharacter(GameObject screen, string objName,
        string charName, Color accent,
        Vector2 avatarAnchor, Vector2 avatarPos,
        Vector2 bubbleAnchor, Vector2 bubblePos,
        string defaultLine)
    {
        RectTransform screenRT = screen.GetComponent<RectTransform>();

        GameObject charObj = new GameObject("Char_" + objName);
        charObj.transform.SetParent(screenRT, false);
        RectTransform charRT = charObj.AddComponent<RectTransform>();
        Stretch(charRT);
        // Non-blocking container
        charObj.AddComponent<CanvasGroup>().blocksRaycasts = true;

        // Avatar circle
        GameObject avatarObj = new GameObject("Avatar");
        avatarObj.transform.SetParent(charRT, false);
        RectTransform avRT = avatarObj.AddComponent<RectTransform>();
        avRT.anchorMin = avatarAnchor; avRT.anchorMax = avatarAnchor;
        avRT.anchoredPosition = avatarPos;
        avRT.sizeDelta = new Vector2(56f, 56f);
        Image avImg = avatarObj.AddComponent<Image>();
        avImg.sprite = Knob;
        avImg.color = new Color(accent.r, accent.g, accent.b, 0.9f);
        Button avBtn = avatarObj.AddComponent<Button>();
        avBtn.targetGraphic = avImg;

        // Inner dot (simple face placeholder)
        GameObject inner = new GameObject("Inner");
        inner.transform.SetParent(avRT, false);
        RectTransform inRT = inner.AddComponent<RectTransform>();
        inRT.anchorMin = new Vector2(0.5f, 0.5f); inRT.anchorMax = new Vector2(0.5f, 0.5f);
        inRT.sizeDelta = new Vector2(24f, 24f);
        Image inImg = inner.AddComponent<Image>();
        inImg.sprite = Knob;
        inImg.color = new Color(0.04f, 0.04f, 0.06f, 0.9f);
        inImg.raycastTarget = false;

        // Bubble
        GameObject bubbleObj = new GameObject("Bubble");
        bubbleObj.transform.SetParent(charRT, false);
        RectTransform bubRT = bubbleObj.AddComponent<RectTransform>();
        bubRT.anchorMin = bubbleAnchor; bubRT.anchorMax = bubbleAnchor;
        bubRT.pivot = bubbleAnchor;
        bubRT.anchoredPosition = bubblePos;
        bubRT.sizeDelta = new Vector2(250f, 64f);
        Image bubImg = bubbleObj.AddComponent<Image>();
        bubImg.sprite = Rounded; bubImg.type = Image.Type.Sliced;
        bubImg.color = new Color(0.1f, 0.1f, 0.15f, 0.97f);

        GameObject nameObj = MakeText("Name", bubRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(12f, -22f), new Vector2(-12f, -4f),
            charName, accent, 11f, FontStyles.Bold, TextAlignmentOptions.Left);
        GameObject msgObj = MakeText("Message", bubRT,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(12f, 6f), new Vector2(-12f, -24f),
            defaultLine, TEXT_PRIMARY, 13f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        TextMeshProUGUI msgTMP = msgObj.GetComponent<TextMeshProUGUI>();
        msgTMP.textWrappingMode = TextWrappingModes.Normal;
        msgTMP.overflowMode = TextOverflowModes.Ellipsis;

        bubbleObj.SetActive(false);

        CharacterBubble cb = charObj.AddComponent<CharacterBubble>();
        SerializedObject so = new SerializedObject(cb);
        so.FindProperty("characterName").stringValue = charName;
        so.FindProperty("defaultLine").stringValue = defaultLine;
        so.FindProperty("avatarImage").objectReferenceValue = avImg;
        so.FindProperty("avatarButton").objectReferenceValue = avBtn;
        so.FindProperty("bubbleRoot").objectReferenceValue = bubbleObj;
        so.FindProperty("bubbleNameText").objectReferenceValue = nameObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("bubbleMessageText").objectReferenceValue = msgTMP;
        so.ApplyModifiedPropertiesWithoutUndo();

        return cb;
    }

    static GameObject MakePanel(string name, RectTransform parent,
        Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax,
        Color color, bool rounded)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
        Image img = go.AddComponent<Image>();
        if (rounded) { img.sprite = Rounded; img.type = Image.Type.Sliced; }
        img.color = color;
        return go;
    }

    static GameObject MakeSimpleButton(string name, RectTransform parent,
        Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size,
        string label, Color textColor, float fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.sprite = Rounded; img.type = Image.Type.Sliced;
        img.color = new Color(1f, 1f, 1f, 0.04f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        GameObject lbl = MakeText("Label", rt, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, label, textColor, fontSize,
            FontStyles.Bold, TextAlignmentOptions.Center);
        lbl.GetComponent<TextMeshProUGUI>().raycastTarget = false;
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
        return go;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
