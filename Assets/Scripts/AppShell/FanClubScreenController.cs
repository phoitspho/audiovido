using System.Collections;
using UnityEngine;

/// <summary>
/// AUDIOVIDO — Fan Club Screen (spec §5.7, SCR-10)
/// Vibe welcomes the user on entry.
/// </summary>
public class FanClubScreenController : MonoBehaviour
{
    [SerializeField] CharacterBubble vibe;

    bool _greeted;

    void OnEnable()
    {
        if (!_greeted)
            StartCoroutine(GreetAfterDelay());
    }

    IEnumerator GreetAfterDelay()
    {
        yield return new WaitForSeconds(0.6f);
        if (!_greeted && vibe != null)
        {
            vibe.Say("Your tribe is here!");
            _greeted = true;
        }
    }
}
