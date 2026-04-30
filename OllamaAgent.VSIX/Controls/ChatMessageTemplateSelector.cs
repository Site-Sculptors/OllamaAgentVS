using System.Windows;
using System.Windows.Controls;

using OllamaAgent.VSIX.Enums;
using OllamaAgent.VSIX.Models;

namespace OllamaAgent.VSIX.Controls
{
	public class ChatMessageTemplateSelector : DataTemplateSelector
	{
		public DataTemplate UserTemplate { get; set; }
		public DataTemplate AITemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var message = item as ChatMessage;
			if (message == null)
				return base.SelectTemplate(item, container);

			if (message.Role == ChatRole.User)
				return UserTemplate;
			else
				return AITemplate;
		}
	}
}
