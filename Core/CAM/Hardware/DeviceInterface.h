#pragma once

#ifdef DEVICEINTERFACE_EXPORTS
#define DEVICEINTERFACE_API __declspec(dllexport)
#else
#define DEVICEINTERFACE_API __declspec(dllimport)
#endif

#include <cstddef>

struct DeviceInfo
{
    char name[256];
    char type[64];
    char serialNumber[128];
    char ipAddress[64];
    char status[64];
    bool isConnected;
};

// Ensure C linkage for all exported functions
extern "C" {
    // Mech-Eye camera functions
    DEVICEINTERFACE_API int __cdecl DiscoverMechEyeCameras();
    DEVICEINTERFACE_API bool __cdecl GetMechEyeDeviceInfo(int index, DeviceInfo* deviceInfo);
    DEVICEINTERFACE_API bool __cdecl ConnectMechEyeCamera(int index);
    DEVICEINTERFACE_API bool __cdecl DisconnectMechEyeCamera();
    DEVICEINTERFACE_API bool __cdecl CaptureMechEyeImage(const char* filename);
    DEVICEINTERFACE_API bool __cdecl IsMechEyeConnected();
   
    // OpenCV webcam functions  
    DEVICEINTERFACE_API int __cdecl DiscoverWebcams();
    DEVICEINTERFACE_API bool __cdecl GetWebcamDeviceInfo(int index, DeviceInfo* deviceInfo);
    DEVICEINTERFACE_API bool __cdecl ConnectWebcam(int index);
    DEVICEINTERFACE_API bool __cdecl DisconnectWebcam();
    DEVICEINTERFACE_API bool __cdecl CaptureWebcamImage(const char* filename);
    DEVICEINTERFACE_API bool __cdecl IsWebcamConnected();
    
    // General functions
    DEVICEINTERFACE_API void __cdecl CleanupDevices();
}   