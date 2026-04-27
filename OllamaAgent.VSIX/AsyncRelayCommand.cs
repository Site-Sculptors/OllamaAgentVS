using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OllamaAgent.VSIX
{
	public class AsyncRelayCommand : ICommand
	{
		private readonly Func<Task> _execute;
		private readonly Func<bool> _canExecute;

		public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter)
			=> _canExecute == null || _canExecute();

		public async void Execute(object parameter)
		{
			try
			{
				await _execute();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Command failed: {ex.Message}");
			}
		}

		public event EventHandler CanExecuteChanged;
	}
}