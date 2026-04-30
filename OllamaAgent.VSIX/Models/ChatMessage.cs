using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Models;

public class ChatMessage
{
	public string Sender { get; set; }
	public string Message { get; set; }
	public string Display => $"{Sender}: {Message}";
	public bool IsUser => Sender == "User";
}
