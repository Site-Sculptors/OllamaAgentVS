using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OllamaAgent.VSIX.Models
{
	public class ChatThread : INotifyPropertyChanged
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();

		private string _name;
		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					OnPropertyChanged();
				}
			}
		}

		private string _solutionPath;
		public string SolutionPath
		{
			get => _solutionPath;
			set
			{
				if (_solutionPath != value)
				{
					_solutionPath = value;
					OnPropertyChanged();
				}
			}
		}

		private string _modelName;
		public string ModelName
		{
			get => _modelName;
			set
			{
				if (_modelName != value)
				{
					_modelName = value;
					OnPropertyChanged();
				}
			}
		}

		private bool _isAutoNamed = true;
		public bool IsAutoNamed
		{
			get => _isAutoNamed;
			set
			{
				if (_isAutoNamed != value)
				{
					_isAutoNamed = value;
					OnPropertyChanged();
				}
			}
		}

		private DateTime _createdAt = DateTime.UtcNow;
		public DateTime CreatedAt
		{
			get => _createdAt;
			set
			{
				if (_createdAt != value)
				{
					_createdAt = value;
					OnPropertyChanged();
				}
			}
		}

		private DateTime _lastActivityAt = DateTime.UtcNow;
		public DateTime LastActivityAt
		{
			get => _lastActivityAt;
			set
			{
				if (_lastActivityAt != value)
				{
					_lastActivityAt = value;
					OnPropertyChanged();
				}
			}
		}

		private ObservableCollection<ChatMessageBase> _messages = new ObservableCollection<ChatMessageBase>();
		public ObservableCollection<ChatMessageBase> Messages
		{
			get => _messages;
			set
			{
				if (_messages != value)
				{
					_messages = value;
					OnPropertyChanged();
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
