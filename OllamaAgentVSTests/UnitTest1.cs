
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using FluentAssertions;
using Moq;
using OllamaAgent.VSIX.Models;
using OllamaAgent.VSIX.Services;
using OllamaAgent.VSIX.ViewModels;
using Xunit;

namespace OllamaAgentVSTests;


public class AgentQnAIntegrationTests
{
	[Fact]
	public async Task AgentQnA_ShouldAddAIResponseToChatHistory()
	{
		// Arrange
		var mockChatService = new Mock<IOllamaChatService>();
		var mockThreadStore = new Mock<IChatThreadStore>();
		var mockEditorContext = new Mock<IEditorContextService>();
		var mockSymbolExtractor = new Mock<ISymbolExtractorService>();
		var mockErrorList = new Mock<IErrorListService>();
		var mockOutputWindow = new Mock<IOutputWindowContextService>();

		// Setup mocks
		mockChatService.Setup(s => s.StreamChatAsync(
			It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<(string, string)>>(), It.IsAny<System.Action<string>>(), It.IsAny<CancellationToken>()))
			.Returns<string, string, IEnumerable<(string, string)>, System.Action<string>, CancellationToken>(
				(endpoint, model, messages, onFragment, token) =>
				{
					onFragment("Message: Hello!\nTitle: Test QnA");
					return Task.CompletedTask;
				});

		mockThreadStore.Setup(s => s.LoadGlobalThreadsAsync())
			.ReturnsAsync(new List<ChatThread> { new ChatThread { Name = "Test Thread" } });
		mockThreadStore.Setup(s => s.SaveThreadAsync(It.IsAny<ChatThread>())).Returns(Task.CompletedTask);

		mockEditorContext.Setup(s => s.GetActiveDocumentContextAsync())
			.ReturnsAsync(("file.cs", "CSharp", "class C{}", null));
		mockSymbolExtractor.Setup(s => s.ExtractSymbolSummaryAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(string.Empty);


		// Provide minimal implementations for required stores
		var modelStoreMock = new Mock<IModelStore>();
		modelStoreMock.SetupAllProperties();
		modelStoreMock.Setup(m => m.Models).Returns(new ObservableCollection<OllamaAgent.VSIX.Models.LLM>());

		var viewModelStateStoreMock = new Mock<IViewModelStateStore>();
		viewModelStateStoreMock.SetupAllProperties();

		var agentStoreMock = new Mock<IAgentStore>();
		agentStoreMock.SetupAllProperties();
		agentStoreMock.Setup(a => a.Agents).Returns(new ObservableCollection<AgentModel> { new AgentModel() });


		// Use mock IServiceProvider for dependency resolution
		var realCustomInstructions = new CustomInstructionsService();
		var mockServiceProvider = new Mock<IServiceProvider>();
		mockServiceProvider.Setup(s => s.GetService(typeof(IChatThreadStore))).Returns(mockThreadStore.Object);
		mockServiceProvider.Setup(s => s.GetService(typeof(CustomInstructionsService))).Returns(realCustomInstructions);
		var realSymbolExtractor = new SymbolExtractorService();
		mockServiceProvider.Setup(s => s.GetService(typeof(ISymbolExtractorService))).Returns(realSymbolExtractor);
		var mockAgentActionService = new Mock<IAgentActionService>();
		mockServiceProvider.Setup(s => s.GetService(typeof(IAgentActionService))).Returns(mockAgentActionService.Object);
		mockServiceProvider.Setup(s => s.GetService(typeof(IModelStore))).Returns(modelStoreMock.Object);
		mockServiceProvider.Setup(s => s.GetService(typeof(IViewModelStateStore))).Returns(viewModelStateStoreMock.Object);
		mockServiceProvider.Setup(s => s.GetService(typeof(IAgentStore))).Returns(agentStoreMock.Object);

		var vm = new ChatViewModel(
			mockChatService.Object,
			Mock.Of<IOllamaAgentService>(),
			Mock.Of<IOllamaModelService>(),
			mockServiceProvider.Object,
			modelStoreMock.Object,
			viewModelStateStoreMock.Object,
			agentStoreMock.Object,
			mockEditorContext.Object,
			mockErrorList.Object,
			mockOutputWindow.Object
		);

		// Set up required state
		vm.SelectedChatModel = new OllamaAgent.VSIX.Models.LLM { Name = "test-model" };
		vm.ActiveThread = new ChatThread { Name = "Test Thread" };
		vm.Input = "What is this?";

		// Act
		await vm.SendCommand.ExecuteAsync(null);

		// Assert
		vm.ChatHistory.Should().ContainSingle(m => m.Role == OllamaAgent.VSIX.Enums.ChatRole.AI && m.Content.Contains("Hello!"));
	}
}
