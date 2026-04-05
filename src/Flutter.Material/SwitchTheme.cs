using Avalonia;
using Avalonia.Media;
using Flutter.Foundation;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/switch_theme.dart (approximate)

public sealed record SwitchThemeData(
    MaterialStateProperty<Color?>? ThumbColor = null,
    MaterialStateProperty<Color?>? TrackColor = null,
    MaterialStateProperty<Color?>? TrackOutlineColor = null,
    MaterialStateProperty<double?>? TrackOutlineWidth = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    double? SplashRadius = null,
    MaterialStateProperty<Icon?>? ThumbIcon = null,
    Thickness? Padding = null);

public sealed class SwitchTheme : InheritedWidget
{
    public SwitchTheme(
        SwitchThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public SwitchThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
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
