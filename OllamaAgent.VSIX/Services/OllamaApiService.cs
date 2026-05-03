using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Linq;


namespace OllamaAgent.VSIX.Services
{
	public class OllamaApiService : IOllamaApiService
	{
		private readonly HttpClient _httpClient;

		public OllamaApiService(HttpClient httpClient = null)
		{
			_httpClient = httpClient ?? new HttpClient();
		}

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

			using (var response = await _httpClient.PostAsync(url, content, token))
			{
				var json = await response.Content.ReadAsStringAsync();
				if (!response.IsSuccessStatusCode)
					throw new HttpRequestException($"OllamaApiService generate failed: {response.StatusCode} - {json}");
				return json;
			}
		}

		public async Task<HttpResponseMessage> StreamChatAsync(string endpoint, string model, IEnumerable<(string role, string content)> messages, CancellationToken token = default)
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

			var response = await _httpClient.PostAsync(url, content, token);
			if (!response.IsSuccessStatusCode)
			{
				var json = await response.Content.ReadAsStringAsync();
				response.Dispose();
				throw new HttpRequestException($"OllamaApiService stream chat failed: {response.StatusCode} - {json}");
			}
			return response;
		}
	}
}
