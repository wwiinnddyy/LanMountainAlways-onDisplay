using System;
using FluentJalium.Controls;
using FluentJalium.Icon;
using Jalium.UI;
using Jalium.UI.Controls;
using LanMountainAlwaysOnDisplay.Views;

namespace LanMountainAlwaysOnDisplay;

public partial class SettingsWindow : Window
{
    private const string WallpaperPageKey = "wallpaper";
    private const string AboutPageKey = "about";

    private readonly Action<WallpaperSettings> _applySettings;
    private readonly WallpaperSettings _currentSettings;

    private WallpaperSettingsPage? _wallpaperPage;
    private AboutSettingsPage? _aboutPage;

    public SettingsWindow(WallpaperSettings currentSettings, Action<WallpaperSettings> applySettings)
    {
        _currentSettings = currentSettings;
        _applySettings = applySettings;

        InitializeComponent();
        InitializeIcons();

        ((FWNavigationView)NavigationView).SelectedItem = WallpaperItem;
        NavigateTo(WallpaperPageKey);
    }

    private void InitializeIcons()
    {
        ((ContentControl)WallpaperIconHost).Content = FluentIconFactory.Regular(FluentIconRegular.Image24, 18);
        ((ContentControl)AboutIconHost).Content = FluentIconFactory.Regular(FluentIconRegular.Info24, 18);
    }

    private void NavigationView_SelectionChanged(object sender, EventArgs e)
    {
        if (((FWNavigationView)NavigationView).SelectedItem is FWNavigationViewItem item && item.Tag is string pageKey)
        {
            NavigateTo(pageKey);
        }
    }

    private void NavigateTo(string pageKey)
    {
        if (ContentHost == null)
        {
            return;
        }

        switch (pageKey)
        {
            case WallpaperPageKey:
                if (_wallpaperPage == null)
                {
                    _wallpaperPage = new WallpaperSettingsPage(_currentSettings);
                    _wallpaperPage.SettingsApplied += (settings) => _applySettings(settings);
                }
                ((ContentControl)ContentHost).Content = _wallpaperPage;
                break;
            case AboutPageKey:
                _aboutPage ??= new AboutSettingsPage();
                ((ContentControl)ContentHost).Content = _aboutPage;
                break;
            default:
                if (_wallpaperPage == null)
                {
                    _wallpaperPage = new WallpaperSettingsPage(_currentSettings);
                    _wallpaperPage.SettingsApplied += (settings) => _applySettings(settings);
                }
                ((ContentControl)ContentHost).Content = _wallpaperPage;
                break;
        }
    }
}
