using UnityEngine;
using TMPro;

/// <summary>
/// AUDIOVIDO — Search Screen (spec §5.4, SCR-05)
/// Petros reacts to the search bar; a mock suggestions panel
/// appears while the query is non-empty.
/// </summary>
public class SearchScreenController : MonoBehaviour
{
    [SerializeField] TMP_InputField searchInput;
    [SerializeField] CharacterBubble petros;
    [SerializeField] GameObject suggestionsPanel;  // shown while typing
    [SerializeField] GameObject browsePanel;       // hidden while typing

    bool _reactedThisFocus;

    void Start()
    {
        if (searchInput != null)
        {
            searchInput.onSelect.AddListener(OnFocus);
            searchInput.onValueChanged.AddListener(OnTyped);
        }
        if (suggestionsPanel != null) suggestionsPanel.SetActive(false);
    }

    void OnFocus(string _)
    {
        _reactedThisFocus = false;
        petros?.Say("Need help finding something specific?");
    }

    void OnTyped(string value)
    {
        bool hasQuery = !string.IsNullOrWhiteSpace(value);

        if (suggestionsPanel != null) suggestionsPanel.SetActive(hasQuery);
        if (browsePanel != null) browsePanel.SetActive(!hasQuery);

        if (hasQuery && !_reactedThisFocus)
        {
            petros?.Say("I know exactly what you need...");
            _reactedThisFocus = true;
        }
    }
}
