# FastVLM Unity iOS Integration Guide

This guide explains how to integrate FastVLM directly into your Unity iOS app for on-device inference without requiring a Python server.

## Overview

The iOS integration provides:
- **Native Performance**: Direct MLX-based inference on Apple Silicon
- **On-Device Processing**: No network connection required
- **Unity Integration**: Seamless C# API for Unity developers
- **Optimized Models**: Pre-quantized models for mobile deployment

## Prerequisites

### Development Environment
- **macOS**: Required for iOS development
- **Xcode 15.0+**: Latest version recommended
- **Unity 2021.3+**: With iOS build support
- **iOS 15.0+**: Minimum target iOS version

### Hardware Requirements
- **iPhone/iPad with A12+ chip**: Required for MLX acceleration
- **8GB+ RAM**: Recommended for larger models
- **2GB+ storage**: For model files

## Setup Instructions

### 1. Prepare Your Unity Project

1. **Copy the Plugin**: Copy the `FastVLM` folder to your Unity project's `Assets` directory:
   ```bash
   cp -r UnityPlugin/FastVLM /path/to/your/unity/project/Assets/
   ```

2. **Configure Player Settings**:
   - Go to **Edit > Project Settings > Player**
   - Select the iOS tab
   - Set **Minimum iOS Version** to **15.0**
   - Enable **Metal API Validation** (optional, for debugging)

3. **Import FastVLM Source**: The build process will automatically copy FastVLM Swift source files during the build.

### 2. Download and Prepare Models

1. **Download FastVLM Models**:
   ```bash
   cd ml-fastvlm-unity/app
   chmod +x get_pretrained_mlx_model.sh
   
   # Download the 0.5B model (recommended for mobile)
   ./get_pretrained_mlx_model.sh --model 0.5b --dest FastVLM/model
   ```

2. **Copy Model to Unity**:
   ```bash
   # Create StreamingAssets folder if it doesn't exist
   mkdir -p /path/to/your/unity/project/Assets/StreamingAssets/FastVLM
   
   # Copy the model
   cp -r ml-fastvlm-unity/app/FastVLM/model /path/to/your/unity/project/Assets/StreamingAssets/FastVLM/
   ```

### 3. Unity Scene Setup

1. **Create FastVLM GameObject**:
   - Create an empty GameObject in your scene
   - Add the `FastVLMiOS` component
   - Set the **Model Path** to `FastVLM/model` (relative to StreamingAssets)

2. **Add Example UI** (optional):
   - Add the `FastVLMiOSExample` component for a complete demo
   - Set up UI elements (buttons, text fields, etc.) as shown in the example

### 4. Build for iOS

1. **Configure Build Settings**:
   - Go to **File > Build Settings**
   - Select **iOS** platform
   - Click **Switch Platform** if needed

2. **Build the Project**:
   - Click **Build** or **Build and Run**
   - Choose an output directory
   - The build processor will automatically configure the Xcode project

3. **Xcode Configuration** (if needed):
   - Open the generated Xcode project
   - Verify the **Deployment Target** is set to **iOS 15.0+**
   - Check that FastVLM Swift files are included in the project
   - Ensure required frameworks are linked

## Usage in Your Unity Scripts

### Basic Usage

```csharp
using FastVLM;

public class MyVLMApp : MonoBehaviour
{
    [SerializeField] private FastVLMiOS vlmClient;
    [SerializeField] private Texture2D inputImage;
    
    private void Start()
    {
        // Subscribe to events
        vlmClient.OnInferenceComplete += OnInferenceComplete;
        vlmClient.OnInferenceError += OnInferenceError;
        vlmClient.OnInitializationStatusChanged += OnInitializationChanged;
        
        // Initialize the model
        vlmClient.InitializeAsync();
    }
    
    private void OnInitializationChanged(bool isInitialized)
    {
        if (isInitialized)
        {
            Debug.Log("FastVLM ready for inference!");
            // Perform inference
            vlmClient.InferAsync("Describe this image", inputImage);
        }
    }
    
    private void OnInferenceComplete(FastVLMResponse response)
    {
        Debug.Log($"Result: {response.result}");
        Debug.Log($"Time: {response.inference_time:F2} seconds");
    }
    
    private void OnInferenceError(string error)
    {
        Debug.LogError($"Inference failed: {error}");
    }
}
```

### Camera Integration

```csharp
public class CameraVLM : MonoBehaviour
{
    [SerializeField] private FastVLMiOS vlmClient;
    private WebCamTexture webCamTexture;
    private Texture2D captureTexture;
    
    private void Start()
    {
        // Initialize camera
        webCamTexture = new WebCamTexture();
        webCamTexture.Play();
        
        captureTexture = new Texture2D(512, 512);
        
        // Initialize VLM
        vlmClient.InitializeAsync();
    }
    
    public void CaptureAndAnalyze()
    {
        if (!vlmClient.IsInitialized) return;
        
        // Capture frame from camera
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            webCamTexture.width, webCamTexture.height);
        Graphics.Blit(webCamTexture, renderTexture);
        
        RenderTexture.active = renderTexture;
        captureTexture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        captureTexture.Apply();
        RenderTexture.active = null;
        
        RenderTexture.ReleaseTemporary(renderTexture);
        
        // Analyze the captured image
        vlmClient.InferAsync("What do you see in this camera view?", captureTexture);
    }
}
```

## Model Options

### FastVLM 0.5B (Recommended for Mobile)
- **Size**: ~1GB
- **Speed**: ~2-3 seconds on iPhone 15
- **Quality**: Good for most use cases
- **Use Case**: Real-time applications, interactive demos

### FastVLM 1.5B (Balanced)
- **Size**: ~3GB
- **Speed**: ~4-6 seconds on iPhone 15
- **Quality**: Better accuracy
- **Use Case**: Applications where quality matters more than speed

### FastVLM 7B (Highest Quality)
- **Size**: ~8GB
- **Speed**: ~10-15 seconds on iPhone 15
- **Quality**: Best available
- **Use Case**: Offline processing, high-accuracy requirements

## Performance Optimization

### Device-Specific Recommendations

**iPhone 15 Pro / iPad Pro M2+**:
- Can handle FastVLM 1.5B smoothly
- Consider FastVLM 7B for highest quality needs

**iPhone 12-14 / iPad Air**:
- FastVLM 0.5B recommended
- FastVLM 1.5B possible but slower

**iPhone SE 3rd gen / older devices**:
- FastVLM 0.5B only
- Reduce image resolution to 256x256

### App Optimization Tips

1. **Lazy Loading**: Initialize the model only when needed
2. **Image Preprocessing**: Resize images to optimal dimensions (512x512 or smaller)
3. **Background Processing**: Use async methods to avoid blocking the UI
4. **Memory Management**: Clean up textures and temporary objects promptly
5. **Thermal Management**: Monitor device temperature and reduce inference frequency if needed

## Troubleshooting

### Common Issues

**"Model not found" error**:
- Verify the model is in `StreamingAssets/FastVLM/model`
- Check that model files were copied correctly during build
- Ensure the model path in FastVLMiOS component is correct

**"Device not supported" error**:
- Check iOS version (15.0+ required)
- Verify device has A12+ chip
- Ensure Metal support is available

**Slow inference times**:
- Use FastVLM 0.5B model
- Reduce image resolution
- Check device thermal state
- Close other memory-intensive apps

**Build errors**:
- Verify Xcode version (15.0+ required)
- Check iOS deployment target (15.0+)
- Ensure Swift files are properly included
- Verify framework dependencies

### Performance Debugging

1. **Enable Debug Mode**: Set `debugMode = true` in FastVLMiOS component
2. **Monitor Memory**: Use Xcode's memory profiler
3. **Check Thermal State**: Monitor device temperature
4. **Profile Inference**: Log inference times for different image sizes

## Advanced Features

### Custom Prompts
```csharp
// Specialized prompts for different use cases
string[] prompts = {
    "Describe this image in detail.",
    "What objects can you identify?", 
    "What is the mood or atmosphere?",
    "Is this safe for children to view?",
    "What text appears in this image?",
    "Count the number of people visible."
};
```

### Batch Processing
```csharp
public async void ProcessImageBatch(Texture2D[] images, string prompt)
{
    foreach (var image in images)
    {
        vlmClient.InferAsync(prompt, image);
        await System.Threading.Tasks.Task.Delay(100); // Prevent overload
    }
}
```

### Result Caching
```csharp
private Dictionary<string, string> resultCache = new Dictionary<string, string>();

private void CacheResult(string imageHash, string result)
{
    resultCache[imageHash] = result;
}

private string GetCachedResult(string imageHash)
{
    return resultCache.TryGetValue(imageHash, out string result) ? result : null;
}
```

## Distribution Considerations

### App Store Submission
- **Model Size**: Large models (7B) may require "Download on First Launch"
- **Privacy**: Include appropriate privacy descriptions in Info.plist
- **Performance**: Test on various device generations
- **Content Rating**: Consider AI-generated content implications

### Model Distribution Options
1. **Bundle with App**: Include model in app bundle (increases app size)
2. **Download on Launch**: Download model on first app launch
3. **Cloud Distribution**: Store models in cloud storage (requires network)
4. **Hybrid Approach**: Bundle lightweight model, download larger ones optionally

## Next Steps

1. **Test on Device**: Always test on physical iOS devices
2. **Optimize for Your Use Case**: Adjust model size and parameters
3. **Implement Error Handling**: Add robust error handling and fallbacks
4. **Monitor Performance**: Track inference times and user experience
5. **Iterate and Improve**: Gather user feedback and optimize accordingly

For additional support, refer to the main documentation or open issues on the GitHub repository.
