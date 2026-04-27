using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OllamaAgent.VSIX
{
	public class ChatMessage
	{
		public string Sender { get; set; }
		public string Message { get; set; }
		public string Display => $"{Sender}: {Message}";
	}

	public class ChatViewModel : INotifyPropertyChanged
	{
		private string _input;
		public string Input
		{
			get => _input;
			set { _input = value; OnPropertyChanged(); }
		}

		public ObservableCollection<ChatMessage> ChatHistory { get; } = new ObservableCollection<ChatMessage>();

		private readonly OllamaModelService _modelService = new OllamaModelService();

		public ObservableCollection<string> Models { get; } = new ObservableCollection<string>();

		private string _selectedModel;
		public string SelectedModel
		{
			get => _selectedModel;
			set { _selectedModel = value; OnPropertyChanged(); }
		}

		private string _endpoint = "http://localhost:11434";
		public string Endpoint
		{
			get => _endpoint;
			set { _endpoint = value; OnPropertyChanged(); }
		}

		public async Task LoadModelsAsync()
		{
			Models.Clear();
			var models = await _modelService.GetModelsAsync(Endpoint);
			foreach (var m in models)
				Models.Add(m);
			if (Models.Count > 0 && (string.IsNullOrWhiteSpace(SelectedModel) || !Models.Contains(SelectedModel)))
			{
				SelectedModel = Models[0];
			}
		}

		public async Task SendMessageAsync()
		{
			var userInput = Input;
			if (string.IsNullOrWhiteSpace(userInput) || string.IsNullOrWhiteSpace(SelectedModel))
				return;

			ChatHistory.Add(new ChatMessage { Sender = "You", Message = userInput });
			Input = string.Empty;

			var response = await _modelService.GenerateCompletionAsync(Endpoint, SelectedModel, userInput);
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
