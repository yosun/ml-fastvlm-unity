using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FastVLM.Unity.Examples
{
    /// <summary>
    /// Example demonstrating how to use FastVLM with Unity
    /// </summary>
    public class FastVLMExample : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button inferButton;
        [SerializeField] private InputField promptInput;
        [SerializeField] private RawImage imageDisplay;
        [SerializeField] private Text resultText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button loadImageButton;

        [Header("FastVLM Configuration")]
        [SerializeField] private FastVLMClient vlmClient;

        [Header("Sample Images")]
        [SerializeField] private Texture2D[] sampleImages;
        [SerializeField] private string[] samplePrompts = {
            "Describe what you see in this image.",
            "What is the main subject of this image?",
            "Count the number of objects in this image.",
            "What colors are prominent in this image?",
            "Describe the scene and atmosphere."
        };

        private Texture2D currentTexture;
        private int currentSampleIndex = 0;

        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
            CheckServerConnection();
        }

        private void InitializeUI()
        {
            if (promptInput != null)
            {
                promptInput.text = samplePrompts[0];
            }

            if (sampleImages != null && sampleImages.Length > 0)
            {
                LoadSampleImage(0);
            }

            if (statusText != null)
            {
                statusText.text = "Checking server connection...";
            }

            if (resultText != null)
            {
                resultText.text = "No inference performed yet.";
            }
        }

        private void SetupEventListeners()
        {
            if (inferButton != null)
            {
                inferButton.onClick.AddListener(PerformInference);
            }

            if (loadImageButton != null)
            {
                loadImageButton.onClick.AddListener(LoadNextSampleImage);
            }

            if (vlmClient != null)
            {
                vlmClient.OnInferenceComplete += OnInferenceComplete;
                vlmClient.OnInferenceError += OnInferenceError;
            }
        }

        private void CheckServerConnection()
        {
            if (vlmClient != null)
            {
                vlmClient.CheckServerHealth(isHealthy =>
                {
                    if (statusText != null)
                    {
                        statusText.text = isHealthy ? "Server: Connected" : "Server: Disconnected";
                        statusText.color = isHealthy ? Color.green : Color.red;
                    }

                    if (inferButton != null)
                    {
                        inferButton.interactable = isHealthy;
                    }
                });
            }
        }

        private void PerformInference()
        {
            if (vlmClient == null)
            {
                Debug.LogError("FastVLM Client is not assigned!");
                return;
            }

            if (currentTexture == null)
            {
                if (statusText != null)
                {
                    statusText.text = "No image loaded!";
                    statusText.color = Color.red;
                }
                return;
            }

            string prompt = promptInput != null ? promptInput.text : samplePrompts[0];

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

            // Perform inference
            vlmClient.InferAsync(prompt, currentTexture);
        }

        private void OnInferenceComplete(FastVLMResponse response)
        {
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

        private void LoadSampleImage(int index)
        {
            if (sampleImages == null || index < 0 || index >= sampleImages.Length)
                return;

            currentTexture = sampleImages[index];
            
            if (imageDisplay != null)
            {
                imageDisplay.texture = currentTexture;
            }

            // Update prompt to match the sample
            if (promptInput != null && index < samplePrompts.Length)
            {
                promptInput.text = samplePrompts[index];
            }
        }

        private void LoadNextSampleImage()
        {
            if (sampleImages == null || sampleImages.Length == 0)
                return;

            currentSampleIndex = (currentSampleIndex + 1) % sampleImages.Length;
            LoadSampleImage(currentSampleIndex);

            if (statusText != null)
            {
                statusText.text = $"Loaded sample image {currentSampleIndex + 1}/{sampleImages.Length}";
                statusText.color = Color.blue;
            }
        }

        /// <summary>
        /// Load image from file path (for testing)
        /// </summary>
        public void LoadImageFromPath(string imagePath)
        {
            if (System.IO.File.Exists(imagePath))
            {
                byte[] imageData = System.IO.File.ReadAllBytes(imagePath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(imageData);
                
                currentTexture = texture;
                
                if (imageDisplay != null)
                {
                    imageDisplay.texture = currentTexture;
                }

                if (statusText != null)
                {
                    statusText.text = "Custom image loaded";
                    statusText.color = Color.blue;
                }
            }
            else
            {
                Debug.LogError($"Image file not found: {imagePath}");
            }
        }

        private void OnDestroy()
        {
            if (vlmClient != null)
            {
                vlmClient.OnInferenceComplete -= OnInferenceComplete;
                vlmClient.OnInferenceError -= OnInferenceError;
            }
        }
    }
}
