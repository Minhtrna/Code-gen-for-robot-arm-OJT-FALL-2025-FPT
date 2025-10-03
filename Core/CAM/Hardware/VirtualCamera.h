#pragma once
#include "area_scan_3d_camera/Camera.h"
#include "area_scan_3d_camera/api_util.h"
#include <opencv2/opencv.hpp>
#include <string>
#include <memory>
#include <random>
#include <chrono>

namespace VirtualDevice {

class VirtualCamera {
public:
    VirtualCamera();
    ~VirtualCamera();

    // Static method to create virtual camera info
    static std::vector<mmind::eye::CameraInfo> createVirtualCameraInfos();

    // Connection management
    bool connect(const mmind::eye::CameraInfo& info);
    void disconnect();
    bool isConnected() const { return _connected; }

    // Camera information
    mmind::eye::ErrorStatus getCameraInfo(mmind::eye::CameraInfo& info) const;
    mmind::eye::ErrorStatus getCameraStatus(mmind::eye::CameraStatus& status) const;
    mmind::eye::ErrorStatus getCameraIntrinsics(mmind::eye::CameraIntrinsics& intrinsics) const;
    mmind::eye::ErrorStatus getCameraResolutions(mmind::eye::CameraResolutions& resolutions) const;

    // Capture methods
    mmind::eye::ErrorStatus capture2D(mmind::eye::Frame2D& frame2D) const;
    mmind::eye::ErrorStatus capture3D(mmind::eye::Frame3D& frame3D) const;
    mmind::eye::ErrorStatus capture2DAnd3D(mmind::eye::Frame2DAnd3D& frame2DAnd3D) const;

    // Point cloud unit management
    mmind::eye::ErrorStatus setPointCloudUnit(mmind::eye::CoordinateUnit unit);
    mmind::eye::ErrorStatus getPointCloudUnit(mmind::eye::CoordinateUnit& unit) const;

private:
    bool _connected;
    mmind::eye::CameraInfo _cameraInfo;
    mmind::eye::CoordinateUnit _pointCloudUnit;
    mutable std::mt19937 _randomGenerator;

    // Helper methods
    cv::Mat generateSampleColorImage() const;
    cv::Mat generateSampleGrayImage() const;
    cv::Mat generateSampleDepthMap() const;
    std::vector<cv::Point3f> generateSamplePointCloud() const;
    void simulateProcessingDelay(int minMs, int maxMs) const;
};

} // namespace VirtualDevice