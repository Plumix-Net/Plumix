using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: cupertino_ui/lib/src/icons.dart

namespace Plumix.Cupertino;

public static partial class CupertinoIcons
{
    // Dart's cupertino_icons package declares the font; a style names it as
    // `packages/cupertino_icons/CupertinoIcons`.
    static CupertinoIcons()
    {
        _ = new FontLoader($"packages/{IconFontPackage}/{IconFont}")
            .AddFont(PlatformAssetBundle.Root.LoadBytesAsync(
                "packages/Plumix.Cupertino/Assets/Fonts/CupertinoIcons.ttf"))
            .Load();
    }
}
