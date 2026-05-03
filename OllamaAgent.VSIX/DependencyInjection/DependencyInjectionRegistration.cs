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
	/// <summary>
	/// This file is only useful if you are using your own DI container (e.g., ServiceCollection) in your own code.
	/// Visual Studio does NOT use this for service resolution. For VS integration, use AddService in your AsyncPackage.
	/// </summary>

	public static void ConfigureStores(IServiceCollection services)
	{
		services.AddSingleton<IModelStore, ModelStore>();
	}

	public static void ConfigureServices(IServiceCollection services)
	{
	   services.AddSingleton<IOllamaApiService, OllamaApiService>();
	   services.AddSingleton<IOllamaAgentService, OllamaAgentService>();
	   services.AddSingleton<IOllamaChatService, OllamaChatService>();
	   services.AddSingleton<IOllamaModelService, OllamaModelService>();
	}

	public static void ConfigureViewModels(IServiceCollection services)
	{
		services.AddSingleton<ViewModelBase>();
		services.AddSingleton<OllamaOptionsViewModel>();
		services.AddSingleton<ChatViewModel>();
	}
}
