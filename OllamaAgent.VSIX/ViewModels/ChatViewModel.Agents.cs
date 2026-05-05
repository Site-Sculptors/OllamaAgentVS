using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.ViewModels;

using OllamaAgent.VSIX.Models;
using OllamaAgent.VSIX.Services;
using System;
using System.Threading.Tasks;

public partial class ChatViewModel
{
	private readonly IAgentActionService _agentActionService;

	// AGENT-RELATED LOGIC
	// Branch for agent mode in SendCommand
	private async Task<bool> TryApplyAgentCodeChangeAsync(string fileName, string newCode, string oldCode)
	{
		if (_agentActionService != null && !string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(oldCode))
		{
			return await _agentActionService.PreviewAndApplyCodeChangeAsync(fileName, newCode, oldCode);
		}
		return false;
	}

	// Helper to determine if agent mode is active
	private bool IsAgentMode => SelectedAgent != null && string.Equals(SelectedAgent.Name, "Agent", StringComparison.OrdinalIgnoreCase);
}
