using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// AUDIOVIDO — Character Chat Service (spec §11.7)
/// Single entry point for AI character conversations.
///
/// Two providers behind one call:
///   • LOCAL  (default): keyword personality engine from CharacterProfiles,
///     with a simulated "typing" delay — used until the backend exists.
///   • REMOTE: POST {apiBaseUrl}/ai/chat/{characterId} with
///     { message, context, screenState } → { reply, emotion, animation }.
///     Activate by setting apiBaseUrl (e.g. from a config screen) — no other
///     code changes needed. Backend: Pedram.
///
/// Lazy singleton — created on first use, survives scene loads.
/// </summary>
public class CharacterChatService : MonoBehaviour
{
    public static CharacterChatService Instance { get; private set; }

    [Header("Remote API (leave empty to use the local engine)")]
    [SerializeField] string apiBaseUrl = "";
    [SerializeField] float requestTimeoutSeconds = 10f;

    [Header("Local engine feel")]
    [SerializeField] float minTypingDelay = 0.7f;
    [SerializeField] float maxTypingDelay = 1.5f;

    System.Random _rng;

    public static CharacterChatService EnsureExists()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("CharacterChatService");
            Instance = go.AddComponent<CharacterChatService>();
        }
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _rng = new System.Random();
    }

    /// <summary>
    /// Request a character reply. <paramref name="onReply"/> is always called
    /// (local engine answers even if the remote API fails — graceful degrade).
    /// </summary>
    public void RequestReply(string characterId, string userMessage,
        string screenState, Action<ChatReply> onReply)
    {
        if (!string.IsNullOrEmpty(apiBaseUrl))
            StartCoroutine(RemoteRoutine(characterId, userMessage, screenState, onReply));
        else
            StartCoroutine(LocalRoutine(characterId, userMessage, onReply));
    }

    // ── Local personality engine ─────────────────────────────────────────────

    IEnumerator LocalRoutine(string characterId, string userMessage, Action<ChatReply> onReply)
    {
        // Feels like someone typing on the other end
        yield return new WaitForSeconds(
            UnityEngine.Random.Range(minTypingDelay, maxTypingDelay));

        onReply?.Invoke(LocalReply(characterId, userMessage));
    }

    ChatReply LocalReply(string characterId, string userMessage)
    {
        CharacterProfile profile = CharacterProfiles.Get(characterId);
        if (profile == null)
            return new ChatReply { reply = "...", emotion = "neutral", animation = "Idle_1" };

        string msg = (userMessage ?? "").ToLowerInvariant();

        foreach ((string[] keywords, ChatReply[] replies) rule in profile.rules)
        {
            foreach (string k in rule.keywords)
            {
                if (msg.Contains(k))
                    return rule.replies[_rng.Next(rule.replies.Length)];
            }
        }
        return profile.fallbacks[_rng.Next(profile.fallbacks.Length)];
    }

    // ── Remote provider (spec §11.7, backend: Pedram) ────────────────────────

    IEnumerator RemoteRoutine(string characterId, string userMessage,
        string screenState, Action<ChatReply> onReply)
    {
        ChatRequest body = new ChatRequest
        {
            message = userMessage,
            context = "",            // Phase 2: rolling conversation summary
            screenState = screenState
        };

        string url = $"{apiBaseUrl.TrimEnd('/')}/ai/chat/{characterId}";
        using (UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] payload = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));
            req.uploadHandler = new UploadHandlerRaw(payload);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = Mathf.CeilToInt(requestTimeoutSeconds);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                ChatReply reply = null;
                try { reply = JsonUtility.FromJson<ChatReply>(req.downloadHandler.text); }
                catch (Exception e) { Debug.LogWarning($"[Chat] Bad API response: {e.Message}"); }

                if (reply != null && !string.IsNullOrEmpty(reply.reply))
                {
                    onReply?.Invoke(reply);
                    yield break;
                }
            }
            Debug.LogWarning($"[Chat] API unavailable ({req.result}) — using local engine.");
        }

        // Graceful degrade: never leave the character speechless
        onReply?.Invoke(LocalReply(characterId, userMessage));
    }
}
