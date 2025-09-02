# FastVLM Unity Plugin - Quick Start Guide

Get up and running with FastVLM in Unity in just a few minutes!

## 📋 Prerequisites

- macOS with Xcode 14.0+
- Unity 2021.3 or later
- iOS device running iOS 15.0+ for testing

## 🚀 Quick Setup (5 minutes)

### Step 1: Download and Setup
```bash
# Clone the repository
git clone https://github.com/yosun/ml-fastvlm-unity.git
cd ml-fastvlm-unity

# Run the setup script
chmod +x UnityPlugin/setup_ios.sh
./UnityPlugin/setup_ios.sh
```

### Step 2: Import into Unity
1. Open Unity and create a new 3D project
2. Copy the `UnityPlugin` folder to your project's `Assets` folder
3. Wait for Unity to import the package

### Step 3: Create Your First Scene
1. Create a new scene
2. Add an empty GameObject and name it "FastVLM Manager"
3. Add the `FastVLMInference` component to it
4. Add the `FastVLMUIHelper` component for a quick UI

### Step 4: Configure for iOS
1. Go to **File > Build Settings**
2. Switch platform to **iOS**
3. Go to **Player Settings**
4. Set **iOS Deployment Target** to **15.0** or higher
5. Set **Target Device Family** to **iPhone & iPad**

### Step 5: Build and Test
1. Click **Build** in Build Settings
2. Choose a build folder
3. Open the generated Xcode project
4. Connect your iOS device
5. Build and run on device

## 🎯 First Inference

Once your app is running on iOS:

1. Tap **Load Model** (this may take a minute)
2. Wait for "Model loaded successfully!"
3. Enter a prompt like "What do you see in this image?"
4. Tap **Run Inference**
5. See the result appear below!

## 📱 Sample Code

Here's the minimal code to get started:

```csharp
using FastVLM.Unity;
using UnityEngine;

public class MyFirstVLM : MonoBehaviour 
{
    private FastVLMInference fastVLM;
    
    void Start() 
    {
        fastVLM = FindObjectOfType<FastVLMInference>();
        fastVLM.OnModelLoaded.AddListener(() => {
            Debug.Log("Ready for inference!");
        });
        fastVLM.OnInferenceComplete.AddListener(result => {
            Debug.Log($"AI says: {result}");
        });
    }
    
    public void AnalyzeImage(Texture2D image) 
    {
        fastVLM.InferAsync(image, "Describe this image");
    }
}
```

## 🔧 Troubleshooting

**Model not loading?**
- Ensure your device has enough storage (500MB+ free)
- Check that you're running on iOS 15.0+ device (not simulator)

**Build errors?**
- Verify Xcode is 14.0+
- Check iOS Deployment Target is 15.0+
- Make sure you're building to a physical device

**App crashes?**
- Ensure device has 4GB+ RAM for larger models
- Try the 0.5B model first for testing

## 📚 Next Steps

- Check out the [Full Documentation](README.md)
- Explore the [Sample Scenes](Samples~/BasicExample/)
- Read the [Setup Guide](SETUP_GUIDE.md) for advanced configuration
- Join our [Discord Community](https://discord.gg/fastvlm) for support

## 💡 Tips

- Start with FastVLM 0.5B for fastest inference
- Use the UIHelper component for rapid prototyping
- Test on device early - simulator performance differs significantly
- Monitor memory usage with larger models

**Happy coding! 🎉**
