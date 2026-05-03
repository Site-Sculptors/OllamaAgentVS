using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace OllamaAgent.VSIX.Services
{


public class OllamaChatService : IOllamaChatService
{
	private readonly IOllamaApiService _apiService;

	public OllamaChatService(IOllamaApiService apiService)
	{
		_apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
	}

	public async Task<string> GenerateCompletionAsync(string endpoint, string model, string prompt, CancellationToken token = default)
	{
		try
		{
			var json = await _apiService.GenerateCompletionAsync(endpoint, model, prompt, token);
			if (string.IsNullOrWhiteSpace(json))
				return null;
			using (var doc = JsonDocument.Parse(json))
			{
				if (doc.RootElement.TryGetProperty("response", out var resp))
				{
					return resp.GetString();
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"OllamaChatService completion exception: {ex}");
		}
		return null;
	}

	public async Task StreamChatAsync(
		string endpoint,
		string model,
		IEnumerable<(string role, string content)> messages,
		Action<string> onMessageFragment,
		CancellationToken token = default)
	{
		try
		{
			using (var response = await _apiService.StreamChatAsync(endpoint, model, messages, token))
			using (var stream = await response.Content.ReadAsStreamAsync())
			using (var reader = new System.IO.StreamReader(stream))
			{
				string line;
				int fragCount = 0;
				while ((line = await reader.ReadLineAsync()) != null)
				{
					if (token.IsCancellationRequested)
					{
						System.Diagnostics.Debug.WriteLine($"[OllamaChatService] Streaming cancelled after {fragCount} fragments at {DateTime.Now:HH:mm:ss.fff}");
						break;
					}
					if (string.IsNullOrWhiteSpace(line))
						continue;
					try
					{
						using (var doc = JsonDocument.Parse(line))
						{
							if (doc.RootElement.TryGetProperty("message", out var msgElem))
							{
								var contentFrag = msgElem.GetProperty("content").GetString();
								if (!string.IsNullOrEmpty(contentFrag))
								{
									fragCount++;
									System.Diagnostics.Debug.WriteLine($"[OllamaChatService] Fragment {fragCount} at {DateTime.Now:HH:mm:ss.fff}: '{contentFrag?.Substring(0, Math.Min(contentFrag.Length, 40))}'");
									onMessageFragment(contentFrag);
								}
							}
						}
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"[OllamaChatService] NDJSON parse error: {ex.Message}\n{line}");
					}
				}
				System.Diagnostics.Debug.WriteLine($"[OllamaChatService] Streaming completed after {fragCount} fragments at {DateTime.Now:HH:mm:ss.fff}");
			}
		}
		catch (OperationCanceledException)
		{
			// Expected if user cancels
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"OllamaChatService streaming exception: {ex}");
		}
	}
}
}
