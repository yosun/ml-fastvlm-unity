using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FastVLM.Unity
{
    /// <summary>
    /// Unity interface for FastVLM - High-performance on-device vision-language models
    /// </summary>
    public class FastVLMInference : MonoBehaviour
    {
        [Header("Model Configuration")]
        [SerializeField] private FastVLMModelType modelType = FastVLMModelType.FastVLM_0_5B;
        [SerializeField] private bool autoLoadModel = true;
        
        [Header("Generation Parameters")]
        [SerializeField] private float temperature = 0.0f;
        [SerializeField] private int maxTokens = 240;
        
        [Header("Events")]
        public UnityEngine.Events.UnityEvent<string> OnInferenceComplete;
        public UnityEngine.Events.UnityEvent<string> OnInferenceError;
        public UnityEngine.Events.UnityEvent<float> OnLoadProgress;
        public UnityEngine.Events.UnityEvent OnModelLoaded;

        private bool isModelLoaded = false;
        private bool isInferenceRunning = false;

#if UNITY_IOS && !UNITY_EDITOR
        // Native iOS plugin methods
        [DllImport("__Internal")]
        private static extern void FastVLM_Initialize();
        
        [DllImport("__Internal")]
        private static extern void FastVLM_LoadModel(int modelType, LoadProgressCallback progressCallback);
        
        [DllImport("__Internal")]
        private static extern void FastVLM_SetGenerationParameters(float temperature, int maxTokens);
        
        [DllImport("__Internal")]
        private static extern void FastVLM_InferAsync(IntPtr imageData, int width, int height, string prompt, InferenceCallback callback);
        
        [DllImport("__Internal")]
        private static extern void FastVLM_Cancel();
        
        [DllImport("__Internal")]
        private static extern bool FastVLM_IsModelLoaded();
        
        [DllImport("__Internal")]
        private static extern bool FastVLM_IsInferenceRunning();

        // Callback delegates
        private delegate void LoadProgressCallback(float progress);
        private delegate void InferenceCallback(IntPtr result);
        
        [AOT.MonoPInvokeCallback(typeof(LoadProgressCallback))]
        private static void OnLoadProgressNative(float progress)
        {
            if (Instance != null)
            {
                Instance.HandleLoadProgress(progress);
            }
        }
        
        [AOT.MonoPInvokeCallback(typeof(InferenceCallback))]
        private static void OnInferenceCompleteNative(IntPtr resultPtr)
        {
            if (Instance != null)
            {
                // Native side returns UTF8 (char*) so use Ansi marshaling explicitly
                string result = Marshal.PtrToStringAnsi(resultPtr);
                Instance.HandleInferenceComplete(result);
            }
        }
#endif

        public static FastVLMInference Instance { get; private set; }

        public bool IsModelLoaded => isModelLoaded;
        public bool IsInferenceRunning => isInferenceRunning;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

#if UNITY_IOS && !UNITY_EDITOR
            FastVLM_Initialize();
#endif
        }

        private void Start()
        {
            if (autoLoadModel)
            {
                LoadModel();
            }
        }

        /// <summary>
        /// Load the FastVLM model
        /// </summary>
        public void LoadModel()
        {
            if (isModelLoaded)
            {
                Debug.LogWarning("FastVLM: Model is already loaded");
                return;
            }

#if UNITY_IOS && !UNITY_EDITOR
            FastVLM_LoadModel((int)modelType, OnLoadProgressNative);
#else
            // Editor/other platforms - simulate loading
            StartCoroutine(SimulateModelLoading());
#endif
        }

        /// <summary>
        /// Perform inference with a texture and prompt
        /// </summary>
        /// <param name="inputTexture">Input image texture</param>
        /// <param name="prompt">Text prompt for the model</param>
        public void InferAsync(Texture2D inputTexture, string prompt)
        {
            if (!isModelLoaded)
            {
                OnInferenceError?.Invoke("Model not loaded. Call LoadModel() first.");
                return;
            }

            if (isInferenceRunning)
            {
                OnInferenceError?.Invoke("Inference already running. Cancel previous inference first.");
                return;
            }

            if (inputTexture == null)
            {
                OnInferenceError?.Invoke("Input texture is null");
                return;
            }

            if (string.IsNullOrEmpty(prompt))
            {
                OnInferenceError?.Invoke("Prompt is null or empty");
                return;
            }

            isInferenceRunning = true;

#if UNITY_IOS && !UNITY_EDITOR
            // Set generation parameters
            FastVLM_SetGenerationParameters(temperature, maxTokens);
            
            // Convert texture to raw data
            Color32[] pixels = inputTexture.GetPixels32();
            int size = pixels.Length * 4; // 4 bytes per pixel (RGBA)
            IntPtr pixelData = Marshal.AllocHGlobal(size);
            
            try
            {
                byte[] byteArray = new byte[size];
                for (int i = 0; i < pixels.Length; i++)
                {
                    byteArray[i * 4] = pixels[i].r;
                    byteArray[i * 4 + 1] = pixels[i].g;
                    byteArray[i * 4 + 2] = pixels[i].b;
                    byteArray[i * 4 + 3] = pixels[i].a;
                }
                
                Marshal.Copy(byteArray, 0, pixelData, size);
                FastVLM_InferAsync(pixelData, inputTexture.width, inputTexture.height, prompt, OnInferenceCompleteNative);
            }
            finally
            {
                Marshal.FreeHGlobal(pixelData);
            }
#else
            // Editor/other platforms - simulate inference
            StartCoroutine(SimulateInference(prompt));
#endif
        }

        /// <summary>
        /// Cancel current inference
        /// </summary>
        public void CancelInference()
        {
            if (!isInferenceRunning)
            {
                return;
            }

#if UNITY_IOS && !UNITY_EDITOR
            FastVLM_Cancel();
#endif
            isInferenceRunning = false;
        }

        private void HandleLoadProgress(float progress)
        {
            OnLoadProgress?.Invoke(progress);
            
            if (progress >= 1.0f)
            {
                isModelLoaded = true;
                OnModelLoaded?.Invoke();
            }
        }

        private void HandleInferenceComplete(string result)
        {
            isInferenceRunning = false;
            
            if (string.IsNullOrEmpty(result))
            {
                OnInferenceError?.Invoke("Empty result from inference");
            }
            else
            {
                OnInferenceComplete?.Invoke(result);
            }
        }

#if UNITY_EDITOR || !UNITY_IOS
        // Editor simulation methods
        private IEnumerator SimulateModelLoading()
        {
            float progress = 0f;
            while (progress < 1f)
            {
                progress += Time.deltaTime * 0.5f; // 2 second load time
                OnLoadProgress?.Invoke(progress);
                yield return null;
            }
            
            OnLoadProgress?.Invoke(1f);
            isModelLoaded = true;
            OnModelLoaded?.Invoke();
        }

        private IEnumerator SimulateInference(string prompt)
        {
            yield return new WaitForSeconds(2f); // Simulate 2 second inference
            
            string simulatedResult = $"Simulated response to: '{prompt}'. This is a placeholder result for editor testing.";
            HandleInferenceComplete(simulatedResult);
        }
#endif

        private void OnDestroy()
        {
            if (Instance == this)
            {
                CancelInference();
                Instance = null;
            }
        }
    }

    /// <summary>
    /// Available FastVLM model types
    /// </summary>
    public enum FastVLMModelType
    {
        FastVLM_0_5B = 0,  // Small and fast
        FastVLM_1_5B = 1,  // Balanced
        FastVLM_7B = 2     // Large and accurate
    }
}
