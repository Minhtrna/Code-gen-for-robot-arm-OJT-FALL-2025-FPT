using System.Windows.Controls;
using System.Windows.Input;
using GUI.ViewModels;

namespace GUI.Views
{
    public partial class ProjectView : UserControl
    {
        public ProjectView()
        {
            InitializeComponent();
            DataContext = new ProjectViewModel();
        }

        private void ChatInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                var viewModel = DataContext as ProjectViewModel;
                if (viewModel?.SendMessageCommand.CanExecute(null) == true)
                {
                    viewModel.SendMessageCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}