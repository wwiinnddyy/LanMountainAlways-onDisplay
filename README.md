# LanMountainAlways-onDisplay

LanMountainAlways-onDisplay is an independent Jalium.UI companion app for LanMountainDesktop. The first MVP opens a full-screen preview window and renders a bundled image.

## Build

```powershell
dotnet restore LanMountainAlwaysOnDisplay.slnx
dotnet build LanMountainAlwaysOnDisplay.slnx -c Debug /p:UseSharedCompilation=false
dotnet build LanMountainAlwaysOnDisplay.slnx -c Release /p:UseSharedCompilation=false
```

The project uses local sibling source references by default:

- `D:\github\Jalium\Jalium.UI`
- `D:\github\Jalium\FluentJalium`
- `D:\github\LanDesktop\LanMountainDesktop`

Override `JaliumSourceRoot`, `FluentJaliumSourceRoot`, or `LanMountainDesktopSourceRoot` when building from a different checkout layout.
