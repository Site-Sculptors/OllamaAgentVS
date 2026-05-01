using CommunityToolkit.Mvvm.Input;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using OllamaAgent.VSIX.Enums;
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

	public class ChatViewModel : ViewModelBase
	{
		private static readonly ObservableCollection<ChatMessage> _emptyMessages = new ObservableCollection<ChatMessage>();
		private readonly IOllamaChatService _ollamaChatService;
		private readonly IChatThreadStore _chatThreadStore;

		public ChatViewModel(IOllamaChatService ollamaChatService, IOllamaAgentService ollamaAgentService, IOllamaModelService ollamaModelService, OllamaAgentVSIXPackage package, IModelStore modelStore)
			: base(ollamaAgentService, ollamaModelService, package, modelStore)
		{
			_ollamaChatService = ollamaChatService;
			_chatThreadStore = (IChatThreadStore)((IServiceProvider)package).GetService(typeof(IChatThreadStore));
			Threads = new ObservableCollection<ChatThread>();
			_ = LoadThreadsForCurrentSolutionAsync();
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


		public ObservableCollection<ChatThread> Threads { get; }

		private ChatThread _activeThread;
		public ChatThread ActiveThread
		{
			get => _activeThread;
			set
			{
				if (_activeThread != value)
				{
					_activeThread = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(ChatHistory));
				}
			}
		}

		// Synchronize thread selection with the UI
		public ChatThread CurrentThread
		{
			get => ActiveThread;
			set => ActiveThread = value;
		}

		public ObservableCollection<ChatMessage> ChatHistory => ActiveThread?.Messages ?? _emptyMessages;

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

			// Use LINQ to find the thread for the current solution
			var match = Threads.FirstOrDefault(t => t.SolutionPath == solutionPath);
			if (match != null)
				ActiveThread = match;
			else if (Threads.Count > 0)
				ActiveThread = Threads[0];
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
					// Debug: show messages count
					System.Diagnostics.Debug.WriteLine($"[OllamaAgent] ActiveThread changed to {match.Name} with {match.Messages?.Count ?? 0} messages.");
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

		private IRelayCommand _chatHistoryCommand;
		public IRelayCommand ChatHistoryCommand =>
			_chatHistoryCommand ??= new RelayCommand(() => IsHistoryVisible = !IsHistoryVisible);

		// OpenSettingsAsync now inherited from ViewModelBase


		private IAsyncRelayCommand _sendCommand;
		public IAsyncRelayCommand SendCommand =>
			_sendCommand ??= new CommunityToolkit.Mvvm.Input.AsyncRelayCommand<object>(async (parameter) =>
			{
				var userInput = Input;
				if (string.IsNullOrWhiteSpace(userInput) || SelectedModel == null || string.IsNullOrWhiteSpace(SelectedModel.Name) || ActiveThread == null)
					return;

				ActiveThread.Messages.Add(new ChatMessage { Role = ChatRole.User, Message = userInput });
				Input = string.Empty;

				// Build conversation context
				var conversation = string.Join("\n", ActiveThread.Messages.Select(m => $"{m.Role}: {m.Message}"));
				var prompt = $"Given the following conversation, reply as the assistant. Also, suggest a concise thread title (max 5 words) that summarizes the conversation so far. Format your response as:\nMessage: <your reply>\nTitle: <suggested title>\n\nConversation:\n{conversation}\nUser: {userInput}";

				var response = await _ollamaChatService.GenerateCompletionAsync(OllamaEndpoint, SelectedModel.Name, prompt);
				string aiMessage = null;
				string newTitle = null;
				if (!string.IsNullOrWhiteSpace(response))
				{
					// Parse response for Message: ... and Title: ...
					var lines = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var line in lines)
					{
						if (line.StartsWith("Message:", StringComparison.OrdinalIgnoreCase))
							aiMessage = line.Substring("Message:".Length).Trim();
						else if (line.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
							newTitle = line.Substring("Title:".Length).Trim();
					}
				}

				if (!string.IsNullOrWhiteSpace(aiMessage))
				{
					ActiveThread.Messages.Add(new ChatMessage { Role = ChatRole.AI, Message = aiMessage });
				}
				else
				{
					ActiveThread.Messages.Add(new ChatMessage { Role = ChatRole.AI, Message = "No response from model." });
				}




				// Update thread name if auto-named and model returned a title
				if (ActiveThread.IsAutoNamed && !string.IsNullOrWhiteSpace(newTitle))
				{
					ActiveThread.Name = newTitle;
					ActiveThread.IsAutoNamed = false;
					OnPropertyChanged(nameof(ActiveThread));
					OnPropertyChanged(nameof(Threads));
				}

				await SaveThreadAsync(ActiveThread);
			});


		private async Task CreateAndSwitchToNewThreadAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			if (SelectedModel == null || string.IsNullOrWhiteSpace(SelectedModel.Name))
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
				ModelName = SelectedModel?.Name,
				CreatedAt = DateTime.UtcNow,
				LastActivityAt = DateTime.UtcNow,
				IsAutoNamed = true
			};
			Threads.Add(thread);
			ActiveThread = thread;
			// Debug: show thread creation
			System.Diagnostics.Debug.WriteLine($"[OllamaAgent] Created new thread {thread.Id} with model {thread.ModelName}");
			await SaveThreadAsync(thread);
		}
	}
}
