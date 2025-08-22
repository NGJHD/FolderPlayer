using System;
using System.Windows.Data;

namespace AudioPlayer
{
    public class BoolToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (System.Convert.ToBoolean(value) == true)
            {
                return ColorDefinitionClass.SelectedColor;
            }

            return ColorDefinitionClass.UnSelectedColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
