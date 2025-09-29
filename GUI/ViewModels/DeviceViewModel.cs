using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GUI.Helpers;
using GUI.Models;
using System.Threading.Tasks;
using System;

namespace GUI.ViewModels
{
    public class DeviceViewModel : INotifyPropertyChanged
    {
        private Device _selectedDevice;
        private string _statusMessage = "Ready to connect devices";
        private bool _canConnect = false;
        private bool _canDisconnect = false;

        public ObservableCollection<Device> AvailableDevices { get; set; }

        public Device SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                OnPropertyChanged();
                UpdateConnectionButtons();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool CanConnect
        {
            get => _canConnect;
            set { _canConnect = value; OnPropertyChanged(); }
        }

        public bool CanDisconnect
        {
            get => _canDisconnect;
            set { _canDisconnect = value; OnPropertyChanged(); }
        }

        public ICommand ConnectDeviceCommand { get; }
        public ICommand DisconnectDeviceCommand { get; }
        public ICommand RefreshDevicesCommand { get; }

        public DeviceViewModel()
        {
            InitializeDemoDevices();

            ConnectDeviceCommand = new RelayCommand(async _ => await ConnectDevice());
            DisconnectDeviceCommand = new RelayCommand(async _ => await DisconnectDevice());
            RefreshDevicesCommand = new RelayCommand(async _ => await RefreshDevices());
        }

        private void InitializeDemoDevices()
        {
            AvailableDevices = new ObservableCollection<Device>
            {
                new Device
                {
                    Name = "USB Camera #1",
                    Type = "Camera",
                    Status = "Available",
                    ConnectionStatus = "Disconnected",
                    IsConnected = false
                },
                new Device
                {
                    Name = "Robot Arm V2",
                    Type = "Robot",
                    Status = "Available",
                    ConnectionStatus = "Disconnected",
                    IsConnected = false
                },
                new Device
                {
                    Name = "IP Camera 192.168.1.100",
                    Type = "Camera",
                    Status = "Available",
                    ConnectionStatus = "Disconnected",
                    IsConnected = false
                },
                new Device
                {
                    Name = "Servo Controller",
                    Type = "Controller",
                    Status = "Offline",
                    ConnectionStatus = "Disconnected",
                    IsConnected = false
                }
            };
        }

        private async Task ConnectDevice()
        {
            if (SelectedDevice == null) return;

            StatusMessage = $"Connecting to {SelectedDevice.Name}...";
            
            // Simulate connection delay
            await Task.Delay(2000);

            SelectedDevice.IsConnected = true;
            SelectedDevice.ConnectionStatus = "Connected";
            SelectedDevice.Status = "Connected";
            
            StatusMessage = $"Successfully connected to {SelectedDevice.Name}";
            UpdateConnectionButtons();
        }

        private async Task DisconnectDevice()
        {
            if (SelectedDevice == null) return;

            StatusMessage = $"Disconnecting from {SelectedDevice.Name}...";
            
            await Task.Delay(1000);

            SelectedDevice.IsConnected = false;
            SelectedDevice.ConnectionStatus = "Disconnected";
            SelectedDevice.Status = "Available";
            
            StatusMessage = $"Disconnected from {SelectedDevice.Name}";
            UpdateConnectionButtons();
        }

        private async Task RefreshDevices()
        {
            StatusMessage = "Refreshing device list...";
            
            await Task.Delay(1500);
            
            // Simulate finding new devices
            if (AvailableDevices.Count < 6)
            {
                AvailableDevices.Add(new Device
                {
                    Name = $"New Device #{AvailableDevices.Count + 1}",
                    Type = "Sensor",
                    Status = "Available",
                    ConnectionStatus = "Disconnected",
                    IsConnected = false
                });
            }
            
            StatusMessage = "Device list refreshed";
        }

        private void UpdateConnectionButtons()
        {
            CanConnect = SelectedDevice != null && !SelectedDevice.IsConnected;
            CanDisconnect = SelectedDevice != null && SelectedDevice.IsConnected;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}