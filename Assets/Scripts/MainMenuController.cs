using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AUDIOVIDO — Main Menu Controller
/// Wires the hub entry screen buttons to SceneLoader.
/// Attach to MainCanvas. References set by MainSceneBuilder.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] Button enterLoungeButton;
    // Future: enterCinemaButton, enterConcertButton

    void Start()
    {
        enterLoungeButton?.onClick.AddListener(OnEnterLounge);
    }

    void OnEnterLounge()
    {
        // Spec §12.3: 3D spaces load additively on top of MainScene
        SceneLoader.Instance?.LoadAdditive("Scene_Lounge");
    }
}
