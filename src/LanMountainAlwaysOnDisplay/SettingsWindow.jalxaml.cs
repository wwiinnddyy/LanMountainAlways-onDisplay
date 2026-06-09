using FluentJalium.Controls;
using FluentJalium.Icon;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Media;

namespace LanMountainAlwaysOnDisplay;

public partial class SettingsWindow : Window
{
    private const string BackgroundPageKey = "background";
    private const string ContentPageKey = "content";
    private const string DisplayPageKey = "display";
    private const string AboutPageKey = "about";

    private readonly Action<WallpaperSettings> _applySettings;

    private WallpaperSettings _draftSettings;
    private WallpaperKind _selectedKind;
    private FWNavigationView? _navigationView;
    private FWNavigationViewItem? _backgroundItem;
    private FWNavigationViewItem? _contentItem;
    private FWNavigationViewItem? _displayItem;
    private FWNavigationViewItem? _aboutItem;
    private FWStackPanel? _contentHost;
    private FWTextBlock? _statusText;
    private FWTextBox? _sourceTextBox;
    private FWButton? _browseButton;
    private FWButton? _useBundledButton;
    private readonly Dictionary<WallpaperKind, FWButton> _wallpaperKindButtons = [];

    public SettingsWindow(WallpaperSettings currentSettings, Action<WallpaperSettings> applySettings)
    {
        _draftSettings = currentSettings.Normalize();
        _selectedKind = _draftSettings.Kind;
        _applySettings = applySettings;

        InitializeComponent();
        Content = CreateContent();
        NavigateTo(BackgroundPageKey);
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

        _backgroundItem = CreateNavigationItem(BackgroundPageKey, "背景", FluentIconRegular.Image24);
        _contentItem = CreateNavigationItem(ContentPageKey, "内容层", FluentIconRegular.Layer24);
        _displayItem = CreateNavigationItem(DisplayPageKey, "显示", FluentIconRegular.Desktop24);
        _aboutItem = CreateNavigationItem(AboutPageKey, "关于", FluentIconRegular.Info24);

        _navigationView.MenuItems.Add(_backgroundItem);
        _navigationView.MenuItems.Add(_contentItem);
        _navigationView.MenuItems.Add(_displayItem);
        _navigationView.FooterMenuItems.Add(_aboutItem);
        _navigationView.SelectionChanged += (_, e) =>
        {
            if (e.SelectedItem?.Tag is string pageKey)
            {
                NavigateTo(pageKey);
            }
        };
        _navigationView.UpdateMenuItems();
        _navigationView.SelectedItem = _backgroundItem;

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
            Content = title,
            Icon = FluentIconFactory.Regular(icon, 18),
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
        _wallpaperKindButtons.Clear();
        _sourceTextBox = null;
        _browseButton = null;
        _useBundledButton = null;
        _statusText = null;

        switch (pageKey)
        {
            case BackgroundPageKey:
                BuildBackgroundPage(_contentHost);
                break;
            case ContentPageKey:
                BuildContentLayerPage(_contentHost);
                break;
            case DisplayPageKey:
                BuildDisplayPage(_contentHost);
                break;
            case AboutPageKey:
                BuildAboutPage(_contentHost);
                break;
            default:
                BuildBackgroundPage(_contentHost);
                break;
        }
    }

    private void BuildBackgroundPage(FWStackPanel host)
    {
        host.Children.Add(CreatePageHeader(
            "背景",
            "选择全天候显示的背景来源。背景层可以承载静态图片、动态视频或 HTML 页面。"));
        host.Children.Add(CreateCurrentWallpaperCard());
        host.Children.Add(CreateWallpaperKindCard());
        host.Children.Add(CreateWallpaperSourceCard());
        host.Children.Add(CreateActionsRow());
        UpdateWallpaperKind(_selectedKind, keepExistingSource: true);
    }

    private void BuildContentLayerPage(FWStackPanel host)
    {
        host.Children.Add(CreatePageHeader(
            "内容层",
            "内容层用于承载后续交互控件，本轮先保留设置入口和结构占位。"));
        host.Children.Add(new FWSettingsCard
        {
            Header = "设置入口",
            Description = "主窗口右上角常驻显示设置按钮，后续可改为悬浮或自动隐藏。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Settings24, 18, SubtleTextBrush()),
            Content = new FWTextBlock
            {
                Text = "已启用",
                Foreground = SubtleTextBrush()
            }
        });
        host.Children.Add(CreatePlaceholderCard(
            FluentIconRegular.Layer24,
            "内容控件",
            "后续会在这里配置时间、天气、日程等全天候显示内容。"));
    }

    private void BuildDisplayPage(FWStackPanel host)
    {
        host.Children.Add(CreatePageHeader(
            "显示",
            "管理窗口显示方式和屏幕行为。当前主窗口保持无边框全屏显示。"));
        host.Children.Add(new FWSettingsCard
        {
            Header = "窗口模式",
            Description = "主窗口使用无边框全屏，背景按比例填充。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.FullScreenMaximize24, 18, SubtleTextBrush()),
            Content = new FWTextBlock
            {
                Text = "全屏",
                Foreground = SubtleTextBrush()
            }
        });
        host.Children.Add(CreatePlaceholderCard(
            FluentIconRegular.Desktop24,
            "目标屏幕",
            "多屏选择会在后续与阑山桌面联动时接入。"));
    }

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
        host.Children.Add(CreatePlaceholderCard(
            FluentIconRegular.PlugDisconnected24,
            "桌面通信",
            "DotnetCampus IPC 引用已保留，后续迭代再启动通信行为。"));
    }

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

    private UIElement CreateCurrentWallpaperCard()
    {
        return new FWSettingsCard
        {
            Header = "当前背景",
            Description = "已经应用到主窗口背景层的来源。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Desktop24, 18, SubtleTextBrush()),
            Content = new Border
            {
                Width = 360,
                Child = new FWTextBlock
                {
                    Text = $"{GetKindTitle(_draftSettings.Kind)}\n{_draftSettings.Source}",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = SubtleTextBrush()
                }
            }
        };
    }

    private UIElement CreateWallpaperKindCard()
    {
        var selector = new FWStackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        selector.Children.Add(CreateWallpaperKindButton(WallpaperKind.Image));
        selector.Children.Add(CreateWallpaperKindButton(WallpaperKind.Video));
        selector.Children.Add(CreateWallpaperKindButton(WallpaperKind.Html));

        return new FWSettingsCard
        {
            Header = "背景类型",
            Description = "选择背景层要承载的内容类型。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Layer24, 18, SubtleTextBrush()),
            Content = selector
        };
    }

    private UIElement CreateWallpaperSourceCard()
    {
        _sourceTextBox = new FWTextBox
        {
            MinHeight = 34,
            TextWrapping = TextWrapping.NoWrap
        };

        _browseButton = new FWButton
        {
            MinWidth = 96
        };
        _browseButton.Click += (_, _) => BrowseForSelectedKind();

        _useBundledButton = new FWButton
        {
            Content = "使用内置预览",
            MinWidth = 112
        };
        _useBundledButton.Click += (_, _) =>
        {
            if (_sourceTextBox != null)
            {
                _sourceTextBox.Text = WallpaperSettings.BundledPreviewSource;
                SetStatus("已切换为内置预览图。");
            }
        };

        var sourceRow = new Grid { ColumnSpacing = 8 };
        sourceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sourceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        sourceRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(_sourceTextBox, 0);
        sourceRow.Children.Add(_sourceTextBox);

        Grid.SetColumn(_browseButton, 1);
        sourceRow.Children.Add(_browseButton);

        Grid.SetColumn(_useBundledButton, 2);
        sourceRow.Children.Add(_useBundledButton);

        return new FWSettingsCard
        {
            Header = "来源",
            Description = "图片和视频使用本地文件；HTML 支持本地文件或网页地址。",
            HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Folder24, 18, SubtleTextBrush()),
            Content = new Border
            {
                Width = 440,
                Child = sourceRow
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

    private FWButton CreateWallpaperKindButton(WallpaperKind kind)
    {
        var button = new FWButton
        {
            MinWidth = 104,
            MinHeight = 36,
            Content = CreateButtonContent(GetKindIcon(kind), GetKindTitle(kind))
        };
        button.Click += (_, _) => UpdateWallpaperKind(kind, keepExistingSource: false);
        _wallpaperKindButtons[kind] = button;
        return button;
    }

    private static FWSettingsCard CreatePlaceholderCard(
        FluentIconRegular icon,
        string header,
        string description)
    {
        return new FWSettingsCard
        {
            Header = header,
            Description = description,
            HeaderIcon = FluentIconFactory.Regular(icon, 18, SubtleTextBrush()),
            Content = new FWTextBlock
            {
                Text = "稍后",
                Foreground = SubtleTextBrush()
            }
        };
    }

    private void UpdateWallpaperKind(WallpaperKind kind, bool keepExistingSource)
    {
        _selectedKind = kind;
        var source = keepExistingSource && _draftSettings.Kind == kind
            ? _draftSettings.Source
            : GetDefaultSource(kind);

        if (_sourceTextBox != null)
        {
            _sourceTextBox.Text = source;
            _sourceTextBox.PlaceholderText = GetSourcePlaceholder(kind);
        }

        if (_browseButton != null)
        {
            _browseButton.Content = CreateButtonContent(
                kind == WallpaperKind.Html ? FluentIconRegular.Document24 : FluentIconRegular.FolderOpen24,
                kind == WallpaperKind.Html ? "选择 HTML" : "浏览");
        }

        if (_useBundledButton != null)
        {
            _useBundledButton.Visibility = kind == WallpaperKind.Image ? Visibility.Visible : Visibility.Collapsed;
        }

        foreach (var pair in _wallpaperKindButtons)
        {
            pair.Value.Content = CreateButtonContent(
                pair.Key == kind ? FluentIconRegular.Checkmark24 : GetKindIcon(pair.Key),
                GetKindTitle(pair.Key));
        }

        SetStatus(GetStatusText(kind));
    }

    private void BrowseForSelectedKind()
    {
        var dialog = new OpenFileDialog
        {
            Title = GetDialogTitle(_selectedKind),
            Filter = GetDialogFilter(_selectedKind),
            CheckFileExists = true,
            Multiselect = false
        };

        var result = dialog.ShowDialog();
        if (result == true && !string.IsNullOrWhiteSpace(dialog.FileName) && _sourceTextBox != null)
        {
            _sourceTextBox.Text = dialog.FileName;
            SetStatus("已选择文件，点击应用后生效。");
        }
    }

    private void ApplyDraftSettings()
    {
        if (_sourceTextBox == null)
        {
            return;
        }

        var source = _sourceTextBox.Text.Trim();
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

        _draftSettings = new WallpaperSettings(_selectedKind, source).Normalize();
        _applySettings(_draftSettings);
        NavigateTo(BackgroundPageKey);
        SetStatus("已应用到背景层。");
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

    private static string GetDefaultSource(WallpaperKind kind)
        => kind == WallpaperKind.Image ? WallpaperSettings.BundledPreviewSource : string.Empty;

    private static string GetKindTitle(WallpaperKind kind)
        => kind switch
        {
            WallpaperKind.Image => "图片",
            WallpaperKind.Video => "视频",
            WallpaperKind.Html => "HTML",
            _ => "背景"
        };

    private static string GetSourcePlaceholder(WallpaperKind kind)
        => kind switch
        {
            WallpaperKind.Image => @"D:\Pictures\wallpaper.png",
            WallpaperKind.Video => @"D:\Videos\wallpaper.mp4",
            WallpaperKind.Html => "https://example.com/dashboard.html",
            _ => string.Empty
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
            WallpaperKind.Image => "选择图片背景",
            WallpaperKind.Video => "选择视频背景",
            WallpaperKind.Html => "选择 HTML 背景",
            _ => "选择背景"
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

    private static SolidColorBrush SubtleTextBrush()
        => new(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF));
}
