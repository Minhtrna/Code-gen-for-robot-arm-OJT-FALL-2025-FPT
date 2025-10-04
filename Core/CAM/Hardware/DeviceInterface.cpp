#include "DeviceInterface.h"
#include "VirtualCamera.h"
#include <opencv2/opencv.hpp>
#include "area_scan_3d_camera/Camera.h"
#include "area_scan_3d_camera/api_util.h"
#include <iostream>
#include <vector>
#include <chrono>

// Global variables for device management - LAZY INITIALIZATION
static std::vector<mmind::eye::CameraInfo>* mechEyeDevices = nullptr;
static std::vector<mmind::eye::CameraInfo>* virtualCameraDevices = nullptr;
static std::vector<int>* webcamDevices = nullptr;
static mmind::eye::Camera* currentMechEyeCamera = nullptr;
static VirtualDevice::VirtualCamera* currentVirtualCamera = nullptr;
static cv::VideoCapture* currentWebcam = nullptr;
static bool mechEyeConnected = false;
static bool virtualCameraConnected = false;
static bool webcamConnected = false;
static int connectedWebcamIndex = -1;

// Lazy initialization functions
static std::vector<mmind::eye::CameraInfo>& GetMechEyeDevices() {
    if (!mechEyeDevices) {
        mechEyeDevices = new std::vector<mmind::eye::CameraInfo>();
    }
    return *mechEyeDevices;
}

static std::vector<mmind::eye::CameraInfo>& GetVirtualCameraDevices() {
    if (!virtualCameraDevices) {
        virtualCameraDevices = new std::vector<mmind::eye::CameraInfo>();
    }
    return *virtualCameraDevices;
}

static std::vector<int>& GetWebcamDevices() {
    if (!webcamDevices) {
        webcamDevices = new std::vector<int>();
    }
    return *webcamDevices;
}

static mmind::eye::Camera& GetCurrentMechEyeCamera() {
    if (!currentMechEyeCamera) {
        currentMechEyeCamera = new mmind::eye::Camera();
    }
    return *currentMechEyeCamera;
}

static VirtualDevice::VirtualCamera& GetCurrentVirtualCamera() {
    if (!currentVirtualCamera) {
        currentVirtualCamera = new VirtualDevice::VirtualCamera();
    }
    return *currentVirtualCamera;
}

static cv::VideoCapture& GetCurrentWebcam() {
    if (!currentWebcam) {
        currentWebcam = new cv::VideoCapture();
    }
    return *currentWebcam;
}

extern "C" {

    DEVICEINTERFACE_API int __cdecl DiscoverMechEyeCameras()
    {
        try
        {
            std::cout << "Discovering Mech-Eye cameras..." << std::endl;

            // Discover real cameras
            GetMechEyeDevices() = mmind::eye::Camera::discoverCameras(2000);

            // Add virtual cameras
            GetVirtualCameraDevices() = VirtualDevice::VirtualCamera::createVirtualCameraInfos();

            // Combine real and virtual cameras
            auto& realCameras = GetMechEyeDevices();
            auto& virtualCameras = GetVirtualCameraDevices();

            realCameras.insert(realCameras.end(), virtualCameras.begin(), virtualCameras.end());

            std::cout << "Found " << GetMechEyeDevices().size() << " Mech-Eye devices ("
                << (GetMechEyeDevices().size() - virtualCameras.size()) << " real, "
                << virtualCameras.size() << " virtual)" << std::endl;

            return static_cast<int>(GetMechEyeDevices().size());
        }
        catch (const std::exception& e)
        {
            std::cerr << "Error discovering Mech-Eye cameras: " << e.what() << std::endl;
            return 0;
        }
    }

    DEVICEINTERFACE_API bool __cdecl GetMechEyeDeviceInfo(int index, DeviceInfo* deviceInfo)
    {
        auto& devices = GetMechEyeDevices();
        if (index < 0 || index >= static_cast<int>(devices.size()) || deviceInfo == nullptr)
            return false;

        const auto& info = devices[index];

        strncpy(deviceInfo->name, info.deviceName.c_str(), sizeof(deviceInfo->name) - 1);
        deviceInfo->name[sizeof(deviceInfo->name) - 1] = '\0';

        // Check if this is a virtual device
        bool isVirtual = info.serialNumber.find("VRT") == 0;
        std::string deviceType = isVirtual ? "Virtual Mech-Eye Camera" : "Mech-Eye Camera";

        strncpy(deviceInfo->type, deviceType.c_str(), sizeof(deviceInfo->type) - 1);
        deviceInfo->type[sizeof(deviceInfo->type) - 1] = '\0';

        strncpy(deviceInfo->serialNumber, info.serialNumber.c_str(), sizeof(deviceInfo->serialNumber) - 1);
        deviceInfo->serialNumber[sizeof(deviceInfo->serialNumber) - 1] = '\0';

        strncpy(deviceInfo->ipAddress, info.ipAddress.c_str(), sizeof(deviceInfo->ipAddress) - 1);
        deviceInfo->ipAddress[sizeof(deviceInfo->ipAddress) - 1] = '\0';

        strncpy(deviceInfo->status, "Available", sizeof(deviceInfo->status) - 1);
        deviceInfo->status[sizeof(deviceInfo->status) - 1] = '\0';

        deviceInfo->isConnected = (mechEyeConnected || virtualCameraConnected);

        return true;
    }

    DEVICEINTERFACE_API bool __cdecl ConnectMechEyeCamera(int index)
    {
        auto& devices = GetMechEyeDevices();
        if (index < 0 || index >= devices.size())
            return false;

        try
        {
            if (mechEyeConnected)
                DisconnectMechEyeCamera();

            if (virtualCameraConnected) {
                GetCurrentVirtualCamera().disconnect();
                virtualCameraConnected = false;
            }

            const auto& deviceInfo = devices[index];
            bool isVirtual = deviceInfo.serialNumber.find("VRT") == 0;

            if (isVirtual) {
                // Connect to virtual camera
                if (GetCurrentVirtualCamera().connect(deviceInfo)) {
                    virtualCameraConnected = true;
                    std::cout << "Successfully connected to virtual camera: " << deviceInfo.deviceName << std::endl;
                    return true;
                }
                else {
                    std::cerr << "Failed to connect to virtual camera" << std::endl;
                    return false;
                }
            }
            else {
                // Connect to real camera
                mmind::eye::ErrorStatus status = GetCurrentMechEyeCamera().connect(deviceInfo);
                if (status.isOK()) {
                    mechEyeConnected = true;
                    std::cout << "Successfully connected to Mech-Eye camera: " << deviceInfo.deviceName << std::endl;
                    return true;
                }
                else {
                    std::cerr << "Failed to connect to Mech-Eye camera" << std::endl;
                    return false;
                }
            }
        }
        catch (const std::exception& e)
        {
            std::cerr << "Exception connecting to Mech-Eye camera: " << e.what() << std::endl;
            return false;
        }
    }

    DEVICEINTERFACE_API bool __cdecl DisconnectMechEyeCamera()
    {
        bool success = true;

        if (mechEyeConnected)
        {
            try
            {
                GetCurrentMechEyeCamera().disconnect();
                mechEyeConnected = false;
                std::cout << "Disconnected from Mech-Eye camera" << std::endl;
            }
            catch (const std::exception& e)
            {
                std::cerr << "Error disconnecting Mech-Eye camera: " << e.what() << std::endl;
                success = false;
            }
        }

        if (virtualCameraConnected)
        {
            try
            {
                GetCurrentVirtualCamera().disconnect();
                virtualCameraConnected = false;
                std::cout << "Disconnected from virtual camera" << std::endl;
            }
            catch (const std::exception& e)
            {
                std::cerr << "Error disconnecting virtual camera: " << e.what() << std::endl;
                success = false;
            }
        }

        return success;
    }

    DEVICEINTERFACE_API bool __cdecl CaptureMechEyeImage(const char* filename)
    {
        if ((!mechEyeConnected && !virtualCameraConnected) || filename == nullptr)
            return false;

        try
        {
            cv::Mat image2D;

            if (virtualCameraConnected) {
                // Use virtual camera to generate sample image
                image2D = cv::Mat(480, 640, CV_8UC3);

                // Generate a sample pattern
                for (int y = 0; y < image2D.rows; ++y) {
                    for (int x = 0; x < image2D.cols; ++x) {
                        image2D.at<cv::Vec3b>(y, x) = cv::Vec3b(
                            static_cast<uchar>((x + y) % 256),
                            static_cast<uchar>((x * 2) % 256),
                            static_cast<uchar>((y * 2) % 256)
                        );
                    }
                }

                // Add timestamp text
                std::string timestamp = "Virtual Camera - " + std::to_string(std::chrono::duration_cast<std::chrono::seconds>(
                    std::chrono::system_clock::now().time_since_epoch()).count());
                cv::putText(image2D, timestamp, cv::Point(10, 30), cv::FONT_HERSHEY_SIMPLEX, 0.7, cv::Scalar(255, 255, 255), 2);

            }
            else {
                // Use real camera
                mmind::eye::Frame2D frame2D;
                mmind::eye::ErrorStatus status = GetCurrentMechEyeCamera().capture2D(frame2D);

                if (!status.isOK())
                {
                    std::cerr << "Failed to capture 2D image from Mech-Eye camera" << std::endl;
                    return false;
                }

                switch (frame2D.colorType())
                {
                case mmind::eye::ColorTypeOf2DCamera::Monochrome:
                {
                    mmind::eye::GrayScale2DImage grayImage = frame2D.getGrayScaleImage();
                    image2D = cv::Mat(grayImage.height(), grayImage.width(), CV_8UC1, grayImage.data());
                    break;
                }
                case mmind::eye::ColorTypeOf2DCamera::Color:
                {
                    mmind::eye::Color2DImage colorImage = frame2D.getColorImage();
                    image2D = cv::Mat(colorImage.height(), colorImage.width(), CV_8UC3, colorImage.data());
                    break;
                }
                default:
                    return false;
                }
            }

            bool result = cv::imwrite(filename, image2D);
            if (result) {
                std::string cameraType = virtualCameraConnected ? "virtual" : "real";
                std::cout << "Captured and saved " << cameraType << " camera image: " << filename << std::endl;
            }

            return result;
        }
        catch (const std::exception& e)
        {
            std::cerr << "Exception capturing camera image: " << e.what() << std::endl;
            return false;
        }
    }

    DEVICEINTERFACE_API bool __cdecl IsMechEyeConnected()
    {
        return mechEyeConnected || virtualCameraConnected;
    }

    DEVICEINTERFACE_API int __cdecl DiscoverWebcams()
    {
        try
        {
            std::cout << "[DeviceInterface] Discovering webcams..." << std::endl;
            GetWebcamDevices().clear();

            // Try to open cameras from index 0 to 9
            for (int i = 0; i < 10; ++i)
            {
                cv::VideoCapture cap(i);
                if (cap.isOpened())
                {
                    GetWebcamDevices().push_back(i);
                    cap.release();
                    std::cout << "[DeviceInterface] Found webcam at index " << i << std::endl;
                }
            }

            std::cout << "[DeviceInterface] Total webcams found: " << GetWebcamDevices().size() << std::endl;
            return static_cast<int>(GetWebcamDevices().size());
        }
        catch (const std::exception& e)
        {
            std::cerr << "[DeviceInterface] Error discovering webcams: " << e.what() << std::endl;
            return 0;
        }
        catch (...)
        {
            std::cerr << "[DeviceInterface] Unknown error discovering webcams" << std::endl;
            return 0;
        }
    }

    DEVICEINTERFACE_API bool __cdecl GetWebcamDeviceInfo(int index, DeviceInfo* deviceInfo)
    {
        auto& devices = GetWebcamDevices();
        if (index < 0 || index >= devices.size() || deviceInfo == nullptr)
            return false;

        std::string nameStr = "USB Camera #" + std::to_string(devices[index]);
        strncpy(deviceInfo->name, nameStr.c_str(), sizeof(deviceInfo->name) - 1);
        deviceInfo->name[sizeof(deviceInfo->name) - 1] = '\0';

        strncpy(deviceInfo->type, "Webcam", sizeof(deviceInfo->type) - 1);
        deviceInfo->type[sizeof(deviceInfo->type) - 1] = '\0';

        strncpy(deviceInfo->serialNumber, ("USB" + std::to_string(devices[index])).c_str(), sizeof(deviceInfo->serialNumber) - 1);
        deviceInfo->serialNumber[sizeof(deviceInfo->serialNumber) - 1] = '\0';

        strncpy(deviceInfo->ipAddress, "N/A", sizeof(deviceInfo->ipAddress) - 1);
        deviceInfo->ipAddress[sizeof(deviceInfo->ipAddress) - 1] = '\0';

        strncpy(deviceInfo->status, "Available", sizeof(deviceInfo->status) - 1);
        deviceInfo->status[sizeof(deviceInfo->status) - 1] = '\0';

        deviceInfo->isConnected = (webcamConnected && connectedWebcamIndex == devices[index]);

        return true;
    }

    DEVICEINTERFACE_API bool __cdecl ConnectWebcam(int index)
    {
        auto& devices = GetWebcamDevices();
        if (index < 0 || index >= devices.size())
            return false;

        try
        {
            if (webcamConnected)
                DisconnectWebcam();

            GetCurrentWebcam().open(devices[index]);
            if (GetCurrentWebcam().isOpened())
            {
                webcamConnected = true;
                connectedWebcamIndex = devices[index];
                std::cout << "Successfully connected to webcam: " << devices[index] << std::endl;
                return true;
            }
            else
            {
                std::cerr << "Failed to connect to webcam: " << devices[index] << std::endl;
                return false;
            }
        }
        catch (const std::exception& e)
        {
            std::cerr << "Exception connecting to webcam: " << e.what() << std::endl;
            return false;
        }
    }

    DEVICEINTERFACE_API bool __cdecl DisconnectWebcam()
    {
        if (webcamConnected)
        {
            GetCurrentWebcam().release();
            webcamConnected = false;
            connectedWebcamIndex = -1;
            std::cout << "Disconnected from webcam" << std::endl;
        }
        return true;
    }

    DEVICEINTERFACE_API bool __cdecl CaptureWebcamImage(const char* filename)
    {
        if (!webcamConnected || filename == nullptr)
            return false;

        try
        {
            cv::Mat frame;
            GetCurrentWebcam() >> frame;

            if (frame.empty())
            {
                std::cerr << "Failed to capture image from webcam" << std::endl;
                return false;
            }

            bool result = cv::imwrite(filename, frame);
            if (result)
                std::cout << "Captured and saved webcam image: " << filename << std::endl;

            return result;
        }
        catch (const std::exception& e)
        {
            std::cerr << "Exception capturing webcam image: " << e.what() << std::endl;
            return false;
        }
    }

    DEVICEINTERFACE_API bool __cdecl IsWebcamConnected()
    {
        return webcamConnected;
    }

    DEVICEINTERFACE_API void __cdecl CleanupDevices()
    {
        DisconnectMechEyeCamera();
        DisconnectWebcam();

        if (mechEyeDevices) {
            mechEyeDevices->clear();
            delete mechEyeDevices;
            mechEyeDevices = nullptr;
        }
        if (virtualCameraDevices) {
            virtualCameraDevices->clear();
            delete virtualCameraDevices;
            virtualCameraDevices = nullptr;
        }
        if (webcamDevices) {
            webcamDevices->clear();
            delete webcamDevices;
            webcamDevices = nullptr;
        }
        if (currentMechEyeCamera) {
            delete currentMechEyeCamera;
            currentMechEyeCamera = nullptr;
        }
        if (currentVirtualCamera) {
            delete currentVirtualCamera;
            currentVirtualCamera = nullptr;
        }
        if (currentWebcam) {
            delete currentWebcam;
            currentWebcam = nullptr;
        }
    }

} // extern "C"