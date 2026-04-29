using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using OllamaAgent.VSIX.Properties;

namespace OllamaAgent.VSIX.ViewModels
{
	public class OllamaOptionsViewModel : ViewModelBase
	{
		// Parameterless constructor for XAML
		public OllamaOptionsViewModel() : this(new OllamaAgent.VSIX.OllamaModelService(), (OllamaAgentVSIXPackage)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(OllamaAgentVSIXPackage))) { }

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

		private string _modelsDirectory;
		public string ModelsDirectory
		{
			get => _modelsDirectory;
			set
			{
				if (_modelsDirectory != value)
				{
					_modelsDirectory = value;
					OnPropertyChanged();
					// Save to user settings
					Settings.Default.ModelsDirectory = value;
					Settings.Default.Save();
				}
			}
		}

		private string _testConnectionMessage;
		public string TestConnectionMessage
		{
			get => _testConnectionMessage;
			set { _testConnectionMessage = value; OnPropertyChanged(); }
		}

		public OllamaOptionsViewModel(OllamaAgent.VSIX.OllamaModelService ollamaModelService, OllamaAgentVSIXPackage package) : base(ollamaModelService, package)
		{
			// Load from user settings
			ModelsDirectory = Settings.Default.ModelsDirectory;
		}

		private ICommand _selectModelsDirectoryCommand;
		public ICommand SelectModelsDirectoryCommand =>
			_selectModelsDirectoryCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			using (var dialog = new FolderBrowserDialog())
			{
				dialog.Description = "Select Ollama Models Directory";
				dialog.SelectedPath = ModelsDirectory;
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					ModelsDirectory = dialog.SelectedPath;
					await SafeLoadAsync();
				}
			}
		});

		private ICommand _testConnectionCommand;
		public ICommand TestConnectionCommand =>
			_testConnectionCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			try
			{
				var models = await OllamaService.GetModelsAsync(Endpoint);
				if (models.Count > 0)
				{
					TestConnectionMessage = $"Connection OK. {models.Count} model(s) found.";
				}
				else
				{
					TestConnectionMessage = "Connection OK, but no models found.";
				}
			}
			catch (Exception ex)
			{
				TestConnectionMessage = $"Connection failed: {ex.Message}";
			}
		});

		public void Save()
		{
			// optional persistence later
		}
	}
}