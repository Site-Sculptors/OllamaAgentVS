using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using OllamaAgent.VSIX.ViewModels;

using System;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OllamaAgent.VSIX.Controls
{
	public partial class OllamaAgentToolWindowControl : UserControl
	{
		// VS theme color GUIDs
		//private static readonly Guid ToolWindowBackground = new Guid("1ded0138-47ce-435e-84ef-9ec1f439b749");
		//private static readonly Guid ToolWindowText = new Guid("5c4976d7-3727-4b11-8c6c-2a1b8e1f1c5e");

		private readonly ChatViewModel _viewModel;
		private bool _autoScrollSubscribed = false;

		public OllamaAgentToolWindowControl(ChatViewModel viewModel)
		{
			InitializeComponent();

			DataContext = viewModel;
			_viewModel = viewModel;

			Loaded += (s, e) =>
			{
				_ = InitializeAsync();
			};

			// Ensure models are always loaded when the chat window is activated or gains focus
			this.IsVisibleChanged += (s, e) =>
			{
				if (this.IsVisible && _viewModel != null)
				{
					_ = _viewModel.SafeLoadAsync();
				}
			};
			this.GotFocus += (s, e) =>
			{
				if (_viewModel != null)
				{
					_ = _viewModel.SafeLoadAsync();
				}
			};

		}

		//private void SetChatWindowColors()
		//{
		//	var themeColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
		//	var wpfColor = Color.FromArgb(themeColor.A, themeColor.R, themeColor.G, themeColor.B);
		//	var brush = (SolidColorBrush)Application.Current.Resources["ChatWindowBackgroundBrush"];
		//	brush.Color = wpfColor;
		//}

		private async Task InitializeAsync()
		{
			try
			{
				if (_viewModel != null)
				{
					await _viewModel.SafeLoadAsync();
				}

				await this.Dispatcher.BeginInvoke(new Action(() =>
				{
					void TryEnableAutoScroll()
					{
						// Defensive: check _viewModel and Models
						var vm = _viewModel;
						if (vm == null || vm.Models == null)
							return;
						if (vm.Status == OllamaAgent.VSIX.Enums.ServerStatus.Online && vm.Models.Count > 0)
						{
							var chatList = CurrentChat ?? (FindName("CurrentChat") as ListBox);
							if (chatList == null)
							{
								System.Diagnostics.Debug.WriteLine("[OllamaAgent] CurrentChat is still null after loading!");
								return;
							}
							// Only subscribe once
							if (!_autoScrollSubscribed)
							{
								_autoScrollSubscribed = true;
								if (vm.ChatHistory != null)
								{
									vm.ChatHistory.CollectionChanged += (s2, e2) =>
									{
										try
										{
											if (chatList?.Items != null && chatList.Items.Count > 0)
											{
												var lastItem = chatList.Items[chatList.Items.Count - 1];
												if (lastItem != null)
													chatList.ScrollIntoView(lastItem);
											}
										}
										catch (Exception ex)
										{
											System.Diagnostics.Debug.WriteLine($"[OllamaAgent] Exception in auto-scroll: {ex.Message}");
										}
									};
								}
							}
						}
					}

					// Track if we've already subscribed
					_autoScrollSubscribed = false;

					// Listen for server status and models changes
					var vm2 = _viewModel;
					if (vm2 != null)
					{
						vm2.PropertyChanged += (sender, args) =>
						{
							if (args.PropertyName == nameof(vm2.Status) || args.PropertyName == nameof(vm2.Models))
							{
								TryEnableAutoScroll();
							}
						};
						if (vm2.Models != null)
						{
							vm2.Models.CollectionChanged += (sender, args) => TryEnableAutoScroll();
						}
					}
					// Initial check
					TryEnableAutoScroll();
				}), System.Windows.Threading.DispatcherPriority.Loaded);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[OllamaAgent] Exception in InitializeAsync: {ex}");
			}
			return;
		}



		//private async Task SetVsThemeColorsAsync()
		//{
		//	await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		//	var vsUIShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell5;

		//	if (vsUIShell != null)
		//	{
		//		Guid bgGuid = ToolWindowBackground;
		//		Guid fgGuid = ToolWindowText;
		//		uint bgColor = vsUIShell.GetThemedColor(ref bgGuid, null, 0xFF2D2D30); // default: dark gray
		//		uint fgColor = vsUIShell.GetThemedColor(ref fgGuid, null, 0xFFF1F1F1); // default: light gray
		//		System.Diagnostics.Debug.WriteLine($"bgColor: 0x{bgColor:X8}, fgColor: 0x{fgColor:X8}");
		//		Color bg = ColorFromUInt(bgColor);
		//		Color fg = ColorFromUInt(fgColor);

		//		this.Background = new SolidColorBrush(bg);
		//		if (CurrentChat != null)
		//		{
		//			CurrentChat.Background = new SolidColorBrush(bg);
		//			CurrentChat.Foreground = new SolidColorBrush(fg);
		//			// Set ListBox item foregrounds (for chat messages)
		//			var itemTemplate = CurrentChat.ItemTemplate;
		//			if (itemTemplate != null)
		//			{
		//				CurrentChat.ItemContainerStyle = new System.Windows.Style(typeof(ListBoxItem));
		//				CurrentChat.ItemContainerStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(fg)));
		//			}
		//		}
		//		if (ReloadButton != null)
		//		{
		//			ReloadButton.Background = new SolidColorBrush(bg);
		//			ReloadButton.Foreground = new SolidColorBrush(fg);
		//		}
		//		if (SettingsButton != null)
		//		{
		//			SettingsButton.Background = new SolidColorBrush(bg);
		//			SettingsButton.Foreground = new SolidColorBrush(fg);
		//		}
		//		if (NewThreadButton != null)
		//		{
		//			NewThreadButton.Background = new SolidColorBrush(bg);
		//			NewThreadButton.Foreground = new SolidColorBrush(fg);
		//		}
		//		if (InputBox != null)
		//		{
		//			InputBox.Background = new SolidColorBrush(bg);
		//			InputBox.Foreground = new SolidColorBrush(fg);
		//		}
		//		if (ModelSelector != null)
		//		{
		//			ModelSelector.Background = new SolidColorBrush(bg);
		//			ModelSelector.Foreground = new SolidColorBrush(fg);
		//		}
		//	}
		//	return;
		//}

		//private static Color ColorFromUInt(uint color)
		//{
		//	byte a = (byte)((color >> 24) & 0xFF);
		//	if (a == 0) a = 0xFF; // Default to opaque if alpha is zero
		//	byte r = (byte)((color >> 16) & 0xFF);
		//	byte g = (byte)((color >> 8) & 0xFF);
		//	byte b = (byte)(color & 0xFF);
		//	return Color.FromArgb(a, r, g, b);
		//}

		private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				// If Shift is held, insert a new line
				if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift)
				{
					var textBox = sender as TextBox;
					if (textBox != null)
					{
						// Insert a new line at the caret position
						int caret = textBox.CaretIndex;
						textBox.Text = textBox.Text.Insert(caret, System.Environment.NewLine);
						textBox.CaretIndex = caret + System.Environment.NewLine.Length;
					}
					e.Handled = true;
				}
				else
				{
					// Enter without Shift: send
					var vm = DataContext as OllamaAgent.VSIX.ViewModels.ChatViewModel;
					if (vm != null && vm.SendCommand.CanExecute(null))
					{
						vm.SendCommand.Execute(null);
						// Move focus back to input box after sending
						var textBox = sender as TextBox;
						if (textBox != null)
						{
							textBox.Focus();
						}
					}
					e.Handled = true;
				}
			}
		}

		// All button logic is now handled via MVVM ICommand bindings in the ViewModel.
	}
}
