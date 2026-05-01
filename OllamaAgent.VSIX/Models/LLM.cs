using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Models;

public class LLM
{
	// Core Modelfile instructions
	public string Name { get; set; }
	public string Description { get; set; }
	public string Version { get; set; }
	public string Author { get; set; }
	public string License { get; set; }
	public string Path { get; set; }
	public long? SizeBytes { get; set; }
	public DateTime? Created { get; set; }
	public DateTime? Updated { get; set; }
	public bool IsEnabled { get; set; }

	// Modelfile-specific properties
	public string From { get; set; } // FROM instruction
	public string System { get; set; } // SYSTEM message
	public string Template { get; set; } // TEMPLATE
	public string Adapter { get; set; } // ADAPTER
	public string Message { get; set; } // MESSAGE

	// Model parameters
	public double? Temperature { get; set; }
	public int? NumCtx { get; set; }
	public double? TopP { get; set; }
	public int? TopK { get; set; }
	public string[] Stop { get; set; }
	public double? RepeatPenalty { get; set; }
	public int? NumPredict { get; set; }
	public int? Seed { get; set; }
}
