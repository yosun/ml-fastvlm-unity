#!/usr/bin/env python3
"""
FastVLM Unity Backend Server
A Flask-based HTTP server that provides REST API endpoints for FastVLM inference.
"""

import os
import sys
import argparse
import base64
import time
import tempfile
from io import BytesIO
from typing import Optional, Dict, Any

import torch
from PIL import Image
import numpy as np
from flask import Flask, request, jsonify
from flask_cors import CORS

# Add the parent directory to path to import FastVLM modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'ml-fastvlm-unity'))

from llava.utils import disable_torch_init
from llava.conversation import conv_templates
from llava.model.builder import load_pretrained_model
from llava.mm_utils import tokenizer_image_token, process_images, get_model_name_from_path
from llava.constants import IMAGE_TOKEN_INDEX, DEFAULT_IMAGE_TOKEN, DEFAULT_IM_START_TOKEN, DEFAULT_IM_END_TOKEN

app = Flask(__name__)
CORS(app)  # Enable CORS for Unity communication

# Global variables for model
model = None
tokenizer = None
image_processor = None
context_len = None
model_config = None

class FastVLMServer:
    def __init__(self, model_path: str, model_base: Optional[str] = None, conv_mode: str = "qwen_2"):
        self.model_path = model_path
        self.model_base = model_base
        self.conv_mode = conv_mode
        self.device = "mps" if torch.backends.mps.is_available() else "cuda" if torch.cuda.is_available() else "cpu"
        self.model_loaded = False
        
    def load_model(self):
        """Load the FastVLM model"""
        global model, tokenizer, image_processor, context_len, model_config
        
        try:
            print(f"Loading FastVLM model from: {self.model_path}")
            print(f"Using device: {self.device}")
            
            # Remove generation config from model folder to read generation parameters from args
            model_path = os.path.expanduser(self.model_path)
            generation_config = None
            if os.path.exists(os.path.join(model_path, 'generation_config.json')):
                generation_config = os.path.join(model_path, '.generation_config.json')
                os.rename(os.path.join(model_path, 'generation_config.json'), generation_config)

            # Load model
            disable_torch_init()
            model_name = get_model_name_from_path(model_path)
            tokenizer, model, image_processor, context_len = load_pretrained_model(
                model_path, self.model_base, model_name, device=self.device
            )
            
            # Set the pad token id for generation
            if hasattr(model, 'generation_config'):
                model.generation_config.pad_token_id = tokenizer.pad_token_id
            
            model_config = model.config
            
            # Restore generation config
            if generation_config is not None:
                os.rename(generation_config, os.path.join(model_path, 'generation_config.json'))
            
            self.model_loaded = True
            print("FastVLM model loaded successfully!")
            
        except Exception as e:
            print(f"Error loading model: {str(e)}")
            raise e
    
    def infer(self, prompt: str, image_base64: str, **kwargs) -> Dict[str, Any]:
        """Perform inference with FastVLM"""
        if not self.model_loaded:
            return {"success": False, "error": "Model not loaded"}
        
        try:
            start_time = time.time()
            
            # Decode base64 image
            image_data = base64.b64decode(image_base64)
            image = Image.open(BytesIO(image_data)).convert('RGB')
            
            # Get generation parameters
            temperature = kwargs.get('temperature', 0.2)
            top_p = kwargs.get('top_p', 0.9)
            num_beams = kwargs.get('num_beams', 1)
            max_tokens = kwargs.get('max_tokens', 256)
            
            # Construct prompt
            qs = prompt
            if model_config.mm_use_im_start_end:
                qs = DEFAULT_IM_START_TOKEN + DEFAULT_IMAGE_TOKEN + DEFAULT_IM_END_TOKEN + '\n' + qs
            else:
                qs = DEFAULT_IMAGE_TOKEN + '\n' + qs
            
            conv = conv_templates[self.conv_mode].copy()
            conv.append_message(conv.roles[0], qs)
            conv.append_message(conv.roles[1], None)
            full_prompt = conv.get_prompt()
            
            # Tokenize prompt
            input_ids = tokenizer_image_token(
                full_prompt, tokenizer, IMAGE_TOKEN_INDEX, return_tensors='pt'
            ).unsqueeze(0).to(torch.device(self.device))
            
            # Process image
            image_tensor = process_images([image], image_processor, model_config)[0]
            
            # Run inference
            with torch.inference_mode():
                output_ids = model.generate(
                    input_ids,
                    images=image_tensor.unsqueeze(0).half(),
                    image_sizes=[image.size],
                    do_sample=True if temperature > 0 else False,
                    temperature=temperature,
                    top_p=top_p,
                    num_beams=num_beams,
                    max_new_tokens=max_tokens,
                    use_cache=True
                )
                
                outputs = tokenizer.batch_decode(output_ids, skip_special_tokens=True)[0].strip()
            
            inference_time = time.time() - start_time
            
            return {
                "success": True,
                "result": outputs,
                "inference_time": inference_time,
                "error": None
            }
            
        except Exception as e:
            return {
                "success": False,
                "result": None,
                "inference_time": 0,
                "error": str(e)
            }

# Global server instance
vlm_server = None

@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    global vlm_server
    
    status = {
        "status": "healthy" if vlm_server and vlm_server.model_loaded else "unhealthy",
        "model_loaded": vlm_server.model_loaded if vlm_server else False,
        "device": vlm_server.device if vlm_server else "unknown"
    }
    
    return jsonify(status)

@app.route('/infer', methods=['POST'])
def infer():
    """Inference endpoint"""
    global vlm_server
    
    if not vlm_server or not vlm_server.model_loaded:
        return jsonify({
            "success": False,
            "error": "Model not loaded",
            "result": None,
            "inference_time": 0
        }), 500
    
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({
                "success": False,
                "error": "No JSON data provided",
                "result": None,
                "inference_time": 0
            }), 400
        
        prompt = data.get('prompt', '')
        image_base64 = data.get('image_base64', '')
        
        if not prompt:
            return jsonify({
                "success": False,
                "error": "Prompt is required",
                "result": None,
                "inference_time": 0
            }), 400
        
        if not image_base64:
            return jsonify({
                "success": False,
                "error": "Image data is required",
                "result": None,
                "inference_time": 0
            }), 400
        
        # Get optional parameters
        kwargs = {
            'temperature': data.get('temperature', 0.2),
            'top_p': data.get('top_p', 0.9),
            'num_beams': data.get('num_beams', 1),
            'max_tokens': data.get('max_tokens', 256)
        }
        
        # Perform inference
        result = vlm_server.infer(prompt, image_base64, **kwargs)
        
        return jsonify(result)
        
    except Exception as e:
        return jsonify({
            "success": False,
            "error": f"Server error: {str(e)}",
            "result": None,
            "inference_time": 0
        }), 500

@app.route('/config', methods=['GET'])
def get_config():
    """Get server configuration"""
    global vlm_server
    
    if not vlm_server:
        return jsonify({"error": "Server not initialized"}), 500
    
    config = {
        "model_path": vlm_server.model_path,
        "model_base": vlm_server.model_base,
        "conv_mode": vlm_server.conv_mode,
        "device": vlm_server.device,
        "model_loaded": vlm_server.model_loaded
    }
    
    return jsonify(config)

def main():
    parser = argparse.ArgumentParser(description="FastVLM Unity Backend Server")
    parser.add_argument("--model-path", type=str, required=True,
                        help="Path to the FastVLM model directory")
    parser.add_argument("--model-base", type=str, default=None,
                        help="Base model path (if applicable)")
    parser.add_argument("--conv-mode", type=str, default="qwen_2",
                        help="Conversation mode (default: qwen_2)")
    parser.add_argument("--host", type=str, default="localhost",
                        help="Server host (default: localhost)")
    parser.add_argument("--port", type=int, default=8000,
                        help="Server port (default: 8000)")
    parser.add_argument("--debug", action="store_true",
                        help="Run in debug mode")
    
    args = parser.parse_args()
    
    # Validate model path
    if not os.path.exists(args.model_path):
        print(f"Error: Model path does not exist: {args.model_path}")
        sys.exit(1)
    
    # Initialize server
    global vlm_server
    vlm_server = FastVLMServer(args.model_path, args.model_base, args.conv_mode)
    
    print("Initializing FastVLM Unity Backend Server...")
    print(f"Model path: {args.model_path}")
    print(f"Host: {args.host}")
    print(f"Port: {args.port}")
    
    try:
        # Load model
        vlm_server.load_model()
        
        # Start server
        print(f"\nStarting server at http://{args.host}:{args.port}")
        print("Endpoints:")
        print(f"  - Health: http://{args.host}:{args.port}/health")
        print(f"  - Inference: http://{args.host}:{args.port}/infer")
        print(f"  - Config: http://{args.host}:{args.port}/config")
        print("\nPress Ctrl+C to stop the server")
        
        app.run(host=args.host, port=args.port, debug=args.debug, threaded=True)
        
    except KeyboardInterrupt:
        print("\nServer stopped by user")
    except Exception as e:
        print(f"Error starting server: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main()
