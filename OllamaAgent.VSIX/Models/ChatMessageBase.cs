
using OllamaAgent.VSIX.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;


namespace OllamaAgent.VSIX.Models
{
   public abstract class ChatMessageBase : INotifyPropertyChanged
   {
	  public ChatRole Role { get; set; }

	  private string _content;
	  public string Content
	  {
		 get => _content;
		 set
		 {
			if (_content != value)
			{
			   _content = value;
			   OnPropertyChanged();
			   OnPropertyChanged(nameof(Display));
			}
		 }
	  }

	  public virtual string Display => $"{Role}: {Content}";
	  public string[] Images { get; set; }

	  public event PropertyChangedEventHandler PropertyChanged;
	  protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	  {
		 PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	  }
   }

   // UserChatMessage and AIChatMessage moved to their own files for single-class-per-file convention.
}
