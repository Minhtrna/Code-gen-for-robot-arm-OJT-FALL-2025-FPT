using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace GUI.Services
{
    public class CameraInfo
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsConnected { get; set; }
    }

    public class CameraService : INotifyPropertyChanged, IDisposable
    {
        private VideoCapture _capture;
        private bool _isCapturing;
        private bool _isDisposed;
        private Mat _currentFrame;
        private readonly object _frameLock = new object();
        private Dispatcher _uiDispatcher;
        private Task _captureTask;
        private CancellationTokenSource _captureCts;

        // Properties for binding
        private BitmapSource _currentBitmap;
        private string _cameraStatus = "Disconnected";
        private string _resolution = "N/A";
        private bool _isCameraConnected;

        public BitmapSource CurrentBitmap
        {
            get => _currentBitmap;
            private set
            {
                _currentBitmap = value;
                OnPropertyChanged();
            }
        }

        public string CameraStatus
        {
            get => _cameraStatus;
            private set
            {
                _cameraStatus = value;
                OnPropertyChanged();
            }
        }

        public string Resolution
        {
            get => _resolution;
            private set
            {
                _resolution = value;
                OnPropertyChanged();
            }
        }

        public bool IsCameraConnected
        {
            get => _isCameraConnected;
            private set
            {
                _isCameraConnected = value;
                OnPropertyChanged();
            }
        }

        public CameraService()
        {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        public List<CameraInfo> GetAvailableCameras()
        {
            var cameras = new List<CameraInfo>();

            try
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        using (var testCapture = new VideoCapture(i))
                        {
                            if (testCapture.IsOpened())
                            {
                                cameras.Add(new CameraInfo
                                {
                                    Index = i,
                                    Name = $"Camera {i}",
                                    Description = $"USB Camera Device {i}",
                                    IsConnected = false
                                });
                                testCapture.Release();
                            }
                        }
                    }
                    catch
                    {
                        // Camera not available at this index
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting cameras: {ex.Message}");
            }

            return cameras;
        }

        public bool ConnectCamera(int cameraIndex)
        {
            try
            {
                DisconnectCamera();

                // Use DirectShow backend for better performance on Windows
                _capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.DSHOW);

                if (!_capture.IsOpened())
                {
                    _capture?.Dispose();
                    _capture = null;
                    return false;
                }

                // Reset all camera settings to AUTO mode (clear any manual settings)
                try
                {
                    // Enable all auto modes to clear manual settings
                    _capture.Set(VideoCaptureProperties.AutoExposure, 0.75); // Auto exposure ON
                    _capture.Set(VideoCaptureProperties.AutoWB, 1); // Auto white balance

                    System.Diagnostics.Debug.WriteLine("Camera settings cleared - all auto modes enabled");
                }
                catch (Exception resetEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Note: Some auto settings not supported: {resetEx.Message}");
                    // This is OK - not all cameras support all auto modes
                }

                // Get camera's natural resolution
                int width = (int)_capture.Get(VideoCaptureProperties.FrameWidth);
                int height = (int)_capture.Get(VideoCaptureProperties.FrameHeight);

                Resolution = $"{width}x{height}";

                IsCameraConnected = true;
                CameraStatus = $"Connected - Camera {cameraIndex}";

                System.Diagnostics.Debug.WriteLine($"Camera connected: {width}x{height}");

                return true;
            }
            catch (Exception ex)
            {
                CameraStatus = $"Connection failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Camera connection error: {ex.Message}");
                return false;
            }
        }

        public bool SetResolution(int width, int height)
        {
            if (_capture == null || !_capture.IsOpened())
                return false;

            try
            {
                // Lock to prevent race condition with capture loop
                lock (_frameLock)
                {
                    _capture.Set(VideoCaptureProperties.FrameWidth, width);
                    _capture.Set(VideoCaptureProperties.FrameHeight, height);

                    // Get actual resolution (camera may not support requested resolution)
                    int actualWidth = (int)_capture.Get(VideoCaptureProperties.FrameWidth);
                    int actualHeight = (int)_capture.Get(VideoCaptureProperties.FrameHeight);

                    Resolution = $"{actualWidth}x{actualHeight}";

                    System.Diagnostics.Debug.WriteLine($"Resolution changed to: {actualWidth}x{actualHeight}");
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Resolution change error: {ex.Message}");
                return false;
            }
        }

        public void DisconnectCamera()
        {
            StopCapture();

            try
            {
                if (_capture != null)
                {
                    lock (_frameLock)
                    {
                        _capture.Release();
                        _capture.Dispose();
                        _capture = null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Camera disconnect error: {ex.Message}");
            }

            IsCameraConnected = false;
            CameraStatus = "Disconnected";
            Resolution = "N/A";
            CurrentBitmap = null;
        }

        public void StartCapture()
        {
            if (_capture == null || !_capture.IsOpened() || _isCapturing)
                return;

            _isCapturing = true;
            CameraStatus = "Streaming...";

            // Create cancellation token for stopping the capture loop
            _captureCts = new CancellationTokenSource();

            // Start continuous capture loop on background thread
            _captureTask = Task.Run(() => CaptureLoop(_captureCts.Token), _captureCts.Token);
        }

        public void StopCapture()
        {
            _isCapturing = false;

            // Cancel the capture loop
            _captureCts?.Cancel();

            // Wait for capture task to complete
            try
            {
                _captureTask?.Wait(1000); // Wait max 1 second
            }
            catch (AggregateException)
            {
                // Task was cancelled, this is expected
            }

            _captureCts?.Dispose();
            _captureCts = null;
            _captureTask = null;

            if (IsCameraConnected)
            {
                CameraStatus = "Connected - Stopped";
            }
        }

        private void CaptureLoop(CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("Capture loop started");

            while (!cancellationToken.IsCancellationRequested && _isCapturing)
            {
                try
                {
                    Mat frame = new Mat();

                    // Read frame with lock to prevent conflicts with resolution changes
                    bool readSuccess;
                    lock (_frameLock)
                    {
                        if (_capture == null || !_capture.IsOpened())
                            break;

                        readSuccess = _capture.Read(frame);
                    }

                    if (!readSuccess || frame.Empty())
                    {
                        frame?.Dispose();
                        continue;
                    }

                    // Store current frame for capture operations
                    lock (_frameLock)
                    {
                        _currentFrame?.Dispose();
                        _currentFrame = frame.Clone();
                    }

                    // Convert to BitmapSource on UI thread
                    if (_uiDispatcher != null && !_isDisposed)
                    {
                        _uiDispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                if (!_isDisposed && frame != null && !frame.Empty())
                                {
                                    var bitmap = frame.ToBitmapSource();

                                    if (bitmap != null && !bitmap.IsFrozen)
                                    {
                                        bitmap.Freeze();
                                    }

                                    CurrentBitmap = bitmap;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Bitmap conversion error: {ex.Message}");
                            }
                            finally
                            {
                                frame?.Dispose();
                            }
                        }), DispatcherPriority.Render);
                    }
                    else
                    {
                        frame?.Dispose();
                    }

                    // Small delay to prevent CPU overload (approximately 30 fps)
                    Thread.Sleep(33);
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        System.Diagnostics.Debug.WriteLine($"Frame capture error: {ex.Message}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("Capture loop stopped");
        }

        public bool CaptureImage(string filePath)
        {
            if (!_isCapturing || _currentFrame == null)
                return false;

            try
            {
                lock (_frameLock)
                {
                    if (_currentFrame != null && !_currentFrame.Empty())
                    {
                        return _currentFrame.SaveImage(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Image capture error: {ex.Message}");
            }

            return false;
        }

        public BitmapSource CaptureImageAsBitmap()
        {
            if (!_isCapturing || _currentFrame == null)
                return null;

            try
            {
                Mat frameCopy;
                lock (_frameLock)
                {
                    if (_currentFrame == null || _currentFrame.Empty())
                        return null;

                    frameCopy = _currentFrame.Clone();
                }

                BitmapSource bitmap = null;
                if (_uiDispatcher != null)
                {
                    _uiDispatcher.Invoke(() =>
                    {
                        try
                        {
                            bitmap = frameCopy.ToBitmapSource();
                            if (bitmap != null && !bitmap.IsFrozen)
                            {
                                bitmap.Freeze();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Bitmap capture error: {ex.Message}");
                        }
                        finally
                        {
                            frameCopy.Dispose();
                        }
                    });
                }
                else
                {
                    frameCopy.Dispose();
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bitmap capture error: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            StopCapture();
            DisconnectCamera();

            lock (_frameLock)
            {
                _currentFrame?.Dispose();
                _currentFrame = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}