using System;

/// <summary>
/// AUDIOVIDO — AI Chat data contracts (spec §11.7)
///
///   POST /ai/chat/{characterId}
///   Body:     { message, context, screenState }
///   Response: { reply, emotion, animation }
///
/// These classes serialize 1:1 with the backend contract so the remote
/// provider (Pedram's API) plugs in without touching UI code.
/// </summary>
[Serializable]
public class ChatRequest
{
    public string message;
    public string context;      // e.g. recent conversation summary
    public string screenState;  // e.g. "lounge/ambient", "cinema/paused"
}

[Serializable]
public class ChatReply
{
    public string reply;
    public string emotion;      // drives avatar mood (spec §12.4 state machine)
    public string animation;    // e.g. "React_Positive", "Talk", "Wave"
}
