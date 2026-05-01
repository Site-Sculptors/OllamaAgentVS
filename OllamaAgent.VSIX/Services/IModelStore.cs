using System.Collections.ObjectModel;
using System.ComponentModel;

using OllamaAgent.VSIX.Models;

namespace OllamaAgent.VSIX.Services
{
	public interface IModelStore : INotifyPropertyChanged
	{
		ObservableCollection<LLM> Models { get; set; }
		LLM SelectedModel { get; set; }
	}
}