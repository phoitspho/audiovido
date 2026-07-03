using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AUDIOVIDO — Hub Screen (spec §5.5 / §5.6, SCR-07/08)
/// Music / Video sub-tabs with Echo and Reel, and 3D space entry
/// (spec §3.3 — 3D spaces are entered from the Hub).
/// </summary>
public class HubScreenController : MonoBehaviour
{
    [Header("Sub-tabs")]
    [SerializeField] Button musicTabButton;
    [SerializeField] Button videoTabButton;
    [SerializeField] TMP_Text musicTabLabel;
    [SerializeField] TMP_Text videoTabLabel;
    [SerializeField] GameObject musicContent;
    [SerializeField] GameObject videoContent;

    [Header("Characters")]
    [SerializeField] CharacterBubble echo;
    [SerializeField] CharacterBubble reel;

    [Header("3D Spaces")]
    [SerializeField] Button enterLoungeButton;

    static readonly Color CYAN  = new Color(0f, 0.831f, 1f, 1f);        // --music-primary
    static readonly Color CORAL = new Color(1f, 0.278f, 0.341f, 1f);    // --video-primary
    static readonly Color DIM   = new Color(0.353f, 0.353f, 0.47f, 1f); // --text-tertiary

    bool _musicActive = true;
    bool _started;

    void Start()
    {
        musicTabButton?.onClick.AddListener(() => ShowTab(true));
        videoTabButton?.onClick.AddListener(() => ShowTab(false));
        enterLoungeButton?.onClick.AddListener(OnEnterLounge);
        _started = true;
        ApplyTab();
        echo?.Say("What's the vibe tonight?");
    }

    void ShowTab(bool music)
    {
        if (music == _musicActive) return;
        _musicActive = music;
        ApplyTab();

        if (music) echo?.Say("Back to the beats. What's the vibe?");
        else       reel?.Say("Every frame tells a story.");
    }

    void ApplyTab()
    {
        if (!_started) return;
        if (musicContent != null) musicContent.SetActive(_musicActive);
        if (videoContent != null) videoContent.SetActive(!_musicActive);

        if (musicTabLabel != null)
        {
            musicTabLabel.color = _musicActive ? CYAN : DIM;
            musicTabLabel.fontStyle = _musicActive ? FontStyles.Bold : FontStyles.Normal;
        }
        if (videoTabLabel != null)
        {
            videoTabLabel.color = _musicActive ? DIM : CORAL;
            videoTabLabel.fontStyle = _musicActive ? FontStyles.Normal : FontStyles.Bold;
        }
    }

    void OnEnterLounge()
    {
        AppShellController.Instance?.EnterSpace("Scene_Lounge");
    }
}
