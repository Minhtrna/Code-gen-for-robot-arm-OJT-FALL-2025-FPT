#pragma once
#include <opencv2/opencv.hpp>
#include <string>
#include <memory>
#include <chrono>
#include <thread>
#include <mutex>

namespace LiveStream {

// Image data format
struct ImageData
{
    int width;
    int height;
    int channels;
    int stride;
    unsigned char* data;
    size_t dataSize;
    
    ImageData() : width(0), height(0), channels(0), stride(0), data(nullptr), dataSize(0) {}
    ~ImageData() { cleanup(); }
    
    void cleanup() {
        if (data) {
            delete[] data;
            data = nullptr;
        }
        dataSize = 0;
    }
};

// Stream configuration
struct StreamConfig
{
    int targetFPS;
    int maxWidth;
    int maxHeight;
    bool enableProcessing;
    int compressionQuality;
    
    StreamConfig() : targetFPS(30), maxWidth(1920), maxHeight(1080), enableProcessing(true), compressionQuality(85) {}
};

class LiveStreamProcessor {
public:
    LiveStreamProcessor();
    ~LiveStreamProcessor();

    // Stream management
    bool initializeStream(const StreamConfig& config);
    void shutdownStream();
    bool isStreamActive() const { return _streamActive; }

    // Live capture interface - works with any camera source
    bool captureFrameFromSource(void* cameraHandle, const std::string& sourceType, ImageData& outImageData);
    
    // Image processing utilities
    bool convertImageFormat(const ImageData& input, ImageData& output, const std::string& targetFormat);
    bool resizeImage(const ImageData& input, ImageData& output, int newWidth, int newHeight);
    
    // Frame rate control
    void setTargetFPS(int fps);
    int getCurrentFPS() const;
    void waitForNextFrame();
    
    // Statistics
    void getStreamStats(int& framesProcessed, float& averageFPS, int& droppedFrames) const;

    // MOVED TO PUBLIC: Memory management methods
    unsigned char* allocateImageBuffer(int width, int height, int channels);
    void freeImageBuffer(unsigned char* buffer);
    int getRequiredBufferSize(int width, int height, int channels) const;

private:
    bool _streamActive;
    StreamConfig _streamConfig;
    std::chrono::steady_clock::time_point _lastFrameTime;
    mutable std::mutex _streamMutex;
    int _framesProcessed;
    int _droppedFrames;
    float _averageFPS;

    // Helper methods (kept private)
    cv::Mat generateVirtualFrame() const;
    void updateStatistics();
};

} // namespace LiveStream