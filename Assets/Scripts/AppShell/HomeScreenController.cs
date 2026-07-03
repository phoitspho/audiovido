using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// AUDIOVIDO — Home Screen (spec §5.3, SCR-04)
/// Time-of-day greeting + Luna behavior:
/// "After 5s on screen, Luna animates and says 'I found something for you →'"
/// </summary>
public class HomeScreenController : MonoBehaviour
{
    [SerializeField] TMP_Text greetingText;
    [SerializeField] CharacterBubble luna;
    [SerializeField] string userName = "PHO";

    bool _lunaSpoke;

    void OnEnable()
    {
        if (greetingText != null)
            greetingText.text = $"Good {TimeOfDayWord()}, {userName}";

        if (!_lunaSpoke)
            StartCoroutine(LunaGreetAfterDelay());
    }

    IEnumerator LunaGreetAfterDelay()
    {
        yield return new WaitForSeconds(5f);
        if (!_lunaSpoke && luna != null)
        {
            luna.Say("I found something for you →");
            _lunaSpoke = true;
        }
    }

    static string TimeOfDayWord()
    {
        int h = System.DateTime.Now.Hour;
        if (h >= 5 && h < 12) return "morning";
        if (h >= 12 && h < 18) return "afternoon";
        return "evening";
    }
}
