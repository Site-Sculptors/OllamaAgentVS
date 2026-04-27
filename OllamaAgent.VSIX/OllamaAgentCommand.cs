using Microsoft.VisualStudio.Shell;

using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX
{
	internal sealed class OllamaAgentCommand
	{
		public const int CommandId = 0x0100;

		public static readonly Guid CommandSet =
			new Guid("a7f0d2e3-8f11-4d5a-9c11-2d5c1a2f9b33");

		private readonly AsyncPackage _package;

		private OllamaAgentCommand(AsyncPackage package, OleMenuCommandService commandService)
		{
			_package = package;

			var menuCommandID = new CommandID(CommandSet, CommandId);

			// Use OleMenuCommand for modern VSIX reliability
			var menuItem = new OleMenuCommand(Execute, menuCommandID);

			commandService.AddCommand(menuItem);
		}

		public static async Task InitializeAsync(AsyncPackage package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			// IMPORTANT: use IMenuCommandService (safer + correct abstraction)
			var service = await package.GetServiceAsync(typeof(IMenuCommandService));

			var commandService = service as OleMenuCommandService;

			if (commandService == null)
			{
				System.Diagnostics.Debug.WriteLine(
					"OllamaAgent: OleMenuCommandService is NULL. Command NOT registered.");
				return;
			}

			new OllamaAgentCommand(package, commandService);
		}

		private void Execute(object sender, EventArgs e)
		{
			System.Windows.Forms.MessageBox.Show("Inside Execute");
			ThreadHelper.ThrowIfNotOnUIThread();
			System.Windows.Forms.MessageBox.Show("COMMAND EXECUTED");
			_ = ShowToolWindowAsync();
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
			{
				Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(frame.Show());
			}
		}
	}
}