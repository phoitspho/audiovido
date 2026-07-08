using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// AUDIOVIDO — Landscape Converter
/// Menu: AUDIOVIDO → Landscape → Convert To Landscape
///
/// Flips the whole app from portrait to landscape in one idempotent pass:
///   1. PlayerSettings → AutoRotation, portrait disabled, both landscapes enabled.
///   2. Every CanvasScaler in every scene → reference 844×390, match height (1),
///      MatchWidthOrHeight mode. Fixes canvases already baked into the scene assets
///      (patching the scene *builders* alone only affects future rebuilds).
///
/// Safe to run repeatedly. Remembers the open scene and restores it afterward.
/// Build-time orientation is also enforced in AndroidBuildSetup.Configure(), so
/// a build can never silently revert to portrait.
/// </summary>
public static class LandscapeConverter
{
    // Landscape UI design frame (portrait 390×844 → landscape 844×390).
    static readonly Vector2 LandscapeReference = new Vector2(844f, 390f);

    static readonly string[] Scenes =
    {
        "Assets/Scenes/Scene_City.unity",
        "Assets/Scenes/Scene_Home.unity",
        "Assets/Scenes/Scene_Lounge.unity",
        "Assets/Scenes/Scene_Cinema.unity",
        "Assets/Scenes/Scene_Arena.unity",
        "Assets/Scenes/Scene_Plaza.unity",
        "Assets/Scenes/MainScene.unity"
    };

    [MenuItem("AUDIOVIDO/Landscape/Convert To Landscape")]
    public static void ConvertToLandscape()
    {
        ApplyOrientation();

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("[Landscape] Cancelled — unsaved changes in the current scene were not saved.");
            return;
        }

        string original = SceneManager.GetActiveScene().path;
        int scenesFixed = 0, scalersFixed = 0;

        foreach (string path in Scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int fixedHere = FixCanvasScalersInOpenScene();
            if (fixedHere > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                scenesFixed++;
                scalersFixed += fixedHere;
            }
            Debug.Log($"[Landscape] {System.IO.Path.GetFileName(path)} — {fixedHere} CanvasScaler(s) set to landscape.");
        }

        if (!string.IsNullOrEmpty(original))
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);

        Debug.Log($"[Landscape] DONE — orientation set to landscape; " +
                  $"{scalersFixed} CanvasScaler(s) across {scenesFixed} scene(s) updated.");
    }

    [MenuItem("AUDIOVIDO/Landscape/Set Orientation Only")]
    public static void ApplyOrientation()
    {
        PlayerSettings.defaultInterfaceOrientation           = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait           = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft      = true;
        PlayerSettings.allowedAutorotateToLandscapeRight     = true;
        Debug.Log("[Landscape] PlayerSettings orientation = AutoRotation (landscape only).");
    }

    static int FixCanvasScalersInOpenScene()
    {
        int count = 0;
        var seen = new HashSet<CanvasScaler>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (CanvasScaler scaler in root.GetComponentsInChildren<CanvasScaler>(true))
            {
                if (!seen.Add(scaler)) continue;
                scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = LandscapeReference;
                scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f; // match height — the shorter axis in landscape
                EditorUtility.SetDirty(scaler);
                count++;
            }
        }
        return count;
    }
}
