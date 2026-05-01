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
					EnsureViewModel();
					_control = new OllamaOptionsControl(_viewModel);
				}
				return _control;
			}
		}

		private void EnsureViewModel()
		{
			if (_viewModel != null)
				return;

			// Resolve the package first — it is always available through Site.
			var vsixPackage = GetPackage();

			if (vsixPackage == null)
				throw new InvalidOperationException(
					"OllamaAgentVSIXPackage could not be resolved from Site. " +
					"Ensure the package is loaded before opening the options page.");

			// Resolve services through the package (IServiceProvider), not
			// GetGlobalService, so we are guaranteed to hit the same container
			// that registered them in InitializeAsync.
			var sp = (IServiceProvider)vsixPackage;

			var agentService = (IOllamaAgentService)sp.GetService(typeof(IOllamaAgentService));
			var modelService = (IOllamaModelService)sp.GetService(typeof(IOllamaModelService));
			var modelStore = (IModelStore)sp.GetService(typeof(IModelStore));

			if (modelStore == null)
				throw new InvalidOperationException(
					"IModelStore could not be resolved from the package service provider. " +
					"Ensure OllamaAgentVSIXPackage.InitializeAsync has completed before " +
					"opening the options page.");

			_viewModel = new OllamaOptionsViewModel(agentService, modelService, vsixPackage, modelStore);
		}

		/// <summary>
		/// Walks the Site service provider chain to find the OllamaAgentVSIXPackage,
		/// forcing a load if necessary.
		/// </summary>
		private OllamaAgentVSIXPackage GetPackage()
		{
			// Try direct cast through Site first (works once the package is loaded).
			if (this.Site != null)
			{
				var pkg = ((IServiceProvider)this.Site).GetService(typeof(OllamaAgentVSIXPackage))
						  as OllamaAgentVSIXPackage;
				if (pkg != null)
					return pkg;
			}

			// Fall back: force the package to load via IVsShell, then retry.
			var shell = Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsShell))
						as Microsoft.VisualStudio.Shell.Interop.IVsShell;

			if (shell != null)
			{
				var packageGuid = typeof(OllamaAgentVSIXPackage).GUID;
				shell.LoadPackage(ref packageGuid, out _);
			}

			// One more attempt after forcing load.
			if (this.Site != null)
			{
				return ((IServiceProvider)this.Site).GetService(typeof(OllamaAgentVSIXPackage))
					   as OllamaAgentVSIXPackage;
			}

			return null;
		}

		protected override void OnActivate(CancelEventArgs e)
		{
			base.OnActivate(e);

			// EnsureViewModel is idempotent — safe to call again here in case
			// Child was never accessed before activation.
			EnsureViewModel();

			if (_viewModel != null)
			{
				_viewModel.LoadSettings();
				System.Diagnostics.Debug.WriteLine(
					$"[OptionsPage] After LoadSettings: " +
					$"ModelsDirectory={_viewModel.ModelsDirectory}, " +
					$"Endpoint={_viewModel.OllamaEndpoint}, " +
					$"Models.Count={_viewModel.Models?.Count}");
				_ = _viewModel.SafeLoadAsync();
			}
		}
	}
}