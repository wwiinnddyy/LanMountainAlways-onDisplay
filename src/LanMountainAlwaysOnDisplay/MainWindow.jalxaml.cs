using FluentJalium.Controls;
using FluentJalium.Icon;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Media;
using MediaElement = Jalium.UI.Controls.MediaElement;

namespace LanMountainAlwaysOnDisplay;

public partial class MainWindow : Window
{
    private readonly WallpaperSettingsStore _settingsStore = new();
    private WallpaperSettings _currentSettings = WallpaperSettings.Default;
    private MediaElement? _activeMediaElement;
    private WebView? _activeWebView;
    private SettingsWindow? _settingsWindow;

    public MainWindow()
    {
        InitializeComponent();
        ConfigureContentLayer();
        _currentSettings = _settingsStore.Load();
        ApplyWallpaper(_currentSettings);
        Closed += (_, _) => ReleaseActiveWallpaper();
    }

    private void ConfigureContentLayer()
    {
        if (SettingsButton is not FWButton settingsButton)
        {
            return;
        }

        settingsButton.Content = new FWStackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children =
            {
                FluentIconFactory.Regular(FluentIconRegular.Settings24, 16),
                new FWTextBlock
                {
                    Text = "设置",
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
        settingsButton.Click += (_, _) => OpenSettingsWindow();
    }

    private void ApplyWallpaper(WallpaperSettings settings)
    {
        try
        {
            var normalized = settings.Normalize();
            var wallpaper = CreateWallpaperElement(normalized);
            _currentSettings = normalized;

            ReplaceBackground(wallpaper);
            HideFallback();
        }
        catch
        {
            TryApplyDefaultWallpaper();
        }
    }

    private void OpenSettingsWindow()
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        var window = new SettingsWindow(_currentSettings, ApplySettingsFromWindow);
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_settingsWindow, window))
            {
                _settingsWindow = null;
            }
        };

        _settingsWindow = window;
        window.Show();
    }

    private void ApplySettingsFromWindow(WallpaperSettings settings)
    {
        var normalized = settings.Normalize();
        ApplyWallpaper(normalized);
        _settingsStore.Save(normalized);
    }

    private UIElement CreateWallpaperElement(WallpaperSettings settings)
    {
        return settings.Kind switch
        {
            WallpaperKind.Image => CreateImageWallpaper(settings.Source),
            WallpaperKind.Video => CreateVideoWallpaper(settings.Source),
            WallpaperKind.Html => CreateHtmlWallpaper(settings.Source),
            _ => CreateImageWallpaper(WallpaperSettings.BundledPreviewSource)
        };
    }

    private Image CreateImageWallpaper(string source)
    {
        var imageSource = new BitmapImage(CreateSourceUri(source));
        imageSource.OnImageLoaded += (_, _) => HideFallback();

        return new Image
        {
            Source = imageSource,
            Stretch = Stretch.UniformToFill,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
    }

    private MediaElement CreateVideoWallpaper(string source)
    {
        var mediaElement = new MediaElement
        {
            Source = CreateSourceUri(source),
            Stretch = Stretch.UniformToFill,
            LoadedBehavior = MediaState.Play,
            UnloadedBehavior = MediaState.Close,
            IsMuted = true,
            Volume = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        mediaElement.MediaEnded += (_, _) =>
        {
            mediaElement.Position = TimeSpan.Zero;
            mediaElement.Play();
        };
        mediaElement.MediaFailed += (_, _) => ShowFallback();
        return mediaElement;
    }

    private WebView CreateHtmlWallpaper(string source)
    {
        var webView = new WebView
        {
            Source = CreateSourceUri(source),
            DefaultBackgroundColor = Color.FromRgb(0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        webView.CoreWebView2InitializationCompleted += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(webView.InitializationError))
            {
                ShowFallback();
            }
        };
        return webView;
    }

    private void ReplaceBackground(UIElement wallpaper)
    {
        ReleaseActiveWallpaper();

        BackgroundLayer.Children.Clear();
        BackgroundLayer.Children.Add(wallpaper);
        BackgroundLayer.Children.Add(FallbackOverlay);

        _activeMediaElement = wallpaper as MediaElement;
        _activeWebView = wallpaper as WebView;
    }

    private static Uri CreateSourceUri(string source)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        if (Path.IsPathRooted(source))
        {
            return new Uri(source, UriKind.Absolute);
        }

        return new Uri(source, UriKind.Relative);
    }

    private void TryApplyDefaultWallpaper()
    {
        try
        {
            var defaultWallpaper = CreateImageWallpaper(WallpaperSettings.BundledPreviewSource);
            _currentSettings = WallpaperSettings.Default;
            ReplaceBackground(defaultWallpaper);
        }
        catch
        {
            BackgroundLayer.Children.Clear();
            BackgroundLayer.Children.Add(FallbackOverlay);
        }

        ShowFallback();
    }

    private void HideFallback() => FallbackOverlay.Visibility = Jalium.UI.Visibility.Collapsed;

    private void ShowFallback() => FallbackOverlay.Visibility = Jalium.UI.Visibility.Visible;

    private void ReleaseActiveWallpaper()
    {
        _activeMediaElement?.Close();
        _activeMediaElement = null;
        _activeWebView?.Dispose();
        _activeWebView = null;
    }
}
