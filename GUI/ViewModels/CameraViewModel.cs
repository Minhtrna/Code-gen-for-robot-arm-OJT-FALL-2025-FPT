using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GUI.Helpers;
using GUI.Services;

namespace GUI.ViewModels
{
    public class CameraViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly CameraService _cameraService;
        private CameraInfo _selectedCamera;
        private bool _canConnect;
        private bool _isRecording;
        private string _recordingTime = "00:00";
        private bool _showFocusRect;
        private bool _hasCapturedImage;
        private string _focusMode = "Auto";
        private int _captureCount;
        private int _recordingCount;
        private string _cameraUptime = "00:00:00";
        private DateTime _connectionStartTime;
        private string _selectedResolution = "Default";
        private BitmapSource _cameraSource;

        public ObservableCollection<CameraInfo> AvailableCameras { get; }
        public ObservableCollection<string> AvailableResolutions { get; }

        // Camera Properties
        public BitmapSource CameraSource
        {
            get => _cameraSource;
            private set
            {
                _cameraSource = value;
                OnPropertyChanged();
            }
        }

        public string CameraStatus => _cameraService?.CameraStatus ?? "Service not initialized";
        public string Resolution => _cameraService?.Resolution ?? "N/A";
        public bool IsCameraConnected => _cameraService?.IsCameraConnected ?? false;

        // UI Properties
        public CameraInfo SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                _selectedCamera = value;
                OnPropertyChanged();
                UpdateCanConnect();
            }
        }

        public bool CanConnect
        {
            get => _canConnect;
            private set
            {
                _canConnect = value;
                OnPropertyChanged();
            }
        }

        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                _isRecording = value;
                OnPropertyChanged();
            }
        }

        public string RecordingTime
        {
            get => _recordingTime;
            set
            {
                _recordingTime = value;
                OnPropertyChanged();
            }
        }

        public bool ShowFocusRect
        {
            get => _showFocusRect;
            set
            {
                _showFocusRect = value;
                OnPropertyChanged();
            }
        }

        public bool HasCapturedImage
        {
            get => _hasCapturedImage;
            set
            {
                _hasCapturedImage = value;
                OnPropertyChanged();
            }
        }

        public string FocusMode
        {
            get => _focusMode;
            set
            {
                _focusMode = value;
                OnPropertyChanged();
            }
        }

        public string SelectedResolution
        {
            get => _selectedResolution;
            set
            {
                _selectedResolution = value;
                OnPropertyChanged();
                ApplyResolutionChange();
            }
        }

        public int CaptureCount
        {
            get => _captureCount;
            set
            {
                _captureCount = value;
                OnPropertyChanged();
            }
        }

        public int RecordingCount
        {
            get => _recordingCount;
            set
            {
                _recordingCount = value;
                OnPropertyChanged();
            }
        }

        public string CameraUptime
        {
            get => _cameraUptime;
            set
            {
                _cameraUptime = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand RefreshCamerasCommand { get; private set; }
        public ICommand ConnectCameraCommand { get; private set; }
        public ICommand CaptureCommand { get; private set; }
        public ICommand RecordCommand { get; private set; }
        public ICommand AutoFocusCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }

        public CameraViewModel()
        {
            _cameraService = new CameraService();

            // Subscribe to service property changes on UI thread
            _cameraService.PropertyChanged += (s, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    switch (e.PropertyName)
                    {
                        case nameof(CameraService.CurrentBitmap):
                            var bitmap = _cameraService.CurrentBitmap;
                            if (bitmap != null)
                            {
                                var clonedBitmap = bitmap.Clone();
                                clonedBitmap.Freeze();
                                CameraSource = clonedBitmap;
                            }
                            break;
                        case nameof(CameraService.CameraStatus):
                            OnPropertyChanged(nameof(CameraStatus));
                            break;
                        case nameof(CameraService.Resolution):
                            OnPropertyChanged(nameof(Resolution));
                            break;
                        case nameof(CameraService.IsCameraConnected):
                            OnPropertyChanged(nameof(IsCameraConnected));
                            UpdateCanConnect();
                            if (IsCameraConnected)
                            {
                                _connectionStartTime = DateTime.Now;
                                StartUptimeTimer();
                                _cameraService.StartCapture();
                            }
                            else
                            {
                                CameraUptime = "00:00:00";
                            }
                            break;
                    }
                });
            };

            AvailableCameras = new ObservableCollection<CameraInfo>();
            AvailableResolutions = new ObservableCollection<string>
            {
                "Default (Auto)",
                "320x240",
                "640x480",
                "800x600",
                "1920x720"
            };

            InitializeCommands();
            _ = RefreshCameras();
        }

        private void InitializeCommands()
        {
            RefreshCamerasCommand = new RelayCommand(async _ => await RefreshCameras());
            ConnectCameraCommand = new RelayCommand(async _ => await ConnectCamera());
            CaptureCommand = new RelayCommand(async _ => await CaptureImage());
            RecordCommand = new RelayCommand(_ => ToggleRecording());
            AutoFocusCommand = new RelayCommand(_ => AutoFocus());
            SaveCommand = new RelayCommand(async _ => await SaveLastCapture());
        }

        private async Task SaveLastCapture()
        {
            if (!HasCapturedImage || CameraSource == null) return;

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Camera Capture",
                    Filter = "JPEG Image|*.jpg|PNG Image|*.png|All Files|*.*",
                    DefaultExt = ".jpg",
                    FileName = $"camera_capture_{DateTime.Now:yyyyMMdd_HHmmss}.jpg"
                };

                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    await Task.Run(() =>
                    {
                        using (var fileStream = new FileStream(dialog.FileName, FileMode.Create))
                        {
                            BitmapEncoder encoder = new JpegBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(CameraSource));
                            encoder.Save(fileStream);
                        }
                    });

                    MessageBox.Show($"Image saved to:\n{dialog.FileName}", "Save Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving image: {ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RefreshCameras()
        {
            try
            {
                var cameras = await Task.Run(() => _cameraService.GetAvailableCameras());

                AvailableCameras.Clear();
                foreach (var camera in cameras)
                {
                    AvailableCameras.Add(camera);
                }

                if (AvailableCameras.Count > 0 && SelectedCamera == null)
                {
                    SelectedCamera = AvailableCameras[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing cameras: {ex.Message}", "Camera Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task ConnectCamera()
        {
            if (SelectedCamera == null) return;

            try
            {
                bool success = await Task.Run(() => _cameraService.ConnectCamera(SelectedCamera.Index));
                if (!success)
                {
                    MessageBox.Show($"Failed to connect to camera {SelectedCamera.Name}",
                        "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to camera: {ex.Message}", "Camera Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CaptureImage()
        {
            if (!IsCameraConnected) return;

            try
            {
                string capturesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "CameraCaptures");
                if (!Directory.Exists(capturesDir))
                {
                    Directory.CreateDirectory(capturesDir);
                }

                string filename = Path.Combine(capturesDir, $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

                bool success = await Task.Run(() => _cameraService.CaptureImage(filename));

                if (success)
                {
                    var bitmap = _cameraService.CurrentBitmap;
                    if (bitmap != null)
                    {
                        var clonedBitmap = bitmap.Clone();
                        clonedBitmap.Freeze();
                        CameraSource = clonedBitmap;
                    }

                    CaptureCount++;
                    HasCapturedImage = true;
                    MessageBox.Show($"Image captured successfully!\nSaved to: {filename}",
                        "Capture Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to capture image", "Capture Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing image: {ex.Message}", "Camera Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleRecording()
        {
            if (!IsCameraConnected) return;

            IsRecording = !IsRecording;

            if (IsRecording)
            {
                RecordingCount++;
                RecordingTime = "00:00";
                // TODO: Implement actual video recording
            }
            else
            {
                RecordingTime = "00:00";
                // TODO: Stop video recording
            }
        }

        private void AutoFocus()
        {
            if (!IsCameraConnected) return;

            ShowFocusRect = true;
            FocusMode = "Focusing...";

            Task.Delay(1500).ContinueWith(_ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowFocusRect = false;
                    FocusMode = "Auto";
                });
            });
        }

        private void ApplyResolutionChange()
        {
            if (!IsCameraConnected || string.IsNullOrEmpty(SelectedResolution)) return;

            if (SelectedResolution == "Default (Auto)")
            {
                // Keep default resolution
                return;
            }

            var parts = SelectedResolution.Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
            {
                Task.Run(() =>
                {
                    bool success = _cameraService.SetResolution(width, height);
                    if (!success)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("Failed to change resolution. Camera may not support this resolution.",
                                "Resolution Change", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                });
            }
        }

        private void UpdateCanConnect()
        {
            CanConnect = SelectedCamera != null && !IsCameraConnected;
        }

        private void StartUptimeTimer()
        {
            Task.Run(async () =>
            {
                while (IsCameraConnected && !_disposed)
                {
                    var uptime = DateTime.Now - _connectionStartTime;
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        CameraUptime = uptime.ToString(@"hh\:mm\:ss");
                    });

                    await Task.Delay(1000);
                }
            });
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cameraService?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}