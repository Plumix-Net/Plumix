using Plumix.Rendering;
using Plumix.UI;

// Dart parity source: material_ui/lib/src/icons.dart

namespace Plumix.Material;

public static partial class Icons
{
    // Dart's app declares the font through `uses-material-design: true`; the package registers it.
    // The bundle hands the bytes over synchronously, so the family exists before the first icon.
    static Icons()
    {
        _ = new FontLoader(IconFont)
            .AddFont(PlatformAssetBundle.Root.LoadBytesAsync(
                "packages/Plumix.Material/Assets/Fonts/MaterialIcons-Regular.otf"))
            .Load();
    }
}
