using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using GUI.Models;

namespace GUI.Services
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DeviceInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Type;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string SerialNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string IpAddress;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Status;
        [MarshalAs(UnmanagedType.Bool)]
        public bool IsConnected;
    }

    // Add structs for LiveStreamProcessor functionality
    [StructLayout(LayoutKind.Sequential)]
    public struct ImageData
    {
        public int width;
        public int height;
        public int channels;
        public int stride;
        public IntPtr data;
        public int dataSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StreamConfig
    {
        public int targetFPS;
        public int maxWidth;
        public int maxHeight;
        public bool enableProcessing;
        public int compressionQuality;
    }

    public class DeviceService : IDisposable
    {
        private const string DLL_NAME = "DeviceInterface.dll"; // Single DLL for everything now
        private bool _disposed = false;

        // P/Invoke declarations for device management
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DiscoverWebcams();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DiscoverMechEyeCameras();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetWebcamDeviceInfo(int index, ref DeviceInfo deviceInfo);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetMechEyeDeviceInfo(int index, ref DeviceInfo deviceInfo);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ConnectWebcam(int index);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ConnectMechEyeCamera(int index);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DisconnectWebcam();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DisconnectMechEyeCamera();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool CaptureWebcamImage([MarshalAs(UnmanagedType.LPStr)] string filename);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool CaptureMechEyeImage([MarshalAs(UnmanagedType.LPStr)] string filename);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool IsWebcamConnected();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool IsMechEyeConnected();

        // P/Invoke declarations for LiveStreamProcessor (now in same DLL)
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool InitializeStream(ref StreamConfig config);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ShutdownStream();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool IsStreamActive();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool CaptureFrameFromSource(
            IntPtr cameraHandle,
            [MarshalAs(UnmanagedType.LPStr)] string sourceType,
            ref ImageData outImageData);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr AllocateImageBuffer(int width, int height, int channels);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void FreeImageBuffer(IntPtr buffer);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void GetStreamStats(out int framesProcessed, out float averageFPS, out int droppedFrames);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void CleanupDevices();


        // NEW: Live stream functionality
        public bool InitializeLiveStream(int targetFPS = 30, int maxWidth = 1920, int maxHeight = 1080)
        {
            var config = new StreamConfig
            {
                targetFPS = targetFPS,
                maxWidth = maxWidth,
                maxHeight = maxHeight,
                enableProcessing = true,
                compressionQuality = 85
            };

            return InitializeStream(ref config);
        }

        public bool IsLiveStreamActive()
        {
            return IsStreamActive();
        }

        public (int frames, float fps, int dropped) GetLiveStreamStats()
        {
            GetStreamStats(out int frames, out float fps, out int dropped);
            return (frames, fps, dropped);
        }

        // STATIC CONNECTION TRACKING to persist across all instances
        private static readonly Dictionary<string, bool> _globalConnectionStates = new Dictionary<string, bool>();

        // Static event so all instances can notify listeners (DeviceViewModel and ProjectViewModel typically use different instances)
        public static event EventHandler DevicesChanged;

        private List<Device> _webcamDevices = new List<Device>();
        private List<Device> _mechEyeDevices = new List<Device>();

        private static void RaiseDevicesChanged()
        {
            try
            {
                DevicesChanged?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceService] DevicesChanged handler error: {ex.Message}");
            }
        }

        public List<Device> DiscoverAllDevices()
        {
            var allDevices = new List<Device>();

            try
            {
                System.Diagnostics.Debug.WriteLine("[DeviceService] Starting device discovery...");

                // Check actual DLL connection status
                bool webcamConnected = IsWebcamConnected();
                bool mechEyeConnected = IsMechEyeConnected();

                System.Diagnostics.Debug.WriteLine($"[DeviceService] DLL status - Webcam: {webcamConnected}, MechEye: {mechEyeConnected}");

                // Discover webcams
                int webcamCount = DiscoverWebcams();
                _webcamDevices.Clear();

                for (int i = 0; i < webcamCount; i++)
                {
                    var deviceInfo = new DeviceInfo();
                    if (GetWebcamDeviceInfo(i, ref deviceInfo))
                    {
                        string serialNumber = deviceInfo.SerialNumber ?? $"USB{i}";

                        // Check global connection state
                        bool isStoredAsConnected = _globalConnectionStates.ContainsKey(serialNumber) && _globalConnectionStates[serialNumber];
                        bool actuallyConnected = isStoredAsConnected && webcamConnected;

                        var device = new Device
                        {
                            Name = deviceInfo.Name ?? $"USB Camera #{i}",
                            Type = deviceInfo.Type ?? "Webcam",
                            SerialNumber = serialNumber,
                            IpAddress = deviceInfo.IpAddress ?? "N/A",
                            Status = actuallyConnected ? "Connected" : "Available",
                            ConnectionStatus = actuallyConnected ? "Connected" : "Disconnected",
                            IsConnected = actuallyConnected
                        };

                        _webcamDevices.Add(device);
                        allDevices.Add(device);

                        System.Diagnostics.Debug.WriteLine($"[DeviceService] Webcam {i}: {device.Name} - Stored: {isStoredAsConnected}, DLL: {webcamConnected}, Final: {device.IsConnected}");
                    }
                }

                // Discover Mech-Eye cameras
                int mechEyeCount = DiscoverMechEyeCameras();
                _mechEyeDevices.Clear();

                for (int i = 0; i < mechEyeCount; i++)
                {
                    var deviceInfo = new DeviceInfo();
                    if (GetMechEyeDeviceInfo(i, ref deviceInfo))
                    {
                        string serialNumber = deviceInfo.SerialNumber ?? $"MechEye{i}";

                        // Check global connection state
                        bool isStoredAsConnected = _globalConnectionStates.ContainsKey(serialNumber) && _globalConnectionStates[serialNumber];
                        bool actuallyConnected = isStoredAsConnected && mechEyeConnected;

                        var device = new Device
                        {
                            Name = deviceInfo.Name ?? "Unknown Mech-Eye",
                            Type = deviceInfo.Type ?? "Mech-Eye Camera",
                            SerialNumber = serialNumber,
                            IpAddress = deviceInfo.IpAddress ?? "N/A",
                            Status = actuallyConnected ? "Connected" : "Available",
                            ConnectionStatus = actuallyConnected ? "Connected" : "Disconnected",
                            IsConnected = actuallyConnected
                        };

                        _mechEyeDevices.Add(device);
                        allDevices.Add(device);

                        System.Diagnostics.Debug.WriteLine($"[DeviceService] MechEye {i}: {device.Name} - Stored: {isStoredAsConnected}, DLL: {mechEyeConnected}, Final: {device.IsConnected}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DeviceService] Discovery complete - Total: {allDevices.Count}, Connected: {allDevices.Count(d => d.IsConnected)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceService] Discovery error: {ex.Message}");
                throw new InvalidOperationException($"Failed to discover devices: {ex.Message}", ex);
            }

            return allDevices;
        }

        public bool ConnectDevice(Device device)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceService] Connecting to: {device.Name} ({device.SerialNumber})");

                int webcamIndex = _webcamDevices.IndexOf(device);
                if (webcamIndex >= 0)
                {
                    bool success = ConnectWebcam(webcamIndex);
                    if (success)
                    {
                        device.IsConnected = true;
                        device.ConnectionStatus = "Connected";
                        device.Status = "Connected";

                        // Store connection state globally
                        _globalConnectionStates[device.SerialNumber] = true;

                        System.Diagnostics.Debug.WriteLine($"[DeviceService] Successfully connected webcam: {device.Name}");

                        // notify listeners across instances
                        RaiseDevicesChanged();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DeviceService] Failed to connect webcam: {device.Name}");
                    }
                    return success;
                }

                int mechEyeIndex = _mechEyeDevices.IndexOf(device);
                if (mechEyeIndex >= 0)
                {
                    bool success = ConnectMechEyeCamera(mechEyeIndex);
                    if (success)
                    {
                        device.IsConnected = true;
                        device.ConnectionStatus = "Connected";
                        device.Status = "Connected";

                        // Store connection state globally
                        _globalConnectionStates[device.SerialNumber] = true;

                        System.Diagnostics.Debug.WriteLine($"[DeviceService] Successfully connected MechEye: {device.Name}");

                        // notify listeners across instances
                        RaiseDevicesChanged();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DeviceService] Failed to connect MechEye: {device.Name}");
                    }
                    return success;
                }

                System.Diagnostics.Debug.WriteLine($"[DeviceService] Device not found in discovery lists: {device.Name}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceService] Connection error: {ex.Message}");
                throw new InvalidOperationException($"Failed to connect to device: {ex.Message}", ex);
            }
        }

        public bool DisconnectDevice(Device device)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceService] Disconnecting from: {device.Name} ({device.SerialNumber})");

                bool success = false;

                if (_webcamDevices.Contains(device))
                {
                    success = DisconnectWebcam();
                }
                else if (_mechEyeDevices.Contains(device))
                {
                    success = DisconnectMechEyeCamera();
                }

                if (success)
                {
                    device.IsConnected = false;
                    device.ConnectionStatus = "Disconnected";
                    device.Status = "Available";

                    // Update global connection state
                    _globalConnectionStates[device.SerialNumber] = false;

                    System.Diagnostics.Debug.WriteLine($"[DeviceService] Successfully disconnected: {device.Name}");

                    // notify listeners across instances
                    RaiseDevicesChanged();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DeviceService] Failed to disconnect: {device.Name}");
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceService] Disconnection error: {ex.Message}");
                throw new InvalidOperationException($"Failed to disconnect device: {ex.Message}", ex);
            }
        }

        public bool CaptureImage(Device device, string filename)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceService] Capturing image from {device.Name} ({device.SerialNumber}) to {filename}");

                if (_webcamDevices.Contains(device) && device.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine($"[DeviceService] Using webcam capture...");
                    bool success = CaptureWebcamImage(filename);
                    System.Diagnostics.Debug.WriteLine($"[DeviceService] Webcam capture result: {success}");

                    if (success && File.Exists(filename))
                    {
                        System.Diagnostics.Debug.WriteLine($"[DeviceService] Image file created: {new FileInfo(filename).Length} bytes");
                    }

                    return success;
                }
                else if (_mechEyeDevices.Contains(device) && device.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine($"[DeviceService] Using MechEye capture...");
                    bool success = CaptureMechEyeImage(filename);
                    System.Diagnostics.Debug.WriteLine($"[DeviceService] MechEye capture result: {success}");
                    return success;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DeviceService] Device not in lists or not connected - Webcam list: {_webcamDevices.Count}, MechEye list: {_mechEyeDevices.Count}, Device connected: {device.IsConnected}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeviceService] Capture exception: {ex.Message}");
                throw new InvalidOperationException($"Failed to capture image: {ex.Message}", ex);
            }
        }

        // NEW: Get connected devices for direct access
        public static List<Device> GetConnectedDevices()
        {
            return _globalConnectionStates.Where(kvp => kvp.Value).Select(kvp => new Device
            {
                SerialNumber = kvp.Key,
                IsConnected = true
            }).ToList();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    ShutdownStream(); // Clean up live stream
                    CleanupDevices(); // Clean up devices
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DeviceService] Cleanup failed: {ex.Message}");
                }
                _disposed = true;
            }
        }
    }
}
