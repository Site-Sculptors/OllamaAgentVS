using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OllamaAgent.VSIX
{
	public class OllamaOptionsViewModel : INotifyPropertyChanged
	{
		private readonly OllamaModelService _service = new OllamaModelService();

		public ObservableCollection<string> Models { get; } = new ObservableCollection<string>();

		private string _endpoint = "http://localhost:11434";
		public string Endpoint
		{
			get => _endpoint;
			set
			{
				_endpoint = value;
				OnPropertyChanged();
			}
		}

		private string _selectedModel;
		public string SelectedModel
		{
			get => _selectedModel;
			set
			{
				_selectedModel = value;
				OnPropertyChanged();
			}
		}

		private bool _agentEnabled = true;
		public bool AgentEnabled
		{
			get => _agentEnabled;
			set
			{
				_agentEnabled = value;
				OnPropertyChanged();
			}
		}

		private ICommand _refreshModelsCommand;
		public ICommand RefreshModelsCommand =>
			_refreshModelsCommand ??= new AsyncRelayCommand(RefreshModelsAsync);

		private ICommand _testConnectionCommand;
		public ICommand TestConnectionCommand =>
			_testConnectionCommand ??= new AsyncRelayCommand(TestConnectionAsync);

		public async Task LoadAsync()
		{
			Models.Clear();

			var models = await _service.GetModelsAsync(Endpoint);

			foreach (var m in models)
				Models.Add(m);

			if (Models.Count > 0 &&
				(string.IsNullOrWhiteSpace(SelectedModel) || !Models.Contains(SelectedModel)))
			{
				SelectedModel = Models[0];
			}
		}

		private async Task RefreshModelsAsync()
		{
			await LoadAsync();
		}

		private async Task TestConnectionAsync()
		{
			try
			{
				var models = await _service.GetModelsAsync(Endpoint);

				System.Diagnostics.Debug.WriteLine(
					models.Count > 0
						? "Ollama connection OK"
						: "Ollama reachable but no models returned");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ollama connection failed: {ex.Message}");
			}
		}

		public void Save()
		{
			// optional persistence later
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}