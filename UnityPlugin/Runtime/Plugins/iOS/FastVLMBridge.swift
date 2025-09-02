//
// FastVLMBridge.swift
// Swift bridge for Unity FastVLM plugin
//

import Foundation
import UIKit
import CoreImage

@objc public class FastVLMBridge: NSObject {
    
    @objc public static let shared = FastVLMBridge()
    
    private var fastVLMModel: FastVLMModel?
    private var isInitialized = false
    private var currentInferenceTask: Task<Void, Never>?
    
    private override init() {
        super.init()
    }
    
    @objc public func initialize(callback: @escaping () -> Void) {
        if !isInitialized {
            fastVLMModel = FastVLMModel()
            isInitialized = true
        }
        callback()
    }
    
    @objc public func loadModel(_ modelType: Int, progressCallback: @escaping (Float) -> Void) {
        guard let model = fastVLMModel else {
            progressCallback(0.0)
            return
        }
        
        Task {
            // Start loading the model
            await model.load()
            
            // Simulate progress updates (the actual FastVLM model doesn't expose progress)
            await MainActor.run {
                progressCallback(1.0)
            }
        }
    }
    
    @objc public func setTemperature(_ temperature: Float, maxTokens: Int) {
        guard let model = fastVLMModel else { return }
        
        // Update generation parameters
        model.generateParameters.temperature = Double(temperature)
        // Note: maxTokens is handled in the FastVLMModel internally
    }
    
    @objc public func inferAsync(imageData: Data, width: Int, height: Int, prompt: String, callback: @escaping (String) -> Void) {
        guard let model = fastVLMModel else {
            callback("Error: Model not initialized")
            return
        }
        
        // Cancel any existing inference
        currentInferenceTask?.cancel()
        
        // Convert raw image data to UIImage
        guard let image = createUIImage(from: imageData, width: width, height: height) else {
            callback("Error: Failed to create image from data")
            return
        }
        
        // Create UserInput for FastVLM
        let userInput = UserInput(text: prompt, images: [image])
        
        // Start inference
        currentInferenceTask = Task {
            let task = await model.generate(userInput)
            await task.value
            
            // Get the result
            await MainActor.run {
                if !Task.isCancelled {
                    callback(model.output)
                }
            }
        }
    }
    
    @objc public func cancel() {
        currentInferenceTask?.cancel()
        currentInferenceTask = nil
        fastVLMModel?.cancel()
    }
    
    @objc public func isModelLoaded() -> Bool {
        guard let model = fastVLMModel else { return false }
        return !model.modelInfo.isEmpty && model.modelInfo != "Downloading"
    }
    
    @objc public func isInferenceRunning() -> Bool {
        guard let model = fastVLMModel else { return false }
        return model.running
    }
    
    // Helper function to create UIImage from raw RGBA data
    private func createUIImage(from data: Data, width: Int, height: Int) -> UIImage? {
        let bytesPerPixel = 4
        let bytesPerRow = width * bytesPerPixel
        
        guard data.count == width * height * bytesPerPixel else {
            return nil
        }
        
        let colorSpace = CGColorSpaceCreateDeviceRGB()
        let bitmapInfo = CGBitmapInfo(rawValue: CGImageAlphaInfo.premultipliedLast.rawValue)
        
        guard let context = CGContext(
            data: UnsafeMutablePointer(mutating: data.withUnsafeBytes { $0.bindMemory(to: UInt8.self).baseAddress }),
            width: width,
            height: height,
            bitsPerComponent: 8,
            bytesPerRow: bytesPerRow,
            space: colorSpace,
            bitmapInfo: bitmapInfo.rawValue
        ) else {
            return nil
        }
        
        guard let cgImage = context.makeImage() else {
            return nil
        }
        
        return UIImage(cgImage: cgImage)
    }
}

// Make sure the FastVLM classes are available
// Note: This assumes the FastVLM framework is properly linked
extension FastVLMBridge {
    // Add any additional FastVLM specific extensions here
}
