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

		// Shared singleton instance for options and other windows
		public ViewModels.OllamaOptionsViewModel SharedOptionsViewModel { get; private set; }

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

	   // Register shared ViewModel state singleton
	   this.AddService(typeof(Services.IViewModelStateStore), (container, cancellationToken, serviceType) =>
	   {
		   return Task.FromResult<object>(new Services.ViewModelStateStore());
	   }, promote: true);

		  // Register IOllamaApiService singleton (must be before IOllamaChatService)
	   this.AddService(typeof(Services.IOllamaApiService), (container, cancellationToken, serviceType) =>
	   {
		   return Task.FromResult<object>(new Services.OllamaApiService());
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
		   var apiService = (Services.IOllamaApiService)((IServiceProvider)container).GetService(typeof(Services.IOllamaApiService));
		   return Task.FromResult<object>(new Services.OllamaChatService(apiService));
	   }, promote: true);

	 // Register CustomInstructionsService singleton
	 var customInstructionsService = new Services.CustomInstructionsService();
	 this.AddService(typeof(Services.CustomInstructionsService), (container, ct, serviceType) =>
	 {
		 return Task.FromResult<object>(customInstructionsService);
	 }, promote: true);

	 // Load custom instructions on solution open
	 string solutionDir = null;
	 await JoinableTaskFactory.RunAsync(async () =>
	 {
		 await JoinableTaskFactory.SwitchToMainThreadAsync();
		 var solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
		 if (solution != null)
		 {
			 solution.GetSolutionInfo(out string dir, out string file, out string opts);
			 solutionDir = dir;
		 }
	 });
	 await customInstructionsService.LoadAsync(solutionDir);

	   // Register OutputWindowContextService singleton
	   this.AddService(typeof(Services.IOutputWindowContextService), (container, ct, serviceType) =>
	   {
		   return Task.FromResult<object>(new Services.OutputWindowContextService());
	   }, promote: true);

	  // Register OllamaOptionsViewModel singleton
   this.AddService(typeof(ViewModels.OllamaOptionsViewModel), (container, cancellationToken, serviceType) =>
   {
	   var agentService = (Services.IOllamaAgentService)((IServiceProvider)container).GetService(typeof(Services.IOllamaAgentService));
	   var modelService = (Services.IOllamaModelService)((IServiceProvider)container).GetService(typeof(Services.IOllamaModelService));
	   var modelStore = (Services.IModelStore)((IServiceProvider)container).GetService(typeof(Services.IModelStore));
	   var viewModelStateStore = (Services.IViewModelStateStore)((IServiceProvider)container).GetService(typeof(Services.IViewModelStateStore));
	   var vm = new ViewModels.OllamaOptionsViewModel(agentService, modelService, this, modelStore, viewModelStateStore);
	   this.SharedOptionsViewModel = vm;
	   return Task.FromResult<object>(vm);
   }, promote: true);

   // Register ChatViewModel singleton
   this.AddService(typeof(ViewModels.ChatViewModel), (container, cancellationToken, serviceType) =>
   {
	   var chatService = (Services.IOllamaChatService)((IServiceProvider)container).GetService(typeof(Services.IOllamaChatService));
	   var agentService = (Services.IOllamaAgentService)((IServiceProvider)container).GetService(typeof(Services.IOllamaAgentService));
	   var modelService = (Services.IOllamaModelService)((IServiceProvider)container).GetService(typeof(Services.IOllamaModelService));
	   var modelStore = (Services.IModelStore)((IServiceProvider)container).GetService(typeof(Services.IModelStore));
	   var viewModelStateStore = (Services.IViewModelStateStore)((IServiceProvider)container).GetService(typeof(Services.IViewModelStateStore));
	   var editorContext = (Services.IEditorContextService)((IServiceProvider)container).GetService(typeof(Services.IEditorContextService));
	   var errorListService = (Services.IErrorListService)((IServiceProvider)container).GetService(typeof(Services.IErrorListService));
	   var outputWindowContextService = (Services.IOutputWindowContextService)((IServiceProvider)container).GetService(typeof(Services.IOutputWindowContextService));
	   return Task.FromResult<object>(new ViewModels.ChatViewModel(chatService, agentService, modelService, this, modelStore, viewModelStateStore, editorContext, errorListService, outputWindowContextService));
   }, promote: true);
   // Register EditorContextService singleton
   this.AddService(typeof(Services.IEditorContextService), (container, ct, serviceType) =>
   {
	   return Task.FromResult<object>(new Services.EditorContextService());
   }, promote: true);

   // Register ErrorListService singleton
   this.AddService(typeof(Services.IErrorListService), (container, ct, serviceType) =>
   {
	   return Task.FromResult<object>(new Services.ErrorListService());
   }, promote: true);

	   // Register IChatThreadStore singleton
	   this.AddService(typeof(Services.IChatThreadStore), (container, ct, serviceType) =>
	   {
		   return Task.FromResult<object>(new Services.ChatThreadStore());
	   }, promote: true);

	   // Register SymbolExtractorService singleton
	   this.AddService(typeof(Services.ISymbolExtractorService), (container, ct, serviceType) =>
	   {
		   return Task.FromResult<object>(new Services.SymbolExtractorService());
	   }, promote: true);

	   // Registers VSCT commands at runtime
	   await OllamaAgentCommand.InitializeAsync(this);
   }
	}
}