using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GUI.ViewModels;

namespace GUI.Views
{
    public partial class ProjectView : UserControl
    {
        private ProjectViewModel _viewModel;
        private bool _isDisposed = false;

        public ProjectView()
        {
            InitializeComponent();
            _viewModel = new ProjectViewModel();
            DataContext = _viewModel;
            
            // DON'T subscribe to Unloaded - it fires when switching tabs!
            // Instead, handle cleanup when the entire window closes
            System.Diagnostics.Debug.WriteLine("[ProjectView] Created and ViewModel initialized");
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

        // Only dispose when the control is truly being destroyed, not just hidden
        ~ProjectView()
        {
            if (!_isDisposed)
            {
                System.Diagnostics.Debug.WriteLine("[ProjectView] Finalizer called - disposing ViewModel");
                _viewModel?.Dispose();
                _isDisposed = true;
            }
        }
    }
}