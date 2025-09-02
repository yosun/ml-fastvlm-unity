#!/bin/bash

# FastVLM Unity Backend Setup Script
# This script sets up the Python environment and dependencies for the FastVLM Unity backend

set -e  # Exit on any error

echo "Setting up FastVLM Unity Backend..."

# Get the directory of this script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
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

# Check if Python 3 is installed
if ! command -v python3 &> /dev/null; then
    print_error "Python 3 is not installed. Please install Python 3.8 or later."
    exit 1
fi

# Check Python version
python_version=$(python3 -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')")
print_status "Found Python $python_version"

# Check if we're in a virtual environment
if [[ -z "${VIRTUAL_ENV}" ]]; then
    print_warning "Not in a virtual environment. Consider using conda or venv."
    read -p "Do you want to continue anyway? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_status "Exiting. Please activate your virtual environment first."
        exit 1
    fi
fi

# Install Python dependencies
print_status "Installing Python dependencies..."
cd "$SCRIPT_DIR"

if ! pip3 install -r requirements.txt; then
    print_error "Failed to install Python dependencies"
    exit 1
fi

# Check if FastVLM model directory exists
FASTVLM_DIR="$PROJECT_ROOT/ml-fastvlm-unity"
if [[ ! -d "$FASTVLM_DIR" ]]; then
    print_error "FastVLM directory not found at: $FASTVLM_DIR"
    print_error "Please ensure the ml-fastvlm-unity directory is present"
    exit 1
fi

# Install FastVLM dependencies
print_status "Installing FastVLM dependencies..."
cd "$FASTVLM_DIR"

if [[ -f "pyproject.toml" ]]; then
    if ! pip3 install -e .; then
        print_error "Failed to install FastVLM"
        exit 1
    fi
else
    print_warning "pyproject.toml not found. Installing common dependencies..."
    # Install common LLaVA dependencies
    pip3 install git+https://github.com/haotian-liu/LLaVA.git || true
fi

# Create a simple test script
print_status "Creating test script..."
cat > "$SCRIPT_DIR/test_server.py" << 'EOF'
#!/usr/bin/env python3
"""
Simple test script for FastVLM Unity Backend
"""
import sys
import os
import requests
import base64
import json

def test_server(host="localhost", port=8000):
    """Test the FastVLM server"""
    base_url = f"http://{host}:{port}"
    
    print(f"Testing FastVLM server at {base_url}")
    
    try:
        # Test health endpoint
        print("Testing health endpoint...")
        response = requests.get(f"{base_url}/health", timeout=5)
        if response.status_code == 200:
            health_data = response.json()
            print(f"Health check: {health_data}")
        else:
            print(f"Health check failed: {response.status_code}")
            return False
            
        # Test config endpoint
        print("Testing config endpoint...")
        response = requests.get(f"{base_url}/config", timeout=5)
        if response.status_code == 200:
            config_data = response.json()
            print(f"Server config: {config_data}")
        else:
            print(f"Config check failed: {response.status_code}")
            
        return True
        
    except requests.exceptions.ConnectionError:
        print(f"Cannot connect to server at {base_url}")
        print("Make sure the server is running with: python fastvlm_server.py --model-path /path/to/model")
        return False
    except Exception as e:
        print(f"Test failed: {e}")
        return False

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="localhost", help="Server host")
    parser.add_argument("--port", default=8000, type=int, help="Server port")
    args = parser.parse_args()
    
    success = test_server(args.host, args.port)
    sys.exit(0 if success else 1)
EOF

chmod +x "$SCRIPT_DIR/test_server.py"

# Create start script
print_status "Creating start script..."
cat > "$SCRIPT_DIR/start_server.sh" << 'EOF'
#!/bin/bash

# Start FastVLM Unity Backend Server
# Usage: ./start_server.sh [model_path]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Default model path (update this to your model location)
DEFAULT_MODEL_PATH="$HOME/fastvlm_models/llava-fastvithd_0.5b_stage3"

MODEL_PATH="${1:-$DEFAULT_MODEL_PATH}"

if [[ ! -d "$MODEL_PATH" ]]; then
    echo "Error: Model path does not exist: $MODEL_PATH"
    echo "Usage: $0 [model_path]"
    echo "Please download a FastVLM model first using get_models.sh"
    exit 1
fi

echo "Starting FastVLM Unity Backend Server..."
echo "Model path: $MODEL_PATH"
echo "Server will be available at: http://localhost:8000"
echo "Press Ctrl+C to stop"

cd "$SCRIPT_DIR"
python3 fastvlm_server.py \
    --model-path "$MODEL_PATH" \
    --host localhost \
    --port 8000 \
    --debug
EOF

chmod +x "$SCRIPT_DIR/start_server.sh"

print_status "Setup complete!"
print_status ""
print_status "Next steps:"
print_status "1. Download a FastVLM model (see ml-fastvlm-unity/README.md)"
print_status "2. Start the server: ./start_server.sh /path/to/your/model"
print_status "3. Test the server: python3 test_server.py"
print_status "4. Use the Unity plugin to connect to the server"
print_status ""
print_status "Server will be available at: http://localhost:8000"
