using OllamaAgent.VSIX.Enums;

namespace OllamaAgent.VSIX.Models
{
   public class UserChatMessage : ChatMessageBase
   {
	   public UserChatMessage() {
		   Role = ChatRole.User;
	   }

	   public UserChatMessage(string content = null, string[] images = null)
	   {
		   Role = ChatRole.User;
		   Content = content;
		   Images = images;
	   }
   }
}
