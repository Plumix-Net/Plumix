using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;
using Plumix.UI;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/switch_theme.dart

public sealed partial record SwitchThemeData(
    WidgetStateProperty<Color?>? ThumbColor = null,
    WidgetStateProperty<Color?>? TrackColor = null,
    WidgetStateProperty<Color?>? TrackOutlineColor = null,
    WidgetStateProperty<double?>? TrackOutlineWidth = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null,
    WidgetStateProperty<MouseCursor?>? MouseCursor = null,
    WidgetStateProperty<Color?>? OverlayColor = null,
    double? SplashRadius = null,
    WidgetStateProperty<Icon?>? ThumbIcon = null,
    Thickness? Padding = null);

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

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((SwitchTheme)oldWidget).Data, Data);
    }

    public static SwitchThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<SwitchTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).SwitchTheme;
    }
}
