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
		public OllamaOptionsViewModel(IOllamaAgentService ollamaAgentService, IOllamaModelService ollamaModelService, OllamaAgentVSIXPackage package, IModelStore modelStore, IViewModelStateStore viewModelStateStore)
			: base(ollamaAgentService, ollamaModelService, package, modelStore, viewModelStateStore)
		{
		}

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
					ModelsDirectory = dialog.SelectedPath;
					await SafeLoadAsync();
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

		//private IAsyncRelayCommand _extensionEnabledToggledCommand;
		//public IAsyncRelayCommand ExtensionEnabledToggledCommand =>
		//	_extensionEnabledToggledCommand ??= new AsyncRelayCommand<object>(async (parameter) =>
		//	{
		//		if (ExtensionEnabled)
		//		{
		//			if (Status == ServerStatus.Disabled)
		//			{
		//				Status = ServerStatus.Unknown;

		//				await EnsureServerOnlineAsync();
		//			}

		//		}
		//		else
		//		{
		//			Status = ServerStatus.Disabled;
		//		}

		//	});
	}
}