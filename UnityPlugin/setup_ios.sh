#!/bin/bash

# FastVLM Unity iOS Setup Script
# Automates the setup process for iOS deployment

set -e  # Exit on any error

echo "🚀 FastVLM Unity iOS Setup"
echo "=========================="

# Get the directory of this script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

# Check if we're on macOS
if [[ "$OSTYPE" != "darwin"* ]]; then
    print_error "This script must be run on macOS for iOS development"
    exit 1
fi

# Check if Xcode is installed
if ! command -v xcodebuild &> /dev/null; then
    print_error "Xcode is not installed. Please install Xcode from the App Store."
    exit 1
fi

# Check Xcode version
xcode_version=$(xcodebuild -version | head -n 1 | awk '{print $2}')
print_status "Found Xcode version: $xcode_version"

# Parse command line arguments
UNITY_PROJECT_PATH=""
MODEL_SIZE="0.5b"
SKIP_MODEL_DOWNLOAD=false
FORCE_SETUP=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --unity-project)
            UNITY_PROJECT_PATH="$2"
            shift 2
            ;;
        --model-size)
            MODEL_SIZE="$2"
            shift 2
            ;;
        --skip-model-download)
            SKIP_MODEL_DOWNLOAD=true
            shift
            ;;
        --force)
            FORCE_SETUP=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --unity-project PATH     Path to Unity project (required)"
            echo "  --model-size SIZE        Model size: 0.5b, 1.5b, or 7b (default: 0.5b)"
            echo "  --skip-model-download    Skip downloading the FastVLM model"
            echo "  --force                  Force setup even if files exist"
            echo "  -h, --help               Show this help message"
            echo ""
            echo "Example:"
            echo "  $0 --unity-project ~/MyUnityProject --model-size 1.5b"
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Validate Unity project path
if [[ -z "$UNITY_PROJECT_PATH" ]]; then
    print_error "Unity project path is required. Use --unity-project PATH"
    exit 1
fi

if [[ ! -d "$UNITY_PROJECT_PATH" ]]; then
    print_error "Unity project directory does not exist: $UNITY_PROJECT_PATH"
    exit 1
fi

# Check if it's a valid Unity project
if [[ ! -d "$UNITY_PROJECT_PATH/Assets" ]]; then
    print_error "Not a valid Unity project (Assets folder not found): $UNITY_PROJECT_PATH"
    exit 1
fi

# Validate model size
case $MODEL_SIZE in
    0.5b|1.5b|7b)
        ;;
    *)
        print_error "Invalid model size: $MODEL_SIZE. Must be 0.5b, 1.5b, or 7b"
        exit 1
        ;;
esac

print_status "Unity project: $UNITY_PROJECT_PATH"
print_status "Model size: $MODEL_SIZE"

# Step 1: Copy FastVLM Unity Plugin
print_step "1. Copying FastVLM Unity Plugin"

FASTVLM_SOURCE="$SCRIPT_DIR/../FastVLM"
FASTVLM_DEST="$UNITY_PROJECT_PATH/Assets/FastVLM"

if [[ -d "$FASTVLM_DEST" ]] && [[ "$FORCE_SETUP" != true ]]; then
    print_warning "FastVLM plugin already exists at $FASTVLM_DEST"
    read -p "Overwrite? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_status "Skipping plugin copy"
    else
        rm -rf "$FASTVLM_DEST"
        cp -r "$FASTVLM_SOURCE" "$FASTVLM_DEST"
        print_status "FastVLM plugin copied successfully"
    fi
else
    if [[ -d "$FASTVLM_DEST" ]]; then
        rm -rf "$FASTVLM_DEST"
    fi
    cp -r "$FASTVLM_SOURCE" "$FASTVLM_DEST"
    print_status "FastVLM plugin copied successfully"
fi

# Step 2: Create StreamingAssets directory
print_step "2. Setting up StreamingAssets"

STREAMING_ASSETS_DIR="$UNITY_PROJECT_PATH/Assets/StreamingAssets"
FASTVLM_STREAMING_DIR="$STREAMING_ASSETS_DIR/FastVLM"

mkdir -p "$FASTVLM_STREAMING_DIR"
print_status "StreamingAssets/FastVLM directory created"

# Step 3: Download and copy FastVLM model
if [[ "$SKIP_MODEL_DOWNLOAD" != true ]]; then
    print_step "3. Downloading FastVLM Model ($MODEL_SIZE)"
    
    # Check if model already exists
    MODEL_DIR="$FASTVLM_STREAMING_DIR/model"
    if [[ -d "$MODEL_DIR" ]] && [[ "$FORCE_SETUP" != true ]]; then
        print_warning "Model already exists at $MODEL_DIR"
        read -p "Re-download? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            print_status "Skipping model download"
        else
            rm -rf "$MODEL_DIR"
            download_model=true
        fi
    else
        download_model=true
    fi
    
    if [[ "$download_model" == true ]]; then
        # Navigate to FastVLM app directory
        FASTVLM_APP_DIR="$PROJECT_ROOT/ml-fastvlm-unity/app"
        
        if [[ ! -d "$FASTVLM_APP_DIR" ]]; then
            print_error "FastVLM app directory not found: $FASTVLM_APP_DIR"
            exit 1
        fi
        
        cd "$FASTVLM_APP_DIR"
        
        # Make download script executable
        chmod +x get_pretrained_mlx_model.sh
        
        # Download model to temporary location
        TEMP_MODEL_DIR=$(mktemp -d)
        print_status "Downloading FastVLM $MODEL_SIZE model..."
        
        if ./get_pretrained_mlx_model.sh --model "$MODEL_SIZE" --dest "$TEMP_MODEL_DIR"; then
            # Copy model to Unity project
            cp -r "$TEMP_MODEL_DIR" "$MODEL_DIR"
            rm -rf "$TEMP_MODEL_DIR"
            print_status "Model downloaded and copied successfully"
        else
            print_error "Failed to download FastVLM model"
            rm -rf "$TEMP_MODEL_DIR"
            exit 1
        fi
    fi
else
    print_status "Skipping model download as requested"
fi

# Step 4: Verify FastVLM source files
print_step "4. Verifying FastVLM Source Files"

FASTVLM_SOURCE_DIR="$PROJECT_ROOT/ml-fastvlm-unity/app/FastVLM"
if [[ -d "$FASTVLM_SOURCE_DIR" ]]; then
    print_status "FastVLM source files found at $FASTVLM_SOURCE_DIR"
    
    # List key Swift files
    swift_files=$(find "$FASTVLM_SOURCE_DIR" -name "*.swift" | wc -l)
    print_status "Found $swift_files Swift source files"
else
    print_warning "FastVLM source directory not found. The build process may not include native iOS components."
fi

# Step 5: Create example scene setup info
print_step "5. Creating Setup Instructions"

SETUP_INFO_FILE="$UNITY_PROJECT_PATH/FastVLM_iOS_Setup.txt"
cat > "$SETUP_INFO_FILE" << EOF
FastVLM Unity iOS Setup Complete!
================================

Your Unity project has been configured for FastVLM iOS integration.

Setup Summary:
- Plugin installed: Assets/FastVLM/
- Model location: Assets/StreamingAssets/FastVLM/model/
- Model size: $MODEL_SIZE
- Source files: $FASTVLM_SOURCE_DIR

Next Steps:
1. Open your Unity project
2. Create a new scene or open an existing one
3. Add an empty GameObject and attach the FastVLMiOS component
4. Set the Model Path to "FastVLM/model" (relative to StreamingAssets)
5. Optionally add the FastVLMiOSExample component for a complete demo

Build Settings:
- Target Platform: iOS
- Minimum iOS Version: 15.0
- Scripting Backend: IL2CPP
- Architecture: ARM64

The FastVLM iOS build processor will automatically:
- Copy Swift source files to Xcode project
- Configure required frameworks
- Set proper build settings
- Add necessary capabilities to Info.plist

For detailed instructions, see:
- Assets/FastVLM/IOS_SETUP_GUIDE.md
- Assets/FastVLM/README.md

Generated on: $(date)
EOF

print_status "Setup instructions saved to $SETUP_INFO_FILE"

# Step 6: Validate setup
print_step "6. Validating Setup"

# Check plugin files
if [[ -f "$UNITY_PROJECT_PATH/Assets/FastVLM/Scripts/FastVLMiOS.cs" ]]; then
    print_status "✓ FastVLMiOS script found"
else
    print_error "✗ FastVLMiOS script missing"
fi

if [[ -f "$UNITY_PROJECT_PATH/Assets/FastVLM/Plugins/iOS/FastVLMNative.h" ]]; then
    print_status "✓ iOS native plugin headers found"
else
    print_error "✗ iOS native plugin headers missing"
fi

# Check model files
if [[ -d "$UNITY_PROJECT_PATH/Assets/StreamingAssets/FastVLM/model" ]]; then
    model_files=$(find "$UNITY_PROJECT_PATH/Assets/StreamingAssets/FastVLM/model" -type f | wc -l)
    if [[ $model_files -gt 0 ]]; then
        print_status "✓ Model files found ($model_files files)"
    else
        print_warning "⚠ Model directory exists but appears empty"
    fi
else
    print_warning "⚠ Model directory not found"
fi

# Final summary
echo ""
print_status "🎉 FastVLM Unity iOS Setup Complete!"
echo ""
echo -e "${BLUE}Summary:${NC}"
echo "  • Unity Project: $UNITY_PROJECT_PATH"
echo "  • Model Size: $MODEL_SIZE"
echo "  • Plugin Location: Assets/FastVLM/"
echo "  • Model Location: Assets/StreamingAssets/FastVLM/model/"
echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo "  1. Open Unity and load your project"
echo "  2. Follow the instructions in $SETUP_INFO_FILE"
echo "  3. Build for iOS and test on device"
echo ""
echo -e "${BLUE}Documentation:${NC}"
echo "  • iOS Setup Guide: Assets/FastVLM/IOS_SETUP_GUIDE.md"
echo "  • General Documentation: Assets/FastVLM/README.md"
echo ""
print_status "Happy coding with FastVLM! 🚀"
