using Jalium.UI.Controls;
using Jalium.UI.Media;

namespace LanMountainAlwaysOnDisplay;

public partial class MainWindow : Window
{
    private const string PreviewImagePath = "Assets/Display/preview.png";

    public MainWindow()
    {
        InitializeComponent();
        LoadPreviewImage();
    }

    private void LoadPreviewImage()
    {
        try
        {
            var source = new BitmapImage(new Uri(PreviewImagePath, UriKind.Relative));
            source.OnImageLoaded += (_, _) => FallbackOverlay.Visibility = Jalium.UI.Visibility.Collapsed;
            PreviewImage.Source = source;
            FallbackOverlay.Visibility = Jalium.UI.Visibility.Collapsed;
        }
        catch
        {
            FallbackOverlay.Visibility = Jalium.UI.Visibility.Visible;
        }
    }
}
