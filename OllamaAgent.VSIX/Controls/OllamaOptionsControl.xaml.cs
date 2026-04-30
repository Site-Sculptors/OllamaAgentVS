using OllamaAgent.VSIX.ViewModels;

using System.Windows.Controls;

namespace OllamaAgent.VSIX.Controls
{
	public partial class OllamaOptionsControl : UserControl
	{
		public OllamaOptionsViewModel ViewModel => DataContext as OllamaOptionsViewModel;

		public OllamaOptionsControl()
		{
			InitializeComponent();
			// Ensure singleton is initialized before this control is created
			if (ViewModelBase.Instance == null)
			{
				// You may need to pass the correct package instance here
				ViewModelBase.InitializeSingleton(new OllamaAgent.VSIX.OllamaModelService(), (OllamaAgentVSIXPackage)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(OllamaAgentVSIXPackage)));
			}
			// Use the singleton instance for DataContext
			DataContext = ViewModelBase.Instance;
		}
	}
}