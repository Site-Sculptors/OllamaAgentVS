using Microsoft.VisualStudio.Shell;

using OllamaAgent.VSIX.ViewModels;

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
	   await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

	   // Register IOllamaAgentService singleton
		 this.AddService(typeof(Services.IOllamaAgentService), (container, cancellationToken, serviceType) =>
	   {
		   return Task.FromResult<object>(new Services.OllamaAgentService());
	   }, promote: true);

	   // Register IOllamaModelService singleton
	   this.AddService(typeof(Services.IOllamaModelService), (container, cancellationToken, serviceType) =>
	   {
		   return Task.FromResult<object>(new Services.OllamaModelService());
	   }, promote: true);

	   // Register ChatViewModel singleton
	   this.AddService(typeof(ViewModels.ChatViewModel), (container, cancellationToken, serviceType) =>
	   {
		   var chatService = (Services.IOllamaChatService)((IServiceProvider)container).GetService(typeof(Services.IOllamaChatService));
		   var agentService = (Services.IOllamaAgentService)((IServiceProvider)container).GetService(typeof(Services.IOllamaAgentService));
		   var modelService = (Services.IOllamaModelService)((IServiceProvider)container).GetService(typeof(Services.IOllamaModelService));
		   return Task.FromResult<object>(new ChatViewModel(chatService, agentService, modelService, this));
	   }, promote: true);

	   // Registers VSCT commands at runtime
	   await OllamaAgentCommand.InitializeAsync(this);
   }
	}
}