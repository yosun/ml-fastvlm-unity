using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace FastVLM.Unity.Examples
{
    /// <summary>
    /// Complete FastVLM demo with camera capture and real-time inference
    /// </summary>
    public class FastVLMCompleteDemo : MonoBehaviour
    {
        [Header("FastVLM Setup")]
        [SerializeField] private FastVLMClient vlmClient;
        
        [Header("Camera Setup")]
        [SerializeField] private Camera captureCamera;
        [SerializeField] private int captureWidth = 512;
        [SerializeField] private int captureHeight = 512;
        
        [Header("UI References")]
        [SerializeField] private RawImage previewImage;
        [SerializeField] private InputField promptInput;
        [SerializeField] private Text resultText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button captureButton;
        [SerializeField] private Button inferButton;
        [SerializeField] private Toggle autoInferToggle;
        [SerializeField] private Slider temperatureSlider;
        [SerializeField] private Text temperatureLabel;
        
        [Header("Demo Settings")]
        [SerializeField] private float autoInferInterval = 3f;
        [SerializeField] private string[] demoPrompts = {
            "Describe what you see in this image.",
            "What is the main object in this scene?",
            "What colors are most prominent?",
            "Count the number of objects visible.",
            "Describe the lighting and atmosphere."
        };
        
        private RenderTexture renderTexture;
        private Texture2D capturedTexture;
        private bool isInferenceInProgress = false;
        private Coroutine autoInferCoroutine;
        private int currentPromptIndex = 0;
        
        private void Start()
        {
            InitializeDemo();
            SetupEventListeners();
            SetupCamera();
            CheckServerConnection();
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
                statusText.text = "Initializing...";
            }
            
            if (resultText != null)
            {
                resultText.text = "No inference performed yet.";
            }
        }
        
        private void SetupEventListeners()
        {
            // FastVLM events
            if (vlmClient != null)
            {
                vlmClient.OnInferenceComplete += OnInferenceComplete;
                vlmClient.OnInferenceError += OnInferenceError;
            }
            
            // UI events
            if (captureButton != null)
            {
                captureButton.onClick.AddListener(CaptureImage);
            }
            
            if (inferButton != null)
            {
                inferButton.onClick.AddListener(PerformInference);
            }
            
            if (autoInferToggle != null)
            {
                autoInferToggle.onValueChanged.AddListener(OnAutoInferToggle);
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
            
            if (captureCamera != null)
            {
                // Create render texture for camera capture
                renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
                capturedTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
                
                // Capture initial image
                CaptureImage();
            }
        }
        
        private void CheckServerConnection()
        {
            if (vlmClient != null)
            {
                vlmClient.CheckServerHealth(OnHealthCheckComplete);
            }
        }
        
        private void OnHealthCheckComplete(bool isHealthy)
        {
            if (statusText != null)
            {
                statusText.text = isHealthy ? "Server: Connected ✓" : "Server: Disconnected ✗";
                statusText.color = isHealthy ? Color.green : Color.red;
            }
            
            // Enable/disable buttons based on server health
            if (captureButton != null) captureButton.interactable = isHealthy;
            if (inferButton != null) inferButton.interactable = isHealthy && capturedTexture != null;
        }
        
        public void CaptureImage()
        {
            if (captureCamera == null || renderTexture == null) return;
            
            // Set camera to render to our texture
            RenderTexture originalTarget = captureCamera.targetTexture;
            captureCamera.targetTexture = renderTexture;
            
            // Render
            captureCamera.Render();
            
            // Read pixels
            RenderTexture.active = renderTexture;
            capturedTexture.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
            capturedTexture.Apply();
            RenderTexture.active = null;
            
            // Restore camera
            captureCamera.targetTexture = originalTarget;
            
            // Update preview
            if (previewImage != null)
            {
                previewImage.texture = capturedTexture;
            }
            
            // Enable infer button
            if (inferButton != null)
            {
                inferButton.interactable = !isInferenceInProgress && vlmClient != null;
            }
            
            if (statusText != null && !isInferenceInProgress)
            {
                statusText.text = "Image captured. Ready for inference.";
                statusText.color = Color.blue;
            }
        }
        
        public void PerformInference()
        {
            if (isInferenceInProgress || vlmClient == null || capturedTexture == null)
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
            
            // Update UI for inference start
            isInferenceInProgress = true;
            
            if (statusText != null)
            {
                statusText.text = "Performing inference...";
                statusText.color = Color.yellow;
            }
            
            if (inferButton != null)
            {
                inferButton.interactable = false;
            }
            
            if (resultText != null)
            {
                resultText.text = "Processing...";
            }
            
            // Update generation parameters
            if (temperatureSlider != null)
            {
                vlmClient.SetGenerationParameters(temp: temperatureSlider.value);
            }
            
            // Perform inference
            vlmClient.InferAsync(prompt, capturedTexture);
        }
        
        private void OnInferenceComplete(FastVLMResponse response)
        {
            isInferenceInProgress = false;
            
            if (resultText != null)
            {
                resultText.text = response.result;
            }
            
            if (statusText != null)
            {
                statusText.text = $"Inference completed in {response.inference_time:F2}s";
                statusText.color = Color.green;
            }
            
            if (inferButton != null)
            {
                inferButton.interactable = true;
            }
        }
        
        private void OnInferenceError(string error)
        {
            isInferenceInProgress = false;
            
            if (resultText != null)
            {
                resultText.text = $"Error: {error}";
            }
            
            if (statusText != null)
            {
                statusText.text = $"Inference failed: {error}";
                statusText.color = Color.red;
            }
            
            if (inferButton != null)
            {
                inferButton.interactable = true;
            }
        }
        
        private void OnAutoInferToggle(bool enabled)
        {
            if (enabled)
            {
                autoInferCoroutine = StartCoroutine(AutoInferLoop());
            }
            else
            {
                if (autoInferCoroutine != null)
                {
                    StopCoroutine(autoInferCoroutine);
                    autoInferCoroutine = null;
                }
            }
        }
        
        private IEnumerator AutoInferLoop()
        {
            while (autoInferToggle != null && autoInferToggle.isOn)
            {
                if (!isInferenceInProgress)
                {
                    // Capture new image
                    CaptureImage();
                    yield return new WaitForSeconds(0.5f);
                    
                    // Cycle through demo prompts
                    if (promptInput != null)
                    {
                        promptInput.text = demoPrompts[currentPromptIndex];
                        currentPromptIndex = (currentPromptIndex + 1) % demoPrompts.Length;
                    }
                    
                    // Perform inference
                    PerformInference();
                }
                
                yield return new WaitForSeconds(autoInferInterval);
            }
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
        
        private void OnDestroy()
        {
            // Clean up
            if (vlmClient != null)
            {
                vlmClient.OnInferenceComplete -= OnInferenceComplete;
                vlmClient.OnInferenceError -= OnInferenceError;
            }
            
            if (autoInferCoroutine != null)
            {
                StopCoroutine(autoInferCoroutine);
            }
            
            if (renderTexture != null)
            {
                renderTexture.Release();
            }
        }
        
        // Keyboard shortcuts for testing
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && !isInferenceInProgress)
            {
                CaptureImage();
            }
            
            if (Input.GetKeyDown(KeyCode.Return) && !isInferenceInProgress)
            {
                PerformInference();
            }
            
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                CyclePrompt();
            }
        }
    }
}
