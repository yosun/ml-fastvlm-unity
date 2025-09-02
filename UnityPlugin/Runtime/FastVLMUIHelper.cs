using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace FastVLM.Unity
{
    /// <summary>
    /// Helper component that provides a simple UI for FastVLM inference
    /// Attach this to a Canvas for quick testing and prototyping
    /// </summary>
    public class FastVLMUIHelper : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button loadModelButton;
        [SerializeField] private Button runInferenceButton;
        [SerializeField] private InputField promptInputField;
        [SerializeField] private RawImage imageDisplay;
        [SerializeField] private Text statusText;
        [SerializeField] private Text resultText;
        [SerializeField] private Slider progressSlider;
        
        [Header("Settings")]
        [SerializeField] private Texture2D testImage;
        [SerializeField] private string defaultPrompt = "What do you see in this image?";
        
        private FastVLMInference fastVLM;
        private bool isUIInitialized = false;

        private void Start()
        {
            InitializeUI();
            FindOrCreateFastVLM();
            SetupEventHandlers();
        }

        private void InitializeUI()
        {
            // Auto-create UI if not assigned
            if (loadModelButton == null || runInferenceButton == null)
            {
                CreateSimpleUI();
            }
            
            // Set initial states
            if (runInferenceButton != null)
                runInferenceButton.interactable = false;
                
            if (progressSlider != null)
                progressSlider.value = 0f;
                
            if (promptInputField != null && string.IsNullOrEmpty(promptInputField.text))
                promptInputField.text = defaultPrompt;
                
            if (imageDisplay != null && testImage != null)
                imageDisplay.texture = testImage;
                
            UpdateStatus("Ready to load model");
            isUIInitialized = true;
        }

        private void CreateSimpleUI()
        {
            // Create a simple UI programmatically if components aren't assigned
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                canvas = new GameObject("Canvas");
                canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.AddComponent<CanvasScaler>();
                canvas.AddComponent<GraphicRaycaster>();
            }

            // Create status text
            if (statusText == null)
            {
                GameObject statusGO = new GameObject("StatusText");
                statusGO.transform.SetParent(canvas.transform);
                statusText = statusGO.AddComponent<Text>();
                statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                statusText.fontSize = 16;
                statusText.color = Color.black;
                statusText.text = "FastVLM Status";
                
                RectTransform rect = statusText.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.offsetMin = new Vector2(10, -50);
                rect.offsetMax = new Vector2(-10, -10);
            }
        }

        private void FindOrCreateFastVLM()
        {
            // Find existing FastVLM instance
            fastVLM = FindObjectOfType<FastVLMInference>();
            
            if (fastVLM == null)
            {
                // Create new FastVLM instance
                GameObject fastVLMGO = new GameObject("FastVLM");
                fastVLM = fastVLMGO.AddComponent<FastVLMInference>();
                UpdateStatus("FastVLM component created");
            }
            else
            {
                UpdateStatus("FastVLM component found");
            }
        }

        private void SetupEventHandlers()
        {
            if (fastVLM == null) return;

            // Button events
            if (loadModelButton != null)
                loadModelButton.onClick.AddListener(OnLoadModelClicked);
                
            if (runInferenceButton != null)
                runInferenceButton.onClick.AddListener(OnRunInferenceClicked);

            // FastVLM events
            fastVLM.OnLoadProgress.AddListener(OnLoadProgress);
            fastVLM.OnModelLoaded.AddListener(OnModelLoaded);
            fastVLM.OnInferenceComplete.AddListener(OnInferenceComplete);
            fastVLM.OnInferenceError.AddListener(OnInferenceError);
        }

        // Button Event Handlers
        private void OnLoadModelClicked()
        {
            if (fastVLM != null && !fastVLM.IsModelLoaded)
            {
                UpdateStatus("Loading model...");
                if (loadModelButton != null)
                    loadModelButton.interactable = false;
                    
                fastVLM.LoadModel();
            }
        }

        private void OnRunInferenceClicked()
        {
            if (fastVLM == null || !fastVLM.IsModelLoaded || fastVLM.IsInferenceRunning)
                return;

            Texture2D inputImage = GetInputImage();
            string prompt = GetPrompt();

            if (inputImage == null)
            {
                UpdateStatus("Error: No input image available");
                return;
            }

            if (string.IsNullOrEmpty(prompt))
            {
                UpdateStatus("Error: No prompt provided");
                return;
            }

            UpdateStatus("Running inference...");
            if (runInferenceButton != null)
                runInferenceButton.interactable = false;

            fastVLM.InferAsync(inputImage, prompt);
        }

        // FastVLM Event Handlers
        private void OnLoadProgress(float progress)
        {
            if (progressSlider != null)
                progressSlider.value = progress;
                
            UpdateStatus($"Loading model... {Mathf.RoundToInt(progress * 100)}%");
        }

        private void OnModelLoaded()
        {
            UpdateStatus("Model loaded successfully!");
            
            if (loadModelButton != null)
                loadModelButton.interactable = false;
                
            if (runInferenceButton != null)
                runInferenceButton.interactable = true;
                
            if (progressSlider != null)
                progressSlider.gameObject.SetActive(false);
        }

        private void OnInferenceComplete(string result)
        {
            UpdateStatus("Inference complete!");
            
            if (resultText != null)
                resultText.text = result;
            else
                Debug.Log($"FastVLM Result: {result}");
                
            if (runInferenceButton != null)
                runInferenceButton.interactable = true;
        }

        private void OnInferenceError(string error)
        {
            UpdateStatus($"Error: {error}");
            
            if (resultText != null)
                resultText.text = $"Error: {error}";
                
            if (runInferenceButton != null)
                runInferenceButton.interactable = true;
        }

        // Helper Methods
        private Texture2D GetInputImage()
        {
            // Try to get image from UI display first
            if (imageDisplay != null && imageDisplay.texture is Texture2D)
            {
                return imageDisplay.texture as Texture2D;
            }
            
            // Fall back to test image
            return testImage;
        }

        private string GetPrompt()
        {
            if (promptInputField != null && !string.IsNullOrEmpty(promptInputField.text))
            {
                return promptInputField.text;
            }
            
            return defaultPrompt;
        }

        private void UpdateStatus(string status)
        {
            if (statusText != null)
                statusText.text = status;
                
            Debug.Log($"FastVLM: {status}");
        }

        // Public API for external scripts
        public void SetInputImage(Texture2D image)
        {
            testImage = image;
            if (imageDisplay != null)
                imageDisplay.texture = image;
        }

        public void SetPrompt(string prompt)
        {
            if (promptInputField != null)
                promptInputField.text = prompt;
            else
                defaultPrompt = prompt;
        }

        public void RunQuickInference(Texture2D image, string prompt)
        {
            SetInputImage(image);
            SetPrompt(prompt);
            
            if (fastVLM != null && fastVLM.IsModelLoaded)
            {
                OnRunInferenceClicked();
            }
            else
            {
                UpdateStatus("Model not loaded. Load model first.");
            }
        }

        // Camera integration helpers
        public void StartCameraCapture()
        {
#if UNITY_IOS && !UNITY_EDITOR
            // This would integrate with iOS camera
            UpdateStatus("Camera integration not implemented in this helper");
#else
            UpdateStatus("Camera only available on iOS");
#endif
        }

        private void OnDestroy()
        {
            // Clean up event listeners
            if (fastVLM != null)
            {
                fastVLM.OnLoadProgress.RemoveListener(OnLoadProgress);
                fastVLM.OnModelLoaded.RemoveListener(OnModelLoaded);
                fastVLM.OnInferenceComplete.RemoveListener(OnInferenceComplete);
                fastVLM.OnInferenceError.RemoveListener(OnInferenceError);
            }
        }
    }
}
