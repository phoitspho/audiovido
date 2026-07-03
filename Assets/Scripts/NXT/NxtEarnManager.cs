using System.Collections;
using UnityEngine;

/// <summary>
/// AUDIOVIDO — NXT Token Earn Manager
/// Spec: users earn NXT by spending time in 3D spaces.
///
/// Earn rates:
///   • Passive:     1 NXT per 60s while in any space
///   • Interaction: 5 NXT bonus on first mood selection per session
///   • Deep mode:   2 NXT bonus when DRIFT enters deep conversation
///
/// Singleton — persists across scene loads (DontDestroyOnLoad).
/// Subscribe to OnNxtChanged to receive (newTotal, delta) callbacks.
/// </summary>
public class NxtEarnManager : MonoBehaviour
{
    public static NxtEarnManager Instance { get; private set; }

    /// <summary>Fired whenever balance changes: (newTotal, delta).</summary>
    public static event System.Action<int, int> OnNxtChanged;

    [Header("Balance")]
    [SerializeField] int startingBalance = 0;

    [Header("Passive Earn")]
    [SerializeField] float earnIntervalSeconds = 60f;  // 1 NXT per minute
    [SerializeField] int   passiveEarnAmount   = 1;

    [Header("Bonus Earn")]
    [SerializeField] int interactionBonus = 5;  // on mood pick
    [SerializeField] int deepModeBonus    = 2;  // on deep mode engage

    int  _balance;
    bool _passiveRunning;
    bool _interactionBonusAwarded; // once per session

    public int Balance => _balance;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _balance = startingBalance;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Start passive earning. Call when entering a 3D space.</summary>
    public void StartEarning()
    {
        if (_passiveRunning) return;
        _passiveRunning = true;
        StartCoroutine(PassiveEarnRoutine());
    }

    /// <summary>Stop passive earning. Call when leaving a 3D space.</summary>
    public void StopEarning()
    {
        _passiveRunning = false;
        StopAllCoroutines();
    }

    /// <summary>Award the first-interaction bonus when player picks a mood.</summary>
    public void AwardInteractionBonus()
    {
        if (_interactionBonusAwarded) return;
        _interactionBonusAwarded = true;
        AddNxt(interactionBonus, "Interaction");
    }

    /// <summary>Award NXT for a deep-mode DRIFT conversation.</summary>
    public void AwardDeepModeBonus()
    {
        AddNxt(deepModeBonus, "Deep Mode");
    }

    /// <summary>Manually add NXT (for testing or special events).</summary>
    public void Add(int amount) => AddNxt(amount, "Manual");

    // ── Internal ─────────────────────────────────────────────────────────────

    void AddNxt(int delta, string reason = "")
    {
        if (delta <= 0) return;
        _balance += delta;
        Debug.Log($"[NXT] +{delta} ({reason}) → total {_balance}");
        OnNxtChanged?.Invoke(_balance, delta);
    }

    IEnumerator PassiveEarnRoutine()
    {
        while (_passiveRunning)
        {
            yield return new WaitForSeconds(earnIntervalSeconds);
            if (_passiveRunning)
                AddNxt(passiveEarnAmount, "Passive");
        }
    }
}
