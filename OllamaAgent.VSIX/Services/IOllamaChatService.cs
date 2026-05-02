using System.Threading;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Services;

public interface IOllamaChatService
{
	Task<string> GenerateCompletionAsync(string endpoint, string model, string prompt, CancellationToken token = default);

	/// <summary>
	/// Streams chat response from Ollama's /api/chat endpoint (NDJSON).
	/// </summary>
	/// <param name="endpoint">Ollama server URL</param>
	/// <param name="model">Model name</param>
	/// <param name="messages">Chat history/messages</param>
	/// <param name="onMessageFragment">Callback for each streamed fragment</param>
	/// <param name="token">Cancellation token</param>
	Task StreamChatAsync(
		string endpoint,
		string model,
		IEnumerable<(string role, string content)> messages,
		Action<string> onMessageFragment,
		CancellationToken token = default
	);
}
