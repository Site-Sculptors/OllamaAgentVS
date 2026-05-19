using CommunityToolkit.Mvvm.Input;

using OllamaAgent.VSIX.Enums;
using OllamaAgent.VSIX.Models;
using OllamaAgent.VSIX.Properties;
using OllamaAgent.VSIX.Services;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace OllamaAgent.VSIX.ViewModels
{
	public class OllamaOptionsViewModel : ViewModelBase
	{
		public OllamaOptionsViewModel(IOllamaAgentService ollamaAgentService, IOllamaModelService ollamaModelService, IServiceProvider serviceProvider, IModelStore modelStore, IViewModelStateStore viewModelStateStore, IAgentStore agentStore)
		   : base(ollamaAgentService, ollamaModelService, serviceProvider, modelStore, viewModelStateStore, agentStore)
		{
		}

		private AsyncRelayCommand _reloadCommand;
		public IAsyncRelayCommand ReloadCommand =>
			(IAsyncRelayCommand)(_reloadCommand ?? (_reloadCommand = new AsyncRelayCommand(async () => await SafeLoadAsync())));

		private ICommand _selectModelsDirectoryCommand;
		public ICommand SelectModelsDirectoryCommand =>
			_selectModelsDirectoryCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		{
			using (var dialog = new FolderBrowserDialog())
			{
				dialog.Description = "Select Ollama Models Directory";
				dialog.SelectedPath = ModelsDirectory;
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					// Stop Ollama server if running
					foreach (var proc in System.Diagnostics.Process.GetProcessesByName("ollama"))
					{
						try { proc.Kill(); } catch { }
					}

					ModelsDirectory = dialog.SelectedPath;
					await SafeLoadAsync();

					// Start Ollama server with new directory
					// This assumes StartServerCommand is available in the base class
					if (StartServerCommand != null && StartServerCommand.CanExecute(null))
					{
						await ((IAsyncRelayCommand)StartServerCommand).ExecuteAsync(null);
					}
				}
			}
		});


		public override LLM SelectedCompletionModel
		{
			get => base.SelectedCompletionModel;
			set => base.SelectedCompletionModel = value;
		}

		public override LLM SelectedChatModel
		{
			get => base.SelectedChatModel;
			set => base.SelectedChatModel = value;
		}
	}
}