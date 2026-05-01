using OllamaAgent.VSIX.ViewModels;

using System.Windows.Controls;

namespace OllamaAgent.VSIX.Controls
{
	public partial class OllamaOptionsControl : UserControl
	{

		public OllamaOptionsControl(OllamaOptionsViewModel viewModel)
		{
			InitializeComponent();
			if (viewModel is OllamaOptionsViewModel)
			{
				DataContext = viewModel; System.Diagnostics.Debug.WriteLine($"[OllamaOptionsControl] DataContext set: {viewModel.GetType().FullName}, HashCode: {viewModel.GetHashCode()}");
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("[OllamaOptionsControl] ViewModel passed in is null!");
			}
		}
	}
}