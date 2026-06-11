namespace LanMountainAlwaysOnDisplay;

public sealed record WallpaperSettings(
    WallpaperKind Kind,
    string Source,
    bool IsMuted = true,
    string? Placement = null)
{
    public const string BundledPreviewSource = "Assets/Display/preview.png";
    public const string DefaultPlacement = "Fill";

    public static WallpaperSettings Default { get; } = new(WallpaperKind.Image, BundledPreviewSource);

    public WallpaperSettings Normalize()
    {
        var source = string.IsNullOrWhiteSpace(Source) ? BundledPreviewSource : Source.Trim();
        var placement = string.IsNullOrWhiteSpace(Placement) ? DefaultPlacement : Placement.Trim();
        return this with { Source = source, Placement = placement };
    }
}
