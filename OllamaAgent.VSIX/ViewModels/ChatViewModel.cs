using OllamaAgent.VSIX.Models;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OllamaAgent.VSIX.ViewModels
{
	public class ChatViewModel : ViewModelBase
	{
		public ChatViewModel(OllamaAgent.VSIX.OllamaModelService ollamaModelService) : base(ollamaModelService)
		{
			_ = SafeLoadAsync();
		}

		private string _input;
		public string Input
		{
			get => _input;
			set
			{
				_input = value;
				OnPropertyChanged();
				if (_sendCommand is AsyncRelayCommand arc)
					arc.RaiseCanExecuteChanged();
			}
		}

		public ObservableCollection<ChatMessage> ChatHistory { get; } = new ObservableCollection<ChatMessage>();

		private ICommand _sendCommand;
		public ICommand SendCommand =>
			_sendCommand ??= new AsyncRelayCommand(SendMessageAsync, () => !string.IsNullOrWhiteSpace(SelectedModel));		

		private ICommand _settingsCommand;
		public ICommand SettingsCommand =>
			_settingsCommand ??= new AsyncRelayCommand(async () => { await Task.CompletedTask; });

		private ICommand _newThreadCommand;
		public ICommand NewThreadCommand =>
			_newThreadCommand ??= new AsyncRelayCommand(ClearChatAsync);

		private async Task ClearChatAsync()
		{
			ChatHistory.Clear();
			await Task.CompletedTask;
		}

		public async Task SendMessageAsync()
		{
			var userInput = Input;
			if (string.IsNullOrWhiteSpace(userInput) || string.IsNullOrWhiteSpace(SelectedModel))
				return;

			ChatHistory.Add(new ChatMessage { Sender = "You", Message = userInput });
			Input = string.Empty;

			var response = await OllamaService.GenerateCompletionAsync(Endpoint, SelectedModel, userInput);
			if (!string.IsNullOrWhiteSpace(response))
			{
				ChatHistory.Add(new ChatMessage { Sender = SelectedModel, Message = response });
			}
			else
			{
				ChatHistory.Add(new ChatMessage { Sender = "System", Message = "No response from model." });
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
