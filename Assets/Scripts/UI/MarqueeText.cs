using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// AUDIOVIDO — Marquee Text Scroller
/// Attach alongside a TextMeshProUGUI component.
/// When the text content is wider than the RectTransform, it scrolls
/// horizontally at a constant speed, loops with a brief pause.
///
/// Usage:
///   Add to the Txt_NowPlaying GameObject in the BottomBar.
///   The RectTransform's overflow clips the scrolling text —
///   make sure the parent panel has Mask or the canvas clips correctly.
///
/// Design: clip viewport via a RectMask2D on the parent container
/// rather than on the label itself (so layout doesn't break).
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class MarqueeText : MonoBehaviour
{
    [Header("Scroll")]
    [SerializeField] float scrollSpeed   = 60f;  // pixels per second
    [SerializeField] float pauseAtStart  = 1.5f; // hold before scrolling
    [SerializeField] float pauseAtEnd    = 0.5f; // hold before looping
    [SerializeField] float gapBetweenLoops = 40f; // pixels of gap

    TextMeshProUGUI _tmp;
    RectTransform   _rt;
    float           _contentWidth;
    float           _viewWidth;

    Coroutine _scrollRoutine;
    string    _currentText = "";

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
        _rt  = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        // React to text changes (including from code)
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        StartScrollIfNeeded();
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);
    }

    void OnTextChanged(Object obj)
    {
        if (obj == _tmp) StartScrollIfNeeded();
    }

    void StartScrollIfNeeded()
    {
        if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);

        // Reset position
        _rt.anchoredPosition = Vector2.zero;

        // Measure content after layout pass
        StartCoroutine(MeasureAndScroll());
    }

    IEnumerator MeasureAndScroll()
    {
        // Wait one frame so TMP has finished layout
        yield return null;
        _tmp.ForceMeshUpdate();

        _contentWidth = _tmp.preferredWidth;
        _viewWidth    = _rt.rect.width;

        if (_contentWidth <= _viewWidth + 2f)
        {
            // Text fits — no scroll needed
            _rt.anchoredPosition = Vector2.zero;
            yield break;
        }

        _scrollRoutine = StartCoroutine(ScrollRoutine());
    }

    IEnumerator ScrollRoutine()
    {
        float scrollDistance = _contentWidth + gapBetweenLoops;

        while (true)
        {
            // Reset to start
            _rt.anchoredPosition = Vector2.zero;

            // Pause at start
            yield return new WaitForSeconds(pauseAtStart);

            // Scroll left until content disappears
            float x = 0f;
            while (x < scrollDistance)
            {
                x += scrollSpeed * Time.deltaTime;
                _rt.anchoredPosition = new Vector2(-x, 0f);
                yield return null;
            }

            // Pause before looping
            yield return new WaitForSeconds(pauseAtEnd);
        }
    }

    /// <summary>Update the displayed text and restart the scroll.</summary>
    public void SetText(string text)
    {
        if (_tmp == null) _tmp = GetComponent<TextMeshProUGUI>();
        _tmp.text = text;
        // OnTextChanged fires automatically via TMPro event
    }
}
