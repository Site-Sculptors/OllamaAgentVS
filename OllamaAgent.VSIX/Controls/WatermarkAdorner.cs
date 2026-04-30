using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace OllamaAgent.VSIX.Controls
{
	public class WatermarkAdorner : Adorner
	{
		private readonly TextBlock _watermarkTextBlock;
		private readonly double? _fontSize;

		public WatermarkAdorner(UIElement adornedElement, string watermark, double? fontSize = null)
			: base(adornedElement)
		{
			IsHitTestVisible = false;
			_fontSize = fontSize;
			_watermarkTextBlock = new TextBlock
			{
				Text = watermark,
				Foreground = Brushes.Gray,
				Margin = new Thickness(2, 0, 0, 0),
				VerticalAlignment = VerticalAlignment.Center
			};
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);
			var textBox = AdornedElement as TextBox;
			if (textBox == null)
				return;

			_watermarkTextBlock.FontSize = _fontSize ?? textBox.FontSize;
			_watermarkTextBlock.FontFamily = textBox.FontFamily;

			// Measure the size needed for the watermark text
			_watermarkTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			var desiredSize = _watermarkTextBlock.DesiredSize;

			// Draw the watermark text at the same position as the TextBox text
			drawingContext.DrawText(
				new FormattedText(
					_watermarkTextBlock.Text,
					System.Globalization.CultureInfo.CurrentUICulture,
					_watermarkTextBlock.FlowDirection,
					new Typeface(_watermarkTextBlock.FontFamily, _watermarkTextBlock.FontStyle, _watermarkTextBlock.FontWeight, _watermarkTextBlock.FontStretch),
					_watermarkTextBlock.FontSize,
					_watermarkTextBlock.Foreground
				),
				new Point(4, (textBox.ActualHeight - desiredSize.Height) / 2)
			);
		}
	}
}
