using CommunityToolkit.Mvvm.Input;

using Microsoft.VisualStudio.Shell;

using OllamaAgent.VSIX.Models;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace OllamaAgent.VSIX.ViewModels
{
	public class ChatViewModel : ViewModelBase
	{
		public ChatViewModel(OllamaAgent.VSIX.OllamaModelService ollamaModelService, OllamaAgentVSIXPackage package) : base(ollamaModelService, package)
		{
			Threads = new ObservableCollection<ChatThread>();
			Threads.CollectionChanged += (s, e) => SaveThreads();
			_ = LoadThreadsAsync();
		}

		private string GetSolutionPath()
		{
			// Try to get the solution path from the package (update as needed for your context)
			try
			{
				var dte = (EnvDTE.DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
				return dte?.Solution?.FullName ?? "default";
			}
			catch { return "default"; }
		}

		private string GetChatHistoryFilePath()
		{
			var solutionPath = GetSolutionPath();
			using (var sha = SHA256.Create())
			{
				var hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(solutionPath))).Replace("-", "").ToLowerInvariant();
				var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OllamaAgentVS", "ChatHistory");
				Directory.CreateDirectory(dir);
				return Path.Combine(dir, $"chat-{hash}.json");
			}
		}

		private async Task LoadThreadsAsync()
		{
			var file = GetChatHistoryFilePath();
			if (File.Exists(file))
			{
				try
				{
			   var json = await Task.Run(() => File.ReadAllText(file));
					var threads = JsonConvert.DeserializeObject<ObservableCollection<ChatThread>>(json) ?? new ObservableCollection<ChatThread>();
					Threads.Clear();
					foreach (var t in threads)
						Threads.Add(t);
					CurrentThread = Threads.Count > 0 ? Threads[0] : null;
				}
				catch { Threads.Clear(); CreateAndSwitchToNewThread(); }
			}
			else
			{
				CreateAndSwitchToNewThread();
			}
		}

		private void SaveThreads()
		{
			try
			{
				var file = GetChatHistoryFilePath();
				var json = JsonConvert.SerializeObject(Threads, Formatting.Indented);
				File.WriteAllText(file, json);
			}
			catch { /* ignore errors */ }
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

		public ObservableCollection<ChatThread> Threads { get; }

		private ChatThread _currentThread;
		public ChatThread CurrentThread
		{
			get => _currentThread;
			set
			{
				if (_currentThread != value)
				{
					_currentThread = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(ChatHistory));
				}
			}
		}

		public ObservableCollection<ChatMessage> ChatHistory => CurrentThread?.Messages;


		private bool _isHistoryVisible;
		public bool IsHistoryVisible
		{
			get => _isHistoryVisible;
			set { _isHistoryVisible = value; OnPropertyChanged(); }
		}

		// SettingsCommand now inherited from ViewModelBase
		private IAsyncRelayCommand _newThreadCommand;
		public IAsyncRelayCommand NewThreadCommand =>
			_newThreadCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			CreateAndSwitchToNewThread();
			SaveThreads();
			await Task.CompletedTask;
		});

		private IAsyncRelayCommand _deleteThreadCommand;
		public IAsyncRelayCommand DeleteThreadCommand =>
			_deleteThreadCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			if (CurrentThread != null)
			{
				var idx = Threads.IndexOf(CurrentThread);
				Threads.Remove(CurrentThread);
				if (Threads.Count > 0)
				{
					CurrentThread = Threads[Math.Max(0, Math.Min(idx, Threads.Count - 1))];
				}
				else
				{
					CreateAndSwitchToNewThread();
				}
				SaveThreads();
			}
			await Task.CompletedTask;
		});

		private IRelayCommand _chatHistoryCommand;
		public IRelayCommand ChatHistoryCommand =>
			_chatHistoryCommand ??= new RelayCommand(() => IsHistoryVisible = !IsHistoryVisible);

		// OpenSettingsAsync now inherited from ViewModelBase

		private IAsyncRelayCommand _sendCommand;
		public IAsyncRelayCommand SendCommand =>
			_sendCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			var userInput = Input;
			if (string.IsNullOrWhiteSpace(userInput) || string.IsNullOrWhiteSpace(SelectedModel) || CurrentThread == null)
				return;

			CurrentThread.Messages.Add(new ChatMessage { Sender = "You", Message = userInput });
			Input = string.Empty;

			var response = await OllamaService.GenerateCompletionAsync(Endpoint, SelectedModel, userInput);
			if (!string.IsNullOrWhiteSpace(response))
			{
				CurrentThread.Messages.Add(new ChatMessage { Sender = SelectedModel, Message = response });
			}
			else
			{
				CurrentThread.Messages.Add(new ChatMessage { Sender = "System", Message = "No response from model." });
			}
			SaveThreads();
		});

		private void CreateAndSwitchToNewThread()
		{
			var thread = new ChatThread
			{
				Id = Guid.NewGuid().ToString(),
				Name = $"Thread {Threads.Count + 1}"
			};
			Threads.Add(thread);
			CurrentThread = thread;
			SaveThreads();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
