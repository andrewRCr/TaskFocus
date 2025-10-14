using System;
using System.Windows.Input;

namespace TaskFocusDesktop.Commands
{
    public class RelayCommand : ICommand
    {
        private Action<object?> _execute;
        private Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        // defines the method to be called when the command is invoked
        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        // defines the method that determines whether the command can execute in its current state
        public bool CanExecute(object? parameter)
        {
            // if no method passed, assume true;
            return _canExecute == null || _canExecute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            // only hook events when appropriate
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
