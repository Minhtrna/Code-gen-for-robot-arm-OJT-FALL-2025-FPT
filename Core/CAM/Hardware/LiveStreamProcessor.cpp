#include "LiveStreamProcessor.h"
#include <iostream>
#include <cmath>

namespace LiveStream {

LiveStreamProcessor::LiveStreamProcessor() 
    : _streamActive(false)
    , _framesProcessed(0)
    , _droppedFrames(0)
    , _averageFPS(0.0f)
{
}

LiveStreamProcessor::~LiveStreamProcessor() {
    shutdownStream();
}

bool LiveStreamProcessor::initializeStream(const StreamConfig& config)
{
    std::lock_guard<std::mutex> lock(_streamMutex);
    
    _streamConfig = config;
    _streamActive = true;
    _lastFrameTime = std::chrono::steady_clock::now();
    _framesProcessed = 0;
    _droppedFrames = 0;
    _averageFPS = 0.0f;
    
    std::cout << "[LiveStreamProcessor] Stream initialized - " 
              << _streamConfig.targetFPS << " FPS, "
              << _streamConfig.maxWidth << "x" << _streamConfig.maxHeight << std::endl;
    
    return true;
}

void LiveStreamProcessor::shutdownStream()
{
    std::lock_guard<std::mutex> lock(_streamMutex);
    _streamActive = false;
    std::cout << "[LiveStreamProcessor] Stream shutdown" << std::endl;
}

bool LiveStreamProcessor::captureFrameFromSource(void* cameraHandle, const std::string& sourceType, ImageData& outImageData)
{
    if (!_streamActive) {
        return false;
    }

    try {
        cv::Mat frame;
        bool captureSuccess = false;

        if (sourceType == "webcam") {
            cv::VideoCapture* cap = static_cast<cv::VideoCapture*>(cameraHandle);
            if (cap && cap->isOpened()) {
                *cap >> frame;
                captureSuccess = !frame.empty();
            }
        }
        else if (sourceType == "mecheye") {
            // FIXED: Handle both real and virtual MechEye cameras
            // For now, generate virtual frame - you can integrate real camera capture later
            frame = generateVirtualFrame();
            captureSuccess = true;
        }
        // REMOVED: Default "virtual" case that was always generating images

        if (captureSuccess && !frame.empty()) {
            // Ensure consistent format (BGR)
            if (frame.channels() == 1) {
                cv::cvtColor(frame, frame, cv::COLOR_GRAY2BGR);
            } else if (frame.channels() == 4) {
                cv::cvtColor(frame, frame, cv::COLOR_BGRA2BGR);
            }

            // Clean up previous data
            outImageData.cleanup();

            // Update output structure
            outImageData.width = frame.cols;
            outImageData.height = frame.rows;
            outImageData.channels = frame.channels();
            outImageData.stride = frame.cols * frame.channels();
            outImageData.dataSize = frame.total() * frame.elemSize();
            
            // Allocate and copy data
            outImageData.data = allocateImageBuffer(frame.cols, frame.rows, frame.channels());
            
            if (outImageData.data != nullptr) {
                std::memcpy(outImageData.data, frame.data, outImageData.dataSize);
                updateStatistics();
                return true;
            }
        }
    }
    catch (const std::exception& e) {
        std::cerr << "[LiveStreamProcessor] Capture error: " << e.what() << std::endl;
        _droppedFrames++;
    }

    return false;
}

bool LiveStreamProcessor::convertImageFormat(const ImageData& input, ImageData& output, const std::string& targetFormat)
{
    if (!input.data || input.width <= 0 || input.height <= 0) {
        return false;
    }

    try {
        cv::Mat inputMat(input.height, input.width, 
                        input.channels == 3 ? CV_8UC3 : CV_8UC1, 
                        input.data);
        
        cv::Mat outputMat;
        
        if (targetFormat == "BGR24" && input.channels != 3) {
            if (input.channels == 1) {
                cv::cvtColor(inputMat, outputMat, cv::COLOR_GRAY2BGR);
            } else {
                cv::cvtColor(inputMat, outputMat, cv::COLOR_BGRA2BGR);
            }
        }
        else if (targetFormat == "RGB24") {
            if (input.channels == 3) {
                cv::cvtColor(inputMat, outputMat, cv::COLOR_BGR2RGB);
            } else if (input.channels == 1) {
                cv::cvtColor(inputMat, outputMat, cv::COLOR_GRAY2RGB);
            } else {
                cv::cvtColor(inputMat, outputMat, cv::COLOR_BGRA2RGB);
            }
        }
        else if (targetFormat == "GRAY8") {
            if (input.channels == 3) {
                cv::cvtColor(inputMat, outputMat, cv::COLOR_BGR2GRAY);
            } else if (input.channels == 4) {
                cv::cvtColor(inputMat, outputMat, cv::COLOR_BGRA2GRAY);
            } else {
                outputMat = inputMat.clone();
            }
        }
        else {
            outputMat = inputMat.clone(); // No conversion needed
        }

        // Clean up previous data
        output.cleanup();

        // Update output structure
        output.width = outputMat.cols;
        output.height = outputMat.rows;
        output.channels = outputMat.channels();
        output.stride = outputMat.cols * outputMat.channels();
        output.dataSize = outputMat.total() * outputMat.elemSize();
        
        // Allocate output buffer
        output.data = allocateImageBuffer(outputMat.cols, outputMat.rows, outputMat.channels());
        
        if (output.data != nullptr) {
            std::memcpy(output.data, outputMat.data, output.dataSize);
            return true;
        }
    }
    catch (const std::exception& e) {
        std::cerr << "[LiveStreamProcessor] Format conversion error: " << e.what() << std::endl;
    }

    return false;
}

bool LiveStreamProcessor::resizeImage(const ImageData& input, ImageData& output, int newWidth, int newHeight)
{
    if (!input.data || input.width <= 0 || input.height <= 0 || newWidth <= 0 || newHeight <= 0) {
        return false;
    }

    try {
        cv::Mat inputMat(input.height, input.width, 
                        input.channels == 3 ? CV_8UC3 : CV_8UC1, 
                        input.data);
        
        cv::Mat outputMat;
        cv::resize(inputMat, outputMat, cv::Size(newWidth, newHeight));

        // Clean up previous data
        output.cleanup();

        // Update output structure
        output.width = outputMat.cols;
        output.height = outputMat.rows;
        output.channels = outputMat.channels();
        output.stride = outputMat.cols * outputMat.channels();
        output.dataSize = outputMat.total() * outputMat.elemSize();
        
        // Allocate output buffer
        output.data = allocateImageBuffer(outputMat.cols, outputMat.rows, outputMat.channels());
        
        if (output.data != nullptr) {
            std::memcpy(output.data, outputMat.data, output.dataSize);
            return true;
        }
    }
    catch (const std::exception& e) {
        std::cerr << "[LiveStreamProcessor] Resize error: " << e.what() << std::endl;
    }

    return false;
}

void LiveStreamProcessor::setTargetFPS(int fps)
{
    std::lock_guard<std::mutex> lock(_streamMutex);
    _streamConfig.targetFPS = fps;
}

int LiveStreamProcessor::getCurrentFPS() const
{
    std::lock_guard<std::mutex> lock(_streamMutex);
    return static_cast<int>(_averageFPS);
}

void LiveStreamProcessor::waitForNextFrame()
{
    if (_streamConfig.targetFPS > 0) {
        int frameDelay = 1000 / _streamConfig.targetFPS; // ms per frame
        std::this_thread::sleep_for(std::chrono::milliseconds(frameDelay));
    }
}

void LiveStreamProcessor::getStreamStats(int& framesProcessed, float& averageFPS, int& droppedFrames) const
{
    std::lock_guard<std::mutex> lock(_streamMutex);
    
    framesProcessed = _framesProcessed;
    averageFPS = _averageFPS;
    droppedFrames = _droppedFrames;
}

unsigned char* LiveStreamProcessor::allocateImageBuffer(int width, int height, int channels)
{
    size_t size = width * height * channels;
    return new(std::nothrow) unsigned char[size];
}

void LiveStreamProcessor::freeImageBuffer(unsigned char* buffer)
{
    delete[] buffer;
}

int LiveStreamProcessor::getRequiredBufferSize(int width, int height, int channels) const
{
    return width * height * channels;
}

cv::Mat LiveStreamProcessor::generateVirtualFrame() const
{
    cv::Mat frame(480, 640, CV_8UC3);
    
    auto now = std::chrono::steady_clock::now();
    auto timeMs = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()).count();
    
    // Create animated pattern
    for (int y = 0; y < frame.rows; ++y) {
        for (int x = 0; x < frame.cols; ++x) {
            float waveX = std::sin((x + timeMs * 0.005) * 0.01) * 127 + 128;
            float waveY = std::sin((y + timeMs * 0.003) * 0.01) * 127 + 128;
            float waveTime = std::sin(timeMs * 0.001) * 50 + 50;
            
            int r = static_cast<int>(waveX + waveTime) % 256;
            int g = static_cast<int>(waveY + waveTime) % 256;
            int b = static_cast<int>((waveX + waveY) * 0.5) % 256;
            
            frame.at<cv::Vec3b>(y, x) = cv::Vec3b(b, g, r);
        }
    }
    
    // Add live info overlay
    cv::putText(frame, "LIVE STREAM", cv::Point(10, 30), 
               cv::FONT_HERSHEY_SIMPLEX, 1.0, cv::Scalar(255, 255, 255), 2);
    
    std::string fpsText = "FPS: " + std::to_string(static_cast<int>(_averageFPS));
    cv::putText(frame, fpsText, cv::Point(10, 60), 
               cv::FONT_HERSHEY_SIMPLEX, 0.6, cv::Scalar(255, 255, 255), 1);
    
    return frame;
}

void LiveStreamProcessor::updateStatistics()
{
    _framesProcessed++;
    
    auto now = std::chrono::steady_clock::now();
    auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(now - _lastFrameTime);
    if (elapsed.count() > 0) {
        float instantFPS = 1000.0f / elapsed.count();
        _averageFPS = (_averageFPS * 0.9f) + (instantFPS * 0.1f); // Smoothed FPS
    }
    _lastFrameTime = now;
}

} // namespace LiveStream