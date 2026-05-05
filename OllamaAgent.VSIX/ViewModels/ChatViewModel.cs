using CommunityToolkit.Mvvm.Input;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using OllamaAgent.VSIX.Enums;
using OllamaAgent.VSIX.Helpers;
using OllamaAgent.VSIX.Models;
using OllamaAgent.VSIX.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OllamaAgent.VSIX.ViewModels
{
public partial class ChatViewModel : ViewModelBase
	{
	   private static readonly ObservableCollection<ChatMessageBase> _emptyMessages = new ObservableCollection<ChatMessageBase>();
		private readonly IOllamaChatService _ollamaChatService;
		private readonly IChatThreadStore _chatThreadStore;
		private readonly IEditorContextService _editorContextService;
		private readonly Services.CustomInstructionsService _customInstructionsService;
		private readonly ISymbolExtractorService _symbolExtractorService;
		private readonly SolutionTreeService _solutionTreeService = new SolutionTreeService();
		private readonly IOutputWindowContextService _outputWindowContextService;
		private readonly IErrorListService _errorListService;

		public bool CustomInstructionsActive => _customInstructionsService?.IsActive == true;
		public string CustomInstructionsPath => _customInstructionsService?.IsActive == true ? _customInstructionsService.Instructions : null;

		private EnvDTE.Events _dteEvents;
		private EnvDTE.DocumentEvents _documentEvents;
		private EnvDTE.WindowEvents _windowEvents;

		public ChatViewModel(IOllamaChatService ollamaChatService, IOllamaAgentService ollamaAgentService, IOllamaModelService ollamaModelService, OllamaAgentVSIXPackage package, IModelStore modelStore, IViewModelStateStore viewModelStateStore, IEditorContextService editorContextService, IErrorListService errorListService, IOutputWindowContextService outputWindowContextService)
			: base(ollamaAgentService, ollamaModelService, package, modelStore, viewModelStateStore)
		{
			_ollamaChatService = ollamaChatService;
			_chatThreadStore = (IChatThreadStore)((IServiceProvider)package).GetService(typeof(IChatThreadStore));
			_editorContextService = editorContextService;
			_customInstructionsService = (Services.CustomInstructionsService)((IServiceProvider)package).GetService(typeof(Services.CustomInstructionsService));
			_symbolExtractorService = (ISymbolExtractorService)((IServiceProvider)package).GetService(typeof(ISymbolExtractorService));
			_errorListService = errorListService;
			_outputWindowContextService = outputWindowContextService;
			Threads = new ObservableCollection<ChatThread>();
			// Ensure threads are loaded before proceeding
			ThreadHelper.JoinableTaskFactory.Run(async () => await LoadThreadsForCurrentSolutionAsync());

			SetActiveDocumentAsAttachedFile();

			// Subscribe to DTE events for active document tracking
			ThreadHelper.ThrowIfNotOnUIThread();
			var dte = (EnvDTE.DTE)ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE));
			if (dte != null)
			{
				_dteEvents = dte.Events;
				_documentEvents = _dteEvents?.get_DocumentEvents(null);
				_windowEvents = _dteEvents?.get_WindowEvents();
				if (_documentEvents != null)
				{
					_documentEvents.DocumentOpened += DocumentOrWindowChanged;
					_documentEvents.DocumentSaved += DocumentOrWindowChanged;
				}
				if (_windowEvents != null)
				{
					_windowEvents.WindowActivated += WindowActivated;
				}
			}
		}

		public void SetActiveDocumentAsAttachedFile()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var dte = (EnvDTE.DTE)ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE));
			var doc = dte?.ActiveDocument;
			if (doc != null && !string.IsNullOrEmpty(doc.FullName))
			{
				// Prevent duplicate attachments
				if (!Attachments.Any(a => a.Label == "Active Document" && (string)a.Context == doc.FullName))
				{
					Attachments.Add(new Models.AttachmentModel
					{
						Icon = "\uE7C3", // Document icon (Segoe MDL2 Assets)
						Label = "Active Document",
						Context = doc.FullName
					});
				}
			}
		}

		// Handler for document or window change events
		private void DocumentOrWindowChanged(EnvDTE.Document doc)
		{
			TryAutoAttachActiveDocument();
		}

		private void WindowActivated(EnvDTE.Window gotFocus, EnvDTE.Window lostFocus)
		{
			TryAutoAttachActiveDocument();
		}

		private void TryAutoAttachActiveDocument()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (AutoAttachActiveDocument)
			{
				// Remove previous "Active Document" attachment if present
				var toRemove = Attachments.Where(a => a.Label == "Active Document").ToList();
				foreach (var att in toRemove)
					Attachments.Remove(att);
				SetActiveDocumentAsAttachedFile();
			}
		}

		// Attachments chip bar state
		public ObservableCollection<AttachmentModel> Attachments { get; } = new ObservableCollection<Models.AttachmentModel>();

		private IRelayCommand<Models.AttachmentModel> _removeAttachmentCommand;
		public IRelayCommand<Models.AttachmentModel> RemoveAttachmentCommand =>
			_removeAttachmentCommand ??= new CommunityToolkit.Mvvm.Input.RelayCommand<Models.AttachmentModel>(attachment =>
			{
				if (attachment != null && Attachments.Contains(attachment))
					Attachments.Remove(attachment);
			});

		// Attach file command (adds to Attachments)
		private IRelayCommand _attachFileCommand;
		public IRelayCommand AttachFileCommand =>
			_attachFileCommand ??= new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
			{
				var window = System.Windows.Application.Current?.Windows
					.OfType<System.Windows.Window>()
					.SelectMany(w => w.OwnedWindows.Cast<System.Windows.Window>().Concat(new[] { w }))
					.SelectMany(w => w.FindVisualChildren<OllamaAgent.VSIX.Controls.OllamaAgentToolWindowControl>())
					.FirstOrDefault();
				var solutionDir = System.IO.Path.GetDirectoryName(GetBestSolutionPath() ?? "");
				window?.ShowAttachFileDialog(solutionDir, async (filePath) =>
				{
					try
					{
						Attachments.Add(new Models.AttachmentModel
						{
							Icon = "\uE8A5", // File icon (Segoe MDL2 Assets)
							Label = System.IO.Path.GetFileName(filePath),
							Context = filePath
						});
					}
					catch (Exception ex)
					{
						System.Windows.MessageBox.Show($"Failed to read file: {ex.Message}", "Attach File Error");
					}
				});
			});

		// Clear all attachments command
		private IRelayCommand _clearAttachmentsCommand;
		public IRelayCommand ClearAttachmentsCommand =>
			_clearAttachmentsCommand ??= new CommunityToolkit.Mvvm.Input.RelayCommand(() => Attachments.Clear());

		// Slash command autocomplete state for binding
		private ObservableCollection<OllamaAgent.VSIX.Models.SlashCommand> _slashCommandSuggestions = new ObservableCollection<OllamaAgent.VSIX.Models.SlashCommand>();
		public ObservableCollection<OllamaAgent.VSIX.Models.SlashCommand> SlashCommandSuggestions => _slashCommandSuggestions;

		private bool _isSlashCommandPopupOpen;
		public bool IsSlashCommandPopupOpen
		{
			get => _isSlashCommandPopupOpen;
			set { _isSlashCommandPopupOpen = value; OnPropertyChanged(); }
		}

		private int _slashCommandSelectedIndex;
		public int SlashCommandSelectedIndex
		{
			get => _slashCommandSelectedIndex;
			set { _slashCommandSelectedIndex = value; OnPropertyChanged(); }
		}
		public ObservableCollection<ChatThread> Threads { get; }

		private ChatThread _activeThread;
		public ChatThread ActiveThread
		{
			get => _activeThread;
			set
			{
				_activeThread = value;
				RefreshActiveThread();
			}
		}

		public ObservableCollection<ChatMessageBase> ChatHistory => ActiveThread?.Messages ?? _emptyMessages;

		private void RefreshActiveThread()
		{
			OnPropertyChanged(nameof(ActiveThread));
			OnPropertyChanged(nameof(ChatHistory));

			if (_sendCommand is AsyncRelayCommand arc)
				arc.RaiseCanExecuteChanged();
		}

		private async Task LoadThreadsForCurrentSolutionAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			var solutionPath = GetBestSolutionPath();
			List<ChatThread> threads;
			if (!string.IsNullOrWhiteSpace(solutionPath))
				threads = await _chatThreadStore.LoadThreadsForSolutionAsync(solutionPath);
			else
				threads = await _chatThreadStore.LoadGlobalThreadsAsync();

			Threads.Clear();
			foreach (var t in threads.OrderByDescending(t => t.LastActivityAt))
				Threads.Add(t);

			// Only reassign ActiveThread if it's not already valid in the new collection
			var currentMatch = ActiveThread != null ? Threads.FirstOrDefault(t => t.Id == ActiveThread.Id) : null;
			if (currentMatch != null)
			{
				ActiveThread = currentMatch; // repoint to new instance but same thread
				return;                      // don't fall through and overwrite it
			}

			ChatThread match = Threads.FirstOrDefault(t => t.SolutionPath == solutionPath)
							?? Threads.FirstOrDefault();

			if (match != null)
				ActiveThread = match;
			else
				await CreateAndSwitchToNewThreadAsync();
		}

		private async Task SaveThreadAsync(ChatThread thread)
		{
			thread.LastActivityAt = DateTime.UtcNow;
			await _chatThreadStore.SaveThreadAsync(thread);
			// Preserve the current thread selection
			var currentId = thread?.Id;
			var sorted = Threads.OrderByDescending(t => t.LastActivityAt).ToList();
			Threads.Clear();
			foreach (var t in sorted)
				Threads.Add(t);

			if (!string.IsNullOrEmpty(currentId))
			{
				var match = Threads.FirstOrDefault(t => t.Id == currentId);
				if (match != null)
				{
					ActiveThread = match;
				}
			}
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

		private bool _isHistoryVisible;
		public bool IsHistoryVisible
		{
			get => _isHistoryVisible;
			set { _isHistoryVisible = value; OnPropertyChanged(); }
		}

		private IRelayCommand<ChatThread> _openThreadCommand;
		public IRelayCommand<ChatThread> OpenThreadCommand =>
			_openThreadCommand ??= new RelayCommand<ChatThread>(OpenThread);

		private void OpenThread(ChatThread thread)
		{
			if (thread == null)
			{
				return;
			}

			if (ActiveThread != null && string.Equals(ActiveThread.Id, thread.Id, StringComparison.Ordinal))
			{
				IsHistoryVisible = false;
				RefreshActiveThread();
				return;
			}

			ActiveThread = thread;
			IsHistoryVisible = false;
		}

		// SettingsCommand now inherited from ViewModelBase

		private IAsyncRelayCommand _newThreadCommand;
		public IAsyncRelayCommand NewThreadCommand =>
			_newThreadCommand ??= new CommunityToolkit.Mvvm.Input.AsyncRelayCommand<object>(async (parameter) =>
			{
				await CreateAndSwitchToNewThreadAsync();
			});


		private IAsyncRelayCommand _deleteThreadCommand;
		public IAsyncRelayCommand DeleteThreadCommand =>
			_deleteThreadCommand ??= new CommunityToolkit.Mvvm.Input.AsyncRelayCommand<object>(async (parameter) =>
			{
				if (ActiveThread != null)
				{
					var idx = Threads.IndexOf(ActiveThread);
					var threadId = ActiveThread.Id;
					Threads.Remove(ActiveThread);
					await _chatThreadStore.DeleteThreadAsync(threadId);
					if (Threads.Count > 0)
						ActiveThread = Threads[Math.Max(0, Math.Min(idx, Threads.Count - 1))];
					else
						await CreateAndSwitchToNewThreadAsync();
				}
			});

		private IAsyncRelayCommand _chatHistoryCommand;
		public IAsyncRelayCommand ChatHistoryCommand =>
			_chatHistoryCommand ??= new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(async () =>
			{
				if (!IsHistoryVisible)
				{
					await LoadThreadsForCurrentSolutionAsync();
					IsHistoryVisible = true;
				}
				else
				{
					IsHistoryVisible = false;
				}
			});

		// OpenSettingsAsync now inherited from ViewModelBase


		private IAsyncRelayCommand _sendCommand;
		public IAsyncRelayCommand SendCommand =>
			_sendCommand ??= new CommunityToolkit.Mvvm.Input.AsyncRelayCommand<object>(async (parameter) =>
			{
				var userInput = Input;
				if (string.IsNullOrWhiteSpace(userInput) || SelectedChatModel == null || string.IsNullOrWhiteSpace(SelectedChatModel.Name) || ActiveThread == null)
					return;

				// --- Slash Command Detection ---
				string systemInstruction = null;
				string commandUsed = null;
				var trimmedInput = userInput.TrimStart();
				var slash = OllamaAgent.VSIX.Models.SlashCommand.All.FirstOrDefault(cmd => trimmedInput.StartsWith(cmd.Command, StringComparison.OrdinalIgnoreCase));
				if (slash != null)
				{
					systemInstruction = slash.SystemInstruction;
					commandUsed = slash.Command;
					// Remove the command from the input for the user message
					userInput = userInput.Substring(commandUsed.Length).TrimStart();
				}

				// --- @solution Token Detection ---
				string solutionTree = null;
				List<(string FileName, string Content)> relevantFiles = null;
				if (userInput.Contains("@solution", StringComparison.OrdinalIgnoreCase))
				{
					solutionTree = await _solutionTreeService.GetSolutionTreeAsync();
					// Attach relevant file contents (capped)
					relevantFiles = await _solutionTreeService.GetRelevantSolutionFilesWithContentsAsync(userInput, 16000);
				}

				// --- #output Token Detection ---
				string outputPaneName = null;
				string outputContent = null;
				if (userInput.Contains("#output", StringComparison.OrdinalIgnoreCase))
				{
					var (paneName, content) = await _outputWindowContextService.GetBuildOrDebugOutputAsync();
					outputPaneName = paneName;
					outputContent = content;
				}

				// --- Context Injection Phase 1 ---
				var (fileName, language, fileContent, selection) = await _editorContextService.GetActiveDocumentContextAsync();

				// --- Symbol Extraction ---
				string symbolSummary = string.Empty;
				if (!string.IsNullOrEmpty(fileContent) && !string.IsNullOrEmpty(language))
				{
					symbolSummary = await _symbolExtractorService.ExtractSymbolSummaryAsync(fileContent, language);
				}

				// --- Context Construction ---
				string contextBlock = string.Empty;
				bool isFixCommand = string.Equals(commandUsed, "/fix", StringComparison.OrdinalIgnoreCase);
				bool hasSelection = !string.IsNullOrEmpty(selection);
				if (hasSelection)
				{
					contextBlock += $"// Selected code (from {fileName}):\n{selection}\n";
				}
				if (!string.IsNullOrEmpty(fileContent))
				{
					contextBlock += $"// Active file: {fileName} [{language}]\n{fileContent}\n";
				}

				// --- Error List Context Injection for /fix ---
				if (isFixCommand && !hasSelection && !string.IsNullOrEmpty(fileName))
				{
					var errors = await _errorListService.GetErrorsForFileAsync(fileName);
					if (errors != null && errors.Count > 0)
					{
						contextBlock += "// Errors in this file (from Error List):\n";
						foreach (var err in errors)
						{
							contextBlock += $"Line {err.Line}: {err.Message} [{err.Severity}]\n";
						}
					}
				}

				// Append symbol summary if present
				if (!string.IsNullOrEmpty(symbolSummary))
				{
					contextBlock += $"// Symbols in active file:\n{symbolSummary}\n";
				}
				// Inject all attached files' content if present
				if (Attachments != null && Attachments.Count > 0)
				{
					foreach (var attachment in Attachments)
					{
						if (!string.IsNullOrEmpty(attachment.Label) && !string.IsNullOrEmpty(attachment.Content))
						{
							contextBlock += $"// Attached file: {attachment.Label}\n{attachment.Content}\n";
						}
					}
				}
				// Inject solution tree if requested
				if (!string.IsNullOrEmpty(solutionTree))
				{
					contextBlock += $"// Solution file tree:\n{solutionTree}\n";
				}
				// Inject relevant file contents if present
				if (relevantFiles != null && relevantFiles.Count > 0)
				{
					foreach (var (fname, content) in relevantFiles)
					{
						contextBlock += $"// File: {fname}\n";
						// Truncate individual file if huge (shouldn't happen, but safety)
						var safeContent = content.Length > 8000 ? content.Substring(0, 8000) + "\n// ...truncated..." : content;
						contextBlock += safeContent + "\n";
					}
				}
				// Inject output window context if requested
				if (!string.IsNullOrEmpty(outputContent))
				{
					contextBlock += $"// Output window ({outputPaneName}):\n";
					// Truncate output if huge
					var safeOutput = outputContent.Length > 16000 ? outputContent.Substring(0, 16000) + "\n// ...truncated..." : outputContent;
					contextBlock += safeOutput + "\n";
				}
				// Clear all attachments after sending
				Attachments.Clear();

				// --- System Instruction Prepending ---
				string prompt = string.Empty;
				// 1. Custom instructions (if present)
				if (_customInstructionsService != null && _customInstructionsService.IsActive)
				{
					prompt += $"[SYSTEM]\n{_customInstructionsService.Instructions}\n";
				}
				// 2. Slash command transformation
				if (!string.IsNullOrEmpty(systemInstruction))
				{
					prompt += $"[SYSTEM]\n{systemInstruction}\n";
				}
				prompt += contextBlock;

				// --- Conversation Context ---
			   ActiveThread.Messages.Add(new UserChatMessage((commandUsed != null ? commandUsed + " " : "") + userInput));
				Input = string.Empty;
			   var conversation = string.Join("\n", ActiveThread.Messages.Select(m => $"{m.Role}: {m.Content}"));
				prompt += $"Given the following conversation, reply as the assistant. Also, suggest a concise thread title (max 5 words) that summarizes the conversation so far. Format your response as:\nMessage: <your reply>\nTitle: <suggested title>\n\nConversation:\n{conversation}\nUser: {userInput}";

				// --- Streaming response ---
			   IsAwaitingAIResponse = true;
				_stopStreamingCts = new System.Threading.CancellationTokenSource();
				var userMsg = ActiveThread.Messages.LastOrDefault(m => m.Role == ChatRole.User);
			   var aiMsg = new AIChatMessage(string.Empty);
			   ActiveThread.Messages.Add(aiMsg);
				OnPropertyChanged(nameof(ChatHistory));

				// Build chat history for Ollama
			   var chatMessages = ActiveThread.Messages
				   .Select(m => (role: m.Role == ChatRole.User ? "user" : "assistant", content: m.Content))
				   .ToList();

				// Remove the last AI message (the one we're about to stream)
				if (chatMessages.Count > 0 && chatMessages.Last().role == "assistant")
					chatMessages.RemoveAt(chatMessages.Count - 1);


				// System prompt as a system message if present
				if (!string.IsNullOrEmpty(prompt))
					chatMessages.Insert(0, ("system", prompt));

				// Streaming callback
			   var sb = new System.Text.StringBuilder();
			   System.Diagnostics.Debug.WriteLine($"[OllamaAgent] Streaming started at {DateTime.Now:HH:mm:ss.fff}");
				  await Task.Run(async () =>
			   {
				  await _ollamaChatService.StreamChatAsync(
				   OllamaEndpoint,
				   SelectedChatModel.Name,
				   chatMessages,
				   fragment =>
				   {
					   System.Diagnostics.Debug.WriteLine($"[OllamaAgent] Fragment received at {DateTime.Now:HH:mm:ss.fff}: '{fragment?.Substring(0, Math.Min(fragment.Length, 40))}'");
					   sb.Append(fragment);
					   var content = sb.ToString();
					   var dispatcher = System.Windows.Application.Current?.Dispatcher;
					   if (dispatcher != null && !dispatcher.CheckAccess())
					   {
						   dispatcher.Invoke(() => {
							   aiMsg.Content = content;
							   OnPropertyChanged(nameof(ChatHistory));
						   });
					   }
					   else
					   {
						   aiMsg.Content = content;
						   OnPropertyChanged(nameof(ChatHistory));
					   }
					   // No need to update IsAwaitingAIResponse here; only after full response
				   },
				   _stopStreamingCts.Token
			   );
			   });
			   // After streaming, extract only the message part if present
				  var fullResponse = sb.ToString();
				  string messageText;
			   string titleText = null;
			   var hasMessage = fullResponse.Contains("Message:", StringComparison.OrdinalIgnoreCase);
			   var hasTitle = fullResponse.Contains("Title:", StringComparison.OrdinalIgnoreCase);
			   if (hasMessage)
			   {
				   var messageIdx = fullResponse.IndexOf("Message:", StringComparison.OrdinalIgnoreCase);
				   var titleIdx = fullResponse.IndexOf("Title:", messageIdx, StringComparison.OrdinalIgnoreCase);
				   if (titleIdx > messageIdx)
				   {
					   messageText = fullResponse.Substring(messageIdx + 8, titleIdx - (messageIdx + 8)).Trim();
					   // Extract title
					   var titleStart = titleIdx + 6;
					   var titleEnd = fullResponse.IndexOf('\n', titleStart);
					   if (titleEnd > titleStart)
						   titleText = fullResponse.Substring(titleStart, titleEnd - titleStart).Trim();
					   else
						   titleText = fullResponse.Substring(titleStart).Trim();
				   }
				   else
				   {
					   messageText = fullResponse.Substring(messageIdx + 8).Trim();
				   }
			   }
			   else if (hasTitle)
			   {
				   // Optionally extract or show the title, or fallback to full response
				   messageText = fullResponse.Trim();
				   var titleIdx = fullResponse.IndexOf("Title:", StringComparison.OrdinalIgnoreCase);
				   if (titleIdx >= 0)
				   {
					   var titleStart = titleIdx + 6;
					   var titleEnd = fullResponse.IndexOf('\n', titleStart);
					   if (titleEnd > titleStart)
						   titleText = fullResponse.Substring(titleStart, titleEnd - titleStart).Trim();
					   else
						   titleText = fullResponse.Substring(titleStart).Trim();
				   }
			   }
			   else
			   {
				   messageText = fullResponse.Trim();
			   }
				  var dispatcher = System.Windows.Application.Current?.Dispatcher;
			   if (dispatcher != null && !dispatcher.CheckAccess())
			   {
				   dispatcher.Invoke(() => {
					   aiMsg.Content = messageText;
					   // Update thread title if present
					  if (!string.IsNullOrWhiteSpace(titleText))
				   {
					   // Always update if auto-named, or if the name is 'New Thread', or if the title is different
					   if (ActiveThread.IsAutoNamed || string.Equals(ActiveThread.Name, "New Thread", StringComparison.OrdinalIgnoreCase) || !string.Equals(ActiveThread.Name, titleText, StringComparison.Ordinal))
					   {
						   ActiveThread.Name = titleText;
						   ActiveThread.IsAutoNamed = false;
					   }
				   }
					   OnPropertyChanged(nameof(ChatHistory));
				   });
			   }
			   else
			   {
				   aiMsg.Content = messageText;
				   if (!string.IsNullOrWhiteSpace(titleText))
				   {
					   ActiveThread.Name = titleText;
					   ActiveThread.IsAutoNamed = false;
				   }
				   OnPropertyChanged(nameof(ChatHistory));
			   }
			   System.Diagnostics.Debug.WriteLine($"[OllamaAgent] Streaming ended at {DateTime.Now:HH:mm:ss.fff}");
			   IsAwaitingAIResponse = false;
			   await SaveThreadAsync(ActiveThread);
			});


		private bool _isAwaitingAIResponse = false;
		public bool IsAwaitingAIResponse
		{
			get => _isAwaitingAIResponse;
			private set { _isAwaitingAIResponse = value; OnPropertyChanged(); }
		}

		private System.Threading.CancellationTokenSource _stopStreamingCts;
		private IRelayCommand _stopStreamingCommand;
		public IRelayCommand StopStreamingCommand =>
			_stopStreamingCommand ??= new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
			{
				_stopStreamingCts?.Cancel();
			}, () => IsAwaitingAIResponse);

		private async Task CreateAndSwitchToNewThreadAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			if (SelectedChatModel == null || string.IsNullOrWhiteSpace(SelectedChatModel.Name))
			{
				System.Windows.MessageBox.Show("Please select a model before starting a new thread.", "Model Required");
				return;
			}
			var solutionPath = GetBestSolutionPath();
			var thread = new ChatThread
			{
				Id = Guid.NewGuid().ToString(),
				Name = "New Thread",
				SolutionPath = solutionPath,
				ModelName = SelectedChatModel?.Name,
				CreatedAt = DateTime.UtcNow,
				LastActivityAt = DateTime.UtcNow,
				IsAutoNamed = true
			};
			Threads.Add(thread);
			ActiveThread = thread;
			System.Diagnostics.Debug.WriteLine($"[OllamaAgent] Created new thread {thread.Id} with model {thread.ModelName}");
			await SaveThreadAsync(thread);
		}

		private string GetSolutionPath()
		{
			try
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				var solution = Microsoft.VisualStudio.Shell.Package.GetGlobalService(
					typeof(SVsSolution)) as IVsSolution;

				if (solution == null) return null;

				solution.GetSolutionInfo(out string solutionDir, out string solutionFile, out string optsFile);
				return solutionFile; // full path to the .sln file, null if no solution open
			}
			catch { return null; }
		}

		// Returns the best available solution path for thread association
		private string GetBestSolutionPath()
		{
			var path = GetSolutionPath();
			if (!string.IsNullOrWhiteSpace(path))
				return path;

			// Synthesize a default path (e.g., use a placeholder)
			var defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var defaultPath = System.IO.Path.Combine(defaultDir, "OllamaAgent", "chats", "default.sln");
			if (System.IO.File.Exists(defaultPath))
				return defaultPath;

			// Fallback: global (null)
			return null;
		}
	}
}
