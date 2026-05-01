using System.Threading;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Services;

public interface IOllamaChatService
{
	Task<string> GenerateCompletionAsync(string endpoint, string model, string prompt, CancellationToken token = default);
}
