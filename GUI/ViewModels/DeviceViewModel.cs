using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GUI.Helpers;
using GUI.Models;
using GUI.Services;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Windows.Threading;
using System.Windows;
using System.Reflection;

namespace GUI.ViewModels
{
    public class DeviceViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly DeviceService _deviceService;
        private Device _selectedDevice;
        private string _statusMessage = "Ready to scan for devices";
        private bool _canConnect = false;
        private bool _canDisconnect = false;
        private bool _canCapture = false;

        public ObservableCollection<Device> AvailableDevices { get; set; }

        public Device SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                OnPropertyChanged();
                UpdateButtons();
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

        public bool CanCapture
        {
            get => _canCapture;
            set { _canCapture = value; OnPropertyChanged(); }
        }

        public ICommand ConnectDeviceCommand { get; }
        public ICommand DisconnectDeviceCommand { get; }
        public ICommand RefreshDevicesCommand { get; }
        public ICommand CaptureImageCommand { get; }

        public DeviceViewModel()
        {
            _deviceService = new DeviceService();
            AvailableDevices = new ObservableCollection<Device>();

            ConnectDeviceCommand = new RelayCommand(async _ => await ConnectDevice());
            DisconnectDeviceCommand = new RelayCommand(async _ => await DisconnectDevice());
            RefreshDevicesCommand = new RelayCommand(async _ => await RefreshDevices());
            CaptureImageCommand = new RelayCommand(async _ => await CaptureImage());

            StatusMessage = "Click 'Refresh Devices' to scan for cameras";
        }

        private async Task ConnectDevice()
        {
            if (SelectedDevice == null) return;

            try
            {
                StatusMessage = $"Connecting to {SelectedDevice.Name}...";
                System.Diagnostics.Debug.WriteLine($"[DeviceViewModel] ===== CONNECTING DEVICE =====");
                System.Diagnostics.Debug.WriteLine($"[DeviceViewModel] Device: {SelectedDevice.Name} ({SelectedDevice.SerialNumber})");
                
                bool success = await Task.Run(() => _deviceService.ConnectDevice(SelectedDevice));
                
                if (success)
                {
                    // Update device status
                    SelectedDevice.IsConnected = true;
                    SelectedDevice.ConnectionStatus = "Connected";
                    SelectedDevice.Status = "Connected";
                    
                    StatusMessage = $"Successfully connected to {SelectedDevice.Name}";
                    UpdateButtons();
                    
                    System.Diagnostics.Debug.WriteLine($"[DeviceViewModel] ===== CONNECTION SUCCESS =====");
               
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DeviceViewModel] ===== CONNECTION FAILED =====");
                    throw new InvalidOperationException("Failed to connect to device");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to connect: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[DeviceViewModel] Connection exception: {ex.Message}");
            }
        }

        private async Task DisconnectDevice()
        {
            if (SelectedDevice == null) return;

            try
            {
                StatusMessage = $"Disconnecting from {SelectedDevice.Name}...";
                System.Diagnostics.Debug.WriteLine($"[DeviceViewModel] ===== DISCONNECTING DEVICE =====");
                
                bool success = await Task.Run(() => _deviceService.DisconnectDevice(SelectedDevice));
                
                if (success)
                {
                    // Update device status
                    SelectedDevice.IsConnected = false;
                    SelectedDevice.ConnectionStatus = "Disconnected";
                    SelectedDevice.Status = "Available";
                    
                    StatusMessage = $"Disconnected from {SelectedDevice.Name}";
                    UpdateButtons();
                    
                    System.Diagnostics.Debug.WriteLine($"[DeviceViewModel] ===== DISCONNECTION SUCCESS =====");
                    
                    
                }
                else
                {
                    throw new InvalidOperationException("Failed to disconnect from device");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to disconnect: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[DeviceViewModel] Disconnection exception: {ex.Message}");
            }
        }

        private async Task RefreshDevices()
        {
            try
            {
                StatusMessage = "Scanning for devices...";
                System.Diagnostics.Debug.WriteLine("[DeviceViewModel] Starting device refresh...");
                
                // Run device discovery in background thread
                var devices = await Task.Run(() => _deviceService.DiscoverAllDevices());
                
                System.Diagnostics.Debug.WriteLine($"[DeviceViewModel] Found {devices.Count} devices");
                
                // UPDATE UI ON UI THREAD
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableDevices.Clear();
                    foreach (var device in devices)
                    {
                        AvailableDevices.Add(device);
                        System.Diagnostics.Debug.WriteLine($"[DeviceViewModel] Added device: {device.Name} - Connected: {device.IsConnected}");
                    }
                });
                
                StatusMessage = $"Found {devices.Count} device(s)";
                UpdateButtons();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error scanning devices: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[DeviceViewModel] Refresh failed: {ex.Message}");
            }


        }

        private async Task CaptureImage()
        {
            if (SelectedDevice == null || !SelectedDevice.IsConnected) return;

            try
            {
                StatusMessage = $"Capturing image from {SelectedDevice.Name}...";
                
                string filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), 
                    $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                
                bool success = await Task.Run(() => _deviceService.CaptureImage(SelectedDevice, filename));
                
                if (success)
                {
                    StatusMessage = $"Image captured and saved to: {filename}";
                }
                else
                {
                    StatusMessage = "Failed to capture image";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error capturing image: {ex.Message}";
            }
        }

        private void UpdateButtons()
        {
            CanConnect = SelectedDevice != null && !SelectedDevice.IsConnected;
            CanDisconnect = SelectedDevice != null && SelectedDevice.IsConnected;
            CanCapture = SelectedDevice != null && SelectedDevice.IsConnected;
            
            System.Diagnostics.Debug.WriteLine($"[DeviceViewModel] UpdateButtons - Selected: {SelectedDevice?.Name}, Connected: {SelectedDevice?.IsConnected}, CanConnect: {CanConnect}, CanDisconnect: {CanDisconnect}");
        }

        public void Dispose()
        {
            _deviceService?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


    }
}