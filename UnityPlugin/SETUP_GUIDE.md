# FastVLM Unity Plugin - Setup Guide

This guide will walk you through setting up and using the FastVLM Unity Plugin.

## Prerequisites

- Unity 2020.3 or later
- Python 3.8 or later
- 8GB+ RAM (16GB+ recommended)
- CUDA-compatible GPU (optional, for faster inference)

## Step-by-Step Setup

### 1. Prepare the Python Environment

First, create a Python virtual environment (recommended):

```bash
# Create virtual environment
python3 -m venv fastvlm_env

# Activate it
# On macOS/Linux:
source fastvlm_env/bin/activate
# On Windows:
# fastvlm_env\Scripts\activate
```

### 2. Set Up the Backend

Navigate to the backend directory and run the setup script:

```bash
cd UnityPlugin/Backend
chmod +x setup.sh
./setup.sh
```

This script will:
- Install required Python dependencies
- Set up the FastVLM environment
- Create helper scripts for running the server

### 3. Download a FastVLM Model

Choose and download a pre-trained FastVLM model:

```bash
cd ../../ml-fastvlm-unity

# Download all models (large download, ~10GB+)
bash get_models.sh

# Or download a specific model manually:
# For 0.5B model (smallest, fastest):
wget https://ml-site.cdn-apple.com/datasets/fastvlm/llava-fastvithd_0.5b_stage3.zip
unzip llava-fastvithd_0.5b_stage3.zip

# For 1.5B model (balanced):
wget https://ml-site.cdn-apple.com/datasets/fastvlm/llava-fastvithd_1.5b_stage3.zip
unzip llava-fastvithd_1.5b_stage3.zip

# For 7B model (highest quality):
wget https://ml-site.cdn-apple.com/datasets/fastvlm/llava-fastvithd_7b_stage3.zip
unzip llava-fastvithd_7b_stage3.zip
```

### 4. Test the Backend Server

Start the FastVLM server:

```bash
cd ../UnityPlugin/Backend

# Start with a specific model path
./start_server.sh /path/to/your/downloaded/model

# Example:
./start_server.sh ~/fastvlm_models/llava-fastvithd_0.5b_stage3
```

Test the server in another terminal:

```bash
python3 test_server.py
```

You should see output indicating the server is healthy.

### 5. Set Up Unity Project

1. **Copy the Plugin**: Copy the entire `FastVLM` folder to your Unity project's `Assets` directory:
   ```bash
   cp -r UnityPlugin/FastVLM /path/to/your/unity/project/Assets/
   ```

2. **Open Unity**: Open your Unity project

3. **Open FastVLM Manager**: Go to **Tools > FastVLM > Manager** in the Unity menu

4. **Configure the Plugin**: In the FastVLM Manager window:
   - Set the server URL (default: `http://localhost:8000`)
   - Set the model path to your downloaded model
   - Click "Start Server" to launch the backend from Unity

### 6. Create Your First VLM Scene

1. **Create a new scene** or open an existing one

2. **Add FastVLM Client**: 
   - Create an empty GameObject
   - Add the `FastVLMClient` component
   - Or use the FastVLM Manager: click "Create FastVLM Client in Scene"

3. **Add Test Component**: Add the `FastVLMTest` component to test functionality

4. **Configure the Client**:
   - Set the server URL (default: `http://localhost:8000`)
   - Adjust generation parameters if needed:
     - Temperature: 0.0-1.0 (0.2 recommended)
     - Top P: 0.0-1.0 (0.9 recommended)
     - Num Beams: 1-5 (1 for fastest)
     - Max Tokens: 1-512 (256 recommended)

## Basic Usage Example

Create a script to use FastVLM:

```csharp
using UnityEngine;
using FastVLM;

public class MyVLMApp : MonoBehaviour
{
    [SerializeField] private FastVLMClient vlmClient;
    [SerializeField] private Texture2D inputImage;
    
    private void Start()
    {
        // Subscribe to events
        vlmClient.OnInferenceComplete += OnInferenceComplete;
        vlmClient.OnInferenceError += OnInferenceError;
        
        // Check server health first
        vlmClient.CheckServerHealth(OnHealthCheck);
    }
    
    private void OnHealthCheck(bool isHealthy)
    {
        if (isHealthy)
        {
            Debug.Log("Server is ready!");
            // Perform inference
            vlmClient.InferAsync("What do you see in this image?", inputImage);
        }
        else
        {
            Debug.LogError("Server is not available!");
        }
    }
    
    private void OnInferenceComplete(FastVLMResponse response)
    {
        Debug.Log($"VLM says: {response.result}");
        Debug.Log($"Took {response.inference_time:F2} seconds");
    }
    
    private void OnInferenceError(string error)
    {
        Debug.LogError($"Inference failed: {error}");
    }
}
```

## Advanced Usage

### Custom Generation Parameters

```csharp
// Set custom parameters for more creative or focused outputs
vlmClient.SetGenerationParameters(
    temp: 0.7f,      // Higher for more creativity
    topP: 0.95f,     // Nucleus sampling
    beams: 3,        // Beam search for better quality
    maxTokens: 128   // Shorter responses
);
```

### Handling Multiple Images

```csharp
public class MultiImageProcessor : MonoBehaviour
{
    [SerializeField] private FastVLMClient vlmClient;
    [SerializeField] private Texture2D[] images;
    [SerializeField] private string[] prompts;
    
    private int currentIndex = 0;
    
    private void Start()
    {
        vlmClient.OnInferenceComplete += OnInferenceComplete;
        ProcessNextImage();
    }
    
    private void ProcessNextImage()
    {
        if (currentIndex < images.Length)
        {
            vlmClient.InferAsync(prompts[currentIndex], images[currentIndex]);
        }
    }
    
    private void OnInferenceComplete(FastVLMResponse response)
    {
        Debug.Log($"Image {currentIndex}: {response.result}");
        currentIndex++;
        ProcessNextImage();
    }
}
```

### Real-time Camera Integration

```csharp
public class CameraVLM : MonoBehaviour
{
    [SerializeField] private FastVLMClient vlmClient;
    [SerializeField] private Camera cam;
    [SerializeField] private float inferenceInterval = 2f;
    
    private RenderTexture renderTexture;
    private Texture2D texture2D;
    
    private void Start()
    {
        // Set up render texture
        renderTexture = new RenderTexture(512, 512, 24);
        cam.targetTexture = renderTexture;
        
        texture2D = new Texture2D(512, 512, TextureFormat.RGB24, false);
        
        vlmClient.OnInferenceComplete += OnInferenceComplete;
        
        // Start periodic inference
        InvokeRepeating(nameof(CaptureAndInfer), 1f, inferenceInterval);
    }
    
    private void CaptureAndInfer()
    {
        if (vlmClient.IsInferenceInProgress) return;
        
        // Capture frame
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;
        
        // Perform inference
        vlmClient.InferAsync("Describe what the camera sees right now.", texture2D);
    }
    
    private void OnInferenceComplete(FastVLMResponse response)
    {
        Debug.Log($"Camera sees: {response.result}");
    }
}
```

## Troubleshooting

### Common Issues

**"Connection refused" error**
- Make sure the Python server is running
- Check that the port (8000) is not blocked by firewall
- Verify the server URL in Unity matches the server

**"Model not found" error**
- Check that the model path exists and contains the FastVLM model files
- Ensure you've downloaded the model completely
- Verify file permissions

**Slow inference**
- Use a smaller model (0.5B instead of 7B)
- Reduce image resolution
- Enable GPU acceleration (CUDA)
- Lower max_tokens parameter

**Out of memory**
- Use the 0.5B model variant
- Reduce image resolution
- Close other applications
- Consider model quantization

### Debug Tips

1. **Enable debug mode** in FastVLMClient to see detailed logs
2. **Check Unity Console** for error messages
3. **Monitor server logs** in the terminal where you started the server
4. **Test server independently** using `test_server.py`
5. **Use the FastVLM Manager** for server health monitoring

### Performance Optimization

**For Development:**
- Use the 0.5B model for fastest iteration
- Keep images under 512x512 pixels
- Use temperature=0 for consistent results

**For Production:**
- Consider the 1.5B model for better quality
- Implement request queuing for multiple users
- Cache common inference results
- Use async/await patterns properly

## Next Steps

1. **Explore the Examples**: Look at the example scripts in the `Examples` folder
2. **Read the API Documentation**: Check the detailed API reference in README.md
3. **Join the Community**: Contribute to the project on GitHub
4. **Build Your App**: Start integrating VLM capabilities into your Unity project!

For more help, check the troubleshooting section or open an issue on GitHub.
