using System.Collections.Generic;
using System.Linq;
using OllamaAgent.VSIX.Enums;

namespace OllamaAgent.VSIX.Models
{
	/// <summary>
	/// Represents a conversation thread with message history and context.
	/// </summary>
	public class ChatConversation
	{
		private readonly List<ChatMessageBase> _messages = new List<ChatMessageBase>();

		public IReadOnlyList<ChatMessageBase> Messages => _messages;

		public void AddMessage(ChatMessageBase message)
		{
			if (message != null)
				_messages.Add(message);
		}

		public void AddUserMessage(string content, string[] images = null)
		{
			AddMessage(new UserChatMessage(content, images));
		}

		public void AddAIMessage(string content, string[] images = null)
		{
			AddMessage(new AIChatMessage(content, images));
		}

		public ChatMessageBase GetLastMessage() => _messages.LastOrDefault();

		public void Clear() => _messages.Clear();
	}
}
