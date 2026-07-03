using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// AUDIOVIDO — Lounge Interactable (SCR-19)
/// Attach to bar stools, tables, or any tappable object in the lounge.
/// Spec §7.2: Tap on object → Interact (sit, approach bar)
/// </summary>
public class LoungeInteractable : MonoBehaviour
{
    public enum InteractType { BarStool, Table, Bar, DriftTrigger }

    [SerializeField] InteractType type = InteractType.BarStool;
    [SerializeField] string interactLabel = "Sit here";

    [Header("Events")]
    public UnityEvent onInteract;

    // Called by the input system (raycast tap)
    public void OnTapped()
    {
        Debug.Log($"[LoungeInteractable] Tapped: {gameObject.name} ({type})");

        switch (type)
        {
            case InteractType.BarStool:
            case InteractType.Bar:
            case InteractType.DriftTrigger:
                LoungeManager.Instance?.OnPlayerApproachBar();
                break;

            case InteractType.Table:
                // Future: sit at table, group chat
                Debug.Log("[LoungeInteractable] Sat at table");
                break;
        }

        onInteract?.Invoke();
    }

    public string GetLabel() => interactLabel;

#if UNITY_EDITOR
    [ContextMenu("Simulate Tap")]
    void SimulateTap() => OnTapped();
#endif
}
