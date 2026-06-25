using System;
using System.Globalization;
using System.Windows.Data;

namespace CloseDriver.Wpf.Converters;

public class FloatToPercentConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is float f && !float.IsNaN(f))
			return $"{f:F1} %";
		return "---";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
