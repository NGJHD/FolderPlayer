using System.Windows.Media;

namespace AudioPlayer
{
    public static class ColorDefinitions
    {
        public static readonly SolidColorBrush Level1GrayColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#101010");
        public static readonly SolidColorBrush Level2GrayColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#212121");
        public static readonly SolidColorBrush Level3GrayColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#303030");
        public static readonly SolidColorBrush Level4GrayColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#424242");
        public static readonly SolidColorBrush Level6GrayColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#616161");

        public static readonly SolidColorBrush SelectedColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#00FF00");
        public static readonly SolidColorBrush UnSelectedColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFFFFF");        
    }

    public static class OpacityDefinitions
    {
        public static readonly double Level1Opacity = 1.00;
        public static readonly double Level2Opacity = 0.70;
        public static readonly double Level3Opacity = 0.57;
        public static readonly double Level4Opacity = 0.30;
        public static readonly double DividerOpacity = 0.12;

        public static readonly double FrostedBlackOpacity = 0.8;
        public static readonly double FrostedWhiteOpacity = 0.6;
    }

    public static class GlobalVariables
    {
        public static MainWindow MainWindow = null;
        public static string NowPlayingSingle = "";
    }
}
