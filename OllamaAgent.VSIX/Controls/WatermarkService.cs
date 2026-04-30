using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;
using System.Linq;

namespace OllamaAgent.VSIX.Controls
{
	public static class WatermarkService
	{
		public static readonly DependencyProperty WatermarkProperty = DependencyProperty.RegisterAttached(
			"Watermark",
			typeof(string),
			typeof(WatermarkService),
			new FrameworkPropertyMetadata(string.Empty, OnWatermarkChanged));

		public static readonly DependencyProperty WatermarkFontSizeProperty = DependencyProperty.RegisterAttached(
			"WatermarkFontSize",
			typeof(double?),
			typeof(WatermarkService),
			new FrameworkPropertyMetadata(null, OnWatermarkFontSizeChanged));

		public static string GetWatermark(DependencyObject obj)
		{
			return (string)obj.GetValue(WatermarkProperty);
		}

		public static void SetWatermark(DependencyObject obj, string value)
		{
			obj.SetValue(WatermarkProperty, value);
		}

		public static double? GetWatermarkFontSize(DependencyObject obj)
		{
			return (double?)obj.GetValue(WatermarkFontSizeProperty);
		}

		public static void SetWatermarkFontSize(DependencyObject obj, double? value)
		{
			obj.SetValue(WatermarkFontSizeProperty, value);
		}

		private static void OnWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var textBox = d as TextBox;
			if (textBox == null)
				return;

			if (!string.IsNullOrEmpty((string)e.NewValue))
			{
				textBox.Loaded += TextBox_Loaded;
				textBox.TextChanged += TextBox_TextChanged;
			}
			else
			{
				textBox.Loaded -= TextBox_Loaded;
				textBox.TextChanged -= TextBox_TextChanged;
			}
		}

		private static void OnWatermarkFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var textBox = d as TextBox;
			if (textBox == null)
				return;
			ShowOrHideWatermark(textBox);
		}

		private static void TextBox_Loaded(object sender, RoutedEventArgs e)
		{
			var textBox = sender as TextBox;
			ShowOrHideWatermark(textBox);
		}

		private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var textBox = sender as TextBox;
			ShowOrHideWatermark(textBox);
		}

		private static void ShowOrHideWatermark(TextBox textBox)
		{
			if (textBox == null)
				return;

			var layer = AdornerLayer.GetAdornerLayer(textBox);
			if (layer == null)
				return;

			var adorners = layer.GetAdorners(textBox);
			if (string.IsNullOrEmpty(textBox.Text) && !string.IsNullOrEmpty(GetWatermark(textBox)))
			{
				if (adorners == null || !adorners.OfType<WatermarkAdorner>().Any())
				{
					layer.Add(new WatermarkAdorner(textBox, GetWatermark(textBox), GetWatermarkFontSize(textBox)));
				}
			}
			else
			{
				if (adorners != null)
				{
					foreach (var adorner in adorners.OfType<WatermarkAdorner>())
					{
						layer.Remove(adorner);
					}
				}
			}
		}
	}
}
