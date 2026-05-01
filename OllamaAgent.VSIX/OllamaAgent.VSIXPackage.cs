using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
	// Ensures package is loaded when a solution exists (so services are registered before options page is shown)
	// You can change UIContextGuids80.SolutionExists to UIContextGuids80.NoSolution if you want always-on
	[ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
	public sealed class OllamaAgentVSIXPackage : AsyncPackage
	{
		public const string PackageGuidString =
			"b94239c4-4aa9-4a3d-b23c-d720cfb207b1";

	  protected override async Task InitializeAsync(
	   CancellationToken cancellationToken,
	   IProgress<ServiceProgressData> progress)
   {
	   await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

	   // Register ModelStore singleton
	   this.AddService(typeof(Services.IModelStore), (container, cancellationToken, serviceType) =>
	   {
		   return Task.FromResult<object>(new Services.ModelStore());
	   }, promote: true);

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

	   // Register IOllamaChatService singleton
	   this.AddService(typeof(Services.IOllamaChatService), (container, cancellationToken, serviceType) =>
	   {
		   return Task.FromResult<object>(new Services.OllamaChatService());
	   }, promote: true);


	  // Register ChatViewModel singleton
   this.AddService(typeof(ViewModels.ChatViewModel), (container, cancellationToken, serviceType) =>
   {
	  var chatService = (Services.IOllamaChatService)((IServiceProvider)container).GetService(typeof(Services.IOllamaChatService));
	  var agentService = (Services.IOllamaAgentService)((IServiceProvider)container).GetService(typeof(Services.IOllamaAgentService));
	  var modelService = (Services.IOllamaModelService)((IServiceProvider)container).GetService(typeof(Services.IOllamaModelService));
	  var modelStore = (Services.IModelStore)((IServiceProvider)container).GetService(typeof(Services.IModelStore));
	  return Task.FromResult<object>(new ViewModels.ChatViewModel(chatService, agentService, modelService, this, modelStore));
   }, promote: true);

	   // Register IChatThreadStore singleton
	   this.AddService(typeof(Services.IChatThreadStore), (container, ct, serviceType) =>
	   {
		   return Task.FromResult<object>(new Services.ChatThreadStore());
	   }, promote: true);

	   // Registers VSCT commands at runtime
	   await OllamaAgentCommand.InitializeAsync(this);
   }
	}
}