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
			switch (item)
			{
				case UserChatMessage:
					return UserTemplate;
				case AIChatMessage:
					return AITemplate;
				default:
					return base.SelectTemplate(item, container);
			}
		}
	}
}
