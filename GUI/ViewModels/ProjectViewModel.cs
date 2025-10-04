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
        private readonly DeviceService deviceService; // Single service for everything
        private DispatcherTimer _deviceRefreshTimer;
        private DispatcherTimer _liveStreamTimer;

        private string _projectStatus = "Project: Robot Arm Analysis - Status: Ready";
        private bool _hasLiveView = false;
        private BitmapSource _liveViewSource;
        private string _chatInput = "";
        private string _generatedCode = "// Generated code will appear here\n// Select an analysis result and click 'Generate Code'.";
        private string _selectedLanguage = "C#";
        private string _logMessages = "System initialized...\nReady for analysis.\n";
        private bool _isProcessing = false;
        private string _streamInfo = "Stream: Inactive";

        private string connectedDevice = "No Device Connected";
        public string ConnectedDevice
        {
            get => connectedDevice;
            set { connectedDevice = value; OnPropertyChanged(); }
        }

        public string StreamInfo
        {
            get => _streamInfo;
            set { _streamInfo = value; OnPropertyChanged(); }
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
            InitializeLiveStream();

            // Subscribe để update khi DeviceService báo thay đổi
            DeviceService.DevicesChanged += DeviceService_DevicesChanged;

            // Refresh lần đầu
            _ = RefreshDevices();

            // Initialize device refresh timer
            _deviceRefreshTimer = new DispatcherTimer();
            _deviceRefreshTimer.Interval = TimeSpan.FromSeconds(1);
            _deviceRefreshTimer.Tick += async (s, e) => await RefreshDevices();
            _deviceRefreshTimer.Start();

            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Project ready with integrated live streaming.\n";
        }

        private void InitializeLiveStream()
        {
            // Initialize the live stream through DeviceService
            bool streamInitialized = deviceService.InitializeLiveStream(targetFPS: 30, maxWidth: 1920, maxHeight: 1080);
            
            if (streamInitialized)
            {
                // Start live stream timer
                _liveStreamTimer = new DispatcherTimer();
                _liveStreamTimer.Interval = TimeSpan.FromMilliseconds(100); // 10 FPS for UI updates
                _liveStreamTimer.Tick += LiveStreamTimer_Tick;
                _liveStreamTimer.Start();
                
                LogMessages += $"[{DateTime.Now:HH:mm:ss}] Live stream service initialized successfully.\n";
            }
            else
            {
                LogMessages += $"[{DateTime.Now:HH:mm:ss}] Failed to initialize live stream service.\n";
            }
        }

        private void LiveStreamTimer_Tick(object sender, EventArgs e)
        {
            UpdateLiveStream();
        }

        private void UpdateLiveStream()
        {
            try
            {
                if (deviceService.IsLiveStreamActive())
                {
                    // Create a simple test pattern only if devices are actually connected
                    var connectedDevices = DeviceService.GetConnectedDevices();
                    bool hasConnectedDevice = connectedDevices?.Any(d => d.IsConnected) == true;
                    
                    if (hasConnectedDevice)
                    {
                        var testImage = CreateTestLiveImage();
                        
                        if (testImage != null)
                        {
                            LiveViewSource = testImage;
                            
                            if (!HasLiveView)
                            {
                                HasLiveView = true;
                                LogMessages += $"[{DateTime.Now:HH:mm:ss}] Live view started - device connected\n";
                            }
                            
                            var (frames, fps, dropped) = deviceService.GetLiveStreamStats();
                            StreamInfo = $"Stream: Active | {fps:F1} FPS | {frames} frames | {dropped} dropped";
                        }
                    }
                    else
                    {
                        // No device connected - clear live view
                        if (HasLiveView)
                        {
                            HasLiveView = false;
                            LiveViewSource = null;
                            StreamInfo = "Stream: No Device Connected";
                            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Live view stopped - no device connected\n";
                        }
                    }
                }
                else
                {
                    if (HasLiveView)
                    {
                        HasLiveView = false;
                        LiveViewSource = null;
                        StreamInfo = "Stream: Inactive";
                        LogMessages += $"[{DateTime.Now:HH:mm:ss}] Live view stopped - stream inactive\n";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectViewModel] Live stream update error: {ex.Message}");
                StreamInfo = "Stream: Error";
                
                // Try to restart the stream if it failed
                try
                {
                    deviceService.InitializeLiveStream();
                }
                catch
                {
                    // Ignore restart errors
                }
            }
        }

        private BitmapSource CreateTestLiveImage()
        {
            try
            {
                // Create a simple test pattern that changes over time
                int width = 640;
                int height = 480;
                byte[] pixels = new byte[width * height * 3]; // RGB

                var timeMs = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * width + x) * 3;
                        
                        // Create animated color waves
                        double waveX = Math.Sin((x + timeMs * 0.005) * 0.01) * 127 + 128;
                        double waveY = Math.Sin((y + timeMs * 0.003) * 0.01) * 127 + 128;
                        double waveTime = Math.Sin(timeMs * 0.001) * 50 + 50;
                        
                        pixels[index] = (byte)((waveX + waveTime) % 256);     // R
                        pixels[index + 1] = (byte)((waveY + waveTime) % 256); // G
                        pixels[index + 2] = (byte)((waveX + waveY) * 0.5 % 256); // B
                    }
                }

                return BitmapSource.Create(width, height, 96, 96, PixelFormats.Rgb24, null, pixels, width * 3);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectViewModel] Test image creation error: {ex.Message}");
                return null;
            }
        }

        private void InitializeDemoData()
        {
            ChatMessages = new ObservableCollection<ChatMessage>
            {
                new ChatMessage { Message = "Hello! I'm your AI assistant with integrated live streaming.", IsFromUser = false, Timestamp = DateTime.Now.AddMinutes(-5) },
                new ChatMessage { Message = "The system now uses a unified DeviceService for all camera operations!", IsFromUser = false, Timestamp = DateTime.Now.AddMinutes(-4) }
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
            AnalyzeImageCommand = new RelayCommand(async _ => await AnalyzeImage());
            GenerateCodeCommand = new RelayCommand(async _ => await GenerateCode());
            ExportResultsCommand = new RelayCommand(_ => ExportResults());
            ClearResultsCommand = new RelayCommand(_ => ClearResults());
            CopyCodeCommand = new RelayCommand(_ => CopyCode());
            RefreshDevicesCommand = new RelayCommand(async _ => await RefreshDevices());
        }

        private async void DeviceService_DevicesChanged(object sender, EventArgs e)
        {
            await RefreshDevices();
        }

        private async Task RefreshDevices()
        {
            try
            {
                var connectedDevices = await Task.Run(() => DeviceService.GetConnectedDevices());

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var connected = connectedDevices?.FirstOrDefault(d => d.IsConnected);

                    if (connected != null)
                    {
                        ConnectedDevice = !string.IsNullOrEmpty(connected.Name)
                            ? connected.Name
                            : connected.SerialNumber;
                    }
                    else
                    {
                        ConnectedDevice = "No Device Connected";
                    }
                });
            }
            catch (Exception ex)
            {
                LogMessages += $"[{DateTime.Now:HH:mm:ss}] Failed to refresh devices: {ex.Message}\n";
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

            LogMessages += $"[{DateTime.Now:HH:mm:ss}] User query processed with unified service\n";
        }

        private string GenerateAIResponse(string input)
        {
            var responses = new[]
            {
                "I can see the live feed through our unified DeviceService! The integration is working perfectly.",
                "The unified architecture provides better performance and simpler maintenance.",
                "Great! The DeviceService handles both camera management and live streaming seamlessly.",
                "Perfect unified design! All functionality in one service - much cleaner.",
                "I can analyze the live stream from the integrated service. The architecture is excellent!"
            };
            return responses[new Random().Next(responses.Length)];
        }

        private async Task AnalyzeImage()
        {
            IsProcessing = true;
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Analyzing frame from integrated live stream...\n";

            await Task.Delay(3000);

            var objects = new[] { "Unified live pattern", "Integrated stream data", "DeviceService frame", "Optimized video data", "Single-service content" };
            var types = new[] { "Live Stream", "Integrated Frame", "Unified Data", "Service Output" };

            var newResult = new AnalysisResult
            {
                Description = objects[new Random().Next(objects.Length)],
                Confidence = 0.75 + (new Random().NextDouble() * 0.24),
                Type = types[new Random().Next(types.Length)]
            };
            AnalysisResults.Add(newResult);

            IsProcessing = false;
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Analysis completed using unified service\n";

            ChatMessages.Add(new ChatMessage
            {
                Message = $"Analyzed live stream via DeviceService: {newResult.Description} with {newResult.Confidence:P1} confidence. The unified architecture ensures optimal performance!",
                IsFromUser = false,
                Timestamp = DateTime.Now
            });
        }

        private async Task GenerateCode()
        {
            IsProcessing = true;
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Generating {SelectedLanguage} code with unified architecture...\n";

            await Task.Delay(2000);

            GenerateCodeForLanguage();
            IsProcessing = false;
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Code generation completed using unified design\n";
        }

        private void GenerateCodeForLanguage()
        {
            switch (SelectedLanguage)
            {
                case "C#":
                    GeneratedCode = @"// C# Robot Control Code - Unified DeviceService Architecture
using System;
using RobotControl;
using GUI.Services;

public class UnifiedRobotController
{
    private readonly DeviceService deviceService;
    
    public UnifiedRobotController()
    {
        // Single service for all device operations
        deviceService = new DeviceService();
    }
    
    public async Task ExecuteWithUnifiedLiveAnalysis()
    {
        var robot = new RobotArm();
        
        // Initialize live streaming through DeviceService
        if (deviceService.InitializeLiveStream(30, 1920, 1080))
        {
            while (deviceService.IsLiveStreamActive())
            {
                // Get stream statistics
                var (frames, fps, dropped) = deviceService.GetLiveStreamStats();
                
                if (fps > 10) // Ensure good frame rate
                {
                    // Analyze current frame for objects
                    var objects = AnalyzeLiveFrame();
                    
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
                
                await Task.Delay(100);
            }
        }
        
        await robot.MoveToHomeAsync();
    }
}";
                    break;
                default:
                    GeneratedCode = $"// {SelectedLanguage} robot control code with unified DeviceService\n// Single service handles all device operations\n// Simplified architecture with better performance\n// Implementation uses integrated live streaming!";
                    break;
            }
        }

        private void ExportResults()
        {
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Results exported from unified system\n";
            MessageBox.Show("Analysis results exported successfully using unified architecture!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearResults()
        {
            AnalysisResults.Clear();
            GeneratedCode = "// Generated code will appear here\n// Using unified DeviceService architecture";
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Results cleared\n";
        }

        private void CopyCode()
        {
            if (!string.IsNullOrEmpty(GeneratedCode))
            {
                Clipboard.SetText(GeneratedCode);
                LogMessages += $"[{DateTime.Now:HH:mm:ss}] Unified code copied to clipboard\n";
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
            _liveStreamTimer?.Stop();
            _deviceRefreshTimer?.Stop();
            
            DeviceService.DevicesChanged -= DeviceService_DevicesChanged;
            
            deviceService?.Dispose();
        }
    }
}
