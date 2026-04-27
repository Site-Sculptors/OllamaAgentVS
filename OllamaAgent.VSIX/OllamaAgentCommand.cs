using Microsoft.VisualStudio.Shell;

using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX
{
	internal sealed class OllamaAgentCommand
	{
		public const int OpenWindowId = 0x0100;
		public const int SettingsId = 0x0101;
		public const int RefreshModelsId = 0x0102;

		public static readonly Guid CommandSet =
			new Guid("a7f0d2e3-8f11-4d5a-9c11-2d5c1a2f9b33");

		private readonly AsyncPackage _package;

		private OllamaAgentCommand(AsyncPackage package, OleMenuCommandService commandService)
		{
			_package = package;

			commandService.AddCommand(new MenuCommand(ExecuteOpenWindow, new CommandID(CommandSet, OpenWindowId)));
			commandService.AddCommand(new MenuCommand(ExecuteSettings, new CommandID(CommandSet, SettingsId)));
			commandService.AddCommand(new MenuCommand(ExecuteRefreshModels, new CommandID(CommandSet, RefreshModelsId)));
		}

		public static async Task InitializeAsync(AsyncPackage package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			var service = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

			if (service != null)
				new OllamaAgentCommand(package, service);
		}

		private void ExecuteOpenWindow(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			_ = ShowToolWindowAsync();
		}

		private void ExecuteSettings(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			// later: open options window
		}

		private void ExecuteRefreshModels(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			// later: trigger model refresh
		}

		private async Task ShowToolWindowAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var window = await _package.FindToolWindowAsync(
				typeof(OllamaAgentToolWindow),
				0,
				create: true,
				cancellationToken: _package.DisposalToken);

			if (window?.Frame is Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame frame)
				Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(frame.Show());
		}
	}
}