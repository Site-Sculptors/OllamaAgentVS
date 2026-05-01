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
		private LLM _selectedModel;

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

		public LLM SelectedModel
		{
			get => _selectedModel;
			set
			{
				if (_selectedModel != value)
				{
					_selectedModel = value;
					OnPropertyChanged(nameof(SelectedModel));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
