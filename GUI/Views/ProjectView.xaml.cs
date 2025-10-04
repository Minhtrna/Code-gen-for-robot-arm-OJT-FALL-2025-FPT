using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GUI.ViewModels;

namespace GUI.Views
{
    public partial class ProjectView : UserControl
    {
        private ProjectViewModel _viewModel;

        public ProjectView()
        {
            InitializeComponent();
            _viewModel = new ProjectViewModel();
            DataContext = _viewModel;
            
            // Subscribe to the Unloaded event instead of overriding OnUnloaded
            this.Unloaded += ProjectView_Unloaded;
        }

        private void ChatInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                // Send message on Enter (without Shift)
                if (_viewModel?.SendMessageCommand?.CanExecute(null) == true)
                {
                    _viewModel.SendMessageCommand.Execute(null);
                }
                e.Handled = true;
            }
        }

        private void ProjectView_Unloaded(object sender, RoutedEventArgs e)
        {
            // Clean up resources when view is unloaded
            _viewModel?.Dispose();
            
            // Unsubscribe from event to prevent memory leaks
            this.Unloaded -= ProjectView_Unloaded;
        }
    }
}