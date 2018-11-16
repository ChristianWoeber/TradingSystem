using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Common.Lib.UI.WPF.Core.Controls.Data;
using HelperLibrary.Trading.PortfolioManager;

namespace Trading.UI.Wpf.Converter
{
    public class SmartMappingItemToValueConverter : IValueConverter
    {
        public static SmartMappingItemToValueConverter Inst { get; } = new SmartMappingItemToValueConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is SmartMappingItem mappingItem))
                return value;

            return (IndexType)mappingItem.Value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is SmartMappingItem mappingItem))
                return value;

            return mappingItem.Value;
        }
    }
}
