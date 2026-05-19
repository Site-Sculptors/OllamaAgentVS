using System.ComponentModel;

using OllamaAgent.VSIX.Enums;

namespace OllamaAgent.VSIX.Services
{
	public class ViewModelStateStore : IViewModelStateStore
	{
		private ServerStatus _status;
		private string _testConnectionMessage;
		private string _ollamaEndpoint = "http://localhost:11434";
		private string _modelsDirectory;
		private string _chatsDirectory;
		private bool _extensionEnabled = true;
		private bool _autoAttachActiveDocument = true;
		private bool _referenceSolutionEnabled = OllamaAgent.VSIX.Properties.Settings.Default.ReferenceSolutionEnabled;

		public ServerStatus Status
		{
			get => _status;
			set
			{
				if (_status != value)
				{
					_status = value;
					OnPropertyChanged(nameof(Status));
				}
			}
		}

		public string TestConnectionMessage
		{
			get => _testConnectionMessage;
			set
			{
				if (_testConnectionMessage != value)
				{
					_testConnectionMessage = value;
					OnPropertyChanged(nameof(TestConnectionMessage));
				}
			}
		}

		public string OllamaEndpoint
		{
			get => _ollamaEndpoint;
			set
			{
				if (_ollamaEndpoint != value)
				{
					_ollamaEndpoint = value;
					OnPropertyChanged(nameof(OllamaEndpoint));
				}
			}
		}

		public string ModelsDirectory
		{
			get => _modelsDirectory;
			set
			{
				if (_modelsDirectory != value)
				{
					_modelsDirectory = value;
					OnPropertyChanged(nameof(ModelsDirectory));
				}
			}
		}

		public string ChatsDirectory
		{
			get => _chatsDirectory;
			set
			{
				if (_chatsDirectory != value)
				{
					_chatsDirectory = value;
					OnPropertyChanged(nameof(ChatsDirectory));
				}
			}
		}

		public bool ExtensionEnabled
		{
			get => _extensionEnabled;
			set
			{
				if (_extensionEnabled != value)
				{
					_extensionEnabled = value;
					OnPropertyChanged(nameof(ExtensionEnabled));
				}
			}
		}

		public bool AutoAttachActiveDocument
		{
			get => _autoAttachActiveDocument;
			set
			{
				if (_autoAttachActiveDocument != value)
				{
					_autoAttachActiveDocument = value;
					OnPropertyChanged(nameof(AutoAttachActiveDocument));
				}
			}
		}

		public bool ReferenceSolutionEnabled
		{
			get => _referenceSolutionEnabled;
			set
			{
				if (_referenceSolutionEnabled != value)
				{
					_referenceSolutionEnabled = value;
					OllamaAgent.VSIX.Properties.Settings.Default.ReferenceSolutionEnabled = value;
					OllamaAgent.VSIX.Properties.Settings.Default.Save();
					OnPropertyChanged(nameof(ReferenceSolutionEnabled));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
