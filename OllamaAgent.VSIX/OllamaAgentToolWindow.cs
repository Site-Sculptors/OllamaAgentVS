using Microsoft.VisualStudio.Shell;

using OllamaAgent.VSIX.Controls;

using System.Runtime.InteropServices;

namespace OllamaAgent.VSIX
{
	[Guid("2d6c2d3a-9b2c-4a4c-8c1f-0c9d7f1f3b21")]
	public class OllamaAgentToolWindow : ToolWindowPane
	{
		public OllamaAgentToolWindow() : base(null)
		{
			this.Caption = "Ollama Agent";
			this.Content = new OllamaAgentToolWindowControl();
		}
	}
}