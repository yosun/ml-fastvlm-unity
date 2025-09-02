using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace FastVLM.Unity.Editor
{
    public class FastVLMiOSBuildProcessor
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget != BuildTarget.iOS)
                return;

#if UNITY_IOS
            string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            // Get the main target
            string targetGuid = project.GetUnityMainTargetGuid();
            string frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();

            // Add FastVLM framework and dependencies
            AddFrameworks(project, targetGuid, frameworkTargetGuid);
            
            // Configure build settings
            ConfigureBuildSettings(project, targetGuid, frameworkTargetGuid);
            
            // Copy FastVLM framework if available
            CopyFastVLMFramework(pathToBuiltProject);
            
            // Enable Swift support
            project.SetBuildProperty(targetGuid, "SWIFT_VERSION", "5.0");
            project.SetBuildProperty(frameworkTargetGuid, "SWIFT_VERSION", "5.0");
            project.SetBuildProperty(targetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
            
            // Add required capabilities
            string entitlementsPath = Path.Combine(pathToBuiltProject, "Unity-iPhone.entitlements");
            PlistDocument entitlements = new PlistDocument();
            
            if (File.Exists(entitlementsPath))
            {
                entitlements.ReadFromFile(entitlementsPath);
            }
            else
            {
                entitlements.Create();
            }
            
            // Add any required entitlements here
            entitlements.WriteToFile(entitlementsPath);

            // Write the modified project
            project.WriteToFile(projectPath);
            
                Debug.Log("FastVLM iOS build configuration completed. If Xcode reports 'Framework MLX not found', manually place MLX*.framework bundles into Xcode Frameworks/ then rebuild.");
#endif
        }

#if UNITY_IOS
        private static void AddFrameworks(PBXProject project, string targetGuid, string frameworkTargetGuid)
        {
            // Add required system frameworks
            project.AddFrameworkToProject(frameworkTargetGuid, "Foundation.framework", false);
            project.AddFrameworkToProject(frameworkTargetGuid, "UIKit.framework", false);
            project.AddFrameworkToProject(frameworkTargetGuid, "CoreImage.framework", false);
            project.AddFrameworkToProject(frameworkTargetGuid, "CoreML.framework", false);
            project.AddFrameworkToProject(frameworkTargetGuid, "Accelerate.framework", false);
            project.AddFrameworkToProject(frameworkTargetGuid, "Metal.framework", false);
            project.AddFrameworkToProject(frameworkTargetGuid, "MetalKit.framework", false);
            
                // Optionally add MLX frameworks if the developer has copied them into the build's Frameworks directory.
                string[] mlxFrameworks = {
                    "MLX.framework",
                    "MLXFast.framework",
                    "MLXNN.framework",
                    "MLXRandom.framework",
                    "MLXVLM.framework",
                    "MLXLMCommon.framework"
                };

                string frameworksDir = Path.Combine(pathToBuiltProject, "Frameworks");
                foreach (string framework in mlxFrameworks)
                {
                    string onDisk = Path.Combine(frameworksDir, framework);
                    if (Directory.Exists(onDisk))
                    {
                        string frameworkPath = "Frameworks/" + framework;
                        string fileGuid = project.AddFile(frameworkPath, frameworkPath, PBXSourceTree.Source);
                        project.AddFileToBuild(frameworkTargetGuid, fileGuid);
                        project.AddFrameworkToProject(frameworkTargetGuid, framework, false);
                    }
                    else
                    {
                        // Skip silently; user might rely on stub Swift layer.
                    }
                }
        }
        
        private static void ConfigureBuildSettings(PBXProject project, string targetGuid, string frameworkTargetGuid)
        {
            // Set minimum iOS version
            project.SetBuildProperty(targetGuid, "IPHONEOS_DEPLOYMENT_TARGET", "15.0");
            project.SetBuildProperty(frameworkTargetGuid, "IPHONEOS_DEPLOYMENT_TARGET", "15.0");
            
            // Enable Objective-C++ compilation
            project.SetBuildProperty(frameworkTargetGuid, "GCC_COMPILE_CPP_AS_OBJCPP", "YES");
            
            // Add header search paths
            project.AddBuildProperty(frameworkTargetGuid, "HEADER_SEARCH_PATHS", "$(inherited)");
            project.AddBuildProperty(frameworkTargetGuid, "HEADER_SEARCH_PATHS", "$(SRCROOT)/Libraries/Plugins/iOS");
            
            // Add library search paths
            project.AddBuildProperty(frameworkTargetGuid, "LIBRARY_SEARCH_PATHS", "$(inherited)");
            project.AddBuildProperty(frameworkTargetGuid, "LIBRARY_SEARCH_PATHS", "$(SRCROOT)/Frameworks");
            
            // Framework search paths
            project.AddBuildProperty(frameworkTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            project.AddBuildProperty(frameworkTargetGuid, "FRAMEWORK_SEARCH_PATHS", "$(SRCROOT)/Frameworks");
            
            // Other linker flags
            project.AddBuildProperty(frameworkTargetGuid, "OTHER_LDFLAGS", "-ObjC");
        }
        
        private static void CopyFastVLMFramework(string buildPath)
        {
            // Look for FastVLM framework in the project
            string[] possiblePaths = {
                Path.Combine(Application.dataPath, "Plugins/iOS/FastVLM.framework"),
                Path.Combine(Application.dataPath, "../ml-fastvlm-unity/app/FastVLM.framework")
            };
            
            string sourcePath = null;
            foreach (string path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    sourcePath = path;
                    break;
                }
            }
            
            if (sourcePath != null)
            {
                string destPath = Path.Combine(buildPath, "Frameworks/FastVLM.framework");
                if (Directory.Exists(destPath))
                {
                    Directory.Delete(destPath, true);
                }
                
                CopyDirectory(sourcePath, destPath);
                Debug.Log($"FastVLM framework copied from {sourcePath} to {destPath}");
            }
            else
            {
                Debug.LogWarning("FastVLM framework not found. Please ensure it's built and available in the project.");
            }
        }
        
        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);
            
            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourceDir, file);
                string destFile = Path.Combine(destinationDir, relativePath);
                string destDir = Path.GetDirectoryName(destFile);
                
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                
                File.Copy(file, destFile, true);
            }
        }
#endif
    }
}
