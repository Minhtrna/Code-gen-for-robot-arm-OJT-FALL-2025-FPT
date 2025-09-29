using System.Windows.Controls;
using GUI.ViewModels;

namespace GUI.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}