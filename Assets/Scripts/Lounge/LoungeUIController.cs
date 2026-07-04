using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AUDIOVIDO — Bar / Lounge 2D UI Overlay (SCR-19)
/// Spec §5.13 UI overlay:
///   Top bar:    ← Exit | "The Lounge" | NXT balance | user count
///   Bottom bar: [Now Playing] [Queue] [Chat] [Invite] [Sit here]
///   Drift bubble: slides in when DRIFT speaks
///   Mood panel:  slides up after DRIFT greeting — 4 mood options
///
/// Design tokens (§1.2):
///   --bg-secondary:   #111118
///   --text-primary:   #FFFFFF
///   --text-secondary: #A0A0B8
///   --music-primary:  #00D4FF  (cyan)
///   --nxt-gold:       #FFD700  (amber)
/// </summary>
public class LoungeUIController : MonoBehaviour
{
    // ── Top Bar ───────────────────────────────────────────────────────────────
    [Header("Top Bar")]
    [SerializeField] Button exitButton;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI userCountText;
    [SerializeField] TextMeshProUGUI nxtBalanceText;   // NXT token balance

    // ── Bottom Bar ────────────────────────────────────────────────────────────
    [Header("Bottom Bar")]
    [SerializeField] TextMeshProUGUI nowPlayingText;
    [SerializeField] Button queueButton;
    [SerializeField] Button chatButton;
    [SerializeField] Button inviteButton;
    [SerializeField] Button sitHereButton;

    // ── DRIFT Chat Bubble ─────────────────────────────────────────────────────
    [Header("DRIFT Chat Bubble")]
    [SerializeField] RectTransform driftBubbleRoot;
    [SerializeField] TextMeshProUGUI driftBubbleText;
    [SerializeField] float bubbleAnimDuration = 0.25f;
    [SerializeField] float autoDismissDuration = 5f;

    // ── Mood Panel ────────────────────────────────────────────────────────────
    [Header("Mood Panel")]
    [SerializeField] RectTransform moodPanelRoot;
    [SerializeField] Button btnMelancholic;
    [SerializeField] Button btnEnergetic;
    [SerializeField] Button btnNostalgic;
    [SerializeField] Button btnChill;
    [SerializeField] float moodPanelAnimDuration = 0.3f;

    // ── Track Info Card ───────────────────────────────────────────────────────
    [Header("Track Card")]
    [SerializeField] RectTransform trackCardRoot;
    [SerializeField] TextMeshProUGUI cardTrackText;
    [SerializeField] TextMeshProUGUI cardArtistText;
    [SerializeField] float trackCardDuration    = 4f;
    [SerializeField] float trackCardAnimSeconds = 0.3f;

    // ── Entry / Now Playing ───────────────────────────────────────────────────
    [Header("Entry")]
    [SerializeField] string entryGreeting = "What are you thinking about?";

    [Header("Now Playing")]
    [SerializeField] string defaultTrack = "Lo-fi chill mix";

    // ── Runtime state ─────────────────────────────────────────────────────────
    Coroutine _autoDismissRoutine;
    Coroutine _trackCardRoutine;
    Vector2 _bubbleHiddenAnchor;
    Vector2 _bubbleShownAnchor;
    Vector2 _moodHiddenAnchor;
    Vector2 _moodShownAnchor;
    Vector2 _cardShownAnchor;
    Vector2 _cardHiddenAnchor;
    bool _moodPanelVisible;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        // DRIFT bubble: slides up from 120px below resting position
        if (driftBubbleRoot != null)
        {
            _bubbleShownAnchor  = driftBubbleRoot.anchoredPosition;
            _bubbleHiddenAnchor = _bubbleShownAnchor + new Vector2(0, -120f);
            driftBubbleRoot.anchoredPosition = _bubbleHiddenAnchor;
            driftBubbleRoot.gameObject.SetActive(false);
        }

        // Mood panel: starts hidden 160px below resting position
        if (moodPanelRoot != null)
        {
            _moodShownAnchor  = moodPanelRoot.anchoredPosition;
            _moodHiddenAnchor = _moodShownAnchor + new Vector2(0, -160f);
            moodPanelRoot.anchoredPosition = _moodHiddenAnchor;
            moodPanelRoot.gameObject.SetActive(false);
        }

        // Track card: slides in from right (300px off screen)
        if (trackCardRoot != null)
        {
            _cardShownAnchor  = trackCardRoot.anchoredPosition;
            _cardHiddenAnchor = _cardShownAnchor + new Vector2(300f, 0f);
            trackCardRoot.anchoredPosition = _cardHiddenAnchor;
            trackCardRoot.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        exitButton?.onClick.AddListener(OnExitClicked);
        sitHereButton?.onClick.AddListener(OnSitHereClicked);
        chatButton?.onClick.AddListener(OnChatClicked);
        queueButton?.onClick.AddListener(OnQueueClicked);
        inviteButton?.onClick.AddListener(OnInviteClicked);

        // The original build placed the action row below the visible bar —
        // pull the buttons up into view (runtime fix, no scene rebuild).
        foreach (Button b in new[] { queueButton, chatButton, inviteButton, sitHereButton })
        {
            if (b == null) continue;
            RectTransform brt = b.GetComponent<RectTransform>();
            brt.anchoredPosition = new Vector2(brt.anchoredPosition.x, -18f);
        }

        // Mood buttons
        btnMelancholic?.onClick.AddListener(() => OnMoodSelected(MoodType.Melancholic));
        btnEnergetic?.onClick.AddListener(() => OnMoodSelected(MoodType.Energetic));
        btnNostalgic?.onClick.AddListener(() => OnMoodSelected(MoodType.Nostalgic));
        btnChill?.onClick.AddListener(() => OnMoodSelected(MoodType.Chill));

        // Static labels
        if (titleText) titleText.text = "The Lounge";
        if (nowPlayingText) nowPlayingText.text = $">> {defaultTrack}";
        SetUserCount(1);
        SetNxtBalance(0); // updated from server in Phase 2

        // Non-interactive labels must not swallow clicks meant for buttons
        // beneath them (Txt_Title spans the full top bar and was eating
        // every click on Btn_Exit).
        if (titleText) titleText.raycastTarget = false;
        if (userCountText) userCountText.raycastTarget = false;
        if (nxtBalanceText) nxtBalanceText.raycastTarget = false;
        if (nowPlayingText) nowPlayingText.raycastTarget = false;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetUserCount(int count)
    {
        if (userCountText) userCountText.text = $"[{count}]";
    }

    public void SetNxtBalance(int amount)
    {
        if (nxtBalanceText) nxtBalanceText.text = $"{amount} NXT";
    }

    public void SetNowPlaying(string trackName)
    {
        if (nowPlayingText) nowPlayingText.text = $">> {trackName}";
        ShowTrackCard(trackName);
    }

    /// <summary>
    /// Slides in the track card. Parses "Artist — Track" or shows whole string as track name.
    /// </summary>
    void ShowTrackCard(string trackString)
    {
        if (trackCardRoot == null) return;

        // Parse "Artist — Track Title"
        string artist = "";
        string track  = trackString;
        int sep = trackString.IndexOf(" — ");
        if (sep > 0)
        {
            artist = trackString.Substring(0, sep);
            track  = trackString.Substring(sep + 3);
        }

        if (cardTrackText)  cardTrackText.text  = track;
        if (cardArtistText) cardArtistText.text = artist;

        if (_trackCardRoutine != null) StopCoroutine(_trackCardRoutine);
        _trackCardRoutine = StartCoroutine(TrackCardRoutine());
    }

    public void ShowEntryBubble()
    {
        StartCoroutine(DelayedBubble(1.2f, entryGreeting));
    }

    public void ShowDriftBubble(string message)
    {
        if (_autoDismissRoutine != null) StopCoroutine(_autoDismissRoutine);
        if (driftBubbleText) driftBubbleText.text = message;
        StartCoroutine(SlideBubble(show: true));
        _autoDismissRoutine = StartCoroutine(AutoDismiss());
    }

    public void HideDriftBubble()
    {
        if (_autoDismissRoutine != null) StopCoroutine(_autoDismissRoutine);
        StartCoroutine(SlideBubble(show: false));
    }

    public void ShowMoodPanel()
    {
        if (_moodPanelVisible) return;
        _moodPanelVisible = true;
        StartCoroutine(SlidePanel(moodPanelRoot, _moodHiddenAnchor, _moodShownAnchor,
                                   moodPanelAnimDuration, show: true));
    }

    public void HideMoodPanel()
    {
        if (!_moodPanelVisible) return;
        _moodPanelVisible = false;
        StartCoroutine(SlidePanel(moodPanelRoot, _moodShownAnchor, _moodHiddenAnchor,
                                   moodPanelAnimDuration, show: false));
    }

    public void HideAll()
    {
        HideDriftBubble();
        HideMoodPanel();
        if (_trackCardRoutine != null) { StopCoroutine(_trackCardRoutine); _trackCardRoutine = null; }
        if (trackCardRoot) trackCardRoot.gameObject.SetActive(false);
    }

    // ── Button Handlers ───────────────────────────────────────────────────────

    void OnExitClicked()
    {
        Debug.Log("[LoungeUI] Exit clicked");
        LoungeManager.Instance?.ExitLounge();
    }
    void OnSitHereClicked()   => LoungeManager.Instance?.OnPlayerApproachBar();
    void OnChatClicked() =>
        ChatSheetController.Open("drift", GetComponentInParent<Canvas>(), "lounge/ambient");
    void OnQueueClicked()     => Debug.Log("[LoungeUI] Queue (Phase 2)");
    void OnInviteClicked()    => Debug.Log("[LoungeUI] Invite (Phase 2)");

    void OnMoodSelected(MoodType mood)
    {
        LoungeManager.Instance?.OnMoodSelected(mood);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    IEnumerator DelayedBubble(float delay, string message)
    {
        yield return new WaitForSeconds(delay);
        ShowDriftBubble(message);
    }

    IEnumerator SlideBubble(bool show)
    {
        if (driftBubbleRoot == null) yield break;
        Vector2 from = show ? _bubbleHiddenAnchor : _bubbleShownAnchor;
        Vector2 to   = show ? _bubbleShownAnchor  : _bubbleHiddenAnchor;
        yield return SlidePanel(driftBubbleRoot, from, to, bubbleAnimDuration, show);
    }

    IEnumerator SlidePanel(RectTransform rt, Vector2 from, Vector2 to,
                            float duration, bool show)
    {
        if (rt == null) yield break;
        rt.gameObject.SetActive(true);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duration);
            rt.anchoredPosition = Vector2.Lerp(from, to, p);
            yield return null;
        }
        rt.anchoredPosition = to;
        if (!show) rt.gameObject.SetActive(false);
    }

    IEnumerator AutoDismiss()
    {
        yield return new WaitForSeconds(autoDismissDuration);
        StartCoroutine(SlideBubble(show: false));
    }

    IEnumerator TrackCardRoutine()
    {
        // Slide in
        yield return SlidePanel(trackCardRoot, _cardHiddenAnchor, _cardShownAnchor,
                                trackCardAnimSeconds, show: true);
        // Hold
        yield return new WaitForSeconds(trackCardDuration);
        // Slide out
        yield return SlidePanel(trackCardRoot, _cardShownAnchor, _cardHiddenAnchor,
                                trackCardAnimSeconds, show: false);
        _trackCardRoutine = null;
    }
}
