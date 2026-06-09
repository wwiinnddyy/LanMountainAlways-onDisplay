namespace LanMountainAlwaysOnDisplay;

public sealed record WallpaperSettings(WallpaperKind Kind, string Source)
{
    public const string BundledPreviewSource = "Assets/Display/preview.png";

    public static WallpaperSettings Default { get; } = new(WallpaperKind.Image, BundledPreviewSource);

    public WallpaperSettings Normalize()
    {
        var source = string.IsNullOrWhiteSpace(Source) ? BundledPreviewSource : Source.Trim();
        return this with { Source = source };
    }
}
