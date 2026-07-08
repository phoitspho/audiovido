using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// AUDIOVIDO — Android Build Pipeline (spec §12.1: Android 8+, URP)
/// Menu: AUDIOVIDO → Android → Configure / Build APK
///
/// Settings per spec + store requirements:
///   • Landscape-only, package com.petros.audiovido
///   • Min SDK 26 (Android 8), IL2CPP + ARM64 (modern devices)
///   • ASTC texture compression (URP mobile default)
/// Output: Builds/audiovido.apk (gitignored).
/// </summary>
public static class AndroidBuildSetup
{
    public static readonly string[] Scenes =
    {
        "Assets/Scenes/Scene_City.unity",
        "Assets/Scenes/Scene_Home.unity",
        "Assets/Scenes/Scene_Lounge.unity",
        "Assets/Scenes/Scene_Cinema.unity",
        "Assets/Scenes/Scene_Arena.unity",
        "Assets/Scenes/Scene_Plaza.unity"
    };

    public static bool AndroidModuleInstalled =>
        BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);

    [MenuItem("AUDIOVIDO/Android/Configure Player Settings")]
    public static void Configure()
    {
        PlayerSettings.companyName = "Petros Entertainment";
        PlayerSettings.productName = "AUDIOVIDO";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.petros.audiovido");
        // Landscape-only: auto-rotate between the two landscape orientations, portrait disabled.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait           = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft      = true;
        PlayerSettings.allowedAutorotateToLandscapeRight     = true;

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // Android 8
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

        Debug.Log("[AndroidSetup] Player settings configured (landscape, IL2CPP/ARM64, minSdk 26).");
    }

    [MenuItem("AUDIOVIDO/Android/Build APK")]
    public static void BuildApk()
    {
        if (!AndroidModuleInstalled)
        {
            Debug.LogError("[AndroidSetup] Android Build Support is NOT installed. " +
                "Unity Hub → Installs → gear icon on 6000.3.18f1 → Add modules → " +
                "Android Build Support (incl. SDK & NDK Tools + OpenJDK).");
            return;
        }

        Configure();

        System.IO.Directory.CreateDirectory("Builds");
        BuildReport report = BuildPipeline.BuildPlayer(
            Scenes, "Builds/audiovido.apk", BuildTarget.Android, BuildOptions.None);

        BuildSummary s = report.summary;
        if (s.result == BuildResult.Succeeded)
            Debug.Log($"[AndroidSetup] APK BUILD SUCCEEDED — {s.totalSize / (1024 * 1024)} MB " +
                      $"at {s.outputPath} ({s.totalTime.TotalMinutes:F1} min, " +
                      $"{s.totalErrors} errors, {s.totalWarnings} warnings)");
        else
            Debug.LogError($"[AndroidSetup] APK build {s.result}: " +
                           $"{s.totalErrors} errors — see log above.");
    }
}
