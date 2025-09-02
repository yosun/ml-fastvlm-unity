# Changelog

All notable changes to the FastVLM Unity Plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-09-01

### Added
- Initial release of FastVLM Unity Plugin
- Native iOS integration with MLX acceleration
- Support for FastVLM 0.5B, 1.5B, and 7B models
- Async inference with progress callbacks
- Texture2D input support for Unity integration
- Automatic iOS build configuration
- Sample scenes and example scripts
- Comprehensive documentation and setup guides
- Memory-efficient inference on iOS devices
- Real-time camera integration support
- Batch processing capabilities
- Performance monitoring tools

### Features
- **FastVLMInference Component**: Main Unity component for model inference
- **FastVLMUIHelper**: Helper component for rapid prototyping
- **iOS Build Processor**: Automatic Xcode project configuration
- **Native Bridge**: C++/Objective-C++ bridge to Swift FastVLM
- **MLX Integration**: Direct integration with Apple's MLX framework
- **Multiple Model Support**: Choose between different model sizes
- **Event System**: Comprehensive callbacks for all operations
- **Error Handling**: Robust error reporting and recovery
- **Memory Management**: Optimized memory usage patterns
- **Threading**: Non-blocking async operations

### Platform Support
- iOS 15.0+ (arm64, arm64e)
- iOS Simulator support for development
- Unity 2021.3+ compatibility
- Xcode 14.0+ build support

### Requirements
- macOS development environment
- Xcode 14.0 or later
- Unity 2021.3 or later
- iOS deployment target 15.0+
- Apple Silicon Mac recommended for optimal performance

### Known Limitations
- iOS only (macOS support planned for future release)
- Requires on-device model storage (~500MB to 7GB depending on model)
- Metal GPU required for optimal performance
- Minimum 4GB RAM recommended for larger models

## [Unreleased]

### Planned Features
- macOS support
- Android support with ONNX runtime
- Cloud inference fallback option
- Model quantization options
- Custom model loading support
- Advanced camera controls
- Video processing capabilities
- Streaming inference for long inputs
