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

		private void AskButton_Click(object sender, RoutedEventArgs e)
		{
			OutputBox.Text = "Ollama Agent ready (next step: connect model)";
		}
	}
}