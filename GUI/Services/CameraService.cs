using System;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

namespace GUI.Services
{
    public class CameraService : IDisposable
    {
        // Change DLL name to match the new one
        private const string DLL_NAME = "DeviceInterface.dll";

        // Use DeviceInterface.dll functions instead
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DiscoverWebcams();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ConnectWebcam(int index);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DisconnectWebcam();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool IsWebcamConnected();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern bool CaptureWebcamImage([MarshalAs(UnmanagedType.LPStr)] string filename);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void CleanupDevices();

        private bool _isDisposed = false;
        private bool _isInitialized = false;

        public bool IsInitialized => _isInitialized;

        public bool Initialize(int deviceId = 0)
        {
            try
            {
                int webcamCount = DiscoverWebcams();
                if (webcamCount > deviceId)
                {
                    bool success = ConnectWebcam(deviceId);
                    _isInitialized = success;
                    return success;
                }
                return false;
            }
            catch (DllNotFoundException)
            {
                throw new InvalidOperationException($"{DLL_NAME} not found. Make sure the DLL is in the application directory.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize camera: {ex.Message}", ex);
            }
        }

        public int GetAvailableCameraCount()
        {
            try
            {
                return DiscoverWebcams();
            }
            catch
            {
                return 0;
            }
        }

        public BitmapSource CaptureFrameAsBitmap()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Camera not initialized");

            try
            {
                // For now, capture to temp file and load as bitmap
                string tempFile = Path.GetTempFileName() + ".png";

                if (CaptureWebcamImage(tempFile))
                {
                    // Load the saved image as BitmapSource
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(tempFile);
                    bitmap.EndInit();

                    // Clean up temp file
                    try { File.Delete(tempFile); } catch { }

                    return bitmap;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to capture frame: {ex.Message}", ex);
            }
        }

        public bool SaveFrameToFile(string filename)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Camera not initialized");

            try
            {
                return CaptureWebcamImage(filename);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save frame: {ex.Message}", ex);
            }
        }

        public void Release()
        {
            if (!_isDisposed && _isInitialized)
            {
                try
                {
                    DisconnectWebcam();
                    _isInitialized = false;
                }
                catch
                {
                    // Ignore exceptions during cleanup
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                Release();
                try
                {
                    CleanupDevices();
                }
                catch
                {
                    // Ignore cleanup errors
                }
                _isDisposed = true;
            }
        }

        ~CameraService()
        {
            Dispose(false);
        }
    }
}   