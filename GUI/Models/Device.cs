using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace GUI.Models
{
    public class Device : INotifyPropertyChanged
    {
        private string _name;
        private string _type;
        private string _serialNumber;
        private string _ipAddress;
        private string _status;
        private string _connectionStatus;
        private bool _isConnected;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public string SerialNumber
        {
            get => _serialNumber;
            set { _serialNumber = value; OnPropertyChanged(); }
        }

        public string IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set { _connectionStatus = value; OnPropertyChanged(); }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set 
            { 
                _isConnected = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public SolidColorBrush StatusColor
        {
            get
            {
                return IsConnected ? new SolidColorBrush(Colors.Green) : 
                       Status == "Available" ? new SolidColorBrush(Colors.Orange) : 
                       new SolidColorBrush(Colors.Red);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}