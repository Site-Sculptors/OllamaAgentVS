using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using OllamaAgent.VSIX.Enums;

namespace OllamaAgent.VSIX.Converters
{
    public class ServerStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ServerStatus status)
            {
                switch (status)
                {
                    case ServerStatus.Online:
                        return Brushes.Green;
                    case ServerStatus.Offline:
                        return Brushes.Red;
                    case ServerStatus.Starting:
                        return Brushes.DarkOrange;
                    case ServerStatus.Unknown:
                    default:
                        return Brushes.Gray;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
