using UnityEngine;

namespace FastVLM.Unity.Samples
{
    /// <summary>
    /// Minimal demonstration of native FastVLM usage.
    /// Drops a FastVLMiOS component on the same GameObject (if missing),
    /// creates a simple test texture (if none provided) and performs a single inference.
    /// Logs results to the Console.
    /// </summary>
    public class BarebonesFastVLMDemo : MonoBehaviour
    {
        [Header("Demo Settings")] public Texture2D inputTexture;
        [TextArea] public string prompt = "Describe this image.";
        public bool autoRunOnStart = true;
        public bool createCheckerTextureIfMissing = true;

        private FastVLMiOS _vlm;
        private bool _inferenceQueued;

        private void Awake()
        {
            // Ensure native component exists
            _vlm = GetComponent<FastVLMiOS>();
            if (_vlm == null)
                _vlm = gameObject.AddComponent<FastVLMiOS>();

            // Subscribe to events
            _vlm.OnInitializationStatusChanged += OnInit;
            _vlm.OnInferenceComplete += OnInferenceComplete;
            _vlm.OnInferenceError += OnInferenceError;
        }

        private void Start()
        {
            if (inputTexture == null && createCheckerTextureIfMissing)
            {
                inputTexture = CreateCheckerTexture(256, 256, 32);
            }

            if (autoRunOnStart)
            {
                Debug.Log("[FastVLM Barebones] Initializing model...");
                _vlm.InitializeAsync();
            }
        }

        private void OnInit(bool ready)
        {
            Debug.Log($"[FastVLM Barebones] Initialization status: {ready}");
            if (ready && !_inferenceQueued && autoRunOnStart)
            {
                RunInference();
            }
        }

        public void RunInference()
        {
            if (!_vlm.IsInitialized)
            {
                Debug.LogWarning("[FastVLM Barebones] Model not ready yet.");
                return;
            }
            if (inputTexture == null)
            {
                Debug.LogError("[FastVLM Barebones] No input texture.");
                return;
            }
            _inferenceQueued = true;
            Debug.Log($"[FastVLM Barebones] Running inference with prompt: '{prompt}'");
            _vlm.InferAsync(prompt, inputTexture);
        }

        private void OnInferenceComplete(FastVLMResponse resp)
        {
            Debug.Log($"[FastVLM Barebones] Result: {resp.result}");
        }

        private void OnInferenceError(string err)
        {
            Debug.LogError($"[FastVLM Barebones] Error: {err}");
        }

        private Texture2D CreateCheckerTexture(int width, int height, int block)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            Color32 c0 = new Color32(30, 30, 30, 255);
            Color32 c1 = new Color32(220, 220, 220, 255);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool even = ((x / block) + (y / block)) % 2 == 0;
                pixels[y * width + x] = even ? c0 : c1;
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private void OnDestroy()
        {
            if (_vlm != null)
            {
                _vlm.OnInitializationStatusChanged -= OnInit;
                _vlm.OnInferenceComplete -= OnInferenceComplete;
                _vlm.OnInferenceError -= OnInferenceError;
            }
        }
    }
}