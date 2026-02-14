using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.Reflection;

public class BuildScript
{
    public static void BuildiOS()
    {
        PlayerSettings.iOS.targetOSVersionString = "14.0";
        PlayerSettings.iOS.cameraUsageDescription = "Required for AR";

        // CRITICAL: In batch mode, ARKitBuildProcessor.LoaderEnabledCheck's static
        // constructor exits early without calling UpdateARKitDefines(). This causes
        // two problems:
        // 1. loaderEnabled stays false → native plugins (libUnityARKit.a) excluded
        // 2. UNITY_XR_ARKIT_LOADER_ENABLED define never added → all ARKit C# code
        //    compiles with stubs (AtLeast11_0() => false, no DllImports, etc.)
        ForceARKitLoaderEnabled();
        EnsureARKitScriptingDefine();

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

    static void EnsureARKitScriptingDefine()
    {
        // The UNITY_XR_ARKIT_LOADER_ENABLED scripting define controls whether ARKit
        // C# code compiles with real DllImport native calls or dead stubs.
        // In batch mode it's never added. Without it, AtLeast11_0() => false,
        // RegisterDescriptor() exits early, and no subsystems are created.
        var target = UnityEditor.Build.NamedBuildTarget.iOS;
        string[] defines;
        PlayerSettings.GetScriptingDefineSymbols(target, out defines);
        bool found = false;
        foreach (var d in defines)
        {
            if (d == "UNITY_XR_ARKIT_LOADER_ENABLED") { found = true; break; }
        }
        if (!found)
        {
            var list = new System.Collections.Generic.List<string>(defines);
            list.Add("UNITY_XR_ARKIT_LOADER_ENABLED");
            PlayerSettings.SetScriptingDefineSymbols(target, list.ToArray());
            Debug.Log("Added UNITY_XR_ARKIT_LOADER_ENABLED scripting define for iOS");
        }
    }

    static void ForceARKitLoaderEnabled()
    {
        // ARKitBuildProcessor is internal, so we use reflection
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = asm.GetType("UnityEditor.XR.ARKit.ARKitBuildProcessor");
            if (type != null)
            {
                var field = type.GetField("loaderEnabled",
                    BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                {
                    field.SetValue(null, true);
                    Debug.Log("Forced ARKitBuildProcessor.loaderEnabled = true (batch mode workaround)");
                    return;
                }
            }
        }
        Debug.LogWarning("Could not find ARKitBuildProcessor.loaderEnabled field");
    }
}
