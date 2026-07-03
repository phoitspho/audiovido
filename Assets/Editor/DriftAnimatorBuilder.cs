using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// AUDIOVIDO — DRIFT Animator Controller Builder
/// Menu: AUDIOVIDO → Build DRIFT Animator
/// Generates Assets/Animations/DRIFT_Controller.controller with:
///   • All trigger parameters DriftController.cs expects
///   • AnyState transitions so triggers fire from any state
///   • Wave + React_Positive auto-return to Idle_WipeGlass
/// After running: drag DRIFT_Controller onto the DRIFT Capsule's Animator component.
/// </summary>
public static class DriftAnimatorBuilder
{
    const string CONTROLLER_PATH = "Assets/Animations/DRIFT_Controller.controller";

    [MenuItem("AUDIOVIDO/Build DRIFT Animator")]
    public static void BuildAnimator()
    {
        // Ensure directory exists
        Directory.CreateDirectory(Application.dataPath + "/Animations");
        AssetDatabase.Refresh();

        // Remove stale controller
        if (File.Exists(Application.dataPath.Replace("Assets", "") + CONTROLLER_PATH))
            AssetDatabase.DeleteAsset(CONTROLLER_PATH);

        // ── Create controller ────────────────────────────────────────────────
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

        // ── Parameters (spec §9.4 — triggers for each state) ────────────────
        ctrl.AddParameter("Idle_WipeGlass",  AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Idle_LookAround", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Idle_Lean",       AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Approach",        AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Talk",            AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("React_Positive",  AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Wave",            AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("State",           AnimatorControllerParameterType.Int);

        // ── State Machine ────────────────────────────────────────────────────
        var sm = ctrl.layers[0].stateMachine;

        // States — positions are editor graph coords only
        var sWipe     = sm.AddState("Idle_WipeGlass",  new Vector3(250, 40));
        var sLook     = sm.AddState("Idle_LookAround", new Vector3(250, 100));
        var sLean     = sm.AddState("Idle_Lean",       new Vector3(250, 160));
        var sApproach = sm.AddState("Approach",        new Vector3(500, 40));
        var sTalk     = sm.AddState("Talk",            new Vector3(500, 100));
        var sReact    = sm.AddState("React_Positive",  new Vector3(500, 160));
        var sWave     = sm.AddState("Wave",            new Vector3(500, 220));

        sm.defaultState = sWipe; // start wiping glass

        // ── AnyState → each state via trigger ────────────────────────────────
        AddAnyTrigger(sm, sWipe,     "Idle_WipeGlass",  0.15f);
        AddAnyTrigger(sm, sLook,     "Idle_LookAround", 0.15f);
        AddAnyTrigger(sm, sLean,     "Idle_Lean",       0.15f);
        AddAnyTrigger(sm, sApproach, "Approach",        0.25f);
        AddAnyTrigger(sm, sTalk,     "Talk",            0.2f);
        AddAnyTrigger(sm, sReact,    "React_Positive",  0.15f);
        AddAnyTrigger(sm, sWave,     "Wave",            0.15f);

        // ── Auto-return: Wave → WipeGlass (at 90% through clip) ──────────────
        var tWaveReturn = sWave.AddTransition(sWipe);
        tWaveReturn.hasExitTime  = true;
        tWaveReturn.exitTime     = 0.9f;
        tWaveReturn.duration     = 0.2f;
        tWaveReturn.hasFixedDuration = false;

        // ── Auto-return: React_Positive → WipeGlass ──────────────────────────
        var tReactReturn = sReact.AddTransition(sWipe);
        tReactReturn.hasExitTime  = true;
        tReactReturn.exitTime     = 0.9f;
        tReactReturn.duration     = 0.2f;
        tReactReturn.hasFixedDuration = false;

        // ── Auto-return: Approach → Talk (DRIFT leans in, then starts talking) ─
        var tApproachTalk = sApproach.AddTransition(sTalk);
        tApproachTalk.hasExitTime  = true;
        tApproachTalk.exitTime     = 0.85f;
        tApproachTalk.duration     = 0.2f;
        tApproachTalk.hasFixedDuration = false;

        // ── Save ──────────────────────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DriftAnimatorBuilder] {CONTROLLER_PATH} created.");
        EditorUtility.DisplayDialog("Done",
            "DRIFT Animator Controller built!\n\n" +
            "Path: Assets/Animations/DRIFT_Controller.controller\n\n" +
            "In Scene_Lounge hierarchy:\n" +
            "1. Select the DRIFT Capsule (DRIFT_Character)\n" +
            "2. Add an Animator component if missing\n" +
            "3. Drag DRIFT_Controller.controller into the Controller slot", "OK");
    }

    static void AddAnyTrigger(AnimatorStateMachine sm, AnimatorState target,
                               string triggerName, float blendDuration)
    {
        var t = sm.AddAnyStateTransition(target);
        t.AddCondition(AnimatorConditionMode.If, 0, triggerName);
        t.hasExitTime        = false;
        t.duration           = blendDuration;
        t.hasFixedDuration   = false;
        t.canTransitionToSelf = false; // don't restart same state
    }
}
