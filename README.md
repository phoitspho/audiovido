# AUDIOVIDO

**Music & Movies — Free Forever.**

Unity 6 (URP) mobile app prototype. The app opens into a navigable neon
night city — districts ARE the navigation:

| District | Space | AI Character |
|---|---|---|
| Home District | Your Room (themes, record player) | — |
| Music Street | The Lounge | DRIFT |
| Club & Dance Arena | Live venue (visualizer, crowd, HYPE!) | PULSE |
| Cinema District | Cinema (projection, play/pause) | NOVA |
| Fan Plaza | Social hub (hologram, WAVE!) | VIBE |

## Structure

- `Assets/Scenes/` — Scene_City (entry) + one scene per district space
- `Assets/Scripts/` — runtime code (City, Home, Lounge, Cinema, Arena, Plaza, NXT, UI)
- `Assets/Editor/` — AUDIOVIDO menu: scene builders that regenerate every scene from code
- `Assets/Materials/` — generated URP materials (Unlit HDR = neon glow)

## How to run

1. Open with **Unity 6.x (6000.3+)**, URP project.
2. Open `Assets/Scenes/Scene_City.unity`.
3. Press Play. Drag to orbit, tap a district, Enter.

Scenes can be regenerated any time via the **AUDIOVIDO** menu in the editor.

## Status

Gray-box prototype: all 5 districts playable, NXT token earning persists
across spaces, EventSystem/scene-handoff architecture proven. Next up:
audio pipeline (FFT-driven visualizers), AI character chat (API),
art pass, Android build.

Backend/API: Pedram · Unity frontend: PHO
