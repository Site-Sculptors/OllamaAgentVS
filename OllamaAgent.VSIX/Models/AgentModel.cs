using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Models;

public class AgentModel
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; }
	public string Description { get; set; }
	public string ModelPath { get; set; }
	public List<string> Capabilities { get; set; } = new List<string>();
	public bool IsEnabled { get; set; }
	public string Version { get; set; }
}
