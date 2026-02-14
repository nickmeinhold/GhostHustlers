using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
using UnityEngine.XR.ARFoundation;

public class ProjectConfigurator
{
    public static void ConfigureProject()
    {
        ConfigurePlayerSettings();
        ConfigureScriptingDefines();
        ConfigureURP();
        ConfigureXR();
        AssetDatabase.SaveAssets();
        Debug.Log("Project configuration complete.");
    }

    static void ConfigurePlayerSettings()
    {
        // Linear color space required for URP
        if (PlayerSettings.colorSpace != ColorSpace.Linear)
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Debug.Log("Set color space to Linear.");
        }

        // Disable multithreaded rendering on iOS — known to cause black screen
        // and crashes with AR Foundation (ARKit XR Plugin changelog, multiple
        // Unity Issue Tracker bugs, community reports on discussions.unity.com)
        PlayerSettings.SetMobileMTRendering(BuildTargetGroup.iOS, false);
        Debug.Log("Disabled multithreaded rendering for iOS.");

        // Disable multithreaded rendering on Android too for safety
        PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, false);
        Debug.Log("Disabled multithreaded rendering for Android.");
    }

    static void ConfigureScriptingDefines()
    {
        // UNITY_XR_ARKIT_LOADER_ENABLED must be set BEFORE scripts compile.
        // In batch mode, ARKitBuildProcessor.LoaderEnabledCheck never calls
        // UpdateARKitDefines(), so this define never gets added. Without it,
        // all ARKit C# compiles with stub implementations (AtLeast11_0() => false,
        // no DllImports) and no subsystem descriptors are registered at runtime.
        var iOS = UnityEditor.Build.NamedBuildTarget.iOS;
        string[] defines;
        PlayerSettings.GetScriptingDefineSymbols(iOS, out defines);
        bool found = false;
        foreach (var d in defines)
        {
            if (d == "UNITY_XR_ARKIT_LOADER_ENABLED") { found = true; break; }
        }
        if (!found)
        {
            var list = new System.Collections.Generic.List<string>(defines);
            list.Add("UNITY_XR_ARKIT_LOADER_ENABLED");
            PlayerSettings.SetScriptingDefineSymbols(iOS, list.ToArray());
            Debug.Log("Added UNITY_XR_ARKIT_LOADER_ENABLED scripting define for iOS.");
        }
    }

    static void ConfigureURP()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");

        string rendererPath = "Assets/Settings/URPRenderer.asset";
        string pipelinePath = "Assets/Settings/URPAsset.asset";

        // Load existing renderer or create new one
        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (rendererData == null)
        {
            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, rendererPath);
        }

        // Assign Post Process Data
        if (rendererData.postProcessData == null)
        {
            var ppGuids = AssetDatabase.FindAssets("t:PostProcessData");
            if (ppGuids.Length > 0)
            {
                var ppData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                    AssetDatabase.GUIDToAssetPath(ppGuids[0]));
                rendererData.postProcessData = ppData;
                Debug.Log("Assigned PostProcessData: " + AssetDatabase.GUIDToAssetPath(ppGuids[0]));
            }
        }

        // Add ARBackgroundRendererFeature (required for AR camera feed in URP)
        // Per: docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/project-setup/universal-render-pipeline.html
        bool hasARBackground = false;
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature is ARBackgroundRendererFeature)
            {
                hasARBackground = true;
                break;
            }
        }

        if (!hasARBackground)
        {
            var arFeature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
            arFeature.name = "AR Background Renderer Feature";
            AssetDatabase.AddObjectToAsset(arFeature, rendererData);
            rendererData.rendererFeatures.Add(arFeature);
            rendererData.SetDirty();
            Debug.Log("Added ARBackgroundRendererFeature to renderer.");
        }

        EditorUtility.SetDirty(rendererData);

        // Load existing pipeline or create new one
        var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (pipelineAsset == null)
        {
            pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
            pipelineAsset.name = "URPAsset";
            AssetDatabase.CreateAsset(pipelineAsset, pipelinePath);
        }

        // AR-safe URP pipeline settings (per community best practices and Unity Issue Tracker)
        pipelineAsset.supportsHDR = false;
        pipelineAsset.msaaSampleCount = 1;  // Disable MSAA for AR
        pipelineAsset.renderScale = 1.0f;   // Must be 1.0 for AR on iOS

        // Disable depth/opaque texture — known to cause black screen on iOS AR
        // (issuetracker.unity3d.com: "Enabling Depth Texture causes Camera to render black")
        var so = new SerializedObject(pipelineAsset);
        so.FindProperty("m_RequireDepthTexture").boolValue = false;
        so.FindProperty("m_RequireOpaqueTexture").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(pipelineAsset);

        // Assign as the default render pipeline
        GraphicsSettings.defaultRenderPipeline = pipelineAsset;

        // Assign to ALL quality levels so no level overrides with a different asset
        int currentLevel = QualitySettings.GetQualityLevel();
        string[] qualityNames = QualitySettings.names;
        for (int i = 0; i < qualityNames.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipelineAsset;
        }
        QualitySettings.SetQualityLevel(currentLevel, false);

        Debug.Log("URP pipeline configured and assigned to all " + qualityNames.Length + " quality levels.");
    }

    static void ConfigureXR()
    {
        // Load or find the per-build-target settings asset
        string[] guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
        XRGeneralSettingsPerBuildTarget buildTargetSettings = null;

        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            buildTargetSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
        }

        if (buildTargetSettings == null)
        {
            buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(buildTargetSettings, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
        }

        // iOS: enable ARKit
        SetupPlatformXR(buildTargetSettings, BuildTargetGroup.iOS,
            "UnityEngine.XR.ARKit.ARKitLoader", "Unity.XR.ARKit");

        // Android: enable ARCore
        SetupPlatformXR(buildTargetSettings, BuildTargetGroup.Android,
            "UnityEngine.XR.ARCore.ARCoreLoader", "Unity.XR.ARCore");

        EditorUtility.SetDirty(buildTargetSettings);
        AssetDatabase.SaveAssets();
    }

    static void SetupPlatformXR(XRGeneralSettingsPerBuildTarget buildTargetSettings,
        BuildTargetGroup targetGroup, string loaderTypeName, string assemblyName)
    {
        // Create default settings if none exist
        if (!buildTargetSettings.HasSettingsForBuildTarget(targetGroup))
            buildTargetSettings.CreateDefaultSettingsForBuildTarget(targetGroup);

        if (!buildTargetSettings.HasManagerSettingsForBuildTarget(targetGroup))
            buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(targetGroup);

        var manager = buildTargetSettings.ManagerSettingsForBuildTarget(targetGroup);
        if (manager == null)
        {
            Debug.LogError("Failed to create XR manager for " + targetGroup);
            return;
        }

        // Persist manager asset if needed
        if (!AssetDatabase.Contains(manager))
        {
            string managerPath = "Assets/XR/XRManagerSettings_" + targetGroup + ".asset";
            AssetDatabase.CreateAsset(manager, managerPath);
        }

        // Find the loader type
        System.Type loaderType = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.GetName().Name == assemblyName)
            {
                loaderType = asm.GetType(loaderTypeName);
                if (loaderType != null) break;
            }
        }

        if (loaderType == null)
        {
            Debug.LogWarning("Could not find loader type: " + loaderTypeName);
            return;
        }

        // Check if already added
        foreach (var existing in manager.activeLoaders)
        {
            if (existing != null && existing.GetType() == loaderType)
            {
                Debug.Log("XR loader already enabled: " + loaderTypeName);
                return;
            }
        }

        // Create and add the loader
        var loader = ScriptableObject.CreateInstance(loaderType) as XRLoader;
        if (loader != null)
        {
            string loaderPath = "Assets/XR/" + loaderType.Name + "_" + targetGroup + ".asset";
            AssetDatabase.CreateAsset(loader, loaderPath);
            manager.TryAddLoader(loader);
            EditorUtility.SetDirty(manager);
            Debug.Log("Enabled XR loader: " + loaderTypeName + " for " + targetGroup);
        }
    }
}
