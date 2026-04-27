using Microsoft.VisualStudio.Shell;

using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX
{
	internal sealed class OllamaAgentCommand
	{
		public const int CommandId = 0x0100;
		public static readonly Guid CommandSet = new Guid("a7f0d2e3-8f11-4d5a-9c11-2d5c1a2f9b33");

		private readonly AsyncPackage package;

		private OllamaAgentCommand(AsyncPackage package, OleMenuCommandService commandService)
		{
			this.package = package;

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new MenuCommand(Execute, menuCommandID);

			commandService.AddCommand(menuItem);
		}

		public static async Task InitializeAsync(AsyncPackage package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

			if (commandService != null)
			{
				new OllamaAgentCommand(package, commandService);
			}
		}

		private void Execute(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_ = ShowToolWindowAsync();
		}

		private async Task ShowToolWindowAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var window = await package.FindToolWindowAsync(
				typeof(OllamaAgentToolWindow),
				0,
				create: true,
				cancellationToken: package.DisposalToken);

			if (window?.Frame is not Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame frame)
				return;

			Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(frame.Show());
		}
	}
}