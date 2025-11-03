using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using GUI.Helpers;
using GUI.Models;
using GUI.Services;

namespace GUI.ViewModels
{
    public class ValidateSimulateViewModel : INotifyPropertyChanged
    {
        private readonly IRobotKinematicsService _kinematicsService;
        private readonly IRobotModelLoaderService _modelLoader;
        private readonly DispatcherTimer _animationTimer;

        private bool _isSimulationRunning;
        private double _simulationSpeed = 1.0;
        private double _currentTime;
        private double _totalTime = 1000.0;
        private string _simulationStatus = "Ready";
        private RobotType _selectedRobotType = RobotType.IRB6700;

        // Target position for IK
        private double _targetX = 1500;
        private double _targetY = 1000;
        private double _targetZ = 1750;

        // Target Orientation
        private double _targetRoll = 0;
        private double _targetPitch = 0;
        private double _targetYaw = 0;

        // Current position
        private Vector3D _currentPosition;
        private Model3DGroup _robotModel;

        // Current Orientation
        private Vector3D _currentOrientation;

        // Code validation
        private string _codeToValidate = "";
        private string _selectedRobotModel = "ABB IRB 6700";
        private int _errorCount = 0;
        private int _warningCount = 0;

        public ValidateSimulateViewModel()
        {
            _kinematicsService = new RobotKinematicsService();
            _modelLoader = new RobotModelLoaderService();

            // Initialize collections
            Joints = new ObservableCollection<Joint>();
            JointControls = new ObservableCollection<JointControl>();
            RobotModel = new ObservableCollection<string>
            {
                "AUBO I10",
                "ABB IRB 4600",
                "ABB IRB 6700"
            };
            ValidationIssues = new ObservableCollection<ValidationIssue>();
            Recommendations = new ObservableCollection<string>();

            // Initialize commands
            InitializeCommands();

            // Animation timer
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(5)
            };
            _animationTimer.Tick += AnimationTimer_Tick;

            // Load default robot
            LoadRobot(RobotType.IRB6700);
        }

        #region Properties

        public ObservableCollection<Joint> Joints { get; }
        public ObservableCollection<JointControl> JointControls { get; }
        public ObservableCollection<string> RobotModel { get; }
        public ObservableCollection<ValidationIssue> ValidationIssues { get; }
        public ObservableCollection<string> Recommendations { get; }

        public Model3DGroup RobotModel3D
        {
            get => _robotModel;
            set
            {
                _robotModel = value;
                OnPropertyChanged();
            }
        }

        // Simulation Controls
        public bool IsSimulationRunning
        {
            get => _isSimulationRunning;
            set
            {
                _isSimulationRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanRunSimulation));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool CanRunSimulation => !IsSimulationRunning && Joints.Count > 0;

        public double SimulationSpeed
        {
            get => _simulationSpeed;
            set
            {
                _simulationSpeed = Math.Clamp(value, 0.1, 5.0);
                OnPropertyChanged();
            }
        }

        public double CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = Math.Clamp(value, 0, TotalTime);
                OnPropertyChanged();
            }
        }

        public double TotalTime
        {
            get => _totalTime;
            set
            {
                _totalTime = value;
                OnPropertyChanged();
            }
        }

        public string SimulationStatus
        {
            get => _simulationStatus;
            set
            {
                _simulationStatus = value;
                OnPropertyChanged();
            }
        }

        public double TargetRoll
        {
            get => _targetRoll;
            set
            {
                _targetRoll = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TargetOrientation));
            }
        }

        public double TargetPitch
        {
            get => _targetPitch;
            set
            {
                _targetPitch = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TargetOrientation));
            }
        }

        public double TargetYaw
        {
            get => _targetYaw;
            set
            {
                _targetYaw = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TargetOrientation));
            }
        }

        public Vector3D TargetOrientation => new Vector3D(TargetRoll, TargetPitch, TargetYaw);

        public Vector3D CurrentOrientation
        {
            get => _currentOrientation;
            set
            {
                _currentOrientation = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentRoll));
                OnPropertyChanged(nameof(CurrentPitch));
                OnPropertyChanged(nameof(CurrentYaw));
                OnPropertyChanged(nameof(CurrentOrientationText));
            }
        }

        public double CurrentRoll => CurrentOrientation.X;
        public double CurrentPitch => CurrentOrientation.Y;
        public double CurrentYaw => CurrentOrientation.Z;

        public string CurrentOrientationText => $"R:{CurrentRoll:F1}° P:{CurrentPitch:F1}° Y:{CurrentYaw:F1}°";

        public string CurrentPositionText => $"X:{CurrentX:F1} Y:{CurrentY:F1} Z:{CurrentZ:F1}";

        // Robot State
        public string CurrentPosition => $"X:{CurrentX:F1} Y:{CurrentY:F1} Z:{CurrentZ:F1}";

        public string CurrentSpeed => IsSimulationRunning
            ? $"{SimulationSpeed:F1}x"
            : "0.0x";

        public Vector3D CurrentPosition3D
        {
            get => _currentPosition;
            set
            {
                _currentPosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentX));
                OnPropertyChanged(nameof(CurrentY));
                OnPropertyChanged(nameof(CurrentZ));
                OnPropertyChanged(nameof(CurrentPosition));
                OnPropertyChanged(nameof(DistanceToTarget));
            }
        }

        public double CurrentX => CurrentPosition3D.X;
        public double CurrentY => CurrentPosition3D.Y;
        public double CurrentZ => CurrentPosition3D.Z;

        public double TargetX
        {
            get => _targetX;
            set
            {
                _targetX = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TargetPosition));
                OnPropertyChanged(nameof(TargetPositionPoint));
                OnPropertyChanged(nameof(DistanceToTarget));
            }
        }

        public double TargetY
        {
            get => _targetY;
            set
            {
                _targetY = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TargetPosition));
                OnPropertyChanged(nameof(TargetPositionPoint));
                OnPropertyChanged(nameof(DistanceToTarget));
            }
        }

        public double TargetZ
        {
            get => _targetZ;
            set
            {
                _targetZ = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TargetPosition));
                OnPropertyChanged(nameof(TargetPositionPoint));
                OnPropertyChanged(nameof(DistanceToTarget));
            }
        }

        public Vector3D TargetPosition => new Vector3D(TargetX, TargetY, TargetZ);
        public Point3D TargetPositionPoint => new Point3D(TargetX, TargetY, TargetZ);

        public double DistanceToTarget
        {
            get
            {
                if (Joints.Count == 0) return 0;
                double[] angles = Joints.Select(j => j.Angle).ToArray();
                return _kinematicsService.DistanceFromTarget(
                    Joints,
                    TargetPosition,
                    TargetOrientation,
                    angles
                );
            }
        }

        // Code Validation
        public string CodeToValidate
        {
            get => _codeToValidate;
            set
            {
                _codeToValidate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCodeToValidate));
            }
        }

        public bool HasCodeToValidate => !string.IsNullOrWhiteSpace(CodeToValidate);

        public string SelectedRobotModel
        {
            get => _selectedRobotModel;
            set
            {
                _selectedRobotModel = value;
                OnPropertyChanged();

                // Switch robot based on selection
                if (value == "ABB IRB 6700")
                    LoadRobot(RobotType.IRB6700);
                else if (value == "ABB IRB 4600")
                    LoadRobot(RobotType.IRB4600);
                else if (value == "AUBO I10")
                    LoadRobot(RobotType.AUBO_I10);
            }
        }

        public int ErrorCount
        {
            get => _errorCount;
            set
            {
                _errorCount = value;
                OnPropertyChanged();
            }
        }

        public int WarningCount
        {
            get => _warningCount;
            set
            {
                _warningCount = value;
                OnPropertyChanged();
            }
        }

        // Analysis
        public string EstimatedExecutionTime => $"{TotalTime:F1}s";
        public string MotionComplexity => Joints.Count > 0 ? "Medium" : "N/A";
        public string SafetyScore => "85/100";
        public string EfficiencyRating => "Good";

        #endregion

        #region Commands

        public ICommand RunSimulationCommand { get; private set; }
        public ICommand StopSimulationCommand { get; private set; }
        public ICommand PauseSimulationCommand { get; private set; }
        public ICommand ResetSimulationCommand { get; private set; }
        public ICommand HomeViewCommand { get; private set; }
        public ICommand ResetViewCommand { get; private set; }
        public ICommand LoadCodeCommand { get; private set; }
        public ICommand ValidateCodeCommand { get; private set; }
        public ICommand ClearCodeCommand { get; private set; }

        private void InitializeCommands()
        {
            RunSimulationCommand = new RelayCommand(ExecuteRunSimulation, () => CanRunSimulation);
            StopSimulationCommand = new RelayCommand(ExecuteStopSimulation, () => IsSimulationRunning);
            PauseSimulationCommand = new RelayCommand(ExecutePauseSimulation, () => IsSimulationRunning);
            ResetSimulationCommand = new RelayCommand(ExecuteResetSimulation);
            HomeViewCommand = new RelayCommand(ExecuteHomeView);
            ResetViewCommand = new RelayCommand(ExecuteResetView);
            LoadCodeCommand = new RelayCommand(ExecuteLoadCode);
            ValidateCodeCommand = new RelayCommand(ExecuteValidateCode, () => HasCodeToValidate);
            ClearCodeCommand = new RelayCommand(ExecuteClearCode);
        }

        private void ExecuteRunSimulation()
        {
            IsSimulationRunning = true;
            SimulationStatus = "Running";
            _animationTimer.Start();
        }

        private void ExecuteStopSimulation()
        {
            IsSimulationRunning = false;
            SimulationStatus = "Stopped";
            _animationTimer.Stop();
            CurrentTime = 0;
        }

        private void ExecutePauseSimulation()
        {
            IsSimulationRunning = false;
            SimulationStatus = "Paused";
            _animationTimer.Stop();
        }

        private void ExecuteResetSimulation()
        {
            ExecuteStopSimulation();

            foreach (var joint in Joints)
            {
                joint.Angle = 0;
            }

            UpdateForwardKinematics();
            SimulationStatus = "Ready";
        }

        private void ExecuteHomeView()
        {
            // Placeholder - will be handled in code-behind
        }

        private void ExecuteResetView()
        {
            // Placeholder - will be handled in code-behind
        }

        private void ExecuteLoadCode()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Robot Code Files (*.mod;*.src;*.prg)|*.mod;*.src;*.prg|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    CodeToValidate = File.ReadAllText(dialog.FileName);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error loading file: {ex.Message}",
                        "Load Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteValidateCode()
        {
            ValidationIssues.Clear();
            Recommendations.Clear();

            if (string.IsNullOrWhiteSpace(CodeToValidate))
            {
                return;
            }

            // Simple validation logic
            var lines = CodeToValidate.Split('\n');
            ErrorCount = 0;
            WarningCount = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Check for common issues
                if (trimmed.Contains("MoveL") || trimmed.Contains("MoveJ"))
                {
                    if (!trimmed.Contains(";"))
                    {
                        ValidationIssues.Add(new ValidationIssue
                        {
                            Severity = "Warning",
                            Message = "Missing semicolon",
                            Description = $"Line: {trimmed}"
                        });
                        WarningCount++;
                    }
                }

                if (trimmed.Contains("GOTO"))
                {
                    ValidationIssues.Add(new ValidationIssue
                    {
                        Severity = "Warning",
                        Message = "GOTO statement detected",
                        Description = "Consider using structured control flow instead"
                    });
                    WarningCount++;
                }
            }

            if (ValidationIssues.Count == 0)
            {
                Recommendations.Add("Code structure looks good");
                Recommendations.Add("Consider adding error handling");
                Recommendations.Add("Optimize motion paths for better cycle time");
            }

            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(WarningCount));
        }

        private void ExecuteClearCode()
        {
            CodeToValidate = "";
            ValidationIssues.Clear();
            Recommendations.Clear();
            ErrorCount = 0;
            WarningCount = 0;
        }

        #endregion

        #region Methods

        private void LoadRobot(RobotType robotType)
        {
            try
            {
                if (IsSimulationRunning)
                    ExecuteStopSimulation();

                Joints.Clear();
                JointControls.Clear();

                
                string basePath = Path.Combine(
                    Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName,
                    "3D_model"  
                );

                
                System.Diagnostics.Debug.WriteLine($"Loading from: {basePath}");

                var loadedJoints = _modelLoader.LoadRobotModels(robotType, basePath);

                foreach (var joint in loadedJoints)
                {
                    joint.PropertyChanged += Joint_PropertyChanged;
                    Joints.Add(joint);

                    // Create UI control for joint
                    JointControls.Add(new JointControl
                    {
                        Name = $"J{joint.Index}",
                        MinValue = joint.AngleMin,
                        MaxValue = joint.AngleMax,
                        CurrentValue = joint.Angle,
                        Joint = joint
                    });
                }

                RobotModel3D = _modelLoader.GetAllModels(loadedJoints, robotType);

                UpdateForwardKinematics();
                SimulationStatus = $"Loaded {robotType}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error loading robot: {ex.Message}",
                    "Load Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        private void Joint_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Joint.Angle) && !IsSimulationRunning)
            {
                UpdateForwardKinematics();
            }
        }

        private void UpdateForwardKinematics()
        {
            if (Joints.Count < 6) return;

            double[] angles = Joints.Select(j => j.Angle).ToArray();
            var (position, orientation) = _kinematicsService.ForwardKinematics(Joints, angles);

            CurrentPosition3D = position;
            CurrentOrientation = orientation;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // Update time
            CurrentTime += 0.01 * SimulationSpeed;

            if (CurrentTime >= TotalTime)
            {
                ExecuteStopSimulation();
                SimulationStatus = "Completed";
                return;
            }

            // Perform IK iteration với orientation
            double[] currentAngles = Joints.Select(j => j.Angle).ToArray();

            double[] newAngles = _kinematicsService.InverseKinematics(
                Joints,
                TargetPosition,
                TargetOrientation,
                currentAngles,
                10 // Nhiều iterations hơn mỗi frame
            );

            // Smooth transition - lerp angles
            double alpha = 0.2 * SimulationSpeed; // Smooth factor
            for (int i = 0; i < 6; i++)
            {
                double currentAngle = Joints[i].Angle;
                double targetAngle = newAngles[i];
                Joints[i].Angle = currentAngle + (targetAngle - currentAngle) * alpha;
            }

            UpdateForwardKinematics();

            // Check if reached target
            double distance = _kinematicsService.DistanceFromTarget(
                Joints,
                TargetPosition,
                TargetOrientation,
                Joints.Select(j => j.Angle).ToArray()
            );

            if (distance < 1)
            {
                ExecuteStopSimulation();
                SimulationStatus = "Target Reached";
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    // Helper classes
    public class JointControl : INotifyPropertyChanged
    {
        private double _currentValue;
        public string Name { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public Joint Joint { get; set; }

        public double CurrentValue
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                if (Joint != null)
                {
                    Joint.Angle = value;
                }
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ValidationIssue
    {
        public string Severity { get; set; }
        public string Message { get; set; }
        public string Description { get; set; }
    }
}