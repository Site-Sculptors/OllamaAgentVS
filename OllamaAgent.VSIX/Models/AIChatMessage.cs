using OllamaAgent.VSIX.Enums;

namespace OllamaAgent.VSIX.Models
{
   public class AIChatMessage : ChatMessageBase
   {
		  public AIChatMessage() {
		   Role = ChatRole.AI;
	   }

	   public AIChatMessage(string content = null, string[] images = null)
	   {
		   Role = ChatRole.AI;
		   Content = content;
		   Images = images;
	   }
   }
}
