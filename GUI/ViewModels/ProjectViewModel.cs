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

namespace GUI.ViewModels
{
    public class ProjectViewModel : INotifyPropertyChanged
    {
        private string _projectStatus = "Project: Robot Arm Analysis - Status: Ready";
        private string _connectedDevice = "USB Camera #1 - Connected";
        private bool _hasLiveView = false;
        private BitmapImage _liveViewSource;
        private string _chatInput = "";
        private string _generatedCode = "// Generated code will appear here\n// Select an analysis result and click 'Generate Code'";
        private string _selectedLanguage = "C#";
        private string _logMessages = "System initialized...\nReady for analysis.\n";
        private bool _isProcessing = false;

        public string ProjectStatus
        {
            get => _projectStatus;
            set { _projectStatus = value; OnPropertyChanged(); }
        }

        public string ConnectedDevice
        {
            get => _connectedDevice;
            set { _connectedDevice = value; OnPropertyChanged(); }
        }

        public bool HasLiveView
        {
            get => _hasLiveView;
            set { _hasLiveView = value; OnPropertyChanged(); }
        }

        public BitmapImage LiveViewSource
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

        // Thay đổi: Sử dụng { get; private set; } thay vì { get; }
        public ICommand StartPreviewCommand { get; private set; }
        public ICommand StopPreviewCommand { get; private set; }
        public ICommand SendMessageCommand { get; private set; }
        public ICommand AnalyzeImageCommand { get; private set; }
        public ICommand GenerateCodeCommand { get; private set; }
        public ICommand ExportResultsCommand { get; private set; }
        public ICommand ClearResultsCommand { get; private set; }
        public ICommand CopyCodeCommand { get; private set; }

        public ProjectViewModel()
        {
            InitializeDemoData();
            InitializeCommands();
        }

        private void InitializeDemoData()
        {
            ChatMessages = new ObservableCollection<ChatMessage>
            {
                new ChatMessage { Message = "Hello! I'm your AI assistant. How can I help you today?", IsFromUser = false, Timestamp = DateTime.Now.AddMinutes(-5) },
                new ChatMessage { Message = "I need to analyze this image for object detection", IsFromUser = true, Timestamp = DateTime.Now.AddMinutes(-4) },
                new ChatMessage { Message = "I can help you with that! Please start the camera preview and I'll analyze what I see.", IsFromUser = false, Timestamp = DateTime.Now.AddMinutes(-3) }
            };

            AnalysisResults = new ObservableCollection<AnalysisResult>
            {
                new AnalysisResult { Description = "Red cube detected", Confidence = 0.95, Type = "Object" },
                new AnalysisResult { Description = "Robot arm visible", Confidence = 0.87, Type = "Equipment" },
                new AnalysisResult { Description = "Work surface identified", Confidence = 0.92, Type = "Surface" }
            };

            AvailableLanguages = new ObservableCollection<string>
            {
                "C#", "Python", "C++", "JavaScript", "MATLAB"
            };
        }

        private void InitializeCommands()
        {
            StartPreviewCommand = new RelayCommand(async _ => await StartPreview());
            StopPreviewCommand = new RelayCommand(async _ => await StopPreview());
            SendMessageCommand = new RelayCommand(async _ => await SendMessage());
            AnalyzeImageCommand = new RelayCommand(async _ => await AnalyzeImage());
            GenerateCodeCommand = new RelayCommand(async _ => await GenerateCode());
            ExportResultsCommand = new RelayCommand(_ => ExportResults());
            ClearResultsCommand = new RelayCommand(_ => ClearResults());
            CopyCodeCommand = new RelayCommand(_ => CopyCode());
        }

        private async Task StartPreview()
        {
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Starting camera preview...\n";
            HasLiveView = true;
            
            await Task.Delay(1000);
            
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Camera preview started successfully\n";
        }

        private async Task StopPreview()
        {
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Stopping camera preview...\n";
            HasLiveView = false;
            LiveViewSource = null;
            
            await Task.Delay(500);
            
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Camera preview stopped\n";
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
                "I understand you want to work with the robot arm. Let me analyze the current scene.",
                "Based on the image, I can see several objects. Would you like me to generate movement code?",
                "I can help you create code to pick up that object. What programming language do you prefer?",
                "The analysis shows good lighting conditions. Ready to proceed with object detection.",
                "I've identified the target object. Shall I generate the robot control sequence?"
            };
            return responses[new Random().Next(responses.Length)];
        }

        private async Task AnalyzeImage()
        {
            IsProcessing = true;
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Starting image analysis...\n";

            await Task.Delay(3000);

            // Add new analysis result
            var newResult = new AnalysisResult
            {
                Description = $"Analysis #{AnalysisResults.Count + 1} completed",
                Confidence = 0.80 + (new Random().NextDouble() * 0.15),
                Type = "Detection"
            };
            AnalysisResults.Add(newResult);

            IsProcessing = false;
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Analysis completed successfully\n";
        }

        private async Task GenerateCode()
        {
            IsProcessing = true;
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Generating code...\n";

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
                    GeneratedCode = @"// C# Robot Control Code
using System;
using RobotControl;

public class RobotController
{
    public void MoveToPosition(double x, double y, double z)
    {
        var robot = new RobotArm();
        robot.MoveTo(x, y, z);
        robot.CloseGripper();
    }
}";
                    break;
                case "Python":
                    GeneratedCode = @"# Python Robot Control Code
import robot_control as rc

def move_to_position(x, y, z):
    robot = rc.RobotArm()
    robot.move_to(x, y, z)
    robot.close_gripper()
    
# Execute movement
move_to_position(100, 50, 20)";
                    break;
                case "C++":
                    GeneratedCode = @"// C++ Robot Control Code
#include <robot_control.h>

void moveToPosition(double x, double y, double z) {
    RobotArm robot;
    robot.moveTo(x, y, z);
    robot.closeGripper();
}

int main() {
    moveToPosition(100.0, 50.0, 20.0);
    return 0;
}";
                    break;
                default:
                    GeneratedCode = $"// {SelectedLanguage} code generation not implemented yet\n// But the framework is ready!";
                    break;
            }
        }

        private void ExportResults()
        {
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Results exported to file\n";
            MessageBox.Show("Results exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearResults()
        {
            AnalysisResults.Clear();
            GeneratedCode = "// Generated code will appear here\n// Select an analysis result and click 'Generate Code'";
            LogMessages += $"[{DateTime.Now:HH:mm:ss}] Results cleared\n";
        }

        private void CopyCode()
        {
            if (!string.IsNullOrEmpty(GeneratedCode))
            {
                Clipboard.SetText(GeneratedCode);
                LogMessages += $"[{DateTime.Now:HH:mm:ss}] Code copied to clipboard\n";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}