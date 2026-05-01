using System;
using System.ComponentModel;
using System.Windows;

using Microsoft.VisualStudio.Shell;

using OllamaAgent.VSIX.Controls;
using OllamaAgent.VSIX.Services;
using OllamaAgent.VSIX.ViewModels;

namespace OllamaAgent.VSIX
{
	public class OllamaAgentOptionsPage : UIElementDialogPage
	{
	   private OllamaOptionsControl _control;
	   private OllamaOptionsViewModel _viewModel;

		protected override UIElement Child
		{
			get
			{
				  if (_control == null)
			   {
				   var agentService = (IOllamaAgentService)Package.GetGlobalService(typeof(IOllamaAgentService));
				   var modelService = (IOllamaModelService)Package.GetGlobalService(typeof(IOllamaModelService));
				   var modelStore = (IModelStore)Package.GetGlobalService(typeof(IModelStore));
				   var vsixPackage = (OllamaAgentVSIXPackage)((IServiceProvider)this.Site).GetService(typeof(OllamaAgentVSIXPackage));
				   _viewModel = new OllamaOptionsViewModel(agentService, modelService, vsixPackage, modelStore);
				   _control = new OllamaOptionsControl(_viewModel);
			   }

				return _control;
			}
		}

		protected override void OnActivate(CancelEventArgs e)
		{
			base.OnActivate(e);

			if (_viewModel != null)
			{
				_viewModel.LoadSettings();
				System.Diagnostics.Debug.WriteLine($"[OptionsPage] After LoadSettings: ModelsDirectory={_viewModel.ModelsDirectory}, Endpoint={_viewModel.OllamaEndpoint}, Models.Count={_viewModel.Models?.Count}");
				_ = _viewModel.SafeLoadAsync(); // fire-and-forget, exceptions are handled internally
			}
		}
	}
}