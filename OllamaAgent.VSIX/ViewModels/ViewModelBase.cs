using CommunityToolkit.Mvvm.Input;

using Microsoft.VisualStudio.Settings;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.ViewModels;

using OllamaAgent.VSIX.Enums;
using OllamaAgent.VSIX.Properties;

using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Input;

public class ViewModelBase : INotifyPropertyChanged
{
	private static ViewModelBase _instance;
	public static ViewModelBase Instance
	{
		get
		{
			if (_instance == null)
				throw new InvalidOperationException("ViewModelBase.Instance has not been initialized. Call InitializeSingleton first.");
			return _instance;
		}
	}

public static void InitializeSingleton(OllamaModelService ollamaModelService, OllamaAgentVSIXPackage package)
{
	if (_instance == null)
		_instance = new ChatViewModel(ollamaModelService, package);
}

	public ObservableCollection<string> Models { get; } = new ObservableCollection<string>();
	public event EventHandler<ServerStatus> StatusChanged;

	public OllamaModelService OllamaService { get; }
	public OllamaAgentVSIXPackage Package { get; }
	private readonly CancellationTokenSource _monitorCts = new CancellationTokenSource();

	public ViewModelBase(OllamaModelService ollamaModelService, OllamaAgentVSIXPackage package)
	{
		OllamaService = ollamaModelService;
		Package = package;
		LoadSettings();

		_=StartServerMonitorAsync(_monitorCts.Token);

		StatusChanged += (s, status) =>
		{
			if (status == ServerStatus.Online)
			{
				_ = SafeLoadAsync();
			}
		};
		if (_instance == null)
			_instance = this;
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


	public void LoadSettings()
	{
		var persistedEndpoint = OllamaAgent.VSIX.Properties.Settings.Default.Endpoint;
		if (!string.IsNullOrWhiteSpace(persistedEndpoint))
		{
			_ollamaEndpoint = persistedEndpoint;
			OnPropertyChanged(nameof(OllamaEndpoint));
		}
		else
		{
			_ollamaEndpoint = "http://localhost:11434";
			OnPropertyChanged(nameof(OllamaEndpoint));
		}
		_agentEnabled = OllamaAgent.VSIX.Properties.Settings.Default.EnableAgent;
		var persistedModelsDirectory = OllamaAgent.VSIX.Properties.Settings.Default.ModelsDirectory;
		if (!string.IsNullOrWhiteSpace(persistedModelsDirectory))
		{
			_modelsDirectory = persistedModelsDirectory;
			OnPropertyChanged(nameof(ModelsDirectory));
		}
	}

	public void SaveSettings()
	{
		OllamaAgent.VSIX.Properties.Settings.Default.Endpoint = OllamaEndpoint;
		OllamaAgent.VSIX.Properties.Settings.Default.EnableAgent = AgentEnabled;
		OllamaAgent.VSIX.Properties.Settings.Default.Save();
	}

	private bool _agentEnabled = true;
	public bool AgentEnabled
	{
		get => _agentEnabled;
		set
		{
			if (_agentEnabled != value)
			{
				_agentEnabled = value;
				OnPropertyChanged();
				// Persist immediately
				OllamaAgent.VSIX.Properties.Settings.Default.EnableAgent = value;
				OllamaAgent.VSIX.Properties.Settings.Default.Save();
			}
		}
	}

	private ServerStatus _status;
	public ServerStatus Status
	{
		get => _status;
		protected set
		{
			if (_status != value)
			{
				_status = value;
				OnPropertyChanged(nameof(Status));
				StatusChanged?.Invoke(this, _status);
			}
		}
	}

	private string _testConnectionMessage;
	public string TestConnectionMessage
	{
		get => _testConnectionMessage;
		set { _testConnectionMessage = value; OnPropertyChanged(); }
	}

	private string _ollamaEndpoint = "http://localhost:11434";
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
				// Refresh models for all windows
				_ = ViewModelBase.Instance.SafeLoadAsync();
			}
		}
	}

	private static readonly HttpClient _httpClient = new HttpClient();

	private const string OllamaProcessName = "ollama"; // Adjust if the executable name is different
	private const string OllamaStartArguments = "serve"; // Adjust if arguments are different

	public async Task CheckOllamaOnlineAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var url = OllamaEndpoint.TrimEnd('/') + "/api/tags";
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(TimeSpan.FromSeconds(2));
			using var response = await _httpClient.GetAsync(url, cts.Token);
			if (response.IsSuccessStatusCode)
			{
				Status = ServerStatus.Online;
			}
			else
			{
				Status = ServerStatus.Offline;
			}
		}
		catch
		{
			Status = ServerStatus.Offline;
		}
	}

private async Task StartServerMonitorAsync(CancellationToken token)
{
	// Aggressive polling: every 2s for up to 30s or until online
	using (var aggressiveCts = CancellationTokenSource.CreateLinkedTokenSource(token))
	{
		aggressiveCts.CancelAfter(TimeSpan.FromSeconds(30));
		while (!aggressiveCts.Token.IsCancellationRequested && !token.IsCancellationRequested)
		{
			await CheckOllamaOnlineAsync(aggressiveCts.Token);
			if (Status == ServerStatus.Online)
				break;
			await Task.Delay(2000, aggressiveCts.Token);
		}
	}
	// Normal polling: every 30s
	while (!token.IsCancellationRequested)
	{
		await CheckOllamaOnlineAsync(token);
		for (int i = 0; i < 30; i++)
		{
			if (token.IsCancellationRequested) return;
			await Task.Delay(1000, token);
		}
	}
}

	private IAsyncRelayCommand _startServerCommand;
	public IAsyncRelayCommand StartServerCommand =>
		_startServerCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			if (!AgentEnabled)
			{
				Status = ServerStatus.Offline;
				return;
			}
			try
			{
				Status = ServerStatus.Starting;
				var processStartInfo = new System.Diagnostics.ProcessStartInfo
				{
					FileName = OllamaProcessName,
					Arguments = OllamaStartArguments,
					UseShellExecute = false,
					CreateNoWindow = true
				};

				// Set OLLAMA_MODELS from settings if available
				var modelsDir = OllamaAgent.VSIX.Properties.Settings.Default.ModelsDirectory;
				if (!string.IsNullOrWhiteSpace(modelsDir))
				{
					processStartInfo.EnvironmentVariables["OLLAMA_MODELS"] = modelsDir;
				}

				System.Diagnostics.Process.Start(processStartInfo);

				// Aggressive polling: every 2s for up to 30s or until online
				const int maxTries = 15;
				bool online = false;
				for (int i = 0; i < maxTries; i++)
				{
					await Task.Delay(2000);
					await CheckOllamaOnlineAsync();
					if (Status == ServerStatus.Online)
					{
						online = true;
						break;
					}
				}
				if (!online)
				{
					Status = ServerStatus.Offline;
					TestConnectionMessage = "Failed to start Ollama server: Timed out waiting for server to come online.";
				}
				// Resume normal polling
				_ = StartServerMonitorAsync(_monitorCts.Token);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Failed to start Ollama server: {ex.Message}");
				Status = ServerStatus.Offline;
			}
		},
		(parameter) => AgentEnabled);

	private IAsyncRelayCommand _testConnectionCommand;
	public IAsyncRelayCommand TestConnectionCommand =>
		_testConnectionCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			if (!AgentEnabled)
			{
				TestConnectionMessage = "Agent is disabled.";
				Status = ServerStatus.Offline;
				return;
			}
			try
			{
				var models = await OllamaService.GetModelsAsync(OllamaEndpoint);
				if (models.Count > 0)
				{
					TestConnectionMessage = $"Connection OK. {models.Count} model(s) found.";
					Status = ServerStatus.Online;
					await SafeLoadAsync();
				}
				else
				{
					Status = ServerStatus.Online;
					TestConnectionMessage = "Connection OK, but no models found.";
					await ViewModelBase.Instance.SafeLoadAsync();
				}
			}
			catch (Exception ex)
			{
				Status = ServerStatus.Offline;
				TestConnectionMessage = $"Connection failed: {ex.Message}";
			}
		},
		(parameter) => AgentEnabled);

	private IAsyncRelayCommand _refreshModelsCommand;
	public IAsyncRelayCommand RefreshModelsCommand =>
		_refreshModelsCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			if (!AgentEnabled)
			{
				return;
			}
			await SafeLoadAsync();
		},
		(parameter) => AgentEnabled);


	public async Task SafeLoadAsync()
	{
		if (!AgentEnabled)
		{
			await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			Models.Clear();
			return;
		}
		try
		{
			var models = await OllamaService.GetModelsAsync(OllamaEndpoint);
			await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			Models.Clear();
			foreach (var m in models)
				Models.Add(m);
			if (Models.Count > 0 &&
				(string.IsNullOrWhiteSpace(SelectedModel) || !Models.Contains(SelectedModel)))
			{
				SelectedModel = Models[0];
			}
		}
		catch (Exception ex)
		{
			// Log or handle exception as needed
			System.Diagnostics.Debug.WriteLine($"Error loading models: {ex.Message}");
		}
	}

	private AsyncRelayCommand _settingsCommand;
	public AsyncRelayCommand SettingsCommand =>
		_settingsCommand ??= new AsyncRelayCommand(async (parameter) =>
		{
			await Task.Yield();
			await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			System.Diagnostics.Debug.WriteLine(Package == null ? "[OllamaAgent] Package is null" : "[OllamaAgent] Package is set");
			if (Package != null)
			{
				Package.ShowOptionPage(typeof(OllamaAgent.VSIX.OllamaAgentOptionsPage));
				System.Diagnostics.Debug.WriteLine("[OllamaAgent] ShowOptionPage called");
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("[OllamaAgent] ShowOptionPage NOT called because Package is null");
			}
		});

	public event PropertyChangedEventHandler PropertyChanged;
	protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			_monitorCts.Cancel();
			_monitorCts.Dispose();
		}
	}

	public void Dispose()
	{
		SaveSettings();
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
