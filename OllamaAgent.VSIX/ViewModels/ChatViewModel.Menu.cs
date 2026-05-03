using CommunityToolkit.Mvvm.Input;

using System.Threading.Tasks;

namespace OllamaAgent.VSIX.ViewModels
{
	public partial class ChatViewModel
	{
		// Menu state
		private bool _autoAttachActiveDocument = true;
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



		// Menu commands (all async, lazy-initialized)

		private IAsyncRelayCommand _attachActiveDocumentCommand;
		public IAsyncRelayCommand AttachActiveDocumentCommand =>
			_attachActiveDocumentCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
			{
				// TODO: Attach the currently active document as chat context
				await Task.CompletedTask;
			});

		private IAsyncRelayCommand _attachSolutionCommand;
		public IAsyncRelayCommand AttachSolutionCommand =>
			_attachSolutionCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
			{
				// TODO: Attach the current solution as chat context
				await Task.CompletedTask;
			});

		private IAsyncRelayCommand _attachFilesCommand;
		public IAsyncRelayCommand AttachFilesCommand =>
			_attachFilesCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
			{
				// TODO: Prompt user to select files and attach as chat context
				await Task.CompletedTask;
			});

		private IAsyncRelayCommand _attachClassesCommand;
		public IAsyncRelayCommand AttachClassesCommand =>
			_attachClassesCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
			{
				// TODO: Prompt user to select classes and attach as chat context
				await Task.CompletedTask;
			});

		private IAsyncRelayCommand _attachMethodsCommand;
		public IAsyncRelayCommand AttachMethodsCommand =>
			_attachMethodsCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
			{
				// TODO: Prompt user to select methods and attach as chat context
				await Task.CompletedTask;
			});

		private IAsyncRelayCommand _attachOutputLogsCommand;
		public IAsyncRelayCommand AttachOutputLogsCommand =>
			_attachOutputLogsCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
			{
				// TODO: Attach output window logs as chat context
				await Task.CompletedTask;
			});

		private IAsyncRelayCommand _attachMcpPromptsCommand;
		public IAsyncRelayCommand AttachMcpPromptsCommand =>
			_attachMcpPromptsCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
			{
				// TODO: Attach MCP prompt examples/templates as chat context
				await Task.CompletedTask;
			});

		private IAsyncRelayCommand _attachMcpResourcesCommand;
		public IAsyncRelayCommand AttachMcpResourcesCommand =>
			_attachMcpResourcesCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
			{
				// TODO: Attach MCP resource documentation as chat context
				await Task.CompletedTask;
			});

		private IAsyncRelayCommand _uploadImageCommand;
		public IAsyncRelayCommand UploadImageCommand =>
			_uploadImageCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
			{
				// TODO: Prompt user to select an image and upload as chat context
				await Task.CompletedTask;
			});

		private async Task AttachActiveDocument()
		{
			// Attach the currently active document in the editor as chat context
			await AttachDocumentContextAsync();
		}

		private async Task AttachSolution()
		{
			// Attach the current solution file as chat context
			await AttachSolutionContextAsync();
		}

		private async Task AttachFiles()
		{
			// Prompt user to select files and attach them as chat context
			await AttachFilesContextAsync();
		}

		private async Task AttachClasses()
		{
			// Prompt user to select classes and attach them as chat context
			await AttachClassesContextAsync();
		}

		private async Task AttachMethods()
		{
			// Prompt user to select methods and attach them as chat context
			await AttachMethodsContextAsync();
		}

		private async Task AttachOutputLogs()
		{
			// Attach the current output window logs as chat context
			await AttachOutputLogsContextAsync();
		}

		private async Task AttachMcpPrompts()
		{
			// Attach MCP prompt examples/templates as chat context
			await AttachMcpPromptsContextAsync();
		}

		private async Task AttachMcpResources()
		{
			// Attach MCP resource documentation as chat context
			await AttachMcpResourcesContextAsync();
		}

		private async Task UploadImage()
		{
			// Prompt user to select an image and upload it as chat context
			await UploadImageContextAsync();
		}



		private IAsyncRelayCommand _toggleAutoAttachCommand;
		public IAsyncRelayCommand ToggleAutoAttachCommand =>
			_toggleAutoAttachCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
			{
				AutoAttachActiveDocument = !AutoAttachActiveDocument;
				await Task.CompletedTask;
			});

		// --- Placeholders for actual context attachment logic ---
		// Implement these methods to integrate with your context system
		private async Task AttachDocumentContextAsync() { /* TODO: Integrate with document context system */ await Task.CompletedTask; }
		private async Task AttachSolutionContextAsync() { /* TODO: Integrate with solution context system */ await Task.CompletedTask; }
		private async Task AttachFilesContextAsync() { /* TODO: Integrate with file picker and context system */ await Task.CompletedTask; }
		private async Task AttachClassesContextAsync() { /* TODO: Integrate with class picker and context system */ await Task.CompletedTask; }
		private async Task AttachMethodsContextAsync() { /* TODO: Integrate with method picker and context system */ await Task.CompletedTask; }
		private async Task AttachOutputLogsContextAsync() { /* TODO: Integrate with output logs system */ await Task.CompletedTask; }
		private async Task AttachMcpPromptsContextAsync() { /* TODO: Integrate with MCP prompts system */ await Task.CompletedTask; }
		private async Task AttachMcpResourcesContextAsync() { /* TODO: Integrate with MCP resources system */ await Task.CompletedTask; }
		private async Task UploadImageContextAsync() { /* TODO: Integrate with image upload system */ await Task.CompletedTask; }

		// partial void InitializeMenuCommands() { ... } // No longer needed with lazy properties
	}
}
