using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using OllamaAgent.VSIX.Models;

namespace OllamaAgent.VSIX.Services
{
public class ModelStore : IModelStore, INotifyPropertyChanged
	{
		private ObservableCollection<LLM> _models = new ObservableCollection<LLM>();
		private LLM _selectedChatModel;
		private LLM _selectedCompletionModel;

		public ObservableCollection<LLM> Models
		{
			get => _models;
			set
			{
				if (_models != value)
				{
					_models = value;
					OnPropertyChanged(nameof(Models));
				}
			}
		}

		public LLM SelectedChatModel
		{
			get => _selectedChatModel;
			set
			{
				if (_selectedChatModel != value)
				{
					_selectedChatModel = value;
					OnPropertyChanged(nameof(SelectedChatModel));
				}
			}
		}

		public LLM SelectedCompletionModel
		{
			get => _selectedCompletionModel;
			set
			{
				if (_selectedCompletionModel != value)
				{
					_selectedCompletionModel = value;
					OnPropertyChanged(nameof(SelectedCompletionModel));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
