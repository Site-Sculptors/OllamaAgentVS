using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX
{
	internal sealed class OllamaAgentCommandHandler
	{
		private readonly AsyncPackage _package;

		// Must match your .vsct command IDs
		public const int CmdOpenWindow = 0x0100;
		public const int CmdSettings = 0x0101;
		public const int CmdRefreshModels = 0x0102;

		// Must match your package GUID (same as in VSIX package)
		public static readonly Guid CommandSet = new Guid("a7f0d2e3-8f11-4d5a-9c11-2d5c1a2f9b33");

		private OllamaAgentCommandHandler(AsyncPackage package, OleMenuCommandService commandService)
		{
			_package = package ?? throw new ArgumentNullException(nameof(package));

			// Open Chat Window
			var openCmdId = new CommandID(CommandSet, CmdOpenWindow);
			commandService.AddCommand(new OleMenuCommand(OpenChatWindow, openCmdId));

			// Settings (just opens VS Options page)
			var settingsCmdId = new CommandID(CommandSet, CmdSettings);
			commandService.AddCommand(new OleMenuCommand(OpenSettings, settingsCmdId));

			// Refresh Models (placeholder for now, we wire Ollama later)
			var refreshCmdId = new CommandID(CommandSet, CmdRefreshModels);
			commandService.AddCommand(new OleMenuCommand(RefreshModels, refreshCmdId));
		}

		public static async Task InitializeAsync(AsyncPackage package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

			if (commandService != null)
			{
				new OllamaAgentCommandHandler(package, commandService);
			}
		}

		private void OpenChatWindow(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_ = ShowToolWindowAsync();
		}

		private async Task ShowToolWindowAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);

			var window = await _package.FindToolWindowAsync(
				typeof(OllamaAgentToolWindow),
				0,
				create: true,
				cancellationToken: _package.DisposalToken);

			if (window?.Frame is Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame frame)
			{
				Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(frame.Show());
			}
		}

		private void OpenSettings(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Opens Tools → Options → Ollama Agent
			_package.ShowOptionPage(typeof(OllamaAgentOptionsPage));
		}

		private void RefreshModels(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			// Placeholder for now
			// Later we will call:
			// http://localhost:11434/api/tags

			VsShellUtilities.ShowMessageBox(
				_package,
				"Model refresh will connect to Ollama API next step.",
				"Ollama Agent",
				OLEMSGICON.OLEMSGICON_INFO,
				OLEMSGBUTTON.OLEMSGBUTTON_OK,
				OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
		}
	}
}