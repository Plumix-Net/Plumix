using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;
using Plumix.UI;
using Plumix.Rendering;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/switch_theme.dart

public sealed partial record SwitchThemeData(
    IWidgetStateProperty<Color?>? ThumbColor = null,
    IWidgetStateProperty<Color?>? TrackColor = null,
    IWidgetStateProperty<Color?>? TrackOutlineColor = null,
    WidgetStateProperty<double?>? TrackOutlineWidth = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null,
    IWidgetStateProperty<MouseCursor?>? MouseCursor = null,
    IWidgetStateProperty<Color?>? OverlayColor = null,
    double? SplashRadius = null,
    WidgetStateProperty<Icon?>? ThumbIcon = null,
    EdgeInsetsGeometry? Padding = null);

public sealed class SwitchTheme : InheritedWidget
{
    public SwitchTheme(
        SwitchThemeData data,
        Widget child,
        Key? key = null) : base(child, key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public SwitchThemeData Data { get; }

    public override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((SwitchTheme)oldWidget).Data, Data);
    }

    public static SwitchThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInheritedWidgetOfExactType<SwitchTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).SwitchTheme;
    }
}
