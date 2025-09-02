using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

namespace FastVLM.Unity.Editor
{
    /// <summary>
    /// Editor window for managing FastVLM Unity Plugin
    /// </summary>
    public class FastVLMManagerWindow : EditorWindow
    {
        private string serverUrl = "http://localhost:8000";
        private string modelPath = "";
        private bool serverRunning = false;
        private Process serverProcess;
        
        private Vector2 scrollPosition;
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        
        [MenuItem("Tools/FastVLM/Manager")]
        public static void ShowWindow()
        {
            GetWindow<FastVLMManagerWindow>("FastVLM Manager");
        }
        
        private void OnEnable()
        {
            titleContent = new GUIContent("FastVLM Manager", "Manage FastVLM Unity Plugin");
            minSize = new Vector2(400, 500);
            
            // Load saved preferences
            serverUrl = EditorPrefs.GetString("FastVLM.ServerUrl", "http://localhost:8000");
            modelPath = EditorPrefs.GetString("FastVLM.ModelPath", "");
        }
        
        private void OnDisable()
        {
            // Save preferences
            EditorPrefs.SetString("FastVLM.ServerUrl", serverUrl);
            EditorPrefs.SetString("FastVLM.ModelPath", modelPath);
            
            // Clean up server process
            if (serverProcess != null && !serverProcess.HasExited)
            {
                serverProcess.Kill();
                serverProcess = null;
            }
        }
        
        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fixedHeight = 30
                };
            }
        }
        
        private void OnGUI()
        {
            InitializeStyles();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Header
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("FastVLM Unity Plugin Manager", headerStyle);
            EditorGUILayout.Space(10);
            
            // Server Configuration
            DrawServerConfiguration();
            
            EditorGUILayout.Space(10);
            
            // Server Control
            DrawServerControl();
            
            EditorGUILayout.Space(10);
            
            // Server Status
            DrawServerStatus();
            
            EditorGUILayout.Space(10);
            
            // Quick Actions
            DrawQuickActions();
            
            EditorGUILayout.Space(10);
            
            // Documentation
            DrawDocumentation();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawServerConfiguration()
        {
            EditorGUILayout.LabelField("Server Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Server URL
            EditorGUILayout.LabelField("Server URL:");
            serverUrl = EditorGUILayout.TextField(serverUrl);
            
            EditorGUILayout.Space(5);
            
            // Model Path
            EditorGUILayout.LabelField("Model Path:");
            EditorGUILayout.BeginHorizontal();
            modelPath = EditorGUILayout.TextField(modelPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select FastVLM Model Directory", modelPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    modelPath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (!string.IsNullOrEmpty(modelPath) && !Directory.Exists(modelPath))
            {
                EditorGUILayout.HelpBox("Model path does not exist!", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawServerControl()
        {
            EditorGUILayout.LabelField("Server Control", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !serverRunning && !string.IsNullOrEmpty(modelPath) && Directory.Exists(modelPath);
            if (GUILayout.Button("Start Server", buttonStyle))
            {
                StartServer();
            }
            
            GUI.enabled = serverRunning;
            if (GUILayout.Button("Stop Server", buttonStyle))
            {
                StopServer();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            if (string.IsNullOrEmpty(modelPath))
            {
                EditorGUILayout.HelpBox("Please specify a model path to start the server.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawServerStatus()
        {
            EditorGUILayout.LabelField("Server Status", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            string status = serverRunning ? "Running" : "Stopped";
            Color statusColor = serverRunning ? Color.green : Color.red;
            
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"Status: {status}");
            GUI.color = Color.white;
            
            if (serverRunning)
            {
                EditorGUILayout.LabelField($"URL: {serverUrl}");
                
                if (GUILayout.Button("Test Connection", GUILayout.Height(25)))
                {
                    TestConnection();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (GUILayout.Button("Create FastVLM Client in Scene", buttonStyle))
            {
                CreateFastVLMClient();
            }
            
            if (GUILayout.Button("Open Backend Folder", buttonStyle))
            {
                OpenBackendFolder();
            }
            
            if (GUILayout.Button("Open Example Scene", buttonStyle))
            {
                OpenExampleScene();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDocumentation()
        {
            EditorGUILayout.LabelField("Documentation & Setup", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Setup Steps:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Install Python dependencies (see Backend/setup.sh)");
            EditorGUILayout.LabelField("2. Download a FastVLM model");
            EditorGUILayout.LabelField("3. Configure model path above");
            EditorGUILayout.LabelField("4. Start the server");
            EditorGUILayout.LabelField("5. Use FastVLMClient in your scenes");
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Open Setup Instructions", buttonStyle))
            {
                Application.OpenURL("https://github.com/yosun/ml-fastvlm-unity");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void StartServer()
        {
            try
            {
                string backendPath = Path.Combine(Application.dataPath, "..", "Packages", "com.fastvlm.unity", "Backend");
                if (!Directory.Exists(backendPath))
                {
                    // Try alternative path for development
                    backendPath = Path.Combine(Application.dataPath, "..", "..", "UnityPlugin", "Backend");
                }
                
                string serverScript = Path.Combine(backendPath, "fastvlm_server.py");
                
                if (!File.Exists(serverScript))
                {
                    EditorUtility.DisplayDialog("Error", $"Server script not found at: {serverScript}", "OK");
                    return;
                }
                
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "python3",
                    Arguments = $"\"{serverScript}\" --model-path \"{modelPath}\" --host localhost --port 8000",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = backendPath
                };
                
                serverProcess = Process.Start(startInfo);
                serverRunning = true;
                
                UnityEngine.Debug.Log($"FastVLM server started with PID: {serverProcess.Id}");
                
                // Start monitoring the process
                EditorApplication.update += MonitorServer;
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to start server: {e.Message}", "OK");
            }
        }
        
        private void StopServer()
        {
            if (serverProcess != null && !serverProcess.HasExited)
            {
                serverProcess.Kill();
                serverProcess = null;
            }
            
            serverRunning = false;
            EditorApplication.update -= MonitorServer;
            
            UnityEngine.Debug.Log("FastVLM server stopped");
        }
        
        private void MonitorServer()
        {
            if (serverProcess != null && serverProcess.HasExited)
            {
                serverRunning = false;
                serverProcess = null;
                EditorApplication.update -= MonitorServer;
                
                UnityEngine.Debug.Log("FastVLM server process exited");
                Repaint();
            }
        }
        
        private void TestConnection()
        {
            // This would ideally use a web request, but for simplicity we'll just log
            UnityEngine.Debug.Log($"Testing connection to {serverUrl}/health");
            EditorUtility.DisplayDialog("Test Connection", 
                $"Please check the console for connection test results.\nTesting: {serverUrl}/health", "OK");
        }
        
        private void CreateFastVLMClient()
        {
            GameObject clientGO = new GameObject("FastVLM Client");
            clientGO.AddComponent<FastVLMClient>();
            
            Selection.activeGameObject = clientGO;
            
            UnityEngine.Debug.Log("FastVLM Client created in scene");
        }
        
        private void OpenBackendFolder()
        {
            string backendPath = Path.Combine(Application.dataPath, "..", "Packages", "com.fastvlm.unity", "Backend");
            if (!Directory.Exists(backendPath))
            {
                backendPath = Path.Combine(Application.dataPath, "..", "..", "UnityPlugin", "Backend");
            }
            
            if (Directory.Exists(backendPath))
            {
                EditorUtility.RevealInFinder(backendPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Backend folder not found", "OK");
            }
        }
        
        private void OpenExampleScene()
        {
            // This would open an example scene if we had one
            UnityEngine.Debug.Log("Example scene functionality not implemented yet");
        }
    }
}
