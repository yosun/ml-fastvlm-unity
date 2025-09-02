using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace FastVLM.Unity.Examples
{
    /// <summary>
    /// Complete iOS example demonstrating FastVLM native integration
    /// </summary>
    public class FastVLMiOSExample : MonoBehaviour
    {
        [Header("FastVLM iOS Setup")]
        [SerializeField] private FastVLMiOS vlmClient;
        
        [Header("UI References")]
        [SerializeField] private RawImage previewImage;
        [SerializeField] private InputField promptInput;
        [SerializeField] private Text resultText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button initializeButton;
        [SerializeField] private Button captureButton;
        [SerializeField] private Button inferButton;
        [SerializeField] private Button photoLibraryButton;
        [SerializeField] private Slider temperatureSlider;
        [SerializeField] private Text temperatureLabel;
        
        [Header("Camera Setup")]
        [SerializeField] private Camera captureCamera;
        [SerializeField] private int captureWidth = 512;
        [SerializeField] private int captureHeight = 512;
        
        [Header("Demo Settings")]
        [SerializeField] private string[] demoPrompts = {
            "Describe what you see in this image.",
            "What is the main object in this scene?",
            "What colors are most prominent?",
            "Count the number of objects visible.",
            "Describe the lighting and atmosphere.",
            "What activities are happening in this image?",
            "Is this taken indoors or outdoors?",
            "What emotions or mood does this image convey?"
        };
        
        private Texture2D capturedTexture;
        private int currentPromptIndex = 0;
        private WebCamTexture webCamTexture;
        
        private void Start()
        {
            InitializeDemo();
            SetupEventListeners();
            SetupCamera();
        }
        
        private void InitializeDemo()
        {
            // Initialize UI
            if (promptInput != null)
            {
                promptInput.text = demoPrompts[0];
            }
            
            if (temperatureSlider != null)
            {
                temperatureSlider.value = 0.2f;
                UpdateTemperatureLabel();
            }
            
            if (statusText != null)
            {
                statusText.text = "Ready to initialize FastVLM...";
            }
            
            if (resultText != null)
            {
                resultText.text = "No inference performed yet.";
            }
            
            UpdateButtonStates();
        }
        
        private void SetupEventListeners()
        {
            // FastVLM events
            if (vlmClient != null)
            {
                vlmClient.OnInferenceComplete += OnInferenceComplete;
                vlmClient.OnInferenceError += OnInferenceError;
                vlmClient.OnInitializationStatusChanged += OnInitializationStatusChanged;
            }
            
            // UI events
            if (initializeButton != null)
            {
                initializeButton.onClick.AddListener(InitializeFastVLM);
            }
            
            if (captureButton != null)
            {
                captureButton.onClick.AddListener(CaptureImage);
            }
            
            if (inferButton != null)
            {
                inferButton.onClick.AddListener(PerformInference);
            }
            
            if (photoLibraryButton != null)
            {
                photoLibraryButton.onClick.AddListener(OpenPhotoLibrary);
            }
            
            if (temperatureSlider != null)
            {
                temperatureSlider.onValueChanged.AddListener(OnTemperatureChanged);
            }
        }
        
        private void SetupCamera()
        {
            if (captureCamera == null)
            {
                captureCamera = Camera.main;
            }
            
            capturedTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            
            // Initialize webcam for real-time preview
            if (WebCamTexture.devices.Length > 0)
            {
                webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, captureWidth, captureHeight);
                if (previewImage != null)
                {
                    previewImage.texture = webCamTexture;
                }
                webCamTexture.Play();
            }
        }
        
        public void InitializeFastVLM()
        {
            if (vlmClient == null)
            {
                if (statusText != null)
                {
                    statusText.text = "FastVLMiOS component not found!";
                    statusText.color = Color.red;
                }
                return;
            }

            if (vlmClient.IsInitialized)
            {
                if (statusText != null)
                {
                    statusText.text = "Already initialized.";
                    statusText.color = Color.green;
                }
                UpdateButtonStates();
                return;
            }

            if (statusText != null)
            {
                statusText.text = "Initializing FastVLM...";
                statusText.color = Color.yellow;
            }

            vlmClient.InitializeAsync();
            UpdateButtonStates();
        }
        
        public void CaptureImage()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                // Capture from webcam
                RenderTexture renderTexture = RenderTexture.GetTemporary(webCamTexture.width, webCamTexture.height);
                Graphics.Blit(webCamTexture, renderTexture);
                
                RenderTexture.active = renderTexture;
                capturedTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                capturedTexture.Apply();
                RenderTexture.active = null;
                
                RenderTexture.ReleaseTemporary(renderTexture);
            }
            else if (captureCamera != null)
            {
                // Capture from camera
                RenderTexture renderTexture = RenderTexture.GetTemporary(captureWidth, captureHeight, 24);
                captureCamera.targetTexture = renderTexture;
                captureCamera.Render();
                
                RenderTexture.active = renderTexture;
                capturedTexture.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
                capturedTexture.Apply();
                RenderTexture.active = null;
                
                captureCamera.targetTexture = null;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
            
            if (statusText != null)
            {
                statusText.text = "Image captured. Ready for inference.";
                statusText.color = Color.blue;
            }
            
            UpdateButtonStates();
        }
        
        public void PerformInference()
        {
            if (vlmClient == null || !vlmClient.IsInitialized || capturedTexture == null)
                return;

            string prompt = promptInput != null ? promptInput.text : demoPrompts[0];
            if (string.IsNullOrEmpty(prompt))
            {
                if (statusText != null)
                {
                    statusText.text = "Please enter a prompt!";
                    statusText.color = Color.red;
                }
                return;
            }

            if (statusText != null)
            {
                statusText.text = "Performing inference...";
                statusText.color = Color.yellow;
            }
            if (resultText != null) resultText.text = "Processing...";

            if (temperatureSlider != null)
            {
                vlmClient.SetGenerationParameters(temp: temperatureSlider.value, maxTok: vlmClient.maxTokens);
            }
            UpdateButtonStates();
            vlmClient.InferAsync(prompt, capturedTexture);
        }
        
        public void OpenPhotoLibrary()
        {
            // This would typically use a native iOS plugin for photo library access
            // For now, we'll simulate it
            if (statusText != null)
            {
                statusText.text = "Photo library access not implemented in this demo.";
                statusText.color = Color.gray;
            }
        }
        
        private void OnInferenceComplete(FastVLMResponse response)
        {
            if (resultText != null) resultText.text = response.result;
            if (statusText != null)
            {
                statusText.text = "Inference complete";
                statusText.color = Color.green;
            }
            UpdateButtonStates();
        }
        
        private void OnInferenceError(string error)
        {
            if (resultText != null) resultText.text = $"Error: {error}";
            if (statusText != null)
            {
                statusText.text = $"Inference failed: {error}";
                statusText.color = Color.red;
            }
            UpdateButtonStates();
        }
        
        private void OnInitializationStatusChanged(bool isInitialized)
        {
            if (statusText != null)
            {
                statusText.text = isInitialized ? "FastVLM ready!" : "FastVLM not initialized";
                statusText.color = isInitialized ? Color.green : Color.red;
            }
            UpdateButtonStates();
        }
        
        private void OnTemperatureChanged(float value)
        {
            UpdateTemperatureLabel();
        }
        
        private void UpdateTemperatureLabel()
        {
            if (temperatureLabel != null && temperatureSlider != null)
            {
                temperatureLabel.text = $"Temperature: {temperatureSlider.value:F2}";
            }
        }
        
        private void UpdateButtonStates()
        {
            bool isInitialized = vlmClient != null && vlmClient.IsInitialized;
            bool isInferenceInProgress = vlmClient != null && vlmClient.IsInferenceInProgress;
            bool hasImage = capturedTexture != null;

            if (initializeButton != null)
                initializeButton.interactable = !isInitialized && !isInferenceInProgress;
            if (captureButton != null)
                captureButton.interactable = isInitialized && !isInferenceInProgress;
            if (inferButton != null)
                inferButton.interactable = isInitialized && !isInferenceInProgress && hasImage;
            if (photoLibraryButton != null)
                photoLibraryButton.interactable = isInitialized && !isInferenceInProgress;
        }
        
        public void CyclePrompt()
        {
            if (promptInput != null && demoPrompts.Length > 0)
            {
                currentPromptIndex = (currentPromptIndex + 1) % demoPrompts.Length;
                promptInput.text = demoPrompts[currentPromptIndex];
            }
        }
        
        public void SetPrompt(string prompt)
        {
            if (promptInput != null)
            {
                promptInput.text = prompt;
            }
        }
        
        // Keyboard shortcuts for testing
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                CaptureImage();
            }
            
            if (Input.GetKeyDown(KeyCode.Return))
            {
                PerformInference();
            }
            
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                CyclePrompt();
            }
            
            if (Input.GetKeyDown(KeyCode.I))
            {
                InitializeFastVLM();
            }
        }
        
        private void OnDestroy()
        {
            // Clean up
            if (vlmClient != null)
            {
                vlmClient.OnInferenceComplete -= OnInferenceComplete;
                vlmClient.OnInferenceError -= OnInferenceError;
                vlmClient.OnInitializationStatusChanged -= OnInitializationStatusChanged;
            }
            
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
            }
        }
        
        // Public methods for UI buttons
        [System.Serializable]
        public class QuickPrompt
        {
            public string name;
            public string prompt;
        }
        
        [Header("Quick Prompts")]
        [SerializeField] private QuickPrompt[] quickPrompts = {
            new QuickPrompt { name = "Describe", prompt = "Describe what you see in this image." },
            new QuickPrompt { name = "Count", prompt = "Count the objects in this image." },
            new QuickPrompt { name = "Colors", prompt = "What colors are prominent in this image?" },
            new QuickPrompt { name = "Location", prompt = "Is this indoors or outdoors?" },
            new QuickPrompt { name = "Activity", prompt = "What activity is happening in this image?" }
        };
        
        public void UseQuickPrompt(int index)
        {
            if (index >= 0 && index < quickPrompts.Length && promptInput != null)
            {
                promptInput.text = quickPrompts[index].prompt;
            }
        }
    }
}
