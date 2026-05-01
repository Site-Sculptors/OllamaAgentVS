using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OllamaAgent.VSIX.Enums;

namespace OllamaAgent.VSIX.Services
{
	public class OllamaAgentService : IOllamaAgentService
	{
		private readonly HttpClient _httpClient = new HttpClient();
		private readonly ObservableCollection<string> _models = new ObservableCollection<string>();
		private ServerStatus _status;
		private CancellationTokenSource _monitorCts;
		private string _lastEndpoint;

		public event EventHandler<ServerStatus> StatusChanged;
		public event EventHandler<IReadOnlyList<string>> ModelsChanged;

		public ServerStatus Status => _status;
		public IReadOnlyList<string> Models => _models;

		public async Task StartMonitoringAsync(CancellationToken token = default)
		{
			_monitorCts = CancellationTokenSource.CreateLinkedTokenSource(token);
			await MonitorLoop(_monitorCts.Token);
		}

		private async Task MonitorLoop(CancellationToken token)
		{
			// Aggressive polling: every 2s for up to 30s or until online
			using (var aggressiveCts = CancellationTokenSource.CreateLinkedTokenSource(token))
			{
				aggressiveCts.CancelAfter(TimeSpan.FromSeconds(30));
				while (!aggressiveCts.Token.IsCancellationRequested && !token.IsCancellationRequested)
				{
					await CheckOllamaOnlineAsync(_lastEndpoint, aggressiveCts.Token);
					if (_status == ServerStatus.Online)
						break;
					await Task.Delay(2000, aggressiveCts.Token);
				}
			}
			// Normal polling: every 30s
			while (!token.IsCancellationRequested)
			{
				await CheckOllamaOnlineAsync(_lastEndpoint, token);
				for (int i = 0; i < 30; i++)
				{
					if (token.IsCancellationRequested) return;
					await Task.Delay(1000, token);
				}
			}
		}

		public async Task CheckOllamaOnlineAsync(string endpoint, CancellationToken token = default)
		{
			_lastEndpoint = endpoint;
			try
			{
				var url = endpoint.TrimEnd('/') + "/api/tags";
				using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
				cts.CancelAfter(TimeSpan.FromSeconds(2));
				using var response = await _httpClient.GetAsync(url, cts.Token);
				var newStatus = response.IsSuccessStatusCode ? ServerStatus.Online : ServerStatus.Offline;
				if (newStatus != _status)
				{
					_status = newStatus;
					StatusChanged?.Invoke(this, _status);
				}
				if (_status == ServerStatus.Online)
				{
					await RefreshModels(endpoint);
				}
			}
			catch
			{
				if (_status != ServerStatus.Offline)
				{
					_status = ServerStatus.Offline;
					StatusChanged?.Invoke(this, _status);
				}
			}
		}

		public async Task<IReadOnlyList<string>> GetModelsAsync(string endpoint)
		{
			var url = endpoint.TrimEnd('/') + "/api/tags";
			var models = new List<string>();
			try
			{
				var response = await _httpClient.GetAsync(url);
				if (response.IsSuccessStatusCode)
				{
					// TODO: Parse response JSON to extract model names
					// For now, simulate with dummy data
					models.Add("model1");
					models.Add("model2");
				}
			}
			catch { }
			return models;
		}

		private async Task RefreshModels(string endpoint)
		{
			var models = await GetModelsAsync(endpoint);
			bool changed = !_models.SequenceEqual(models);
			if (changed)
			{
				_models.Clear();
				foreach (var m in models)
					_models.Add(m);
				ModelsChanged?.Invoke(this, _models.ToList());
			}
		}



		public void Dispose()
		{
			_monitorCts?.Cancel();
			_monitorCts?.Dispose();
		}
	}
}
