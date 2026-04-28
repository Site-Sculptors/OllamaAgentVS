using System.ComponentModel;
using System.Windows;

using Microsoft.VisualStudio.Shell;

using OllamaAgent.VSIX.Controls;
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
					_control = new OllamaOptionsControl();
				}

				return _control;
			}
		}

		protected override void OnActivate(CancelEventArgs e)
		{
			base.OnActivate(e);

			ViewModel?.SafeLoadAsync();
		}

		protected override void OnApply(PageApplyEventArgs e)
		{
			base.OnApply(e);

			ViewModel?.Save();
		}
	}
}