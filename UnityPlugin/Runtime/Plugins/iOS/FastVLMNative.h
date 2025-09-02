//
// FastVLMNative.h
// Unity iOS plugin bridge for FastVLM
//

#ifndef FastVLMNative_h
#define FastVLMNative_h

#ifdef __cplusplus
extern "C" {
#endif

// Callback function types
typedef void (*LoadProgressCallback)(float progress);
typedef void (*InferenceCallback)(const char* result);

// Plugin interface
void FastVLM_Initialize(void);
void FastVLM_LoadModel(int modelType, LoadProgressCallback progressCallback);
void FastVLM_SetGenerationParameters(float temperature, int maxTokens);
void FastVLM_InferAsync(unsigned char* imageData, int width, int height, const char* prompt, InferenceCallback callback);
void FastVLM_Cancel(void);
bool FastVLM_IsModelLoaded(void);
bool FastVLM_IsInferenceRunning(void);

#ifdef __cplusplus
}
#endif

#endif /* FastVLMNative_h */
