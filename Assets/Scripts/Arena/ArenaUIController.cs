using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AUDIOVIDO — Arena UI Overlay (spec §5.12)
/// Top bar (Exit, title), bottom bar (now playing + HYPE! reaction button),
/// PULSE's speech bubble. Wired by ArenaSceneBuilder.
/// </summary>
public class ArenaUIController : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] Button exitButton;
    [SerializeField] TMP_Text titleText;

    [Header("Bottom Bar")]
    [SerializeField] TMP_Text nowPlayingText;
    [SerializeField] Button hypeButton;

    [Header("PULSE Bubble")]
    [SerializeField] GameObject pulseBubbleRoot;
    [SerializeField] TMP_Text pulseMessageText;
    [SerializeField] float defaultBubbleSeconds = 4f;

    Coroutine _hideRoutine;

    void Start()
    {
        exitButton?.onClick.AddListener(() => ArenaManager.Instance?.ExitArena());
        hypeButton?.onClick.AddListener(() => ArenaManager.Instance?.OnHypePressed());

        if (titleText) { titleText.text = "AUDIOVIDO ARENA"; titleText.raycastTarget = false; }
        if (nowPlayingText) nowPlayingText.raycastTarget = false;
        if (pulseMessageText) pulseMessageText.raycastTarget = false;
        if (pulseBubbleRoot) pulseBubbleRoot.SetActive(false);
    }

    public void SetNowPlaying(string track)
    {
        if (nowPlayingText) nowPlayingText.text = ">> " + track;
    }

    public void ShowPulseLine(string line, float seconds = -1f)
    {
        if (pulseBubbleRoot == null || pulseMessageText == null) return;
        pulseMessageText.text = line;
        pulseBubbleRoot.SetActive(true);
        if (_hideRoutine != null) StopCoroutine(_hideRoutine);
        _hideRoutine = StartCoroutine(
            HideBubbleAfter(seconds > 0f ? seconds : defaultBubbleSeconds));
    }

    public void HideAll()
    {
        if (_hideRoutine != null) { StopCoroutine(_hideRoutine); _hideRoutine = null; }
        if (pulseBubbleRoot) pulseBubbleRoot.SetActive(false);
    }

    IEnumerator HideBubbleAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (pulseBubbleRoot) pulseBubbleRoot.SetActive(false);
        _hideRoutine = null;
    }
}
