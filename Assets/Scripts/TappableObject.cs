using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base component for every interactive object in a room (mirror, closet, shelf, etc.).
/// Attach this (or a subclass) to any GameObject you want the player to tap.
/// Requires a Collider on the object or its children.
/// </summary>
public class TappableObject : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] string objectId = "object_id";   // unique id, e.g. "mirror", "diary"
    [SerializeField] string displayName = "Object";

    [Header("Interaction")]
    [SerializeField] bool isInteractable = true;
    [SerializeField] float highlightIntensity = 0.25f;

    [Header("Events")]
    public UnityEvent<TappableObject> OnTappedEvent;   // wire up in Inspector or via code

    // State
    bool _highlighted;
    Material[] _originalMaterials;
    Renderer[] _renderers;

    public bool IsInteractable
    {
        get => isInteractable;
        set => isInteractable = value;
    }
    public string ObjectId => objectId;
    public string DisplayName => displayName;

    protected virtual void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
    }

    // Test helper — right-click this component in Play mode → "Simulate Tap"
    [ContextMenu("Simulate Tap")]
    void SimulateTap() => OnTapped(default);

    // Called by RoomInputManager when this object is tapped
    public virtual void OnTapped(RaycastHit hit)
    {
        Debug.Log($"[Tapped] {displayName} ({objectId})");
        OnTappedEvent?.Invoke(this);
        PlayTapFeedback();
    }

    // Visual pulse on tap
    void PlayTapFeedback()
    {
        StopAllCoroutines();
        StartCoroutine(PulseRoutine());
    }

    System.Collections.IEnumerator PulseRoutine()
    {
        SetEmissiveBoost(2f);
        yield return new WaitForSeconds(0.12f);
        SetEmissiveBoost(0f);
    }

    void SetEmissiveBoost(float amount)
    {
        foreach (var rend in _renderers)
        {
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    Color baseColor = mat.GetColor("_BaseColor");
                    mat.SetColor("_EmissionColor", baseColor * amount);
                }
            }
        }
    }

    // Optional: call this to enable/disable interactability at runtime
    public void SetInteractable(bool value)
    {
        isInteractable = value;
    }
}
