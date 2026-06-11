using FluentJalium.Controls;
using FluentJalium.Icon;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Media;

namespace LanMountainAlwaysOnDisplay;

public partial class SettingsWindow : Window
{
    private const string WallpaperPageKey = "wallpaper";
    private const string AboutPageKey = "about";

    private readonly Action<WallpaperSettings> _applySettings;

    private WallpaperSettings _draftSettings;
    private WallpaperKind _selectedKind;
    private FWNavigationView? _navigationView;
    private FWNavigationViewItem? _wallpaperItem;
    private FWNavigationViewItem? _aboutItem;
    private FWStackPanel? _contentHost;

    // 壁纸类型选择器
    private FWSelectorBar? _wallpaperKindSelector;
    private readonly Dictionary<WallpaperKind, FWSelectorBarItem> _wallpaperKindItems = [];

    // 类型专属设置面板
    private FWStackPanel? _kindSettingsHost;

    // 图片设置
    private FWTextBox? _imageSourceTextBox;
    private FWComboBox? _imagePlacementCombo;

    // 视频设置
    private FWTextBox? _videoSourceTextBox;
    private FWToggleSwitch? _videoMutedSwitch;

    // HTML 设置
    private FWTextBox? _htmlSourceTextBox;

    // 状态
    private FWTextBlock? _statusText;

    public SettingsWindow(WallpaperSettings currentSettings, Action<WallpaperSettings> applySettings)
    {
        _draftSettings = currentSettings.Normalize();
        _selectedKind = _draftSettings.Kind;
        _applySettings = applySettings;

        InitializeComponent();
        Content = CreateContent();
        NavigateTo(WallpaperPageKey);
    }

    private UIElement CreateContent()
    {
        _contentHost = new FWStackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 18
        };

        _navigationView = new FWNavigationView
        {
            PaneTitle = "阑山全天候显示",
            PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
            Density = FWNavigationDensity.Comfortable,
            IsPaneOpen = true,
            OpenPaneLength = 248,
            CompactPaneLength = 48,
            PaneHeader = CreatePaneHeader(),
            Content = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = new Border
                {
                    Padding = new Thickness(34, 28, 38, 32),
                    Child = _contentHost
                }
            }
        };

        _wallpaperItem = CreateNavigationItem(WallpaperPageKey, "壁纸", FluentIconRegular.Image24);
        _aboutItem = CreateNavigationItem(AboutPageKey, "关于", FluentIconRegular.Info24);

        _navigationView.MenuItems.Add(_wallpaperItem);
        _navigationView.FooterMenuItems.Add(_aboutItem);
        _navigationView.SelectionChanged += (_, e) =>
        {
            if (e.SelectedItem?.Tag is string pageKey)
            {
                NavigateTo(pageKey);
            }
        };
        _navigationView.UpdateMenuItems();
        _navigationView.SelectedItem = _wallpaperItem;

        return new FWFluentWindowSurface
        {
            Padding = new Thickness(0),
            Child = _navigationView
        };
    }

    private UIElement CreatePaneHeader()
    {
        return new FWStackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
            Margin = new Thickness(16, 14, 16, 12),
            Children =
            {
                new FWTextBlock
                {
                    Text = "设置",
                    FontSize = 20,
                    FontWeight = FontWeights.SemiBold
                },
                new FWTextBlock
                {
                    Text = "Always-on Display",
                    FontSize = 12,
                    Foreground = SubtleTextBrush()
                }
            }
        };
    }

    private static FWNavigationViewItem CreateNavigationItem(string pageKey, string title, FluentIconRegular icon)
    {
        return new FWNavigationViewItem
        {
            Content = CreateNavigationContent(icon, title),
            Tag = pageKey
        };
    }

    private void NavigateTo(string pageKey)
    {
        if (_contentHost == null)
        {
            return;
        }

        _contentHost.Children.Clear();
        ResetWallpaperControls();

        switch (pageKey)
        {
            case WallpaperPageKey:
                BuildWallpaperPage(_contentHost);
                break;
            case AboutPageKey:
                BuildAboutPage(_contentHost);
                break;
            default:
                BuildWallpaperPage(_contentHost);
                break;
        }
    }

    private void ResetWallpaperControls()
    {
        _wallpaperKindItems.Clear();
        _wallpaperKindSelector = null;
        _kindSettingsHost = null;
        _imageSourceTextBox = null;
        _imagePlacementCombo = null;
        _videoSourceTextBox = null;
        _videoMutedSwitch = null;
        _htmlSourceTextBox = null;
        _statusText = null;
    }

    // ─── 壁纸页面 ──────────────────────────────────────────────

    private void BuildWallpaperPage(FWStackPanel host)
    {
        host.Children.Add(CreatePageHeader(
            "壁纸",
            "选择全天候显示的背景。支持图片、视频和 HTML 页面三种类型。"));

        // 壁纸类型选择
        host.Children.Add(CreateWallpaperKindCard());

        // 类型专属设置区域
        _kindSettingsHost = new FWStackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8
        };
        host.Children.Add(_kindSettingsHost);

        // 应用按钮
        host.Children.Add(CreateActionsRow());

        // 初始化类型对应的设置
        UpdateWallpaperKind(_selectedKind, keepExistingSource: true);
    }

    private UIElement CreateWallpaperKindCard()
    {
        _wallpaperKindSelector = new FWSelectorBar
        {
            Orientation = Orientation.Horizontal,
            Density = FWNavigationDensity.Comfortable,
            SelectionIndicatorPlacement = FWSelectorBarSelectionIndicatorPlacement.Bottom
        };

        _wallpaperKindSelector.Items.Add(CreateWallpaperKindItem(WallpaperKind.Image));
        _wallpaperKindSelector.Items.Add(CreateWallpaperKindItem(WallpaperKind.Video));
        _wallpaperKindSelector.Items.Add(CreateWallpaperKindItem(WallpaperKind.Html));
        _wallpaperKindSelector.SelectionChanged += (_, _) =>
        {
            if (_wallpaperKindSelector.SelectedItem is FWSelectorBarItem item && item.Tag is WallpaperKind kind)
            {
                UpdateWallpaperKind(kind, keepExistingSource: false);
            }
        };

        return new FWSettingsCard
        {
            Header = "壁纸类型",
            Description = "选择背景内容的类型。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Layer24, 18, SubtleTextBrush()),
            Content = _wallpaperKindSelector
        };
    }

    private void UpdateWallpaperKind(WallpaperKind kind, bool keepExistingSource)
    {
        _selectedKind = kind;

        // 更新选择器
        if (_wallpaperKindSelector != null && _wallpaperKindItems.TryGetValue(kind, out var selectedItem))
        {
            _wallpaperKindSelector.SelectedItem = selectedItem;
        }

        // 重建类型专属设置
        if (_kindSettingsHost != null)
        {
            _kindSettingsHost.Children.Clear();

            switch (kind)
            {
                case WallpaperKind.Image:
                    BuildImageSettings(_kindSettingsHost, keepExistingSource);
                    break;
                case WallpaperKind.Video:
                    BuildVideoSettings(_kindSettingsHost, keepExistingSource);
                    break;
                case WallpaperKind.Html:
                    BuildHtmlSettings(_kindSettingsHost, keepExistingSource);
                    break;
            }
        }

        SetStatus(GetStatusText(kind));
    }

    // ─── 图片壁纸设置 ──────────────────────────────────────────

    private void BuildImageSettings(FWStackPanel host, bool keepExistingSource)
    {
        var source = keepExistingSource && _draftSettings.Kind == WallpaperKind.Image
            ? _draftSettings.Source
            : WallpaperSettings.BundledPreviewSource;

        // 文件来源
        _imageSourceTextBox = new FWTextBox
        {
            Text = source,
            MinHeight = 34,
            TextWrapping = TextWrapping.NoWrap,
            Width = 320
        };

        var browseButton = new FWButton
        {
            Width = 36,
            Height = 34,
            Content = FluentIconFactory.Regular(FluentIconRegular.FolderOpen24, 16)
        };
        browseButton.Click += (_, _) => BrowseForKind(WallpaperKind.Image);

        var bundledButton = new FWButton
        {
            Width = 36,
            Height = 34,
            Content = FluentIconFactory.Regular(FluentIconRegular.Image24, 16)
        };
        bundledButton.Click += (_, _) =>
        {
            if (_imageSourceTextBox != null)
            {
                _imageSourceTextBox.Text = WallpaperSettings.BundledPreviewSource;
                SetStatus("已切换为内置预览图。");
            }
        };

        var sourceRow = new Grid { ColumnSpacing = 8 };
        sourceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sourceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sourceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(_imageSourceTextBox, 0);
        sourceRow.Children.Add(_imageSourceTextBox);
        Grid.SetColumn(browseButton, 1);
        sourceRow.Children.Add(browseButton);
        Grid.SetColumn(bundledButton, 2);
        sourceRow.Children.Add(bundledButton);

        host.Children.Add(new FWSettingsCard
        {
            Header = "图片来源",
            Description = "选择本地图片文件作为壁纸。支持 PNG、JPG、BMP、WEBP 格式。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Folder24, 18, SubtleTextBrush()),
            Content = new Border { Width = 440, Child = sourceRow }
        });

        // 填充方式
        _imagePlacementCombo = new FWComboBox
        {
            Width = 200,
            MinHeight = 34
        };
        _imagePlacementCombo.Items.Add("填充");
        _imagePlacementCombo.Items.Add("适应");
        _imagePlacementCombo.Items.Add("拉伸");
        _imagePlacementCombo.Items.Add("居中");
        _imagePlacementCombo.Items.Add("平铺");
        _imagePlacementCombo.SelectedIndex = GetPlacementIndex(_draftSettings.Placement);

        host.Children.Add(new FWSettingsCard
        {
            Header = "填充方式",
            Description = "调整图片在桌面上的显示方式。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.FullScreenMaximize24, 18, SubtleTextBrush()),
            Content = _imagePlacementCombo
        });
    }

    // ─── 视频壁纸设置 ──────────────────────────────────────────

    private void BuildVideoSettings(FWStackPanel host, bool keepExistingSource)
    {
        var source = keepExistingSource && _draftSettings.Kind == WallpaperKind.Video
            ? _draftSettings.Source
            : string.Empty;

        // 文件来源
        _videoSourceTextBox = new FWTextBox
        {
            Text = source,
            MinHeight = 34,
            TextWrapping = TextWrapping.NoWrap,
            PlaceholderText = @"D:\Videos\wallpaper.mp4",
            Width = 320
        };

        var browseButton = new FWButton
        {
            Width = 36,
            Height = 34,
            Content = FluentIconFactory.Regular(FluentIconRegular.FolderOpen24, 16)
        };
        browseButton.Click += (_, _) => BrowseForKind(WallpaperKind.Video);

        var sourceRow = new Grid { ColumnSpacing = 8 };
        sourceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sourceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(_videoSourceTextBox, 0);
        sourceRow.Children.Add(_videoSourceTextBox);
        Grid.SetColumn(browseButton, 1);
        sourceRow.Children.Add(browseButton);

        host.Children.Add(new FWSettingsCard
        {
            Header = "视频来源",
            Description = "选择本地视频文件作为壁纸。支持 MP4、MOV、MKV、WEBM、AVI 格式。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Folder24, 18, SubtleTextBrush()),
            Content = new Border { Width = 440, Child = sourceRow }
        });

        // 静音开关
        _videoMutedSwitch = new FWToggleSwitch
        {
            IsOn = _draftSettings.IsMuted
        };

        host.Children.Add(new FWSettingsCard
        {
            Header = "静音播放",
            Description = "视频壁纸默认静音循环播放。关闭后可听到视频原声。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.SpeakerMute24, 18, SubtleTextBrush()),
            Content = _videoMutedSwitch
        });
    }

    // ─── HTML 壁纸设置 ─────────────────────────────────────────

    private void BuildHtmlSettings(FWStackPanel host, bool keepExistingSource)
    {
        var source = keepExistingSource && _draftSettings.Kind == WallpaperKind.Html
            ? _draftSettings.Source
            : string.Empty;

        // URL / 文件路径
        _htmlSourceTextBox = new FWTextBox
        {
            Text = source,
            MinHeight = 34,
            TextWrapping = TextWrapping.NoWrap,
            PlaceholderText = "https://example.com/dashboard.html",
            Width = 320
        };

        var browseButton = new FWButton
        {
            Width = 36,
            Height = 34,
            Content = FluentIconFactory.Regular(FluentIconRegular.Document24, 16)
        };
        browseButton.Click += (_, _) => BrowseForKind(WallpaperKind.Html);

        var sourceRow = new Grid { ColumnSpacing = 8 };
        sourceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sourceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(_htmlSourceTextBox, 0);
        sourceRow.Children.Add(_htmlSourceTextBox);
        Grid.SetColumn(browseButton, 1);
        sourceRow.Children.Add(browseButton);

        host.Children.Add(new FWSettingsCard
        {
            Header = "页面来源",
            Description = "输入网页地址或选择本地 HTML 文件。支持 http、https 协议。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Globe24, 18, SubtleTextBrush()),
            Content = new Border { Width = 440, Child = sourceRow }
        });
    }

    // ─── 关于页面 ──────────────────────────────────────────────

    private void BuildAboutPage(FWStackPanel host)
    {
        host.Children.Add(CreatePageHeader(
            "关于",
            "LanMountain Always-on Display 是阑山桌面的独立组件程序。"));

        host.Children.Add(new FWSettingsCard
        {
            Header = "版本",
            Description = "独立组件预览版。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Info24, 18, SubtleTextBrush()),
            Content = new FWTextBlock
            {
                Text = "0.1.0-preview.1",
                Foreground = SubtleTextBrush()
            }
        });

        host.Children.Add(new FWSettingsCard
        {
            Header = "桌面通信",
            Description = "DotnetCampus IPC 引用已保留，后续迭代再启动通信行为。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.PlugDisconnected24, 18, SubtleTextBrush()),
            Content = new FWTextBlock
            {
                Text = "稍后",
                Foreground = SubtleTextBrush()
            }
        });
    }

    // ─── 通用组件 ──────────────────────────────────────────────

    private static UIElement CreatePageHeader(string title, string description)
    {
        return new FWStackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            Children =
            {
                new FWTextBlock
                {
                    Text = title,
                    FontSize = 28,
                    FontWeight = FontWeights.SemiBold
                },
                new FWTextBlock
                {
                    Text = description,
                    FontSize = 14,
                    Foreground = SubtleTextBrush(),
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };
    }

    private UIElement CreateActionsRow()
    {
        _statusText = new FWTextBlock
        {
            Foreground = SubtleTextBrush(),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };

        var applyButton = new FWButton
        {
            Content = CreateButtonContent(FluentIconRegular.Checkmark24, "应用"),
            MinWidth = 96,
            MinHeight = 36
        };
        applyButton.Click += (_, _) => ApplyDraftSettings();

        var actions = new Grid { ColumnSpacing = 12 };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(_statusText, 0);
        actions.Children.Add(_statusText);

        Grid.SetColumn(applyButton, 1);
        actions.Children.Add(applyButton);

        return actions;
    }

    private FWSelectorBarItem CreateWallpaperKindItem(WallpaperKind kind)
    {
        var item = new FWSelectorBarItem
        {
            Text = GetKindTitle(kind),
            Icon = FluentIconFactory.Regular(GetKindIcon(kind), 16),
            Tag = kind
        };
        _wallpaperKindItems[kind] = item;
        return item;
    }

    // ─── 文件浏览 ──────────────────────────────────────────────

    private void BrowseForKind(WallpaperKind kind)
    {
        var dialog = new OpenFileDialog
        {
            Title = GetDialogTitle(kind),
            Filter = GetDialogFilter(kind),
            CheckFileExists = true,
            Multiselect = false
        };

        var result = dialog.ShowDialog();
        if (result != true || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        var textBox = kind switch
        {
            WallpaperKind.Image => _imageSourceTextBox,
            WallpaperKind.Video => _videoSourceTextBox,
            WallpaperKind.Html => _htmlSourceTextBox,
            _ => null
        };

        if (textBox != null)
        {
            textBox.Text = dialog.FileName;
            SetStatus("已选择文件，点击应用后生效。");
        }
    }

    // ─── 应用设置 ──────────────────────────────────────────────

    private void ApplyDraftSettings()
    {
        var source = GetSourceFromCurrentKind();
        if (string.IsNullOrWhiteSpace(source))
        {
            SetStatus("请先选择或输入背景来源。");
            return;
        }

        if (!IsSourceUsable(_selectedKind, source))
        {
            SetStatus("背景来源无效，请检查文件路径或网页地址。");
            return;
        }

        var isMuted = _selectedKind == WallpaperKind.Video
            ? _videoMutedSwitch?.IsOn ?? true
            : true;

        var placement = _selectedKind == WallpaperKind.Image
            ? GetPlacementFromCombo()
            : WallpaperSettings.DefaultPlacement;

        _draftSettings = new WallpaperSettings(_selectedKind, source, isMuted, placement).Normalize();
        _applySettings(_draftSettings);
        NavigateTo(WallpaperPageKey);
        SetStatus("已应用到背景层。");
    }

    private string GetPlacementFromCombo()
    {
        return _imagePlacementCombo?.SelectedIndex switch
        {
            0 => "Fill",
            1 => "Fit",
            2 => "Stretch",
            3 => "Center",
            4 => "Tile",
            _ => WallpaperSettings.DefaultPlacement
        };
    }

    private string? GetSourceFromCurrentKind()
    {
        return _selectedKind switch
        {
            WallpaperKind.Image => _imageSourceTextBox?.Text.Trim(),
            WallpaperKind.Video => _videoSourceTextBox?.Text.Trim(),
            WallpaperKind.Html => _htmlSourceTextBox?.Text.Trim(),
            _ => null
        };
    }

    private static bool IsSourceUsable(WallpaperKind kind, string source)
    {
        if (kind == WallpaperKind.Html && Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            if (uri.IsFile)
            {
                return File.Exists(uri.LocalPath);
            }

            return uri.Scheme is "http" or "https" or "file";
        }

        if (source == WallpaperSettings.BundledPreviewSource)
        {
            return kind == WallpaperKind.Image;
        }

        if (Uri.TryCreate(source, UriKind.Absolute, out var absoluteUri))
        {
            if (absoluteUri.IsFile)
            {
                return File.Exists(absoluteUri.LocalPath);
            }

            return kind == WallpaperKind.Html && absoluteUri.Scheme is "http" or "https";
        }

        return Path.IsPathRooted(source) && File.Exists(source);
    }

    private void SetStatus(string text)
    {
        if (_statusText != null)
        {
            _statusText.Text = text;
        }
    }

    // ─── 静态辅助方法 ──────────────────────────────────────────

    private static UIElement CreateButtonContent(FluentIconRegular icon, string text)
    {
        return new FWStackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                FluentIconFactory.Regular(icon, 16),
                new FWTextBlock
                {
                    Text = text,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
    }

    private static UIElement CreateNavigationContent(FluentIconRegular icon, string text)
    {
        return new FWStackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                FluentIconFactory.Regular(icon, 18),
                new FWTextBlock
                {
                    Text = text,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
    }

    private static string GetKindTitle(WallpaperKind kind)
        => kind switch
        {
            WallpaperKind.Image => "图片",
            WallpaperKind.Video => "视频",
            WallpaperKind.Html => "HTML",
            _ => "背景"
        };

    private static string GetStatusText(WallpaperKind kind)
        => kind switch
        {
            WallpaperKind.Image => "支持 PNG、JPG、JPEG、BMP、WEBP。",
            WallpaperKind.Video => "支持 Jalium.UI 媒体管线可解码的视频文件。",
            WallpaperKind.Html => "支持 http、https 地址，也可以选择本地 HTML 文件。",
            _ => string.Empty
        };

    private static string GetDialogTitle(WallpaperKind kind)
        => kind switch
        {
            WallpaperKind.Image => "选择图片壁纸",
            WallpaperKind.Video => "选择视频壁纸",
            WallpaperKind.Html => "选择 HTML 壁纸",
            _ => "选择壁纸"
        };

    private static string GetDialogFilter(WallpaperKind kind)
        => kind switch
        {
            WallpaperKind.Image => "Images (*.png;*.jpg;*.jpeg;*.bmp;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All files (*.*)|*.*",
            WallpaperKind.Video => "Videos (*.mp4;*.mov;*.mkv;*.webm;*.avi)|*.mp4;*.mov;*.mkv;*.webm;*.avi|All files (*.*)|*.*",
            WallpaperKind.Html => "HTML (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*",
            _ => "All files (*.*)|*.*"
        };

    private static FluentIconRegular GetKindIcon(WallpaperKind kind)
        => kind switch
        {
            WallpaperKind.Image => FluentIconRegular.Image24,
            WallpaperKind.Video => FluentIconRegular.Video24,
            WallpaperKind.Html => FluentIconRegular.Code24,
            _ => FluentIconRegular.Desktop24
        };

    private static int GetPlacementIndex(string? placement)
        => placement?.Trim().ToLowerInvariant() switch
        {
            "fit" => 1,
            "stretch" => 2,
            "center" => 3,
            "tile" => 4,
            _ => 0
        };

    private static SolidColorBrush SubtleTextBrush()
        => new(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF));
}
