using CommunityToolkit.Mvvm.Input;

using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.ViewModels;

using OllamaAgent.VSIX.Enums;
using OllamaAgent.VSIX.Properties;
using OllamaAgent.VSIX.Services;

using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Input;



using OllamaAgent.VSIX.Models;

public class ViewModelBase : INotifyPropertyChanged
{
	public event EventHandler<ServerStatus> StatusChanged;

	public IOllamaAgentService OllamaAgentService { get; }
	public IOllamaModelService OllamaModelService { get; }
	public OllamaAgentVSIXPackage Package { get; }
	public IModelStore ModelStore { get; }
	private readonly CancellationTokenSource _monitorCts = new CancellationTokenSource();

   public ViewModelBase(IOllamaAgentService ollamaAgentService, IOllamaModelService ollamaModelService, OllamaAgentVSIXPackage package, IModelStore modelStore)
   {
	   if (modelStore == null)
		   throw new ArgumentNullException(nameof(modelStore), "ModelStore cannot be null. Check your DI or constructor calls.");
	   OllamaAgentService = ollamaAgentService;
	   OllamaModelService = ollamaModelService;
	   Package = package;
	   ModelStore = modelStore;

	   LoadSettings();
	   _ = StartServerMonitorAsync(_monitorCts.Token);

	   StatusChanged += (s, status) =>
	   {
		   if (status == ServerStatus.Online)
		   {
			   _ = SafeLoadAsync();
		   }
	   };
   }

public ObservableCollection<LLM> Models
{
	get
	{
		if (ModelStore == null)
			throw new InvalidOperationException("ModelStore is null. ViewModelBase must be constructed with a valid IModelStore.");
		return ModelStore.Models;
	}
}

	public virtual LLM SelectedModel
	{
		get => ModelStore.SelectedModel;
		set
		{
			if (ModelStore.SelectedModel != value)
			{
				ModelStore.SelectedModel = value;
				OnPropertyChanged();
			}
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
				_ = SafeLoadAsync();
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
	  if (OllamaModelService == null)
	  {
		 TestConnectionMessage = "Model service is not available.";
		 Status = ServerStatus.Offline;
		 return;
	  }
	  if (string.IsNullOrWhiteSpace(OllamaEndpoint))
	  {
		 TestConnectionMessage = "Ollama endpoint is not set.";
		 Status = ServerStatus.Offline;
		 return;
	  }
	  try
	  {
		 var models = await OllamaModelService.GetModelsAsync(OllamaEndpoint);
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
			await SafeLoadAsync();
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
			if (OllamaModelService == null)
			{
				System.Diagnostics.Debug.WriteLine("OllamaModelService is null in SafeLoadAsync.");
				return;
			}
			var models = await OllamaModelService.GetModelsAsync(OllamaEndpoint);
			await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		   var oldSelected = SelectedModel?.Name;
			  Models.Clear();
		   foreach (var m in models)
		   {
			   if (!string.IsNullOrWhiteSpace(m))
				   Models.Add(new LLM { Name = m });
		   }
		   if (!string.IsNullOrWhiteSpace(oldSelected))
		   {
			   var match = Models.FirstOrDefault(x => x.Name == oldSelected);
			   if (match != null)
				   SelectedModel = match;
		   }
		   else if (Models.Count > 0)
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
			VsShellUtilities.ShowToolsOptionsPage<OllamaAgentOptionsPage>();
			System.Diagnostics.Debug.WriteLine("[OllamaAgent] ShowOptionPage called");
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
