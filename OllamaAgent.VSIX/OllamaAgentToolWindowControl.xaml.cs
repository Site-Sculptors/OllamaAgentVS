using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace OllamaAgent.VSIX
{
	public partial class OllamaAgentToolWindowControl : UserControl
	{
		private readonly ChatViewModel _viewModel = new ChatViewModel();

		// VS theme color GUIDs
		private static readonly Guid ToolWindowBackground = new Guid("1ded0138-47ce-435e-84ef-9ec1f439b749");
		private static readonly Guid ToolWindowText = new Guid("5c4976d7-3727-4b11-8c6c-2a1b8e1f1c5e");

		public OllamaAgentToolWindowControl()
		{
			InitializeComponent();
			DataContext = _viewModel;
			SetVsThemeColors();

			// Load models for ComboBox
			Loaded += async (s, e) =>
			{
				await _viewModel.LoadModelsAsync();
			};
		}

		private void SetVsThemeColors()
		{
			var vsUIShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell5;
			if (vsUIShell != null)
			{
				Guid bgGuid = ToolWindowBackground;
				Guid fgGuid = ToolWindowText;
				uint bgColor = vsUIShell.GetThemedColor(ref bgGuid, null, 0xFF2D2D30); // default: dark gray
				uint fgColor = vsUIShell.GetThemedColor(ref fgGuid, null, 0xFFF1F1F1); // default: light gray
				Color bg = ColorFromUInt(bgColor);
				Color fg = ColorFromUInt(fgColor);

				this.Background = new SolidColorBrush(bg);
				ChatHistory.Background = new SolidColorBrush(bg);
				ChatHistory.Foreground = new SolidColorBrush(fg);
				ReloadButton.Background = new SolidColorBrush(bg);
				ReloadButton.Foreground = new SolidColorBrush(fg);
				SettingsButton.Background = new SolidColorBrush(bg);
				SettingsButton.Foreground = new SolidColorBrush(fg);
				NewThreadButton.Background = new SolidColorBrush(bg);
				NewThreadButton.Foreground = new SolidColorBrush(fg);
				InputBox.Background = new SolidColorBrush(bg);
				InputBox.Foreground = new SolidColorBrush(fg);
				ModelSelector.Background = new SolidColorBrush(bg);
				ModelSelector.Foreground = new SolidColorBrush(fg);

				// Set ListBox item foregrounds (for chat messages)
				var itemTemplate = ChatHistory.ItemTemplate;
				if (itemTemplate != null)
				{
					ChatHistory.ItemContainerStyle = new System.Windows.Style(typeof(ListBoxItem));
					ChatHistory.ItemContainerStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(fg)));
				}
			}
		}

		private static Color ColorFromUInt(uint color)
		{
			byte a = (byte)((color >> 24) & 0xFF);
			byte r = (byte)((color >> 16) & 0xFF);
			byte g = (byte)((color >> 8) & 0xFF);
			byte b = (byte)(color & 0xFF);
			return Color.FromArgb(a, r, g, b);
		}

		private void AskButton_Click(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(_viewModel.Input))
			{
				_viewModel.ChatHistory.Add(new ChatMessage { Sender = "You", Message = _viewModel.Input });
				_viewModel.Input = string.Empty;
			}
		}
	}
}
