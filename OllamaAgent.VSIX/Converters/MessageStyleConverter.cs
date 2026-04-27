// MessageStyleConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace OllamaAgent.VSIX.Converters
{
	public class MessageStyleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool isUser = (bool)value;
			return isUser ? "UserMessageStyle" : "AssistantMessageStyle";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}