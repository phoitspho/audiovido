using System.Collections;
using UnityEngine;

/// <summary>
/// AUDIOVIDO — DRIFT Character Controller (Bar / Lounge SCR-19)
/// Spec §4.1: 38yr male, barman vest, slicked hair, tattoo sleeves.
/// Spec §5.13 DRIFT behavior:
///   • Always behind bar, wiping glass when idle
///   • Approaches camera when user "sits at bar"
///   • Starts conversation: "Long day?"
///   • Recommends music based on mood
///   • Deep mode: philosophical conversations
///   • Night mode: scene gets darker, more intimate
/// </summary>
public class DriftController : MonoBehaviour
{
    // ── Animator state names (must match Animator Controller) ────────────────
    static readonly int ANIM_IDLE_WIPE    = Animator.StringToHash("Idle_WipeGlass");
    static readonly int ANIM_IDLE_LOOK    = Animator.StringToHash("Idle_LookAround");
    static readonly int ANIM_IDLE_LEAN    = Animator.StringToHash("Idle_Lean");
    static readonly int ANIM_APPROACH     = Animator.StringToHash("Approach");
    static readonly int ANIM_TALK         = Animator.StringToHash("Talk");
    static readonly int ANIM_REACT_POS    = Animator.StringToHash("React_Positive");
    static readonly int ANIM_WAVE         = Animator.StringToHash("Wave");
    static readonly int ANIM_STATE        = Animator.StringToHash("State");

    [Header("References")]
    [SerializeField] Animator animator;
    [SerializeField] Transform barPosition;       // DRIFT's position behind bar
    [SerializeField] Transform approachPosition;  // position DRIFT moves to when player sits

    [Header("Proximity")]
    [SerializeField] Transform playerCamera;
    [SerializeField] float approachRadius = 2.5f; // units from bar stool trigger

    [Header("Idle Cycle")]
    [SerializeField] float idleCycleMin = 4f;     // spec: 3 idle animations random cycle
    [SerializeField] float idleCycleMax = 8f;

    [Header("Conversation Lines")]
    [SerializeField] string[] greetingLines = {
        "Long day?",
        "What are you thinking about?",
        "You look like someone who needs the right song.",
        "Pull up a stool. I'll find something for you."
    };

    [SerializeField] string[] deepModeLines = {
        "Music is just time made beautiful.",
        "Every great song is someone's 3AM.",
        "What does home sound like to you?"
    };

    // ── Mood Responses (spec §5.13 — recommend music by mood) ───────────────
    // Each entry: (DRIFT dialogue line, track name for NowPlaying)
    static readonly (string line, string track)[] MOOD_MELANCHOLIC = {
        ("I know just the thing. Something that sits with you.", "Bon Iver — Holocene"),
        ("Let it breathe. Here.", "Sufjan Stevens — Death With Dignity"),
        ("No words needed. Just this.", "Nils Frahm — Says")
    };

    static readonly (string line, string track)[] MOOD_ENERGETIC = {
        ("Right. Let's wake this place up.", "Bicep — Glue"),
        ("I've been waiting for someone to say that.", "Moderat — Bad Kingdom"),
        ("Full send.", "Four Tet — Angel Echoes")
    };

    static readonly (string line, string track)[] MOOD_NOSTALGIC = {
        ("Close your eyes. Go back.", "The xx — Intro"),
        ("Some songs are just time machines.", "Mazzy Star — Fade Into You"),
        ("This one always brings people home.", "Beach House — Space Song")
    };

    static readonly (string line, string track)[] MOOD_CHILL = {
        ("Exactly what this bar was made for.", "Bonobo — Kiara"),
        ("Good call. Let it slow down.", "Tycho — Awake"),
        ("Perfect. No rush tonight.", "Khruangbin — Lady and Man")
    };

    // ── State ────────────────────────────────────────────────────────────────
    enum DriftState { Idle, Approaching, Talking, Returning }
    DriftState _state = DriftState.Idle;
    Coroutine _idleCycleRoutine;
    Coroutine _approachRoutine;

    // ── Lifecycle ────────────────────────────────────────────────────────────
    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (playerCamera == null) playerCamera = Camera.main?.transform;
        StartIdleCycle();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void PlayIdleGreeting()
    {
        if (animator) animator.SetTrigger(ANIM_WAVE);
        // DRIFT waves as player enters
    }

    public void OnPlayerApproached()
    {
        if (_state == DriftState.Approaching || _state == DriftState.Talking) return;
        StopIdleCycle();
        _approachRoutine = StartCoroutine(ApproachRoutine());
    }

    public void ReturnToIdle()
    {
        if (_approachRoutine != null) StopCoroutine(_approachRoutine);
        StartCoroutine(ReturnToBarRoutine());
    }

    /// <summary>Returns a contextual greeting line.</summary>
    public string GetGreetingLine()
    {
        return greetingLines[Random.Range(0, greetingLines.Length)];
    }

    /// <summary>Returns a deep-mode philosophical line.</summary>
    public string GetDeepLine()
    {
        return deepModeLines[Random.Range(0, deepModeLines.Length)];
    }

    /// <summary>
    /// Returns DRIFT's response to the player's mood selection.
    /// Out: the dialogue line and the track name to show in NowPlaying.
    /// </summary>
    public (string line, string track) GetMoodResponse(MoodType mood)
    {
        var pool = mood switch
        {
            MoodType.Melancholic => MOOD_MELANCHOLIC,
            MoodType.Energetic   => MOOD_ENERGETIC,
            MoodType.Nostalgic   => MOOD_NOSTALGIC,
            MoodType.Chill       => MOOD_CHILL,
            _                    => MOOD_CHILL
        };
        return pool[Random.Range(0, pool.Length)];
    }

    /// <summary>Plays the React_Positive animation when player picks a mood.</summary>
    public void ReactPositive()
    {
        if (animator) animator.SetTrigger(ANIM_REACT_POS);
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    void StartIdleCycle()
    {
        _idleCycleRoutine = StartCoroutine(IdleCycleRoutine());
    }

    void StopIdleCycle()
    {
        if (_idleCycleRoutine != null) StopCoroutine(_idleCycleRoutine);
    }

    IEnumerator IdleCycleRoutine()
    {
        // Spec §9.4: 3 idle animations in random cycle
        int[] idleAnims = { ANIM_IDLE_WIPE, ANIM_IDLE_LOOK, ANIM_IDLE_LEAN };
        while (true)
        {
            int pick = idleAnims[Random.Range(0, idleAnims.Length)];
            if (animator) animator.SetTrigger(pick);
            yield return new WaitForSeconds(Random.Range(idleCycleMin, idleCycleMax));
        }
    }

    IEnumerator ApproachRoutine()
    {
        _state = DriftState.Approaching;
        if (animator) animator.SetTrigger(ANIM_APPROACH);

        // Move DRIFT toward approachPosition (leans forward / comes to front of bar)
        if (approachPosition != null)
        {
            float t = 0f;
            float duration = 0.6f; // spec: 600ms slow animation
            Vector3 start = transform.position;
            Quaternion startRot = transform.rotation;
            Vector3 target = approachPosition.position;
            Quaternion targetRot = approachPosition.rotation;

            while (t < duration)
            {
                t += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, t / duration);
                transform.position = Vector3.Lerp(start, target, progress);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, progress);
                yield return null;
            }
            transform.position = target;
            transform.rotation = targetRot;
        }

        _state = DriftState.Talking;
        if (animator) animator.SetTrigger(ANIM_TALK);
    }

    IEnumerator ReturnToBarRoutine()
    {
        _state = DriftState.Returning;
        if (barPosition != null)
        {
            float t = 0f;
            float duration = 0.4f;
            Vector3 start = transform.position;
            Quaternion startRot = transform.rotation;

            while (t < duration)
            {
                t += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, t / duration);
                transform.position = Vector3.Lerp(start, barPosition.position, progress);
                transform.rotation = Quaternion.Slerp(startRot, barPosition.rotation, progress);
                yield return null;
            }
        }

        _state = DriftState.Idle;
        StartIdleCycle();
    }
}
