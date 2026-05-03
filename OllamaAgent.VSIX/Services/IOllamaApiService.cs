using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX.Services
{
	public interface IOllamaApiService
	{
		Task<string> GenerateCompletionAsync(string endpoint, string model, string prompt, CancellationToken token = default);
		Task<HttpResponseMessage> StreamChatAsync(string endpoint, string model, IEnumerable<(string role, string content)> messages, CancellationToken token = default);
	}
}
