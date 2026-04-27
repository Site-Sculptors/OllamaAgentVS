using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaAgent.VSIX
{
	internal sealed class OllamaModelService
	{
		private static readonly HttpClient _httpClient = new HttpClient();

		public async Task<List<string>> GetModelsAsync(string endpoint, CancellationToken token = default)
		{
			if (string.IsNullOrWhiteSpace(endpoint))
				throw new ArgumentException("Ollama endpoint is required.", nameof(endpoint));

			var url = endpoint.TrimEnd('/') + "/api/tags";

			try
			{
				using (var response = await _httpClient.GetAsync(url, token))
				{
					var json = await response.Content.ReadAsStringAsync();

					if (!response.IsSuccessStatusCode)
					{
						System.Diagnostics.Debug.WriteLine(
							$"Ollama call failed: {response.StatusCode} - {json}");

						return new List<string>();
					}

					if (string.IsNullOrWhiteSpace(json))
						return new List<string>();

					using (var doc = JsonDocument.Parse(json))
					{
						var results = new List<string>();

						if (doc.RootElement.TryGetProperty("models", out var models))
						{
							foreach (var model in models.EnumerateArray())
							{
								if (model.TryGetProperty("name", out var name))
								{
									var value = name.GetString();
									if (!string.IsNullOrWhiteSpace(value))
										results.Add(value);
								}
							}
						}

						return results;
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(
					$"OllamaModelService exception: {ex}");

				return new List<string>();
			}
		}
	}
}