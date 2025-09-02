# FastVLM Unity iOS Plugin

A Unity package that integrates FastVLM (Fast Vision Language Model) for **on‑device iOS inference** via Apple's MLX plus an optional **HTTP backend** path for cross‑platform prototyping. The primary (recommended) path is fully on‑device using the native iOS bridge.

## 🌟 Features

### Core Capabilities
- 🚀 **Native iOS Performance**: Direct MLX usage through a Swift / Obj‑C++ bridge
- 📱 **On‑Device Inference**: Runs entirely on device (no server required)
- 🔄 **Async Operations**: Non‑blocking model load + inference callbacks
- 🎯 **Unity Integration**: Drop a component, subscribe to events
- 📦 **Multiple Model Sizes**: 0.5B / 1.5B / 7B (device memory permitting)
- 🛡️ **Mobile Conscious**: Minimal allocations; texture → RGBA buffer → native

### Technical Features
- 🖼️ **Texture2D Input**: Provide any readable `Texture2D`
- ⚡ **Realtime Friendly**: Camera capture → inference pattern supported
- 🔧 **Post-Build Configuration**: iOS build processor links required frameworks
- 🎮 **Event Callbacks**: Load progress, model ready, inference result, error
- 🔒 **Error Handling**: Graceful validation before invoking native layer
- 🎛️ **Adjustable Parameters**: Temperature + max tokens (native), richer params via HTTP path
- 🛠️ **Editor Utilities**: Optional backend manager window (for HTTP mode)
- 🌐 **Optional HTTP Mode**: Prototype on desktop / other platforms

> Note: Explicit performance / memory profiling utilities are **not yet implemented** (removed from earlier claims). Use Unity Profiler / Xcode Instruments.

## 🚀 Quick Start

Pick ONE of the two integration paths:

| Path | When to Use | Component |
|------|-------------|-----------|
| Native iOS (Recommended) | Shipping iOS / on‑device privacy & speed | `FastVLMiOS` (or lower‑level `FastVLMInference`) |
| HTTP Backend (Optional) | Cross‑platform prototyping / editor testing | `FastVLMClient` |

git clone https://github.com/yosun/ml-fastvlm-unity.git
### Method 1: Native iOS (Recommended)

```bash
# 1. Clone repository
git clone https://github.com/yosun/ml-fastvlm-unity.git
cd ml-fastvlm-unity

# 2. (Optional helper) run iOS setup script to copy plugin & download a model
cd UnityPlugin
chmod +x setup_ios.sh
./setup_ios.sh --unity-project /ABS/PATH/To/YourUnityProject --model-size 0.5b

# 3. OR manually copy the package (alternative):
#   Copy the entire UnityPlugin folder into YourUnityProject/Packages/com.fastvlm.unity
#   or add via Package Manager (Add package from disk... select package.json)

# 4. Place model weights (if not using script) into:
#   YourUnityProject/Assets/StreamingAssets/FastVLM/model
```

In Unity:
1. Create an empty GameObject (e.g. `FastVLM`)
2. Add the `FastVLMiOS` component (or `FastVLMInference` if you prefer the lower-level interface)
3. Enable Auto Initialize (or call `InitializeAsync()` manually)
4. Subscribe to events (initialization → inference)
5. Build for iOS (Deployment Target ≥ 15.0, IL2CPP, ARM64)

### Method 2: HTTP Backend (Optional)

```bash
# 1. (Optional) Set up backend server for desktop/editor prototyping
cd UnityPlugin/Backend
./setup.sh   # installs dependencies

# 2. Start server (default port 8000)
./start_server.sh

# 3. In Unity: Add the FastVLMClient component to a GameObject
```

### Unity Configuration Summary

1. Native iOS: add `FastVLMiOS` (preferred) or `FastVLMInference` to a GameObject
2. HTTP mode: add `FastVLMClient` to a GameObject (and start backend)
3. iOS Player Settings: Deployment Target ≥ 15.0, Scripting Backend IL2CPP, Architecture ARM64
4. Build & run on a physical iOS device (simulators may not reflect performance)

## Usage Examples

### Native iOS Example (Recommended)

```csharp
using FastVLM.Unity;
using UnityEngine;

public class NativeVLMExample : MonoBehaviour
{
    [SerializeField] private FastVLMiOS fastVLM; // or FastVLMInference
    [SerializeField] private Texture2D inputImage;
    
    private void Start()
    {
        // Subscribe to events
        fastVLM.OnModelLoaded.AddListener(() => {
            Debug.Log("FastVLM model loaded and ready!");
        });
        
        fastVLM.OnInferenceComplete.AddListener((result) => {
            Debug.Log($"FastVLM Result: {result}");
        });
        
        fastVLM.OnInferenceError.AddListener((error) => {
            Debug.LogError($"FastVLM Error: {error}");
        });
    }
    
    public void AnalyzeImage()
    {
        if (fastVLM.IsModelLoaded && inputImage != null)
        {
            fastVLM.InferAsync(inputImage, "What do you see in this image?");
        }
    }
}
```

### HTTP Client Example (Development)

```csharp
using UnityEngine;
using FastVLM;

public class HTTPVLMExample : MonoBehaviour
{
    [SerializeField] private FastVLMClient vlmClient;
    [SerializeField] private Texture2D inputImage;

    private void Start()
    {
        vlmClient.OnInferenceComplete += OnInferenceComplete;
        vlmClient.OnInferenceError += OnInferenceError;
        // To change server URL: set the serialized field in Inspector or expose a public method
        // (Current implementation uses a serialized private field `serverUrl`.)
    }

    private void OnInferenceComplete(FastVLMResponse response)
    {
        Debug.Log($"HTTP VLM Response: {response.result}");
    }

    private void OnInferenceError(string error)
    {
        Debug.LogError($"HTTP VLM Error: {error}");
    }

    public void AnalyzeImage()
    {
        if (inputImage != null)
            vlmClient.InferAsync("Describe what you see in this image.", inputImage);
    }
}
```

## API Reference

### Components Overview

| Component | Platform | Purpose |
|-----------|----------|---------|
| `FastVLMiOS` | iOS (device) | High‑level native on‑device wrapper (recommended) |
| `FastVLMInference` | iOS (device) | Lower‑level native interface (kept for flexibility) |
| `FastVLMClient` | Any (with backend) | HTTP JSON inference client |
| `FastVLMUIHelper` | Editor / Any | Quick prototyping UI for `FastVLMInference` |

### FastVLMClient (HTTP Mode)

#### Properties

- `serverUrl` (string): URL of the FastVLM backend server
- `requestTimeout` (float): Timeout for HTTP requests in seconds
- `debugMode` (bool): Enable debug logging
- `temperature` (float): Sampling temperature (0.0-1.0)
- `topP` (float): Top-p sampling parameter
- `numBeams` (int): Number of beams for beam search
- `maxTokens` (int): Maximum number of tokens to generate

#### Methods

- `InferAsync(string prompt, Texture2D texture)`: Perform asynchronous inference
- `InferSync(string prompt, Texture2D texture, Action<FastVLMResponse> callback)`: (Wrapper around async) invokes event then callback
- `CheckServerHealth(Action<bool> callback)`: Check if the server is available
- `SetGenerationParameters(float temp, float topP, int beams, int maxTokens)`: Update generation parameters

#### Events

- `OnInferenceComplete`: Triggered when inference completes successfully
- `OnInferenceError`: Triggered when inference fails

### FastVLMResponse

Data structure for inference results.

```csharp
[Serializable]
public class FastVLMResponse
{
    public string result;           // The generated text
    public bool success;            // Whether inference succeeded
    public string error;            // Error message (if any)
    public float inference_time;    // Time taken for inference
}
```

### FastVLMRequest

Data structure for inference requests.

```csharp
[Serializable]
public class FastVLMRequest
{
    public string prompt;           // Text prompt
    public string image_base64;     // Base64-encoded image
    public float temperature;       // Sampling temperature
    public float top_p;             // Top-p sampling
    public int num_beams;           // Beam search beams
    public int max_tokens;          // Maximum tokens
}
```

## Backend Server (Optional)

Used only for the HTTP prototyping path. Not required for native iOS operation.

### API Endpoints

#### Health Check
```
GET /health
```
Returns server status and model loading state.

#### Inference
```
POST /infer
Content-Type: application/json

{
  "prompt": "Describe the image.",
  "image_base64": "base64_encoded_image_data",
  "temperature": 0.2,
  "top_p": 0.9,
  "num_beams": 1,
  "max_tokens": 256
}
```

#### Configuration
```
GET /config
```
Returns server configuration information.

### Server Options

```bash
python fastvlm_server.py \
    --model-path /path/to/model \
    --host localhost \
    --port 8000 \
    --debug
```

## System Requirements

### Unity
- Unity 2020.3 or later
- .NET Standard 2.0 or .NET Framework 4.x

### Backend Server (Optional)
- Python 3.8+
- PyTorch 1.9+
- 8GB+ RAM (16GB+ recommended)
- CUDA-compatible GPU (optional, for faster inference)

### Dependencies

The backend requires these Python packages:
- torch
- transformers
- pillow
- flask
- flask-cors
- numpy

See `Backend/requirements.txt` for a complete list.

## Performance Optimization

### Model Selection
- **FastVLM-0.5B**: Fastest, lowest memory usage
- **FastVLM-1.5B**: Balanced performance and quality
- **FastVLM-7B**: Best quality, highest resource requirements

### Unity Optimization
- Use lower resolution textures when possible
- Cache the `FastVLMClient` component
- Implement request queuing for multiple simultaneous requests
- Consider using `InferSync` for sequential operations

### Server Optimization
- Use GPU acceleration when available
- Adjust image resolution & model size
- (Optional) Add request queueing to avoid overload

## Troubleshooting

### Common Issues

**Server won't start**
- Check Python dependencies: `pip install -r requirements.txt`
- Verify model path exists and contains valid FastVLM model
- Check port availability (default: 8000)

**Connection refused**
- Ensure server is running
- Check firewall settings
- Verify server URL in Unity

**Out of memory**
- Use smaller model variant
- Reduce image resolution
- Lower max_tokens parameter

**Slow inference**
- Enable GPU acceleration
- Use smaller images
- Consider model quantization

### Debug Mode

Enable debug mode in the `FastVLMClient` to see detailed logs:

```csharp
vlmClient.debugMode = true;
```

## Development

### Building from Source

1. Clone the repository:
```bash
git clone https://github.com/yosun/ml-fastvlm-unity.git
cd ml-fastvlm-unity
```

2. (Optional HTTP) set up backend if you will use the HTTP path:
```bash
cd UnityPlugin/Backend && ./setup.sh
```

3. Add the package to Unity (Package Manager from disk → select `UnityPlugin/package.json`) or copy under `Packages/com.fastvlm.unity`.

### Contributing

Contributions are welcome! Please see the [contributing guidelines](CONTRIBUTING.md) for details.

## License

This project is licensed under the same terms as the original FastVLM project. See [LICENSE](LICENSE) for details.

## Accuracy / Documentation Status

This README reflects the current codebase (components present, available methods). If you add new generation parameters (e.g. top_p / beams) to the native layer, update both the wrapper (`FastVLMiOS`) and this document.

## Citation

If you use this plugin in your research, please cite the original FastVLM paper:

```bibtex
@InProceedings{fastvlm2025,
  author = {Pavan Kumar Anasosalu Vasu, Fartash Faghri, Chun-Liang Li, Cem Koc, Nate True, Albert Antony, Gokul Santhanam, James Gabriel, Peter Grasch, Oncel Tuzel, Hadi Pouransari},
  title = {FastVLM: Efficient Vision Encoding for Vision Language Models},
  booktitle = {Proceedings of the IEEE/CVF Conference on Computer Vision and Pattern Recognition (CVPR)},
  month = {June},
  year = {2025},
}
```

## Support

For issues and questions:
- Check the [troubleshooting section](#troubleshooting)
- Open an issue on [GitHub](https://github.com/yosun/ml-fastvlm-unity/issues)
- Check the original [FastVLM repository](https://github.com/apple/ml-fastvlm) for model-related questions
