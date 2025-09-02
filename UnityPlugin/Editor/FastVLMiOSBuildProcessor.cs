using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Net;
using System.IO.Compression;

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

            // Ensure MLX frameworks downloaded/placed if user supplied ZIP URL
            TryFetchMLXFrameworks();

            // Copy FastVLM framework if present and auto-copy MLX dependency frameworks
            CopyFastVLMFramework(pathToBuiltProject);
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
            project.SetBuildProperty(frameworkTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

            // Ensure runpath search paths include Frameworks dir (both targets)
            project.AddBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks @loader_path/Frameworks");
            project.AddBuildProperty(frameworkTargetGuid, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks @loader_path/Frameworks");
            
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
            
            Debug.Log("FastVLM iOS build configuration completed. If Xcode still reports 'Framework MLX not found', confirm MLX*.framework exist inside Xcode Frameworks/ and have correct architectures (device + simulator if using xcframework).");
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

            int copied = 0; int expected = MLXFrameworkNames.Length; var missing = new System.Collections.Generic.List<string>(MLXFrameworkNames);
            foreach (var root in searchRoots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                // Count availability in this root
                foreach (var fw in MLXFrameworkNames)
                {
                    string src = Path.Combine(root, fw);
                    if (Directory.Exists(src))
                    {
                        string dest = Path.Combine(destFrameworksDir, fw);
                        if (Directory.Exists(dest)) Directory.Delete(dest, true);
                        CopyDirectory(src, dest);
                        copied++;
                        missing.Remove(fw);
                    }
                }
                if (copied == expected) break; // all done
            }

            if (copied > 0)
            {
                if (missing.Count == 0)
                    Debug.Log($"FastVLM: Copied all {expected} MLX frameworks.");
                else
                    Debug.LogWarning($"FastVLM: Copied {copied}/{expected} MLX frameworks. Missing: {string.Join(", ", missing)} (will attempt build; stubs used for missing).");
            }
            else
            {
                Debug.LogWarning("FastVLM: No MLX frameworks found to copy. Using stub Swift layer. Set FASTVLM_MLX_FRAMEWORKS_DIR or place frameworks in Assets/Plugins/iOS/MLXFrameworks.");
            }
        }
#endif

#if UNITY_IOS
        private static void TryFetchMLXFrameworks()
        {
            try
            {
                // If all frameworks already present in any search dir, skip
                string assetsPluginDir = Path.Combine(Application.dataPath, "Plugins/iOS/MLXFrameworks");
                if (HasAllFrameworks(assetsPluginDir)) return;

                string zipUrl = System.Environment.GetEnvironmentVariable("FASTVLM_MLX_FRAMEWORKS_ZIP_URL");
                if (string.IsNullOrEmpty(zipUrl)) return; // nothing to do

                Directory.CreateDirectory(assetsPluginDir);
                string tempDir = Path.Combine(Path.GetTempPath(), "fastvlm_mlx_fw");
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);
                string zipPath = Path.Combine(tempDir, "mlx_frameworks.zip");

                using (var wc = new WebClient())
                {
                    Debug.Log($"FastVLM: Downloading MLX frameworks archive from {zipUrl} ...");
                    wc.DownloadFile(zipUrl, zipPath);
                }

                ZipFile.ExtractToDirectory(zipPath, tempDir);

                // Move any found *.framework folders into assetsPluginDir
                int moved = 0;
                foreach (var fwName in MLXFrameworkNames)
                {
                    var found = Directory.GetDirectories(tempDir, fwName, SearchOption.AllDirectories);
                    foreach (var src in found)
                    {
                        string dest = Path.Combine(assetsPluginDir, fwName);
                        if (Directory.Exists(dest)) Directory.Delete(dest, true);
                        CopyDirectory(src, dest);
                        moved++;
                    }
                }

                if (moved > 0)
                {
                    Debug.Log($"FastVLM: Placed {moved} MLX frameworks from downloaded archive into {assetsPluginDir}.");
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogWarning("FastVLM: Download succeeded but no MLX *.framework bundles found inside archive.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"FastVLM: Failed to fetch MLX frameworks automatically: {ex.Message}");
            }
        }

        private static bool HasAllFrameworks(string rootDir)
        {
            if (string.IsNullOrEmpty(rootDir) || !Directory.Exists(rootDir)) return false;
            foreach (var fw in MLXFrameworkNames)
            {
                if (!Directory.Exists(Path.Combine(rootDir, fw))) return false;
            }
            return true;
        }
#endif


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
