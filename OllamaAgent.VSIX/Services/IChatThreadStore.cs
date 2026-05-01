using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OllamaAgent.VSIX.Models;

namespace OllamaAgent.VSIX.Services
{
	public interface IChatThreadStore
	{
		Task<List<ChatThread>> LoadThreadsForSolutionAsync(string solutionPath);
		Task<List<ChatThread>> LoadGlobalThreadsAsync();
		Task SaveThreadAsync(ChatThread thread);
		Task DeleteThreadAsync(string threadId);
		string GetStorageDirectory();
	}
}
