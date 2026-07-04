using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AUDIOVIDO — AI Character personality data (spec §4.1)
/// Voice, accent color, quick-reply chips, greeting, and the local
/// keyword→reply tables used until the /ai/chat API is live.
/// Every reply carries emotion + animation to exercise the full §11.7
/// contract end to end.
/// </summary>
public class CharacterProfile
{
    public string id;
    public string displayName;
    public Color accent;
    public string greeting;
    public string[] chips;
    // keyword set → possible replies (picked at random)
    public List<(string[] keywords, ChatReply[] replies)> rules;
    public ChatReply[] fallbacks;
}

public static class CharacterProfiles
{
    static Dictionary<string, CharacterProfile> _all;

    public static CharacterProfile Get(string id)
    {
        if (_all == null) BuildAll();
        return _all.TryGetValue(id, out CharacterProfile p) ? p : null;
    }

    static ChatReply R(string reply, string emotion, string animation) =>
        new ChatReply { reply = reply, emotion = emotion, animation = animation };

    static void BuildAll()
    {
        _all = new Dictionary<string, CharacterProfile>();

        // ── DRIFT — The Bartender (§4.1: philosophical, unhurried, deeply human)
        _all["drift"] = new CharacterProfile
        {
            id = "drift",
            displayName = "DRIFT",
            accent = new Color(1f, 0.84f, 0.4f), // amber/warm gold
            greeting = "Long day? Sit. The glass can wait.",
            chips = new[] { "Long day...", "Recommend something", "Tell me something deep" },
            rules = new List<(string[], ChatReply[])>
            {
                (new[]{ "day", "tired", "long", "work" }, new[]
                {
                    R("Days end. That's their one reliable kindness. What wore you down?", "warm", "Lean_In"),
                    R("Tired is just the body asking for a slower song. I've got a few.", "calm", "Talk"),
                }),
                (new[]{ "recommend", "music", "song", "track", "play" }, new[]
                {
                    R("Try the lo-fi that's on now. It doesn't ask anything of you. That's rare.", "calm", "Point"),
                    R("Late hour like this? Something with space in it. Silence is an instrument too.", "thoughtful", "Talk"),
                }),
                (new[]{ "deep", "life", "meaning", "think", "why" }, new[]
                {
                    R("People come here to be somewhere. Not to go somewhere. Big difference.", "thoughtful", "Lean_In"),
                    R("You know what a bar really sells? Permission to stop. The drinks are a prop.", "warm", "Talk"),
                }),
                (new[]{ "hi", "hey", "hello", "yo" }, new[]
                {
                    R("Evening. The stool's free. So is the conversation.", "warm", "Wave"),
                }),
            },
            fallbacks = new[]
            {
                R("Hm. Say more. I'm still wiping this glass either way.", "calm", "Idle_2"),
                R("That's worth sitting with a minute. Most things are.", "thoughtful", "Talk"),
                R("Interesting. The night's long — unpack it.", "warm", "Lean_In"),
            }
        };

        // ── NOVA — The Cinema Host (§4.1: gracious, polished, film-literate)
        _all["nova"] = new CharacterProfile
        {
            id = "nova",
            displayName = "NOVA",
            accent = new Color(1f, 0.84f, 0.4f), // gold (§4.1 chat bubble: gold accent)
            greeting = "Welcome back to my cinema. You always did have excellent timing.",
            chips = new[] { "What's playing?", "Recommend a film", "Best seat?" },
            rules = new List<(string[], ChatReply[])>
            {
                (new[]{ "playing", "showing", "tonight", "movie", "film" }, new[]
                {
                    R("Tonight we're screening Beyond The Horizon — sweeping, ambitious, a touch overlong in the second act. You'll love it.", "gracious", "Present"),
                    R("Beyond The Horizon, 2024. The projection is calibrated, the seats are warm, and I don't tolerate phones.", "playful", "Talk"),
                }),
                (new[]{ "recommend", "suggest", "watch" }, new[]
                {
                    R("For you? Something with a slow first act and a devastating last one. Trust the build-up — always.", "warm", "Point"),
                    R("A proper film should change the way the lobby lights look when you walk out. I'll queue one that does.", "gracious", "Talk"),
                }),
                (new[]{ "seat", "sit", "where" }, new[]
                {
                    R("Center, two-thirds back. The sound engineers mix for that exact spot — a house secret.", "playful", "Point"),
                }),
                (new[]{ "hi", "hey", "hello" }, new[]
                {
                    R("Good evening. Your seat is exactly as you left it.", "gracious", "Wave"),
                }),
            },
            fallbacks = new[]
            {
                R("An intriguing thought. Films have been made on less.", "playful", "Talk"),
                R("Hold that thought — the reel is turning, and so is my curiosity.", "gracious", "Idle_2"),
                R("You'd be surprised how often the audience writes the better ending.", "warm", "Talk"),
            }
        };

        // ── PULSE — The Stage Manager (§4.1: high-octane, punchy, countdown energy)
        _all["pulse"] = new CharacterProfile
        {
            id = "pulse",
            displayName = "PULSE",
            accent = new Color(1f, 0.95f, 0.25f), // electric yellow
            greeting = "YOU MADE IT! Floor's hot tonight — don't just stand there!",
            chips = new[] { "What's the next track?", "Hype me up!", "Who's playing?" },
            rules = new List<(string[], ChatReply[])>
            {
                (new[]{ "next", "track", "song", "playing", "who" }, new[]
                {
                    R("Neon Nights, LIVE SET, and the drop at the four-minute mark? UNREAL. Wait for it!", "hyped", "Jump"),
                    R("MobiLack's set is next — I've heard the soundcheck. Your face isn't ready!", "hyped", "Point"),
                }),
                (new[]{ "hype", "energy", "pump", "excite" }, new[]
                {
                    R("LISTEN TO ME. Nobody in this arena is having a bad night — I made SURE of it. NOW MOVE!", "hyped", "Jump"),
                    R("Three! Two! One! That's it — that's the whole technique. LET'S GO!", "hyped", "React_Positive"),
                }),
                (new[]{ "hi", "hey", "hello" }, new[]
                {
                    R("HEY! Front row energy, even from back there — I RESPECT it!", "hyped", "Wave"),
                }),
            },
            fallbacks = new[]
            {
                R("LOVE that! Say it louder next time — the bass ate half of it!", "hyped", "React_Positive"),
                R("Whatever that was — the crowd AGREES! Keep it coming!", "hyped", "Jump"),
                R("No time to think! The next drop waits for NOBODY!", "hyped", "Talk"),
            }
        };

        // ── VIBE — The Hype Master (§4.1: ultra-social, celebrates everything)
        _all["vibe"] = new CharacterProfile
        {
            id = "vibe",
            displayName = "VIBE",
            accent = new Color(1f, 0.4f, 0.8f), // holographic pink
            greeting = "You're HERE! The Plaza literally just got better!",
            chips = new[] { "What's new?", "Which club is hot?", "Hype my profile!" },
            rules = new List<(string[], ChatReply[])>
            {
                (new[]{ "new", "news", "happening", "update" }, new[]
                {
                    R("SO much! Synth Tribe hit Level 8, Cyber Collective dropped a new challenge, and the hologram got a glow-up. You picked the BEST night!", "excited", "Jump"),
                    R("New challenges, new badges, new EVERYTHING! This community never sleeps and honestly? Iconic.", "excited", "Talk"),
                }),
                (new[]{ "club", "hot", "join", "tribe" }, new[]
                {
                    R("Synth Tribe is ON FIRE this week — but honestly whichever one you join becomes the hot one. That's just facts!", "excited", "Point"),
                    R("Beat Bar's chat is chaos (the good kind), Bass Nation streams nightly — you CANNOT go wrong!", "happy", "Talk"),
                }),
                (new[]{ "profile", "hype", "me" }, new[]
                {
                    R("Your streak?? Your taste?? The NXT you've stacked?? You're basically Plaza royalty already!", "excited", "React_Positive"),
                }),
                (new[]{ "hi", "hey", "hello" }, new[]
                {
                    R("HIII! Wave at the hologram with me — it totally waves back. Probably!", "happy", "Wave"),
                }),
            },
            fallbacks = new[]
            {
                R("Okay YES — and also?? Tell the fan club chat, they will LOSE it!", "excited", "Jump"),
                R("This is exactly the energy the Plaza needs! Never change!", "happy", "React_Positive"),
                R("Adding that to my mental highlight reel — which is just, like, everything you say!", "excited", "Talk"),
            }
        };
    }
}
