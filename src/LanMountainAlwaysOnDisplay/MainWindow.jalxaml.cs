using FluentJalium.Controls;
using FluentJalium.Icon;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Input;
using Jalium.UI.Media;
using MediaElement = Jalium.UI.Controls.MediaElement;

namespace LanMountainAlwaysOnDisplay;

public partial class MainWindow : Window
{
    private const double UnlockSwipeThreshold = 120;
    private const double UnlockSwipeIntentThreshold = 18;
    private const double UnlockSwipeHorizontalToleranceRatio = 0.75;

    private readonly WallpaperSettingsStore _settingsStore = new();
    private WallpaperSettings _currentSettings = WallpaperSettings.Default;
    private MediaElement? _activeMediaElement;
    private WebView? _activeWebView;
    private SettingsWindow? _settingsWindow;
    private bool _isUnlockMouseTracking;
    private bool _isUnlockMouseCaptured;
    private Point _unlockMouseStart;
    private int _activeUnlockTouchId = -1;
    private bool _isUnlockTouchCaptured;
    private Point _unlockTouchStart;

    public MainWindow()
    {
        InitializeComponent();
        ConfigureContentLayer();
        ConfigureUnlockGesture();
        _currentSettings = _settingsStore.Load();
        ApplyWallpaper(_currentSettings);
        Closed += (_, _) =>
        {
            CloseSettingsWindow();
            ReleaseActiveWallpaper();
        };
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

    private void ConfigureUnlockGesture()
    {
        ContentLayer.AddHandler(PreviewMouseDownEvent, new MouseButtonEventHandler(OnUnlockMouseDown), true);
        ContentLayer.AddHandler(PreviewMouseMoveEvent, new MouseEventHandler(OnUnlockMouseMove), true);
        ContentLayer.AddHandler(PreviewMouseUpEvent, new MouseButtonEventHandler(OnUnlockMouseUp), true);
        ContentLayer.AddHandler(LostMouseCaptureEvent, new RoutedEventHandler(OnUnlockLostMouseCapture), true);

        ContentLayer.AddHandler(PreviewTouchDownEvent, new RoutedEventHandler(OnUnlockTouchDown), true);
        ContentLayer.AddHandler(PreviewTouchMoveEvent, new RoutedEventHandler(OnUnlockTouchMove), true);
        ContentLayer.AddHandler(PreviewTouchUpEvent, new RoutedEventHandler(OnUnlockTouchUp), true);
        ContentLayer.AddHandler(LostTouchCaptureEvent, new RoutedEventHandler(OnUnlockLostTouchCapture), true);
    }

    private void OnUnlockMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || IsInputFromSettingsButton(e.OriginalSource))
        {
            return;
        }

        _isUnlockMouseTracking = true;
        _isUnlockMouseCaptured = false;
        _unlockMouseStart = e.GetPosition(this);
    }

    private void OnUnlockMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isUnlockMouseTracking || e.LeftButton != MouseButtonState.Pressed)
        {
            ResetUnlockMouseGesture();
            return;
        }

        var current = e.GetPosition(this);
        if (TryUnlockFromSwipe(_unlockMouseStart, current))
        {
            e.Handled = true;
            ExitAlwaysOnDisplay();
            return;
        }

        if (!_isUnlockMouseCaptured && HasUpwardIntent(_unlockMouseStart, current))
        {
            ContentLayer.CaptureMouse();
            _isUnlockMouseCaptured = true;
        }

        if (_isUnlockMouseCaptured)
        {
            e.Handled = true;
        }
    }

    private void OnUnlockMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (_isUnlockMouseTracking && TryUnlockFromSwipe(_unlockMouseStart, e.GetPosition(this)))
        {
            e.Handled = true;
            ExitAlwaysOnDisplay();
            return;
        }

        if (_isUnlockMouseCaptured)
        {
            e.Handled = true;
        }

        ResetUnlockMouseGesture();
    }

    private void OnUnlockLostMouseCapture(object sender, RoutedEventArgs e) => ResetUnlockMouseGesture(false);

    private void OnUnlockTouchDown(object sender, RoutedEventArgs e)
    {
        if (e is not TouchEventArgs touchArgs ||
            _activeUnlockTouchId != -1 ||
            IsInputFromSettingsButton(e.OriginalSource))
        {
            return;
        }

        _activeUnlockTouchId = touchArgs.TouchDevice.Id;
        _isUnlockTouchCaptured = false;
        _unlockTouchStart = touchArgs.GetTouchPoint(this).Position;
    }

    private void OnUnlockTouchMove(object sender, RoutedEventArgs e)
    {
        if (e is not TouchEventArgs touchArgs || touchArgs.TouchDevice.Id != _activeUnlockTouchId)
        {
            return;
        }

        var current = touchArgs.GetTouchPoint(this).Position;
        if (TryUnlockFromSwipe(_unlockTouchStart, current))
        {
            touchArgs.Cancel = true;
            e.Handled = true;
            ExitAlwaysOnDisplay();
            return;
        }

        if (!_isUnlockTouchCaptured && HasUpwardIntent(_unlockTouchStart, current))
        {
            ContentLayer.CaptureTouch(touchArgs.TouchDevice);
            _isUnlockTouchCaptured = true;
        }

        if (_isUnlockTouchCaptured)
        {
            touchArgs.Cancel = true;
            e.Handled = true;
        }
    }

    private void OnUnlockTouchUp(object sender, RoutedEventArgs e)
    {
        if (e is not TouchEventArgs touchArgs || touchArgs.TouchDevice.Id != _activeUnlockTouchId)
        {
            return;
        }

        if (TryUnlockFromSwipe(_unlockTouchStart, touchArgs.GetTouchPoint(this).Position))
        {
            touchArgs.Cancel = true;
            e.Handled = true;
            ExitAlwaysOnDisplay();
            return;
        }

        if (_isUnlockTouchCaptured)
        {
            touchArgs.Cancel = true;
            e.Handled = true;
        }

        ResetUnlockTouchGesture(touchArgs.TouchDevice);
    }

    private void OnUnlockLostTouchCapture(object sender, RoutedEventArgs e)
    {
        if (e is TouchEventArgs touchArgs && touchArgs.TouchDevice.Id == _activeUnlockTouchId)
        {
            ResetUnlockTouchGesture();
        }
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
            WallpaperKind.Video => CreateVideoWallpaper(settings.Source, settings.IsMuted),
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

    private MediaElement CreateVideoWallpaper(string source, bool isMuted)
    {
        var mediaElement = new MediaElement
        {
            Source = CreateSourceUri(source),
            Stretch = Stretch.UniformToFill,
            LoadedBehavior = MediaState.Play,
            UnloadedBehavior = MediaState.Close,
            IsMuted = isMuted,
            Volume = isMuted ? 0 : 1,
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

    private static bool TryUnlockFromSwipe(Point start, Point current)
    {
        var deltaX = current.X - start.X;
        var deltaY = current.Y - start.Y;
        var upwardDistance = -deltaY;

        return upwardDistance >= UnlockSwipeThreshold &&
               Math.Abs(deltaX) <= upwardDistance * UnlockSwipeHorizontalToleranceRatio;
    }

    private static bool HasUpwardIntent(Point start, Point current)
    {
        var deltaX = current.X - start.X;
        var deltaY = current.Y - start.Y;
        var upwardDistance = -deltaY;

        return upwardDistance >= UnlockSwipeIntentThreshold &&
               upwardDistance > Math.Abs(deltaX);
    }

    private bool IsInputFromSettingsButton(object? source)
    {
        for (var current = source as Visual; current != null; current = current.VisualParent)
        {
            if (ReferenceEquals(current, SettingsButton))
            {
                return true;
            }
        }

        return false;
    }

    private void ExitAlwaysOnDisplay()
    {
        ResetUnlockMouseGesture();
        ResetUnlockTouchGesture();
        CloseSettingsWindow();
        Close();
    }

    private void CloseSettingsWindow()
    {
        var window = _settingsWindow;
        if (window == null)
        {
            return;
        }

        _settingsWindow = null;
        window.Close();
    }

    private void ResetUnlockMouseGesture(bool releaseCapture = true)
    {
        _isUnlockMouseTracking = false;

        if (releaseCapture && _isUnlockMouseCaptured)
        {
            ContentLayer.ReleaseMouseCapture();
        }

        _isUnlockMouseCaptured = false;
    }

    private void ResetUnlockTouchGesture(TouchDevice? touchDevice = null)
    {
        if (_isUnlockTouchCaptured)
        {
            if (touchDevice != null)
            {
                ContentLayer.ReleaseTouchCapture(touchDevice);
            }
            else
            {
                ContentLayer.ReleaseAllTouchCaptures();
            }
        }

        _activeUnlockTouchId = -1;
        _isUnlockTouchCaptured = false;
    }

    private void ReleaseActiveWallpaper()
    {
        _activeMediaElement?.Close();
        _activeMediaElement = null;
        _activeWebView?.Dispose();
        _activeWebView = null;
    }
}
