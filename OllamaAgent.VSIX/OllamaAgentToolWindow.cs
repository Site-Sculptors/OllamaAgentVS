using Microsoft.VisualStudio.Shell;

using OllamaAgent.VSIX.Controls;

using System;
using System.Runtime.InteropServices;

namespace OllamaAgent.VSIX
{
	[Guid("2d6c2d3a-9b2c-4a4c-8c1f-0c9d7f1f3b21")]
	public class OllamaAgentToolWindow : ToolWindowPane
	{
		public OllamaAgentToolWindow() : base(null)
		{
			this.Caption = "Ollama Agent";
		}

		public override object Content
		{
			get
			{
				if (_content == null)
				{
					var chatViewModel = (ViewModels.ChatViewModel)((IServiceProvider)this.Package).GetService(typeof(ViewModels.ChatViewModel));
					_content = new Controls.OllamaAgentToolWindowControl(chatViewModel);
				}
				return _content;
			}
			set { _content = value; }
		}

		private object _content;
	}
}