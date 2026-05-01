using System;
using System.Collections.ObjectModel;

namespace OllamaAgent.VSIX.Models
{
	public class ChatThread
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string Name { get; set; }
		public string SolutionPath { get; set; }  // filter by open solution
		public string ModelName { get; set; }      // which LLM was used
		public bool IsAutoNamed { get; set; } = true;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
		public ObservableCollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();
	}
}
