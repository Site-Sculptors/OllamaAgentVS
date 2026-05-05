using System.Collections.ObjectModel;

namespace OllamaAgent.VSIX.Models;

public interface IAgentStore
{
	ObservableCollection<AgentModel> Agents { get; }
	void AddAgent(AgentModel agent);
	void RemoveAgent(AgentModel agent);
	AgentModel GetAgentById(System.Guid id);
}