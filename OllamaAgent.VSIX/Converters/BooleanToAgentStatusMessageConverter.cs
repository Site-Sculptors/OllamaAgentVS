using System;
using System.Globalization;
using System.Windows.Data;

namespace OllamaAgent.VSIX.Converters
{
	public class BooleanToAgentStatusMessageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool enabled)
			{
				return enabled ? string.Empty : "Agent is disabled. Enable to use agent features.";
			}
			return string.Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
