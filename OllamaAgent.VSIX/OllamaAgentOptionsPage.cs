using System.ComponentModel;
using System.Windows;

using Microsoft.VisualStudio.Shell;

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

			ViewModel?.LoadAsync();
		}

		protected override void OnApply(PageApplyEventArgs e)
		{
			base.OnApply(e);

			ViewModel?.Save();
		}
	}
}