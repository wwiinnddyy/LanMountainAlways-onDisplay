using FluentJalium.Controls;
using FluentJalium.Icon;
using Jalium.UI;
using Jalium.UI.Controls;
using Jalium.UI.Media;

namespace LanMountainAlwaysOnDisplay.Views;

public partial class AboutSettingsPage : UserControl
{
    public AboutSettingsPage()
    {
        InitializeComponent();
        InitializeIcons();
    }

    private void InitializeIcons()
    {
        ((FWSettingsCard)VersionCard).HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.Info24, 18, SubtleTextBrush());
        ((FWSettingsCard)CommunicationCard).HeaderIcon = FluentIconFactory.Regular(FluentIconRegular.PlugDisconnected24, 18, SubtleTextBrush());
    }

    private static SolidColorBrush SubtleTextBrush() => new(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF));
}
