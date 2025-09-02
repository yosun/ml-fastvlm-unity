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

            // First copy MLX & FastVLM frameworks if available so they can be added
            CopyMLXFrameworks(pathToBuiltProject);
                // Copy FastVLM main framework if available
                CopyFastVLMFramework(pathToBuiltProject);

                // Auto-copy MLX dependency frameworks if present in known locations
                AutoCopyMLXFrameworks(pathToBuiltProject);

            // Add FastVLM framework and dependencies (after copy so presence is detected)
            AddFrameworks(project, targetGuid, frameworkTargetGuid, pathToBuiltProject);
            
            // Configure build settings
            ConfigureBuildSettings(project, targetGuid, frameworkTargetGuid);
            
            // (FastVLM framework already attempted above; left here previously)
            
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
    private static void AddFrameworks(PBXProject project, string targetGuid, string frameworkTargetGuid, string pathToBuiltProject)
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
#if UNITY_IOS
        private static readonly string[] MLXFrameworkNames = {
            "MLX.framework",
            "MLXFast.framework",
            "MLXNN.framework",
            "MLXRandom.framework",
            "MLXVLM.framework",
            "MLXLMCommon.framework"
        };

        private static void AutoCopyMLXFrameworks(string buildPath)
        {
            string envDir = System.Environment.GetEnvironmentVariable("FASTVLM_MLX_FRAMEWORKS_DIR");
            string assetsPluginDir = Path.Combine(Application.dataPath, "Plugins/iOS/MLXFrameworks");
            string appFrameworksDir = Path.Combine(Application.dataPath, "../ml-fastvlm-unity/app/Frameworks");
            string repoFrameworksDir = Path.Combine(Application.dataPath, "../Frameworks");

            string[] searchRoots = new [] { envDir, assetsPluginDir, appFrameworksDir, repoFrameworksDir };
            string destFrameworksDir = Path.Combine(buildPath, "Frameworks");
            Directory.CreateDirectory(destFrameworksDir);

            int copied = 0; int expected = MLXFrameworkNames.Length; int available = 0;
            foreach (var root in searchRoots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                // Count availability in this root
                foreach (var fw in MLXFrameworkNames)
                {
                    string src = Path.Combine(root, fw);
                    if (Directory.Exists(src))
                    {
                        available++;
                        string dest = Path.Combine(destFrameworksDir, fw);
                        if (Directory.Exists(dest)) Directory.Delete(dest, true);
                        CopyDirectory(src, dest);
                        copied++;
                    }
                }
                if (copied == expected) break; // all done
            }

            if (copied > 0)
            {
                Debug.Log($"FastVLM: Copied {copied} MLX frameworks to Xcode project (of {expected}).");
            }
            else
            {
                Debug.LogWarning("FastVLM: No MLX frameworks found to copy. Using stub Swift layer. Set FASTVLM_MLX_FRAMEWORKS_DIR or place frameworks in Assets/Plugins/iOS/MLXFrameworks.");
            }
        }
#endif
        
        private static void CopyMLXFrameworks(string buildPath)
        {
            string[] expected = {
                "MLX.framework",
                "MLXFast.framework",
                "MLXNN.framework",
                "MLXRandom.framework",
                "MLXVLM.framework",
                "MLXLMCommon.framework"
            };

            // Candidate source directories (highest priority first)
            string envDir = System.Environment.GetEnvironmentVariable("FASTVLM_MLX_FRAMEWORKS_DIR");
            string[] candidateDirs = {
                string.IsNullOrEmpty(envDir) ? null : envDir,
                Path.Combine(Application.dataPath, "Plugins/iOS/MLXFrameworks"),
                Path.Combine(Application.dataPath, "../ml-fastvlm-unity/app/Frameworks"),
                Path.Combine(Application.dataPath, "../Frameworks")
            };

            string frameworksDest = Path.Combine(buildPath, "Frameworks");
            Directory.CreateDirectory(frameworksDest);

            int copied = 0;
            foreach (string framework in expected)
            {
                string sourcePath = null;
                foreach (string dir in candidateDirs)
                {
                    if (string.IsNullOrEmpty(dir)) continue;
                    string candidate = Path.Combine(dir, framework);
                    if (Directory.Exists(candidate)) { sourcePath = candidate; break; }
                }
                if (sourcePath != null)
                {
                    string destPath = Path.Combine(frameworksDest, framework);
                    if (Directory.Exists(destPath)) Directory.Delete(destPath, true);
                    CopyDirectory(sourcePath, destPath);
                    copied++;
                }
            }

            if (copied > 0)
            {
                Debug.Log($"FastVLM: Copied {copied} MLX frameworks to Xcode project (of {expected.Length}).");
            }
            else
            {
                Debug.Log("FastVLM: No MLX frameworks found to copy (will use stub Swift layer). Set FASTVLM_MLX_FRAMEWORKS_DIR env var to automate.");
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
