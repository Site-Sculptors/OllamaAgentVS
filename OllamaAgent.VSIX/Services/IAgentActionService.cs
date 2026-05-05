using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Services;

public interface IAgentActionService
{
	Task<bool> PreviewAndApplyCodeChangeAsync(string filePath, string newCode, string oldCode);
}
