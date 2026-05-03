using System;

namespace OllamaAgent.VSIX.ViewModels
{
	public class AttachmentViewModel
	{
		public string Icon { get; set; } // Unicode or image path
		public string Label { get; set; }
		public object Context { get; set; } // Optional: reference to attached object
	}
}
