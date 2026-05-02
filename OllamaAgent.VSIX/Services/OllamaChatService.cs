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

namespace OllamaAgent.VSIX.Services;


public class OllamaChatService : IOllamaChatService
{
	private static readonly HttpClient _httpClient = new HttpClient();

	public async Task<string> GenerateCompletionAsync(string endpoint, string model, string prompt, CancellationToken token = default)
	{
		if (string.IsNullOrWhiteSpace(endpoint))
			throw new ArgumentException("Ollama endpoint is required.", nameof(endpoint));
		if (string.IsNullOrWhiteSpace(model))
			throw new ArgumentException("Model is required.", nameof(model));
		if (string.IsNullOrWhiteSpace(prompt))
			throw new ArgumentException("Prompt is required.", nameof(prompt));

		var url = endpoint.TrimEnd('/') + "/api/generate";
		var request = new
		{
			model = model,
			prompt = prompt,
			stream = false
		};
		var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

		try
		{
			using (var response = await _httpClient.PostAsync(url, content, token))
			{
				var json = await response.Content.ReadAsStringAsync();
				if (!response.IsSuccessStatusCode)
				{
					System.Diagnostics.Debug.WriteLine($"OllamaChatService generate failed: {response.StatusCode} - {json}");
					return null;
				}
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
		if (string.IsNullOrWhiteSpace(endpoint))
			throw new ArgumentException("Ollama endpoint is required.", nameof(endpoint));
		if (string.IsNullOrWhiteSpace(model))
			throw new ArgumentException("Model is required.", nameof(model));
		if (messages == null)
			throw new ArgumentException("Messages are required.", nameof(messages));

		var url = endpoint.TrimEnd('/') + "/api/chat";
		var payload = new
		{
			model = model,
			messages = messages.Select(m => new { role = m.role, content = m.content }).ToArray(),
			stream = true
		};
		var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

		try
		{
			using (var response = await _httpClient.PostAsync(url, content, HttpCompletionOption.ResponseHeadersRead, token))
			{
				response.EnsureSuccessStatusCode();
				using (var stream = await response.Content.ReadAsStreamAsync(token))
				using (var reader = new System.IO.StreamReader(stream))
				{
					string line;
					while ((line = await reader.ReadLineAsync()) != null)
					{
						if (token.IsCancellationRequested)
							break;
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
										onMessageFragment(contentFrag);
								}
							}
						}
						catch (Exception ex)
						{
							System.Diagnostics.Debug.WriteLine($"[OllamaChatService] NDJSON parse error: {ex.Message}\n{line}");
						}
					}
				}
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
