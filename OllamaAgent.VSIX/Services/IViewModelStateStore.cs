using System.ComponentModel;

using OllamaAgent.VSIX.Enums;

namespace OllamaAgent.VSIX.Services
{
	public interface IViewModelStateStore : INotifyPropertyChanged
	{
		ServerStatus Status { get; set; }
		string TestConnectionMessage { get; set; }
		string OllamaEndpoint { get; set; }
		string ModelsDirectory { get; set; }
		string ChatsDirectory { get; set; }
		bool ExtensionEnabled { get; set; }
		bool AutoAttachActiveDocument { get; set; }
		bool ReferenceSolutionEnabled { get; set; }
	}
}
