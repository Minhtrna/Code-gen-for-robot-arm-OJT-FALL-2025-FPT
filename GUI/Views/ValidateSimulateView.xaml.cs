using System.Windows;
using System.Windows.Controls;
using GUI.ViewModels;

namespace GUI.Views
{
    public partial class ValidateSimulateView : UserControl
    {
        public ValidateSimulateView()
        {
            InitializeComponent();
            DataContext = new ValidateSimulateViewModel();
        }

        private void ZoomExtents_Click(object sender, RoutedEventArgs e)
        {
            SimulationViewport.ZoomExtents();
        }
    }
}