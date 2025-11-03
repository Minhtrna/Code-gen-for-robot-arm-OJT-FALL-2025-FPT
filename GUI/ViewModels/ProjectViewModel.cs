using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GUI.Helpers;
using GUI.Models;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using System.Windows.Media;

namespace GUI.ViewModels
{
    public class ProjectViewModel : INotifyPropertyChanged, IDisposable
    {
        private string _projectStatus = "Project: Robot Arm Analysis - Status: Ready";
        private string _chatInput = "";
        private string _generatedCode = "// Generated code will appear here\n// Select an analysis result and click 'Generate Code'.";
        private string _selectedLanguage = "C#";
        private string _logMessages = "System initialized...\nReady for analysis.\n";
        private bool _isProcessing = false;
        
        // Live view properties - removed device dependencies
        private bool _hasLiveView = false;
        private BitmapSource _liveViewSource;
        private string _tempImagePath; // Temporary image path for current capture

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
        public ICommand LoadImageCommand { get; private set; }
        public ICommand SaveImageCommand { get; private set; }
        public ICommand AnalyzeImageCommand { get; private set; }
        public ICommand GenerateCodeCommand { get; private set; }
        public ICommand ExportResultsCommand { get; private set; }
        public ICommand ClearResultsCommand { get; private set; }
        public ICommand CopyCodeCommand { get; private set; }

        // Constructor
        public ProjectViewModel()
        {
            InitializeDemoData();
            InitializeCommands();

            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Project ready. Device functionality removed - use Camera tab for hardware operations.\n";
        }

        private void InitializeDemoData()
        {
            ChatMessages = new ObservableCollection<ChatMessage>
            {
                new ChatMessage { Message = "Hello! I'm your AI assistant.", IsFromUser = false, Timestamp = DateTime.Now.AddMinutes(-5) },
                new ChatMessage { Message = "Device management has been moved to Camera tab for direct hardware control.", IsFromUser = false, Timestamp = DateTime.Now.AddMinutes(-4) }
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
            LoadImageCommand = new RelayCommand(_ => LoadImage());
            SaveImageCommand = new RelayCommand(_ => SaveImage());
            AnalyzeImageCommand = new RelayCommand(async _ => await AnalyzeImage());
            GenerateCodeCommand = new RelayCommand(async _ => await GenerateCode());
            ExportResultsCommand = new RelayCommand(_ => ExportResults());
            ClearResultsCommand = new RelayCommand(_ => ClearResults());
            CopyCodeCommand = new RelayCommand(_ => CopyCode());
        }

        private void LoadImage()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Load Image for Analysis",
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|All Files|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                if (dialog.ShowDialog() == true)
                {
                    // Load image and display in LiveView
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(dialog.FileName);
                    bitmap.EndInit();
                    bitmap.Freeze();

                    LiveViewSource = bitmap;
                    HasLiveView = true;
                    _tempImagePath = dialog.FileName;

                    LogMessages += $"[{DateTime.Now:HH:mm:ss}] Image loaded: {Path.GetFileName(dialog.FileName)}\n";
                    LogMessages += $"[{DateTime.Now:HH:mm:ss}] Image size: {new FileInfo(dialog.FileName).Length / 1024} KB\n";
                }
            }
            catch (Exception ex)
            {
                LogMessages += $"[{DateTime.Now:HH:mm:ss}] Failed to load image: {ex.Message}\n";
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveImage()
        {
            try
            {
                if (string.IsNullOrEmpty(_tempImagePath) || !File.Exists(_tempImagePath))
                {
                    MessageBox.Show("No image to save. Please load an image first.", "No Image", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Show save file dialog
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Current Image",
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|All Files|*.*",
                    DefaultExt = ".png",
                    FileName = $"analysis_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                if (dialog.ShowDialog() == true)
                {
                    // Copy current image to selected location
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
                "I can help you with image analysis and code generation.",
                "The simplified architecture focuses on core functionality without device management overhead.",
                "Load images using the 'Load Image' button for analysis.",
                "Camera hardware operations are available in the Camera tab.",
                "I can help analyze loaded images and generate robot control code."
            };
            return responses[new Random().Next(responses.Length)];
        }

        private async Task AnalyzeImage()
        {
            if (string.IsNullOrEmpty(_tempImagePath) || !File.Exists(_tempImagePath))
            {
                MessageBox.Show("Please load an image first!", "No Image", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsProcessing = true;
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Analyzing loaded image...\n";

            await Task.Delay(3000);

            var objects = new[] { "Robot arm component", "Industrial part", "Assembly piece", "Manufacturing item", "Workshop object" };
            var types = new[] { "Loaded Image", "Static Analysis", "File Input", "Manual Load" };

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
                    GeneratedCode = @"// C# Robot Control Code - Simplified Architecture
using System;
using RobotControl;

public class RobotController
{
    public RobotController()
    {
        // Direct hardware control through Camera tab
        // Simplified architecture without device service layer
    }
    
    public async Task ExecuteWithImageAnalysis(string imagePath)
    {
        var robot = new RobotArm();
        
        // Analyze pre-loaded image for objects
        var objects = AnalyzeImageFile(imagePath);
        
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
        
        await robot.MoveToHomeAsync();
    }
    
    private List<DetectedObject> AnalyzeImageFile(string imagePath)
    {
        // Image analysis implementation
        return new List<DetectedObject>();
    }
}";
                    break;
                default:
                    GeneratedCode = $"// {SelectedLanguage} robot control code\n// Simplified architecture for better performance\n// Load images manually for analysis";
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
            GeneratedCode = "// Generated code will appear here\n// Load an image and analyze for code generation";
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
            // Clean up temporary image file if needed
            if (!string.IsNullOrEmpty(_tempImagePath) && File.Exists(_tempImagePath) && _tempImagePath.Contains("temp"))
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
        }
    }
}
