using System.Windows.Controls;
using GUI.ViewModels;

namespace GUI.Views
{
    public partial class DeviceView : UserControl
    {
        public DeviceView()
        {
            InitializeComponent();
            DataContext = new DeviceViewModel();
        }
    }
}