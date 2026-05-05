using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace OllamaAgent.VSIX.Models;

public class AgentStore : IAgentStore
{
	public ObservableCollection<AgentModel> Agents { get; } = new ObservableCollection<AgentModel>();

	public AgentStore()
	{
		// Seed with two default agents: Ask and Agent
		Agents.Add(new AgentModel
		{
			Name = "Ask",
			Description = "Default agent for general questions and completions.",
			IsEnabled = true,
			Version = "1.0"
		});
		Agents.Add(new AgentModel
		{
			Name = "Agent",
			Description = "Specialized agent for advanced or custom tasks.",
			IsEnabled = true,
			Version = "1.0"
		});
	}

	public void AddAgent(AgentModel agent)
	{
		if (agent == null) throw new ArgumentNullException(nameof(agent));
		if (!Agents.Contains(agent))
			Agents.Add(agent);
	}

	public void RemoveAgent(AgentModel agent)
	{
		if (agent == null) throw new ArgumentNullException(nameof(agent));
		Agents.Remove(agent);
	}

	public AgentModel GetAgentById(Guid id)
	{
		return Agents.FirstOrDefault(a => a.Id == id);
	}
}
