using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OllamaAgent.VSIX.Enums;

namespace OllamaAgent.VSIX.Models;

public class ChatMessage
{
	public ChatRole Role { get; set; }
	public string Message { get; set; }
	public string Display => $"{Role}: {Message}";
}
