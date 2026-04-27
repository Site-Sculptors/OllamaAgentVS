using System.Windows;
using System.Windows.Controls;

namespace OllamaAgent.VSIX
{
	public partial class OllamaAgentToolWindowControl : UserControl
	{
		public OllamaAgentToolWindowControl()
		{
			InitializeComponent();
		}

		private async void AskButton_Click(object sender, RoutedEventArgs e)
		{
			OutputBox.Text = "Agent ready... (next step: Ollama integration)";
		}
	}
}