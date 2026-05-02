using OllamaAgent.VSIX.Models;
using System.Collections.ObjectModel;
using System.Linq;

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
			var textBox = sender as TextBox;
			if (IsSlashCommandPopupOpen)
			{
				var listBox = this.FindName("SlashCommandListBox") as ListBox;
				if (e.Key == Key.Down)
				{
					if (SlashCommandSelectedIndex < _slashCommandSuggestions.Count - 1)
						SlashCommandSelectedIndex++;
					e.Handled = true;
					return;
				}
				if (e.Key == Key.Up)
				{
					if (SlashCommandSelectedIndex > 0)
						SlashCommandSelectedIndex--;
					e.Handled = true;
					return;
				}
				if (e.Key == Key.Enter || e.Key == Key.Tab)
				{
					if (_slashCommandSuggestions.Count > 0 && SlashCommandSelectedIndex >= 0 && SlashCommandSelectedIndex < _slashCommandSuggestions.Count)
					{
						var cmd = _slashCommandSuggestions[SlashCommandSelectedIndex];
						// Insert the command at the slash position
						var text = textBox.Text;
						int slashIdx = text.LastIndexOf('/');
						if (slashIdx >= 0)
						{
							textBox.Text = text.Substring(0, slashIdx) + cmd.Command + " ";
							textBox.CaretIndex = textBox.Text.Length;
						}
						IsSlashCommandPopupOpen = false;
						e.Handled = true;
						return;
					}
				}
				if (e.Key == Key.Escape)
				{
					IsSlashCommandPopupOpen = false;
					e.Handled = true;
					return;
				}
			}

			if (e.Key == System.Windows.Input.Key.Enter)
			{
				// If Shift is held, insert a new line
				if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift)
				{
					if (textBox != null)
					{
						int caret = textBox.CaretIndex;
						textBox.Text = textBox.Text.Insert(caret, System.Environment.NewLine);
						textBox.CaretIndex = caret + System.Environment.NewLine.Length;
					}
					e.Handled = true;
				}
				else
				{
					var vm = DataContext as OllamaAgent.VSIX.ViewModels.ChatViewModel;
					if (vm != null && vm.SendCommand.CanExecute(null))
					{
						vm.SendCommand.Execute(null);
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

		// Attach file dialog logic (invoked by AttachFileCommand in ViewModel)
		public void ShowAttachFileDialog(string initialDirectory, Action<string> onFileSelected)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
			var dlg = new Microsoft.Win32.OpenFileDialog();
			dlg.Title = "Attach file to chat";
			dlg.Filter = "All files (*.*)|*.*";
			dlg.CheckFileExists = true;
			dlg.Multiselect = false;
			if (!string.IsNullOrWhiteSpace(initialDirectory) && System.IO.Directory.Exists(initialDirectory))
				dlg.InitialDirectory = initialDirectory;
			var result = dlg.ShowDialog();
			if (result == true)
			{
				onFileSelected?.Invoke(dlg.FileName);
			}
		}

		// Slash command autocomplete state
		private ObservableCollection<SlashCommand> _slashCommandSuggestions = new ObservableCollection<SlashCommand>();
		private bool _isSlashCommandPopupOpen = false;
		private int _slashCommandSelectedIndex = 0;

		public ObservableCollection<SlashCommand> SlashCommandSuggestions => _slashCommandSuggestions;
		public bool IsSlashCommandPopupOpen
		{
			get => _isSlashCommandPopupOpen;
			set
			{
				_isSlashCommandPopupOpen = value;
				var popup = this.FindName("SlashCommandPopup") as System.Windows.Controls.Primitives.Popup;
				if (popup != null)
					popup.IsOpen = value;
			}
		}
		public int SlashCommandSelectedIndex
		{
			get => _slashCommandSelectedIndex;
			set
			{
				_slashCommandSelectedIndex = value;
				var listBox = this.FindName("SlashCommandListBox") as ListBox;
				if (listBox != null)
					listBox.SelectedIndex = value;
			}
		}
		private void InputBox_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			var textBox = sender as TextBox;
			if (textBox == null) return;
			var caret = textBox.CaretIndex;
			var text = textBox.Text;
			// Detect if slash command should trigger
			int slashIdx = text.LastIndexOf('/') >= 0 ? text.LastIndexOf('/') : -1;
			if (slashIdx == 0 || (slashIdx > 0 && (slashIdx == 0 || char.IsWhiteSpace(text[slashIdx - 1]))))
			{
				var afterSlash = text.Substring(slashIdx + 1);
				var matches = SlashCommand.All.Where(cmd => cmd.Command.StartsWith("/" + afterSlash, StringComparison.OrdinalIgnoreCase)).ToList();
				_slashCommandSuggestions.Clear();
				foreach (var cmd in matches) _slashCommandSuggestions.Add(cmd);
				IsSlashCommandPopupOpen = matches.Count > 0;
				SlashCommandSelectedIndex = 0;
			}
			else
			{
				IsSlashCommandPopupOpen = false;
			}
		}
	}
}
