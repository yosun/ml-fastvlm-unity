using System;
using UnityEngine;

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace FastVLM.Unity
{
    /// <summary>
    /// Higher-level convenience wrapper specifically for native iOS usage.
    /// Bridges Unity Texture -> native FastVLM pipeline with simplified events similar to FastVLMClient.
    /// </summary>
    public class FastVLMiOS : MonoBehaviour
    {
        [Header("Generation Parameters")] [Range(0,1)] public float temperature = 0.2f;
        [Range(1,4096)] public int maxTokens = 256;
        public FastVLMModelType modelType = FastVLMModelType.FastVLM_0_5B;
        public bool autoInitialize = true;

        public event Action<bool> OnInitializationStatusChanged; // bool -> initialized
        public event Action<FastVLMResponse> OnInferenceComplete;
        public event Action<string> OnInferenceError;
        public event Action<float> OnLoadProgress; // model load progress

        public bool IsInitialized => _modelLoaded;
        public bool IsInferenceInProgress => _inferenceRunning;

        private bool _modelLoaded;
        private bool _inferenceRunning;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void FastVLM_Initialize();
        [DllImport("__Internal")] private static extern void FastVLM_LoadModel(int modelType, LoadProgressCallback progressCallback);
        [DllImport("__Internal")] private static extern void FastVLM_SetGenerationParameters(float temperature, int maxTokens);
        [DllImport("__Internal")] private static extern void FastVLM_InferAsync(IntPtr imageData, int width, int height, string prompt, InferenceCallback callback);
        [DllImport("__Internal")] private static extern void FastVLM_Cancel();
        [DllImport("__Internal")] private static extern bool FastVLM_IsModelLoaded();
        [DllImport("__Internal")] private static extern bool FastVLM_IsInferenceRunning();

        private delegate void LoadProgressCallback(float progress);
        private delegate void InferenceCallback(IntPtr result);

        [AOT.MonoPInvokeCallback(typeof(LoadProgressCallback))]
        private static void HandleProgress(float progress)
        {
            if (Instance != null) Instance.InternalHandleProgress(progress);
        }

        [AOT.MonoPInvokeCallback(typeof(InferenceCallback))]
        private static void HandleResult(IntPtr resultPtr)
        {
            if (Instance != null)
            {
                string result = Marshal.PtrToStringAnsi(resultPtr);
                Instance.InternalHandleResult(result);
            }
        }
#endif

        public static FastVLMiOS Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
#if UNITY_IOS && !UNITY_EDITOR
            FastVLM_Initialize();
#endif
        }

        private void Start()
        {
            if (autoInitialize) InitializeAsync();
        }

        public void InitializeAsync()
        {
            if (_modelLoaded) return;
#if UNITY_IOS && !UNITY_EDITOR
            FastVLM_LoadModel((int)modelType, HandleProgress);
#else
            // Simulate in editor
            StartCoroutine(SimLoad());
#endif
        }

        public void SetGenerationParameters(float temp = 0.2f, int maxTok = 256)
        {
            temperature = temp;
            maxTokens = maxTok;
#if UNITY_IOS && !UNITY_EDITOR
            if (_modelLoaded) FastVLM_SetGenerationParameters(temperature, maxTokens);
#endif
        }

        public void InferAsync(string prompt, Texture2D texture)
        {
            if (!_modelLoaded)
            {
                OnInferenceError?.Invoke("Model not loaded");
                return;
            }
            if (_inferenceRunning)
            {
                OnInferenceError?.Invoke("Inference already running");
                return;
            }
            if (texture == null)
            {
                OnInferenceError?.Invoke("Texture null");
                return;
            }
            if (string.IsNullOrEmpty(prompt))
            {
                OnInferenceError?.Invoke("Prompt empty");
                return;
            }

            _inferenceRunning = true;
#if UNITY_IOS && !UNITY_EDITOR
            FastVLM_SetGenerationParameters(temperature, maxTokens);
            Color32[] pixels = texture.GetPixels32();
            int size = pixels.Length * 4;
            IntPtr unmanaged = Marshal.AllocHGlobal(size);
            try
            {
                byte[] data = new byte[size];
                for (int i = 0; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    int o = i * 4;
                    data[o] = p.r; data[o + 1] = p.g; data[o + 2] = p.b; data[o + 3] = p.a;
                }
                Marshal.Copy(data, 0, unmanaged, size);
                FastVLM_InferAsync(unmanaged, texture.width, texture.height, prompt, HandleResult);
            }
            finally
            {
                Marshal.FreeHGlobal(unmanaged);
            }
#else
            StartCoroutine(SimInfer(prompt));
#endif
        }

        public void Cancel()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_inferenceRunning) FastVLM_Cancel();
#endif
            _inferenceRunning = false;
        }

        private void InternalHandleProgress(float p)
        {
            OnLoadProgress?.Invoke(p);
            if (p >= 1f)
            {
                _modelLoaded = true;
                OnInitializationStatusChanged?.Invoke(true);
            }
        }

        private void InternalHandleResult(string result)
        {
            _inferenceRunning = false;
            if (string.IsNullOrEmpty(result))
            {
                OnInferenceError?.Invoke("Empty result");
                return;
            }
            var response = new FastVLMResponse { result = result, success = true, inference_time = 0f };
            OnInferenceComplete?.Invoke(response);
        }

#if UNITY_EDITOR || !UNITY_IOS
        private System.Collections.IEnumerator SimLoad()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.5f;
                OnLoadProgress?.Invoke(t);
                yield return null;
            }
            _modelLoaded = true;
            OnInitializationStatusChanged?.Invoke(true);
        }

        private System.Collections.IEnumerator SimInfer(string prompt)
        {
            yield return new WaitForSeconds(1.5f);
            _inferenceRunning = false;
            var response = new FastVLMResponse { result = $"Simulated iOS native response to: {prompt}", success = true, inference_time = 1.5f };
            OnInferenceComplete?.Invoke(response);
        }
#endif

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
