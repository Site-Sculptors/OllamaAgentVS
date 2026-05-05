using System.Threading.Tasks;
using System.Windows;

namespace OllamaAgent.VSIX.Services;

public class AgentActionService : IAgentActionService
{
	public async Task<bool> PreviewAndApplyCodeChangeAsync(string filePath, string newCode, string oldCode)
	{
		// Try to extract a C# code block from the agent/model response
		string codeToApply = ExtractCodeBlockOrFull(newCode);

		var result = MessageBox.Show(
			$"Apply the following change to {filePath}?\n\n--- Old ---\n{oldCode}\n--- New ---\n{codeToApply}",
			"Preview Code Change",
			MessageBoxButton.YesNo,
			MessageBoxImage.Question);
		if (result == MessageBoxResult.Yes)
		{
			System.IO.File.WriteAllText(filePath, codeToApply);
			return true;
		}
		return false;
	}

	// Extracts the first C# code block from markdown, or returns the full text if none found
	private string ExtractCodeBlockOrFull(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return text;
		const string codeFence = "```csharp";
		int start = text.IndexOf(codeFence, System.StringComparison.OrdinalIgnoreCase);
		if (start >= 0)
		{
			start += codeFence.Length;
			int end = text.IndexOf("```", start, System.StringComparison.OrdinalIgnoreCase);
			if (end > start)
			{
				return text.Substring(start, end - start).Trim();
			}
		}
		// fallback: try generic code block
		const string genericFence = "```";
		start = text.IndexOf(genericFence, System.StringComparison.OrdinalIgnoreCase);
		if (start >= 0)
		{
			start += genericFence.Length;
			int end = text.IndexOf("```", start, System.StringComparison.OrdinalIgnoreCase);
			if (end > start)
			{
				return text.Substring(start, end - start).Trim();
			}
		}
		// fallback: return full text
		return text.Trim();
	}
}
