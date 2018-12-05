using System;
using System.Globalization;
using System.Windows.Data;
using Common.Lib.UI.WPF.Core.Controls.Data;
using HelperLibrary.Trading.PortfolioManager;
using HelperLibrary.Trading.PortfolioManager.Exposure;

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
