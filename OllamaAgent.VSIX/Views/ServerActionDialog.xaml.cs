using System.Windows;

namespace OllamaAgent.VSIX.Views
{
	public partial class ServerActionDialog : Window
	{
		public enum ServerActionResult { Stop, Restart, Cancel }
		public ServerActionResult Result { get; private set; } = ServerActionResult.Cancel;

		public ServerActionDialog()
		{
			InitializeComponent();
		}

		private void StopButton_Click(object sender, RoutedEventArgs e)
		{
			Result = ServerActionResult.Stop;
			DialogResult = true;
		}

		private void RestartButton_Click(object sender, RoutedEventArgs e)
		{
			Result = ServerActionResult.Restart;
			DialogResult = true;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Result = ServerActionResult.Cancel;
			DialogResult = false;
		}
	}
}
