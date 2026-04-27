using System.ComponentModel;

using Microsoft.VisualStudio.Shell;

namespace OllamaAgent.VSIX
{
	public class OllamaAgentOptionsPage : DialogPage
	{
		private string ollamaEndpoint = "http://localhost:11434";
		private string defaultModel = "qwen2.5-coder:14b";
		private bool agentEnabled = true;

		[Category("General")]
		[DisplayName("Ollama Endpoint")]
		[Description("URL of the local Ollama server.")]
		public string OllamaEndpoint
		{
			get => ollamaEndpoint;
			set => ollamaEndpoint = value;
		}

		[Category("General")]
		[DisplayName("Default Model")]
		[Description("Default model used by the agent.")]
		public string DefaultModel
		{
			get => defaultModel;
			set => defaultModel = value;
		}

		[Category("General")]
		[DisplayName("Agent Enabled")]
		[Description("Enable or disable the Ollama Agent.")]
		public bool AgentEnabled
		{
			get => agentEnabled;
			set => agentEnabled = value;
		}
	}
}