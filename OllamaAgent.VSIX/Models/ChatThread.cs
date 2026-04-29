using System.Collections.ObjectModel;

namespace OllamaAgent.VSIX.Models
{
	public class ChatThread
	{
		public string Id { get; set; } // Unique identifier (could be a GUID or timestamp string)
		public string Name { get; set; } // User-friendly name
		public ObservableCollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();
	}
}
