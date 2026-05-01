using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using OllamaAgent.VSIX.Services;
using OllamaAgent.VSIX.ViewModels;

namespace OllamaAgent.VSIX.DependencyInjection;

public static class DependencyInjectionRegistration
{
	public static void ConfigureServices(IServiceCollection services)
	{
		// Register your services here
		services.AddSingleton<IOllamaAgentService, OllamaAgentService>();
		services.AddSingleton<IOllamaChatService, OllamaChatService>();
		services.AddSingleton<IOllamaModelService, OllamaModelService>();
		services.AddSingleton<IModelStore, ModelStore>();
	}
	public static void ConfigureViewModels(IServiceCollection services)
	{
		// Register your view models here

		services.AddSingleton<ViewModelBase>();
		services.AddSingleton<OllamaOptionsViewModel>();
		services.AddSingleton<ChatViewModel>();
	}
}
