using System.Windows.Controls;

namespace OllamaAgent.VSIX
{
	public partial class OllamaOptionsControl : UserControl
	{
		public OllamaOptionsViewModel ViewModel => DataContext as OllamaOptionsViewModel;

		public OllamaOptionsControl()
		{
			InitializeComponent();
		}
	}
}