//
// FastVLMNative.mm
// Unity iOS plugin bridge for FastVLM
//

#import "FastVLMNative.h"
#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

// Forward declarations for Swift interop
@interface FastVLMBridge : NSObject
+ (instancetype)shared;
- (void)initializeWithCallback:(void(^)(void))callback;
- (void)loadModel:(int)modelType progressCallback:(void(^)(float))progressCallback;
- (void)setTemperature:(float)temperature maxTokens:(int)maxTokens;
- (void)inferAsyncWithImageData:(NSData*)imageData width:(int)width height:(int)height prompt:(NSString*)prompt callback:(void(^)(NSString*))callback;
- (void)cancel;
- (BOOL)isModelLoaded;
- (BOOL)isInferenceRunning;
@end

// Global instance
static FastVLMBridge* g_fastVLMBridge = nil;

// Global callback storage
static LoadProgressCallback g_loadProgressCallback = nil;
static InferenceCallback g_inferenceCallback = nil;

#ifdef __cplusplus
extern "C" {
#endif

void FastVLM_Initialize(void) {
    if (g_fastVLMBridge == nil) {
        g_fastVLMBridge = [FastVLMBridge shared];
        [g_fastVLMBridge initializeWithCallback:^{
            // Initialization complete
        }];
    }
}

void FastVLM_LoadModel(int modelType, LoadProgressCallback progressCallback) {
    if (g_fastVLMBridge == nil) {
        FastVLM_Initialize();
    }
    
    g_loadProgressCallback = progressCallback;
    
    [g_fastVLMBridge loadModel:modelType progressCallback:^(float progress) {
        if (g_loadProgressCallback != nil) {
            g_loadProgressCallback(progress);
        }
    }];
}

void FastVLM_SetGenerationParameters(float temperature, int maxTokens) {
    if (g_fastVLMBridge != nil) {
        [g_fastVLMBridge setTemperature:temperature maxTokens:maxTokens];
    }
}

void FastVLM_InferAsync(unsigned char* imageData, int width, int height, const char* prompt, InferenceCallback callback) {
    if (g_fastVLMBridge == nil) {
        if (callback != nil) {
            callback("Error: FastVLM not initialized");
        }
        return;
    }
    
    g_inferenceCallback = callback;
    
    // Convert raw image data to NSData
    NSData* data = [NSData dataWithBytes:imageData length:width * height * 4];
    NSString* promptStr = [NSString stringWithUTF8String:prompt];
    
    [g_fastVLMBridge inferAsyncWithImageData:data width:width height:height prompt:promptStr callback:^(NSString* result) {
        if (g_inferenceCallback != nil) {
            const char* cResult = [result UTF8String];
            g_inferenceCallback(cResult);
        }
    }];
}

void FastVLM_Cancel(void) {
    if (g_fastVLMBridge != nil) {
        [g_fastVLMBridge cancel];
    }
}

bool FastVLM_IsModelLoaded(void) {
    if (g_fastVLMBridge != nil) {
        return [g_fastVLMBridge isModelLoaded];
    }
    return false;
}

bool FastVLM_IsInferenceRunning(void) {
    if (g_fastVLMBridge != nil) {
        return [g_fastVLMBridge isInferenceRunning];
    }
    return false;
}

#ifdef __cplusplus
}
#endif
