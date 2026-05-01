using System.Collections.ObjectModel;
using OllamaAgent.VSIX.Models;

namespace OllamaAgent.VSIX.Services
{
	public interface IModelStore
	{
		ObservableCollection<LLM> Models { get; set; }
		LLM SelectedModel { get; set; }
	}
}
