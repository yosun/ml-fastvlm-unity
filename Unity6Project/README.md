# Unity 6 FastVLM Test Project

Minimal Unity 6 style project structure referencing the local FastVLM plugin via file dependency in `Packages/manifest.json`.

## Structure
- `Packages/manifest.json` includes `"com.fastvlm.unity": "file:../UnityPlugin"`
- `Assets/Scenes/SampleScene.unity` placeholder scene
- `ProjectSettings/ProjectVersion.txt` placeholder version (adjust to your installed Unity 6 editor)

## Usage
1. Open Unity Hub > Add project > point to this `Unity6Project` folder.
2. When opened, Unity will resolve the local package.
3. Import samples from the Package Manager if desired.
4. Create a GameObject and add `FastVLMiOS` or use an existing sample scene.

## Notes
- Update `ProjectVersion.txt` to match the exact Unity 6 editor version you have installed.
- For iOS builds, set Player Settings (IL2CPP, ARM64, min iOS 15.0+).
- Place model weights under `Assets/StreamingAssets/FastVLM/model` for on-device inference.
