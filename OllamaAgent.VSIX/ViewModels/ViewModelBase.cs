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
	public IViewModelStateStore ViewModelStateStore { get; }
	private readonly CancellationTokenSource _monitorCts = new CancellationTokenSource();

	// FIX #2: Guard against concurrent SafeLoadAsync calls racing on the shared ModelStore
	private int _isLoading = 0;

	public ViewModelBase(IOllamaAgentService ollamaAgentService, IOllamaModelService ollamaModelService, OllamaAgentVSIXPackage package, IModelStore modelStore, IViewModelStateStore viewModelStateStore)
	{
		if (modelStore == null)
			throw new ArgumentNullException(nameof(modelStore), "ModelStore cannot be null. Check your DI or constructor calls.");
		if (viewModelStateStore == null)
			throw new ArgumentNullException(nameof(viewModelStateStore));
		OllamaAgentService = ollamaAgentService;
		OllamaModelService = ollamaModelService;
		Package = package;
		ModelStore = modelStore;
		ViewModelStateStore = viewModelStateStore;

		// FIX #1: Subscribe to ModelStore.PropertyChanged so that when any ViewModel
		// mutates SelectedChatModel or Models on the shared store, all other ViewModels
		// bound to those properties get notified and their UI updates too.
		ModelStore.PropertyChanged += ModelStore_PropertyChanged;
		ViewModelStateStore.PropertyChanged += ViewModelStateStore_PropertyChanged;

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

	// FIX #1: Forward ModelStore property changes as this ViewModel's own
	// PropertyChanged notifications so WPF bindings on Models and SelectedChatModel update.
	private void ModelStore_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(IModelStore.SelectedChatModel))
			OnPropertyChanged(nameof(SelectedChatModel));

		if (e.PropertyName == nameof(IModelStore.SelectedCompletionModel))
			OnPropertyChanged(nameof(SelectedCompletionModel));

		if (e.PropertyName == nameof(IModelStore.Models))
			OnPropertyChanged(nameof(Models));
	}

	private void ViewModelStateStore_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(e.PropertyName))
		{
			OnPropertyChanged(nameof(Status));
			OnPropertyChanged(nameof(TestConnectionMessage));
			OnPropertyChanged(nameof(OllamaEndpoint));
			OnPropertyChanged(nameof(ModelsDirectory));
			OnPropertyChanged(nameof(ChatsDirectory));
			OnPropertyChanged(nameof(ExtensionEnabled));
			OnPropertyChanged(nameof(AutoAttachActiveDocument));
			OnPropertyChanged(nameof(ReferenceSolutionEnabled));
			OnPropertyChanged(nameof(AgentEnabled));
			return;
		}

		OnPropertyChanged(e.PropertyName);

		if (e.PropertyName == nameof(IViewModelStateStore.Status) ||
			e.PropertyName == nameof(IViewModelStateStore.ExtensionEnabled))
		{
			OnPropertyChanged(nameof(AgentEnabled));
		}

		if (e.PropertyName == nameof(IViewModelStateStore.Status))
		{
			StatusChanged?.Invoke(this, Status);
		}
	}

	public bool AgentEnabled => ExtensionEnabled;

	public ObservableCollection<LLM> Models
	{
		get
		{
			if (ModelStore == null)
				throw new InvalidOperationException("ModelStore is null. ViewModelBase must be constructed with a valid IModelStore.");
			return ModelStore.Models;
		}
	}

	public virtual LLM SelectedChatModel
	{
		get => ModelStore.SelectedChatModel;
		set
		{
			if (ModelStore.SelectedChatModel != value)
			{
				ModelStore.SelectedChatModel = value;
				Settings.Default.SelectedChatModel = value?.Name;
				Settings.Default.Save();
			}
		}
	}
	public virtual LLM SelectedCompletionModel
	{
		get => ModelStore.SelectedCompletionModel;
		set
		{
			if (ModelStore.SelectedCompletionModel != value)
			{
				ModelStore.SelectedCompletionModel = value;
				Settings.Default.SelectedCompletionModel = value?.Name;
				Settings.Default.Save();
			}
		}
	}

	private void RestoreSelectedModels()
	{
		var persistedChatModel = Settings.Default.SelectedChatModel;
		var persistedCompletionModel = Settings.Default.SelectedCompletionModel;
		var currentChatModel = ModelStore.SelectedChatModel?.Name;
		var currentCompletionModel = ModelStore.SelectedCompletionModel?.Name;

		var chatModelName = !string.IsNullOrWhiteSpace(persistedChatModel)
			? persistedChatModel
			: currentChatModel;

		var completionModelName = !string.IsNullOrWhiteSpace(persistedCompletionModel)
			? persistedCompletionModel
			: currentCompletionModel;

		SelectedChatModel = !string.IsNullOrWhiteSpace(chatModelName)
			? Models.FirstOrDefault(m => m.Name == chatModelName)
			: null;

		SelectedCompletionModel = !string.IsNullOrWhiteSpace(completionModelName)
			? Models.FirstOrDefault(m => m.Name == completionModelName)
			: null;

		if (SelectedChatModel is null)
		{
			SelectedChatModel = Models.FirstOrDefault();
		}

		if (SelectedCompletionModel is null)
		{
			SelectedCompletionModel = Models.FirstOrDefault();
		}
	}

	// Call this ONLY after models are loaded/refreshed
	public void LoadSettings()
	{
		ViewModelStateStore.ModelsDirectory = Settings.Default.ModelsDirectory;
		OnPropertyChanged(nameof(ModelsDirectory));

		RestoreSelectedModels();

		var persistedEndpoint = OllamaAgent.VSIX.Properties.Settings.Default.Endpoint;
		if (!string.IsNullOrWhiteSpace(persistedEndpoint))
		{
			ViewModelStateStore.OllamaEndpoint = persistedEndpoint;
			OnPropertyChanged(nameof(OllamaEndpoint));
		}
		else
		{
			ViewModelStateStore.OllamaEndpoint = "http://localhost:11434";
			OnPropertyChanged(nameof(OllamaEndpoint));
		}

		ViewModelStateStore.ExtensionEnabled = OllamaAgent.VSIX.Properties.Settings.Default.ExtensionEnabled;
		OnPropertyChanged(nameof(ExtensionEnabled));

		var persistedModelsDirectory = OllamaAgent.VSIX.Properties.Settings.Default.ModelsDirectory;

		if (!string.IsNullOrWhiteSpace(persistedModelsDirectory))
		{
			ViewModelStateStore.ModelsDirectory = persistedModelsDirectory;
			OnPropertyChanged(nameof(ModelsDirectory));
		}

		var persistedChatsDirectory = OllamaAgent.VSIX.Properties.Settings.Default.ChatsDirectory;
		if (!string.IsNullOrWhiteSpace(persistedChatsDirectory))
		{
			ViewModelStateStore.ChatsDirectory = persistedChatsDirectory;
			OnPropertyChanged(nameof(ChatsDirectory));
		}

		ViewModelStateStore.AutoAttachActiveDocument = OllamaAgent.VSIX.Properties.Settings.Default.AutoAttachActiveDocument;
		OnPropertyChanged(nameof(AutoAttachActiveDocument));
	}

	public void SaveSettings()
	{
		Settings.Default.ModelsDirectory = ModelsDirectory;
		Settings.Default.SelectedChatModel = SelectedChatModel?.Name;
		Settings.Default.SelectedCompletionModel = SelectedCompletionModel?.Name;
		Settings.Default.Endpoint = OllamaEndpoint;
		Settings.Default.ExtensionEnabled = ExtensionEnabled;
		Settings.Default.Save();
	}

	public bool ExtensionEnabled
	{
		get => ViewModelStateStore.ExtensionEnabled;
		set
		{
			if (ViewModelStateStore.ExtensionEnabled != value)
			{
				ViewModelStateStore.ExtensionEnabled = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(AgentEnabled));
				// Persist immediately
				OllamaAgent.VSIX.Properties.Settings.Default.ExtensionEnabled = value;
				OllamaAgent.VSIX.Properties.Settings.Default.Save();
			}
		}
	}

	protected async Task EnsureServerOnlineAsync()
	{
		await CheckOllamaOnlineAsync();
		if (Status != ServerStatus.Online && Status != ServerStatus.Starting)
		{
			// Try to start the server monitor (which will attempt to bring it online)
			_ = StartServerMonitorAsync(_monitorCts.Token);
		}

		OnPropertyChanged(nameof(Status));
	}

	public ServerStatus Status
	{
		get => ViewModelStateStore.Status;
		protected set
		{
			if (ViewModelStateStore.Status != value)
			{
				ViewModelStateStore.Status = value;
			}
		}
	}

	public string TestConnectionMessage
	{
		get => ViewModelStateStore.TestConnectionMessage;
		set
		{
			if (ViewModelStateStore.TestConnectionMessage != value)
			{
				ViewModelStateStore.TestConnectionMessage = value;
				OnPropertyChanged();
			}
		}
	}

	public string OllamaEndpoint
	{
		get => ViewModelStateStore.OllamaEndpoint;
		set
		{
			if (ViewModelStateStore.OllamaEndpoint != value)
			{
				ViewModelStateStore.OllamaEndpoint = value;
				OnPropertyChanged(nameof(OllamaEndpoint));
			}
		}
	}

	public string ModelsDirectory
	{
		get => ViewModelStateStore.ModelsDirectory;
		set
		{
			if (ViewModelStateStore.ModelsDirectory != value)
			{
				ViewModelStateStore.ModelsDirectory = value;
				OnPropertyChanged();
				// Save to user settings
				Settings.Default.ModelsDirectory = value;
				Settings.Default.Save();
				// Refresh models for all windows
				_ = SafeLoadAsync();
			}
		}
	}

	public string ChatsDirectory
	{
		get => ViewModelStateStore.ChatsDirectory;
		set
		{
			if (ViewModelStateStore.ChatsDirectory != value)
			{
				ViewModelStateStore.ChatsDirectory = value;
				OnPropertyChanged();
				Settings.Default.ChatsDirectory = value;
				Settings.Default.Save();
			}
		}
	}

	public bool AutoAttachActiveDocument
	{
		get => ViewModelStateStore.AutoAttachActiveDocument;
		set
		{
			if (ViewModelStateStore.AutoAttachActiveDocument != value)
			{
				ViewModelStateStore.AutoAttachActiveDocument = value;
				OnPropertyChanged();
				// Persist immediately
				OllamaAgent.VSIX.Properties.Settings.Default.AutoAttachActiveDocument = value;
				OllamaAgent.VSIX.Properties.Settings.Default.Save();
				SaveSettings();
			}
		}
	}

	public bool ReferenceSolutionEnabled
	{
		get => ViewModelStateStore.ReferenceSolutionEnabled;
		set
		{
			if (ViewModelStateStore.ReferenceSolutionEnabled != value)
			{
				ViewModelStateStore.ReferenceSolutionEnabled = value;
				OnPropertyChanged();
			}
		}
	}

	private static readonly HttpClient _httpClient = new HttpClient();

	private const string OllamaProcessName = "ollama";
	private const string OllamaStartArguments = "serve";

	public async Task CheckOllamaOnlineAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			if (!ExtensionEnabled)
			{
				Status = ServerStatus.Disabled;
				return;
			}

			var url = OllamaEndpoint.TrimEnd('/') + "/api/tags";
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(TimeSpan.FromSeconds(10)); // Increased timeout to 10s
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
			if (!ExtensionEnabled)
			{
				Status = ServerStatus.Disabled;
				var result = System.Windows.Forms.MessageBox.Show(
					"The extension is currently disabled. Would you like to enable it now?",
					"Extension Disabled",
					System.Windows.Forms.MessageBoxButtons.YesNo,
					System.Windows.Forms.MessageBoxIcon.Question);
				if (result == System.Windows.Forms.DialogResult.Yes)
				{
					ExtensionEnabled = true;
					await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
					VsShellUtilities.ShowToolsOptionsPage<OllamaAgentOptionsPage>();
				}
				return;
			}
			try
			{
				if (Status == ServerStatus.Online)
				{
					// Double-check server is really online
					await CheckOllamaOnlineAsync();
					if (Status == ServerStatus.Online)
					{
						// Show custom dialog: Stop, Restart, Cancel
						await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
						var dialog = new OllamaAgent.VSIX.Views.ServerActionDialog();
						var owner = System.Windows.Application.Current?.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive);
						if (owner != null)
							dialog.Owner = owner;
						var dialogResult = dialog.ShowDialog();
						var action = dialog.Result;
						if (action == OllamaAgent.VSIX.Views.ServerActionDialog.ServerActionResult.Stop)
						{
							foreach (var proc in System.Diagnostics.Process.GetProcessesByName(OllamaProcessName))
							{
								try { proc.Kill(); } catch { }
							}
							Status = ServerStatus.Offline;
							TestConnectionMessage = "Ollama server stopped.";
							return;
						}
						else if (action == OllamaAgent.VSIX.Views.ServerActionDialog.ServerActionResult.Restart)
						{
							foreach (var proc in System.Diagnostics.Process.GetProcessesByName(OllamaProcessName))
							{
								try { proc.Kill(); } catch { }
							}
							Status = ServerStatus.Offline;
							TestConnectionMessage = "Restarting Ollama server...";
							await Task.Delay(1000);
							// Continue to start server below
						}
						else // Cancel
						{
							TestConnectionMessage = "Ollama server is already online.";
							return;
						}
					}
					else
					{
						// If server is not really online, continue to start
					}
				}

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

				// Aggressive polling: every 2s for up to 60s or until online
				const int maxTries = 30; // 30 tries at 2s = 60s
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
					// After 15 tries (30s), update message to indicate still starting
					if (i == 15)
					{
						TestConnectionMessage = "Ollama server is still starting...";
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
		});

	private IAsyncRelayCommand _testConnectionCommand;
	public IAsyncRelayCommand TestConnectionCommand =>
		_testConnectionCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			if (!ExtensionEnabled)
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
		(parameter) => ExtensionEnabled);

	private IAsyncRelayCommand _refreshModelsCommand;
	public IAsyncRelayCommand RefreshModelsCommand =>
		_refreshModelsCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			if (!ExtensionEnabled)
			{
				return;
			}
			await SafeLoadAsync();
		},
		(parameter) => ExtensionEnabled);

	// FIX #2: Interlocked flag prevents two ViewModels both responding to StatusChanged
	// at the same time and racing to Clear() + repopulate the shared Models collection.
	public async Task SafeLoadAsync()
	{
		if (Interlocked.CompareExchange(ref _isLoading, 1, 0) != 0)
			return; // Another ViewModel (or a re-entrant call) is already loading

		try
		{
			if (!ExtensionEnabled)
			{
				await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				Models.Clear();
				return;
			}

			if (OllamaModelService == null)
			{
				System.Diagnostics.Debug.WriteLine("OllamaModelService is null in SafeLoadAsync.");
				return;
			}

			var models = await OllamaModelService.GetModelsAsync(OllamaEndpoint);

			// Update status based on model retrieval
			if (models.Count > 0)
			{
				if (Status != ServerStatus.Online)
					Status = ServerStatus.Online;
			}
			else
			{
				await CheckOllamaOnlineAsync();
			}

			await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var oldSelectedChat = SelectedChatModel?.Name ?? Settings.Default.SelectedChatModel;
			var oldSelectedCompletion = SelectedCompletionModel?.Name ?? Settings.Default.SelectedCompletionModel;
			Models.Clear();
			foreach (var m in models)
			{
				if (!string.IsNullOrWhiteSpace(m))
					Models.Add(new LLM { Name = m });
			}

			SelectedChatModel = !string.IsNullOrWhiteSpace(oldSelectedChat)
				? Models.FirstOrDefault(x => x.Name == oldSelectedChat)
				: null;

			SelectedCompletionModel = !string.IsNullOrWhiteSpace(oldSelectedCompletion)
				? Models.FirstOrDefault(x => x.Name == oldSelectedCompletion)
				: null;

			if (SelectedChatModel == null && Models.Count > 0)
			{
				SelectedChatModel = Models[0];
			}

			if (SelectedCompletionModel == null && Models.Count > 0)
			{
				SelectedCompletionModel = Models[0];
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error loading models: {ex.Message}");
		}
		finally
		{
			// Always release the lock, even if an exception occurred
			Interlocked.Exchange(ref _isLoading, 0);
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

	private ICommand _selectChatDirectoryCommand;
	public ICommand SelectChatDirectoryCommand =>
		_selectChatDirectoryCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			await Task.Yield();
			await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var oldFolder = ChatsDirectory;
			using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
			{
				dialog.Description = "Select Ollama Chats Folder";
				dialog.SelectedPath = oldFolder;
				if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					var newFolder = dialog.SelectedPath;
					if (!string.Equals(oldFolder, newFolder, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(oldFolder))
					{
						try
						{
							var oldFiles = System.IO.Directory.Exists(oldFolder)
								? System.IO.Directory.GetFiles(oldFolder, "*.json")
								: Array.Empty<string>();

							if (oldFiles.Length > 0)
							{
								var result = System.Windows.Forms.MessageBox.Show(
									$"You have {oldFiles.Length} chat thread(s) in your previous folder.\n\nDo you want to move them to the new folder?",
									"Move Chat Threads",
									System.Windows.Forms.MessageBoxButtons.YesNo,
									System.Windows.Forms.MessageBoxIcon.Question);

								if (result == System.Windows.Forms.DialogResult.Yes)
								{
									System.IO.Directory.CreateDirectory(newFolder);
									foreach (var file in oldFiles)
									{
										var dest = System.IO.Path.Combine(newFolder, System.IO.Path.GetFileName(file));
										System.IO.File.Copy(file, dest, overwrite: false);
									}
								}
								else
								{
									var confirm = System.Windows.Forms.MessageBox.Show(
										"Are you sure? If you continue, you will lose access to your previous chat threads in the old folder.",
										"Confirm Folder Change",
										System.Windows.Forms.MessageBoxButtons.YesNo,
										System.Windows.Forms.MessageBoxIcon.Warning);

									if (confirm != System.Windows.Forms.DialogResult.Yes)
										return; // Cancel folder change
								}
							}
						}
						catch (Exception ex)
						{
							System.Windows.Forms.MessageBox.Show(
								$"Error migrating chat threads: {ex.Message}",
								"Migration Error",
								System.Windows.Forms.MessageBoxButtons.OK,
								System.Windows.Forms.MessageBoxIcon.Error);
						}
					}

					ChatsDirectory = newFolder;
				}
			}
		});


	private IAsyncRelayCommand _extensionEnabledToggledCommand;
	public IAsyncRelayCommand ExtensionEnabledToggledCommand =>
		_extensionEnabledToggledCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			if (ExtensionEnabled)
			{
				if (Status == ServerStatus.Disabled)
				{
					Status = ServerStatus.Unknown;
				}

				await EnsureServerOnlineAsync();
			}
			else
			{
				Status = ServerStatus.Disabled;
			}

				OnPropertyChanged(nameof(Status));
		});

	public event PropertyChangedEventHandler PropertyChanged;
	protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			// FIX #1: Unsubscribe to avoid memory leaks / phantom notifications
			// after this ViewModel is disposed.
			ModelStore.PropertyChanged -= ModelStore_PropertyChanged;
			ViewModelStateStore.PropertyChanged -= ViewModelStateStore_PropertyChanged;
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