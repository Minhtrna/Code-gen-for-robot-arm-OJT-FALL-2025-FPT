using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GUI.Helpers;

namespace GUI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _statusMessage = "Ready.";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public ICommand ConnectCameraCommand { get; }
        public ICommand ConnectRobotCommand { get; }

        public MainViewModel()
        {
            ConnectCameraCommand = new RelayCommand(_ => ConnectCamera());
            ConnectRobotCommand = new RelayCommand(_ => ConnectRobot());
        }

        private void ConnectCamera()
        {
            StatusMessage = "Camera connected!";
        }

        private void ConnectRobot()
        {
            StatusMessage = "Robot connected!";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
