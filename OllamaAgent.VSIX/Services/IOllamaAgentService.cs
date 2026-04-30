using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OllamaAgent.VSIX.Enums;

namespace OllamaAgent.VSIX.Services
{
	public interface IOllamaAgentService : IDisposable
	{
		event EventHandler<ServerStatus> StatusChanged;
		event EventHandler<IReadOnlyList<string>> ModelsChanged;

		ServerStatus Status { get; }
		IReadOnlyList<string> Models { get; }

		Task StartMonitoringAsync(CancellationToken token = default);
		Task<IReadOnlyList<string>> GetModelsAsync(string endpoint);
		Task CheckOllamaOnlineAsync(string endpoint, CancellationToken token = default);

		Task<string> GenerateCompletionAsync(string endpoint, string model, string prompt);
	}
}
