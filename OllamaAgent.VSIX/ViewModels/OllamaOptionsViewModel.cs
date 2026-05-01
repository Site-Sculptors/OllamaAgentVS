using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

using OllamaAgent.VSIX.Properties;
using OllamaAgent.VSIX.Services;

namespace OllamaAgent.VSIX.ViewModels
{
	public class OllamaOptionsViewModel : ViewModelBase
	{


		public OllamaOptionsViewModel(IOllamaAgentService ollamaAgentService, IOllamaModelService ollamaModelService, OllamaAgentVSIXPackage package)
			: base(ollamaAgentService, ollamaModelService, package)
		{
			// Load from user settings
			ModelsDirectory = Settings.Default.ModelsDirectory;

			// Subscribe to settings changes
			Settings.Default.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(Settings.Default.SelectedModel))
				{
					OnPropertyChanged(nameof(SelectedModel));
				}
			};
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
	}
}