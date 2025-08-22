using System;
using System.Windows.Data;

namespace AudioPlayer
{
    public class SLBIVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string filterText = System.IO.Path.GetFileNameWithoutExtension(values[0].ToString());

            if (String.IsNullOrWhiteSpace(filterText) == true)
            {
                return System.Windows.Visibility.Visible;
            }

            string songName = values[1].ToString();

            if (songName.ToLower().Contains(filterText.ToLower()) == true)
            {
                return System.Windows.Visibility.Visible;
            }

            return System.Windows.Visibility.Collapsed;            
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
