using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AUDIOVIDO — Fan Plaza UI Overlay
/// Top bar (Exit, title), community feed line, WAVE! button, VIBE bubble.
/// Wired by PlazaSceneBuilder.
/// </summary>
public class PlazaUIController : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] Button exitButton;
    [SerializeField] TMP_Text titleText;

    [Header("Bottom Bar")]
    [SerializeField] TMP_Text feedText;
    [SerializeField] Button waveButton;

    [Header("VIBE Bubble")]
    [SerializeField] GameObject vibeBubbleRoot;
    [SerializeField] TMP_Text vibeMessageText;
    [SerializeField] float bubbleSeconds = 4.5f;

    Coroutine _hideRoutine;

    void Start()
    {
        exitButton?.onClick.AddListener(() => PlazaManager.Instance?.ExitPlaza());
        waveButton?.onClick.AddListener(() => PlazaManager.Instance?.OnWavePressed());

        if (titleText) { titleText.text = "FAN PLAZA"; titleText.raycastTarget = false; }
        if (feedText) { feedText.text = "Share. Belong. Inspire."; feedText.raycastTarget = false; }
        if (vibeMessageText) vibeMessageText.raycastTarget = false;
        if (vibeBubbleRoot) vibeBubbleRoot.SetActive(false);
    }

    public void SetFeedLine(string line)
    {
        if (feedText) feedText.text = "• " + line;
    }

    public void ShowVibeLine(string line)
    {
        if (vibeBubbleRoot == null || vibeMessageText == null) return;
        vibeMessageText.text = line;
        vibeBubbleRoot.SetActive(true);
        if (_hideRoutine != null) StopCoroutine(_hideRoutine);
        _hideRoutine = StartCoroutine(HideBubbleAfter(bubbleSeconds));
    }

    public void HideAll()
    {
        if (_hideRoutine != null) { StopCoroutine(_hideRoutine); _hideRoutine = null; }
        if (vibeBubbleRoot) vibeBubbleRoot.SetActive(false);
    }

    IEnumerator HideBubbleAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (vibeBubbleRoot) vibeBubbleRoot.SetActive(false);
        _hideRoutine = null;
    }
}
