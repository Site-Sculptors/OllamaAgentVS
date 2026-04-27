using Microsoft.VisualStudio.Shell;

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX
{
	[PackageRegistration(
		UseManagedResourcesOnly = true,
		AllowsBackgroundLoading = true)]

	// REQUIRED for VSCT command menus
	[ProvideMenuResource("Menus.ctmenu", 1)]

	// Keeps your tool window registered
	[ProvideToolWindow(typeof(OllamaAgentToolWindow))]

	// Keeps your Options page registered
	[ProvideOptionPage(
		typeof(OllamaAgentOptionsPage),
		"Ollama Agent",
		"General",
		0,
		0,
		true)]

	[Guid(PackageGuidString)]
	public sealed class OllamaAgentVSIXPackage : AsyncPackage
	{
		public const string PackageGuidString =
			"b94239c4-4aa9-4a3d-b23c-d720cfb207b1";

		protected override async Task InitializeAsync(
			CancellationToken cancellationToken,
			IProgress<ServiceProgressData> progress)
		{
			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			// Registers VSCT commands at runtime
			await OllamaAgentCommand.InitializeAsync(this);
		}
	}
}