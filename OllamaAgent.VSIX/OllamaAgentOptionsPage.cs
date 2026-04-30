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

		public OllamaOptionsViewModel ViewModel => _control?.ViewModel;

		protected override UIElement Child
		{
			get
			{
				if (_control == null)
				{
					var viewModel = (OllamaOptionsViewModel)Package.GetGlobalService(typeof(OllamaOptionsViewModel));
					_control = new OllamaOptionsControl(viewModel);
				}

				return _control;
			}
		}

		protected override void OnActivate(CancelEventArgs e)
		{
			base.OnActivate(e);

			if (ViewModel != null)
			{
				ViewModel.LoadSettings();
				_ = ViewModel.SafeLoadAsync(); // fire-and-forget, exceptions are handled internally
			}
		}
	}
}