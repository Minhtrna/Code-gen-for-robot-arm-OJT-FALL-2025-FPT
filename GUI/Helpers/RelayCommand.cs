using System;
using System.Windows.Input;

namespace GUI.Helpers
{
    /// <summary>
    /// RelayCommand cơ bản, tương thích với các phiên bản cũ trong project
    /// và hỗ trợ thêm các action không tham số (Action) cùng generic RelayCommand<T>.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _executeWithParam;
        private readonly Predicate<object> _canExecuteWithParam;
        private readonly Action _executeNoParam;
        private readonly Func<bool> _canExecuteNoParam;

        // --- Giữ tương thích với code cũ ---
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _executeWithParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteWithParam = canExecute;
        }

        // --- Hỗ trợ method không tham số ---
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _executeNoParam = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteNoParam = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecuteNoParam != null)
                return _canExecuteNoParam();

            return _canExecuteWithParam?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            if (_executeNoParam != null)
                _executeNoParam();
            else
                _executeWithParam?.Invoke(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// RelayCommand có tham số kiểu cụ thể (ví dụ RelayCommand&lt;RobotType&gt;).
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
                return true;

            return _canExecute(ConvertParameter(parameter));
        }

        public void Execute(object parameter)
        {
            _execute(ConvertParameter(parameter));
        }

        private static T ConvertParameter(object parameter)
        {
            // Null => default(T)
            if (parameter == null)
                return default;

            // Nếu parameter đúng kiểu
            if (parameter is T variable)
                return variable;

            // Cố gắng ép kiểu nếu có thể (ví dụ string -> enum)
            return (T)Convert.ChangeType(parameter, typeof(T));
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
