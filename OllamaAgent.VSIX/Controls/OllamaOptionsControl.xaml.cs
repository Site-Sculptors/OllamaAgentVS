using OllamaAgent.VSIX.ViewModels;

using System.Windows.Controls;

namespace OllamaAgent.VSIX.Controls
{
	public partial class OllamaOptionsControl : UserControl
	{
	   public OllamaOptionsViewModel ViewModel => DataContext as OllamaOptionsViewModel;

	   public OllamaOptionsControl(OllamaOptionsViewModel viewModel)
	   {
		   InitializeComponent();
		   DataContext = viewModel;
	   }
	}
}