using OllamaAgent.VSIX.Enums;
using System.Diagnostics;

namespace OllamaAgent.VSIX.Models
{
	public abstract class ChatMessageBase
	{
		public ChatRole Role { get; set; }
		public string Content { get; set; }
		public virtual string Display => $"{Role}: {Content}";
		public string[] Images { get; set; }
	}

   // UserChatMessage and AIChatMessage moved to their own files for single-class-per-file convention.
}
