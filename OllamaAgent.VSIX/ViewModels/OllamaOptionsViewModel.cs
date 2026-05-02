using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

using OllamaAgent.VSIX.Properties;
using OllamaAgent.VSIX.Services;
using OllamaAgent.VSIX.Models;

namespace OllamaAgent.VSIX.ViewModels
{
	public class OllamaOptionsViewModel : ViewModelBase
	{
		public OllamaOptionsViewModel(IOllamaAgentService ollamaAgentService, IOllamaModelService ollamaModelService, OllamaAgentVSIXPackage package, IModelStore modelStore)
			: base(ollamaAgentService, ollamaModelService, package, modelStore)
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
	}
}