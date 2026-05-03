using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using OllamaAgent.VSIX.Models;
using OllamaAgent.VSIX.Properties;

namespace OllamaAgent.VSIX.Services
{
	public class ChatThreadStore : IChatThreadStore
	{
		private const string DefaultSubDir = "OllamaAgent/chats";

		public string GetStorageDirectory()
		{
			var dir = Settings.Default.ChatsDirectory;
			if (string.IsNullOrWhiteSpace(dir))
			{
				dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DefaultSubDir);
			}
			return dir;
		}

		public async Task<List<ChatThread>> LoadThreadsForSolutionAsync(string solutionPath)
		{
			var all = await LoadAllThreadsAsync();
			return all.Where(t => t.SolutionPath == solutionPath).ToList();
		}

		public async Task<List<ChatThread>> LoadGlobalThreadsAsync()
		{
			var all = await LoadAllThreadsAsync();
			return all.Where(t => string.IsNullOrWhiteSpace(t.SolutionPath)).ToList();
		}

		public async Task SaveThreadAsync(ChatThread thread)
		{
			try
			{
				var dir = GetStorageDirectory();
				Directory.CreateDirectory(dir);
				var file = Path.Combine(dir, thread.Id + ".json");
				var dto = ToDto(thread);
			   var settings = new JsonSerializerSettings
			   {
				   TypeNameHandling = TypeNameHandling.Auto,
				   Formatting = Formatting.Indented
			   };
			   var json = JsonConvert.SerializeObject(dto, settings);
			   await Task.Run(() => File.WriteAllText(file, json));
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ChatThreadStore] SaveThreadAsync failed: {ex}");
			}
		}

		public async Task DeleteThreadAsync(string threadId)
		{
			try
			{
				var dir = GetStorageDirectory();
				var file = Path.Combine(dir, threadId + ".json");
				if (File.Exists(file))
					File.Delete(file);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ChatThreadStore] DeleteThreadAsync failed: {ex}");
			}
		}

		private async Task<List<ChatThread>> LoadAllThreadsAsync()
		{
			var dir = GetStorageDirectory();
			var result = new List<ChatThread>();
			if (!Directory.Exists(dir))
				return result;
			var files = Directory.GetFiles(dir, "*.json");
			foreach (var file in files)
			{
				try
				{
					var json = await Task.Run(() => File.ReadAllText(file));
					var settings = new JsonSerializerSettings
					{
						TypeNameHandling = TypeNameHandling.Auto
					};
					var dto = JsonConvert.DeserializeObject<ChatThreadDto>(json, settings);
					if (dto != null)
						result.Add(FromDto(dto));
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"[ChatThreadStore] Failed to load {file}: {ex}");
				}
			}
			// Only keep threads that are usable (have messages or are new)
			var usableThreads = result.Where(t => t.Messages != null && t.Messages.Count > 0).ToList();
			// If none are usable, create a new thread
			if (usableThreads.Count == 0)
			{
				usableThreads.Add(new ChatThread
				{
					Name = "New Thread",
					CreatedAt = DateTime.UtcNow,
					LastActivityAt = DateTime.UtcNow,
					Messages = new ObservableCollection<ChatMessageBase>()
				});
			}
			return usableThreads;
		}

		// DTO for serialization
		private class ChatThreadDto
		{
			public string Id { get; set; }
			public string Name { get; set; }
			public string SolutionPath { get; set; }
			public string ModelName { get; set; }
			public bool IsAutoNamed { get; set; }
			public DateTime CreatedAt { get; set; }
			public DateTime LastActivityAt { get; set; }
		   public List<ChatMessageBase> Messages { get; set; }
		}

		   private static ChatThreadDto ToDto(ChatThread thread)
		   {
			   return new ChatThreadDto
			   {
				   Id = thread.Id,
				   Name = thread.Name,
				   SolutionPath = thread.SolutionPath,
				   ModelName = thread.ModelName,
				   IsAutoNamed = thread.IsAutoNamed,
				   CreatedAt = thread.CreatedAt,
				   LastActivityAt = thread.LastActivityAt,
				   Messages = thread.Messages?.ToList() ?? new List<ChatMessageBase>()
			   };
		   }

		   private static ChatThread FromDto(ChatThreadDto dto)
		   {
			   return new ChatThread
			   {
				   Id = dto.Id,
				   Name = dto.Name,
				   SolutionPath = dto.SolutionPath,
				   ModelName = dto.ModelName,
				   IsAutoNamed = dto.IsAutoNamed,
				   CreatedAt = dto.CreatedAt,
				   LastActivityAt = dto.LastActivityAt,
				   Messages = new ObservableCollection<ChatMessageBase>(dto.Messages ?? new List<ChatMessageBase>())
			   };
		   }
	}
}
