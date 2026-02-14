using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public class BuildScript
{
    public static void BuildiOS()
    {
        PlayerSettings.iOS.targetOSVersionString = "14.0";
        PlayerSettings.iOS.cameraUsageDescription = "Required for AR";

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/GhostHustlers.unity" },
            locationPathName = "Builds/iOS",
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("iOS build failed: " + report.summary.totalErrors + " errors");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("iOS build succeeded: " + report.summary.outputPath);
        }
    }

    public static void BuildAndroid()
    {
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/GhostHustlers.unity" },
            locationPathName = "Builds/GhostHustlers.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("Android build failed: " + report.summary.totalErrors + " errors");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("Android build succeeded: " + report.summary.outputPath);
        }
    }
}
