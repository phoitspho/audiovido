using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// AUDIOVIDO — Track Info Card Builder
/// Menu: AUDIOVIDO → Build Track Card
///
/// Adds TrackCard panel to LoungeUICanvas:
///   • Slides in from the right when a mood is picked and the track changes
///   • Shows: coloured album-art placeholder | Track name | Artist name
///   • Auto-dismisses after 4s
///
/// Run AFTER "Build Lounge UI" has been executed.
/// Then re-run "Wire Lounge References".
/// </summary>
public static class LoungeTrackCardBuilder
{
    static readonly Color AMBER      = Hex("FFD700");
    static readonly Color TEXT_WHITE = Hex("FFFFFF");
    static readonly Color TEXT_DIM   = Hex("A0A0B8");
    static readonly Color PANEL_BG   = new Color(0.08f, 0.08f, 0.12f, 0.96f);

    [MenuItem("AUDIOVIDO/Build Track Card")]
    public static void Build()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "No Canvas found. Run 'Build Lounge UI' first.", "OK");
            return;
        }

        if (GameObject.Find("TrackCard") != null)
        {
            EditorUtility.DisplayDialog("Already exists", "TrackCard already in scene.", "OK");
            return;
        }

        RectTransform canvasRT = canvas.GetComponent<RectTransform>();

        // ── Card root ─────────────────────────────────────────────────────────
        // Anchored to right side, vertically centered-ish (above bottom bar)
        GameObject card = new GameObject("TrackCard");
        card.transform.SetParent(canvasRT, false);
        var cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(1f, 0f);
        cardRT.anchorMax = new Vector2(1f, 0f);
        cardRT.pivot     = new Vector2(1f, 0f);
        cardRT.anchoredPosition = new Vector2(0f, 100f);   // just above bottom bar
        cardRT.sizeDelta = new Vector2(240f, 64f);

        var cardImg = card.AddComponent<Image>();
        cardImg.color = PANEL_BG;

        // ── Album art placeholder (left square) ───────────────────────────────
        GameObject art = new GameObject("AlbumArt");
        art.transform.SetParent(card.transform, false);
        var artRT = art.AddComponent<RectTransform>();
        artRT.anchorMin = new Vector2(0f, 0.5f);
        artRT.anchorMax = new Vector2(0f, 0.5f);
        artRT.pivot     = new Vector2(0f, 0.5f);
        artRT.anchoredPosition = new Vector2(10f, 0f);
        artRT.sizeDelta = new Vector2(44f, 44f);
        var artImg = art.AddComponent<Image>();
        artImg.color = new Color(AMBER.r, AMBER.g, AMBER.b, 0.3f); // gold placeholder

        // Small music note label on art placeholder
        var noteObj = new GameObject("ArtIcon");
        noteObj.transform.SetParent(art.transform, false);
        var noteRT = noteObj.AddComponent<RectTransform>();
        noteRT.anchorMin = Vector2.zero; noteRT.anchorMax = Vector2.one;
        noteRT.offsetMin = Vector2.zero; noteRT.offsetMax = Vector2.zero;
        var noteTMP = noteObj.AddComponent<TextMeshProUGUI>();
        noteTMP.text = "#";
        noteTMP.color = AMBER;
        noteTMP.fontSize = 20f;
        noteTMP.fontStyle = FontStyles.Bold;
        noteTMP.alignment = TextAlignmentOptions.Center;

        // ── Track name label ──────────────────────────────────────────────────
        GameObject trackNameObj = new GameObject("Txt_CardTrack");
        trackNameObj.transform.SetParent(card.transform, false);
        var trackRT = trackNameObj.AddComponent<RectTransform>();
        trackRT.anchorMin = new Vector2(0f, 0.5f);
        trackRT.anchorMax = new Vector2(1f, 0.5f);
        trackRT.pivot     = new Vector2(0f, 0f);
        trackRT.anchoredPosition = new Vector2(64f, 4f);
        trackRT.sizeDelta = new Vector2(-74f, 24f);
        var trackTMP = trackNameObj.AddComponent<TextMeshProUGUI>();
        trackTMP.text = "Track Name";
        trackTMP.color = TEXT_WHITE;
        trackTMP.fontSize = 13f;
        trackTMP.fontStyle = FontStyles.Bold;
        trackTMP.alignment = TextAlignmentOptions.Left;
        trackTMP.textWrappingMode = TextWrappingModes.NoWrap;
        trackTMP.overflowMode = TextOverflowModes.Ellipsis;

        // ── Artist name label ─────────────────────────────────────────────────
        GameObject artistObj = new GameObject("Txt_CardArtist");
        artistObj.transform.SetParent(card.transform, false);
        var artistRT = artistObj.AddComponent<RectTransform>();
        artistRT.anchorMin = new Vector2(0f, 0.5f);
        artistRT.anchorMax = new Vector2(1f, 0.5f);
        artistRT.pivot     = new Vector2(0f, 1f);
        artistRT.anchoredPosition = new Vector2(64f, 0f);
        artistRT.sizeDelta = new Vector2(-74f, 20f);
        var artistTMP = artistObj.AddComponent<TextMeshProUGUI>();
        artistTMP.text = "Artist";
        artistTMP.color = TEXT_DIM;
        artistTMP.fontSize = 11f;
        artistTMP.fontStyle = FontStyles.Normal;
        artistTMP.alignment = TextAlignmentOptions.Left;
        artistTMP.textWrappingMode = TextWrappingModes.NoWrap;
        artistTMP.overflowMode = TextOverflowModes.Ellipsis;

        // ── NowPlaying indicator ──────────────────────────────────────────────
        GameObject npDot = new GameObject("NowPlayingDot");
        npDot.transform.SetParent(card.transform, false);
        var dotRT = npDot.AddComponent<RectTransform>();
        dotRT.anchorMin = new Vector2(1f, 1f);
        dotRT.anchorMax = new Vector2(1f, 1f);
        dotRT.pivot     = new Vector2(1f, 1f);
        dotRT.anchoredPosition = new Vector2(-6f, -6f);
        dotRT.sizeDelta = new Vector2(6f, 6f);
        var dotImg = npDot.AddComponent<Image>();
        dotImg.color = Hex("00D4FF"); // cyan playing indicator

        // Start hidden off-screen to the right
        card.SetActive(false);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[LoungeTrackCardBuilder] TrackCard built.");
        EditorUtility.DisplayDialog("Done",
            "TrackCard built!\n\nRun AUDIOVIDO → Wire Lounge References\nto connect it to LoungeUIController.", "OK");
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
