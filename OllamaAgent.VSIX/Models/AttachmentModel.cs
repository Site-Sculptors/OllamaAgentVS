using OllamaAgent.VSIX.Enums;

namespace OllamaAgent.VSIX.Models
{
	public class AttachmentModel
	{
		public string Icon { get; set; }
		public string Label { get; set; }
		public string Path { get; set; }
		public string Content { get; set; }
		public AttachmentType Type { get; set; }
		public object Context { get; set; }
	}
}
