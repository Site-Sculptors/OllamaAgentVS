
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using OllamaAgent.VSIX.Models;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.ViewModels
{
	public partial class ChatViewModel
	{

		/// <summary>
		/// Raised when the View should display the attach/context menu.
		/// The View (XAML code-behind) should subscribe and open the menu.
		/// </summary>
		public event System.Action OnShowAttachMenuRequested;


		// Menu commands (all async, lazy-initialized)

		// Command to trigger the attach menu popup (for + button)
		private IRelayCommand _showAttachMenuCommand;
		public IRelayCommand ShowAttachMenuCommand =>
			_showAttachMenuCommand ??= new RelayCommand(() =>
			{
				OnShowAttachMenuRequested?.Invoke();
			});

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

		private IAsyncRelayCommand _toggleAutoAttachCommand;
		public IAsyncRelayCommand ToggleAutoAttachCommand =>
			_toggleAutoAttachCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
			{
				AutoAttachActiveDocument = !AutoAttachActiveDocument;

				//Persist this bool


				if (AutoAttachActiveDocument)
				{
					Attachments.Add(new AttachmentModel { Label = "Active Document" });
				}
				else
				{
					var activeDocAttachment = Attachments.FirstOrDefault(a => a.Label == "Active Document");
					if (activeDocAttachment != null)
					{
						Attachments.Remove(activeDocAttachment);
					}
				}
			});
	}
}
