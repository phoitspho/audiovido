using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// AUDIOVIDO — Lounge Scene Validator
/// Menu: AUDIOVIDO → Validate Lounge Scene
///
/// Checks every reference needed for the end-to-end flow:
///   MainScene button → SceneLoader → fade → Scene_Lounge
///   Tap stool → LoungeInteractable → LoungeManager.OnPlayerApproachBar()
///   → DriftController.OnPlayerApproached() → bubble "Long day?" → auto-dismiss
///
/// Run while Scene_Lounge is the active scene.
/// </summary>
public static class LoungeValidator
{
    static List<string> _failures = new List<string>();
    static StringBuilder _log     = new StringBuilder();
    static int _pass, _fail;

    [MenuItem("AUDIOVIDO/Validate Lounge Scene")]
    public static void Validate()
    {
        _failures.Clear();
        _log.Clear();
        _pass = 0; _fail = 0;

        _log.AppendLine("=== AUDIOVIDO Lounge Validation ===\n");

        // ── LoungeManager ─────────────────────────────────────────────────────
        _log.AppendLine("[LoungeManager]");
        var mgr = Object.FindFirstObjectByType<LoungeManager>();
        if (mgr == null) { Fail("LoungeManager not found in scene"); }
        else
        {
            Pass("LoungeManager found");
            var so = new SerializedObject(mgr);
            CheckRef(so, "drift",        "DriftController ref");
            CheckRef(so, "ui",           "LoungeUIController ref");
            CheckRef(so, "fadeCanvas",   "FadeCanvas CanvasGroup");
            CheckRef(so, "ambientAudio", "AudioSource (ambient)");
        }

        // ── DriftController ───────────────────────────────────────────────────
        _log.AppendLine("\n[DriftController]");
        var drift = Object.FindFirstObjectByType<DriftController>();
        if (drift == null) { Fail("DriftController not found"); }
        else
        {
            Pass("DriftController found");
            var so = new SerializedObject(drift);
            CheckRef(so, "barPosition",      "barPosition Transform");
            CheckRef(so, "approachPosition", "approachPosition Transform");
            CheckRef(so, "playerCamera",     "playerCamera Transform");

            var anim = drift.GetComponent<Animator>();
            if (anim == null || anim.runtimeAnimatorController == null)
                Fail("Animator / AnimatorController missing on DRIFT Capsule");
            else
                Pass($"AnimatorController: {anim.runtimeAnimatorController.name}");
        }

        // ── LoungeUIController ────────────────────────────────────────────────
        _log.AppendLine("\n[LoungeUIController]");
        var ui = Object.FindFirstObjectByType<LoungeUIController>();
        if (ui == null) { Fail("LoungeUIController not found"); }
        else
        {
            Pass("LoungeUIController found");
            var so = new SerializedObject(ui);
            CheckRef(so, "exitButton",     "exitButton");
            CheckRef(so, "sitHereButton",  "sitHereButton");
            CheckRef(so, "driftBubbleRoot","driftBubbleRoot");
            CheckRef(so, "driftBubbleText","driftBubbleText");
            CheckRef(so, "nowPlayingText", "nowPlayingText");
        }

        // ── LoungeInteractables ───────────────────────────────────────────────
        _log.AppendLine("\n[LoungeInteractables]");
        var interactables = Object.FindObjectsByType<LoungeInteractable>(FindObjectsSortMode.None);
        if (interactables.Length == 0)
            Fail("No LoungeInteractable found — stools/tables won't respond to taps");
        else
            Pass($"{interactables.Length} LoungeInteractable(s) present");

        // ── LoungeInputManager ────────────────────────────────────────────────
        _log.AppendLine("\n[LoungeInputManager]");
        var input = Object.FindFirstObjectByType<LoungeInputManager>();
        if (input == null)
            Fail("LoungeInputManager missing — tap/raycast won't work on device");
        else
            Pass("LoungeInputManager on " + input.gameObject.name);

        // ── Camera ────────────────────────────────────────────────────────────
        _log.AppendLine("\n[Camera]");
        var cam = Camera.main;
        if (cam == null) Fail("No Main Camera tagged 'MainCamera'");
        else
        {
            Pass($"Main Camera at {cam.transform.position}");
            if (cam.GetComponent<LoungeInputManager>() != null)
                Pass("LoungeInputManager is on Main Camera (correct)");
        }

        // ── EventSystem ───────────────────────────────────────────────────────
        _log.AppendLine("\n[EventSystem]");
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            Fail("EventSystem missing — UI buttons won't fire");
        else
            Pass("EventSystem present");

        // ── Build Settings ────────────────────────────────────────────────────
        _log.AppendLine("\n[Build Settings]");
        bool hasLounge = false, hasMain = false;
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path.Contains("Scene_Lounge")) hasLounge = true;
            if (scene.path.Contains("MainScene"))    hasMain   = true;
        }
        if (hasLounge) Pass("Scene_Lounge in Build Settings");
        else           Fail("Scene_Lounge NOT in Build Settings");
        if (hasMain)   Pass("MainScene in Build Settings");
        else           Fail("MainScene NOT in Build Settings");

        // ── DRIFT Animator Controller ─────────────────────────────────────────
        _log.AppendLine("\n[DRIFT Animator]");
        bool controllerExists = System.IO.File.Exists(
            Application.dataPath + "/Animations/DRIFT_Controller.controller");
        if (controllerExists) Pass("DRIFT_Controller.controller exists");
        else                  Fail("Run AUDIOVIDO > Build DRIFT Animator first");

        // ── Summary ───────────────────────────────────────────────────────────
        _log.AppendLine($"\n=== {_pass} passed  |  {_fail} failed ===");
        Debug.Log(_log.ToString());

        if (_fail == 0)
        {
            EditorUtility.DisplayDialog("All clear",
                $"[AUDIOVIDO] {_pass} checks passed.\n\nScene_Lounge is ready for play.", "OK");
        }
        else
        {
            var errList = string.Join("\n", _failures);
            EditorUtility.DisplayDialog("Issues found",
                $"{_fail} issue(s):\n\n{errList}\n\nSee Console for full report.", "OK");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void Pass(string msg) { _log.AppendLine("  [OK]  " + msg); _pass++; }
    static void Fail(string msg) { _log.AppendLine("  [!!]  " + msg); _failures.Add("• " + msg); _fail++; }

    static void CheckRef(SerializedObject so, string field, string label)
    {
        var prop = so.FindProperty(field);
        if (prop == null || prop.objectReferenceValue == null)
            Fail($"{label} not assigned");
        else
            Pass($"{label} → {prop.objectReferenceValue.name}");
    }
}
