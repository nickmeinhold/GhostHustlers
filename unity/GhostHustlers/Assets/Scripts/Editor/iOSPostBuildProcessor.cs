#if UNITY_IOS
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using System.IO;

// Runs after Unity generates the Xcode project but before we build it externally.
// Fixes Swift compatibility: libUnityARKit.a contains Swift code
// (RoomCaptureSessionWrapper.o) which requires Swift standard libraries to link.
// We add a dummy .swift file to the UnityFramework target so Xcode invokes swiftc,
// which links the Swift runtime and resolves swiftCompatibility* symbols.
public class iOSPostBuildProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 99; // Run after other post-processors

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS)
            return;

        string outputPath = report.summary.outputPath;
        string projectPath = PBXProject.GetPBXProjectPath(outputPath);
        var project = new PBXProject();
        project.ReadFromString(File.ReadAllText(projectPath));

        string frameworkGuid = project.GetUnityFrameworkTargetGuid();

        // Set Swift version on UnityFramework target
        project.SetBuildProperty(frameworkGuid, "SWIFT_VERSION", "5.0");
        project.SetBuildProperty(frameworkGuid, "CLANG_ENABLE_MODULES", "YES");

        // Add a dummy Swift file to force Xcode to link the Swift runtime.
        // No bridging header — frameworks use the umbrella header instead.
        string swiftFilePath = Path.Combine(outputPath, "Classes", "SwiftBridge.swift");
        File.WriteAllText(swiftFilePath,
            "import Foundation\n" +
            "// This file exists solely to make Xcode invoke swiftc for this target,\n" +
            "// which links the Swift runtime needed by libUnityARKit.a.\n" +
            "@objc public class SwiftBridge: NSObject {}\n");
        string fileGuid = project.AddFile(
            "Classes/SwiftBridge.swift", "Classes/SwiftBridge.swift",
            PBXSourceTree.Source);
        project.AddFileToBuild(frameworkGuid, fileGuid);

        // Do NOT use bridging headers with framework targets — Xcode forbids it.
        // Remove it if somehow set.
        project.SetBuildProperty(frameworkGuid, "SWIFT_OBJC_BRIDGING_HEADER", "");

        // Disable library evolution — avoids "module interfaces unsupported" errors
        project.SetBuildProperty(frameworkGuid, "BUILD_LIBRARY_FOR_DISTRIBUTION", "NO");

        File.WriteAllText(projectPath, project.WriteToString());
        UnityEngine.Debug.Log("iOS post-build: Added Swift support to UnityFramework target.");
    }
}
#endif
