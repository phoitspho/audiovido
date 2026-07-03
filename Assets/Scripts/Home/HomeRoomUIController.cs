using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AUDIOVIDO — Your Room UI Overlay
/// Top bar (Exit, title, level), bottom bar (now playing + Music + Theme).
/// Wired by HomeSceneBuilder.
/// </summary>
public class HomeRoomUIController : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] Button exitButton;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text levelText;

    [Header("Bottom Bar")]
    [SerializeField] TMP_Text nowPlayingText;
    [SerializeField] Button musicButton;
    [SerializeField] TMP_Text musicLabel;
    [SerializeField] Button themeButton;
    [SerializeField] TMP_Text themeNameText;

    void Start()
    {
        exitButton?.onClick.AddListener(() => HomeRoomManager.Instance?.ExitRoom());
        musicButton?.onClick.AddListener(() => HomeRoomManager.Instance?.ToggleMusic());
        themeButton?.onClick.AddListener(() => HomeRoomManager.Instance?.CycleTheme());

        if (titleText) { titleText.text = "YOUR ROOM"; titleText.raycastTarget = false; }
        if (levelText) { levelText.text = "LEVEL 37"; levelText.raycastTarget = false; }
        if (nowPlayingText) nowPlayingText.raycastTarget = false;
        if (themeNameText) themeNameText.raycastTarget = false;
    }

    public void SetNowPlaying(string track, bool playing)
    {
        if (nowPlayingText) nowPlayingText.text = (playing ? ">> " : "|| ") + track;
        if (musicLabel) musicLabel.text = playing ? "Pause" : "Play";
    }

    public void SetThemeName(string themeName)
    {
        if (themeNameText) themeNameText.text = themeName;
    }
}
