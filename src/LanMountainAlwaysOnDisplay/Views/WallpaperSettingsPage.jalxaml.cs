using System;
using System.Collections.Generic;
using System.IO;
using FluentJalium.Controls;
using FluentJalium.Icon;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Media;

namespace LanMountainAlwaysOnDisplay.Views;

public partial class WallpaperSettingsPage : UserControl
{
    private WallpaperSettings _draftSettings;
    private WallpaperKind _selectedKind;
    private readonly Dictionary<WallpaperKind, FWSelectorBarItem> _wallpaperKindItems = [];

    public event Action<WallpaperSettings>? SettingsApplied;

    public WallpaperSettingsPage(WallpaperSettings currentSettings)
    {
        _draftSettings = currentSettings.Normalize();
        _selectedKind = _draftSettings.Kind;

        InitializeComponent();
        InitializeIcons();
        InitializeKindSelector();
        UpdateWallpaperKind(_selectedKind, keepExistingSource: true);
    }

    private void InitializeIcons()
    {
        ((FWSettingsCard)KindCard).HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Layer24, 18, SubtleTextBrush());
        
        ((FWSettingsCard)ImageSourceCard).HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Folder24, 18, SubtleTextBrush());
        ((FWSettingsCard)ImagePlacementCard).HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.FullScreenMaximize24, 18, SubtleTextBrush());
        ((FWButton)ImageBrowseButton).Content = FluentIconFactory.Regular(FluentIconRegular.FolderOpen24, 16);
        ((FWButton)ImageBundledButton).Content = FluentIconFactory.Regular(FluentIconRegular.Image24, 16);
        
        ((FWSettingsCard)VideoSourceCard).HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Folder24, 18, SubtleTextBrush());
        ((FWSettingsCard)VideoMuteCard).HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.SpeakerMute24, 18, SubtleTextBrush());
        ((FWButton)VideoBrowseButton).Content = FluentIconFactory.Regular(FluentIconRegular.FolderOpen24, 16);
        
        ((FWSettingsCard)HtmlSourceCard).HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Globe24, 18, SubtleTextBrush());
        ((FWButton)HtmlBrowseButton).Content = FluentIconFactory.Regular(FluentIconRegular.Document24, 16);

        ((FWButton)ApplyButton).Content = CreateButtonContent(FluentIconRegular.Checkmark24, "应用");
    }

    private void InitializeKindSelector()
    {
        var selector = (FWSelectorBar)WallpaperKindSelector;
        selector.Items.Add(CreateWallpaperKindItem(WallpaperKind.Image));
        selector.Items.Add(CreateWallpaperKindItem(WallpaperKind.Video));
        selector.Items.Add(CreateWallpaperKindItem(WallpaperKind.Html));
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

    private void WallpaperKindSelector_SelectionChanged(object sender, EventArgs e)
    {
        if (((FWSelectorBar)WallpaperKindSelector).SelectedItem is FWSelectorBarItem item && item.Tag is WallpaperKind kind)
        {
            UpdateWallpaperKind(kind, keepExistingSource: false);
        }
    }

    private void UpdateWallpaperKind(WallpaperKind kind, bool keepExistingSource)
    {
        _selectedKind = kind;

        if (_wallpaperKindItems.TryGetValue(kind, out var selectedItem))
        {
            ((FWSelectorBar)WallpaperKindSelector).SelectedItem = selectedItem;
        }

        ImageSettingsPanel.Visibility = kind == WallpaperKind.Image ? Visibility.Visible : Visibility.Collapsed;
        VideoSettingsPanel.Visibility = kind == WallpaperKind.Video ? Visibility.Visible : Visibility.Collapsed;
        HtmlSettingsPanel.Visibility = kind == WallpaperKind.Html ? Visibility.Visible : Visibility.Collapsed;

        if (kind == WallpaperKind.Image)
        {
            var source = keepExistingSource && _draftSettings.Kind == WallpaperKind.Image ? _draftSettings.Source : WallpaperSettings.BundledPreviewSource;
            ((FWTextBox)ImageSourceTextBox).Text = source;
            ((FWComboBox)ImagePlacementCombo).SelectedIndex = GetPlacementIndex(_draftSettings.Placement);
        }
        else if (kind == WallpaperKind.Video)
        {
            var source = keepExistingSource && _draftSettings.Kind == WallpaperKind.Video ? _draftSettings.Source : string.Empty;
            ((FWTextBox)VideoSourceTextBox).Text = source;
            ((FWToggleSwitch)VideoMutedSwitch).IsOn = _draftSettings.IsMuted;
        }
        else if (kind == WallpaperKind.Html)
        {
            var source = keepExistingSource && _draftSettings.Kind == WallpaperKind.Html ? _draftSettings.Source : string.Empty;
            ((FWTextBox)HtmlSourceTextBox).Text = source;
        }

        SetStatus(GetStatusText(kind));
    }

    private void ImageBrowseButton_Click(object sender, Jalium.UI.RoutedEventArgs e) => BrowseForKind(WallpaperKind.Image);
    
    private void ImageBundledButton_Click(object sender, Jalium.UI.RoutedEventArgs e)
    {
        ((FWTextBox)ImageSourceTextBox).Text = WallpaperSettings.BundledPreviewSource;
        SetStatus("已切换为内置预览图。");
    }

    private void VideoBrowseButton_Click(object sender, Jalium.UI.RoutedEventArgs e) => BrowseForKind(WallpaperKind.Video);
    private void HtmlBrowseButton_Click(object sender, Jalium.UI.RoutedEventArgs e) => BrowseForKind(WallpaperKind.Html);

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
            WallpaperKind.Image => (FWTextBox)ImageSourceTextBox,
            WallpaperKind.Video => (FWTextBox)VideoSourceTextBox,
            WallpaperKind.Html => (FWTextBox)HtmlSourceTextBox,
            _ => null
        };

        if (textBox != null)
        {
            textBox.Text = dialog.FileName;
            SetStatus("已选择文件，点击应用后生效。");
        }
    }

    private void ApplyButton_Click(object sender, Jalium.UI.RoutedEventArgs e)
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

        var isMuted = _selectedKind == WallpaperKind.Video ? ((FWToggleSwitch)VideoMutedSwitch).IsOn : true;

        var placement = _selectedKind == WallpaperKind.Image
            ? GetPlacementFromCombo()
            : WallpaperSettings.DefaultPlacement;

        _draftSettings = new WallpaperSettings(_selectedKind, source, isMuted, placement).Normalize();
        SettingsApplied?.Invoke(_draftSettings);
        UpdateWallpaperKind(_selectedKind, keepExistingSource: true);
        SetStatus("已应用到背景层。");
    }

    private string GetPlacementFromCombo()
    {
        return ((FWComboBox)ImagePlacementCombo).SelectedIndex switch
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
            WallpaperKind.Image => ((FWTextBox)ImageSourceTextBox).Text.Trim(),
            WallpaperKind.Video => ((FWTextBox)VideoSourceTextBox).Text.Trim(),
            WallpaperKind.Html => ((FWTextBox)HtmlSourceTextBox).Text.Trim(),
            _ => null
        };
    }

    private static bool IsSourceUsable(WallpaperKind kind, string source)
    {
        if (kind == WallpaperKind.Html && Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            if (uri.IsFile) return File.Exists(uri.LocalPath);
            return uri.Scheme is "http" or "https" or "file";
        }
        if (source == WallpaperSettings.BundledPreviewSource) return kind == WallpaperKind.Image;
        
        if (Uri.TryCreate(source, UriKind.Absolute, out var absoluteUri))
        {
            if (absoluteUri.IsFile) return File.Exists(absoluteUri.LocalPath);
            return kind == WallpaperKind.Html && absoluteUri.Scheme is "http" or "https";
        }

        return Path.IsPathRooted(source) && File.Exists(source);
    }

    private void SetStatus(string text)
    {
        ((FWTextBlock)StatusText).Text = text;
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

    private static string GetKindTitle(WallpaperKind kind) => kind switch
    {
        WallpaperKind.Image => "图片",
        WallpaperKind.Video => "视频",
        WallpaperKind.Html => "HTML",
        _ => "背景"
    };

    private static string GetStatusText(WallpaperKind kind) => kind switch
    {
        WallpaperKind.Image => "支持 PNG、JPG、JPEG、BMP、WEBP。",
        WallpaperKind.Video => "支持 Jalium.UI 媒体管线可解码的视频文件。",
        WallpaperKind.Html => "支持 http、https 地址，也可以选择本地 HTML 文件。",
        _ => string.Empty
    };

    private static string GetDialogTitle(WallpaperKind kind) => kind switch
    {
        WallpaperKind.Image => "选择图片壁纸",
        WallpaperKind.Video => "选择视频壁纸",
        WallpaperKind.Html => "选择 HTML 壁纸",
        _ => "选择壁纸"
    };

    private static string GetDialogFilter(WallpaperKind kind) => kind switch
    {
        WallpaperKind.Image => "Images (*.png;*.jpg;*.jpeg;*.bmp;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All files (*.*)|*.*",
        WallpaperKind.Video => "Videos (*.mp4;*.mov;*.mkv;*.webm;*.avi)|*.mp4;*.mov;*.mkv;*.webm;*.avi|All files (*.*)|*.*",
        WallpaperKind.Html => "HTML (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*",
        _ => "All files (*.*)|*.*"
    };

    private static FluentIconRegular GetKindIcon(WallpaperKind kind) => kind switch
    {
        WallpaperKind.Image => FluentIconRegular.Image24,
        WallpaperKind.Video => FluentIconRegular.Video24,
        WallpaperKind.Html => FluentIconRegular.Code24,
        _ => FluentIconRegular.Desktop24
    };

    private static int GetPlacementIndex(string? placement) => placement?.Trim().ToLowerInvariant() switch
    {
        "fit" => 1,
        "stretch" => 2,
        "center" => 3,
        "tile" => 4,
        _ => 0
    };

    private static SolidColorBrush SubtleTextBrush() => new(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF));
}
