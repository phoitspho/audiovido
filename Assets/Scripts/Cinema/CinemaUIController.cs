using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AUDIOVIDO — Cinema UI Overlay (spec §5.11.2)
/// Top bar (Exit, title), bottom controls (now showing + play/pause),
/// and NOVA's host speech bubble. Wired by CinemaSceneBuilder.
/// </summary>
public class CinemaUIController : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] Button exitButton;
    [SerializeField] TMP_Text titleText;

    [Header("Bottom Controls")]
    [SerializeField] TMP_Text nowShowingText;
    [SerializeField] Button playPauseButton;
    [SerializeField] TMP_Text playPauseLabel;

    [Header("NOVA Bubble")]
    [SerializeField] GameObject novaBubbleRoot;
    [SerializeField] TMP_Text novaMessageText;
    [SerializeField] float bubbleSeconds = 5f;

    Coroutine _hideRoutine;

    void Start()
    {
        exitButton?.onClick.AddListener(() => CinemaManager.Instance?.ExitCinema());
        playPauseButton?.onClick.AddListener(() => CinemaManager.Instance?.TogglePlayback());

        if (titleText) titleText.text = "AUDIOVIDO CINEMA";
        if (novaBubbleRoot) novaBubbleRoot.SetActive(false);

        // Labels must never swallow clicks meant for buttons (Lounge lesson).
        if (titleText) titleText.raycastTarget = false;
        if (nowShowingText) nowShowingText.raycastTarget = false;
        if (novaMessageText) novaMessageText.raycastTarget = false;

        // AI chat with NOVA (spec §4.2) — button added at runtime, no rebuild
        if (playPauseButton != null)
        {
            CharacterProfile nova = CharacterProfiles.Get("nova");
            Button chat = ChatSheetController.AddChatButton(
                playPauseButton.transform.parent as RectTransform,
                new Vector2(-118f, 0f), new Vector2(66f, 40f), nova.accent);
            chat.onClick.AddListener(() => ChatSheetController.Open(
                "nova", GetComponentInParent<Canvas>(),
                "cinema/" + (CinemaManager.Instance != null
                    ? CinemaManager.Instance.State.ToString().ToLowerInvariant() : "watching")));
        }
    }

    public void SetNowShowing(string title, bool playing)
    {
        if (nowShowingText) nowShowingText.text = (playing ? ">> " : "|| ") + title;
        if (playPauseLabel) playPauseLabel.text = playing ? "Pause" : "Play";
    }

    public void ShowNovaLine(string line)
    {
        if (novaBubbleRoot == null || novaMessageText == null) return;
        novaMessageText.text = line;
        novaBubbleRoot.SetActive(true);
        if (_hideRoutine != null) StopCoroutine(_hideRoutine);
        _hideRoutine = StartCoroutine(HideBubbleAfter(bubbleSeconds));
    }

    public void HideAll()
    {
        if (_hideRoutine != null) { StopCoroutine(_hideRoutine); _hideRoutine = null; }
        if (novaBubbleRoot) novaBubbleRoot.SetActive(false);
    }

    IEnumerator HideBubbleAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (novaBubbleRoot) novaBubbleRoot.SetActive(false);
        _hideRoutine = null;
    }
}
