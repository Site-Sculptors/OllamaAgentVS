using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;

using Microsoft.VisualStudio.Shell;

namespace OllamaAgent.VSIX
{
	public class OllamaAgentOptionsPage : DialogPage
	{
		private string ollamaEndpoint = "http://localhost:11434";
		private string defaultModel = "qwen2.5-coder:14b";
		private bool agentEnabled = true;

		private List<string> availableModels = new List<string>();

		// -----------------------------
		// CORE SETTINGS
		// -----------------------------

		[Category("General")]
		[DisplayName("Ollama Endpoint")]
		[Description("URL of the local Ollama server (example: http://localhost:11434).")]
		public string OllamaEndpoint
		{
			get => ollamaEndpoint;
			set => ollamaEndpoint = value;
		}

		[Category("General")]
		[DisplayName("Agent Enabled")]
		[Description("Enable or disable the Ollama Agent.")]
		public bool AgentEnabled
		{
			get => agentEnabled;
			set => agentEnabled = value;
		}

		[Category("General")]
		[DisplayName("Default Model")]
		[Description("Model used for code generation and analysis.")]
		[Editor(typeof(ModelDropdownEditor), typeof(UITypeEditor))]
		public string DefaultModel
		{
			get => defaultModel;
			set => defaultModel = value;
		}

		// -----------------------------
		// MODEL STORAGE (runtime only)
		// -----------------------------

		[Browsable(false)]
		public List<string> AvailableModels
		{
			get => availableModels;
			set => availableModels = value;
		}

		// -----------------------------
		// ACTION: REFRESH MODELS
		// -----------------------------

		[Category("Actions")]
		[DisplayName("Refresh Models")]
		[Description("Fetch available models from Ollama server.")]
		[Editor(typeof(RefreshModelsEditor), typeof(UITypeEditor))]
		public string RefreshModelsTrigger
		{
			get => "Refresh";
			set { }
		}

		// -----------------------------
		// ACTION: TEST CONNECTION
		// -----------------------------

		[Category("Actions")]
		[DisplayName("Test Connection")]
		[Description("Test connection to Ollama endpoint.")]
		[Editor(typeof(TestConnectionEditor), typeof(UITypeEditor))]
		public string TestConnectionTrigger
		{
			get => "Test";
			set { }
		}
	}
}