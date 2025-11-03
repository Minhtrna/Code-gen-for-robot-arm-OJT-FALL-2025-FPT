using System.Windows.Controls;
using GUI.ViewModels;

namespace GUI.Views
{
    public partial class CameraView : UserControl
    {
        private CameraViewModel _viewModel;

        public CameraView()
        {
            InitializeComponent();
            _viewModel = new CameraViewModel();
            DataContext = _viewModel;
        }


        // Clean up when the view is unloaded
        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel?.Dispose();
        }
    }
}