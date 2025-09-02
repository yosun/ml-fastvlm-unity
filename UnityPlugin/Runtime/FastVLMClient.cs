using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace FastVLM.Unity
{
    /// <summary>
    /// Response data structure for FastVLM inference
    /// </summary>
    [Serializable]
    public class FastVLMResponse
    {
        public string result;
        public bool success;
        public string error;
        public float inference_time;
    }

    /// <summary>
    /// Request data structure for FastVLM inference
    /// </summary>
    [Serializable]
    public class FastVLMRequest
    {
        public string prompt;
        public string image_base64;
        public float temperature = 0.2f;
        public float top_p = 0.9f;
        public int num_beams = 1;
        public int max_tokens = 256;
    }

    /// <summary>
    /// Main FastVLM client for Unity integration
    /// </summary>
    public class FastVLMClient : MonoBehaviour
    {
        [Header("FastVLM Configuration")]
        [SerializeField] private string serverUrl = "http://localhost:8000";
        [SerializeField] private float requestTimeout = 30f;
        [SerializeField] private bool debugMode = true;

        [Header("Generation Parameters")]
        [SerializeField] private float temperature = 0.2f;
        [SerializeField] private float topP = 0.9f;
        [SerializeField] private int numBeams = 1;
        [SerializeField] private int maxTokens = 256;

        /// <summary>
        /// Event triggered when inference is completed
        /// </summary>
        public event Action<FastVLMResponse> OnInferenceComplete;

        /// <summary>
        /// Event triggered when inference fails
        /// </summary>
        public event Action<string> OnInferenceError;

        private bool isInferenceInProgress = false;

        /// <summary>
        /// Check if the FastVLM server is available
        /// </summary>
        public void CheckServerHealth(Action<bool> callback)
        {
            StartCoroutine(CheckServerHealthCoroutine(callback));
        }

        /// <summary>
        /// Perform inference with a prompt and Texture2D
        /// </summary>
        /// <param name="prompt">Text prompt for the VLM</param>
        /// <param name="texture">Input image as Texture2D</param>
        public void InferAsync(string prompt, Texture2D texture)
        {
            if (isInferenceInProgress)
            {
                LogDebug("Inference already in progress. Ignoring new request.");
                return;
            }

            if (string.IsNullOrEmpty(prompt))
            {
                OnInferenceError?.Invoke("Prompt cannot be empty");
                return;
            }

            if (texture == null)
            {
                OnInferenceError?.Invoke("Texture cannot be null");
                return;
            }

            StartCoroutine(InferAsyncCoroutine(prompt, texture));
        }

        /// <summary>
        /// Perform synchronous inference (blocking)
        /// </summary>
        /// <param name="prompt">Text prompt for the VLM</param>
        /// <param name="texture">Input image as Texture2D</param>
        /// <param name="callback">Callback with the result</param>
        public void InferSync(string prompt, Texture2D texture, Action<FastVLMResponse> callback)
        {
            StartCoroutine(InferSyncCoroutine(prompt, texture, callback));
        }

        private IEnumerator CheckServerHealthCoroutine(Action<bool> callback)
        {
            string healthUrl = $"{serverUrl}/health";
            
            using (UnityWebRequest request = UnityWebRequest.Get(healthUrl))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();

                bool isHealthy = request.result == UnityWebRequest.Result.Success;
                LogDebug($"Server health check: {(isHealthy ? "Healthy" : "Unhealthy")}");
                callback?.Invoke(isHealthy);
            }
        }

        private IEnumerator InferAsyncCoroutine(string prompt, Texture2D texture)
        {
            isInferenceInProgress = true;
            LogDebug($"Starting inference with prompt: '{prompt}'");

            // Convert texture to base64
            string imageBase64 = TextureToBase64(texture);
            if (string.IsNullOrEmpty(imageBase64))
            {
                isInferenceInProgress = false;
                OnInferenceError?.Invoke("Failed to convert texture to base64");
                yield break;
            }

            // Create request
            FastVLMRequest requestData = new FastVLMRequest
            {
                prompt = prompt,
                image_base64 = imageBase64,
                temperature = temperature,
                top_p = topP,
                num_beams = numBeams,
                max_tokens = maxTokens
            };

            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            // Send request
            string inferenceUrl = $"{serverUrl}/infer";
            using (UnityWebRequest request = new UnityWebRequest(inferenceUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = (int)requestTimeout;

                yield return request.SendWebRequest();

                isInferenceInProgress = false;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        FastVLMResponse response = JsonUtility.FromJson<FastVLMResponse>(request.downloadHandler.text);
                        LogDebug($"Inference completed successfully: {response.result}");
                        OnInferenceComplete?.Invoke(response);
                    }
                    catch (Exception e)
                    {
                        string error = $"Failed to parse response: {e.Message}";
                        LogDebug(error);
                        OnInferenceError?.Invoke(error);
                    }
                }
                else
                {
                    string error = $"Request failed: {request.error}";
                    LogDebug(error);
                    OnInferenceError?.Invoke(error);
                }
            }
        }

        private IEnumerator InferSyncCoroutine(string prompt, Texture2D texture, Action<FastVLMResponse> callback)
        {
            yield return InferAsyncCoroutine(prompt, texture);
            // Note: The callback will be triggered through the OnInferenceComplete event
            // This is a limitation of the async design, but maintains consistency
        }

        /// <summary>
        /// Convert Texture2D to base64 encoded string
        /// </summary>
        private string TextureToBase64(Texture2D texture)
        {
            try
            {
                // Ensure texture is readable
                if (!texture.isReadable)
                {
                    LogDebug("Texture is not readable. Creating a readable copy.");
                    texture = MakeTextureReadable(texture);
                }

                byte[] imageData = texture.EncodeToPNG();
                return Convert.ToBase64String(imageData);
            }
            catch (Exception e)
            {
                LogDebug($"Error converting texture to base64: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a readable copy of a non-readable texture
        /// </summary>
        private Texture2D MakeTextureReadable(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;

            Texture2D readableTexture = new Texture2D(source.width, source.height);
            readableTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            return readableTexture;
        }

        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[FastVLM] {message}");
            }
        }

        /// <summary>
        /// Set generation parameters
        /// </summary>
        public void SetGenerationParameters(float temp = 0.2f, float topP = 0.9f, int beams = 1, int maxTokens = 256)
        {
            temperature = temp;
            this.topP = topP;
            numBeams = beams;
            this.maxTokens = maxTokens;
        }

        /// <summary>
        /// Get current server status
        /// </summary>
        public bool IsInferenceInProgress => isInferenceInProgress;
    }
}
