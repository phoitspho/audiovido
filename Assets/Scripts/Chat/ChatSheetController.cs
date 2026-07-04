using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AUDIOVIDO — AI Character Chat Bottom Sheet (spec §4.2)
///
///   [Avatar] ECHO
///   "What kind of vibe tonight?"
///   [Chat messages scroll area]
///   [Quick reply chips]
///   [Text input] [Send]
///
/// Built ENTIRELY at runtime into the hosting space's canvas, so no scene
/// needs rebuilding — every district space gets chat for free. Slides up
/// per spec §9.1 (250ms), backdrop tap or ✕ closes it. Replies come from
/// CharacterChatService (§11.7 contract).
/// </summary>
public class ChatSheetController : MonoBehaviour
{
    static ChatSheetController _openSheet; // one at a time

    const float SLIDE_SECONDS = 0.25f;     // spec §9.1 "Default: 250ms"
    const float SHEET_FRACTION = 0.62f;    // between half and full (§6.4)

    static readonly Color SHEET_BG   = new Color(0.067f, 0.067f, 0.094f, 0.98f); // --bg-secondary
    static readonly Color TEXT_DIM   = new Color(0.63f, 0.63f, 0.72f, 1f);
    static readonly Color BUBBLE_CHAR = new Color(1f, 1f, 1f, 0.05f);

    string _characterId;
    string _screenState;
    CharacterProfile _profile;

    RectTransform _sheet;
    RectTransform _messagesContent;
    ScrollRect _scroll;
    TMP_InputField _input;
    TextMeshProUGUI _typingText;   // the "..." bubble awaiting a reply
    bool _busy, _closing;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Open the chat sheet for a character inside the given canvas.</summary>
    public static void Open(string characterId, Canvas hostCanvas, string screenState)
    {
        if (_openSheet != null || hostCanvas == null) return;
        CharacterProfile profile = CharacterProfiles.Get(characterId);
        if (profile == null)
        {
            Debug.LogWarning($"[Chat] Unknown character '{characterId}'");
            return;
        }
        CharacterChatService.EnsureExists();

        GameObject root = new GameObject("ChatSheet_" + characterId);
        root.transform.SetParent(hostCanvas.transform, false);
        ChatSheetController sheet = root.AddComponent<ChatSheetController>();
        sheet._characterId = characterId;
        sheet._profile = profile;
        sheet._screenState = screenState;
        sheet.BuildUI();
        _openSheet = sheet;
    }

    /// <summary>Runtime "Chat" button factory for existing space toolbars.</summary>
    public static Button AddChatButton(RectTransform parent, Vector2 anchoredPos,
        Vector2 size, Color accent)
    {
        GameObject go = new GameObject("Btn_Chat_Runtime");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.color = new Color(accent.r, accent.g, accent.b, 0.16f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        MakeLabel("Label", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "Chat", accent, 13f, FontAlign(TextAlignmentOptions.Center), true);
        return btn;
    }

    // ── UI construction ──────────────────────────────────────────────────────

    void BuildUI()
    {
        RectTransform rootRT = gameObject.AddComponent<RectTransform>();
        Stretch(rootRT);

        // Backdrop — tap to dismiss (spec §6.4 backdrop rgba(0,0,0,0.6))
        GameObject backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(rootRT, false);
        RectTransform bdRT = backdrop.AddComponent<RectTransform>();
        Stretch(bdRT);
        Image bdImg = backdrop.AddComponent<Image>();
        bdImg.color = new Color(0f, 0f, 0f, 0.6f);
        Button bdBtn = backdrop.AddComponent<Button>();
        bdBtn.targetGraphic = bdImg;
        bdBtn.transition = Selectable.Transition.None;
        bdBtn.onClick.AddListener(Close);

        // Sheet
        GameObject sheetObj = new GameObject("Sheet");
        sheetObj.transform.SetParent(rootRT, false);
        _sheet = sheetObj.AddComponent<RectTransform>();
        _sheet.anchorMin = new Vector2(0f, 0f);
        _sheet.anchorMax = new Vector2(1f, SHEET_FRACTION);
        _sheet.offsetMin = Vector2.zero;
        _sheet.offsetMax = Vector2.zero;
        Image sheetImg = sheetObj.AddComponent<Image>();
        sheetImg.color = SHEET_BG;

        // Drag handle (spec §6.4: 40×4, centered top)
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(_sheet, false);
        RectTransform hRT = handle.AddComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0.5f, 1f);
        hRT.anchorMax = new Vector2(0.5f, 1f);
        hRT.pivot = new Vector2(0.5f, 1f);
        hRT.anchoredPosition = new Vector2(0f, -8f);
        hRT.sizeDelta = new Vector2(40f, 4f);
        handle.AddComponent<Image>().color = TEXT_DIM;

        // Header: avatar dot + character name + close
        GameObject avatar = new GameObject("Avatar");
        avatar.transform.SetParent(_sheet, false);
        RectTransform avRT = avatar.AddComponent<RectTransform>();
        avRT.anchorMin = new Vector2(0f, 1f);
        avRT.anchorMax = new Vector2(0f, 1f);
        avRT.pivot = new Vector2(0f, 1f);
        avRT.anchoredPosition = new Vector2(16f, -20f);
        avRT.sizeDelta = new Vector2(26f, 26f);
        avatar.AddComponent<Image>().color = _profile.accent;

        MakeLabel("Txt_Name", _sheet,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(52f, -48f), new Vector2(-56f, -18f),
            _profile.displayName, _profile.accent, 15f,
            FontAlign(TextAlignmentOptions.Left), true);

        GameObject closeObj = new GameObject("Btn_Close");
        closeObj.transform.SetParent(_sheet, false);
        RectTransform clRT = closeObj.AddComponent<RectTransform>();
        clRT.anchorMin = new Vector2(1f, 1f);
        clRT.anchorMax = new Vector2(1f, 1f);
        clRT.pivot = new Vector2(1f, 1f);
        clRT.anchoredPosition = new Vector2(-10f, -10f);
        clRT.sizeDelta = new Vector2(40f, 40f);
        Image clImg = closeObj.AddComponent<Image>();
        clImg.color = new Color(1f, 1f, 1f, 0.05f);
        Button clBtn = closeObj.AddComponent<Button>();
        clBtn.targetGraphic = clImg;
        clBtn.onClick.AddListener(Close);
        MakeLabel("Label", clRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "X", TEXT_DIM, 14f, FontAlign(TextAlignmentOptions.Center), true);

        // Messages scroll area (between header and chips+input)
        GameObject scrollObj = new GameObject("Messages");
        scrollObj.transform.SetParent(_sheet, false);
        RectTransform scRT = scrollObj.AddComponent<RectTransform>();
        scRT.anchorMin = new Vector2(0f, 0f);
        scRT.anchorMax = new Vector2(1f, 1f);
        scRT.offsetMin = new Vector2(0f, 104f);   // above chips + input
        scRT.offsetMax = new Vector2(0f, -56f);   // below header
        Image scHit = scrollObj.AddComponent<Image>();
        scHit.color = new Color(0f, 0f, 0f, 0.001f);
        scrollObj.AddComponent<RectMask2D>();
        _scroll = scrollObj.AddComponent<ScrollRect>();

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scRT, false);
        _messagesContent = contentObj.AddComponent<RectTransform>();
        _messagesContent.anchorMin = new Vector2(0f, 1f);
        _messagesContent.anchorMax = new Vector2(1f, 1f);
        _messagesContent.pivot = new Vector2(0.5f, 1f);
        _messagesContent.offsetMin = Vector2.zero;
        _messagesContent.offsetMax = Vector2.zero;
        VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 8, 8);
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scroll.viewport = scRT;
        _scroll.content = _messagesContent;
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.scrollSensitivity = 20f;

        // Quick reply chips (spec §4.2)
        GameObject chipsRow = new GameObject("Chips");
        chipsRow.transform.SetParent(_sheet, false);
        RectTransform chRT = chipsRow.AddComponent<RectTransform>();
        chRT.anchorMin = new Vector2(0f, 0f);
        chRT.anchorMax = new Vector2(1f, 0f);
        chRT.pivot = new Vector2(0.5f, 0f);
        chRT.offsetMin = new Vector2(12f, 60f);
        chRT.offsetMax = new Vector2(-12f, 96f);
        HorizontalLayoutGroup hlg = chipsRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        foreach (string chip in _profile.chips)
        {
            string captured = chip;
            GameObject chipObj = new GameObject("Chip");
            chipObj.transform.SetParent(chRT, false);
            chipObj.AddComponent<RectTransform>();
            Image chipImg = chipObj.AddComponent<Image>();
            chipImg.color = new Color(_profile.accent.r, _profile.accent.g,
                _profile.accent.b, 0.13f);
            Button chipBtn = chipObj.AddComponent<Button>();
            chipBtn.targetGraphic = chipImg;
            chipBtn.onClick.AddListener(() => Submit(captured));
            MakeLabel("Label", chipObj.GetComponent<RectTransform>(),
                Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f),
                chip, _profile.accent, 11f, FontAlign(TextAlignmentOptions.Center), true);
        }

        // Input row
        GameObject inputObj = new GameObject("InputField");
        inputObj.transform.SetParent(_sheet, false);
        RectTransform inRT = inputObj.AddComponent<RectTransform>();
        inRT.anchorMin = new Vector2(0f, 0f);
        inRT.anchorMax = new Vector2(1f, 0f);
        inRT.pivot = new Vector2(0.5f, 0f);
        inRT.offsetMin = new Vector2(12f, 10f);
        inRT.offsetMax = new Vector2(-86f, 52f);
        Image inImg = inputObj.AddComponent<Image>();
        inImg.color = new Color(1f, 1f, 1f, 0.06f);
        _input = inputObj.AddComponent<TMP_InputField>();
        _input.targetGraphic = inImg;

        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(inRT, false);
        RectTransform taRT = textArea.AddComponent<RectTransform>();
        Stretch(taRT);
        taRT.offsetMin = new Vector2(12f, 6f);
        taRT.offsetMax = new Vector2(-12f, -6f);
        textArea.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = MakeLabel("Placeholder", taRT,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            $"Message {_profile.displayName}...", TEXT_DIM, 13f,
            FontAlign(TextAlignmentOptions.Left), false);
        placeholder.fontStyle = FontStyles.Italic;

        TextMeshProUGUI inputText = MakeLabel("Text", taRT,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "", Color.white, 13f, FontAlign(TextAlignmentOptions.Left), false);

        _input.textViewport = taRT;
        _input.textComponent = inputText;
        _input.placeholder = placeholder;
        _input.onSubmit.AddListener(_ => Submit(_input.text));

        GameObject sendObj = new GameObject("Btn_Send");
        sendObj.transform.SetParent(_sheet, false);
        RectTransform sendRT = sendObj.AddComponent<RectTransform>();
        sendRT.anchorMin = new Vector2(1f, 0f);
        sendRT.anchorMax = new Vector2(1f, 0f);
        sendRT.pivot = new Vector2(1f, 0f);
        sendRT.anchoredPosition = new Vector2(-12f, 10f);
        sendRT.sizeDelta = new Vector2(66f, 42f);
        Image sendImg = sendObj.AddComponent<Image>();
        sendImg.color = _profile.accent;
        Button sendBtn = sendObj.AddComponent<Button>();
        sendBtn.targetGraphic = sendImg;
        sendBtn.onClick.AddListener(() => Submit(_input.text));
        MakeLabel("Label", sendRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "Send", new Color(0.04f, 0.04f, 0.06f, 1f), 13f,
            FontAlign(TextAlignmentOptions.Center), true);

        // Greeting + slide in
        AddMessage(_profile.greeting, true);
        StartCoroutine(SlideRoutine(open: true, onDone: null));
    }

    // ── Conversation flow ────────────────────────────────────────────────────

    void Submit(string text)
    {
        if (_busy || _closing || string.IsNullOrWhiteSpace(text)) return;
        _busy = true;
        if (_input != null) _input.text = "";

        AddMessage(text.Trim(), false);
        _typingText = AddMessage("...", true);

        CharacterChatService.Instance.RequestReply(
            _characterId, text, _screenState, OnReply);
    }

    void OnReply(ChatReply reply)
    {
        if (this == null || _closing) return;
        if (_typingText != null)
            _typingText.text = FormatCharacterLine(reply.reply);
        _typingText = null;
        _busy = false;
        // emotion/animation (§11.7) available for the avatar layer (Phase 2):
        // e.g. drive presence bob speed by reply.emotion.
        StartCoroutine(ScrollToBottom());
    }

    TextMeshProUGUI AddMessage(string text, bool fromCharacter)
    {
        GameObject row = new GameObject(fromCharacter ? "Msg_Char" : "Msg_User");
        row.transform.SetParent(_messagesContent, false);
        row.AddComponent<RectTransform>();
        Image bg = row.AddComponent<Image>();
        bg.color = fromCharacter
            ? BUBBLE_CHAR
            : new Color(_profile.accent.r, _profile.accent.g, _profile.accent.b, 0.13f);
        bg.raycastTarget = false;

        VerticalLayoutGroup pad = row.AddComponent<VerticalLayoutGroup>();
        pad.padding = new RectOffset(12, 12, 8, 8);
        pad.childControlWidth = true;
        pad.childControlHeight = true;
        pad.childForceExpandWidth = true;
        pad.childForceExpandHeight = false;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(row.transform, false);
        textObj.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = fromCharacter ? FormatCharacterLine(text) : text;
        tmp.fontSize = 13f;
        tmp.color = Color.white;
        tmp.alignment = fromCharacter ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;

        StartCoroutine(ScrollToBottom());
        return tmp;
    }

    string FormatCharacterLine(string text)
    {
        string hex = ColorUtility.ToHtmlStringRGB(_profile.accent);
        return $"<color=#{hex}><b>{_profile.displayName}</b></color>  {text}";
    }

    IEnumerator ScrollToBottom()
    {
        yield return null; // wait one frame for layout
        if (_scroll != null) _scroll.verticalNormalizedPosition = 0f;
    }

    // ── Open / close ─────────────────────────────────────────────────────────

    public void Close()
    {
        if (_closing) return;
        _closing = true;
        StartCoroutine(SlideRoutine(open: false, onDone: () =>
        {
            if (_openSheet == this) _openSheet = null;
            Destroy(gameObject);
        }));
    }

    IEnumerator SlideRoutine(bool open, System.Action onDone)
    {
        float height = _sheet.rect.height > 1f ? _sheet.rect.height : 500f;
        float from = open ? -height : 0f;
        float to = open ? 0f : -height;
        float t = 0f;
        while (t < SLIDE_SECONDS)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / SLIDE_SECONDS));
            _sheet.anchoredPosition = new Vector2(0f, Mathf.Lerp(from, to, k));
            yield return null;
        }
        _sheet.anchoredPosition = new Vector2(0f, to);
        onDone?.Invoke();
    }

    void OnDestroy()
    {
        if (_openSheet == this) _openSheet = null;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TextAlignmentOptions FontAlign(TextAlignmentOptions a) => a;

    static TextMeshProUGUI MakeLabel(string name, RectTransform parent,
        Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax,
        string text, Color color, float size, TextAlignmentOptions align, bool bold)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = size;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }
}
