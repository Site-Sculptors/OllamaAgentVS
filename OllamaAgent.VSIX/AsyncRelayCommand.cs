using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OllamaAgent.VSIX
{
	public class AsyncRelayCommand : ICommand
	{
		private readonly Func<Task> _execute;
		private readonly Func<object, Task> _executeWithParam;
		private readonly Func<bool> _canExecute;
		private readonly Func<object, bool> _canExecuteWithParam;

		public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public AsyncRelayCommand(Func<object, Task> execute, Func<object, bool> canExecute = null)
		{
			_executeWithParam = execute;
			_canExecuteWithParam = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			if (_canExecuteWithParam != null)
				return _canExecuteWithParam(parameter);
			if (_canExecute != null)
				return _canExecute();
			return true;
		}

		public async void Execute(object parameter)
		{
			try
			{
				if (_executeWithParam != null)
					await _executeWithParam(parameter);
				else if (_execute != null)
					await _execute();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Command failed: {ex.Message}");
			}
		}

		public event EventHandler CanExecuteChanged;

		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}