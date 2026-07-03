using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// AUDIOVIDO — Profile Screen (spec §5.8, SCR-14)
/// Mirror reflects the user's stats; NXT earned stays live.
/// </summary>
public class ProfileScreenController : MonoBehaviour
{
    [SerializeField] CharacterBubble mirror;
    [SerializeField] TMP_Text nxtEarnedText;

    bool _greeted;

    void OnEnable()
    {
        RefreshNxt(NxtEarnManager.Instance != null ? NxtEarnManager.Instance.Balance : 0, 0);
        NxtEarnManager.OnNxtChanged += RefreshNxt;

        if (!_greeted)
            StartCoroutine(GreetAfterDelay());
    }

    void OnDisable()
    {
        NxtEarnManager.OnNxtChanged -= RefreshNxt;
    }

    IEnumerator GreetAfterDelay()
    {
        yield return new WaitForSeconds(0.8f);
        if (!_greeted && mirror != null)
        {
            mirror.Say("Every session tells me a little more about you.");
            _greeted = true;
        }
    }

    void RefreshNxt(int total, int delta)
    {
        if (nxtEarnedText != null)
            nxtEarnedText.text = $"NXT Earned: {total}";
    }
}
