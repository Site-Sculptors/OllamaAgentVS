using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Services;

public interface IOllamaModelService
{
	Task<string> GenerateCompletionAsync(string endpoint, string model, string prompt, CancellationToken token = default);
	Task<List<string>> GetModelsAsync(string endpoint, CancellationToken token = default);
}