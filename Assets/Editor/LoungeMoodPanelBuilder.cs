using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// AUDIOVIDO — Mood Panel + NXT Balance Builder
/// Menu: AUDIOVIDO → Build Mood Panel
///
/// Adds to the existing LoungeUICanvas:
///   • MoodPanel (slides up above bottom bar when DRIFT greets)
///       [Melancholic] [Energetic] [Nostalgic] [Chill]
///   • Txt_NxtBalance in the top bar (gold #FFD700)
///
/// Run AFTER "Build Lounge UI" has already created LoungeUICanvas.
/// Then re-run "Wire Lounge References" to wire the new fields.
/// </summary>
public static class LoungeMoodPanelBuilder
{
    static readonly Color CYAN        = Hex("00D4FF");
    static readonly Color AMBER       = Hex("FFD700");
    static readonly Color TEXT_WHITE  = Hex("FFFFFF");
    static readonly Color TEXT_DIM    = Hex("A0A0B8");
    static readonly Color PANEL_BG    = new Color(0.067f, 0.067f, 0.094f, 0.97f);

    // Mood button accent colours
    static readonly Color COL_MELANCHOLIC = Hex("4A90D9"); // muted blue
    static readonly Color COL_ENERGETIC   = Hex("FF6B35"); // warm orange
    static readonly Color COL_NOSTALGIC   = Hex("9B59B6"); // purple
    static readonly Color COL_CHILL       = Hex("00D4FF"); // cyan (same as brand)

    [MenuItem("AUDIOVIDO/Build Mood Panel")]
    public static void Build()
    {
        // Find the canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found. Run 'Build Lounge UI' first.", "OK");
            return;
        }
        RectTransform canvasRT = canvas.GetComponent<RectTransform>();

        // ── NXT Balance label in top bar ──────────────────────────────────────
        // Placed left of user count in the TopBar
        var topBar = GameObject.Find("TopBar");
        if (topBar != null)
        {
            var topRT = topBar.GetComponent<RectTransform>();
            if (GameObject.Find("Txt_NxtBalance") == null)
            {
                var nxtObj = new GameObject("Txt_NxtBalance");
                nxtObj.transform.SetParent(topRT, false);
                var rt = nxtObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-158f, 0f);
                rt.sizeDelta = new Vector2(80f, 40f);
                var tmp = nxtObj.AddComponent<TextMeshProUGUI>();
                tmp.text = "0 NXT";
                tmp.color = AMBER;
                tmp.fontSize = 13f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Right;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                Debug.Log("[MoodPanelBuilder] Txt_NxtBalance added to TopBar.");
            }
            else Debug.Log("[MoodPanelBuilder] Txt_NxtBalance already exists.");
        }
        else Debug.LogWarning("[MoodPanelBuilder] TopBar not found — Txt_NxtBalance skipped.");

        // ── Mood Panel ─────────────────────────────────────────────────────────
        if (GameObject.Find("MoodPanel") != null)
        {
            EditorUtility.DisplayDialog("Already exists", "MoodPanel already in scene.", "OK");
            return;
        }

        // Root panel — anchored just above bottom bar (90px), slides up 160px when active
        GameObject panel = new GameObject("MoodPanel");
        panel.transform.SetParent(canvasRT, false);
        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 0f);
        panelRT.anchorMax = new Vector2(1f, 0f);
        panelRT.pivot     = new Vector2(0.5f, 0f);
        panelRT.anchoredPosition = new Vector2(0f, 96f);   // sits just above bottom bar
        panelRT.sizeDelta = new Vector2(0f, 100f);

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = PANEL_BG;

        // Header label
        var header = MakeText("Txt_MoodHeader", panelRT,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -28f), new Vector2(0f, -4f),
            new Vector2(0.5f, 1f),
            "How are you feeling?", TEXT_DIM, 11f, FontStyles.Bold, TextAlignmentOptions.Center);

        // 4 mood buttons
        float btnW    = 80f;
        float btnH    = 44f;
        float spacing = 8f;
        float totalW  = 4 * btnW + 3 * spacing;
        float startX  = -totalW / 2f + btnW / 2f;
        float btnY    = -66f; // from top of panel

        string[] labels = { "Melancholic", "Energetic", "Nostalgic", "Chill" };
        string[] names  = { "Btn_Melancholic", "Btn_Energetic", "Btn_Nostalgic", "Btn_Chill" };
        Color[] colors  = { COL_MELANCHOLIC, COL_ENERGETIC, COL_NOSTALGIC, COL_CHILL };

        for (int i = 0; i < 4; i++)
        {
            float x = startX + i * (btnW + spacing);
            MakeMoodButton(names[i], panelRT, x, btnY, btnW, btnH, labels[i], colors[i]);
        }

        // Hide by default (LoungeUIController will reveal it)
        panel.SetActive(false);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[MoodPanelBuilder] MoodPanel + Txt_NxtBalance added.");
        EditorUtility.DisplayDialog("Done",
            "MoodPanel and NXT label built!\n\n" +
            "Now run:\n  AUDIOVIDO → Wire Lounge References\n" +
            "to auto-assign the new fields in LoungeUIController.", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject MakeMoodButton(string name, RectTransform parent,
        float x, float y, float w, float h, string label, Color accentColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        var img = go.AddComponent<Image>();
        img.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.18f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        // Label
        var lblObj = new GameObject("Label");
        lblObj.transform.SetParent(go.transform, false);
        var lrt = lblObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var tmp = lblObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.color     = accentColor;
        tmp.fontSize  = 11f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return go;
    }

    static GameObject MakeText(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Vector2 pivot,
        string text, Color color, float fontSize,
        FontStyles style, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot     = pivot;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.color = color;
        tmp.fontSize = fontSize; tmp.fontStyle = style;
        tmp.alignment = alignment;
        return go;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
