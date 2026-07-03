using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AUDIOVIDO — Character Bubble (spec §4.2, 2D mode)
/// Reusable floating AI-character widget for 2D screens:
///   • Avatar circle with breathing idle pulse
///   • Tap avatar → toggles speech bubble (default line)
///   • Say(line) → shows bubble, auto-hides after a few seconds
/// One instance per character per screen (Luna, Petros, Echo, Reel, Vibe, Mirror).
/// </summary>
public class CharacterBubble : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] string characterName = "LUNA";
    [SerializeField] string defaultLine = "Hey there!";

    [Header("References")]
    [SerializeField] Image avatarImage;
    [SerializeField] Button avatarButton;
    [SerializeField] GameObject bubbleRoot;
    [SerializeField] TMP_Text bubbleNameText;
    [SerializeField] TMP_Text bubbleMessageText;

    [Header("Idle Pulse (spec §9.4 breathing, ~4s loop)")]
    [SerializeField] float pulseAmount = 0.05f;
    [SerializeField] float pulseSpeed = 1.6f;

    Vector3 _baseScale = Vector3.one;
    Coroutine _hideRoutine;

    void Awake()
    {
        if (avatarImage != null)
            _baseScale = avatarImage.transform.localScale;
        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);
        if (bubbleNameText != null)
            bubbleNameText.text = characterName;
        if (avatarButton != null)
            avatarButton.onClick.AddListener(OnAvatarTapped);
    }

    void Update()
    {
        if (avatarImage == null) return;
        float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        avatarImage.transform.localScale = _baseScale * s;
    }

    /// <summary>Show the speech bubble with a line. autoHideSeconds &lt;= 0 keeps it open.</summary>
    public void Say(string line, float autoHideSeconds = 6f)
    {
        if (bubbleRoot == null || bubbleMessageText == null) return;
        bubbleMessageText.text = line;
        bubbleRoot.SetActive(true);
        if (_hideRoutine != null) StopCoroutine(_hideRoutine);
        if (autoHideSeconds > 0f && isActiveAndEnabled)
            _hideRoutine = StartCoroutine(HideAfter(autoHideSeconds));
    }

    public void Hide()
    {
        if (_hideRoutine != null) { StopCoroutine(_hideRoutine); _hideRoutine = null; }
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
    }

    void OnAvatarTapped()
    {
        if (bubbleRoot != null && bubbleRoot.activeSelf) Hide();
        else Say(defaultLine);
    }

    IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
        _hideRoutine = null;
    }
}
