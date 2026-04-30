using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using OllamaAgent.VSIX.Properties;

namespace OllamaAgent.VSIX.ViewModels
{
	public class OllamaOptionsViewModel : ViewModelBase
	{
		// Parameterless constructor for XAML
		public OllamaOptionsViewModel() : base(ViewModelBase.Instance.OllamaService, ViewModelBase.Instance.Package) { }

		public OllamaOptionsViewModel(OllamaAgent.VSIX.OllamaModelService ollamaModelService, OllamaAgentVSIXPackage package) : base(ollamaModelService, package)
		{
			// Load from user settings
			ModelsDirectory = Settings.Default.ModelsDirectory;
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