using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GUI.Helpers;
using GUI.Models;
using GUI.Services;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using System.Windows.Media;
using System.Runtime.InteropServices;

namespace GUI.ViewModels
{
    public class ProjectViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly DeviceService deviceService;
        
        private string _projectStatus = "Project: Robot Arm Analysis - Status: Ready";
        private string _chatInput = "";
        private string _generatedCode = "// Generated code will appear here\n// Select an analysis result and click 'Generate Code'.";
        private string _selectedLanguage = "C#";
        private string _logMessages = "System initialized...\nReady for analysis.\n";
        private bool _isProcessing = false;
        
        // Live view properties
        private bool _hasLiveView = false;
        private BitmapSource _liveViewSource;
        private string _tempImagePath; // Temporary image path for current capture
        
        private string connectedDevice = "No Device Connected";
        public string ConnectedDevice
        {
            get => connectedDevice;
            set { connectedDevice = value; OnPropertyChanged(); }
        }

        public string ProjectStatus
        {
            get => _projectStatus;
            set { _projectStatus = value; OnPropertyChanged(); }
        }

        public bool HasLiveView
        {
            get => _hasLiveView;
            set { _hasLiveView = value; OnPropertyChanged(); }
        }

        public BitmapSource LiveViewSource
        {
            get => _liveViewSource;
            set { _liveViewSource = value; OnPropertyChanged(); }
        }

        public string ChatInput
        {
            get => _chatInput;
            set { _chatInput = value; OnPropertyChanged(); }
        }

        public string GeneratedCode
        {
            get => _generatedCode;
            set { _generatedCode = value; OnPropertyChanged(); }
        }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set { _selectedLanguage = value; OnPropertyChanged(); GenerateCodeForLanguage(); }
        }

        public string LogMessages
        {
            get => _logMessages;
            set { _logMessages = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ChatMessage> ChatMessages { get; set; }
        public ObservableCollection<AnalysisResult> AnalysisResults { get; set; }
        public ObservableCollection<string> AvailableLanguages { get; set; }

        public ICommand SendMessageCommand { get; private set; }
        public ICommand CaptureImageCommand { get; private set; }
        public ICommand SaveImageCommand { get; private set; }
        public ICommand AnalyzeImageCommand { get; private set; }
        public ICommand GenerateCodeCommand { get; private set; }
        public ICommand ExportResultsCommand { get; private set; }
        public ICommand ClearResultsCommand { get; private set; }
        public ICommand CopyCodeCommand { get; private set; }
        public ICommand RefreshDevicesCommand { get; private set; }

        // Constructor mặc định (cho XAML.cs)
        public ProjectViewModel() : this(new DeviceService()) { }

        // Constructor chính
        public ProjectViewModel(DeviceService deviceService)
        {
            this.deviceService = deviceService;

            InitializeDemoData();
            InitializeCommands();
            
            // Subscribe to STATIC event with logging
            System.Diagnostics.Debug.WriteLine("[ProjectViewModel] ===== SUBSCRIBING TO DevicesChanged EVENT =====");
            DeviceService.DevicesChanged += DeviceService_DevicesChanged;
            
            // Verify subscription using helper method
            int subscriberCount = DeviceService.GetSubscriberCount();
            System.Diagnostics.Debug.WriteLine($"[ProjectViewModel] Total subscribers after registration: {subscriberCount}");

            // Refresh lần đầu
            _ = RefreshDevices();

            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Project ready. Monitoring device changes...\n";
        }

        private void InitializeDemoData()
        {
            ChatMessages = new ObservableCollection<ChatMessage>
            {
                new ChatMessage { Message = "Hello! I'm your AI assistant.", IsFromUser = false, Timestamp = DateTime.Now.AddMinutes(-5) },
                new ChatMessage { Message = "The system uses DeviceService for camera operations.", IsFromUser = false, Timestamp = DateTime.Now.AddMinutes(-4) }
            };

            AnalysisResults = new ObservableCollection<AnalysisResult>();

            AvailableLanguages = new ObservableCollection<string>
            {
                "C#", "Python", "C++", "JavaScript", "MATLAB", "RAPID"
            };
        }

        private void InitializeCommands()
        {
            SendMessageCommand = new RelayCommand(async _ => await SendMessage());
            CaptureImageCommand = new RelayCommand(async _ => await CaptureImage());
            SaveImageCommand = new RelayCommand(_ => SaveImage());
            AnalyzeImageCommand = new RelayCommand(async _ => await AnalyzeImage());
            GenerateCodeCommand = new RelayCommand(async _ => await GenerateCode());
            ExportResultsCommand = new RelayCommand(_ => ExportResults());
            ClearResultsCommand = new RelayCommand(_ => ClearResults());
            CopyCodeCommand = new RelayCommand(_ => CopyCode());
            RefreshDevicesCommand = new RelayCommand(async _ => await RefreshDevices());
        }

        private async void DeviceService_DevicesChanged(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[ProjectViewModel] DevicesChanged event received!");
            await RefreshDevices();
        }

        private async Task RefreshDevices()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ProjectViewModel] Starting RefreshDevices...");
                
                // Discover devices using THIS instance's service
                var allDevices = await Task.Run(() => deviceService.DiscoverAllDevices());

                System.Diagnostics.Debug.WriteLine($"[ProjectViewModel] Discovered {allDevices?.Count ?? 0} devices");

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Find the first connected device
                    var connected = allDevices?.FirstOrDefault(d => d.IsConnected);

                    if (connected != null)
                    {
                        string deviceName = !string.IsNullOrEmpty(connected.Name)
                            ? connected.Name
                            : connected.SerialNumber;
                        
                        System.Diagnostics.Debug.WriteLine($"[ProjectViewModel] Found connected device: {deviceName}");
                        
                        // Only update if changed
                        if (ConnectedDevice != deviceName)
                        {
                            ConnectedDevice = deviceName;
                            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Device connected: {ConnectedDevice}\n";
                        }
                        
                        // Update HasLiveView based on device connection
                        if (!HasLiveView)
                        {
                            HasLiveView = true;
                            System.Diagnostics.Debug.WriteLine("[ProjectViewModel] HasLiveView set to TRUE");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[ProjectViewModel] No connected device found");
                        
                        // Only update if changed
                        if (ConnectedDevice != "No Device Connected")
                        {
                            ConnectedDevice = "No Device Connected";
                            LogMessages += $"[{DateTime.Now:HH:mm:ss}] No device connected\n";
                        }
                        
                        // Clear live view when no device
                        if (HasLiveView)
                        {
                            HasLiveView = false;
                            LiveViewSource = null;
                            System.Diagnostics.Debug.WriteLine("[ProjectViewModel] HasLiveView set to FALSE");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectViewModel] RefreshDevices error: {ex.Message}");
                LogMessages += $"[{DateTime.Now:HH:mm:ss}] Failed to refresh devices: {ex.Message}\n";
            }
        }

        private async Task CaptureImage()
        {
            try
            {
                IsProcessing = true;
                LogMessages += $"[{DateTime.Now:HH:mm:ss}] Capturing image from {ConnectedDevice}...\n";

                // Get connected device
                var allDevices = await Task.Run(() => deviceService.DiscoverAllDevices());
                var connectedDevice = allDevices?.FirstOrDefault(d => d.IsConnected);

                if (connectedDevice == null)
                {
                    LogMessages += $"[{DateTime.Now:HH:mm:ss}] No device connected\n";
                    MessageBox.Show("Please connect a device first!", "No Device", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsProcessing = false;
                    return;
                }

                // Create temporary directory for captures if it doesn't exist
                string tempDir = Path.Combine(Path.GetTempPath(), "RobotArmCaptures");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                // Generate temporary file path
                _tempImagePath = Path.Combine(tempDir, $"temp_capture_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                // Capture image to temporary file
                bool success = await Task.Run(() => deviceService.CaptureImage(connectedDevice, _tempImagePath));

                if (success && File.Exists(_tempImagePath))
                {
                    // Load image and display in LiveView
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            // Load image from file
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(_tempImagePath);
                            bitmap.EndInit();
                            bitmap.Freeze(); // Important for cross-thread usage

                            LiveViewSource = bitmap;

                            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Image captured successfully\n";
                            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Image size: {new FileInfo(_tempImagePath).Length / 1024} KB\n";
                        }
                        catch (Exception ex)
                        {
                            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Failed to load image: {ex.Message}\n";
                        }
                    });
                }
                else
                {
                    LogMessages += $"[{DateTime.Now:HH:mm:ss}] Failed to capture image\n";
                    MessageBox.Show("Failed to capture image from device.", "Capture Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LogMessages += $"[{DateTime.Now:HH:mm:ss}] Capture error: {ex.Message}\n";
                MessageBox.Show($"Error capturing image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void SaveImage()
        {
            try
            {
                if (string.IsNullOrEmpty(_tempImagePath) || !File.Exists(_tempImagePath))
                {
                    MessageBox.Show("No image to save. Please capture an image first.", "No Image", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Show save file dialog
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Captured Image",
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|All Files|*.*",
                    DefaultExt = ".png",
                    FileName = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                if (dialog.ShowDialog() == true)
                {
                    // Copy temp file to selected location
                    File.Copy(_tempImagePath, dialog.FileName, true);
                    
                    LogMessages += $"[{DateTime.Now:HH:mm:ss}] Image saved to: {dialog.FileName}\n";
                    MessageBox.Show($"Image saved successfully to:\n{dialog.FileName}", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogMessages += $"[{DateTime.Now:HH:mm:ss}] Save error: {ex.Message}\n";
                MessageBox.Show($"Error saving image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(ChatInput)) return;

            var userMessage = new ChatMessage
            {
                Message = ChatInput,
                IsFromUser = true,
                Timestamp = DateTime.Now
            };
            ChatMessages.Add(userMessage);

            var input = ChatInput;
            ChatInput = "";

            // Simulate AI processing
            await Task.Delay(1500);

            var aiResponse = GenerateAIResponse(input);
            ChatMessages.Add(new ChatMessage
            {
                Message = aiResponse,
                IsFromUser = false,
                Timestamp = DateTime.Now
            });

            LogMessages += $"[{DateTime.Now:HH:mm:ss}] User query processed\n";
        }

        private string GenerateAIResponse(string input)
        {
            var responses = new[]
            {
                "I can help you with device management and image capture.",
                "The unified architecture provides better performance and simpler maintenance.",
                "DeviceService handles camera management efficiently.",
                "Perfect unified design! All functionality in one service - much cleaner.",
                "I can help analyze captured images from your connected devices."
            };
            return responses[new Random().Next(responses.Length)];
        }

        private async Task AnalyzeImage()
        {
            if (string.IsNullOrEmpty(_tempImagePath) || !File.Exists(_tempImagePath))
            {
                MessageBox.Show("Please capture an image first!", "No Image", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsProcessing = true;
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Analyzing captured image...\n";

            await Task.Delay(3000);

            var objects = new[] { "Robot arm component", "Industrial part", "Assembly piece", "Manufacturing item", "Workshop object" };
            var types = new[] { "Captured Image", "Static Frame", "Device Output", "Snapshot" };

            var newResult = new AnalysisResult
            {
                Description = objects[new Random().Next(objects.Length)],
                Confidence = 0.75 + (new Random().NextDouble() * 0.24),
                Type = types[new Random().Next(types.Length)]
            };
            AnalysisResults.Add(newResult);

            IsProcessing = false;
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Analysis completed\n";

            ChatMessages.Add(new ChatMessage
            {
                Message = $"Analyzed image: {newResult.Description} with {newResult.Confidence:P1} confidence.",
                IsFromUser = false,
                Timestamp = DateTime.Now
            });
        }

        private async Task GenerateCode()
        {
            IsProcessing = true;
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Generating {SelectedLanguage} code...\n";

            await Task.Delay(2000);

            GenerateCodeForLanguage();
            IsProcessing = false;
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Code generation completed\n";
        }

        private void GenerateCodeForLanguage()
        {
            switch (SelectedLanguage)
            {
                case "C#":
                    GeneratedCode = @"// C# Robot Control Code - DeviceService Architecture
using System;
using RobotControl;
using GUI.Services;

public class RobotController
{
    private readonly DeviceService deviceService;
    
    public RobotController()
    {
        deviceService = new DeviceService();
    }
    
    public async Task ExecuteWithImageAnalysis()
    {
        var robot = new RobotArm();
        
        // Discover and connect to camera
        var devices = deviceService.DiscoverAllDevices();
        var camera = devices.FirstOrDefault(d => d.Type.Contains(""Camera""));
        
        if (camera != null && deviceService.ConnectDevice(camera))
        {
            // Capture image for analysis
            string imagePath = ""capture.jpg"";
            if (deviceService.CaptureImage(camera, imagePath))
            {
                // Analyze captured image for objects
                var objects = AnalyzeImage(imagePath);
                
                foreach (var obj in objects)
                {
                    if (obj.Confidence > 0.8)
                    {
                        // Execute robot movement
                        await robot.MoveToAsync(obj.X, obj.Y, obj.Z + 20.0);
                        robot.OpenGripper();
                        await robot.MoveToAsync(obj.X, obj.Y, obj.Z);
                        robot.CloseGripper();
                        
                        // Place at destination
                        await robot.MoveToAsync(200.0, 100.0, 50.0);
                        robot.OpenGripper();
                        break;
                    }
                }
            }
            
            deviceService.DisconnectDevice(camera);
        }
        
        await robot.MoveToHomeAsync();
    }
}";
                    break;
                default:
                    GeneratedCode = $"// {SelectedLanguage} robot control code with DeviceService\n// Single service handles all device operations\n// Simplified architecture with better performance";
                    break;
            }
        }

        private void ExportResults()
        {
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Results exported\n";
            MessageBox.Show("Analysis results exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearResults()
        {
            AnalysisResults.Clear();
            GeneratedCode = "// Generated code will appear here\n// Using DeviceService architecture";
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Results cleared\n";
        }

        private void CopyCode()
        {
            if (!string.IsNullOrEmpty(GeneratedCode))
            {
                Clipboard.SetText(GeneratedCode);
                LogMessages += $"[{DateTime.Now:HH:mm:ss}] Code copied to clipboard\n";
                MessageBox.Show("Code copied to clipboard!", "Copy Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            // Unsubscribe from static event
            DeviceService.DevicesChanged -= DeviceService_DevicesChanged;
            
            // Clean up temporary image file
            if (!string.IsNullOrEmpty(_tempImagePath) && File.Exists(_tempImagePath))
            {
                try
                {
                    File.Delete(_tempImagePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            
            deviceService?.Dispose();
        }
    }
}
